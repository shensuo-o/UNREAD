#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
    /// <summary>
    /// Shared Play/Pause/Resume/Stop test controls for the emitter inspectors. Pass the target
    /// when in Play Mode, or null to show the "enter play mode" hint instead.
    /// </summary>
    internal static class EmitterEditorUtility
    {
        public static VisualElement TestButtons<T> (T target,
            Action<T> play, Action<T> pause, Action<T> resume, Action<T> stop)
        {
            var container = new VisualElement { style = { marginTop = 4 } };

            if (target == null)
            {
                var info = new Label("Enter Play Mode to test the emitter.");
                info.AddToClassList("info-box");
                container.Add(info);
                return container;
            }

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            row.Add(Button("▶ Play", () => play(target), "create-button"));
            row.Add(Button("❚❚ Pause", () => pause(target), "light-orange-button"));
            row.Add(Button("▶ Resume", () => resume(target), "light-orange-button"));
            row.Add(Button("■ Stop", () => stop(target), "light-orange-button"));
            container.Add(row);
            return container;
        }

        private static Button Button (string text, Action onClick, string uss)
        {
            var button = new Button(onClick) { text = text };
            button.AddToClassList(uss);
            button.style.flexGrow = 1;
            button.style.height = 26;
            button.style.marginLeft = 2;
            button.style.marginRight = 2;
            return button;
        }

        public static void TestButtonsIMGUI<T> (T target,
            Action<T> play, Action<T> pause, Action<T> resume, Action<T> stop)
        {
            if (target == null)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test the emitter.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("▶ Play")) play(target);
            if (GUILayout.Button("❚❚ Pause")) pause(target);
            if (GUILayout.Button("▶ Resume")) resume(target);
            if (GUILayout.Button("■ Stop")) stop(target);
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
