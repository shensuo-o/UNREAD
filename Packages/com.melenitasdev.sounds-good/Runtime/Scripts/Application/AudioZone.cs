/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using UnityEngine;

namespace MelenitasDev.SoundsGood
{
    // Settings are serialized but kept private: the custom editor binds to them by name and the
    // Inspector edits them, while game code never reaches in to reshape the zone.
    public abstract partial class AudioZone // Properties
    {
        internal enum Shape
        {
            Sphere,
            Box
        }

        // Shape Label
        [SerializeField] private Shape zoneShape;

        [SerializeField] private bool useScaleAsZoneSize = false;

        [SerializeField] private float radius = 5f;
        [SerializeField] private float extraRadiusFade = 0.2f;
        [SerializeField] private float height = 5f;
        [SerializeField] private float width = 5f;
        [SerializeField] private float depth = 5f;
        [SerializeField] private bool perFaceFade = false;
        [SerializeField] private float uniformBoxFade = 0.5f;
        [SerializeField] private BoxFade boxFade;

        [SerializeField] private Color areaColor = new Color(0.992f, 0.694f, 0.012f);
        [SerializeField] private Color fadeColor = new Color(1f, 0.953f, 0.847f);
    }

    /// <summary>
    /// Shared base for the world audio zones (<see cref="AudioEffectZone"/>, <see cref="MusicZone"/>):
    /// a spherical or box region with a fade margin. Holds the shape configuration, the position →
    /// weight math (through <see cref="AudioZoneGeometry"/>) and the selection gizmos, so every zone
    /// is shaped and drawn identically. Subclasses decide what the weight drives - an effect, a
    /// music volume, etc.
    /// </summary>
    public abstract partial class AudioZone : MonoBehaviour
    {
        /// <summary> 1 inside the zone, fading to 0 across the margin, 0 outside. </summary>
        protected float GetWeight (Vector3 position)
        {
            return AudioZoneGeometry.Weight(transform, position, zoneShape == Shape.Box, useScaleAsZoneSize,
                radius, extraRadiusFade, width, height, depth, perFaceFade, uniformBoxFade, boxFade);
        }

        protected virtual void OnDrawGizmosSelected ()
        {
            AudioZoneGeometry.DrawGizmos(transform, zoneShape == Shape.Box, useScaleAsZoneSize,
                radius, extraRadiusFade, width, height, depth, perFaceFade, uniformBoxFade, boxFade,
                areaColor, fadeColor);
        }
    }
}
