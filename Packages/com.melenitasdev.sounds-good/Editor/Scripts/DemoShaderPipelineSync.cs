/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
// Aliased because UnityEditor still carries a legacy PackageInfo of its own, and an unqualified
// reference is ambiguous between the two.
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace MelenitasDev.SoundsGood.Editor
{
    /// <summary>
    /// Keeps the demo shaders working on whichever render pipeline the project uses.
    /// <para>
    /// A shader file can't simply carry both pipelines: Unity compiles every SubShader when it
    /// imports the file, regardless of the active pipeline, so a URP SubShader in a project without
    /// URP fails to open URP's ShaderLibrary and the shader errors out. The URP half is therefore
    /// kept beside each shader as a plain .txt (which the shader compiler never sees) and pasted
    /// into the shader only once the URP package is actually installed — and taken back out if it
    /// is later removed.
    /// </para>
    /// The shader assets keep their GUIDs throughout, so materials never need reassigning.
    /// </summary>
    internal static class DemoShaderPipelineSync
    {
        private const string URP_PACKAGE = "com.unity.render-pipelines.universal";
        private const string BEGIN_MARKER = "//#SG_URP_BEGIN";
        private const string END_MARKER = "//#SG_URP_END";

        // Every demo shader that has a URP counterpart, by its Shader.Find name. Looking them up by
        // name rather than by path keeps this working whether the sample sits in the package or was
        // imported into Assets/Samples/.
        private static readonly string[] shaderNames =
        {
            "Melenitas Dev/SG_Standard",
            "Melenitas Dev/SG_Triplanar",
            "Melenitas Dev/SG_ScrollOffset",
            "Melenitas Dev/SG_MusicZoneVisualizer",
            "Melenitas Dev/SG_InteractableOutline",
            "Melenitas Dev/SG_ParticleUnlit"
        };

        [InitializeOnLoadMethod]
        private static void OnLoad ()
        {
            // Installing or removing a package triggers a domain reload, so this covers the moment
            // the user switches pipelines without needing to watch the Package Manager.
            EditorApplication.delayCall += Sync;
        }

        /// <summary>
        /// Brings every demo shader in line with whether URP is installed. Shaders already in the
        /// right state are left alone, so this is cheap enough to run on every domain reload — which
        /// is also the only time it needs to run, since installing or removing a package causes one.
        /// </summary>
        private static void Sync ()
        {
            bool wantUrp = IsPackageInstalled(URP_PACKAGE);
            int changed = 0;

            foreach (string shaderName in shaderNames)
            {
                if (SyncShader(shaderName, wantUrp)) changed++;
            }

            if (changed == 0) return;

            AssetDatabase.Refresh();
            Debug.Log($"[Sounds Good] {changed} demo shader(s) rebuilt for " +
                      $"{(wantUrp ? "Universal Render Pipeline" : "the Built-In Render Pipeline")}.");
        }

        /// <summary> True when the shader had to be rewritten. </summary>
        private static bool SyncShader (string shaderName, bool wantUrp)
        {
            Shader shader = Shader.Find(shaderName);
            // Not an error: the demo sample simply isn't imported in this project.
            if (shader == null) return false;

            string assetPath = AssetDatabase.GetAssetPath(shader);
            if (string.IsNullOrEmpty(assetPath)) return false;

            string source;
            string fullPath;
            try
            {
                // Resolves a virtual Packages/... path to a real one on disk.
                fullPath = Path.GetFullPath(assetPath);
                source = File.ReadAllText(fullPath);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Sounds Good] Couldn't read '{assetPath}' to set it up for the " +
                                 $"current render pipeline: {e.Message}");
                return false;
            }

            int begin = source.IndexOf(BEGIN_MARKER, System.StringComparison.Ordinal);
            int end = source.IndexOf(END_MARKER, System.StringComparison.Ordinal);
            if (begin < 0 || end < begin)
            {
                Debug.LogWarning($"[Sounds Good] '{assetPath}' has no URP markers, so it can't be " +
                                 "switched between render pipelines. Reimport the demo sample to restore it.");
                return false;
            }

            string body = wantUrp ? ReadUrpSource(assetPath) : "";
            if (wantUrp && body == null) return false;

            // Everything between the two markers is ours to rewrite; the rest of the file is left
            // exactly as it is, so any edit the user made to the Built-In half survives.
            int bodyStart = begin + BEGIN_MARKER.Length;
            string current = source.Substring(bodyStart, end - bodyStart);
            string replacement = body.Length == 0 ? "\n    " : $"\n{body}    ";
            if (current == replacement) return false;

            try
            {
                File.WriteAllText(fullPath, source.Substring(0, bodyStart) + replacement + source.Substring(end));
            }
            // A read-only install (e.g. installed as a package from a registry/tarball, cached
            // under Library/PackageCache) throws UnauthorizedAccessException here, which does NOT
            // derive from IOException — an uncaught one used to abort this whole loop partway
            // through shaderNames, leaving every shader after the first one un-synced.
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Sounds Good] Couldn't write '{assetPath}'. If the package is " +
                                 $"installed read-only, import the demo sample into your project " +
                                 $"first so the shaders can be edited. ({e.Message})");
                return false;
            }

            return true;
        }

        /// <summary> The URP SubShader kept next to the shader, or null if it's missing. </summary>
        private static string ReadUrpSource (string shaderAssetPath)
        {
            string directory = Path.GetDirectoryName(shaderAssetPath)?.Replace('\\', '/');
            string name = Path.GetFileNameWithoutExtension(shaderAssetPath);
            string sourcePath = $"{directory}/URP/{name}.urp.txt";

            // Loaded through the AssetDatabase rather than File.IO so it works the same whether the
            // sample lives in a package or in Assets/.
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(sourcePath);
            if (asset != null) return asset.text;

            Debug.LogWarning($"[Sounds Good] Missing '{sourcePath}', so '{name}' can't be set up for " +
                             "URP. Reimport the demo sample to restore it.");
            return null;
        }

        private static bool IsPackageInstalled (string packageName)
        {
            foreach (PackageInfo package in PackageInfo.GetAllRegisteredPackages())
            {
                if (package.name == packageName) return true;
            }

            return false;
        }
    }
}
#endif
