#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
    [CustomEditor(typeof(MusicZone))]
    [CanEditMultipleObjects]
    public class MusicZoneEditor : UnityEditor.Editor
    {
        private const string Description =
            "Fades the Audio Emitter on this object in and out based on the listener's position " +
            "inside a spherical or box zone.";

        private const string MissingEmitter =
            "Add an Audio Emitter (Music / Playlist / Dynamic Music) to this GameObject. " +
            "The zone fades it by the listener's distance.";

        private bool HasEmitter => ((MusicZone)target).GetComponent<AudioEmitter>() != null;

        // =====================================================================
        //  UI Toolkit (primary) — matches the Sounds Good editor windows.
        // =====================================================================
        public override VisualElement CreateInspectorGUI ()
            => SG_InspectorElements.BuildInspector(this, BuildBody);

        private void BuildBody (VisualElement root)
        {
            root.Add(SG_InspectorElements.Description(Description));

            if (!HasEmitter)
            {
                var warn = new Label(MissingEmitter);
                warn.AddToClassList("info-box");
                warn.style.marginBottom = 4;
                root.Add(warn);
            }

            var shapeSection = SG_InspectorElements.FoldoutSection("Shape", out var box);
            SG_InspectorElements.ZoneShapeBody(serializedObject, box);
            root.Add(shapeSection);
        }

        // =====================================================================
        //  IMGUI (fallback) — used when the inspector is forced into IMGUI.
        // =====================================================================
        public override void OnInspectorGUI ()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(Description, MessageType.Info);
            if (!HasEmitter) EditorGUILayout.HelpBox(MissingEmitter, MessageType.Warning);

            SG_InspectorElements.ZoneShapeSectionIMGUI(serializedObject);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
