/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MelenitasDev.SoundsGood.Domain
{
    public class OcclusionMaterialDataCollection : ScriptableObject
    {
        [SerializeField] private OcclusionMaterialData[] materials = Array.Empty<OcclusionMaterialData>();

        private Dictionary<string, OcclusionMaterialData> materialsDictionary = new Dictionary<string, OcclusionMaterialData>();

        public OcclusionMaterialData[] Materials => materials;

        void OnEnable ()
        {
            Init();
        }

        private void Init ()
        {
            materialsDictionary.Clear();
            foreach (OcclusionMaterialData materialData in materials)
            {
                if (materialData == null || string.IsNullOrEmpty(materialData.Tag)) continue;
                materialsDictionary[materialData.Tag] = materialData;
            }
        }

        public AudioOcclusionMaterial GetMaterial (string tag)
        {
            if (materialsDictionary == null || materialsDictionary.Count == 0) Init();

            if (materialsDictionary.TryGetValue(tag, out OcclusionMaterialData materialData)) return materialData.Material;

            Debug.LogWarning($"Occlusion material with tag '{tag}' does not exist.");
            return null;
        }

        public bool CreateMaterial (string tag, AudioOcclusionMaterial material, out string result)
        {
            if (tag == "")
            {
                result = "Tag required! Please, write a tag to identify this material.";
                return false;
            }

            if (materials.Any(materialData => materialData.Tag == tag))
            {
                result = $"The tag '{tag}' already exist!";
                return false;
            }

            OcclusionMaterialData newMaterial = new OcclusionMaterialData(tag, material);
            OcclusionMaterialData[] previousMaterials = materials;
            materials = new OcclusionMaterialData[materials.Length + 1];
            for (int i = 0; i < materials.Length - 1; i++)
            {
                materials[i] = previousMaterials[i];
            }
            materials[materials.Length - 1] = newMaterial;

            Init();
            result = $"Material '{tag}' has been created successfully.";
            return true;
        }

        public bool EditMaterial (string oldTag, string newTag, out string result)
        {
            if (string.IsNullOrEmpty(newTag))
            {
                result = "Tag required! Please, write a tag to identify this material.";
                return false;
            }

            if (oldTag != newTag && materials.Any(materialData => materialData.Tag == newTag))
            {
                result = $"The tag '{newTag}' already exist!";
                return false;
            }

            OcclusionMaterialData target = materials.FirstOrDefault(materialData => materialData.Tag == oldTag);
            if (target == null)
            {
                result = $"Material '{oldTag}' does not exist.";
                return false;
            }

            target.Tag = newTag;
            Init();
            result = $"Material '{newTag}' has been updated successfully.";
            return true;
        }

        public void RemoveMaterial (string tagToRemove)
        {
            List<OcclusionMaterialData> newMaterialsList = materials.ToList();
            foreach (var materialData in materials)
            {
                if (!materialData.Tag.Equals(tagToRemove)) continue;
                newMaterialsList.Remove(materialData);
                break;
            }
            materials = newMaterialsList.ToArray();
            Init();
        }

        public void RemoveAll ()
        {
            materials = Array.Empty<OcclusionMaterialData>();
            Init();
        }
    }
}
