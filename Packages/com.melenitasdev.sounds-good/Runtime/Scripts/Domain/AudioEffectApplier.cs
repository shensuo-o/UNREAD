/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using System.Collections.Generic;
using UnityEngine;

namespace MelenitasDev.SoundsGood.Domain
{
    /// <summary>
    /// Applies (or clears) an AudioEffectPreset's filters onto a GameObject that holds an
    /// AudioSource. Idempotent: only enables the filters the preset uses and updates their
    /// parameters, disabling the rest — safe to call every frame for live previewing.
    /// </summary>
    public static class AudioEffectApplier
    {
        private const float SILENT_LEVEL = -10000f;

        /// <summary> Every parameter a built-in reverb preset writes into the filter. </summary>
        private struct ReverbSettings
        {
            public float DryLevel, Room, RoomHF, RoomLF, DecayTime, DecayHFRatio;
            public float ReflectionsLevel, ReflectionsDelay, ReverbLevel, ReverbDelay;
            public float HFReference, LFReference, Diffusion, Density;
        }

        // Each built-in preset's parameters, sampled once and reused. Needed because blending
        // requires the filter to be on the User preset, which no longer supplies these values.
        private static readonly Dictionary<AudioReverbPreset, ReverbSettings> reverbSettingsByPreset =
            new Dictionary<AudioReverbPreset, ReverbSettings>();

        /// <summary> Cutoff at which an AudioLowPassFilter is effectively not filtering. </summary>
        public const float NO_LOW_PASS_CUTOFF = 22000f;

        /// <summary>
        /// Interpolates a low pass cutoff from <paramref name="maxCutoff"/> (t=0, most open) to
        /// <paramref name="minCutoff"/> (t=1, most muffled) in log space. Frequency is perceived
        /// logarithmically, so a linear sweep would spend most of its range in high frequencies
        /// nobody can hear (the change only becoming audible near the very end); interpolating the
        /// exponent instead makes the sweep sound perceptually even across the whole 0..1 range.
        /// </summary>
        public static float LogLerpCutoff (float maxCutoff, float minCutoff, float t)
        {
            float from = Mathf.Log10(Mathf.Max(10f, maxCutoff));
            float to = Mathf.Log10(Mathf.Max(10f, minCutoff));
            return Mathf.Pow(10f, Mathf.Lerp(from, to, Mathf.Clamp01(t)));
        }

        public static void Apply (GameObject go, AudioEffectPreset preset) => Apply(go, preset, 1f);

        /// <summary>
        /// The preset's low pass cutoff blended by <paramref name="weight"/>, or
        /// <see cref="NO_LOW_PASS_CUTOFF"/> when the preset doesn't use one.
        /// Low pass and volume are not applied by <see cref="Apply"/>: on a pooled source the
        /// occlusion already owns the low pass filter and the volume, so the caller has to
        /// combine both contributions instead of letting one overwrite the other.
        /// </summary>
        public static float GetLowPassCutoff (AudioEffectPreset preset, float weight)
        {
            weight = Mathf.Clamp01(weight);
            if (preset == null || !preset.UseLowPass || weight <= 0f) return NO_LOW_PASS_CUTOFF;

            return LogLerpCutoff(NO_LOW_PASS_CUTOFF, preset.LowPassCutoff, weight);
        }

        /// <summary> The preset's low pass resonance blended by <paramref name="weight"/> (neutral is 1). </summary>
        public static float GetLowPassResonance (AudioEffectPreset preset, float weight)
        {
            weight = Mathf.Clamp01(weight);
            if (preset == null || !preset.UseLowPass || weight <= 0f) return 1f;

            return Mathf.Lerp(1f, preset.LowPassResonance, weight);
        }

        /// <summary> The preset's volume attenuation blended by <paramref name="weight"/> (neutral is 1). </summary>
        public static float GetVolumeMultiplier (AudioEffectPreset preset, float weight)
        {
            weight = Mathf.Clamp01(weight);
            if (preset == null || !preset.UseVolume || weight <= 0f) return 1f;

            return Mathf.Lerp(1f, preset.Volume, weight);
        }

        /// <summary>
        /// The preset's pitch shift blended by <paramref name="weight"/> (neutral is 1). It's a
        /// multiplier on the source's own pitch, so — like volume and low pass — the caller combines
        /// it instead of letting <see cref="Apply"/> overwrite the source's configured pitch.
        /// </summary>
        public static float GetPitchMultiplier (AudioEffectPreset preset, float weight)
        {
            weight = Mathf.Clamp01(weight);
            if (preset == null || !preset.UsePitch || weight <= 0f) return 1f;

            return Mathf.Lerp(1f, preset.Pitch, weight);
        }

