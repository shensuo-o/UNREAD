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
    public class EffectDataCollection : ScriptableObject
    {
        [SerializeField] private EffectData[] effects = Array.Empty<EffectData>();

        private Dictionary<string, EffectData> effectsDictionary = new Dictionary<string, EffectData>();

        public EffectData[] Effects => effects;

        void OnEnable ()
        {
            Init();
        }

        private void Init ()
        {
            effectsDictionary.Clear();
            foreach (EffectData effectData in effects)
            {
                if (effectData == null || string.IsNullOrEmpty(effectData.Tag)) continue;
                effectsDictionary[effectData.Tag] = effectData;
            }
        }

        public AudioEffectPreset GetEffect (string tag)
        {
            if (effectsDictionary == null || effectsDictionary.Count == 0) Init();

            if (effectsDictionary.TryGetValue(tag, out EffectData effectData)) return effectData.Preset;

            Debug.LogWarning($"Effect with tag '{tag}' does not exist.");
            return null;
        }

        public bool CreateEffect (string tag, AudioEffectPreset preset, out string result)
        {
            if (tag == "")
            {
                result = "Tag required! Please, write a tag to identify this effect.";
                return false;
            }

            if (effects.Any(effectData => effectData.Tag == tag))
            {
                result = $"The tag '{tag}' already exist!";
                return false;
            }

            EffectData newEffect = new EffectData(tag, preset);
            EffectData[] previousEffects = effects;
            effects = new EffectData[effects.Length + 1];
            for (int i = 0; i < effects.Length - 1; i++)
            {
                effects[i] = previousEffects[i];
            }
            effects[effects.Length - 1] = newEffect;

            Init();
            result = $"Effect '{tag}' has been created successfully.";
            return true;
        }

        public bool EditEffect (string oldTag, string newTag, out string result)
        {
            if (string.IsNullOrEmpty(newTag))
            {
                result = "Tag required! Please, write a tag to identify this effect.";
                return false;
            }

            if (oldTag != newTag && effects.Any(effectData => effectData.Tag == newTag))
            {
                result = $"The tag '{newTag}' already exist!";
                return false;
            }

            EffectData target = effects.FirstOrDefault(effectData => effectData.Tag == oldTag);
            if (target == null)
            {
                result = $"Effect '{oldTag}' does not exist.";
                return false;
            }

            target.Tag = newTag;
            Init();
            result = $"Effect '{newTag}' has been updated successfully.";
            return true;
        }

        public void RemoveEffect (string tagToRemove)
        {
            List<EffectData> newEffectsList = effects.ToList();
            foreach (var effectData in effects)
            {
                if (!effectData.Tag.Equals(tagToRemove)) continue;
                newEffectsList.Remove(effectData);
                break;
            }
            effects = newEffectsList.ToArray();
            Init();
        }

        public void RemoveAll ()
        {
            effects = Array.Empty<EffectData>();
            Init();
        }
    }
}
