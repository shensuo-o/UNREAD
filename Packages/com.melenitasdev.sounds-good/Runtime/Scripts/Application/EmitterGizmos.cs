/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using UnityEngine;

namespace MelenitasDev.SoundsGood
{
    /// <summary>
    /// Shared scene gizmos for the emitters: two wireframe spheres showing where the sound is
    /// fully audible (min distance) and where it fades out (max distance), like a native
    /// AudioSource's 3D distance handles.
    /// </summary>
    internal static class EmitterGizmos
    {
        private static readonly Color MinColor = new Color(0.992f, 0.694f, 0.012f, 0.75f);  // orange, fully audible
        private static readonly Color MaxColor = new Color(1f, 0.953f, 0.847f, 0.75f);       // light, edge of hearing

        public static void DrawHearDistance (Vector3 position, float minDistance, float maxDistance)
        {
            Gizmos.color = MinColor;
            Gizmos.DrawWireSphere(position, minDistance);

            Gizmos.color = MaxColor;
            Gizmos.DrawWireSphere(position, maxDistance);
        }
    }
}
