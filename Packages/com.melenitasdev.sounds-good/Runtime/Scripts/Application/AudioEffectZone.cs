/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using MelenitasDev.SoundsGood.Domain;
using UnityEngine;

namespace MelenitasDev.SoundsGood
{
    // Settings are serialized but kept private: the custom editor binds to them by name and the
    // Inspector edits them, while game code never reaches in to reshape the zone.
    public partial class AudioEffectZone // Properties
    {
        /// <summary>What the zone's effect reaches.</summary>
        public enum Affect
        {
            /// <summary>Only sounds physically inside the zone (heard with the effect from anywhere).</summary>
            Sources,
            /// <summary>Every Sounds Good sound, while the listener is inside (the whole mix goes underwater).</summary>
            Listener,
            /// <summary>Both at once, combined so a source inside is never processed twice.</summary>
            Both
        }

        // Effect Label
        [SerializeField] private Effect effect = default;
        [SerializeField] private Affect affect = Affect.Sources;
    }

    /// <summary>
    /// Applies an <see cref="Effect"/> to the sounds a spherical or box zone reaches, fading it across
    /// the zone's margin. What it reaches is set by <see cref="Affect"/>. The shape, weight math and
    /// gizmos come from <see cref="AudioZone"/>.
    /// </summary>
    [AddComponentMenu("Sounds Good/Effects/Audio Effect Zone", 20)]
    public partial class AudioEffectZone : AudioZone
    {
        void Update ()
        {
            AudioEffectPreset preset = SoundsGoodManager.GetEffect(effect);
            if (preset == null) return;

            // The listener weight is the same for every source, so measure it once per frame.
            float listenerWeight = 0f;
            if (affect != Affect.Sources && SoundsGoodManager.TryGetListener(out AudioListener listener))
                listenerWeight = GetWeight(listener.transform.position);

            foreach (SoundsGoodAudioSource source in SoundsGoodManager.AudioSourcePool)
            {
                if (source == null || !source.Using) continue;

                float weight = affect switch
                {
                    Affect.Sources => GetWeight(source.transform.position),
                    Affect.Listener => listenerWeight,
                    // Max, so a source inside gets one effect (never source + listener stacked) and
                    // the transition in/out of the zone has no dip.
                    _ => Mathf.Max(GetWeight(source.transform.position), listenerWeight)
                };

                if (weight > 0f) source.ClaimZoneEffect(preset, weight);
            }
        }
    }
}
