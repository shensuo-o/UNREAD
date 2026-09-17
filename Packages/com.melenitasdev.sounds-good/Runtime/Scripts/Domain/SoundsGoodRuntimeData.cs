/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using UnityEngine;
using UnityEngine.Audio;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MelenitasDev.SoundsGood.Domain
{
    /// <summary>
    /// Serialized references to the user's audio data (collections, generated enums and the
    /// master mixer). Lives in the user's Assets (Assets/SoundsGood/Resources/) so it survives
    /// package updates and stays writable on immutable (UPM) installs.
    ///
    /// In the editor each getter re-resolves its reference by name if missing, so moving the
    /// data around (via DataRootPath) keeps working. That resolution is editor-only, so before
    /// a build the references must be resolved AND persisted to disk (see ResolveAndPersist),
    /// because the build reads the serialized values only.
    /// </summary>
    internal class SoundsGoodRuntimeData : ScriptableObject
    {
        [Header("Collections")]
        [SerializeField] private SoundDataCollection soundDataCollection;
        [SerializeField] private MusicDataCollection musicDataCollection;
        [SerializeField] private OutputDataCollection outputDataCollection;
        [SerializeField] private EffectDataCollection effectDataCollection;
        [SerializeField] private OcclusionMaterialDataCollection occlusionMaterialCollection;
        [SerializeField] private AudioMixer masterAudioMixer;

        [Header("Enumerators")]
        [SerializeField] private TextAsset sfxEnum;
        [SerializeField] private TextAsset tracksEnum;
        [SerializeField] private TextAsset outputsEnum;
        [SerializeField] private TextAsset effectsEnum;
        [SerializeField] private TextAsset occlusionMaterialsEnum;

        private static SoundsGoodRuntimeData instance;
        internal static SoundsGoodRuntimeData Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<SoundsGoodRuntimeData>("SoundsGoodRuntimeData");
                }

                return instance;
            }
        }

        internal static void ResetCache () => instance = null;

        internal SoundDataCollection SoundDataCollection
        {
            get
            {
#if UNITY_EDITOR
                if (soundDataCollection == null)
                    soundDataCollection = GetFromUserData<SoundDataCollection>("SoundCollection");
#endif
                return soundDataCollection;
            }
        }

        internal MusicDataCollection MusicDataCollection
        {
            get
            {
#if UNITY_EDITOR
                if (musicDataCollection == null)
                    musicDataCollection = GetFromUserData<MusicDataCollection>("MusicCollection");
#endif
                return musicDataCollection;
            }
        }

        internal OutputDataCollection OutputDataCollection
        {
            get
            {
#if UNITY_EDITOR
                if (outputDataCollection == null)
                    outputDataCollection = GetFromUserData<OutputDataCollection>("OutputCollection");
#endif
                return outputDataCollection;
            }
        }

        internal EffectDataCollection EffectDataCollection
        {
            get
            {
#if UNITY_EDITOR
                if (effectDataCollection == null)
                    effectDataCollection = GetFromUserData<EffectDataCollection>("EffectCollection");
#endif
                return effectDataCollection;
            }
        }

        internal OcclusionMaterialDataCollection OcclusionMaterialCollection
        {
            get
            {
#if UNITY_EDITOR
                if (occlusionMaterialCollection == null)
                    occlusionMaterialCollection = GetFromUserData<OcclusionMaterialDataCollection>("OcclusionMaterialCollection");
#endif
                return occlusionMaterialCollection;
            }
        }

        internal AudioMixer MasterAudioMixer
        {
            get
            {
#if UNITY_EDITOR
                if (masterAudioMixer == null)
                    masterAudioMixer = GetFromUserData<AudioMixer>("Master");
#endif
                return masterAudioMixer;
            }
        }

        internal TextAsset SfxEnum
        {
            get
            {
#if UNITY_EDITOR
                if (sfxEnum == null)
                    sfxEnum = GetFromUserData<TextAsset>("SFX_Generated");
#endif
                return sfxEnum;
            }
        }

        internal TextAsset TracksEnum
        {
            get
            {
#if UNITY_EDITOR
                if (tracksEnum == null)
                    tracksEnum = GetFromUserData<TextAsset>("Track_Generated");
#endif
                return tracksEnum;
            }
        }

        internal TextAsset OutputsEnum
        {
            get
            {
#if UNITY_EDITOR
                if (outputsEnum == null)
                    outputsEnum = GetFromUserData<TextAsset>("Output_Generated");
#endif
                return outputsEnum;
            }
        }

        internal TextAsset EffectsEnum
        {
            get
            {
#if UNITY_EDITOR
                if (effectsEnum == null)
                    effectsEnum = GetFromUserData<TextAsset>("Effect_Generated");
#endif
                return effectsEnum;
            }
        }

        internal TextAsset OcclusionMaterialsEnum
        {
            get
            {
#if UNITY_EDITOR
                if (occlusionMaterialsEnum == null)
                    occlusionMaterialsEnum = GetFromUserData<TextAsset>("OcclusionMaterial_Generated");
#endif
                return occlusionMaterialsEnum;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Force-resolves every reference and writes them to disk, so a build (where the
        /// editor-only resolution above is stripped) reads valid serialized references.
        /// </summary>
        internal void ResolveAndPersist ()
        {
            _ = SoundDataCollection;
            _ = MusicDataCollection;
            _ = OutputDataCollection;
            _ = EffectDataCollection;
            _ = OcclusionMaterialCollection;
            _ = MasterAudioMixer;
            _ = SfxEnum;
            _ = TracksEnum;
            _ = OutputsEnum;
            _ = EffectsEnum;
            _ = OcclusionMaterialsEnum;

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        private static T GetFromUserData<T> (string filenameWithoutExtension) where T : Object
        {
            string rootPath = "Assets/Plugins/SoundsGood/Data/";
            if (AssetLocator.SoundsGoodSettings != null)
            {
                rootPath = AssetLocator.SoundsGoodSettings.GetNormalizedDataRootPath();
            }

            var searchFolders = new[] { rootPath };

            string filter = $"{filenameWithoutExtension} t:{typeof(T).Name}";

            string[] guids = AssetDatabase.FindAssets(filter, searchFolders);
            if (guids == null || guids.Length <= 0)
            {
                Debug.LogError($"[SoundsGood] Asset of type {typeof(T).Name} named '{filenameWithoutExtension}' " +
                               $"not found under '{rootPath}'.");
                return null;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                Debug.LogError($"[SoundsGood] Found asset path '{assetPath}', but failed to load as type {typeof(T).Name}.");
            }
            return asset;
        }
#endif
    }
}
