using System.IO;
using System.Text.RegularExpressions;
using MelenitasDev.SoundsGood.Domain;
using UnityEditor;
using UnityEngine;

namespace MelenitasDev.SoundsGood.Editor
{
    internal class EditorHelper : MonoBehaviour
    {
        internal static readonly Color32 ORANGE_COLOR = new (255, 192, 88, 255);
        internal static readonly Color32 GREY_COLOR = new (142, 142, 142, 255);
        internal static readonly Color32 RED_COLOR = new (255, 65, 65, 255);
        
        internal static void SaveCollectionChanges (Sections section, bool saveAssets = true)
        {
            GenerateAudioEnum(section);
            
            if (section == Sections.Sounds) EditorUtility.SetDirty(AssetLocator.Instance.SoundDataCollection);
            else EditorUtility.SetDirty(AssetLocator.Instance.MusicDataCollection);
            
            if (!saveAssets) return;
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        internal static void ReloadOutputsDatabase (bool saveAssets = true)
        {
            AssetLocator.Instance.OutputDataCollection.LoadOutputs();
            EditorUtility.SetDirty(AssetLocator.Instance.OutputDataCollection);
            
            GenerateOutputsEnum();
            
            if (!saveAssets) return;
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        internal static void SaveEffectCollectionChanges (bool saveAssets = true)
        {
            GenerateEffectsEnum();

            EditorUtility.SetDirty(AssetLocator.Instance.EffectDataCollection);

            if (!saveAssets) return;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        internal static void SaveOcclusionMaterialCollectionChanges (bool saveAssets = true)
        {
            GenerateOcclusionMaterialsEnum();

            EditorUtility.SetDirty(AssetLocator.Instance.OcclusionMaterialCollection);

            if (!saveAssets) return;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        internal static void ChangeAudioClipImportSettings (AudioClip[] clips, CompressionPreset preset, bool forceMono)
        {
            foreach (AudioClip clip in clips)
            {
                AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
                if (importer == null) return;

                AudioImporterSampleSettings sampleSettings = importer.defaultSampleSettings;
                switch (preset)
                {
                    case CompressionPreset.AmbientMusic:
                        bool shortDuration = clip.length < 10;
                        sampleSettings.loadType = shortDuration
                            ? AudioClipLoadType.CompressedInMemory
                            : AudioClipLoadType.Streaming;
                        sampleSettings.compressionFormat =
                            shortDuration ? AudioCompressionFormat.ADPCM : AudioCompressionFormat.Vorbis;
                        sampleSettings.quality = 0.60f;
                        break;
                    case CompressionPreset.FrequentSound:
                        sampleSettings.loadType = AudioClipLoadType.DecompressOnLoad;
                        sampleSettings.compressionFormat = AudioCompressionFormat.ADPCM;
                        sampleSettings.quality = 1f;
                        break;
                    case CompressionPreset.OccasionalSound:
                        sampleSettings.loadType = AudioClipLoadType.CompressedInMemory;
                        sampleSettings.compressionFormat = AudioCompressionFormat.Vorbis;
                        sampleSettings.quality = 0.35f;
                        break;
                }

                importer.forceToMono = forceMono;
#if UNITY_2022_1_OR_NEWER
                sampleSettings.preloadAudioData = true;
#else
                importer.preloadAudioData = true;
#endif
                importer.loadInBackground = true;
                importer.defaultSampleSettings = sampleSettings;
                importer.SaveAndReimport();
            }
        }
        
        internal static bool IsTagValid (string tag)
        {
            if (string.IsNullOrEmpty(tag)) return false;
            if (Regex.IsMatch(tag, @"[^a-zA-Z0-9]")) return false;
            if (Regex.IsMatch(tag, "^[0-9]")) return false;
            if (!Regex.IsMatch(tag, @"[a-zA-Z]")) return false;
            return true;
        }
        
        internal static void DeleteDirectoryAndContent (string path)
        {
            if (!AssetDatabase.IsValidFolder(path)) return;

            AssetDatabase.DeleteAsset(path);
            AssetDatabase.Refresh();
        }
        
        // ----- Private Methods
        private static void GenerateAudioEnum (Sections section)
        {
            int i = 0;
            SoundData[] currentSoundsData = section == Sections.Sounds
                ? AssetLocator.Instance.SoundDataCollection.Sounds
                : AssetLocator.Instance.MusicDataCollection.MusicTracks;
            string[] tags = new string[currentSoundsData.Length];
            foreach (SoundData sound in currentSoundsData)
            {
                tags[i] = sound.Tag;
                i++;
            }

            using (EnumGenerator enumGenerator = new EnumGenerator())
            {
                string enumName = section == Sections.Sounds ? "SFX" : "Track";
                string enumLocationPath = AssetDatabase.GetAssetPath(section == Sections.Sounds
                    ? AssetLocator.Instance.SfxEnum
                    : AssetLocator.Instance.TracksEnum);
                enumGenerator.GeneratePseudoEnum(enumName, tags, enumLocationPath);
            }
        }
        
        private static void GenerateEffectsEnum ()
        {
            var collection = AssetLocator.Instance.EffectDataCollection;
            if (collection == null || AssetLocator.Instance.EffectsEnum == null) return;

            string[] tags = new string[collection.Effects.Length];
            int i = 0;
            foreach (EffectData effectData in collection.Effects)
            {
                tags[i] = effectData.Tag;
                i++;
            }

            using (EnumGenerator enumGenerator = new EnumGenerator())
            {
                enumGenerator.GeneratePseudoEnum("Effect", tags,
                    AssetDatabase.GetAssetPath(AssetLocator.Instance.EffectsEnum));
            }
        }

        private static void GenerateOcclusionMaterialsEnum ()
        {
            var collection = AssetLocator.Instance.OcclusionMaterialCollection;
            if (collection == null || AssetLocator.Instance.OcclusionMaterialsEnum == null) return;

            string[] tags = new string[collection.Materials.Length];
            int i = 0;
            foreach (OcclusionMaterialData materialData in collection.Materials)
            {
                tags[i] = materialData.Tag;
                i++;
            }

            using (EnumGenerator enumGenerator = new EnumGenerator())
            {
                enumGenerator.GeneratePseudoEnum("OcclusionMaterial", tags,
                    AssetDatabase.GetAssetPath(AssetLocator.Instance.OcclusionMaterialsEnum));
            }
        }

        private static void GenerateOutputsEnum ()
        {
            string[] outputNames = new string[AssetLocator.Instance.OutputDataCollection.Outputs.Length];
            int i = 0;
            foreach (OutputData outputData in AssetLocator.Instance.OutputDataCollection.Outputs)
            {
                outputNames[i] = outputData.Name.Replace(" ", "");
                i++;
            }
            
            using (EnumGenerator enumGenerator = new EnumGenerator())
            {
                enumGenerator.GeneratePseudoEnum("Output", outputNames, 
                    AssetDatabase.GetAssetPath(AssetLocator.Instance.OutputsEnum));
            }
        }
    }
}