/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using MelenitasDev.SoundsGood.Domain;
using UnityEngine;
using UnityEngine.Audio;
using MelenitasDev.SoundsGood.SystemUtilities;

namespace MelenitasDev.SoundsGood
{
    internal partial class SoundsGoodAudioSource // Fields
    {
        private AudioSource source;
        private Transform followTarget = null;
        private float volume = 1;
        private float volumeBeforePause = 1;
        // The configured level, captured before any fade starts moving `volume` around. LerpVolume
        // drives that field itself, so mid-fade it holds a point on a ramp rather than the level
        // the sound is meant to sit at: a fade-out leaves it at 0, and a fade-in has not reached
        // its target yet. Anything that needs to know where the volume belongs - the next track,
        // a pause that has to be resumed later - reads it from here instead.
        private float volumeBeforeFade = 1;
        private float basePitch = 1;
//#SG_PRO_BEGIN
        // When true (default), playback speed follows Time.timeScale and every per-source timer runs
        // in scaled game time. When false the source ignores the time scale entirely (unscaled).
        private bool useScaledTime = true;
//#SG_PRO_END
        private float fadeOutTime = 0;
        private float fadeInTime = 0;
        private bool loop = false;
        private bool stopping = false;
        private bool changingTrack = false;
        private bool isPlaylist;
        private Queue<AudioClip> playlist = new Queue<AudioClip>();
        private int tracksPlayedThisCycle;
        //#SG_PRO_BEGIN
        private bool occlusionEnabled = false;
        //#SG_PRO_END
        private float occlusionVolumeMultiplier = 1f;
        private AudioLowPassFilter lowPassFilter;
        //#SG_PRO_BEGIN
        private float occlusionCheckTimer = 0f;
        private float occlusionCurrentFactor = 0f;
        private float occlusionTargetFactor = 0f;
        //#SG_PRO_END

        //#SG_PRO_BEGIN
        private AudioEffectPreset ownEffect;
        private float ownEffectWeight = 1f;
        private AudioEffectPreset claimedZoneEffect;
        private float claimedZoneWeight;
        //#SG_PRO_END
        // A global/output effect (pause, etc.) claimed this frame. Wins over zones and, unlike them,
        // also reaches 2D sounds.
        //#SG_PRO_BEGIN
        private AudioEffectPreset claimedGlobalEffect;
        private float claimedGlobalWeight;
        //#SG_PRO_END
        // True while any override (global or zone) is applied instead of the source's own effect.
        //#SG_PRO_BEGIN
        private bool overrideActive;
        //#SG_PRO_END
        // The override actually applied, which trails the claims so it can fade out rather than be
        // dropped. It outlives the last claim by the length of that fade. See UpdateOverride.
        //#SG_PRO_BEGIN
        private AudioEffectPreset overrideEffect;
        private float overrideWeight;
        //#SG_PRO_END

        // Seconds left before a silenced reverb/echo has drained and can be disabled. 0 when there's
        // nothing to wait for. See DrainTailFilters.
        //#SG_PRO_BEGIN
        private float effectDrainTimer;
        //#SG_PRO_END

        // Bounds for that fade out. The floor keeps an effect with no tail at all from snapping off
        // at the frame rate; the ceiling stops a long preset (a hangar rings for ten seconds) from
        // feeling like the reverb followed the listener out of the room.
        private const float MIN_EFFECT_RELEASE_TIME = 0.25f;
        private const float MAX_EFFECT_RELEASE_TIME = 2f;

        // The low pass filter and the volume are shared between the occlusion and the effects,
        // so each one keeps its own contribution and they get combined before being applied.
        //#SG_PRO_BEGIN
        private float occlusionCutoff = AudioEffectApplier.NO_LOW_PASS_CUTOFF;
        private float effectCutoff = AudioEffectApplier.NO_LOW_PASS_CUTOFF;
        private float effectResonance = 1f;
        //#SG_PRO_END
        private float effectVolumeMultiplier = 1f;
        // The effect's pitch shift is a multiplier on the source's own pitch (basePitch), so both
        // are combined in RefreshPitch instead of one overwriting the other.
        private float effectPitchMultiplier = 1f;

        //#SG_PRO_BEGIN
        private float occlusionDepthCurrent = 0f;
        private float occlusionDepthTarget = 0f;
        private bool occlusionInitialized = false;
        //#SG_PRO_END

        private Coroutine lerpVolumeCor;
        private Coroutine fadeInOnChangeTrackCor;
        private Coroutine lerpPitchCor;
        //#SG_PRO_BEGIN
        private Coroutine lerpEffectCor;
        //#SG_PRO_END
        private Coroutine tailReleaseCor;

        // True while the clip has finished but the source is kept reserved so an effect tail
        // (reverb/echo) can ring out before it returns to the pool.
        private bool tailing;

        // A tiny silent, looping clip fed to the source during the tail so its DSP keeps running
        // and the reverb/echo renders its decay. Shared by every pooled source.
        private static AudioClip silenceClip;
        private static AudioClip SilenceClip
        {
            get
            {
                if (silenceClip == null)
                    silenceClip = AudioClip.Create("SG_TailSilence", 4096, 1, AudioSettings.outputSampleRate, false);
                return silenceClip;
            }
        }

        private SourceState currentState = SourceState.Stopped;

        private enum SourceState
        {
            Playing,
            Paused,
            Pausing,
            FadingIn,
            ChangingTrack,
            Stopping,
            Stopped
        }
    }

    internal partial class SoundsGoodAudioSource // Fields (Callbacks)
    {
        private Action onPlay;
        private Action onComplete;
        private Action onLoopCycleComplete;
        private Action onNextTrackStart;
        private Action onPause;
        private Action onPauseComplete;
        private Action onResume;
    }

