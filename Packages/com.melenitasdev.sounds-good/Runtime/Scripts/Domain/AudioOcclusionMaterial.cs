/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using UnityEngine;

namespace MelenitasDev.SoundsGood.Domain
{
    /// <summary>
    /// How strongly a surface blocks sound. Assigned to scene objects through an Audio Occlusion
    /// Surface component so that a thin window occludes less than a dense concrete wall, instead of
    /// every collider blocking equally. Density goes from 0 (sound passes through untouched) to
    /// 1 (full occlusion, same as a plain collider today).
    /// </summary>
    [CreateAssetMenu(menuName = "Sounds Good/Audio Occlusion Material", fileName = "AudioOcclusionMaterial")]
    public class AudioOcclusionMaterial : ScriptableObject
    {
        [field: Tooltip("How much this material occludes sound: 0 = fully transparent (no muffling), " +
                        "1 = fully blocking (same as a plain collider).")]
        [field: SerializeField, Range(0f, 1f)] public float Density { get; set; } = 0.5f;

        [field: Tooltip("Color used to tint this material in the editor (Occlusion Material Creator list).")]
        [field: SerializeField] public Color EditorColor { get; set; } = new Color(0.992f, 0.694f, 0.012f);
    }
}
