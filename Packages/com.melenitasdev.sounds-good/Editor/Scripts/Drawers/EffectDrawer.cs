using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
    [CustomPropertyDrawer(typeof(Effect))]
    public class EffectDrawer : PropertyDrawer
    {
        // ----- Fields
        private string[] names;
        private Effect[] values;
        private bool cached;

        private static readonly Dictionary<string, string> searchFilters = new Dictionary<string, string>();
        private static readonly Dictionary<string, bool> searchVisible = new Dictionary<string, bool>();
        private static readonly Dictionary<string, string> lastSelectedValues = new Dictionary<string, string>();
        private static bool playHooked;

        // ----- Public Methods
        public override VisualElement CreatePropertyGUI (SerializedProperty property)
        {
            Cache();

            SerializedProperty stringProp = property.FindPropertyRelative("value");
            string current = stringProp.stringValue;

            int originalIndex = Array.FindIndex(values, v => v.ToString() == current);
            if (originalIndex < 0)
            {
                int nullIndexByName = Array.FindIndex(names, n => n == "Null");
                originalIndex = nullIndexByName >= 0 ? nullIndexByName : 0;
            }

            string key = property.propertyPath;
            if (!searchFilters.TryGetValue(key, out string filter))
                filter = string.Empty;

            if (!searchVisible.TryGetValue(key, out bool isVisible))
                isVisible = false;

            var root = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row }
            };

            var popupContainer = new VisualElement
            {
                style =
                {
                    flexGrow = 1f,
                    flexShrink = 1f
                }
            };

            var buttonsContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexShrink = 0f
                }
            };

            var searchButton = new Button { text = "🔍" };

            var searchField = new TextField
            {
                label = string.Empty,
                tooltip = "Filter",
                style =
                {
                    width = 90,
                    flexShrink = 0f,
                    marginLeft = 2,
                    marginRight = 2
                }
            };
            searchField.value = filter;

            var closeButton = new Button
            {
                text = "x",
                style = { flexShrink = 0f }
            };

            searchField.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            closeButton.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            searchButton.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;

            var allNames = names.ToList();
            var filteredIndices = new List<int>();
            var filteredNames = new List<string>();

            void RebuildFilteredLists (string currentFilter)
            {
                filteredIndices.Clear();

                if (string.IsNullOrEmpty(currentFilter))
                {
                    for (int i = 0; i < allNames.Count; i++)
                        filteredIndices.Add(i);
                }
                else
                {
                    string lower = currentFilter.ToLowerInvariant();
                    for (int i = 0; i < allNames.Count; i++)
                    {
                        string nameLower = allNames[i].ToLowerInvariant();
                        string tagLower = values[i].ToString().ToLowerInvariant();

                        if (nameLower.StartsWith(lower) || tagLower.StartsWith(lower))
                            filteredIndices.Add(i);
                    }

                    if (filteredIndices.Count == 0)
                    {
                        int nullIndex = Array.FindIndex(names, n => n == "Null");
                        if (nullIndex < 0) nullIndex = 0;
                        filteredIndices.Add(nullIndex);
                    }
                }

                filteredNames.Clear();
                foreach (int idx in filteredIndices)
                    filteredNames.Add(allNames[idx]);
            }

            RebuildFilteredLists(filter);

            int displayedIndex = filteredIndices.IndexOf(originalIndex);
            if (displayedIndex < 0) displayedIndex = 0;

            var popup = new PopupField<string>(filteredNames, Mathf.Clamp(displayedIndex, 0, filteredNames.Count - 1))
            {
                label = property.displayName
            };
            popup.showMixedValue = stringProp.hasMultipleDifferentValues;

            popup.RegisterValueChangedCallback(evt =>
            {
                int originalNameIndex = Array.IndexOf(names, evt.newValue);
                if (originalNameIndex >= 0)
                {
                    stringProp.stringValue = values[originalNameIndex].ToString();
                    property.serializedObject.ApplyModifiedProperties();
                    popup.showMixedValue = false;
                }
            });

            void ForceCloseSearchUI ()
            {
                if (!searchVisible.TryGetValue(key, out bool v) || !v) return;

                searchVisible[key] = false;
                searchFilters[key] = string.Empty;

                searchField.SetValueWithoutNotify(string.Empty);
                searchField.style.display = DisplayStyle.None;
                closeButton.style.display = DisplayStyle.None;
                searchButton.style.display = DisplayStyle.Flex;

                RebuildFilteredLists(string.Empty);
                popup.choices = filteredNames;

                string currentValue = stringProp.stringValue;
                int currentIndex = Array.FindIndex(values, x => x.ToString() == currentValue);
                if (currentIndex < 0)
                {
                    currentIndex = Array.FindIndex(names, n => n == "Null");
                    if (currentIndex < 0) currentIndex = 0;
                }

                string currentName = names[currentIndex];
                if (!filteredNames.Contains(currentName))
                    currentName = filteredNames[0];

                popup.SetValueWithoutNotify(currentName);
            }

            void OnPlayModeChanged (PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.ExitingEditMode ||
                    state == PlayModeStateChange.EnteredPlayMode)
                {
                    ForceCloseSearchUI();
                }
            }

            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            root.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            });

            searchButton.clicked += () =>
            {
                searchVisible[key] = true;
                searchField.style.display = DisplayStyle.Flex;
                closeButton.style.display = DisplayStyle.Flex;
                searchButton.style.display = DisplayStyle.None;
                searchField.Focus();
            };

            searchField.RegisterValueChangedCallback(evt =>
            {
                string newFilter = evt.newValue ?? string.Empty;
                searchFilters[key] = newFilter;

                RebuildFilteredLists(newFilter);
                popup.choices = filteredNames;

                string currentValue = stringProp.stringValue;
                int currentIndex = Array.FindIndex(values, v => v.ToString() == currentValue);
                if (currentIndex < 0)
                {
                    currentIndex = Array.FindIndex(names, n => n == "Null");
                    if (currentIndex < 0) currentIndex = 0;
                }

                string currentName = names[currentIndex];
                if (!filteredNames.Contains(currentName))
                    currentName = filteredNames[0];

                popup.value = currentName;
            });

            closeButton.clicked += () => { ForceCloseSearchUI(); };

            popupContainer.Add(popup);
            buttonsContainer.Add(searchButton);
            buttonsContainer.Add(searchField);
            buttonsContainer.Add(closeButton);
            root.Add(popupContainer);
            root.Add(buttonsContainer);

            return root;
        }

        public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {
            Cache();

            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty stringProp = property.FindPropertyRelative("value");
            string key = property.propertyPath;
            string current = stringProp.stringValue;

            int originalIndex = Array.FindIndex(values, v => v.ToString() == current);
            if (originalIndex < 0)
            {
                int nullIndexByName = Array.FindIndex(names, n => n == "Null");
                originalIndex = nullIndexByName >= 0 ? nullIndexByName : 0;
            }

            if (!searchFilters.TryGetValue(key, out string filter))
                filter = string.Empty;

            if (!searchVisible.TryGetValue(key, out bool isVisible))
                isVisible = false;

            float buttonWidth = 20f;
            float spacing = 2f;
            float searchWidth = isVisible ? 90f : 0f;
            float closeWidth = isVisible ? 20f : 0f;

            float popupWidth = position.width - buttonWidth - spacing - searchWidth -
                               (isVisible ? spacing + closeWidth : 0f);
            if (popupWidth < 50f) popupWidth = 50f;

            Rect popupRect = new Rect(position.x, position.y, popupWidth, position.height);
            Rect searchButtonRect = new Rect(popupRect.xMax + spacing, position.y, buttonWidth, position.height);
            Rect searchRect = new Rect(searchButtonRect.xMax + spacing, position.y, searchWidth, position.height);
            Rect closeRect = new Rect(searchRect.xMax + spacing, position.y, closeWidth, position.height);

            var filteredIndices = new List<int>();
            if (string.IsNullOrEmpty(filter))
            {
                for (int i = 0; i < names.Length; i++)
                    filteredIndices.Add(i);
            }
            else
            {
                string lower = filter.ToLowerInvariant();
                for (int i = 0; i < names.Length; i++)
                {
                    string nameLower = names[i].ToLowerInvariant();
                    string tagLower = values[i].ToString().ToLowerInvariant();

                    if (nameLower.StartsWith(lower) || tagLower.StartsWith(lower))
                        filteredIndices.Add(i);
                }

                if (filteredIndices.Count == 0)
                {
                    int nullIndex = Array.FindIndex(names, n => n == "Null");
                    if (nullIndex < 0) nullIndex = 0;
                    filteredIndices.Add(nullIndex);
                }
            }

            string[] displayedNames = filteredIndices.Select(i => names[i]).ToArray();

            int displayedIndex = filteredIndices.IndexOf(originalIndex);
            if (displayedIndex < 0) displayedIndex = 0;

            if (!isVisible)
            {
                if (GUI.Button(searchButtonRect, new GUIContent("🔍", "Show search")))
                {
                    searchVisible[key] = true;
                    isVisible = true;
                    GUI.FocusControl(null);
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                string newFilter = EditorGUI.TextField(searchRect, GUIContent.none, filter);
                if (EditorGUI.EndChangeCheck())
                {
                    searchFilters[key] = newFilter ?? string.Empty;
                    filter = searchFilters[key];
                }

                if (GUI.Button(closeRect, new GUIContent("x", "Close search")))
                {
                    int commitOriginalIndex =
                        filteredIndices[Mathf.Clamp(displayedIndex, 0, filteredIndices.Count - 1)];
                    string commitValue = values[commitOriginalIndex].ToString();
                    stringProp.stringValue = commitValue;
                    lastSelectedValues[key] = commitValue;
                    property.serializedObject.ApplyModifiedProperties();

                    searchVisible[key] = false;
                    isVisible = false;
                    searchFilters[key] = string.Empty;
                    filter = string.Empty;

                    GUI.FocusControl(null);
                }
            }

            if (isVisible && filteredIndices.Count > 0)
            {
                int autoOriginalIndex = filteredIndices[Mathf.Clamp(displayedIndex, 0, filteredIndices.Count - 1)];
                string autoValue = values[autoOriginalIndex].ToString();

                if (stringProp.stringValue != autoValue)
                {
                    stringProp.stringValue = autoValue;
                    lastSelectedValues[key] = autoValue;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }

            bool previousMixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = stringProp.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int selectedDisplayedIndex = EditorGUI.Popup(popupRect, label.text, displayedIndex, displayedNames);
            EditorGUI.showMixedValue = previousMixed;
            if (EditorGUI.EndChangeCheck())
            {
                if (selectedDisplayedIndex >= 0 && selectedDisplayedIndex < filteredIndices.Count)
                {
                    int selectedOriginalIndex = filteredIndices[selectedDisplayedIndex];
                    string newValue = values[selectedOriginalIndex].ToString();

                    stringProp.stringValue = newValue;
                    lastSelectedValues[key] = newValue;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUI.EndProperty();
        }

        // ----- Private Methods
        private void Cache ()
        {
            if (cached) return;

            var fields = typeof(Effect)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(Effect))
                .OrderBy(f => f.Name == "Null" ? 0 : 1)
                .ThenBy(f => f.Name);

            names = fields.Select(f => f.Name).ToArray();
            values = fields.Select(f => (Effect)f.GetValue(null)).ToArray();

            if (names.Length == 0)
            {
                names = new[] { "-----" };
                values = new[] { new Effect(string.Empty) };
            }

            cached = true;

            EnsurePlayHook();
        }

        private void EnsurePlayHook ()
        {
            if (playHooked) return;
            playHooked = true;

            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode ||
                    state == PlayModeStateChange.EnteredPlayMode)
                {
                    searchVisible.Clear();
                    searchFilters.Clear();
                }
            };
        }
    }
}