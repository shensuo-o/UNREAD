#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
    [CustomEditor(typeof(RandomAmbience))]
    [CanEditMultipleObjects]
    public class RandomAmbienceEditor : UnityEditor.Editor
    {
        // =====================================================================
        //  UI Toolkit (primary) — matches the Sounds Good editor windows.
        // =====================================================================
        public override VisualElement CreateInspectorGUI ()
            => SG_InspectorElements.BuildInspector(this, BuildBody);

        private void BuildBody (VisualElement root)
        {
            root.Add(SG_InspectorElements.Description(
                "Plays random ambient sounds at random intervals (birds, drips, creaks...). " +
                "Emits from this object, a radius around it, or following it."));

            // ----- Sounds -----
            var soundsWrap = SG_InspectorElements.FoldoutSection("Sounds", out var soundsBox);
            soundsBox.Add(new PropertyField(serializedObject.FindProperty("sounds"), "Sounds"));
            root.Add(soundsWrap);

            // ----- Timing -----
            var timingWrap = SG_InspectorElements.FoldoutSection("Timing", out var timingBox);

            var minProp = serializedObject.FindProperty("minInterval");
            var maxProp = serializedObject.FindProperty("maxInterval");

            // Built by hand rather than through SG_InspectorElements.Float because the two clamp
            // against each other below, but they still get everything a builder-made row has.
            var minField = new SG_FloatField
            {
                LabelText = "Min Interval",
                Suffix = "s",
                Value = minProp.floatValue,
                Delayed = true,
                DefaultValue = 3f,
                tooltip = minProp.tooltip
            };
            minField.ShowMixedValue = minProp.hasMultipleDifferentValues;
            var maxField = new SG_FloatField
            {
                LabelText = "Max Interval",
                Suffix = "s",
                Value = maxProp.floatValue,
                Delayed = true,
                DefaultValue = 10f,
                tooltip = maxProp.tooltip
            };
            maxField.ShowMixedValue = maxProp.hasMultipleDifferentValues;

            minField.OnValueChanged += v =>
            {
                v = Mathf.Max(0f, v);
                minField.Value = v;
                minProp.floatValue = v;
                if (maxProp.floatValue < v)
                {
                    maxProp.floatValue = v;
                    maxField.Value = v;
                }
                serializedObject.ApplyModifiedProperties();
            };
            maxField.OnValueChanged += v =>
            {
                v = Mathf.Max(minProp.floatValue, v);
                maxField.Value = v;
                maxProp.floatValue = v;
                serializedObject.ApplyModifiedProperties();
            };

            timingBox.Add(minField);
            timingBox.Add(maxField);
            timingBox.Add(SG_InspectorElements.Bool(serializedObject, "playImmediatelyOnEnable", "Play On Enable"));
            root.Add(timingWrap);

            // ----- Audio -----
            var audioWrap = SG_InspectorElements.FoldoutSection("Audio", out var audioBox);

            var volume = SG_InspectorElements.Slider01(serializedObject, "volume", "Volume");
            var randomVolume = SG_InspectorElements.Bool(serializedObject, "randomVolume", "Random Volume");
            var volumeRange = SG_InspectorElements.Vector2Range(serializedObject, "volumeRange", "Volume Range",
                defaultValue: new Vector2(0.8f, 1f));
            audioBox.Add(volume);
            audioBox.Add(randomVolume);
            audioBox.Add(volumeRange);
            void UpdateVolume (bool on)
            {
                volume.style.display = on ? DisplayStyle.None : DisplayStyle.Flex;
                volumeRange.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            }
            UpdateVolume(serializedObject.FindProperty("randomVolume").boolValue);
            randomVolume.OnValueChanged += UpdateVolume;

            var randomPitch = SG_InspectorElements.Bool(serializedObject, "randomPitch", "Random Pitch");
            var pitchRange = SG_InspectorElements.Vector2Range(serializedObject, "pitchRange", "Pitch Range",
                defaultValue: new Vector2(0.85f, 1.15f));
            var pitch = SG_InspectorElements.SliderRange(serializedObject, "pitch", "Pitch", -3f, 3f, 2);
            audioBox.Add(randomPitch);
            audioBox.Add(pitchRange);
            audioBox.Add(pitch);
            void UpdatePitchRange (bool on)
            {
                pitchRange.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                pitch.style.display = on ? DisplayStyle.None : DisplayStyle.Flex;
            }
            UpdatePitchRange(serializedObject.FindProperty("randomPitch").boolValue);
            randomPitch.OnValueChanged += UpdatePitchRange;

            audioBox.Add(SG_InspectorElements.EnumPopup<TimeMode>(serializedObject, "timeMode", "Time Mode"));
            audioBox.Add(new PropertyField(serializedObject.FindProperty("output"), "Audio Output"));
            audioBox.Add(SG_InspectorElements.EffectBlock(serializedObject));
            root.Add(audioWrap);

            // ----- Spatialization -----
            var spatialWrap = SG_InspectorElements.FoldoutSection("Spatialization", out var spatialBox);
            var spatial = SG_InspectorElements.Bool(serializedObject, "spatialSound", "Spatial Sound");
            spatialBox.Add(spatial);
            var occlusion = SG_InspectorElements.Bool(serializedObject, "useOcclusion", "Use Occlusion");
            spatialBox.Add(occlusion);
            var doppler = SG_InspectorElements.SliderRange(serializedObject, "dopplerLevel", "Doppler Level", 0f, 5f, 2);
            spatialBox.Add(doppler);
            var distance = SG_InspectorElements.DistanceBlock(serializedObject);
            spatialBox.Add(distance);

            var modeProp = serializedObject.FindProperty("emissionMode");
            var modeField = new EnumField("Emission Mode", (RandomAmbience.EmissionMode)modeProp.enumValueIndex);
            modeField.showMixedValue = modeProp.hasMultipleDifferentValues;
            spatialBox.Add(modeField);

            var radiusField = SG_InspectorElements.Float(serializedObject, "emissionRadius", "Emission Radius", "m", float.NegativeInfinity, 5f);
            var colorField = new PropertyField(serializedObject.FindProperty("areaColor"), "Area Color");
            spatialBox.Add(radiusField);
            spatialBox.Add(colorField);

            // Everything but the Spatial Sound toggle only matters for 3D sound.
            void UpdateVisibility ()
            {
                bool sp = serializedObject.FindProperty("spatialSound").boolValue;
                bool radiusMode = (RandomAmbience.EmissionMode)modeProp.enumValueIndex ==
                                  RandomAmbience.EmissionMode.RandomWithinRadius;

                occlusion.style.display = sp ? DisplayStyle.Flex : DisplayStyle.None;
                doppler.style.display = sp ? DisplayStyle.Flex : DisplayStyle.None;
                distance.style.display = sp ? DisplayStyle.Flex : DisplayStyle.None;
                modeField.style.display = sp ? DisplayStyle.Flex : DisplayStyle.None;
                radiusField.style.display = (sp && radiusMode) ? DisplayStyle.Flex : DisplayStyle.None;
                colorField.style.display = (sp && radiusMode) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            UpdateVisibility();
            spatial.OnValueChanged += _ => UpdateVisibility();
            modeField.RegisterValueChangedCallback(e =>
            {
                modeProp.enumValueIndex = (int)(RandomAmbience.EmissionMode)e.newValue;
                serializedObject.ApplyModifiedProperties();
                UpdateVisibility();
            });
            root.Add(spatialWrap);

            // ----- Test button -----
            root.Add(BuildTestButton());
        }

        private VisualElement BuildTestButton ()
        {
            var container = new VisualElement { style = { marginTop = 4 } };

            if (Application.isPlaying)
            {
                var button = new Button(() => ((RandomAmbience)target).PlayRandomSound())
                {
                    text = "▶ Play Random Sound Now"
                };
                button.AddToClassList("create-button");
                button.style.height = 26;
                container.Add(button);
            }
            else
            {
                var info = new Label("Enter Play Mode to test the ambience.");
                info.AddToClassList("info-box");
                container.Add(info);
            }

            return container;
        }

        // =====================================================================
        //  IMGUI (fallback) — used when the inspector is forced into IMGUI.
        // =====================================================================
        public override void OnInspectorGUI ()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Plays random ambient sounds at random intervals (birds, drips, creaks...). " +
                "Emits from this object, a radius around it, or following it.",
                MessageType.Info);

            EditorGUILayout.LabelField("Sounds", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sounds"), new GUIContent("Sounds"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);

            var minProp = serializedObject.FindProperty("minInterval");
            var maxProp = serializedObject.FindProperty("maxInterval");
            minProp.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField("Min Interval", minProp.floatValue));
            maxProp.floatValue = Mathf.Max(minProp.floatValue, EditorGUILayout.FloatField("Max Interval", maxProp.floatValue));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playImmediatelyOnEnable"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("randomVolume"));
            if (serializedObject.FindProperty("randomVolume").boolValue)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("volumeRange"), new GUIContent("Volume Range"));
            else
                EditorGUILayout.PropertyField(serializedObject.FindProperty("volume"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("randomPitch"));
            if (serializedObject.FindProperty("randomPitch").boolValue)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pitchRange"));
            else
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pitch"), new GUIContent("Pitch"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("timeMode"), new GUIContent("Time Mode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("output"), new GUIContent("Audio Output"));
            SG_InspectorElements.EffectBlockIMGUI(serializedObject);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spatialization", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spatialSound"));
            if (serializedObject.FindProperty("spatialSound").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useOcclusion"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("dopplerLevel"), new GUIContent("Doppler Level"));
                SG_InspectorElements.DistanceBlockIMGUI(serializedObject);
                var modeProp = serializedObject.FindProperty("emissionMode");
                EditorGUILayout.PropertyField(modeProp, new GUIContent("Emission Mode"));
                if ((RandomAmbience.EmissionMode)modeProp.enumValueIndex == RandomAmbience.EmissionMode.RandomWithinRadius)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("emissionRadius"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("areaColor"));
                }
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            if (Application.isPlaying)
            {
                if (GUILayout.Button("▶ Play Random Sound Now")) ((RandomAmbience)target).PlayRandomSound();
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test the ambience.", MessageType.Info);
            }
        }
    }
}
#endif
