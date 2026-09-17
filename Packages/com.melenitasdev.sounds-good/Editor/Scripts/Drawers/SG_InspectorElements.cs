#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
    /// <summary>
    /// Shared UI Toolkit building blocks so the component inspectors match the look of the
    /// Sounds Good editor windows (Audio Creator, Settings, etc.): the same MyStyles.uss,
    /// the branded "| Section" headers, orange boxes and the SG_ field controls.
    /// </summary>
    internal static class SG_InspectorElements
    {
        private const string USS_PATH =
            "Packages/com.melenitasdev.sounds-good/Editor/UI/USS/MyStyles.uss";

        private const string INSPECTOR_USS_PATH =
            "Packages/com.melenitasdev.sounds-good/Editor/UI/USS/SG_Inspector.uss";

        private static readonly Color OrangeBorder = new Color(0.737f, 0.502f, 0.286f);
        private static readonly Color OrangeHeader = new Color(0.992f, 0.694f, 0.012f);

        public static void LoadStyles (VisualElement root)
        {
            root.AddToClassList("sg-inspector");

            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(USS_PATH);
            if (uss != null) root.styleSheets.Add(uss);

            var inspectorUss = AssetDatabase.LoadAssetAtPath<StyleSheet>(INSPECTOR_USS_PATH);
            if (inspectorUss != null) root.styleSheets.Add(inspectorUss);
        }

        /// <summary>
        /// Builds a UI Toolkit inspector that rebuilds its body from the current serialized state on
        /// every undo/redo. Our SG_ controls set their value with SetValueWithoutNotify and aren't
        /// bound by path, so Unity doesn't refresh them — nor the conditional show/hide driven by
        /// them, nor the inline EnumField/TagField controls — when the user presses Ctrl+Z. Rebuilding
        /// keeps everything in sync. It only fires on undo/redo, never on the user's own edits, so it
        /// doesn't disrupt typing. Each editor passes its build body via <paramref name="buildBody"/>;
        /// styles are loaded once and survive the rebuild, so the body must not call LoadStyles or Bind.
        /// </summary>
        public static VisualElement BuildInspector (UnityEditor.Editor editor, Action<VisualElement> buildBody)
        {
            var root = new VisualElement();
            LoadStyles(root);

            void Rebuild ()
            {
                root.Clear();
                editor.serializedObject.Update();
                buildBody(root);
                root.Bind(editor.serializedObject);
            }
            Rebuild();

            Undo.undoRedoPerformed += Rebuild;
            root.RegisterCallback<DetachFromPanelEvent>(_ => Undo.undoRedoPerformed -= Rebuild);

            return root;
        }

        /// <summary>Creates a "| Title" header + an orange box to fill. Returns the wrapper.</summary>
        public static VisualElement Section (string title, out VisualElement box)
        {
            var wrapper = new VisualElement();
            wrapper.style.marginTop = 4;

            var header = new Label("| " + title);
            header.AddToClassList("subtitle");
            header.style.paddingLeft = 0;
            wrapper.Add(header);

            box = new VisualElement();
            ApplyBox(box);
            wrapper.Add(box);

            return wrapper;
        }

        /// <summary>
        /// Like <see cref="Section"/> but collapsible: clicking the header shows/hides the box.
        /// The expanded state is remembered per section title via EditorPrefs.
        /// </summary>
        public static VisualElement FoldoutSection (string title, out VisualElement box)
        {
            var wrapper = new VisualElement();
            wrapper.style.marginTop = 4;

            string prefKey = "SG_Foldout_" + title;
            bool expanded = EditorPrefs.GetBool(prefKey, true);

            var header = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center }
            };

            var arrow = new Label(expanded ? "▼" : "▶");
            arrow.AddToClassList("subtitle");
            arrow.style.paddingLeft = 0;
            arrow.style.paddingRight = 0;
            arrow.style.marginRight = 4;
            arrow.style.width = 12;
            header.Add(arrow);

            var titleLabel = new Label("| " + title);
            titleLabel.AddToClassList("subtitle");
            titleLabel.style.paddingLeft = 0;
            titleLabel.style.flexGrow = 1;
            header.Add(titleLabel);

            wrapper.Add(header);

            var innerBox = new VisualElement();
            ApplyBox(innerBox);
            innerBox.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
            wrapper.Add(innerBox);
            box = innerBox;

            header.RegisterCallback<ClickEvent>(_ =>
            {
                expanded = !expanded;
                EditorPrefs.SetBool(prefKey, expanded);
                innerBox.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
                arrow.text = expanded ? "▼" : "▶";
            });

            return wrapper;
        }

        public static void ApplyBox (VisualElement box)
        {
            box.style.marginBottom = 8;
            box.style.paddingTop = 8;
            box.style.paddingBottom = 8;
            box.style.paddingLeft = 8;
            box.style.paddingRight = 8;

            box.style.borderTopWidth = 1;
            box.style.borderBottomWidth = 1;
            box.style.borderLeftWidth = 1;
            box.style.borderRightWidth = 1;

            box.style.borderTopColor = OrangeBorder;
            box.style.borderBottomColor = OrangeBorder;
            box.style.borderLeftColor = OrangeBorder;
            box.style.borderRightColor = OrangeBorder;

            box.style.borderTopLeftRadius = 4;
            box.style.borderTopRightRadius = 4;
            box.style.borderBottomLeftRadius = 4;
            box.style.borderBottomRightRadius = 4;
        }

        // ----- Field builders (SG_ controls wired to a SerializedProperty) -----

        public static SG_BoolField Bool (SerializedObject so, string propName, string label)
        {
            var prop = so.FindProperty(propName);
            var field = new SG_BoolField { LabelText = label, Value = prop.boolValue };
            field.tooltip = prop.tooltip;
            field.ShowMixedValue = prop.hasMultipleDifferentValues;
            field.OnValueChanged += v =>
            {
                prop.boolValue = v;
                so.ApplyModifiedProperties();
            };
            return field;
        }

        /// <param name="defaultValue">What the reset button restores. It has to be passed in because
        /// the current value is the component's, not its default. Left out, no reset is shown.</param>
        public static SG_FloatField Float (SerializedObject so, string propName, string label,
            string suffix = "", float min = float.NegativeInfinity, float defaultValue = float.NaN)
            => Float(so.FindProperty(propName), label, suffix, min, defaultValue);

        public static SG_FloatField Float (SerializedProperty prop, string label,
            string suffix = "", float min = float.NegativeInfinity, float defaultValue = float.NaN)
        {
            // Delayed so the min clamp only kicks in on commit, not while the user is still
            // typing a small value (otherwise "0.05" gets clamped to min the moment it reads "0").
            var field = new SG_FloatField
            {
                LabelText = label,
                Suffix = suffix,
                Value = prop.floatValue,
                Delayed = true,
                DefaultValue = defaultValue
            };
            field.tooltip = prop.tooltip;
            field.ShowMixedValue = prop.hasMultipleDifferentValues;
            field.OnValueChanged += v =>
            {
                if (v < min)
                {
                    v = min;
                    field.Value = v;
                }
                prop.floatValue = v;
                prop.serializedObject.ApplyModifiedProperties();
            };
            return field;
        }

        /// <param name="defaultValue">What the reset button restores. Left out, no reset is shown.</param>
        public static SG_IntField Int (SerializedObject so, string propName, string label,
            string suffix = "", int min = int.MinValue, int defaultValue = int.MinValue)
        {
            var prop = so.FindProperty(propName);
            // Delayed for the same reason as the float field: a min clamp mustn't fight the user
            // while they are still typing.
            var field = new SG_IntField
            {
                LabelText = label,
                Suffix = suffix,
                Value = prop.intValue,
                Delayed = true,
                DefaultValue = defaultValue
            };
            field.tooltip = prop.tooltip;
            field.ShowMixedValue = prop.hasMultipleDifferentValues;
            field.OnValueChanged += v =>
            {
                if (min != int.MinValue && v < min)
                {
                    v = min;
                    field.Value = v;
                }
                prop.intValue = v;
                prop.serializedObject.ApplyModifiedProperties();
            };
            return field;
        }

        /// <summary>
        /// A <c>Vector2</c> row. Every Vector2 in Sounds Good is a range, so the two components are
        /// labelled Min and Max by default rather than X and Y.
        /// </summary>
        // Not called just "Vector2": a method sharing a type's name reads badly and invites
        // resolution surprises in the class that declares it.
        public static SG_Vector2Field Vector2Range (SerializedObject so, string propName, string label,
            string xLabel = "Min", string yLabel = "Max", Vector2? defaultValue = null)
        {
            var prop = so.FindProperty(propName);
            var field = new SG_Vector2Field
            {
                LabelText = label,
                XLabel = xLabel,
                YLabel = yLabel,
                Value = prop.vector2Value,
                // Nullable rather than comparing against default, so a legitimate (0,0) default
                // isn't mistaken for "no default".
                DefaultValue = defaultValue ?? new Vector2(float.NaN, float.NaN)
            };
            field.tooltip = prop.tooltip;
            field.ShowMixedValue = prop.hasMultipleDifferentValues;
            field.OnValueChanged += v =>
            {
                prop.vector2Value = v;
                prop.serializedObject.ApplyModifiedProperties();
            };
            return field;
        }

        // Per-face box fade block, part of the zone editors.