        /// <summary>
        /// Applies the preset scaled by <paramref name="weight"/> (0 = no effect, 1 = the preset's
        /// stored values). Every parameter is mapped independently from its neutral (no-effect)
        /// value to its stored one, so the effect can be ramped up gradually.
        /// Parameters that shape the effect's character rather than its amount (echo/chorus delay
        /// and chorus rate) always use their stored value: scaling them would change what the
        /// effect sounds like instead of how much of it there is.
        /// </summary>
        public static void Apply (GameObject go, AudioEffectPreset preset, float weight)
        {
            if (go == null) return;

            weight = Mathf.Clamp01(weight);
            if (weight <= 0f) preset = null;

            ApplyReverb(go, preset, weight);
            ApplyEcho(go, preset, weight);
            ApplyChorus(go, preset, weight);
            ApplyDistortion(go, preset, weight);
            ApplyHighPass(go, preset, weight);
        }

        private static void ApplyReverb (GameObject go, AudioEffectPreset preset, float weight)
        {
            var filter = go.GetComponent<AudioReverbFilter>();
            bool want = preset != null && preset.UseReverb;

            if (want)
            {
                if (filter == null) filter = go.AddComponent<AudioReverbFilter>();

                if (!filter.enabled)
                {
                    // Starting from silence, so a reinitialisation is inaudible — and it's the only
                    // way to empty whatever the delay line was left holding when the filter was
                    // last switched off, which would otherwise resume on top of this sound.
                    filter.reverbPreset = AudioReverbPreset.Off;
                    filter.enabled = true;
                }

                ReverbSettings settings = GetPresetSettings(filter, preset.ReverbPreset);

                // The filter ignores individual parameters unless it's on the User preset, so
                // switch to it and write the preset's sampled values ourselves. This happens even
                // at full weight, where assigning the built-in preset would give the same sound:
                // writing reverbPreset reinitialises the filter, and that would land exactly on
                // the frame a zone's weight first dips below 1 — an audible click right as the
                // listener starts leaving the zone.
                filter.reverbPreset = AudioReverbPreset.User;

                filter.dryLevel = settings.DryLevel;
                filter.roomHF = settings.RoomHF;
                filter.roomLF = settings.RoomLF;
                filter.decayTime = settings.DecayTime;
                filter.decayHFRatio = settings.DecayHFRatio;
                filter.reflectionsLevel = settings.ReflectionsLevel;
                filter.reflectionsDelay = settings.ReflectionsDelay;
                filter.reverbLevel = settings.ReverbLevel;
                filter.reverbDelay = settings.ReverbDelay;
                filter.hfReference = settings.HFReference;
                filter.lfReference = settings.LFReference;
                filter.diffusion = settings.Diffusion;
                filter.density = settings.Density;

                // Room is the master level of the whole room effect, so fading it fades the
                // reverb as a whole.
                filter.room = LerpLevel(settings.Room, weight);
            }
            else if (filter != null && filter.enabled)
            {
                // Silenced, not disabled. A filter that stops processing keeps its delay line
                // frozen exactly as it was, so switching it off while it still rings both cuts the
                // tail short now and resumes that same tail the next time it comes back on.
                // Leaving it enabled lets the tail drain in real time; DisableTailFilters takes it
                // out of the chain once it has.
                filter.reverbPreset = AudioReverbPreset.User;
                filter.room = SILENT_LEVEL;
                filter.dryLevel = 0f;
            }
        }

        /// <summary>
        /// The parameters a built-in preset writes into the filter. Unity only refills them when
        /// the preset value actually changes, so the first read forces a change before sampling.
        /// Cached: a preset's values are the same for every filter.
        /// </summary>
        private static ReverbSettings GetPresetSettings (AudioReverbFilter filter, AudioReverbPreset reverbPreset)
        {
            if (reverbSettingsByPreset.TryGetValue(reverbPreset, out ReverbSettings cached)) return cached;

            filter.reverbPreset = reverbPreset == AudioReverbPreset.Off
                ? AudioReverbPreset.Generic
                : AudioReverbPreset.Off;
            filter.reverbPreset = reverbPreset;

            var settings = new ReverbSettings
            {
                DryLevel = filter.dryLevel,
                Room = filter.room,
                RoomHF = filter.roomHF,
                RoomLF = filter.roomLF,
                DecayTime = filter.decayTime,
                DecayHFRatio = filter.decayHFRatio,
                ReflectionsLevel = filter.reflectionsLevel,
                ReflectionsDelay = filter.reflectionsDelay,
                ReverbLevel = filter.reverbLevel,
                ReverbDelay = filter.reverbDelay,
                HFReference = filter.hfReference,
                LFReference = filter.lfReference,
                Diffusion = filter.diffusion,
                Density = filter.density
            };

            reverbSettingsByPreset[reverbPreset] = settings;
            return settings;
        }

