#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
    [CustomEditor(typeof(PlaylistEmitter))]
    [CanEditMultipleObjects]
    public class PlaylistEmitterEditor : UnityEditor.Editor
    {
        private const string DESCRIPTION =
            "Configures a Playlist from the Inspector, exactly like the code API. Reference it to " +
            "call Play/Pause/Resume/Stop, and use its .Playlist property to call any Set* method at runtime.";

        public override VisualElement CreateInspectorGUI ()
            => SG_InspectorElements.BuildInspector(this, BuildBody);

        private void BuildBody (VisualElement root)
        {
            root.Add(SG_InspectorElements.Description(DESCRIPTION));

            // ----- Tracks -----
            var tracksWrap = SG_InspectorElements.FoldoutSection("Tracks", out var tracksBox);
            tracksBox.Add(new PropertyField(serializedObject.FindProperty("playlistTracks"), "Playlist Tracks"));
            root.Add(tracksWrap);

            // ----- Settings -----
            var wrap = SG_InspectorElements.FoldoutSection("Settings", out var box);
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
            box.Add(SG_InspectorElements.SliderRange(serializedObject, "pitch", "Pitch", -3f, 3f, 2));
            var loop = SG_InspectorElements.Bool(serializedObject, "loop", "Loop");
            box.Add(loop);
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
            eventsBox.Add(new PropertyField(serializedObject.FindProperty("onNextTrackStart")));
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

            root.Add(EmitterEditorUtility.TestButtons(
                Application.isPlaying ? (PlaylistEmitter)target : null,
                e => e.Play(), e => e.Pause(), e => e.Resume(), e => e.Stop()));
        }

        private bool Bool (string prop) => serializedObject.FindProperty(prop).boolValue;

        public override void OnInspectorGUI ()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox(DESCRIPTION, MessageType.Info);

            EditorGUILayout.LabelField("Tracks", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playlistTracks"), new GUIContent("Playlist Tracks"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            if (!Bool("randomVolume"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("volume"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("randomVolume"));
            if (Bool("randomVolume"))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("volumeRange"), new GUIContent("Volume Range"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pitch"), new GUIContent("Pitch"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("loop"));
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onNextTrackStart"));
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
            EmitterEditorUtility.TestButtonsIMGUI(
                Application.isPlaying ? (PlaylistEmitter)target : null,
                e => e.Play(), e => e.Pause(), e => e.Resume(), e => e.Stop());
        }
    }
}
#endif
