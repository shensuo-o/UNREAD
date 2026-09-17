#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
    [CustomEditor(typeof(AudioOcclusionSurface))]
    [CanEditMultipleObjects]
    public class AudioOcclusionSurfaceEditor : UnityEditor.Editor
    {
        private const string DESCRIPTION =
            "Sets how much this object's collider(s) block sound. Pick a reusable Material or a Custom " +
            "density (0 = sound passes through, 1 = full block). Colliders without this component block fully.";

        // =====================================================================
        //  UI Toolkit (primary) — matches the Sounds Good editor windows.
        // =====================================================================
        public override VisualElement CreateInspectorGUI ()
            => SG_InspectorElements.BuildInspector(this, BuildBody);

        private void BuildBody (VisualElement root)
        {
            root.Add(SG_InspectorElements.Description(DESCRIPTION));

            var wrapper = SG_InspectorElements.Section("Occlusion", out var box);

            var sourceField = SG_InspectorElements.EnumPopup<AudioOcclusionSurface.DensitySource>(
                serializedObject, "source", "Source");
            box.Add(sourceField);

            var materialField = new PropertyField(serializedObject.FindProperty("material"), "Material");
            box.Add(materialField);

            var densitySlider = SG_InspectorElements.Slider01(serializedObject, "customDensity", "Density", 0.5f);
            box.Add(densitySlider);

            box.Add(SG_InspectorElements.Bool(serializedObject, "includeChildren", "Include Children"));

            void UpdateMode ()
            {
                var src = (AudioOcclusionSurface.DensitySource)serializedObject.FindProperty("source").enumValueIndex;
                materialField.style.display = src == AudioOcclusionSurface.DensitySource.Material
                    ? DisplayStyle.Flex : DisplayStyle.None;
                densitySlider.style.display = src == AudioOcclusionSurface.DensitySource.Custom
                    ? DisplayStyle.Flex : DisplayStyle.None;
            }

            UpdateMode();
            sourceField.RegisterValueChangedCallback(_ => UpdateMode());

            root.Add(wrapper);
        }

        // =====================================================================
        //  IMGUI (fallback) — used when the inspector is forced into IMGUI.
        // =====================================================================
        public override void OnInspectorGUI ()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(DESCRIPTION, MessageType.Info);

            EditorGUILayout.LabelField("Occlusion", EditorStyles.boldLabel);

            var sourceProp = serializedObject.FindProperty("source");
            EditorGUILayout.PropertyField(sourceProp, new GUIContent("Source"));

            var src = (AudioOcclusionSurface.DensitySource)sourceProp.enumValueIndex;
            if (src == AudioOcclusionSurface.DensitySource.Material)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("material"), new GUIContent("Material"));
            else
                EditorGUILayout.PropertyField(serializedObject.FindProperty("customDensity"), new GUIContent("Density"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("includeChildren"), new GUIContent("Include Children"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
