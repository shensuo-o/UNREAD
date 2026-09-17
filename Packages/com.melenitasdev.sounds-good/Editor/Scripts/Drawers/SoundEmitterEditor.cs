#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
    [CustomEditor(typeof(SoundEmitter))]
    [CanEditMultipleObjects]
    public class SoundEmitterEditor : UnityEditor.Editor
    {

        private const string DESCRIPTION =
            "Configures a Sound from the Inspector, exactly like the code API. Reference it to call " +
            "Play/Pause/Resume/Stop, and use its .Sound property to call any Set* method at runtime.";

        // =====================================================================
        //  UI Toolkit (primary)
        // =====================================================================
        public override VisualElement CreateInspectorGUI ()
            => SG_InspectorElements.BuildInspector(this, BuildBody);

        private void BuildBody (VisualElement root)
        {
            root.Add(SG_InspectorElements.Description(DESCRIPTION));

            // ----- Clip -----
            var clipWrap = SG_InspectorElements.FoldoutSection("Clip", out var clipBox);
            clipBox.Add(new PropertyField(serializedObject.FindProperty("sfx"), "Sound"));
            var randomClip = SG_InspectorElements.Bool(serializedObject, "randomClip", "Random Clip");
            clipBox.Add(randomClip);
            var clipIndex = SG_InspectorElements.Int(serializedObject, "clipIndex", "Clip Index", "", 0, 0);
            clipBox.Add(clipIndex);
            void UpdateClip (bool on) => clipIndex.style.display = on ? DisplayStyle.None : DisplayStyle.Flex;
            UpdateClip(Bool("randomClip"));
            randomClip.OnValueChanged += UpdateClip;
            root.Add(clipWrap);

            // ----- Settings -----
            var wrap = SG_InspectorElements.FoldoutSection("Settings", out var box);

            // Volume: fixed slider, or a random range when Random Volume is on.
            var volume = SG_InspectorElements.Slider01(serializedObject, "volume", "Volume");
            var randomVolume = SG_InspectorElements.Bool(serializedObject, "randomVolume", "Random Volume");
            var volumeRange = SG_InspectorElements.Vector2Range(serializedObject, "volumeRange", "Volume Range",
                defaultValue: new Vector2(0.8f, 1f));
            box.Add(volume);
            box.Add(randomVolume);
            box.Add(volumeRange);
            void UpdateVolume (bool on)
            {
                volume.style.display = on ? DisplayStyle.None : DisplayStyle.Flex;
                volumeRange.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            }
            UpdateVolume(Bool("randomVolume"));
            randomVolume.OnValueChanged += UpdateVolume;

            // Pitch: same pattern as volume — fixed slider, or a random range.
            var pitch = SG_InspectorElements.SliderRange(serializedObject, "pitch", "Pitch", -3f, 3f, 2);
            var randomPitch = SG_InspectorElements.Bool(serializedObject, "randomPitch", "Random Pitch");
            var pitchRange = SG_InspectorElements.Vector2Range(serializedObject, "pitchRange", "Pitch Range",
                defaultValue: new Vector2(0.85f, 1.15f));
            box.Add(pitch);
            box.Add(randomPitch);
            box.Add(pitchRange);
            void UpdatePitch (bool on)
            {
                pitch.style.display = on ? DisplayStyle.None : DisplayStyle.Flex;
                pitchRange.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            }
            UpdatePitch(Bool("randomPitch"));
            randomPitch.OnValueChanged += UpdatePitch;

            var loop = SG_InspectorElements.Bool(serializedObject, "loop", "Loop");
            box.Add(loop);
            box.Add(SG_InspectorElements.Slider01(serializedObject, "playProbability", "Play Probability"));

            var spatial = SG_InspectorElements.Bool(serializedObject, "spatialSound", "Spatial Sound");
            box.Add(spatial);
//#SG_PRO_BEGIN
            var occlusion = SG_InspectorElements.Bool(serializedObject, "useOcclusion", "Use Occlusion");
            box.Add(occlusion);
//#SG_PRO_END
            var doppler = SG_InspectorElements.SliderRange(serializedObject, "dopplerLevel", "Doppler Level", 0f, 5f, 2);
            box.Add(doppler);
            var distance = SG_InspectorElements.DistanceBlock(serializedObject);
            box.Add(distance);
            var follow = SG_InspectorElements.Bool(serializedObject, "followTransform", "Follow Transform");
            box.Add(follow);

//#SG_PRO_BEGIN
            box.Add(SG_InspectorElements.EnumPopup<TimeMode>(serializedObject, "timeMode", "Time Mode"));
//#SG_PRO_END
            box.Add(SG_InspectorElements.Float(serializedObject, "fadeInTime", "Fade In Time", "s", 0f, 0f));
            box.Add(SG_InspectorElements.Float(serializedObject, "fadeOutTime", "Fade Out Time", "s", 0f, 0f));
            box.Add(new PropertyField(serializedObject.FindProperty("output"), "Audio Output"));
            box.Add(SG_InspectorElements.String(serializedObject, "id", "Id"));

            void UpdateSpatial (bool on)
            {
                var d = on ? DisplayStyle.Flex : DisplayStyle.None;
//#SG_PRO_BEGIN
                occlusion.style.display = d;
//#SG_PRO_END
                doppler.style.display = d;
                distance.style.display = d;
                follow.style.display = d;
            }
            UpdateSpatial(Bool("spatialSound"));
            spatial.OnValueChanged += UpdateSpatial;
            root.Add(wrap);

//#SG_PRO_BEGIN
            // ----- Effect -----
            var effectWrap = SG_InspectorElements.FoldoutSection("Effect", out var effectBox);
            effectBox.Add(SG_InspectorElements.EffectBlock(serializedObject));
            root.Add(effectWrap);
//#SG_PRO_END

            // ----- Events -----
            var eventsWrap = SG_InspectorElements.FoldoutSection("Events", out var eventsBox);
            eventsBox.Add(new PropertyField(serializedObject.FindProperty("onPlay")));
            eventsBox.Add(new PropertyField(serializedObject.FindProperty("onComplete")));
            var loopCycle = new PropertyField(serializedObject.FindProperty("onLoopCycleComplete"));
            eventsBox.Add(loopCycle);
            eventsBox.Add(new PropertyField(serializedObject.FindProperty("onPause")));
            eventsBox.Add(new PropertyField(serializedObject.FindProperty("onPauseComplete")));
            eventsBox.Add(new PropertyField(serializedObject.FindProperty("onResume")));
            void UpdateLoopCycle (bool on) => loopCycle.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            UpdateLoopCycle(Bool("loop"));
            loop.OnValueChanged += UpdateLoopCycle;
            root.Add(eventsWrap);

            // ----- Playback -----
            var playbackWrap = SG_InspectorElements.FoldoutSection("Playback", out var playbackBox);
            playbackBox.Add(SG_InspectorElements.Bool(serializedObject, "playOnStart", "Play On Start"));
            root.Add(playbackWrap);

            root.Add(BuildTestButtons());
        }

        private VisualElement BuildTestButtons ()
        {
            var container = new VisualElement { style = { marginTop = 4 } };

            if (!Application.isPlaying)
            {
                var info = new Label("Enter Play Mode to test the emitter.");
                info.AddToClassList("info-box");
                container.Add(info);
                return container;
            }

            var emitter = (SoundEmitter)target;
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            row.Add(TestButton("▶ Play", () => emitter.Play(), "create-button"));
            row.Add(TestButton("❚❚ Pause", () => emitter.Pause(), "light-orange-button"));
            row.Add(TestButton("▶ Resume", () => emitter.Resume(), "light-orange-button"));
            row.Add(TestButton("■ Stop", () => emitter.Stop(), "light-orange-button"));
            container.Add(row);
            return container;
        }

        private Button TestButton (string text, System.Action onClick, string uss)
        {
            var button = new Button(onClick) { text = text };
            button.AddToClassList(uss);
            button.style.flexGrow = 1;
            button.style.height = 26;
            button.style.marginLeft = 2;
            button.style.marginRight = 2;
            return button;
        }

        private bool Bool (string prop) => serializedObject.FindProperty(prop).boolValue;

        // =====================================================================
        //  IMGUI (fallback)
        // =====================================================================
        public override void OnInspectorGUI ()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox(DESCRIPTION, MessageType.Info);

            EditorGUILayout.LabelField("Clip", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sfx"), new GUIContent("Sound"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("randomClip"));
            if (!Bool("randomClip"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("clipIndex"), new GUIContent("Clip Index"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            if (!Bool("randomVolume"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("volume"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("randomVolume"));
            if (Bool("randomVolume"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("volumeRange"), new GUIContent("Volume Range"));
            if (!Bool("randomPitch"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pitch"), new GUIContent("Pitch"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("randomPitch"));
            if (Bool("randomPitch"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pitchRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("loop"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playProbability"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spatialSound"));
            bool spatial = Bool("spatialSound");
            if (spatial)
            {
//#SG_PRO_BEGIN
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useOcclusion"));
//#SG_PRO_END
                EditorGUILayout.PropertyField(serializedObject.FindProperty("dopplerLevel"), new GUIContent("Doppler Level"));
                SG_InspectorElements.DistanceBlockIMGUI(serializedObject);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("followTransform"));
            }
//#SG_PRO_BEGIN
            EditorGUILayout.PropertyField(serializedObject.FindProperty("timeMode"), new GUIContent("Time Mode"));
//#SG_PRO_END
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeInTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeOutTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("output"), new GUIContent("Audio Output"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("id"));

//#SG_PRO_BEGIN
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Effect", EditorStyles.boldLabel);
            SG_InspectorElements.EffectBlockIMGUI(serializedObject);
//#SG_PRO_END

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onPlay"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onComplete"));
            if (Bool("loop"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onLoopCycleComplete"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onPause"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onPauseComplete"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onResume"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnStart"), new GUIContent("Play On Start"));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            if (Application.isPlaying)
            {
                var emitter = (SoundEmitter)target;
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("▶ Play")) emitter.Play();
                if (GUILayout.Button("❚❚ Pause")) emitter.Pause();
                if (GUILayout.Button("▶ Resume")) emitter.Resume();
                if (GUILayout.Button("■ Stop")) emitter.Stop();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test the emitter.", MessageType.Info);
            }
        }
    }
}
#endif
