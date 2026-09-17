#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
    [CustomEditor(typeof(AudioTrigger))]
    [CanEditMultipleObjects]
    public class AudioTriggerEditor : UnityEditor.Editor
    {
        private static readonly Color Orange = new Color(0.992f, 0.694f, 0.012f);

        private const string Description =
            "Drives the Audio Emitter on this object from physics, lifecycle or manual events. " +
            "All audio settings live on the emitter — this only decides when to Play/Stop it.";

        private const string MissingEmitter =
            "Add an Audio Emitter (Sound / Music / Playlist / Dynamic Music) to this GameObject. " +
            "The trigger plays through it.";

        private bool HasEmitter => ((AudioTrigger)target).GetComponent<AudioEmitter>() != null;

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

            root.Add(BuildTriggersSection());
            root.Add(BuildTestButtons());
        }

        private VisualElement BuildTriggersSection ()
        {
            var wrapper = SG_InspectorElements.FoldoutSection("Triggers", out var box);

            box.Add(MiniHeader("Lifecycle"));
            box.Add(TriggerRow("On Enable", "triggerOnEnable", "actionOnEnable", null));
            box.Add(TriggerRow("On Start", "triggerOnStart", "actionOnStart", null));
            box.Add(TriggerRow("On Destroy", "triggerOnDestroy", "actionOnDestroy", null));

            box.Add(MiniHeader("Physics"));

            var physicsFilter = new VisualElement();
            SG_InspectorElements.ApplyBox(physicsFilter);
            physicsFilter.style.marginTop = 4;
            physicsFilter.Add(MiniHeader("Physics Filter"));
            physicsFilter.Add(SG_InspectorElements.LayerMask(serializedObject, "filterLayers", "Layers"));

            var useTag = SG_InspectorElements.Bool(serializedObject, "useTagFilter", "Use Tag Filter");
            physicsFilter.Add(useTag);

            var tagProp = serializedObject.FindProperty("requiredTag");
            var tagField = new TagField("Required Tag",
                string.IsNullOrEmpty(tagProp.stringValue) ? "Untagged" : tagProp.stringValue);
            tagField.RegisterValueChangedCallback(e =>
            {
                tagProp.stringValue = e.newValue;
                serializedObject.ApplyModifiedProperties();
            });
            physicsFilter.Add(tagField);
            void UpdateTag (bool on) => tagField.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            UpdateTag(Bool("useTagFilter"));
            useTag.OnValueChanged += UpdateTag;

            void UpdatePhysicsFilter ()
            {
                bool any = Bool("triggerOnCollisionEnter") || Bool("triggerOnCollisionExit") ||
                           Bool("triggerOnTriggerEnter") || Bool("triggerOnTriggerExit");
                physicsFilter.style.display = any ? DisplayStyle.Flex : DisplayStyle.None;
            }

            box.Add(TriggerRow("On Collision Enter", "triggerOnCollisionEnter", "actionOnCollisionEnter", UpdatePhysicsFilter));
            box.Add(TriggerRow("On Collision Exit", "triggerOnCollisionExit", "actionOnCollisionExit", UpdatePhysicsFilter));
            box.Add(TriggerRow("On Trigger Enter", "triggerOnTriggerEnter", "actionOnTriggerEnter", UpdatePhysicsFilter));
            box.Add(TriggerRow("On Trigger Exit", "triggerOnTriggerExit", "actionOnTriggerExit", UpdatePhysicsFilter));
            box.Add(physicsFilter);
            UpdatePhysicsFilter();

            box.Add(MiniHeader("Repeat Control"));
            box.Add(SG_InspectorElements.Float(serializedObject, "cooldown", "Cooldown", "s", float.NegativeInfinity, 0f));
            box.Add(SG_InspectorElements.Bool(serializedObject, "playOnce", "Play Once"));

            return wrapper;
        }

        private VisualElement BuildTestButtons ()
        {
            var container = new VisualElement { style = { marginTop = 4 } };

            if (Application.isPlaying)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

                var play = new Button(() => ((AudioTrigger)target).Play()) { text = "▶ Play" };
                play.AddToClassList("create-button");
                play.style.flexGrow = 1;
                play.style.height = 26;

                var stop = new Button(() => ((AudioTrigger)target).Stop()) { text = "■ Stop" };
                stop.AddToClassList("light-orange-button");
                stop.style.flexGrow = 1;
                stop.style.height = 26;
                stop.style.marginLeft = 4;

                row.Add(play);
                row.Add(stop);
                container.Add(row);
            }
            else
            {
                var info = new Label("Enter Play Mode to test the audio.");
                info.AddToClassList("info-box");
                container.Add(info);
            }

            return container;
        }

        private VisualElement TriggerRow (string label, string boolProp, string actionProp, System.Action onChanged)
        {
            var pB = serializedObject.FindProperty(boolProp);
            var pA = serializedObject.FindProperty(actionProp);

            var row = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 1, marginBottom = 1 }
            };

            var toggle = new Toggle { value = pB.boolValue };
            toggle.showMixedValue = pB.hasMultipleDifferentValues;
            toggle.style.marginRight = 4;

            var lbl = new Label(label) { style = { flexGrow = 1 } };
            lbl.AddToClassList("sg-setting-row");

            var action = new EnumField((AudioTrigger.TriggerAction)pA.enumValueIndex)
            {
                style = { width = 120, display = pB.boolValue ? DisplayStyle.Flex : DisplayStyle.None }
            };
            action.showMixedValue = pA.hasMultipleDifferentValues;

            toggle.RegisterValueChangedCallback(e =>
            {
                pB.boolValue = e.newValue;
                serializedObject.ApplyModifiedProperties();
                action.style.display = e.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                onChanged?.Invoke();
            });
            action.RegisterValueChangedCallback(e =>
            {
                pA.enumValueIndex = (int)(AudioTrigger.TriggerAction)e.newValue;
                serializedObject.ApplyModifiedProperties();
            });

            row.Add(toggle);
            row.Add(lbl);
            row.Add(action);
            return row;
        }

        private Label MiniHeader (string text)
        {
            var l = new Label(text)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = Orange,
                    marginTop = 5,
                    marginBottom = 2
                }
            };
            return l;
        }

        private bool Bool (string prop) => serializedObject.FindProperty(prop).boolValue;

        // =====================================================================
        //  IMGUI (fallback) — used when the inspector is forced into IMGUI.
        // =====================================================================
        public override void OnInspectorGUI ()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(Description, MessageType.Info);
            if (!HasEmitter) EditorGUILayout.HelpBox(MissingEmitter, MessageType.Warning);

            EditorGUILayout.LabelField("Triggers", EditorStyles.boldLabel);

            DrawTriggerIMGUI("On Enable", "triggerOnEnable", "actionOnEnable");
            DrawTriggerIMGUI("On Start", "triggerOnStart", "actionOnStart");
            DrawTriggerIMGUI("On Destroy", "triggerOnDestroy", "actionOnDestroy");
            DrawTriggerIMGUI("On Collision Enter", "triggerOnCollisionEnter", "actionOnCollisionEnter");
            DrawTriggerIMGUI("On Collision Exit", "triggerOnCollisionExit", "actionOnCollisionExit");
            DrawTriggerIMGUI("On Trigger Enter", "triggerOnTriggerEnter", "actionOnTriggerEnter");
            DrawTriggerIMGUI("On Trigger Exit", "triggerOnTriggerExit", "actionOnTriggerExit");

            bool anyPhysics = Bool("triggerOnCollisionEnter") || Bool("triggerOnCollisionExit") ||
                              Bool("triggerOnTriggerEnter") || Bool("triggerOnTriggerExit");
            if (anyPhysics)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("filterLayers"), new GUIContent("Layers"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useTagFilter"));
                if (Bool("useTagFilter"))
                {
                    var tagProp = serializedObject.FindProperty("requiredTag");
                    tagProp.stringValue = EditorGUILayout.TagField("Required Tag",
                        string.IsNullOrEmpty(tagProp.stringValue) ? "Untagged" : tagProp.stringValue);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("cooldown"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnce"));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("▶ Play")) ((AudioTrigger)target).Play();
                if (GUILayout.Button("■ Stop")) ((AudioTrigger)target).Stop();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test the audio.", MessageType.Info);
            }
        }

        private void DrawTriggerIMGUI (string label, string boolProp, string actionProp)
        {
            var b = serializedObject.FindProperty(boolProp);
            EditorGUILayout.BeginHorizontal();
            b.boolValue = EditorGUILayout.ToggleLeft(label, b.boolValue, GUILayout.Width(180));
            if (b.boolValue)
                EditorGUILayout.PropertyField(serializedObject.FindProperty(actionProp), GUIContent.none);
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
