/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using System.Collections.Generic;
using MelenitasDev.SoundsGood.Domain;
using UnityEngine;

namespace MelenitasDev.SoundsGood
{
    // Settings are serialized but kept private: the custom editor binds to them by name and the
    // Inspector edits them, while game code just reads the resulting Density.
    public partial class AudioOcclusionSurface // Properties
    {
        /// <summary>Where the surface's occlusion density comes from.</summary>
        public enum DensitySource
        {
            /// <summary>A reusable Occlusion Material created in the Occlusion Material Creator.</summary>
            Material,
            /// <summary>A value set directly on this object with the slider below.</summary>
            Custom
        }

        [SerializeField] private DensitySource source = DensitySource.Material;
        [SerializeField] private OcclusionMaterial material = OcclusionMaterial.Null;
        [SerializeField, Range(0f, 1f)] private float customDensity = 0.5f;
        [SerializeField] private bool includeChildren = false;
    }

    /// <summary>
    /// Tells the occlusion system how much this object's collider(s) block sound, so a thin window
    /// muffles less than a dense concrete wall instead of every collider counting the same. Put it on
    /// an object with a collider and pick a reusable <b>Material</b> or set a <b>Custom</b> density
    /// (0 = sound passes through, 1 = full block, the same as a plain collider). A collider without
    /// this component keeps blocking fully, so existing scenes are unaffected.
    /// </summary>
    [AddComponentMenu("Sounds Good/Occlusion Surface", 44)]
    public partial class AudioOcclusionSurface : MonoBehaviour
    {
        private readonly List<Collider> registeredColliders = new List<Collider>();

        /// <summary>How much this surface occludes sound, 0 (transparent) to 1 (full block).</summary>
        public float Density
        {
            get
            {
                if (source == DensitySource.Custom) return customDensity;

                AudioOcclusionMaterial mat = SoundsGoodManager.GetOcclusionMaterial(material);
                return mat != null ? mat.Density : 1f;
            }
        }

        void OnEnable ()
        {
            RegisterColliders();
        }

        void OnDisable ()
        {
            UnregisterColliders();
        }

        private void RegisterColliders ()
        {
            UnregisterColliders();

            Collider[] colliders = includeChildren
                ? GetComponentsInChildren<Collider>()
                : GetComponents<Collider>();

            foreach (Collider collider in colliders)
            {
                OcclusionSurfaceRegistry.Register(collider, this);
                registeredColliders.Add(collider);
            }
        }

        private void UnregisterColliders ()
        {
            foreach (Collider collider in registeredColliders)
                OcclusionSurfaceRegistry.Unregister(collider);

            registeredColliders.Clear();
        }
    }

    /// <summary>
    /// Maps a collider to the surface that describes its density, so the occlusion raycasts can look
    /// up how much a hit collider blocks in O(1) without a GetComponent per ray. Surfaces register
    /// their colliders while enabled and drop them when disabled/destroyed.
    /// </summary>
    internal static class OcclusionSurfaceRegistry
    {
        private static readonly Dictionary<Collider, AudioOcclusionSurface> surfaces =
            new Dictionary<Collider, AudioOcclusionSurface>();

        internal static void Register (Collider collider, AudioOcclusionSurface surface)
        {
            if (collider == null) return;
            surfaces[collider] = surface;
        }

        internal static void Unregister (Collider collider)
        {
            if (collider == null) return;
            surfaces.Remove(collider);
        }

        /// <summary> The collider's occlusion density, or 1 (full block) if it has no surface. </summary>
        internal static float GetDensity (Collider collider)
        {
            if (collider != null && surfaces.TryGetValue(collider, out AudioOcclusionSurface surface) && surface != null)
                return surface.Density;

            return 1f;
        }
    }
}
