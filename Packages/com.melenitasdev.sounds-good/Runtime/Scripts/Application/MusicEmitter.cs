/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using UnityEngine;
using UnityEngine.Events;

namespace MelenitasDev.SoundsGood
{
    // Every setting is serialized but kept private: the custom editor binds to it by name and the
    // Inspector edits it, yet game code can't reach in and desync the emitter. To reconfigure at
    // runtime, go through the encapsulated Music object (e.g. emitter.Music.SetVolume(...)).
    public partial class MusicEmitter // Properties
    {
        [SerializeField]
        [Tooltip("The music track, created in the Audio Creator window, that this emitter plays.")]
        private Track track;
        [SerializeField]
        [Tooltip("Pick a random clip among those registered under the track tag each time it plays.")]
        private bool randomClip = true;
        [SerializeField]
        [Tooltip("Index of the specific clip to play, used when Random Clip is off.")]
        private int clipIndex = 0;

        [SerializeField]
        [Tooltip("Play automatically when the scene starts (on Start).")]
        private bool playOnStart = false;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Playback volume, from 0 (silent) to 1 (full).")]
        private float volume = 1f;
        [SerializeField]
        [Tooltip("Pick a random volume within a range each time it plays, to avoid repetition.")]
        private bool randomVolume = false;
        [SerializeField]
        [Tooltip("Minimum and maximum volume used when Random Volume is on.")]
        private Vector2 volumeRange = new Vector2(0.8f, 1f);
        [SerializeField]
        [Tooltip("Loop the music until it's stopped.")]
        private bool loop = true;
        [SerializeField]
        [Range(-3f, 3f)]
        [Tooltip("Fixed playback pitch (1 = original). Negative values play the clip reversed.")]
        private float pitch = 1f;

        [SerializeField]
        [Tooltip("3D positional audio. Turn off for 2D / global music.")]
        private bool spatialSound = false;
        //#SG_PRO_BEGIN
        [SerializeField]
        [Tooltip("Muffle the music when obstacles block the path between it and the listener.")]
        private bool useOcclusion = false;
        //#SG_PRO_END
//#SG_PRO_BEGIN
        [SerializeField]
        [Tooltip("Scaled: playback speed follows Time.timeScale (faster/shorter in fast motion, " +
                 "frozen on pause). Unscaled: ignores the time scale and always plays at normal speed.")]
        private TimeMode timeMode = TimeMode.Unscaled;
//#SG_PRO_END
        [SerializeField]
        [Range(0f, 5f)]
        [Tooltip("Strength of the Doppler pitch shift caused by movement (1 = realistic).")]
        private float dopplerLevel = 1f;
        [SerializeField]
        [Tooltip("Distance at which the music becomes fully audible.")]
        private float hearDistanceMin = 3f;
        [SerializeField]
        [Tooltip("Distance at which the music starts to become audible.")]
        private float hearDistanceMax = 500f;
        [SerializeField]
        [Tooltip("How the volume falls off with distance.")]
        private ComponentRolloff volumeRolloff = ComponentRolloff.Logarithmic;
        [SerializeField]
        [Tooltip("Custom volume-over-distance curve, used when Rolloff Curve is set to Custom.")]
        private AnimationCurve customVolumeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        [SerializeField]
        [Tooltip("Audio effect, created in the Effect Creator window, to apply to this music.")]
        private Effect effect = default;
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How strongly the effect is applied (0 = none, 1 = the effect as saved).")]
        private float effectIntensity = 1f;

        [SerializeField]
        [Tooltip("Audio Output (AudioMixer group) this music is routed through.")]
        private Output output = default;
        [SerializeField]
        [Tooltip("Seconds the fade in lasts when playing or resuming.")]
        private float fadeInTime = 0f;
        [SerializeField]
        [Tooltip("Seconds the fade out lasts when stopping or pausing.")]
        private float fadeOutTime = 0f;
        [SerializeField]
        [Tooltip("Keep the music at this object's position, following it every frame.")]
        private bool followTransform = false;
        [SerializeField]
        [Tooltip("Optional id to control this music from SoundsGoodManager's static methods.")]
        private string id = "";

