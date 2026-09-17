using UnityEngine;

namespace MelenitasDev.SoundsGood.Domain
{
    public class SoundsGoodSettings : ScriptableObject
    {
        [field: Header("Data")]
        [field: Tooltip("Project-relative root path where Sounds Good will store its data assets (collections, mixers, generated enums, etc.). For example: 'Assets/Plugins/SoundsGood/Data/'.")]
        [field: SerializeField]
        public string DataRootPath { get; set; } = "Assets/SoundsGood/Data/";
        
        [field: SerializeField, HideInInspector]
        public string LastAppliedDataRootPath { get; set; } = string.Empty;

        [field: Header("Occlusion")]
        [field: Tooltip("Enables or disables occlusion globally. If disabled, no occlusion raycasts or filters will be applied.")]
        [field: SerializeField]
        public bool EnableOcclusion { get; set; } = true;

        [field: Tooltip("Layers that are considered physical obstacles for occlusion (walls, doors, props, etc.).")]
        [field: SerializeField]
        public LayerMask OcclusionLayers { get; set; } = ~0;

        [field: Tooltip("Maximum distance at which occlusion is evaluated. Beyond this range, no occlusion is computed.")]
        [field: SerializeField, Min(0f)]
        public float MaxDistance { get; set; } = 50f;

        [field: Tooltip("Minimum low-pass cutoff frequency when occlusion is at its maximum (fully blocked).")]
        [field: SerializeField, Min(10f)]
        public float MinCutoff { get; set; } = 1200f;

        [field: Tooltip("Maximum low-pass cutoff frequency when there is no occlusion (fully visible).")]
        [field: SerializeField, Min(10f)]
        public float MaxCutoff { get; set; } = 22000f;

        [field: Tooltip("Final volume multiplier when occlusion is at its maximum. (0 = muted, 1 = unchanged volume).")]
        [field: SerializeField, Range(0f, 1f)]
        public float MinVolumeMultiplier { get; set; } = 0.25f;
        
        [field: Tooltip("Approximate maximum number of occlusion layers considered for extra attenuation when audio is already fully occluded.")]
        [field: SerializeField, Min(1)]
        public int MaxOcclusionLayers { get; set; } = 3;

        [field: Tooltip("Additional volume multiplier when occlusion depth reaches its maximum (stacked walls/rooms). 1 = no extra attenuation, 0 = fully muted.")]
        [field: SerializeField, Range(0f, 1f)]
        public float ExtraDepthMinVolumeMultiplier { get; set; } = 0.6f;

        [field: Tooltip("Additional high-frequency reduction factor when occlusion depth reaches its maximum. 1 = no extra extra filtering, 0 = very dark sound.")]
        [field: SerializeField, Range(0.1f, 1f)]
        public float ExtraDepthCutoffMultiplier { get; set; } = 0.7f;

        [field: Tooltip("Maximum number of indirect raycast bounces. 0 = direct occlusion only, 1 = one bounce layer, etc.")]
        [field: SerializeField, Min(0)]
        public int MaxBounces { get; set; } = 1;

        [field: Tooltip("Minimum radius of the bounce ring around the listener used to approximate diffraction around corners.")]
        [field: SerializeField, Min(0f)]
        public float BounceRadiusMin { get; set; } = 1.0f;

        [field: Tooltip("Number of rays used in the bounce ring. Higher values give smoother occlusion at a higher CPU cost.")]
        [field: SerializeField, Min(4)]
        public int BounceRaysPerCircle { get; set; } = 8;

        [field: Tooltip("Time interval (in seconds) between occlusion recalculations for each active audio source.")]
        [field: SerializeField, Min(0.01f)]
        public float CheckInterval { get; set; } = 0.1f;

        [field: Tooltip("How quickly audio responds to occlusion changes (used for smooth transitions over time).")]
        [field: SerializeField, Min(0.01f)]
        public float LerpSpeed { get; set; } = 10f;
        
        public string GetNormalizedDataRootPath ()
        {
            if (string.IsNullOrWhiteSpace(DataRootPath)) return "Assets/SoundsGood/Data/";
            
            string path = DataRootPath.Trim();
            if (!path.StartsWith("Assets/") && !path.StartsWith("Assets\\"))
            {
                path = "Assets/" + path.TrimStart('/', '\\');
            }
            
            return path;
        }
        
        public void ResetToDefaults ()
        {
            DataRootPath = "Assets/SoundsGood/Data/";
            LastAppliedDataRootPath = string.Empty;

            EnableOcclusion = true;
            OcclusionLayers = ~0;

            MaxDistance = 50f;
            MinCutoff = 1200f;
            MaxCutoff = 22000f;
            MinVolumeMultiplier = 0.25f;

            MaxOcclusionLayers = 3;
            ExtraDepthMinVolumeMultiplier = 0.6f;
            ExtraDepthCutoffMultiplier = 0.7f;

            MaxBounces = 1;
            BounceRadiusMin = 1.0f;
            BounceRaysPerCircle = 8;

            CheckInterval = 0.1f;
            LerpSpeed = 10f;
        }
    }
}