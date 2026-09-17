#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
    [CustomEditor(typeof(AudioEffectZone))]
    [CanEditMultipleObjects]
    public class AudioEffectZoneEditor : UnityEditor.Editor
    {
        private const string DESCRIPTION =
            "Applies an audio effect to every sound playing inside a spherical or box zone " +
            "(underwater, a cave, a tunnel...). The effect fades in gradually across the fade area.";

        // =====================================================================
        //  UI Toolkit (primary) — matches the Sounds Good editor windows.
        // =====================================================================
        public override VisualElement CreateInspectorGUI ()
            => SG_InspectorElements.BuildInspector(this, BuildBody);

        private void BuildBody (VisualElement root)
        {
            root.Add(SG_InspectorElements.Description(DESCRIPTION));

            root.Add(BuildEffectSection());

            var shapeSection = SG_InspectorElements.Section("Shape", out var box);
            SG_InspectorElements.ZoneShapeBody(serializedObject, box);
            root.Add(shapeSection);
        }

        private VisualElement BuildEffectSection ()
        {
            var wrapper = SG_InspectorElements.Section("Effect", out var box);

            box.Add(new PropertyField(serializedObject.FindProperty("effect"), "Effect"));

            var affectProp = serializedObject.FindProperty("affect");
            var affectField = new EnumField("Affect", (AudioEffectZone.Affect)affectProp.enumValueIndex)
            {
                tooltip = affectProp.tooltip
            };
            affectField.showMixedValue = affectProp.hasMultipleDifferentValues;
            box.Add(affectField);

            var info = new Label();
            info.AddToClassList("info-box");
            box.Add(info);

            void UpdateInfo ()
            {
                info.text = (AudioEffectZone.Affect)affectProp.enumValueIndex switch
                {
                    AudioEffectZone.Affect.Sources =>
                        "Only sounds inside the zone get the effect (heard with it from anywhere).",
                    AudioEffectZone.Affect.Listener =>
                        "Every Sounds Good sound gets the effect while the listener is inside.",
                    _ =>
                        "Sounds inside get the effect, and so does the whole mix while the listener is inside."
                };
            }
            UpdateInfo();
            affectField.RegisterValueChangedCallback(e =>
            {
                affectProp.enumValueIndex = (int)(AudioEffectZone.Affect)e.newValue;
                serializedObject.ApplyModifiedProperties();
                UpdateInfo();
            });

            return wrapper;
        }

        // =====================================================================
        //  IMGUI (fallback) — used when the inspector is forced into IMGUI.
        // =====================================================================
        public override void OnInspectorGUI ()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(DESCRIPTION, MessageType.Info);

            EditorGUILayout.LabelField("Effect", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("effect"), new GUIContent("Effect"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("affect"), new GUIContent("Affect"));

            EditorGUILayout.Space();
            SG_InspectorElements.ZoneShapeSectionIMGUI(serializedObject);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