//#SG_PRO_BEGIN
        /// <summary>
        /// The per-face fade block for a box zone: one distance field per face, in world units.
        /// The property must be a <see cref="BoxFade"/>. Ordered entrance-first (front/back) then
        /// the sides so the common "fade only at the entrance" case reads at a glance.
        /// </summary>
        public static VisualElement BoxFadeBlock (SerializedObject so, string propName = "boxFade")
        {
            var prop = so.FindProperty(propName);
            var container = new VisualElement();
            container.Add(Float(prop.FindPropertyRelative("front"), "Front Fade (+Z)", "", 0f, 0f));
            container.Add(Float(prop.FindPropertyRelative("back"), "Back Fade (-Z)", "", 0f, 0f));
            container.Add(Float(prop.FindPropertyRelative("right"), "Right Fade (+X)", "", 0f, 0f));
            container.Add(Float(prop.FindPropertyRelative("left"), "Left Fade (-X)", "", 0f, 0f));
            container.Add(Float(prop.FindPropertyRelative("top"), "Top Fade (+Y)", "", 0f, 0f));
            container.Add(Float(prop.FindPropertyRelative("bottom"), "Bottom Fade (-Y)", "", 0f, 0f));
            return container;
        }

        /// <summary> IMGUI equivalent of <see cref="BoxFadeBlock"/>. </summary>
        public static void BoxFadeBlockIMGUI (SerializedObject so, string propName = "boxFade")
        {
            var prop = so.FindProperty(propName);
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("front"), new GUIContent("Front Fade (+Z)"));
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("back"), new GUIContent("Back Fade (-Z)"));
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("right"), new GUIContent("Right Fade (+X)"));
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("left"), new GUIContent("Left Fade (-X)"));
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("top"), new GUIContent("Top Fade (+Y)"));
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("bottom"), new GUIContent("Bottom Fade (-Y)"));
        }