    internal partial class SoundsGoodAudioSource // Properties
    {
        internal bool Using { get; private set; }
        internal bool Playing => source.isPlaying;
        internal float Volume => source.volume;
        internal float Pitch => source.pitch;
        internal bool Paused { get; private set; }
        internal string Id { get; private set; }
        internal float PlayingTime { get; private set; }
        internal float CurrentLoopCycleTime => source.time;
        internal int CompletedLoopCycles { get; private set; }
        internal int ReproducedTracks { get; private set; }
        internal float CurrentClipDuration => source.clip.length;
        internal AudioClip CurrentClip => source.clip;
        internal AudioClip NextPlaylistClip => playlist.Peek();
    }

    internal partial class SoundsGoodAudioSource : MonoBehaviour
    {
//#SG_PRO_BEGIN
        // Resolves the effect claims gathered this frame. Priority: a global/output effect (pause,
        // etc.) wins over an AudioEffectZone, which wins over the source's own effect. When no claim
        // remains, the source's own effect is restored.
        void LateUpdate ()
        {
            // While ringing out a tail the sound has already ended: freeze whatever effect it had
            // so the tail decays naturally instead of being cut when the source leaves a zone.
            if (tailing)
            {
                ClearEffectClaims();
                return;
            }

            AudioEffectPreset targetEffect = null;
            float targetWeight = 0f;

            if (claimedGlobalEffect != null && claimedGlobalWeight > 0f)
            {
                targetEffect = claimedGlobalEffect;
                targetWeight = claimedGlobalWeight;
            }
            else if (claimedZoneEffect != null && claimedZoneWeight > 0f)
            {
                targetEffect = claimedZoneEffect;
                targetWeight = claimedZoneWeight;
            }

            ClearEffectClaims();
            UpdateOverride(targetEffect, targetWeight);
        }
//#SG_PRO_END

//#SG_PRO_BEGIN
        /// <summary>
        /// Drives the override towards the claim resolved this frame. Rising is immediate, but
        /// falling is rate limited, wherever the fall comes from — a zone's weight shrinking as the
        /// listener nears its edge, or the claim disappearing altogether.
        /// A zone's fade margin is a distance, not a duration: running out of one collapses its
        /// weight in a couple of frames, and a reverb dropped that fast is gated rather than
        /// decaying, cutting the tail on any sound still playing (a line of dialogue following the
        /// player). Unity's reverb filter has no wet input that could be left ringing while the dry
        /// signal stops feeding it — its room level gates the tail too — so this ramp *is* the tail
        /// the listener hears, and it has to cover the whole way down.
        /// Being a cap and not a fixed duration, a wide margin crossed slowly still tracks the zone
        /// exactly; only falls faster than the tail get slowed.
        /// </summary>
        private void UpdateOverride (AudioEffectPreset targetEffect, float targetWeight)
        {
            if (targetEffect != null && targetEffect != overrideEffect)
            {
                // Another effect took over: there's nothing of the previous one worth ringing out.
                overrideEffect = targetEffect;
                overrideWeight = targetWeight;
            }
            else if (targetWeight >= overrideWeight)
            {
                overrideWeight = targetWeight;
            }
            else
            {
                float releaseTime = Mathf.Clamp(AudioEffectApplier.GetTailTime(overrideEffect),
                    MIN_EFFECT_RELEASE_TIME, MAX_EFFECT_RELEASE_TIME);
                overrideWeight = Mathf.MoveTowards(overrideWeight, targetWeight, DeltaTime / releaseTime);
            }

            if (overrideWeight > 0f)
            {
                ApplyEffect(overrideEffect, overrideWeight);
                overrideActive = true;
                effectDrainTimer = 0f;
            }
            else if (overrideActive)
            {
                // Silent by now, so handing the source back to its own effect is inaudible. The
                // reverb/echo filters are only silenced by this, not disabled — they still hold a
                // decaying tail, and taking them out of the chain freezes it in place.
                ApplyEffect(ownEffect, ownEffectWeight);
                overrideActive = false;
                effectDrainTimer = AudioEffectApplier.GetTailTime(overrideEffect);
                overrideEffect = null;
            }

            DrainTailFilters();
        }
//#SG_PRO_END

//#SG_PRO_BEGIN
        /// <summary>
        /// Counts down the time a silenced reverb/echo needs to empty its delay line, then takes it
        /// out of the DSP chain. Skipped while an effect is applied, and while the source rings out
        /// its own tail — in both cases the filters are still doing work.
        /// </summary>
        private void DrainTailFilters ()
        {
            if (effectDrainTimer <= 0f) return;

            effectDrainTimer -= DeltaTime;
            if (effectDrainTimer > 0f) return;

            effectDrainTimer = 0f;
            AudioEffectApplier.DisableTailFilters(gameObject);
        }
//#SG_PRO_END

//#SG_PRO_BEGIN
        private void ClearEffectClaims ()
        {
            claimedZoneEffect = null;
            claimedZoneWeight = 0f;
            claimedGlobalEffect = null;
            claimedGlobalWeight = 0f;
        }
//#SG_PRO_END

//#SG_PRO_BEGIN
        private void ApplyEffect (AudioEffectPreset preset, float weight)
        {
            AudioEffectApplier.Apply(gameObject, preset, weight);

            effectCutoff = AudioEffectApplier.GetLowPassCutoff(preset, weight);
            effectResonance = AudioEffectApplier.GetLowPassResonance(preset, weight);
            effectVolumeMultiplier = AudioEffectApplier.GetVolumeMultiplier(preset, weight);
            effectPitchMultiplier = AudioEffectApplier.GetPitchMultiplier(preset, weight);

            RefreshLowPass();
            RefreshVolume();
            RefreshPitch();
        }
//#SG_PRO_END

//#SG_PRO_BEGIN
        /// <summary>
        /// Drives the low pass filter from whichever of the occlusion and the effect asks for
        /// more filtering. Two things muffling a sound should never make it clearer, so the
        /// lowest cutoff wins rather than the last one written.
        /// </summary>
        private void RefreshLowPass ()
        {
            float cutoff = Mathf.Min(occlusionCutoff, effectCutoff);
            bool needed = cutoff < AudioEffectApplier.NO_LOW_PASS_CUTOFF;

            if (!needed)
            {
                if (lowPassFilter != null) lowPassFilter.enabled = false;
                return;
            }

            if (lowPassFilter == null) lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();

            lowPassFilter.enabled = true;
            lowPassFilter.cutoffFrequency = cutoff;
            lowPassFilter.lowpassResonanceQ = effectResonance;
        }
//#SG_PRO_END

