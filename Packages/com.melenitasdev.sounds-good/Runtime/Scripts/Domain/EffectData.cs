/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using UnityEngine;

namespace MelenitasDev.SoundsGood.Domain
{
    [System.Serializable]
    public class EffectData
    {
        [SerializeField] private string tag;
        [SerializeField] private AudioEffectPreset preset;

        public string Tag { get => tag; set => tag = value; }
        public AudioEffectPreset Preset { get => preset; set => preset = value; }

        public EffectData (string tag, AudioEffectPreset preset)
        {
            this.tag = tag;
            this.preset = preset;
        }
    }
}
