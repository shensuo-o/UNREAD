/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MelenitasDev.SoundsGood
{
    // Every setting is serialized but kept private: the custom editor binds to it by name and the
    // Inspector edits it, yet game code can't reach in and desync the emitter. To reconfigure at
    // runtime, go through the encapsulated Playlist object (e.g. emitter.Playlist.SetVolume(...)).
    public partial class PlaylistEmitter // Properties
    {
        [SerializeField]
        [Tooltip("The ordered list of music tracks the playlist plays, one after another.")]
        private List<Track> playlistTracks = new List<Track>();

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
        [Tooltip("Loop the whole playlist once it reaches the end.")]
        private bool loop = true;
        [SerializeField]
        [Range(-3f, 3f)]
        [Tooltip("Fixed playback pitch (1 = original). Negative values play reversed.")]
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
        [Tooltip("Audio effect, created in the Effect Creator window, to apply to this playlist.")]
        private Effect effect = default;
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How strongly the effect is applied (0 = none, 1 = the effect as saved).")]
        private float effectIntensity = 1f;

        [SerializeField]
        [Tooltip("Audio Output (AudioMixer group) this playlist is routed through.")]
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
        [Tooltip("Optional id to control this playlist from SoundsGoodManager's static methods.")]
        private string id = "";

        [SerializeField]
        [Tooltip("Invoked when the playlist starts playing.")]
        private UnityEvent onPlay;
        [SerializeField]
        [Tooltip("Invoked when the playlist finishes, or on Stop when it isn't looping.")]
        private UnityEvent onComplete;
        [SerializeField]
        [Tooltip("Invoked each time the next track in the playlist starts.")]
        private UnityEvent onNextTrackStart;
        [SerializeField]
        [Tooltip("Invoked each time the playlist completes a full loop cycle.")]
        private UnityEvent onLoopCycleComplete;
        [SerializeField]
        [Tooltip("Invoked when the playlist is paused.")]
        private UnityEvent onPause;
        [SerializeField]
        [Tooltip("Invoked when the pause fade-out finishes.")]
        private UnityEvent onPauseComplete;
        [SerializeField]
        [Tooltip("Invoked when the playlist resumes after a pause.")]
        private UnityEvent onResume;
    }

    /// <summary>
    /// Configures a <see cref="Playlist"/> from the Inspector, with the exact same options you'd
    /// set in code. Reference this component to drive playback (Play/Pause/Resume/Stop) and,
    /// through its <see cref="Playlist"/> property, to call any Set* method at runtime.
    /// </summary>
    [AddComponentMenu("Sounds Good/Emitters/Playlist Emitter", 2)]
    public partial class PlaylistEmitter : AudioEmitter
    {
        private Playlist playlist;

        /// <summary> The underlying Playlist. Use it to call any Set*/On* method at runtime. </summary>
        public Playlist Playlist
        {
            get
            {
                if (playlist == null) Build();
                return playlist;
            }
        }

        public override bool IsPlaying => playlist != null && playlist.Using;

        void Awake () => Build();

        void Start ()
        {
            if (playOnStart) Play();
        }

        void OnDrawGizmosSelected ()
        {
            if (spatialSound) EmitterGizmos.DrawHearDistance(transform.position, hearDistanceMin, hearDistanceMax);
        }

        /// <summary>Plays the playlist with the configured fade in.</summary>
        public override void Play ()
        {
            if (randomVolume) Playlist.SetRandomVolume(volumeRange.x, volumeRange.y);
            Playlist.SetFadeIn(fadeInTime);
            Playlist.Play();
        }

        /// <summary>Stops the playlist with the configured fade out.</summary>
        public override void Stop () => Playlist.Stop(fadeOutTime);

        /// <summary>Pauses the playlist with the configured fade out.</summary>
        public override void Pause () => Playlist.Pause(fadeOutTime);

        /// <summary>Resumes the playlist with the configured fade in.</summary>
        public override void Resume () => Playlist.Resume(fadeInTime);

        /// <summary>Scales the configured volume by <paramref name="factor"/> (used by MusicZone).</summary>
        public override void SetZoneVolume (float factor, float fadeTime = 0f)
            => Playlist.ChangeVolume(volume * Mathf.Clamp01(factor), fadeTime);

        private void Build ()
        {
            playlist = new Playlist(playlistTracks.ToArray())
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
                .SetFadeIn(fadeInTime)
                .SetFadeOut(fadeOutTime)
                .SetId(id)
                .OnPlay(() => onPlay?.Invoke())
                .OnComplete(() => onComplete?.Invoke())
                .OnNextTrackStart(() => onNextTrackStart?.Invoke())
                .OnLoopCycleComplete(() => onLoopCycleComplete?.Invoke())
                .OnPause(() => onPause?.Invoke())
                .OnPauseComplete(() => onPauseComplete?.Invoke())
                .OnResume(() => onResume?.Invoke());

            ComponentAudioSettings.Apply(playlist, hearDistanceMin, hearDistanceMax, volumeRolloff, customVolumeCurve,
                effect, effectIntensity);

            playlist.SetPitch(pitch);

            if (followTransform) playlist.SetFollowTarget(transform);
            else playlist.SetPosition(transform.position);
        }
    }
}