        private void RefreshVolume ()
        {
            if (source == null) return;
//#SG_PRO_BEGIN
            source.volume = volume * occlusionVolumeMultiplier * effectVolumeMultiplier;
//#SG_PRO_END
//#SG_FREE_BEGIN
//            source.volume = volume;
//#SG_FREE_END

        }

//#SG_PRO_BEGIN
        /// <summary>
        /// Turns the current occlusion factor and depth into the low pass cutoff and the volume
        /// multiplier, then pushes them to the filters. Shared by the per-frame lerp in
        /// <see cref="Update"/> and the pre-play snap in <see cref="PrimeOcclusion"/>.
        /// </summary>
        private void ApplyOcclusionToFilters ()
        {
            var settings = AssetLocator.SoundsGoodSettings;

            // Log-space sweep so the muffling is perceptually even across the whole factor range
            // (a linear cutoff would only become audible near full occlusion). See AudioEffectApplier.
            float baseCutoff = AudioEffectApplier.LogLerpCutoff(settings.MaxCutoff, settings.MinCutoff, occlusionCurrentFactor);
            float baseVolumeMul = Mathf.Lerp(1f, settings.MinVolumeMultiplier, occlusionCurrentFactor);
            float depthVolumeMul = Mathf.Lerp(1f, settings.ExtraDepthMinVolumeMultiplier, occlusionDepthCurrent);
            float depthCutoffMul = Mathf.Lerp(1f, settings.ExtraDepthCutoffMultiplier, occlusionDepthCurrent);

            occlusionCutoff = baseCutoff * depthCutoffMul;
            occlusionVolumeMultiplier = baseVolumeMul * depthVolumeMul;

            RefreshLowPass();
            RefreshVolume();
        }
//#SG_PRO_END

//#SG_PRO_BEGIN
        /// <summary>
        /// Measures occlusion once and applies it with no smoothing, so a sound is already muffled
        /// at the correct level on its very first sample. Called right before the source plays —
        /// without it, occlusion would ramp in from "clear" over the first frames (LerpSpeed), which
        /// short clips (a gunshot) finish before, so they'd be heard un-occluded. The periodic check
        /// in <see cref="Update"/> takes over from here for moving sources/listeners.
        /// </summary>
        private void PrimeOcclusion ()
        {
            if (!occlusionEnabled) return;
            if (!SoundsGoodManager.TryGetListener(out var listener)) return;

            // followTarget is applied in Update, so read it here to measure from the real position.
            Vector3 sourcePosition = followTarget != null ? followTarget.position : transform.position;

            SoundsGoodManager.CalculateOcclusion(listener.transform.position,
                sourcePosition, out float factor, out float depth);

            occlusionTargetFactor = factor;
            occlusionDepthTarget = depth;
            occlusionCurrentFactor = factor;
            occlusionDepthCurrent = depth;
            occlusionInitialized = true;

            // Already measured this frame; let the periodic check resume after the normal interval.
            occlusionCheckTimer = AssetLocator.SoundsGoodSettings.CheckInterval;

            ApplyOcclusionToFilters();
        }
//#SG_PRO_END

        /// <summary>
        /// Drives the source pitch from the sound's own pitch, the effect's pitch multiplier and —
        /// when following scaled time — the current <c>Time.timeScale</c>, so an effect that shifts
        /// pitch stacks on top of the configured pitch and the whole thing tracks game speed. It's
        /// recomputed every frame while scaled (see <see cref="Update"/>) since the time scale is live.
        /// </summary>
        private void RefreshPitch ()
        {
            if (source == null) return;
//#SG_PRO_BEGIN
            float timeScaleFactor = useScaledTime ? Time.timeScale : 1f;
            source.pitch = basePitch * effectPitchMultiplier * timeScaleFactor;
//#SG_PRO_END
//#SG_FREE_BEGIN
//            source.pitch = basePitch * effectPitchMultiplier;
//#SG_FREE_END
        }

        /// <summary> Per-source delta, scaled with game time or unscaled depending on the mode. </summary>
//#SG_PRO_BEGIN
        private float DeltaTime => useScaledTime ? Time.deltaTime : Time.unscaledDeltaTime;
//#SG_PRO_END
//#SG_FREE_BEGIN
//        private float DeltaTime => Time.unscaledDeltaTime;
//#SG_FREE_END

