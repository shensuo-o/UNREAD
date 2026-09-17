/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MelenitasDev.SoundsGood.Editor
{
    /// <summary>
    /// Material inspector for the demo's SG_Standard shader.
    /// <para>
    /// Transparency in that shader is plain render state — source blend, destination blend, depth
    /// write and the render queue — which works but means four separate things to get right before
    /// a material blends at all. This turns them into one Surface Type dropdown and writes all four
    /// itself, the way URP's own Lit inspector does.
    /// </para>
    /// </summary>
    public class SG_StandardShaderGUI : ShaderGUI
    {
        // ----- Types
        private enum SurfaceType { Opaque = 0, Transparent = 1 }

        // ----- Public Methods
        public override void OnGUI (MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var surface = FindProperty("_Surface", properties, false);

            // Nothing to drive (an older material, or the property was removed): fall back to the
            // stock inspector rather than drawing a dropdown that controls nothing.
            if (surface == null)
            {
                base.OnGUI(materialEditor, properties);
                return;
            }

            foreach (Material material in materialEditor.targets)
            {
                ReconcileSurfaceType(material);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = surface.hasMixedValue;

            var current = (SurfaceType)surface.floatValue;
            var selected = (SurfaceType)EditorGUILayout.EnumPopup("Surface Type", current);

            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo("Surface Type");
                surface.floatValue = (float)selected;

                foreach (Material material in materialEditor.targets)
                {
                    ApplySurfaceType(material, selected);
                }
            }

            EditorGUILayout.Space();

            // Everything else draws as usual. The three render-state properties are marked
            // HideInInspector in the shader, so they don't show up twice.
            base.OnGUI(materialEditor, properties);
        }

        /// <summary>
        /// Applied when a material arrives from elsewhere — converted from Unity's Standard shader,
        /// or set up by hand before this dropdown existed — where the blend state already says
        /// transparent but Surface Type is still sitting at its Opaque default. Reads the state and
        /// makes the dropdown agree with it, instead of the dropdown silently lying.
        /// </summary>
        private static void ReconcileSurfaceType (Material material)
        {
            if (!material.HasProperty("_Surface") || !material.HasProperty("_DstBlend")) return;

            bool blendsAsTransparent = !Mathf.Approximately(material.GetFloat("_DstBlend"), (float)BlendMode.Zero);
            bool markedTransparent = !Mathf.Approximately(material.GetFloat("_Surface"), (float)SurfaceType.Opaque);

            if (blendsAsTransparent == markedTransparent) return;

            material.SetFloat("_Surface", blendsAsTransparent ? (float)SurfaceType.Transparent
                                                             : (float)SurfaceType.Opaque);
        }

        private static void ApplySurfaceType (Material material, SurfaceType surfaceType)
        {
            bool transparent = surfaceType == SurfaceType.Transparent;

            material.SetFloat("_SrcBlend", (float)(transparent ? BlendMode.SrcAlpha : BlendMode.One));
            material.SetFloat("_DstBlend", (float)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
            material.SetFloat("_ZWrite", transparent ? 0f : 1f);

            // RenderType is what post-processing and replacement shaders read to tell the two apart.
            material.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque");

            // The queue is what actually decides draw order, and it's the piece people miss: without
            // it a blended object still renders in the opaque pass, before the things behind it.
            // -1 hands the queue back to the shader.
            material.renderQueue = transparent ? (int)RenderQueue.Transparent : -1;
        }
    }
}
#endif