        [SerializeField]
        [Tooltip("Invoked when the music starts playing.")]
        private UnityEvent onPlay;
        [SerializeField]
        [Tooltip("Invoked when the music finishes, or on Stop when it isn't looping.")]
        private UnityEvent onComplete;
        [SerializeField]
        [Tooltip("Invoked each time a loop cycle completes.")]
        private UnityEvent onLoopCycleComplete;
        [SerializeField]
        [Tooltip("Invoked when the music is paused.")]
        private UnityEvent onPause;
        [SerializeField]
        [Tooltip("Invoked when the pause fade-out finishes.")]
        private UnityEvent onPauseComplete;
        [SerializeField]
        [Tooltip("Invoked when the music resumes after a pause.")]
        private UnityEvent onResume;
    }

    /// <summary>
    /// Configures a <see cref="Music"/> from the Inspector, with the exact same options you'd set
    /// in code. Reference this component to drive playback (Play/Pause/Resume/Stop) and, through
    /// its <see cref="Music"/> property, to call any Set* method to reconfigure it at runtime.
    /// </summary>
    [AddComponentMenu("Sounds Good/Emitters/Music Emitter", 1)]
    public partial class MusicEmitter : AudioEmitter
    {
        private Music music;

        /// <summary> The underlying Music. Use it to call any Set*/On* method at runtime. </summary>
        public Music Music
        {
            get
            {
                if (music == null) Build();
                return music;
            }
        }

        public override bool IsPlaying => music != null && music.Using;

        void Awake () => Build();

        void Start ()
        {
            if (playOnStart) Play();
        }

        void OnDrawGizmosSelected ()
        {
            if (spatialSound) EmitterGizmos.DrawHearDistance(transform.position, hearDistanceMin, hearDistanceMax);
        }

        /// <summary>Plays the music with the configured fade in.</summary>
        public override void Play ()
        {
            if (randomVolume) Music.SetRandomVolume(volumeRange.x, volumeRange.y);
            Music.Play(fadeInTime);
        }

        /// <summary>Stops the music with the configured fade out.</summary>
        public override void Stop () => Music.Stop(fadeOutTime);

        /// <summary>Pauses the music with the configured fade out.</summary>
        public override void Pause () => Music.Pause(fadeOutTime);

        /// <summary>Resumes the music with the configured fade in.</summary>
        public override void Resume () => Music.Resume(fadeInTime);

        /// <summary>Scales the configured volume by <paramref name="factor"/> (used by MusicZone).</summary>
        public override void SetZoneVolume (float factor, float fadeTime = 0f)
            => Music.ChangeVolume(volume * Mathf.Clamp01(factor), fadeTime);

        private void Build ()
        {
            music = new Music(track)
                .SetVolume(volume)
                .SetLoop(loop)
                .SetSpatialSound(spatialSound)
//#SG_PRO_BEGIN
                .SetOcclusion(useOcclusion)
//#SG_PRO_END
//#SG_PRO_BEGIN
                .SetTimeMode(timeMode)
//#SG_PRO_END
                .SetDopplerLevel(dopplerLevel)
                .SetOutput(output)
                .SetFadeOut(fadeOutTime)
                .SetId(id)
                .OnPlay(() => onPlay?.Invoke())
                .OnComplete(() => onComplete?.Invoke())
                .OnLoopCycleComplete(() => onLoopCycleComplete?.Invoke())
                .OnPause(() => onPause?.Invoke())
                .OnPauseComplete(() => onPauseComplete?.Invoke())
                .OnResume(() => onResume?.Invoke());

            music.SetRandomClip(randomClip);
            if (!randomClip) music.SetClipByIndex(clipIndex);

            ComponentAudioSettings.Apply(music, hearDistanceMin, hearDistanceMax, volumeRolloff, customVolumeCurve,
                effect, effectIntensity);

            music.SetPitch(pitch);

            if (followTransform) music.SetFollowTarget(transform);
            else music.SetPosition(transform.position);
        }
    }
}