        void Update ()
        {
            if (!Using) return;

//#SG_PRO_BEGIN
            // Time scale is live, so a scaled source re-derives its playback speed every frame.
            if (useScaledTime) RefreshPitch();
//#SG_PRO_END

            // While ringing out an effect tail there's no clip to manage, but the source must
            // still follow its target so the tail moves with it.
            if (tailing)
            {
                if (followTarget != null) transform.position = followTarget.position;
                return;
            }

            if (!loop)
            {
                if (!isPlaylist) HandleSoundStop();
                else HandlePlaylistStop();
            }

            if (!isPlaylist) HandleSoundPlaying();
            else HandlePlaylistPlaying();

            // Per-frame occlusion tracking: the whole thing is a paid feature.
//#SG_PRO_BEGIN
            if (occlusionEnabled && !Paused)
            {
                occlusionCheckTimer -= DeltaTime;
                if (occlusionCheckTimer <= 0f)
                {
                    occlusionCheckTimer = AssetLocator.SoundsGoodSettings.CheckInterval;

                    if (SoundsGoodManager.TryGetListener(out var listener))
                    {
                        SoundsGoodManager.CalculateOcclusion(listener.transform.position,
                            transform.position, out float factor, out float depth);

                        occlusionTargetFactor = factor;
                        occlusionDepthTarget = depth;

                        // Snap to the first measured value so a sound spawned already
                        // behind obstacles starts occluded instead of fading in from clear.
                        if (!occlusionInitialized)
                        {
                            occlusionCurrentFactor = factor;
                            occlusionDepthCurrent = depth;
                            occlusionInitialized = true;
                        }
                    }
                }

                float lerpSpeed = AssetLocator.SoundsGoodSettings.LerpSpeed;

                occlusionCurrentFactor = Mathf.Lerp(occlusionCurrentFactor,
                    occlusionTargetFactor, DeltaTime * lerpSpeed);
                occlusionDepthCurrent = Mathf.Lerp(occlusionDepthCurrent,
                    occlusionDepthTarget, DeltaTime * lerpSpeed);

                ApplyOcclusionToFilters();
            }
//#SG_PRO_END

            if (followTarget == null) return;

            transform.position = followTarget.position;
        }
    }

    internal partial class SoundsGoodAudioSource // Internal Methods
    {
        internal SoundsGoodAudioSource Init (AudioSource source)
        {
            this.source = source;
            return this;
        }

        internal SoundsGoodAudioSource MarkAsPlaylist ()
        {
            isPlaylist = true;
            return this;
        }

        internal SoundsGoodAudioSource SetVolume (float volume, float lerpTime = 0)
        {
            this.volume = volume;

            if (currentState == SourceState.Paused || currentState == SourceState.Stopping ||
                currentState == SourceState.ChangingTrack)
            {
                // The fade-out in flight rewrites `volume` on every frame, so the value just
                // assigned would be gone within the frame. Kept where the fade back in reads it
                // from, which is what the warning below tells the caller will happen.
                volumeBeforePause = volume;
                volumeBeforeFade = volume;

                Debug.LogWarning($"There's a volume fade out taking place at this moment, " +
                                 "so volume won't change right now, but on the next fade in it will go " +
                                 $"up until the new volume of {volume}");
                return this;
            }

            if (lerpTime <= 0)
            {
                RefreshVolume();
            }
            else
            {
                lerpVolumeCor = StartCoroutine(LerpVolume(volume, lerpTime));
            }

            return this;
        }

        internal SoundsGoodAudioSource SetHearDistance (float minHearDistance, float maxHearDistance)
        {
            source.minDistance = minHearDistance;
            source.maxDistance = maxHearDistance;
            return this;
        }

        internal SoundsGoodAudioSource SetVolumeRolloffCurve (AudioRolloffMode audioRolloffMode,
            AnimationCurve customVolumeCurve)
        {
            source.rolloffMode = audioRolloffMode;
            if (audioRolloffMode == AudioRolloffMode.Custom)
            {
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, customVolumeCurve);
            }

            return this;
        }

        internal SoundsGoodAudioSource SetPitch (float pitch, float lerpTime = 0)
        {
            if (lerpTime <= 0)
            {
                basePitch = pitch;
                RefreshPitch();
            }
            else
            {
                if (lerpPitchCor != null)
                {
                    StopCoroutine(lerpPitchCor);
                    lerpPitchCor = null;
                }

                lerpPitchCor = StartCoroutine(LerpPitch(pitch, lerpTime));
            }

            return this;
        }

        internal SoundsGoodAudioSource SetDopplerLevel (float dopplerLevel)
        {
            source.dopplerLevel = dopplerLevel;
            return this;
        }

        /// <summary>
        /// Sets whether this source follows <c>Time.timeScale</c> (true) or ignores it (false). While
        /// scaled, its playback speed and every per-source timer track game time.
        /// </summary>
//#SG_PRO_BEGIN
        internal SoundsGoodAudioSource SetUseScaledTime (bool useScaled)
        {
            useScaledTime = useScaled;
            RefreshPitch();
            return this;
        }
//#SG_PRO_END

        internal SoundsGoodAudioSource SetClip (AudioClip audioClip)
        {
            source.clip = audioClip;
            return this;
        }

        internal SoundsGoodAudioSource SetPlaylist (Queue<AudioClip> playlist)
        {
            this.playlist = new Queue<AudioClip>(playlist);
            tracksPlayedThisCycle = 0;
            return this;
        }

        internal void AddToPlaylist (AudioClip addedClip)
        {
            playlist.Enqueue(addedClip);
        }

        internal SoundsGoodAudioSource SetId (string id)
        {
            Id = id;
            return this;
        }