//#SG_PRO_END

        // Zone shape editors: used only by Audio Effect Zone and Music Zone.
//#SG_PRO_BEGIN
        // ----- Zone shape (shared by the world audio zones: Audio Effect Zone, Music Zone) -----

        /// <summary>
        /// Fills a section box with the full shape editor shared by every <see cref="AudioZone"/>:
        /// sphere/box picker, transform-scale toggle, the size fields, the uniform/per-face fade and
        /// the gizmo colors, showing only the fields that apply to the current shape. Field names are
        /// fixed by convention on <see cref="AudioZone"/> (zoneShape, radius, width…).
        /// </summary>
        public static void ZoneShapeBody (SerializedObject so, VisualElement box)
        {
            var shapeProp = so.FindProperty("zoneShape");
            var shapeField = new EnumField("Shape", (AudioZone.Shape)shapeProp.enumValueIndex);
            shapeField.showMixedValue = shapeProp.hasMultipleDifferentValues;
            box.Add(shapeField);

            var useScale = Bool(so, "useScaleAsZoneSize", "Use Transform Scale");
            box.Add(useScale);

            box.Add(Spacer());
            box.Add(MiniHeader("Size"));

            var usingScaleInfo = new Label("Using transform scale to resize the zone.");
            usingScaleInfo.AddToClassList("info-box");

            // Box size fields
            var boxDims = new VisualElement();
            boxDims.Add(Float(so, "width", "Width", "", 0f, 5f));
            boxDims.Add(Float(so, "height", "Height", "", 0f, 5f));
            boxDims.Add(Float(so, "depth", "Depth", "", 0f, 5f));

            // Fade for the box (world units, 0 = hard edge). Uniform by default; per-face on demand.
            var boxFade = new VisualElement();
            boxFade.Add(MiniHeader("Fade"));
            var perFace = Bool(so, "perFaceFade", "Fade Per Face");
            boxFade.Add(perFace);
            var uniformFade = Float(so, "uniformBoxFade", "Fade", "", 0f, 0.5f);
            var perFaceBlock = BoxFadeBlock(so);
            boxFade.Add(uniformFade);
            boxFade.Add(perFaceBlock);

            void UpdateFadeMode (bool on)
            {
                uniformFade.style.display = on ? DisplayStyle.None : DisplayStyle.Flex;
                perFaceBlock.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            }
            UpdateFadeMode(so.FindProperty("perFaceFade").boolValue);
            perFace.OnValueChanged += UpdateFadeMode;

            // Sphere size fields
            var sphereDims = new VisualElement();
            sphereDims.Add(Float(so, "radius", "Radius", "", 0f, 5f));
            var sphereFade = Float(so, "extraRadiusFade", "Fade Radius", "", 0.05f, 0.2f);

            box.Add(usingScaleInfo);
            box.Add(boxDims);
            box.Add(boxFade);
            box.Add(sphereDims);
            box.Add(sphereFade);

            box.Add(Spacer());
            box.Add(new UnityEditor.UIElements.PropertyField(so.FindProperty("areaColor"), "Area Color"));
            box.Add(new UnityEditor.UIElements.PropertyField(so.FindProperty("fadeColor"), "Fade Color"));

            void UpdateShape ()
            {
                bool isBox = (AudioZone.Shape)shapeProp.enumValueIndex == AudioZone.Shape.Box;
                bool scale = so.FindProperty("useScaleAsZoneSize").boolValue;

                usingScaleInfo.style.display = scale ? DisplayStyle.Flex : DisplayStyle.None;

                boxDims.style.display = (isBox && !scale) ? DisplayStyle.Flex : DisplayStyle.None;
                boxFade.style.display = isBox ? DisplayStyle.Flex : DisplayStyle.None;

                sphereDims.style.display = (!isBox && !scale) ? DisplayStyle.Flex : DisplayStyle.None;
                sphereFade.style.display = !isBox ? DisplayStyle.Flex : DisplayStyle.None;
            }

            UpdateShape();
            shapeField.RegisterValueChangedCallback(e =>
            {
                shapeProp.enumValueIndex = (int)(AudioZone.Shape)e.newValue;
                so.ApplyModifiedProperties();
                UpdateShape();
            });
            useScale.OnValueChanged += _ => UpdateShape();
        }

        /// <summary> IMGUI equivalent of <see cref="ZoneShapeBody"/> (includes the "Shape" header). </summary>
        public static void ZoneShapeSectionIMGUI (SerializedObject so)
        {
            EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);

            var shapeProp = so.FindProperty("zoneShape");
            EditorGUILayout.PropertyField(shapeProp, new GUIContent("Shape"));
            var useScaleProp = so.FindProperty("useScaleAsZoneSize");
            EditorGUILayout.PropertyField(useScaleProp, new GUIContent("Use Transform Scale"));

            bool isBox = (AudioZone.Shape)shapeProp.enumValueIndex == AudioZone.Shape.Box;
            bool scale = useScaleProp.boolValue;

            if (scale) EditorGUILayout.HelpBox("Using transform scale to resize the zone.", MessageType.Info);

            if (isBox)
            {
                if (!scale)
                {
                    ClampedFloat(so, "width", "Width", 0f);
                    ClampedFloat(so, "height", "Height", 0f);
                    ClampedFloat(so, "depth", "Depth", 0f);
                }
                var perFaceProp = so.FindProperty("perFaceFade");
                EditorGUILayout.PropertyField(perFaceProp, new GUIContent("Fade Per Face"));
                if (perFaceProp.boolValue) BoxFadeBlockIMGUI(so);
                else ClampedFloat(so, "uniformBoxFade", "Fade", 0f);
            }
            else
            {
                if (!scale) ClampedFloat(so, "radius", "Radius", 0f);
                ClampedFloat(so, "extraRadiusFade", "Fade Radius", 0.05f);
            }

            EditorGUILayout.PropertyField(so.FindProperty("areaColor"), new GUIContent("Area Color"));
            EditorGUILayout.PropertyField(so.FindProperty("fadeColor"), new GUIContent("Fade Color"));
        }
