/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using UnityEngine;

namespace MelenitasDev.SoundsGood.Domain
{
    /// <summary>
    /// A bundle of audio effects (reverb, echo, chorus, distortion, low pass, high pass, volume,
    /// pitch) that can be applied to any Sound, Music, Playlist or Dynamic Music with a single call.
    /// Toggle the effects you want and tune them; the enabled ones are applied to the audio source on play.
    /// </summary>
    [CreateAssetMenu(menuName = "Sounds Good/Audio Effect Preset", fileName = "AudioEffectPreset")]
    public class AudioEffectPreset : ScriptableObject
    {
        [field: Header("Reverb")]
        [field: Tooltip("Adds reverberation, simulating the space the sound is in (room, cave, hall...).")]
        [field: SerializeField] public bool UseReverb { get; set; }
        [field: SerializeField] public AudioReverbPreset ReverbPreset { get; set; } = AudioReverbPreset.Generic;

        [field: Header("Echo")]
        [field: Tooltip("Repeats the sound with a decaying delay.")]
        [field: SerializeField] public bool UseEcho { get; set; }
        [field: SerializeField, Range(10f, 5000f)] public float EchoDelay { get; set; } = 500f;
        [field: SerializeField, Range(0f, 1f)] public float EchoDecayRatio { get; set; } = 0.5f;
        [field: SerializeField, Range(0f, 1f)] public float EchoWetMix { get; set; } = 1f;
        [field: SerializeField, Range(0f, 1f)] public float EchoDryMix { get; set; } = 1f;

        [field: Header("Chorus")]
        [field: Tooltip("Thickens the sound by layering slightly delayed, modulated copies.")]
        [field: SerializeField] public bool UseChorus { get; set; }
        [field: SerializeField, Range(0f, 1f)] public float ChorusDryMix { get; set; } = 0.5f;
        [field: SerializeField, Range(0f, 1f)] public float ChorusWetMix1 { get; set; } = 0.5f;
        [field: SerializeField, Range(0f, 1f)] public float ChorusWetMix2 { get; set; } = 0.5f;
        [field: SerializeField, Range(0f, 1f)] public float ChorusWetMix3 { get; set; } = 0.5f;
        [field: SerializeField, Range(0.1f, 100f)] public float ChorusDelay { get; set; } = 40f;
        [field: SerializeField, Range(0f, 20f)] public float ChorusRate { get; set; } = 0.8f;
        [field: SerializeField, Range(0f, 1f)] public float ChorusDepth { get; set; } = 0.03f;

        [field: Header("Distortion")]
        [field: Tooltip("Overdrives the sound for a gritty, broken effect.")]
        [field: SerializeField] public bool UseDistortion { get; set; }
        [field: SerializeField, Range(0f, 1f)] public float DistortionLevel { get; set; } = 0.5f;

        [field: Header("Low Pass")]
        [field: Tooltip("Cuts the high frequencies, muffling the sound (underwater, behind a wall...).")]
        [field: SerializeField] public bool UseLowPass { get; set; }
        [field: SerializeField, Range(10f, 22000f)] public float LowPassCutoff { get; set; } = 1000f;
        [field: SerializeField, Range(1f, 10f)] public float LowPassResonance { get; set; } = 1f;

        [field: Header("High Pass")]
        [field: Tooltip("Cuts the low frequencies, thinning the sound (small speaker, radio, phone...).")]
        [field: SerializeField] public bool UseHighPass { get; set; }
        [field: SerializeField, Range(10f, 22000f)] public float HighPassCutoff { get; set; } = 3000f;
        [field: SerializeField, Range(1f, 10f)] public float HighPassResonance { get; set; } = 1f;

        [field: Header("Volume")]
        [field: Tooltip("Attenuates the sound while the effect is applied. 1 leaves it untouched.")]
        [field: SerializeField] public bool UseVolume { get; set; }
        [field: SerializeField, Range(0f, 1f)] public float Volume { get; set; } = 1f;

        [field: Header("Pitch")]
        [field: Tooltip("Multiplies the pitch while the effect is applied. 1 leaves it untouched, " +
                        "0.5 is an octave down, 2 an octave up.")]
        [field: SerializeField] public bool UsePitch { get; set; }
        [field: SerializeField, Range(-3f, 3f)] public float Pitch { get; set; } = 1f;
    }
}
