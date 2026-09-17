using UnityEditor;
using UnityEngine;
using System.IO;
using MelenitasDev.SoundsGood.Domain;

namespace MelenitasDev.SoundsGood.Editor
{
    static class DefaultDataInitializer
    {
        // ----- Constants
        private const string LEGACY_USER_DATA_PATH = "Assets/SoundsGood/Data/";
        private const string DEFAULT_DATA_PATH = "Packages/com.melenitasdev.sounds-good/Runtime/Data/Default/";

        // Settings live in the user's Assets (found by name via Resources.Load, regardless
        // of folder) so a package update can't wipe them. The location is fixed, NOT derived
        // from DataRootPath, to avoid a bootstrap paradox (DataRootPath is stored inside the
        // settings asset itself).
        private const string SETTINGS_FOLDER = "Assets/SoundsGood/Resources/";
        private const string SETTINGS_ASSET_PATH = "Assets/SoundsGood/Resources/SoundsGoodSettings.asset";

        // Runtime data (references to the user's collections/enums/mixer) also lives in the
        // user's Assets Resources so it survives updates and can be persisted before a build.
        private const string RUNTIME_DATA_ASSET_PATH = "Assets/SoundsGood/Resources/SoundsGoodRuntimeData.asset";

        private static readonly string[] dataSubfolders = { "Collections", "Mixers", "Generated" };

        // Ready-made example effects shipped with the package (one .asset per name under
        // Runtime/Data/Default/Effects/). Copied into the user's data the first time so the Effect
        // Creator window already shows usable presets. To ship another, drop its preset asset in that
        // folder and add its name here. The name is used both as the file name and the effect tag.
        private static readonly string[] defaultEffects = { "Cave", "Underwater", "PhoneCall" };

        // Ready-made example occlusion materials shipped with the package (one .asset per name under
        // Runtime/Data/Default/OcclusionMaterials/). Copied into the user's data the first time so the
        // Occlusion Material Creator window already shows usable materials. The name is used both as
        // the file name and the material tag.
        private static readonly string[] defaultOcclusionMaterials = { "Concrete", "Metal", "Wood", "Glass" };

        // Once the generated enum files are created they start out empty, and their content can only
        // be written after Unity has imported them and AssetLocator can see the collections. That
        // takes an unknown number of editor ticks, so this is the one thing here that polls — and it
        // stops the moment it succeeds. The cap keeps a broken install from polling forever.
        private const int MAX_ENUM_WRITE_ATTEMPTS = 600;

        // ----- Fields
        private static bool pumpingEnums;
        private static int enumWriteAttempts;
        private static bool enumsCreated;
        private static bool enumsInitialized;

        // ----- Properties
        private static SoundsGoodSettings Settings => AssetLocator.SoundsGoodSettings;

        // ----- Unity Events
        [InitializeOnLoadMethod]
        private static void OnInitialize ()
        {
            // Deferred one frame: on a domain reload the AssetDatabase isn't ready to be queried yet.
            EditorApplication.delayCall += Run;
        }

        // ----- Internal Methods
        /// <summary>
        /// Creates or migrates every asset Sounds Good needs. This is initialization, so it runs once
        /// per domain reload — never per frame. Call it again after changing the data root path, which
        /// is the only setting that invalidates what it did.
        /// </summary>
        internal static void Run ()
        {
            EnsureSettingsAsset();
            EnsureRuntimeDataAsset();
            EnsureUserDataRoot();
            CopyAllDefaultsToUserData();
            EnsureEffectAssets();
            EnsureOcclusionMaterialAssets();

            if (enumsCreated && !enumsInitialized) StartEnumPump();
            else if (GeneratedEnumsNeedGuard()) RegenerateEnums();
        }

        /// <summary>
        /// True when the generated pseudo-enums predate the compilation guard around them. Those files
        /// live in the user's Assets and outlive the package, so without the guard uninstalling Sounds
        /// Good leaves them declaring a type whose constructor is gone. Rewriting them once fixes it.
        /// </summary>
        private static bool GeneratedEnumsNeedGuard ()
        {
            string assetPath = GetUserDataRootPath() + "Generated/SFX_Generated.cs";
            if (!AssetFileExists(assetPath)) return false;

            try
            {
                string content = File.ReadAllText(Path.Combine(Application.dataPath, "../", assetPath));
                return content.Length > 0 && !content.Contains("#if " + EnumGenerator.PACKAGE_DEFINE);
            }
            catch (IOException)
            {
                return false;
            }
        }

