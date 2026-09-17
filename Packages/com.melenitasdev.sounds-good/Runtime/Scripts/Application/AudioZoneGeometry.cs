/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using System;
using UnityEngine;

namespace MelenitasDev.SoundsGood
{
    /// <summary>
    /// Per-face fade distances (world units) for a box zone. Each value is how far the effect keeps
    /// fading out past that face; 0 means a hard edge flush with the face (no bleed past it). The
    /// faces are named in the object's local space, so they follow its rotation.
    /// </summary>
    [Serializable]
    internal struct BoxFade
    {
        [Min(0f)] [Tooltip("Fade distance past the left face (-X), in world units. 0 = hard edge.")]
        public float left;
        [Min(0f)] [Tooltip("Fade distance past the right face (+X), in world units. 0 = hard edge.")]
        public float right;
        [Min(0f)] [Tooltip("Fade distance past the bottom face (-Y), in world units. 0 = hard edge.")]
        public float bottom;
        [Min(0f)] [Tooltip("Fade distance past the top face (+Y), in world units. 0 = hard edge.")]
        public float top;
        [Min(0f)] [Tooltip("Fade distance past the back face (-Z), in world units. 0 = hard edge.")]
        public float back;
        [Min(0f)] [Tooltip("Fade distance past the front face (+Z), in world units. 0 = hard edge.")]
        public float front;

        public bool AnyFade => left > 0f || right > 0f || bottom > 0f || top > 0f || back > 0f || front > 0f;

        /// <summary> The same fade distance on every face. </summary>
        public static BoxFade Uniform (float distance) => new BoxFade
        {
            left = distance, right = distance, bottom = distance, top = distance, back = distance, front = distance
        };
    }

    /// <summary>
    /// Shared math and gizmos for the audio zones (Audio Effect / Listener Effect / Music). Keeps
    /// the weight (0 outside, 1 inside, fading across the margin) and the wireframe drawing in one
    /// place so every zone behaves identically. Box zones follow the transform's rotation and
    /// support an independent fade distance per face; sphere zones fade uniformly by radius.
    /// </summary>
    internal static class AudioZoneGeometry
    {
        /// <summary> 1 inside the zone, fading to 0 across each face's margin, 0 outside. </summary>
        public static float Weight (Transform t, Vector3 worldPos, bool isBox, bool useScaleAsSize,
            float radius, float extraRadiusFade, float width, float height, float depth,
            bool perFaceFade, float uniformBoxFade, in BoxFade boxFade)
        {
            if (!isBox) return SphereWeight(t, worldPos, useScaleAsSize, radius, extraRadiusFade);

            BoxFade fade = perFaceFade ? boxFade : BoxFade.Uniform(uniformBoxFade);
            return BoxWeight(t, worldPos, useScaleAsSize, width, height, depth, fade);
        }

        private static float BoxWeight (Transform t, Vector3 worldPos, bool useScaleAsSize,
            float width, float height, float depth, in BoxFade fade)
        {
            Vector3 half = (useScaleAsSize ? t.localScale : new Vector3(width, height, depth)) * 0.5f;

            // Into the box's local frame (rotation only) so the faces follow the object's rotation.
            Vector3 local = Quaternion.Inverse(t.rotation) * (worldPos - t.position);

            float f = 0f;
            f = Mathf.Max(f, FaceFade(local.x, half.x, fade.left, fade.right));
            f = Mathf.Max(f, FaceFade(local.y, half.y, fade.bottom, fade.top));
            f = Mathf.Max(f, FaceFade(local.z, half.z, fade.back, fade.front));
            return 1f - f;
        }

        // 0 inside the core on this axis, fading to 1 across the relevant face's margin. The face is
        // picked by the sign, so each side of the axis fades over its own distance (0 = hard cut).
        private static float FaceFade (float signedDistance, float half, float fadeNeg, float fadePos)
        {
            float d = Mathf.Abs(signedDistance);
            if (d <= half) return 0f;

            float margin = signedDistance >= 0f ? fadePos : fadeNeg;
            if (margin <= 0f) return 1f;
            if (d >= half + margin) return 1f;

            return (d - half) / margin;
        }

        private static float SphereWeight (Transform t, Vector3 worldPos, bool useScaleAsSize,
            float radius, float extraRadiusFade)
        {
            float r = useScaleAsSize ? t.localScale.x : radius;
            float fadeR = r * (extraRadiusFade + 1f);

            float distance = Vector3.Distance(t.position, worldPos);
            if (distance <= r) return 1f;
            if (distance >= fadeR) return 0f;

            return 1f - ((distance - r) / (fadeR - r));
        }

        /// <summary> Draws the zone's core and fade wireframes, following the transform's rotation. </summary>
        public static void DrawGizmos (Transform t, bool isBox, bool useScaleAsSize,
            float radius, float extraRadiusFade, float width, float height, float depth,
            bool perFaceFade, float uniformBoxFade, in BoxFade boxFade,
            Color areaColor, Color fadeColor)
        {
            if (isBox)
            {
                BoxFade fade = perFaceFade ? boxFade : BoxFade.Uniform(uniformBoxFade);

                Vector3 size = useScaleAsSize ? t.localScale : new Vector3(width, height, depth);
                Vector3 half = size * 0.5f;

                Matrix4x4 previous = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(t.position, t.rotation, Vector3.one);

                Gizmos.color = areaColor;
                Gizmos.DrawWireCube(Vector3.zero, size);

                if (fade.AnyFade)
                {
                    // Each face pushed out by its own margin, so the fade box can be asymmetric.
                    Vector3 min = new Vector3(-half.x - fade.left, -half.y - fade.bottom, -half.z - fade.back);
                    Vector3 max = new Vector3(half.x + fade.right, half.y + fade.top, half.z + fade.front);

                    Gizmos.color = fadeColor;
                    Gizmos.DrawWireCube((min + max) * 0.5f, max - min);
                }

                Gizmos.matrix = previous;
                return;
            }

            float r = useScaleAsSize ? t.localScale.x : radius;

            Gizmos.color = areaColor;
            Gizmos.DrawWireSphere(t.position, r);

            if (extraRadiusFade <= 0f) return;
            Gizmos.color = fadeColor;
            Gizmos.DrawWireSphere(t.position, r * (extraRadiusFade + 1f));
        }
    }
}
