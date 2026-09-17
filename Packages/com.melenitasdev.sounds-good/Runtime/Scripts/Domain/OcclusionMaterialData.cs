/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using UnityEngine;

namespace MelenitasDev.SoundsGood.Domain
{
    [System.Serializable]
    public class OcclusionMaterialData
    {
        [SerializeField] private string tag;
        [SerializeField] private AudioOcclusionMaterial material;

        public string Tag { get => tag; set => tag = value; }
        public AudioOcclusionMaterial Material { get => material; set => material = value; }

        public OcclusionMaterialData (string tag, AudioOcclusionMaterial material)
        {
            this.tag = tag;
            this.material = material;
        }
    }
}