        internal SoundsGoodAudioSource SetSpatialSound (bool activate)
        {
            source.spatialBlend = activate ? 1 : 0;
            return this;
        }

//#SG_PRO_BEGIN
        internal SoundsGoodAudioSource SetOcclusion (bool enable)
        {
            if (!AssetLocator.SoundsGoodSettings.EnableOcclusion) return this;

            occlusionEnabled = enable;

            occlusionCheckTimer = 0f;
            occlusionCurrentFactor = 0f;
            occlusionTargetFactor = 0f;
            occlusionDepthCurrent = 0f;
            occlusionDepthTarget = 0f;
            occlusionInitialized = false;
            occlusionVolumeMultiplier = 1f;

            // Drop only the occlusion's own contribution: an effect may still want the low pass.
            occlusionCutoff = enable
                ? AssetLocator.SoundsGoodSettings.MaxCutoff
                : AudioEffectApplier.NO_LOW_PASS_CUTOFF;

            RefreshLowPass();
            RefreshVolume();

            return this;
        }
//#SG_PRO_END

//#SG_PRO_BEGIN
        internal SoundsGoodAudioSource SetEffect (AudioEffectPreset preset, float weight = 1f)
        {
//#SG_PRO_BEGIN
            StopEffectLerp();
//#SG_PRO_END

            ownEffect = preset;
            ownEffectWeight = Mathf.Clamp01(weight);
            if (!overrideActive) ApplyEffect(ownEffect, ownEffectWeight);
            return this;
        }
//#SG_PRO_END

//#SG_PRO_BEGIN
        /// <summary>
        /// Changes the current effect's intensity while playing, optionally over a lerp time
        /// (0 = instant). Use it to fade the source's own effect in or out at runtime.
        /// </summary>
        internal SoundsGoodAudioSource ChangeEffect (float targetWeight, float lerpTime = 0)
        {
            StartEffectLerp(ownEffect, targetWeight, lerpTime, presetChanged: false);
            return this;
        }
//#SG_PRO_END

//#SG_PRO_BEGIN
        /// <summary>
        /// Swaps the source's own effect and lerps its intensity to <paramref name="targetWeight"/>.
        /// A different effect fades in from neutral so it doesn't pop in at the previous amount.
        /// </summary>
        internal SoundsGoodAudioSource ChangeEffect (AudioEffectPreset preset, float targetWeight, float lerpTime = 0)
        {
            StartEffectLerp(preset, targetWeight, lerpTime, presetChanged: preset != ownEffect);
            return this;
        }
//#SG_PRO_END

//#SG_PRO_BEGIN
        private void StartEffectLerp (AudioEffectPreset preset, float targetWeight, float lerpTime, bool presetChanged)
        {
//#SG_PRO_BEGIN
            StopEffectLerp();
//#SG_PRO_END


            ownEffect = preset;
            targetWeight = Mathf.Clamp01(targetWeight);

            if (lerpTime <= 0f)
            {
                ownEffectWeight = targetWeight;
                if (!overrideActive) ApplyEffect(ownEffect, ownEffectWeight);
                return;
            }

            if (presetChanged) ownEffectWeight = 0f;

            lerpEffectCor = StartCoroutine(LerpEffect(targetWeight, lerpTime));
        }
//#SG_PRO_END

//#SG_PRO_BEGIN
        private IEnumerator LerpEffect (float targetWeight, float lerpTime)
        {
            float start = ownEffectWeight;
            float t = 0f;

            while (t < lerpTime)
            {
                t += DeltaTime;
                ownEffectWeight = Mathf.Lerp(start, targetWeight, t / lerpTime);
                // While a zone/global effect overrides this source, keep lerping the value but let
                // that override own the filters; the new value takes over once it clears.
                if (!overrideActive) ApplyEffect(ownEffect, ownEffectWeight);
                yield return null;
            }

            ownEffectWeight = targetWeight;
            if (!overrideActive) ApplyEffect(ownEffect, ownEffectWeight);
            lerpEffectCor = null;
        }
//#SG_PRO_END

//#SG_PRO_BEGIN
        private void StopEffectLerp ()
        {
            if (lerpEffectCor == null) return;
            StopCoroutine(lerpEffectCor);
            lerpEffectCor = null;
        }
//#SG_PRO_END

        internal AudioMixerGroup Output => source != null ? source.outputAudioMixerGroup : null;

//#SG_PRO_BEGIN
        /// <summary>
        /// Claimed once per frame by the global/output effect driver (pause, etc.). Wins over zone
        /// claims and reaches every sound, 2D included, since it isn't spatial.
        /// </summary>
        internal void ClaimGlobalEffect (AudioEffectPreset preset, float weight)
        {
            if (preset == null || weight <= claimedGlobalWeight) return;
            claimedGlobalEffect = preset;
            claimedGlobalWeight = weight;
        }
//#SG_PRO_END

//#SG_PRO_BEGIN
        /// <summary>
        /// Called by every AudioEffectZone reaching this source, once per frame. The strongest
        /// claim wins, and it overrides the source's own effect while it lasts. Effect zones are
        /// spatial, so they only touch 3D sounds: a 2D sound has no real position, and muffling it
        /// by a world zone (2D music or UI going underwater) is never wanted.
        /// </summary>
        internal void ClaimZoneEffect (AudioEffectPreset preset, float weight)
        {
            if (source == null || source.spatialBlend <= 0f) return;
            if (preset == null || weight <= claimedZoneWeight) return;
            claimedZoneEffect = preset;
            claimedZoneWeight = weight;
        }
//#SG_PRO_END

        internal SoundsGoodAudioSource SetPosition (Vector3 position)
        {
            transform.position = position;
            return this;
        }

        internal SoundsGoodAudioSource SetFollowTarget (Transform followTarget)
        {
            this.followTarget = followTarget;
            return this;
        }

        internal SoundsGoodAudioSource SetFadeIn (float fadeInTime)
        {
            this.fadeInTime = fadeInTime;
            return this;
        }

        internal SoundsGoodAudioSource SetFadeOut (float fadeOutTime)
        {
            this.fadeOutTime = fadeOutTime;
            return this;
        }