        /// <summary>
        /// Fades a millibel level towards silence by <paramref name="weight"/>. Interpolating
        /// millibels directly would be nearly inaudible until the very end (half of -10000 is
        /// still -50 dB, i.e. silence), so the blend happens in linear amplitude.
        /// </summary>
        private static float LerpLevel (float levelInMillibels, float weight)
        {
            float amplitude = Mathf.Pow(10f, levelInMillibels / 2000f) * weight;
            if (amplitude <= 0.00001f) return SILENT_LEVEL;

            return Mathf.Max(SILENT_LEVEL, 2000f * Mathf.Log10(amplitude));
        }

        /// <summary>
        /// How long the reverb keeps ringing (seconds), so a pooled source can be held until the
        /// tail decays. A built-in preset doesn't expose its decay through the filter's decayTime
        /// property (that only reflects the User preset), so its known decay is used instead.
        /// </summary>
        public static float GetReverbDecayTime (AudioReverbFilter filter)
        {
            if (filter == null || !filter.enabled) return 0f;
            // Enabled but silenced: it's only still in the chain so its delay line can drain, and
            // nothing it renders from here on is audible.
            if (filter.room <= SILENT_LEVEL + 1f) return 0f;
            if (filter.reverbPreset == AudioReverbPreset.User) return filter.decayTime;
            return ReverbPresetDecay(filter.reverbPreset);
        }

        // Decay times (RT60, seconds) of Unity's built-in reverb presets.
        private static float ReverbPresetDecay (AudioReverbPreset preset)
        {
            switch (preset)
            {
                case AudioReverbPreset.Off: return 0f;
                case AudioReverbPreset.PaddedCell: return 0.17f;
                case AudioReverbPreset.Room: return 0.40f;
                case AudioReverbPreset.Bathroom: return 1.49f;
                case AudioReverbPreset.Livingroom: return 0.50f;
                case AudioReverbPreset.Stoneroom: return 2.31f;
                case AudioReverbPreset.Auditorium: return 4.32f;
                case AudioReverbPreset.Concerthall: return 3.92f;
                case AudioReverbPreset.Cave: return 2.91f;
                case AudioReverbPreset.Arena: return 7.24f;
                case AudioReverbPreset.Hangar: return 10.05f;
                case AudioReverbPreset.CarpetedHallway: return 0.30f;
                case AudioReverbPreset.Hallway: return 1.49f;
                case AudioReverbPreset.StoneCorridor: return 2.70f;
                case AudioReverbPreset.Alley: return 1.49f;
                case AudioReverbPreset.Forest: return 1.49f;
                case AudioReverbPreset.City: return 1.49f;
                case AudioReverbPreset.Mountains: return 1.49f;
                case AudioReverbPreset.Quarry: return 1.49f;
                case AudioReverbPreset.Plain: return 1.49f;
                case AudioReverbPreset.ParkingLot: return 1.65f;
                case AudioReverbPreset.SewerPipe: return 2.81f;
                case AudioReverbPreset.Underwater: return 1.49f;
                case AudioReverbPreset.Drugged: return 8.39f;
                case AudioReverbPreset.Dizzy: return 17.23f;
                case AudioReverbPreset.Psychotic: return 7.56f;
                default: return 1.49f; // Generic and any unknown
            }
        }

        /// <summary>
        /// How long an echo keeps repeating (seconds) before it's effectively silent (~1%).
        /// </summary>
        public static float GetEchoTailTime (AudioEchoFilter filter)
        {
            if (filter == null || !filter.enabled || filter.wetMix <= 0f) return 0f;

            return EchoTail(filter.delay, filter.decayRatio);
        }

        /// <summary>
        /// How long <paramref name="preset"/>'s reverb and echo keep ringing (seconds). Read from
        /// the preset rather than from live filters, so a caller can know the tail *before* deciding
        /// how fast it may fade the effect out.
        /// </summary>
        public static float GetTailTime (AudioEffectPreset preset)
        {
            if (preset == null) return 0f;

            float tail = 0f;
            if (preset.UseReverb) tail = Mathf.Max(tail, ReverbPresetDecay(preset.ReverbPreset));
            if (preset.UseEcho) tail = Mathf.Max(tail, EchoTail(preset.EchoDelay, preset.EchoDecayRatio));
            return tail;
        }

        private static float EchoTail (float delayInMilliseconds, float decayRatio)
        {
            float delaySeconds = delayInMilliseconds / 1000f;
            float ratio = Mathf.Clamp(decayRatio, 0.01f, 0.99f);
            int repeats = Mathf.CeilToInt(Mathf.Log(0.01f) / Mathf.Log(ratio));
            return delaySeconds * repeats;
        }