//#SG_PRO_END

        /// <summary> A delayed float field clamped to a minimum only on commit (IMGUI). </summary>
        public static void ClampedFloat (SerializedObject so, string propName, string label, float min)
        {
            var prop = so.FindProperty(propName);
            prop.floatValue = Mathf.Max(min, EditorGUILayout.DelayedFloatField(label, prop.floatValue));
        }

        private static Label MiniHeader (string text)
        {
            return new Label(text)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = OrangeHeader,
                    marginTop = 5,
                    marginBottom = 2
                }
            };
        }

        private static VisualElement Spacer () => new VisualElement { style = { height = 4 } };

        public static SG_Slider Slider01 (SerializedObject so, string propName, string label,
            float defaultValue = 1f)
        {
            var prop = so.FindProperty(propName);
            var field = new SG_Slider
            {
                LabelText = label,
                MinValue = 0f,
                MaxValue = 1f,
                PercentageMode = true,
                Decimals = 0,
                Suffix = "%",
                Value = prop.floatValue,
                DefaultValue = defaultValue
            };
            field.tooltip = prop.tooltip;
            field.ShowMixedValue = prop.hasMultipleDifferentValues;
            field.OnValueChanged += v =>
            {
                prop.floatValue = v;
                so.ApplyModifiedProperties();
            };
            return field;
        }

        public static SG_Slider SliderRange (SerializedObject so, string propName, string label,
            float min, float max, int decimals = 2, string suffix = "", float defaultValue = 1f)
        {
            var prop = so.FindProperty(propName);
            var field = new SG_Slider
            {
                LabelText = label,
                MinValue = min,
                MaxValue = max,
                PercentageMode = false,
                Decimals = decimals,
                Suffix = suffix,
                Value = prop.floatValue,
                DefaultValue = defaultValue
            };
            field.tooltip = prop.tooltip;
            field.ShowMixedValue = prop.hasMultipleDifferentValues;
            field.OnValueChanged += v =>
            {
                prop.floatValue = v;
                so.ApplyModifiedProperties();
            };
            return field;
        }

        /// <summary>
        /// The shared 3D distance block (min/max hear distance + rolloff curve, with the custom
        /// AnimationCurve shown only when Custom is picked). Used by every no-code component so the
        /// options stay identical. Field names are fixed by convention across the components.
        /// </summary>
        public static VisualElement DistanceBlock (SerializedObject so)
        {
            var container = new VisualElement();

            container.Add(Float(so, "hearDistanceMin", "Min Distance", "", 0f, 3f));
            container.Add(Float(so, "hearDistanceMax", "Max Distance", "", 0f, 500f));

            var rolloffProp = so.FindProperty("volumeRolloff");
            var rolloffField = new EnumField("Rolloff Curve", (ComponentRolloff)rolloffProp.enumValueIndex)
            {
                tooltip = rolloffProp.tooltip
            };
            rolloffField.showMixedValue = rolloffProp.hasMultipleDifferentValues;
            container.Add(rolloffField);

            var curve = new UnityEditor.UIElements.PropertyField(so.FindProperty("customVolumeCurve"), "Custom Curve");
            container.Add(curve);

            void UpdateCurve ()
            {
                bool custom = (ComponentRolloff)rolloffProp.enumValueIndex == ComponentRolloff.Custom;
                curve.style.display = custom ? DisplayStyle.Flex : DisplayStyle.None;
            }
            UpdateCurve();
            rolloffField.RegisterValueChangedCallback(e =>
            {
                rolloffProp.enumValueIndex = (int)(ComponentRolloff)e.newValue;
                so.ApplyModifiedProperties();
                UpdateCurve();
            });

            return container;
        }

        /// <summary> IMGUI equivalent of <see cref="DistanceBlock"/>. </summary>
        public static void DistanceBlockIMGUI (SerializedObject so)
        {
            EditorGUILayout.PropertyField(so.FindProperty("hearDistanceMin"), new GUIContent("Min Distance"));
            EditorGUILayout.PropertyField(so.FindProperty("hearDistanceMax"), new GUIContent("Max Distance"));

            var rolloffProp = so.FindProperty("volumeRolloff");
            EditorGUILayout.PropertyField(rolloffProp, new GUIContent("Rolloff Curve"));
            if ((ComponentRolloff)rolloffProp.enumValueIndex == ComponentRolloff.Custom)
                EditorGUILayout.PropertyField(so.FindProperty("customVolumeCurve"), new GUIContent("Custom Curve"));
        }

        // The audio-effect block: only the paid components expose an Effect field.