        internal SoundsGoodAudioSource SetLoop (bool loop)
        {
            this.loop = loop;
            source.loop = loop;
            return this;
        }

        internal SoundsGoodAudioSource SetOutput (AudioMixerGroup output)
        {
            source.outputAudioMixerGroup = output;
            return this;
        }

        internal SoundsGoodAudioSource OnPlay (Action onPlay)
        {
            this.onPlay = onPlay;
            return this;
        }

        internal SoundsGoodAudioSource OnComplete (Action onComplete)
        {
            this.onComplete = onComplete;
            return this;
        }

        internal SoundsGoodAudioSource OnLoopCycleComplete (Action onLoopCycleComplete)
        {
            this.onLoopCycleComplete = onLoopCycleComplete;
            return this;
        }

        internal SoundsGoodAudioSource OnNextTrackStart (Action onNextTrackStart)
        {
            this.onNextTrackStart = onNextTrackStart;
            return this;
        }

        internal SoundsGoodAudioSource OnPause (Action onPause)
        {
            this.onPause = onPause;
            return this;
        }

        internal SoundsGoodAudioSource OnPauseComplete (Action onPauseComplete)
        {
            this.onPauseComplete = onPauseComplete;
            return this;
        }

        internal SoundsGoodAudioSource OnResume (Action onResume)
        {
            this.onResume = onResume;
            return this;
        }

        internal void Play (float fadeInTime = 0)
        {
            if (source.clip == null)
            {
                Debug.LogError(
                    "No audio clip found, make sure you have initialized it in a method (not in the declaration)");
                return;
            }

            Using = true;
            Paused = false;
            PlayingTime = 0;
            CompletedLoopCycles = 0;

            onPlay?.Invoke();

            // Occlusion at its correct level from the very first sample (before any audio is heard).
//#SG_PRO_BEGIN
            PrimeOcclusion();
//#SG_PRO_END


            source.Play();
            ChangeState(SourceState.Playing);
            enabled = true;

            if (fadeInTime > 0)
            {
                ChangeState(SourceState.FadingIn);
                // Ramp from silence: LerpVolume lerps from the base 'volume' field, so it must
                // start at 0 (setting only source.volume would be overwritten to full on frame 1).
                volumeBeforeFade = volume;
                volume = 0;
                source.volume = 0;
                lerpVolumeCor = StartCoroutine(LerpVolume(volumeBeforeFade, fadeInTime,
                    (() => ChangeState(SourceState.Playing))));
            }
        }

        internal void PlayPlaylist (float fadeInTime)
        {
            bool validClips = true;
            for (int i = 0; i < playlist.Count; i++)
            {
                if (playlist.ToArray()[i] != null) continue;
                validClips = false;
            }

            if (!validClips)
            {
                Debug.LogError("There are invalid audio clips in playlist, make sure you have initialized it.");
                return;
            }

            Using = true;
            Paused = false;
            PlayingTime = 0;
            ReproducedTracks = 0;
            CompletedLoopCycles = 0;
            tracksPlayedThisCycle = 0;
            if (loop) source.loop = false;
            changingTrack = false;

            // Occlusion at its correct level from the very first sample (before any audio is heard).
//#SG_PRO_BEGIN
            PrimeOcclusion();
//#SG_PRO_END


            // PlayNextSong fades the first track in on its own. Starting a second fade here raced
            // it: this one read `volume` after the first had already zeroed it, so it lerped
            // towards silence and won, and a playlist with a fade-in never made a sound.
            volumeBeforeFade = volume;
            PlayNextSong();
            enabled = true;
        }

        internal void Pause (float fadeOutTime = 0)
        {
            if (!Using) return;
            if (Paused) return;

            Paused = true;

            // Capture the intended volume before any fade-out drives it to 0, so Resume can restore
            // it (a pause fade mutates the volume field). When a fade is already in flight `volume`
            // is a point on its ramp rather than a level anyone chose - pausing halfway through a
            // fade-in used to make the sound come back at whatever fraction it had reached - so the
            // value to return to is the one captured before that fade started.
            bool fading = currentState == SourceState.FadingIn || currentState == SourceState.ChangingTrack;
            volumeBeforePause = fading ? volumeBeforeFade : volume;

            onPause?.Invoke();

            void CompletePause ()
            {
                onPauseComplete?.Invoke();
                source.Pause();
                ChangeState(SourceState.Paused);
            }

            if (changingTrack)
            {
                CompletePause();
                return;
            }

            if (fadeOutTime > 0)
            {
                if (currentState == SourceState.FadingIn)
                {
                    StopCoroutine(fadeInOnChangeTrackCor);
                    fadeInOnChangeTrackCor = null;
                }

                StopLerpCoroutine();
                ChangeState(SourceState.Pausing);
                lerpVolumeCor = StartCoroutine(LerpVolume(0, fadeOutTime, CompletePause, true));
                return;
            }

            CompletePause();
        }

        internal void Resume (float fadeInTime = 0)
        {
            if (!Paused) return;

            onResume?.Invoke();

            Paused = false;
            source.UnPause();
            ChangeState(SourceState.Playing);

            if (changingTrack) return;

            // Kill any in-flight fade (e.g. an unfinished pause fade-out) before resuming.
            StopLerpCoroutine();

            if (fadeInTime > 0)
            {
                ChangeState(SourceState.FadingIn);
                volumeBeforeFade = volumeBeforePause;
                volume = 0;
                source.volume = 0;
                lerpVolumeCor = StartCoroutine(LerpVolume(volumeBeforeFade, fadeInTime,
                    (() => ChangeState(SourceState.Playing))));
            }
            else
            {
                // Restore the volume a pause fade-out may have driven to 0.
                volume = volumeBeforePause;
                RefreshVolume();
            }
        }

