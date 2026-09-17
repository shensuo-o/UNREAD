/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MelenitasDev.SoundsGood.Editor
{
    /// <summary>
    /// Removes files left behind by older versions of Sounds Good.
    /// <para>
    /// Importing a .unitypackage only ever adds and overwrites — it never deletes. So when a file is
    /// renamed between versions, the old one stays in the project forever. That matters here because
    /// the renames kept their GUIDs (which is what lets existing scenes and prefabs carry over): the
    /// old file and the new one end up claiming the same GUID, Unity resolves the clash in favour of
    /// whichever it saw first, and the project keeps using the version that was replaced. A Music
    /// Zone still showing its pre-2.2 Inspector is this, not a broken component.
    /// </para>
    /// Deleting the superseded file resolves the GUID to the new one, so everything referencing it
    /// migrates on its own with nothing else to do.
    /// </summary>
    internal static class ObsoleteAssetCleaner
    {
        // ----- Types
        private readonly struct ObsoleteAsset
        {
            internal readonly string RelativePath;
            internal readonly string Guid;
            internal readonly string ReplacedBy;

            internal ObsoleteAsset (string relativePath, string guid, string replacedBy)
            {
                RelativePath = relativePath;
                Guid = guid;
                ReplacedBy = replacedBy;
            }
        }

        // ----- Constants
        // Superseded in 2.2.0, when the runtime components dropped the SG_ prefix. The GUID is
        // recorded so a file is only ever deleted when it really is the one that shipped back then,
        // and never because it happens to share a name with something the user made.
        private static readonly ObsoleteAsset[] obsoleteAssets =
        {
            new ObsoleteAsset("Runtime/Scripts/Application/SG_MusicZone.cs",
                "486ca8a7ffa55274ab8bed64516bc859", "MusicZone.cs"),
            new ObsoleteAsset("Runtime/Scripts/Application/SG_GenericSlider.cs",
                "87350a6f9b6fc4147bed127049763b55", "GenericSlider.cs"),
            new ObsoleteAsset("Runtime/Scripts/Application/SG_OutputVolumeHandler.cs",
                "6d76f784126441f48919b93c99d374b5", "OutputVolumeSlider.cs"),
            new ObsoleteAsset("Runtime/Scripts/Application/OutputVolumeHandler.cs",
                "fd16dca35612aba4b89c590543a15fd1", "OutputVolumeSlider.cs"),
            new ObsoleteAsset("Runtime/Prefabs/SG_Music Zone.prefab",
                "df6fa8b736c31b847813f76bc399fae1", "Music Zone.prefab"),
            new ObsoleteAsset("Runtime/Prefabs/UI/SG_GenericSlider.prefab",
                "bf47a78844e4c884686d4b2f05eb394f", "Generic Slider.prefab"),
            new ObsoleteAsset("Runtime/Prefabs/UI/SG_Output Volume Slider.prefab",
                "a47fc6d47cf498f438b233a395178f85", "Output Volume Slider.prefab"),
//#SG_PRO_BEGIN
            // Left behind when someone upgrades from the Lite edition. These two only exist there,
            // to stand in for the paid windows at the same menu paths, so importing the full version
            // does not overwrite them - it only ever adds. Left in place they keep winning the menu
            // entry, and clicking Open 3D Demo Scene opens the upsell instead of the scene.
            new ObsoleteAsset("Editor/Scripts/UIToolkit/ProUpgradeWindow.cs",
                "4a563be278f10a640852baea032284dd", "the full version's own windows"),
            new ObsoleteAsset("Editor/UI/Uxml/ProUpgrade.uxml",
                "17ce6daee0310dee51737646b1a98fe3", "the full version's own windows"),
//#SG_PRO_END
        };

        private const string PACKAGE_FOLDER = "com.melenitasdev.sounds-good";

        // Session-scoped, so declining doesn't ask again on every script reload, but restarting the
        // editor offers it once more rather than burying the problem forever.
        private const string DECLINED_KEY = "SoundsGood.ObsoleteAssetCleaner.Declined";

        // ----- Unity Events
        [InitializeOnLoadMethod]
        private static void OnInitialize ()
        {
            EditorApplication.delayCall += Run;
        }

        // ----- Private Methods
        private static void Run ()
        {
            if (SessionState.GetBool(DECLINED_KEY, false)) return;

            string root = FindPackageRoot();
            if (root == null) return;

            List<ObsoleteAsset> found = Collect(root);

            // The overwhelmingly common case: a clean install, or one that has already been tidied.
            // Nothing is shown, so the vast majority of projects never learn this exists.
            if (found.Count == 0) return;

            string list = string.Empty;
            foreach (ObsoleteAsset asset in found)
            {
                list += $"\n• {Path.GetFileName(asset.RelativePath)}  →  superseded by {asset.ReplacedBy}";
            }

            bool remove = EditorUtility.DisplayDialog("Sounds Good — files from a previous install",
                $"{found.Count} file(s) left over from a previous install were found. Importing a " +
                "package only ever adds and overwrites — it never deletes — so a file that is no " +
                "longer part of Sounds Good stays in the project and keeps being used instead of " +
                "the one that replaced it. That is why a component can still show its old Inspector, " +
                "or a menu entry can open the wrong window.\n" +
                $"{list}\n\n" +
                "Removing them makes everything in your scenes and prefabs point at the current " +
                "version. Nothing you made is touched.",
                "Remove them", "Keep for now");

            if (!remove)
            {
                SessionState.SetBool(DECLINED_KEY, true);
                return;
            }

            int deleted = 0;
            foreach (ObsoleteAsset asset in found)
            {
                if (Delete(Path.Combine(root, asset.RelativePath))) deleted++;
            }

            if (deleted > 0) AssetDatabase.Refresh();

            Debug.Log($"[Sounds Good] Removed {deleted} file(s) left over from an older version. " +
                      "Components that were still using them now resolve to the current ones.");
        }

        /// <summary> The obsolete files actually present, matched by GUID as well as by path. </summary>
        private static List<ObsoleteAsset> Collect (string root)
        {
            var found = new List<ObsoleteAsset>();

            foreach (ObsoleteAsset asset in obsoleteAssets)
            {
                string fullPath = Path.Combine(root, asset.RelativePath);
                if (!File.Exists(fullPath)) continue;

                // The GUID is read straight from the .meta on disk: with two files claiming the same
                // GUID, the AssetDatabase can't be trusted to say which one is which.
                if (ReadGuid(fullPath + ".meta") != asset.Guid) continue;

                found.Add(asset);
            }

            return found;
        }

        private static string ReadGuid (string metaPath)
        {
            if (!File.Exists(metaPath)) return null;

            try
            {
                foreach (string line in File.ReadLines(metaPath))
                {
                    if (!line.StartsWith("guid:", StringComparison.Ordinal)) continue;
                    return line.Substring("guid:".Length).Trim();
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                return null;
            }

            return null;
        }

        private static bool Delete (string fullPath)
        {
            // Deleted through the file system rather than AssetDatabase.DeleteAsset: while two files
            // share a GUID, the AssetDatabase may not have the superseded one registered at all.
            try
            {
                File.Delete(fullPath);
                if (File.Exists(fullPath + ".meta")) File.Delete(fullPath + ".meta");
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Sounds Good] Couldn't remove '{fullPath}'. If Sounds Good is " +
                                 $"installed read-only, delete it by hand. ({e.Message})");
                return false;
            }
        }

        /// <summary> Where Sounds Good lives in this project, or null if it can't be located. </summary>
        private static string FindPackageRoot ()
        {
            foreach (string candidate in new[] { "Packages", "Assets" })
            {
                if (!Directory.Exists(candidate)) continue;

                foreach (string directory in Directory.GetDirectories(candidate, PACKAGE_FOLDER,
                             SearchOption.AllDirectories))
                {
                    return directory;
                }
            }

            return null;
        }
    }
}
#endif
