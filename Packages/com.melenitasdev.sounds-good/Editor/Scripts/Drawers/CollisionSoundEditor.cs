#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
    [CustomEditor(typeof(CollisionSound))]
    [CanEditMultipleObjects]
    public class CollisionSoundEditor : UnityEditor.Editor
    {
        private const string Description =
            "Plays the Sound Emitter on this object on physical collisions, scaling its volume " +
            "with impact strength. All audio settings live on the emitter — this only decides " +
            "when it fires and how loud, based on the collision. Needs a Collider + Rigidbody.";

        private const string MissingEmitter =
            "Add a Sound Emitter to this GameObject. Collision Sound plays it on impact.";

        private bool HasEmitter => ((CollisionSound)target).GetComponent<SoundEmitter>() != null;

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

            root.Add(BuildImpactSection());
            root.Add(BuildFilterSection());
        }

        private VisualElement BuildImpactSection ()
        {
            var wrapper = SG_InspectorElements.FoldoutSection("Impact", out var box);
            box.Add(SG_InspectorElements.Float(serializedObject, "minImpact", "Min Impact", "m/s", 0f, 1f));
            box.Add(SG_InspectorElements.Float(serializedObject, "maxImpact", "Max Impact", "m/s", 0f, 10f));
            box.Add(SG_InspectorElements.Slider01(serializedObject, "minImpactVolumeScale", "Min Impact Volume", 0.3f));
            box.Add(SG_InspectorElements.Float(serializedObject, "cooldown", "Cooldown", "s", 0f, 0.05f));
            return wrapper;
        }

        private VisualElement BuildFilterSection ()
        {
            var wrapper = SG_InspectorElements.FoldoutSection("Filter", out var box);
            box.Add(SG_InspectorElements.LayerMask(serializedObject, "filterLayers", "Layers"));

            var useTag = SG_InspectorElements.Bool(serializedObject, "useTagFilter", "Use Tag Filter");
            box.Add(useTag);

            var tagProp = serializedObject.FindProperty("requiredTag");
            var tagField = new TagField("Required Tag",
                string.IsNullOrEmpty(tagProp.stringValue) ? "Untagged" : tagProp.stringValue);
            tagField.RegisterValueChangedCallback(e =>
            {
                tagProp.stringValue = e.newValue;
                serializedObject.ApplyModifiedProperties();
            });
            box.Add(tagField);
            void UpdateTag (bool on) => tagField.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            UpdateTag(serializedObject.FindProperty("useTagFilter").boolValue);
            useTag.OnValueChanged += UpdateTag;

            return wrapper;
        }

        // =====================================================================
        //  IMGUI (fallback) — used when the inspector is forced into IMGUI.
        // =====================================================================
        public override void OnInspectorGUI ()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(Description, MessageType.Info);
            if (!HasEmitter) EditorGUILayout.HelpBox(MissingEmitter, MessageType.Warning);

            EditorGUILayout.LabelField("Impact", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minImpact"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxImpact"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minImpactVolumeScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cooldown"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Filter", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("filterLayers"), new GUIContent("Layers"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useTagFilter"));
            if (serializedObject.FindProperty("useTagFilter").boolValue)
            {
                var tagProp = serializedObject.FindProperty("requiredTag");
                tagProp.stringValue = EditorGUILayout.TagField("Required Tag",
                    string.IsNullOrEmpty(tagProp.stringValue) ? "Untagged" : tagProp.stringValue);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