//#SG_PRO_BEGIN
        /// <summary>
        /// The shared audio-effect block: the Effect pseudo-enum picker plus its intensity slider.
        /// Field names are fixed by convention across the components ("effect", "effectIntensity").
        /// </summary>
        public static VisualElement EffectBlock (SerializedObject so)
        {
            var container = new VisualElement();
            container.Add(new UnityEditor.UIElements.PropertyField(so.FindProperty("effect"), "Effect"));
            container.Add(SliderRange(so, "effectIntensity", "Effect Intensity", 0f, 1f, 2));
            return container;
        }

        /// <summary> IMGUI equivalent of <see cref="EffectBlock"/>. </summary>
        public static void EffectBlockIMGUI (SerializedObject so)
        {
            EditorGUILayout.PropertyField(so.FindProperty("effect"), new GUIContent("Effect"));
            EditorGUILayout.PropertyField(so.FindProperty("effectIntensity"), new GUIContent("Effect Intensity"));
        }
//#SG_PRO_END

        public static SG_StringField String (SerializedObject so, string propName, string label)
        {
            var prop = so.FindProperty(propName);
            var field = new SG_StringField { LabelText = label, Value = prop.stringValue };
            field.tooltip = prop.tooltip;
            field.ShowMixedValue = prop.hasMultipleDifferentValues;
            field.OnValueChanged += v =>
            {
                prop.stringValue = v;
                so.ApplyModifiedProperties();
            };
            return field;
        }

        /// <summary>A plain enum popup bound to an enum <see cref="SerializedProperty"/>.</summary>
        public static EnumField EnumPopup<TEnum> (SerializedObject so, string propName, string label)
            where TEnum : struct, System.Enum
        {
            var prop = so.FindProperty(propName);
            var current = (TEnum)System.Enum.ToObject(typeof(TEnum), prop.enumValueIndex);
            var field = new EnumField(label, current) { tooltip = prop.tooltip };
            field.showMixedValue = prop.hasMultipleDifferentValues;
            field.RegisterValueChangedCallback(e =>
            {
                prop.enumValueIndex = System.Convert.ToInt32(e.newValue);
                so.ApplyModifiedProperties();
            });
            return field;
        }

        public static SG_LayerMaskField LayerMask (SerializedObject so, string propName, string label)
        {
            var prop = so.FindProperty(propName);
            var field = new SG_LayerMaskField { LabelText = label, Value = prop.intValue };
            field.ShowMixedValue = prop.hasMultipleDifferentValues;
            field.OnValueChanged += v =>
            {
                prop.intValue = v;
                so.ApplyModifiedProperties();
            };
            return field;
        }

        // ----- Test buttons (only interactive in Play Mode) -----

        public static VisualElement PlayStopButtons (Action onPlay, Action onStop, string idleMessage)
        {
            var container = new VisualElement { style = { marginTop = 4 } };

            if (Application.isPlaying)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

                var play = new Button(() => onPlay?.Invoke()) { text = "▶ Play" };
                play.AddToClassList("create-button");
                play.style.flexGrow = 1;
                play.style.height = 26;

                var stop = new Button(() => onStop?.Invoke()) { text = "■ Stop" };
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
                container.Add(InfoBox(idleMessage));
            }

            return container;
        }

        public static VisualElement PlayButton (Action onPlay, string label, string idleMessage)
        {
            var container = new VisualElement { style = { marginTop = 4 } };

            if (Application.isPlaying)
            {
                var play = new Button(() => onPlay?.Invoke()) { text = label };
                play.AddToClassList("create-button");
                play.style.height = 26;
                container.Add(play);
            }
            else
            {
                container.Add(InfoBox(idleMessage));
            }

            return container;
        }

        public static Label InfoBox (string message)
        {
            var info = new Label(message);
            info.AddToClassList("info-box");
            return info;
        }

        /// <summary>Branded "what this component does" blurb shown at the top of the inspector.</summary>
        public static VisualElement Description (string text)
        {
            var box = new VisualElement();
            ApplyBox(box);
            box.style.backgroundColor = new Color(0.992f, 0.694f, 0.012f, 0.08f);

            var label = new Label(text)
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    color = new Color(0.910f, 0.792f, 0.643f),
                    fontSize = 11
                }
            };
            box.Add(label);
            return box;
        }
    }
}
#endif