        private static void RegenerateEnums ()
        {
            enumsCreated = true;
            enumsInitialized = false;
            StartEnumPump();
        }

        // ----- Private Methods
        private static void StartEnumPump ()
        {
            if (pumpingEnums) return;

            pumpingEnums = true;
            enumWriteAttempts = 0;
            EditorApplication.update += PumpEnums;
        }

        private static void StopEnumPump ()
        {
            if (!pumpingEnums) return;

            pumpingEnums = false;
            EditorApplication.update -= PumpEnums;
        }

        private static void PumpEnums ()
        {
            if (enumsInitialized)
            {
                StopEnumPump();
                return;
            }

            if (++enumWriteAttempts > MAX_ENUM_WRITE_ATTEMPTS)
            {
                StopEnumPump();
                Debug.LogError("[Sounds Good] Gave up writing the generated enums: the data collections " +
                               "never became available. Check that the data folder isn't missing or " +
                               "read-only, then reimport the package.");
                return;
            }

            WriteEnumsContent();
        }

        private static void EnsureSettingsAsset ()
        {
            var existing = AssetLocator.SoundsGoodSettings;
            string existingPath = existing != null ? AssetDatabase.GetAssetPath(existing) : string.Empty;

            // Already stored in the user's Assets. Nothing to do.
            if (existing != null && existingPath.StartsWith("Assets/")) return;

            EnsureFolderHierarchy(SETTINGS_FOLDER.TrimEnd('/'));

            // Legacy asset living inside the package (older installs). Migrate a copy into
            // Assets — preserving the user's values — and drop the package-side copy so a
            // future update can't wipe it and Resources.Load isn't left ambiguous.
            if (existing != null && existingPath.StartsWith("Packages/"))
            {
                if (AssetDatabase.CopyAsset(existingPath, SETTINGS_ASSET_PATH))
                {
                    // Best-effort: fails silently on read-only (immutable UPM) installs,
                    // which is fine since new package versions no longer ship this asset.
                    AssetDatabase.DeleteAsset(existingPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    AssetLocator.ResetSettingsCache();
                }
                else
                {
                    Debug.LogError($"[SoundsGood] Failed to migrate settings from '{existingPath}' " +
                                   $"to '{SETTINGS_ASSET_PATH}'.");
                }

                return;
            }

            // Nothing anywhere. Create a fresh settings asset in the user's Assets.
            var instance = ScriptableObject.CreateInstance<SoundsGoodSettings>();
            instance.ResetToDefaults();

            AssetDatabase.CreateAsset(instance, SETTINGS_ASSET_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AssetLocator.ResetSettingsCache();
        }

        private static void EnsureRuntimeDataAsset ()
        {
            if (SoundsGoodRuntimeData.Instance != null) return;

            EnsureFolderHierarchy(SETTINGS_FOLDER.TrimEnd('/'));

            var instance = ScriptableObject.CreateInstance<SoundsGoodRuntimeData>();

            AssetDatabase.CreateAsset(instance, RUNTIME_DATA_ASSET_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SoundsGoodRuntimeData.ResetCache();
        }
        
        private static string GetUserDataRootPath ()
        {
            string root = Settings.GetNormalizedDataRootPath();
            root = root.Trim().Replace("\\", "/");

            if (PathUtility.TrySanitizeDataRootPath(root, out var safe, out _)) return safe;
            
            safe = "Assets/SoundsGood/Data/";
            
            if (Settings == null) return safe;
            
            Settings.DataRootPath = safe;
            Settings.LastAppliedDataRootPath = safe;
            EditorUtility.SetDirty(Settings);
            AssetDatabase.SaveAssets();

            return safe;
        }

        private static string NormalizeAssetsPath (string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            string result = path.Trim().Replace("\\", "/");

            if (!result.StartsWith("Assets/"))
                result = "Assets/" + result.TrimStart('/');

            if (!result.EndsWith("/"))
                result += "/";

            return result;
        }

        private static void EnsureUserDataRoot ()
        {
            string currentRoot = GetUserDataRootPath();
            string legacyRoot = LEGACY_USER_DATA_PATH;

            bool currentHasData = HasAnyUserData(currentRoot);
            bool legacyHasData = HasAnyUserData(legacyRoot);
            bool settingsDirty = false;

            if (Settings == null)
            {
                if (currentHasData || !legacyHasData) return;
                
                MoveDataSubfolders(legacyRoot, currentRoot);
                TryDeleteFolderIfEmpty(legacyRoot);
                return;
            }

            string lastApplied = Settings.LastAppliedDataRootPath;
            bool hasLastApplied = !string.IsNullOrWhiteSpace(lastApplied);

            if (!hasLastApplied)
            {
                if (!currentHasData && legacyHasData)
                {
                    MoveDataSubfolders(legacyRoot, currentRoot);
                    TryDeleteFolderIfEmpty(legacyRoot);
                }

                Settings.LastAppliedDataRootPath = currentRoot;
                settingsDirty = true;
            }
            else
            {
                string normalizedLast = NormalizeAssetsPath(lastApplied);

                if (normalizedLast != currentRoot)
                {
                    bool lastHasData = HasAnyUserData(normalizedLast);

                    if (lastHasData)
                    {
                        MoveDataSubfolders(normalizedLast, currentRoot);
                        TryDeleteFolderIfEmpty(normalizedLast);
                    }
                    else if (!currentHasData && legacyHasData)
                    {
                        MoveDataSubfolders(legacyRoot, currentRoot);
                        TryDeleteFolderIfEmpty(legacyRoot);
                    }
                }
                else
                {
                    if (!currentHasData && legacyHasData)
                    {
                        MoveDataSubfolders(legacyRoot, currentRoot);
                        TryDeleteFolderIfEmpty(legacyRoot);
                    }
                }

                if (Settings.LastAppliedDataRootPath != currentRoot)
                {
                    Settings.LastAppliedDataRootPath = currentRoot;
                    settingsDirty = true;
                }
            }

            if (settingsDirty) EditorUtility.SetDirty(Settings);
        }

        private static bool HasAnyUserData (string rootPath)
        {
            string normalizedRoot = NormalizeAssetsPath(rootPath);
            if (!AssetDatabase.IsValidFolder(normalizedRoot)) return false;

            foreach (var folder in dataSubfolders)
            {
                string subfolder = normalizedRoot + folder;
                if (!AssetDatabase.IsValidFolder(subfolder)) continue;

                string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { subfolder });
                if (guids != null && guids.Length > 0) return true;
            }

            return false;
        }

        private static void CopyAllDefaultsToUserData ()
        {
            bool refreshAssets = false;

            string userDataRoot = GetUserDataRootPath();

            EnsureFolderHierarchy(userDataRoot.TrimEnd('/'));

            string collectionsPath = userDataRoot + "Collections";
            if (!AssetDatabase.IsValidFolder(collectionsPath))
            {
                EnsureFolderHierarchy(collectionsPath);
                CopyAssetIfNotExists(
                    DEFAULT_DATA_PATH + "Collections/DefaultSoundCollection.asset",
                    userDataRoot + "Collections/SoundCollection.asset");
                CopyAssetIfNotExists(
                    DEFAULT_DATA_PATH + "Collections/DefaultMusicCollection.asset",
                    userDataRoot + "Collections/MusicCollection.asset");
                CopyAssetIfNotExists(
                    DEFAULT_DATA_PATH + "Collections/DefaultOutputCollection.asset",
                    userDataRoot + "Collections/OutputCollection.asset");

                refreshAssets = true;
            }

            string mixersPath = userDataRoot + "Mixers";
            if (!AssetDatabase.IsValidFolder(mixersPath))
            {
                EnsureFolderHierarchy(mixersPath);
                CopyAssetIfNotExists(
                    DEFAULT_DATA_PATH + "Mixers/DefaultMaster.mixer",
                    userDataRoot + "Mixers/Master.mixer");

                refreshAssets = true;
            }

            string generatedPath = userDataRoot + "Generated";
            if (!AssetDatabase.IsValidFolder(generatedPath))
            {
                EnsureFolderHierarchy(generatedPath);

                File.WriteAllText(userDataRoot + "Generated/SFX_Generated.cs", string.Empty);
                File.WriteAllText(userDataRoot + "Generated/Track_Generated.cs", string.Empty);
                File.WriteAllText(userDataRoot + "Generated/Output_Generated.cs", string.Empty);

                CopyAssetIfNotExists(DEFAULT_DATA_PATH + "Generated/SoundsGood.Application.asmref",
                    userDataRoot + "Generated/SoundsGood.Application.asmref");

                enumsInitialized = false;
                enumsCreated = true;

                refreshAssets = true;
            }

            if (!refreshAssets) return;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            PrintResult();
        }

        private static void EnsureEffectAssets ()
        {
            string userDataRoot = GetUserDataRootPath();

            string collectionsFolder = userDataRoot + "Collections";
            string generatedFolder = userDataRoot + "Generated";

            // Only run once the base data folders already exist (created by CopyAllDefaultsToUserData).
            if (!AssetDatabase.IsValidFolder(collectionsFolder) || !AssetDatabase.IsValidFolder(generatedFolder))
                return;

            bool changed = false;

            // Folder where the Effect Creator window stores the effect preset assets.
            EnsureFolderHierarchy((userDataRoot + "Effects").TrimEnd('/'));

            string effectCollectionPath = userDataRoot + "Collections/EffectCollection.asset";
            if (!AssetFileExists(effectCollectionPath))
            {
                // Copy the default preset assets into the user's Effects folder (each gets a fresh
                // GUID on copy), then build a collection referencing those copies — so the user
                // starts with ready-made, editable example effects instead of an empty window.
                var collection = ScriptableObject.CreateInstance<EffectDataCollection>();

                foreach (string effectName in defaultEffects)
                {
                    string destPresetPath = userDataRoot + "Effects/" + effectName + ".asset";
                    CopyAssetIfNotExists(DEFAULT_DATA_PATH + "Effects/" + effectName + ".asset", destPresetPath);

                    var preset = AssetDatabase.LoadAssetAtPath<AudioEffectPreset>(destPresetPath);
                    if (preset != null) collection.CreateEffect(effectName, preset, out _);
                    else Debug.LogError($"[SoundsGood] Failed to load copied effect preset at '{destPresetPath}'.");
                }

                AssetDatabase.CreateAsset(collection, effectCollectionPath);
                changed = true;
            }

            string effectEnumPath = userDataRoot + "Generated/Effect_Generated.cs";
            if (!AssetFileExists(effectEnumPath))
            {
                File.WriteAllText(effectEnumPath, string.Empty);
                enumsInitialized = false;
                enumsCreated = true;
                changed = true;
            }

            if (!changed) return;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            SoundsGoodRuntimeData.ResetCache();
        }

        private static void EnsureOcclusionMaterialAssets ()
        {
            string userDataRoot = GetUserDataRootPath();

            string collectionsFolder = userDataRoot + "Collections";
            string generatedFolder = userDataRoot + "Generated";

            // Only run once the base data folders already exist (created by CopyAllDefaultsToUserData).
            if (!AssetDatabase.IsValidFolder(collectionsFolder) || !AssetDatabase.IsValidFolder(generatedFolder))
                return;

            bool changed = false;

            // Folder where the Occlusion Material Creator window stores the material assets.
            EnsureFolderHierarchy((userDataRoot + "OcclusionMaterials").TrimEnd('/'));

            string collectionPath = userDataRoot + "Collections/OcclusionMaterialCollection.asset";
            if (!AssetFileExists(collectionPath))
            {
                // Copy the default material assets into the user's folder (fresh GUIDs on copy), then
                // build a collection referencing those copies — so the user starts with ready-made,
                // editable example materials instead of an empty window.
                var collection = ScriptableObject.CreateInstance<OcclusionMaterialDataCollection>();

                foreach (string materialName in defaultOcclusionMaterials)
                {
                    string destPath = userDataRoot + "OcclusionMaterials/" + materialName + ".asset";
                    CopyAssetIfNotExists(DEFAULT_DATA_PATH + "OcclusionMaterials/" + materialName + ".asset", destPath);

                    var material = AssetDatabase.LoadAssetAtPath<AudioOcclusionMaterial>(destPath);
                    if (material != null) collection.CreateMaterial(materialName, material, out _);
                    else Debug.LogError($"[SoundsGood] Failed to load copied occlusion material at '{destPath}'.");
                }

                AssetDatabase.CreateAsset(collection, collectionPath);
                changed = true;
            }

            string enumPath = userDataRoot + "Generated/OcclusionMaterial_Generated.cs";
            if (!AssetFileExists(enumPath))
            {
                File.WriteAllText(enumPath, string.Empty);
                enumsInitialized = false;
                enumsCreated = true;
                changed = true;
            }

            if (!changed) return;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            SoundsGoodRuntimeData.ResetCache();
        }

        private static bool AssetFileExists (string assetPath)
        {
            return File.Exists(Path.Combine(Application.dataPath, "../", assetPath));
        }

        private static void CopyAssetIfNotExists (string sourcePath, string destPath)
        {
            string fullDestPath = Path.Combine(Application.dataPath, "../", destPath);
            if (File.Exists(fullDestPath)) return;

            if (AssetDatabase.CopyAsset(sourcePath, destPath))
            {
                Debug.Log($"[SoundsGood] Copied: {destPath}");
                return;
            }

            Debug.LogError($"[SoundsGood] Failed to copy {sourcePath} to {destPath}");
        }

        private static void EnsureFolderHierarchy (string assetFolderPath)
        {
            if (string.IsNullOrWhiteSpace(assetFolderPath))
                return;

            string formatted = assetFolderPath.Trim().Replace("\\", "/");
            string[] segments = formatted.Split('/');

            if (segments.Length == 0 || segments[0] != "Assets")
            {
                Debug.LogError(
                    $"[SoundsGood] EnsureFolderHierarchy only supports paths under 'Assets/': {assetFolderPath}");
                return;
            }

            string currentPath = "Assets";
            for (int i = 1; i < segments.Length; i++)
            {
                if (string.IsNullOrEmpty(segments[i]))
                    continue;

                string nextPath = currentPath + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                    AssetDatabase.CreateFolder(currentPath, segments[i]);

                currentPath = nextPath;
            }
        }

        private static void MoveDataSubfolders (string sourceRoot, string destinationRoot)
        {
            string srcRoot = NormalizeAssetsPath(sourceRoot);
            string dstRoot = NormalizeAssetsPath(destinationRoot);

            foreach (var sub in dataSubfolders)
            {
                string sourceFolder = srcRoot + sub;
                if (!AssetDatabase.IsValidFolder(sourceFolder)) continue;

                string destFolder = dstRoot + sub;
                EnsureFolderHierarchy(destFolder);

                string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { sourceFolder });
                foreach (var guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    if (AssetDatabase.IsValidFolder(assetPath)) continue;

                    string fileName = Path.GetFileName(assetPath);
                    string newPath = destFolder.TrimEnd('/') + "/" + fileName;

                    if (assetPath == newPath) continue;

                    string fullDestPath = Path.Combine(Application.dataPath, "../", newPath);
                    if (File.Exists(fullDestPath)) continue;

                    string error = AssetDatabase.MoveAsset(assetPath, newPath);
                    if (!string.IsNullOrEmpty(error))
                        Debug.LogError($"[SoundsGood] Error moving asset from {assetPath} to {newPath}: {error}");
                }

                TryDeleteFolderIfEmpty(sourceFolder);
            }
            
            enumsInitialized = false;
            enumsCreated = true;
            PrintResult();
        }

        private static void TryDeleteFolderIfEmpty (string folderPath)
        {
            string normalized = NormalizeAssetsPath(folderPath);
            if (!AssetDatabase.IsValidFolder(normalized))
                return;

            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { normalized });
            if (guids != null && guids.Length > 0)
                return;

            AssetDatabase.DeleteAsset(normalized);
        }