        internal void Stop (float fadeOutTime = 0, Action onStop = null)
        {
            if (fadeOutTime > 0)
            {
                stopping = true;
                ChangeState(SourceState.Stopping);
                lerpVolumeCor = StartCoroutine(LerpVolume(0, fadeOutTime, () => Stop(0, onStop)));
                return;
            }

            onComplete?.Invoke();
            onStop?.Invoke();

            // Manual stop: release right away, cutting any effect tail.
            ReleaseSource();
        }

        /// <summary>
        /// Called when the clip ends on its own. If the sound is being processed by an effect with
        /// a tail (reverb/echo), the source is kept reserved with its filters enabled while the tail
        /// rings out, and only then returned to the pool. This stops a reused source from cutting
        /// the previous sound's tail (e.g. footsteps in a reverberant cave).
        /// </summary>
        private void Complete ()
        {
            onComplete?.Invoke();
            onComplete = null; // already fired; a manual Stop during the tail must not fire it again

//#SG_PRO_BEGIN
            float tail = ComputeEffectTail();

            if (tail <= 0f)
            {
                ReleaseSource();
                return;
            }
//#SG_PRO_END
//#SG_FREE_BEGIN
//            // No effects means no tail to ring out: the source goes straight back to the pool.
//            ReleaseSource();
//            return;
//#SG_FREE_END

            // Unity stops processing a source's reverb/echo the moment it isn't playing, freezing
            // the tail. Keep the voice alive playing silence so the filters render their decay,
            // while the source stays reserved so the pool can't reuse it and cut the tail. By the
            // time it's freed the buffer is empty, so a reused source won't bleed the old tail.
//#SG_PRO_BEGIN
            tailing = true;
            ChangeState(SourceState.Stopped);
            source.clip = SilenceClip;
            source.loop = true;
            source.time = 0f;
            source.Play();
            tailReleaseCor = StartCoroutine(HoldTailThenRelease(tail));
//#SG_PRO_END
        }

//#SG_PRO_BEGIN
        private IEnumerator HoldTailThenRelease (float tail)
        {
            yield return new WaitForSeconds(tail);
            tailReleaseCor = null;
            ReleaseSource();
        }
//#SG_PRO_END

//#SG_PRO_BEGIN
        /// <summary>
        /// Seconds an active reverb/echo tail needs to decay, read from the live filters. 0 if none.
        /// </summary>
        private float ComputeEffectTail ()
        {
            float tail = 0f;
            tail = Mathf.Max(tail, AudioEffectApplier.GetReverbDecayTime(GetComponent<AudioReverbFilter>()));
            tail = Mathf.Max(tail, AudioEffectApplier.GetEchoTailTime(GetComponent<AudioEchoFilter>()));
            return tail;
        }
//#SG_PRO_END

        private void ReleaseSource ()
        {
            StopTailCoroutine();
//#SG_PRO_BEGIN
            StopEffectLerp();
//#SG_PRO_END


            source.Stop();
            ChangeState(SourceState.Stopped);
            source.clip = null;
            source.loop = false;

            playlist.Clear();
            isPlaylist = false;

            followTarget = null;

            onComplete = null;
            onLoopCycleComplete = null;
            onPause = null;
            onPauseComplete = null;
            onResume = null;

            Id = null;

            // Reset occlusion
            //#SG_PRO_BEGIN
            occlusionEnabled = false;
            //#SG_PRO_END
            occlusionVolumeMultiplier = 1f;
            //#SG_PRO_BEGIN
            occlusionCheckTimer = 0f;
            occlusionCurrentFactor = 0f;
            occlusionTargetFactor = 0f;
            occlusionDepthCurrent = 0f;
            occlusionDepthTarget = 0f;
            occlusionInitialized = false;
            occlusionCutoff = AudioEffectApplier.NO_LOW_PASS_CUTOFF;
            //#SG_PRO_END
            if (lowPassFilter != null)
            {
                lowPassFilter.enabled = false;
                lowPassFilter.cutoffFrequency = AssetLocator.SoundsGoodSettings.MaxCutoff;
                lowPassFilter.lowpassResonanceQ = 1f;
            }

            // Reset effects
            //#SG_PRO_BEGIN
            ownEffect = null;
            ownEffectWeight = 1f;
            claimedZoneEffect = null;
            claimedZoneWeight = 0f;
            claimedGlobalEffect = null;
            claimedGlobalWeight = 0f;
            overrideActive = false;
            overrideEffect = null;
            overrideWeight = 0f;
            effectDrainTimer = 0f;
            effectCutoff = AudioEffectApplier.NO_LOW_PASS_CUTOFF;
            effectResonance = 1f;
            //#SG_PRO_END
            effectVolumeMultiplier = 1f;
            effectPitchMultiplier = 1f;
            basePitch = 1f;
//#SG_PRO_BEGIN
            useScaledTime = true;
//#SG_PRO_END
            if (source != null) source.pitch = 1f;
            AudioEffectApplier.Apply(gameObject, null);
            // The tail hold before this already gave the reverb/echo time to ring out, so they can
            // be taken out of the chain here — flushed, so the next sound to reuse this pooled
            // source doesn't inherit whatever was left in their delay lines.
            AudioEffectApplier.DisableTailFilters(gameObject);

            changingTrack = false;
            stopping = false;
            tailing = false;
            Using = false;
            enabled = false;
        }