        private static void ApplyEcho (GameObject go, AudioEffectPreset preset, float weight)
        {
            var filter = go.GetComponent<AudioEchoFilter>();
            bool want = preset != null && preset.UseEcho;

            if (want)
            {
                if (filter == null) filter = go.AddComponent<AudioEchoFilter>();
                filter.enabled = true;
                filter.delay = preset.EchoDelay;
                filter.decayRatio = Mathf.Lerp(0f, preset.EchoDecayRatio, weight);
                filter.wetMix = Mathf.Lerp(0f, preset.EchoWetMix, weight);
                // Dry is the original signal: its neutral value is 1, not 0.
                filter.dryMix = Mathf.Lerp(1f, preset.EchoDryMix, weight);
            }
            else if (filter != null && filter.enabled)
            {
                // Silenced rather than disabled, for the same reason as the reverb: an echo's delay
                // line freezes with the filter and would replay on the next sound to use it.
                filter.wetMix = 0f;
                filter.dryMix = 1f;
            }
        }

        /// <summary>
        /// Takes the tail filters out of the DSP chain for good. Only safe once they've been silent
        /// long enough to drain (see <see cref="GetTailTime"/>): disabling one that still holds a
        /// tail freezes it there, and it would ring on again the next time the filter is enabled.
        /// </summary>
        public static void DisableTailFilters (GameObject go)
        {
            if (go == null) return;

            var reverb = go.GetComponent<AudioReverbFilter>();
            if (reverb != null)
            {
                // Off reinitialises the filter, so anything still in the delay line is dropped
                // instead of waiting frozen for the next time it's switched on.
                reverb.reverbPreset = AudioReverbPreset.Off;
                reverb.enabled = false;
            }

            var echo = go.GetComponent<AudioEchoFilter>();
            if (echo != null) echo.enabled = false;
        }

        private static void ApplyChorus (GameObject go, AudioEffectPreset preset, float weight)
        {
            var filter = go.GetComponent<AudioChorusFilter>();
            bool want = preset != null && preset.UseChorus;

            if (want)
            {
                if (filter == null) filter = go.AddComponent<AudioChorusFilter>();
                filter.enabled = true;
                // Dry is the original signal: its neutral value is 1, not 0.
                filter.dryMix = Mathf.Lerp(1f, preset.ChorusDryMix, weight);
                filter.wetMix1 = Mathf.Lerp(0f, preset.ChorusWetMix1, weight);
                filter.wetMix2 = Mathf.Lerp(0f, preset.ChorusWetMix2, weight);
                filter.wetMix3 = Mathf.Lerp(0f, preset.ChorusWetMix3, weight);
                filter.delay = preset.ChorusDelay;
                filter.rate = preset.ChorusRate;
                filter.depth = Mathf.Lerp(0f, preset.ChorusDepth, weight);
            }
            else if (filter != null)
            {
                filter.enabled = false;
            }
        }

        private static void ApplyDistortion (GameObject go, AudioEffectPreset preset, float weight)
        {
            var filter = go.GetComponent<AudioDistortionFilter>();
            bool want = preset != null && preset.UseDistortion;

            if (want)
            {
                if (filter == null) filter = go.AddComponent<AudioDistortionFilter>();
                filter.enabled = true;
                filter.distortionLevel = Mathf.Lerp(0f, preset.DistortionLevel, weight);
            }
            else if (filter != null)
            {
                filter.enabled = false;
            }
        }

        /// <summary>
        /// Unlike the low pass (shared with occlusion, combined by the caller), the high pass has no
        /// occlusion counterpart, so it's applied here like the other filters. Its neutral cutoff is
        /// the minimum (10 Hz passes everything), and it's swept up to the stored cutoff in log space
        /// so the fade is perceptually even.
        /// </summary>
        private static void ApplyHighPass (GameObject go, AudioEffectPreset preset, float weight)
        {
            var filter = go.GetComponent<AudioHighPassFilter>();
            bool want = preset != null && preset.UseHighPass;

            if (want)
            {
                if (filter == null) filter = go.AddComponent<AudioHighPassFilter>();
                filter.enabled = true;

                float from = Mathf.Log10(10f);
                float to = Mathf.Log10(Mathf.Max(10f, preset.HighPassCutoff));
                filter.cutoffFrequency = Mathf.Pow(10f, Mathf.Lerp(from, to, weight));
                filter.highpassResonanceQ = Mathf.Lerp(1f, preset.HighPassResonance, weight);
            }
            else if (filter != null)
            {
                filter.enabled = false;
            }
        }
    }
}
