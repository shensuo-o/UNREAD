/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace MelenitasDev.SoundsGood
{
    public partial class DynamicMusicEmitter // Properties
    {
        [Serializable]
        public class TrackInfo
        {
            [Tooltip("A music track played as one simultaneous layer of the dynamic music.")]
            public Track track = default;
            [Range(0f, 1f)]
            [Tooltip("This layer's volume. Change it at runtime to blend layers in and out.")]
            public float volume = 1f;
        }

        [SerializeField]
        [Tooltip("The tracks played at the same time as layers. Blend them with each layer's volume.")]
        private List<TrackInfo> dynamicTracks = new List<TrackInfo>();

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Volume of the music as a whole, applied on top of each layer's own volume. " +
                 "Use it to raise or lower every layer at once without changing how they blend.")]
        private float masterVolume = 1f;

        [SerializeField]
        [Tooltip("Play automatically when the scene starts (on Start).")]
        private bool playOnStart = false;

        [SerializeField]
        [Tooltip("Loop the layers once they reach the end.")]
        private bool loop = true;
        [SerializeField]
        [Range(-3f, 3f)]
        [Tooltip("Fixed playback pitch (1 = original). Negative values play reversed.")]
        private float pitch = 1f;

        [SerializeField]
        [Tooltip("3D positional audio. Turn off for 2D / global music.")]
        private bool spatialSound = false;
        [SerializeField]
        [Tooltip("Muffle the music when obstacles block the path between it and the listener.")]
        private bool useOcclusion = false;
        [SerializeField]
        [Tooltip("Scaled: playback speed follows Time.timeScale (faster/shorter in fast motion, " +
                 "frozen on pause). Unscaled: ignores the time scale and always plays at normal speed.")]
        private TimeMode timeMode = TimeMode.Unscaled;
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
    /// Configures a <see cref="DynamicMusic"/> from the Inspector, with the exact same options
    /// you'd set in code. Reference this component to drive playback (Play/Pause/Resume/Stop) and,
    /// through its <see cref="DynamicMusic"/> property, to blend layers or call any Set* method
    /// at runtime.
    /// </summary>
    [AddComponentMenu("Sounds Good/Emitters/Dynamic Music Emitter", 3)]
    public partial class DynamicMusicEmitter : AudioEmitter
    {
        private DynamicMusic dynamicMusic;

        /// <summary> The underlying DynamicMusic. Use it to blend layers or call any Set*/On* at runtime. </summary>
        public DynamicMusic DynamicMusic
        {
            get
            {
                if (dynamicMusic == null) Build();
                return dynamicMusic;
            }
        }

        public override bool IsPlaying => dynamicMusic != null && dynamicMusic.Using;

        /// <summary>
        /// The music's general volume, applied on top of each layer's own volume, so raising or
        /// lowering it leaves the blend between layers untouched. Takes effect immediately while
        /// playing; use <see cref="SetMasterVolume"/> to fade to it instead.
        /// </summary>
        public float MasterVolume
        {
            get => masterVolume;
            set => SetMasterVolume(value);
        }

        void Awake () => Build();

        void Start ()
        {
            if (playOnStart) Play();
        }

        void OnDrawGizmosSelected ()
        {
            if (spatialSound) EmitterGizmos.DrawHearDistance(transform.position, hearDistanceMin, hearDistanceMax);
        }

        /// <summary>Plays the dynamic music with the configured fade in.</summary>
        public override void Play () => DynamicMusic.Play(fadeInTime);

        /// <summary>Stops the dynamic music with the configured fade out.</summary>
        public override void Stop () => DynamicMusic.Stop(fadeOutTime);

        /// <summary>Pauses the dynamic music with the configured fade out.</summary>
        public override void Pause () => DynamicMusic.Pause(fadeOutTime);

        /// <summary>Resumes the dynamic music with the configured fade in.</summary>
        public override void Resume () => DynamicMusic.Resume(fadeInTime);

        /// <summary>
        /// Sets the music's general volume, optionally fading to it over
        /// <paramref name="fadeTime"/> seconds. Each layer keeps its own volume underneath.
        /// </summary>
        /// <param name="volume">Volume: min 0, Max 1.</param>
        /// <param name="fadeTime">Seconds to fade to the new volume (0 = instant).</param>
        public void SetMasterVolume (float volume, float fadeTime = 0f)
        {
            masterVolume = Mathf.Clamp01(volume);
            DynamicMusic.ChangeMasterVolume(masterVolume, fadeTime);
        }

        // Keeps the Inspector slider live while playing: on layered music a general volume is
        // something you dial in by ear, so dragging it has to be audible straight away.
        void OnValidate ()
        {
            if (!Application.isPlaying || dynamicMusic == null) return;
            dynamicMusic.ChangeMasterVolume(masterVolume);
        }

        /// <summary>
        /// Scales every layer's configured volume by <paramref name="factor"/> (used by MusicZone),
        /// keeping the relative blend between layers.
        /// </summary>
        public override void SetZoneVolume (float factor, float fadeTime = 0f)
        {
            factor = Mathf.Clamp01(factor);
            foreach (TrackInfo dynamicTrack in dynamicTracks)
                DynamicMusic.ChangeTrackVolume(dynamicTrack.track, dynamicTrack.volume * factor, fadeTime);
        }

        private void Build ()
        {
            dynamicMusic = new DynamicMusic(dynamicTracks.Select(t => t.track).ToArray())
                .SetMasterVolume(masterVolume)
                .SetLoop(loop)
                .SetSpatialSound(spatialSound)
                .SetOcclusion(useOcclusion)
                .SetTimeMode(timeMode)
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

            foreach (TrackInfo dynamicTrack in dynamicTracks)
            {
                dynamicMusic.SetTrackVolume(dynamicTrack.track, dynamicTrack.volume);
            }

            ComponentAudioSettings.Apply(dynamicMusic, hearDistanceMin, hearDistanceMax, volumeRolloff, customVolumeCurve,
                effect, effectIntensity);

            dynamicMusic.SetPitch(pitch);

            if (followTransform) dynamicMusic.SetFollowTarget(transform);
            else dynamicMusic.SetPosition(transform.position);
        }
    }
}