        private void StopTailCoroutine ()
        {
            if (tailReleaseCor == null) return;
            StopCoroutine(tailReleaseCor);
            tailReleaseCor = null;
            tailing = false;
        }
    }

    internal partial class SoundsGoodAudioSource // Private Methods
    {
        /// <summary>
        /// Seconds of real time left before the current clip ends, which is not the same as the
        /// clip time left: pitch is the playback speed, so at pitch 2 a 4-second clip is over in 2
        /// seconds. Recomputed from the live playback position every frame rather than predicted
        /// when the clip starts, so changing the pitch mid-track corrects itself.
        /// </summary>
        private float RemainingRealTime
        {
            get
            {
                if (source.clip == null) return 0f;

                float speed = Mathf.Abs(source.pitch);
                // Frozen (pitch 0, or scaled time while the game is paused): it never gets there.
                if (speed < 0.01f) return float.PositiveInfinity;

                // Played backwards, the playhead runs towards 0 instead of towards the end.
                float remainingClipTime = source.pitch < 0f
                    ? source.time
                    : source.clip.length - source.time;

                return remainingClipTime / speed;
            }
        }

        private void HandleSoundPlaying ()
        {
            PlayingTime += DeltaTime;

            if (!loop) return;

            // Fire once per completed pass: wait until playback has crossed the end of the cycle
            // we're currently in.
            if (PlayingTime < CurrentClipDuration * (CompletedLoopCycles + 1)) return;

            CompletedLoopCycles++;

            onLoopCycleComplete?.Invoke();
        }

        private void HandlePlaylistPlaying ()
        {
            PlayingTime += DeltaTime;

            // A clip that already stopped has to be caught too: at fadeOutTime 0 the remaining
            // time never quite reaches zero before Unity ends the clip.
            bool clipEnded = !Paused && !source.isPlaying;
            if (!changingTrack && (clipEnded || RemainingRealTime <= Mathf.Max(fadeOutTime, DeltaTime)))
            {
                changingTrack = true;
                // Read before any fade-out starts, because that fade is what destroys it.
                volumeBeforeFade = volume;
                ChangeState(SourceState.ChangingTrack);
                if (fadeOutTime > 0)
                    lerpVolumeCor = StartCoroutine(LerpVolume(0, fadeOutTime, () => PlayNextSong()));
                else PlayNextSong();
            }
        }

        private void HandleSoundStop ()
        {
            if (stopping) return;

            if (fadeOutTime > 0)
            {
                if (RemainingRealTime <= fadeOutTime + 0.05f)
                {
                    Stop(fadeOutTime);
                }
            }

            if (!Playing && !Paused)
            {
                Complete();
            }
        }

        private void HandlePlaylistStop ()
        {
            if (playlist.Count > 0) return;
            if (stopping) return;

            if (fadeOutTime > 0)
            {
                if (RemainingRealTime <= fadeOutTime + 0.05f)
                {
                    Stop(fadeOutTime);
                }
            }

            if (!Playing && !Paused)
            {
                Complete();
            }
        }

        private void PlayNextSong (bool firstTrack = false)
        {
            if (playlist.Count == 0) return;

            source.clip = playlist.Dequeue();
            if (loop) playlist.Enqueue(source.clip);
            if (!firstTrack)
            {
                ReproducedTracks++;
                onNextTrackStart?.Invoke();

                // A playlist's loop cycle completes when it wraps back to its first track. While
                // looping the queue always holds every track (each one is re-enqueued as it plays),
                // so its count is the length of a full cycle.
                if (loop && ++tracksPlayedThisCycle >= playlist.Count)
                {
                    tracksPlayedThisCycle = 0;
                    CompletedLoopCycles++;
                    onLoopCycleComplete?.Invoke();
                }
            }

            changingTrack = false;

            if (Paused) return;

            source.Play();
            if (fadeInTime > 0)
            {
                ChangeState(SourceState.FadingIn);
                volume = 0;
                source.volume = 0;
                fadeInOnChangeTrackCor = StartCoroutine(LerpVolume(volumeBeforeFade, fadeInTime,
                    () => ChangeState(SourceState.Playing)));
            }
            else
            {
                ChangeState(SourceState.Playing);
                // Restored explicitly: with a fade-out and no fade-in, `volume` is still the 0 the
                // fade left behind, and refreshing from it would start the new track silent.
                volume = volumeBeforeFade;
                RefreshVolume();
            }
        }

        private void StopLerpCoroutine ()
        {
            if (lerpVolumeCor == null) return;
            StopCoroutine(lerpVolumeCor);
            lerpVolumeCor = null;
        }

        private void ChangeState (SourceState newState) { currentState = newState; }
    }

    internal partial class SoundsGoodAudioSource // Private Methods (Coroutines)
    {
        private IEnumerator LerpVolume (float newVolume, float lerpTime, Action onFinishLerp = null,
            bool ignorePause = false)
        {
            float initialBaseVolume = volume;
            float targetBaseVolume = newVolume;

            for (float t = 0.0f; t < lerpTime; t += DeltaTime)
            {
                if (!ignorePause)
                {
                    while (Paused)
                    {
                        yield return null;
                    }
                }

                float lerpedBase = Mathf.Lerp(initialBaseVolume, targetBaseVolume, t / lerpTime);
                volume = lerpedBase;
                source.volume = lerpedBase * occlusionVolumeMultiplier * effectVolumeMultiplier;
                yield return null;
            }

            volume = targetBaseVolume;
            RefreshVolume();

            onFinishLerp?.Invoke();

            lerpVolumeCor = null;
        }

        private IEnumerator LerpPitch (float newPitch, float lerpTime)
        {
            float initialPitch = basePitch;

            for (float t = 0.0f; t < lerpTime; t += DeltaTime)
            {
                while (Paused)
                {
                    yield return null;
                }

                basePitch = Mathf.Lerp(initialPitch, newPitch, t / lerpTime);
                RefreshPitch();
                yield return null;
            }

            basePitch = newPitch;
            RefreshPitch();
            lerpPitchCor = null;
        }
    }
}