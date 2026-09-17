using System;
using UnityEditor;
using UnityEngine.UIElements;
using MelenitasDev.SoundsGood.Domain;
using UnityEngine;

namespace MelenitasDev.SoundsGood.Editor
{
    public class OutputTemplate : VisualElement
    {
        // ----- UI Elements
        private Label outputNameLabel => this.Q<Label>("OutputNameLabel");
        private Slider volumeSlider => this.Q<Slider>("VolumeSlider");
        private Label volumePercentageLabel => this.Q<Label>("VolumePercentageLabel");
        private VisualElement volumeLabel => this.Q<VisualElement>("VolumeLabel");
        private VisualElement exposeVolumeWarningLabel => this.Q<VisualElement>("ExposeVolumeWarningLabel");

        // ----- Fields
        private string outputName;
        private Action<string, float> onValueChange;

        // Built in code (not in the UXML) so a row only grows these controls when the window
        // actually passes the callbacks — Master never does, since it can't be renamed or removed.
        private TextField renameField;
        private Button renameButton;
        private Image renameIcon;
        private Action<string, string> onRename;
        private bool renaming;

        // ----- Public Methods
        public OutputTemplate (string outputName, float volume, Action<string, float> onValueChange,
            Action<string, string> onRename = null, Action<string> onRemove = null)
        {
            VisualTreeAsset asset = AssetLocator.Instance.OutputTemplate;
            asset.CloneTree(this);

            volumeSlider.RegisterValueChangedCallback(evt => OnSliderValueChange(evt.newValue));

            outputNameLabel.text = outputName;
            volumePercentageLabel.text = $"{volume * 100:F0}%";

            this.outputName = outputName;
            this.onValueChange = onValueChange;
            this.onRename = onRename;

            if (volume != -1)
            {
                volumeSlider.value = volume;
                SwitchVolumeLabel(true);
            }
            else
            {
                SwitchVolumeLabel(false);
            }

            BuildActions(onRemove);
        }

        public void SetBoldName ()
        {
            outputNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        }

        // ----- Private Methods
        private void BuildActions (Action<string> onRemove)
        {
            if (onRename == null && onRemove == null) return;

            VisualElement row = this.Q<VisualElement>("OutputElement") ?? this;

            if (onRename != null)
            {
                // Sits on top of the name label: the label hides while the field is shown.
                renameField = new TextField
                {
                    value = outputName,
                    tooltip = "Renaming an output breaks the references to it in your code.",
                    style = { width = Length.Percent(35), display = DisplayStyle.None, alignSelf = Align.Center }
                };
                renameField.AddToClassList("text-field");
                renameField.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) CommitRename();
                    else if (evt.keyCode == KeyCode.Escape) CancelRename();
                });
                row.Insert(1, renameField);

                renameButton = new Button(ToggleRename)
                {
                    tooltip = "Rename this output",
                    // Left margin keeps it clear of the volume percentage.
                    style = { width = 26, height = 22, alignSelf = Align.Center, marginLeft = 10, marginRight = 3 }
                };
                renameButton.AddToClassList("orange-button");

                // Unity's own edit icon reads as "rename" at a glance, and unlike a text glyph it
                // can't end up substituted by an emoji in the editor font.
                Texture icon = FindIcon("editicon.sml", "d_editicon.sml", "Edit", "d_Edit");
                if (icon != null)
                {
                    renameIcon = new Image
                    {
                        image = icon,
                        scaleMode = ScaleMode.ScaleToFit,
                        style = { width = 13, height = 13, alignSelf = Align.Center }
                    };
                    renameButton.Add(renameIcon);
                }
                else renameButton.text = "Edit";

                row.Add(renameButton);
            }

            if (onRemove == null) return;

            var removeButton = new Button(() => onRemove(outputName))
            {
                text = "X",
                tooltip = "Delete this output",
                style = { width = 26, height = 22, alignSelf = Align.Center }
            };
            removeButton.AddToClassList("light-orange-button");
            row.Add(removeButton);
        }

        private void ToggleRename ()
        {
            if (renaming) CommitRename();
            else BeginRename();
        }

        private void BeginRename ()
        {
            renaming = true;
            renameField.SetValueWithoutNotify(outputName);
            renameField.style.display = DisplayStyle.Flex;
            outputNameLabel.style.display = DisplayStyle.None;
            SetRenameButtonConfirming(true);

            // Focus only lands once the field has been laid out (it was display:none until now).
            renameField.schedule.Execute(() =>
            {
                renameField.Focus();
                renameField.SelectAll();
            }).StartingIn(0);
        }

        private void CommitRename ()
        {
            if (!renaming) return;

            string newName = renameField.value?.Trim() ?? "";
            CancelRename();

            if (string.IsNullOrEmpty(newName) || newName == outputName) return;

            onRename?.Invoke(outputName, newName);
        }

        private void CancelRename ()
        {
            renaming = false;
            renameField.style.display = DisplayStyle.None;
            outputNameLabel.style.display = DisplayStyle.Flex;
            SetRenameButtonConfirming(false);
        }

        /// <summary> Swaps the edit icon for a plain "OK" while the name is being typed. </summary>
        private void SetRenameButtonConfirming (bool confirming)
        {
            if (renameIcon != null)
            {
                renameIcon.style.display = confirming ? DisplayStyle.None : DisplayStyle.Flex;
                renameButton.text = confirming ? "OK" : "";
                return;
            }

            renameButton.text = confirming ? "OK" : "Edit";
        }

        /// <summary>
        /// First of <paramref name="names"/> that exists as a built-in editor icon, or null.
        /// FindTexture returns null quietly when a name is missing, so trying several keeps this
        /// working if Unity renames one of them.
        /// </summary>
        private static Texture FindIcon (params string[] names)
        {
            foreach (string name in names)
            {
                Texture texture = EditorGUIUtility.FindTexture(name);
                if (texture != null) return texture;
            }

            return null;
        }

        private void OnSliderValueChange (float volume)
        {
            volumePercentageLabel.text = $"{volume * 100:F0}%";
            onValueChange?.Invoke(outputName, volume);
        }

        private void SwitchVolumeLabel (bool exposed)
        {
            volumeLabel.style.display = exposed ? DisplayStyle.Flex : DisplayStyle.None;
            exposeVolumeWarningLabel.style.display = exposed ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