        private static void WriteEnumsContent ()
        {
            if (AssetLocator.Instance == null ||
                AssetLocator.Instance.SoundDataCollection == null ||
                AssetLocator.Instance.MusicDataCollection == null ||
                AssetLocator.Instance.OutputDataCollection == null ||
                AssetLocator.Instance.SfxEnum == null ||
                AssetLocator.Instance.TracksEnum == null ||
                AssetLocator.Instance.OutputsEnum == null)
            {
                return;
            }

            EditorHelper.SaveCollectionChanges(Sections.Sounds, false);
            EditorHelper.SaveCollectionChanges(Sections.Music, false);
            EditorHelper.ReloadOutputsDatabase(false);
            EditorHelper.SaveEffectCollectionChanges(false); // no-ops if the effect data isn't ready yet
            EditorHelper.SaveOcclusionMaterialCollectionChanges(false); // no-ops if the material data isn't ready yet

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            enumsInitialized = true;
        }

        private static void PrintResult ()
        {
            string result = $"[Sounds Good] Data moved or created in {AssetLocator.SoundsGoodSettings.DataRootPath}\n" +
                            $"Content: {AssetLocator.Instance.SoundDataCollection}, {AssetLocator.Instance.MusicDataCollection}, " +
                            $"{AssetLocator.Instance.OutputDataCollection}, {AssetLocator.Instance.SfxEnum}, " +
                            $"{AssetLocator.Instance.TracksEnum}, {AssetLocator.Instance.OutputsEnum}, " +
                            $"{AssetLocator.Instance.MasterAudioMixer}";
            Debug.Log(result);
        }
    }
}