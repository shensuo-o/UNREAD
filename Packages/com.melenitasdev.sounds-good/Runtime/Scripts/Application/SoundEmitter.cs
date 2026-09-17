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
    // runtime, go through the encapsulated Sound object (e.g. emitter.Sound.SetVolume(...)).
    public partial class SoundEmitter // Properties
    {
        [SerializeField]
        [Tooltip("The sound tag, created in the Audio Creator window, that this emitter plays.")]
        private SFX sfx;
        [SerializeField]
        [Tooltip("Pick a random clip among those registered under the sound tag each time it plays.")]
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
        [Tooltip("Loop the sound until it's stopped.")]
        private bool loop = false;

        [SerializeField]
        [Tooltip("Randomize the pitch within a range each time it plays, to avoid repetition.")]
        private bool randomPitch = false;
        [SerializeField]
        [Tooltip("Minimum and maximum pitch used when Random Pitch is on.")]
        private Vector2 pitchRange = new Vector2(0.85f, 1.15f);
        [SerializeField]
        [Range(-3f, 3f)]
        [Tooltip("Fixed playback pitch (1 = original). Negative values play the clip reversed.")]
        private float pitch = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Chance the sound actually plays when triggered (0 = never, 1 = always).")]
        private float playProbability = 1f;

        [SerializeField]
        [Tooltip("3D positional audio. Turn off for 2D / global sound.")]
        private bool spatialSound = true;
        //#SG_PRO_BEGIN
        [SerializeField]
        [Tooltip("Muffle the sound when obstacles block the path between it and the listener.")]
        private bool useOcclusion = false;
        //#SG_PRO_END
//#SG_PRO_BEGIN
        [SerializeField]
        [Tooltip("Scaled: playback speed follows Time.timeScale (faster/shorter in fast motion, " +
                 "frozen on pause). Unscaled: ignores the time scale and always plays at normal speed.")]
        private TimeMode timeMode = TimeMode.Scaled;
//#SG_PRO_END
        [SerializeField]
        [Range(0f, 5f)]
        [Tooltip("Strength of the Doppler pitch shift caused by movement (1 = realistic).")]
        private float dopplerLevel = 1f;
        [SerializeField]
        [Tooltip("Distance at which the sound becomes fully audible.")]
        private float hearDistanceMin = 3f;
        [SerializeField]
        [Tooltip("Distance at which the sound starts to become audible.")]
        private float hearDistanceMax = 500f;
        [SerializeField]
        [Tooltip("How the volume falls off with distance.")]
        private ComponentRolloff volumeRolloff = ComponentRolloff.Logarithmic;
        [SerializeField]
        [Tooltip("Custom volume-over-distance curve, used when Rolloff Curve is set to Custom.")]
        private AnimationCurve customVolumeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        [SerializeField]
        [Tooltip("Audio effect, created in the Effect Creator window, to apply to this sound.")]
        private Effect effect = default;
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How strongly the effect is applied (0 = none, 1 = the effect as saved).")]
        private float effectIntensity = 1f;

        [SerializeField]
        [Tooltip("Audio Output (AudioMixer group) this sound is routed through.")]
        private Output output = default;
        [SerializeField]
        [Tooltip("Seconds the fade in lasts when playing or resuming.")]
        private float fadeInTime = 0f;
        [SerializeField]
        [Tooltip("Seconds the fade out lasts when stopping or pausing.")]
        private float fadeOutTime = 0f;
        [SerializeField]
        [Tooltip("Keep the sound at this object's position, following it every frame.")]
        private bool followTransform = true;
        [SerializeField]
        [Tooltip("Optional id to control this sound from SoundsGoodManager's static methods.")]
        private string id = "";

        [SerializeField]
        [Tooltip("Invoked when the sound starts playing.")]
        private UnityEvent onPlay;
        [SerializeField]
        [Tooltip("Invoked when the sound finishes, or on Stop when it isn't looping.")]
        private UnityEvent onComplete;
        [SerializeField]
        [Tooltip("Invoked each time a loop cycle completes.")]
        private UnityEvent onLoopCycleComplete;
        [SerializeField]
        [Tooltip("Invoked when the sound is paused.")]
        private UnityEvent onPause;
        [SerializeField]
        [Tooltip("Invoked when the pause fade-out finishes.")]
        private UnityEvent onPauseComplete;
        [SerializeField]
        [Tooltip("Invoked when the sound resumes after a pause.")]
        private UnityEvent onResume;
    }

    /// <summary>
    /// Configures a <see cref="Sound"/> from the Inspector, with the exact same options you'd set
    /// in code. Reference this component to drive playback (Play/Pause/Resume/Stop) and, through
    /// its <see cref="Sound"/> property, to call any Set* method to reconfigure it at runtime.
    /// </summary>
    [AddComponentMenu("Sounds Good/Emitters/Sound Emitter", 0)]
    public partial class SoundEmitter : AudioEmitter
    {
        private Sound sound;

        /// <summary> The underlying Sound. Use it to call any Set*/On* method at runtime. </summary>
        public Sound Sound
        {
            get
            {
                if (sound == null) Build();
                return sound;
            }
        }

        public override bool IsPlaying => sound != null && sound.Using;

        void Awake ()
        {
            Build();
        }

        void OnDrawGizmosSelected ()
        {
            if (spatialSound) EmitterGizmos.DrawHearDistance(transform.position, hearDistanceMin, hearDistanceMax);
        }

        void Start ()
        {
            if (playOnStart) Play();
        }

        /// <summary>Plays the sound with the configured fade in.</summary>
        public override void Play ()
        {
            // Re-roll the random settings on every play so repeated sounds keep varying.
            if (randomVolume) Sound.SetRandomVolume(volumeRange.x, volumeRange.y);
            if (randomPitch) Sound.SetRandomPitch(pitchRange);
            Sound.Play(fadeInTime);
        }

        /// <summary>Stops the sound with the configured fade out.</summary>
        public override void Stop () => Sound.Stop(fadeOutTime);

        /// <summary>Pauses the sound with the configured fade out.</summary>
        public override void Pause () => Sound.Pause(fadeOutTime);

        /// <summary>Resumes the sound with the configured fade in.</summary>
        public override void Resume () => Sound.Resume(fadeInTime);

        private void Build ()
        {
            sound = new Sound(sfx)
                .SetVolume(volume)
                .SetLoop(loop)
                .SetPlayProbability(playProbability)
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

            sound.SetRandomClip(randomClip);
            if (!randomClip) sound.SetClipByIndex(clipIndex);

            ComponentAudioSettings.Apply(sound, hearDistanceMin, hearDistanceMax, volumeRolloff, customVolumeCurve,
                effect, effectIntensity);

            // Baseline pitch; when Random Pitch is on it's re-rolled on every Play instead.
            sound.SetPitch(pitch);

            if (followTransform) sound.SetFollowTarget(transform);
            else sound.SetPosition(transform.position);
        }
    }
}
