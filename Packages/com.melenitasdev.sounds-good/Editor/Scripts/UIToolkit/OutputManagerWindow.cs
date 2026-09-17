using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;
using MelenitasDev.SoundsGood.Domain;

namespace MelenitasDev.SoundsGood.Editor
{
    public class OutputManagerWindow : EditorWindow
    {
        [SerializeField] private VisualTreeAsset tree;

        // Main Label
        private ListView outputsList;
        private VisualElement masterOutputContainer;
        private VisualElement outputsListContainer;
        private VisualElement mainLabel;
        private Button openCreateLabelButton;
        private Button refreshButton;
        private Button mainOpenAudioMixerButton;

        // Create Label
        private Button backButton;
        private Button openAudioMixerButton;
        private Button createOutputButton;
        private VisualElement createOutputLabel;
        private VisualElement createGroupImage;
        private VisualElement renameGroupImage;
        private VisualElement exposeVolumeImage;
        private VisualElement locateExposedParamImage;
        private VisualElement renameExposedParamImage;
        private VisualElement instructionsList;
        private Label instructionsTitle;

        // Automatic creation (built in code, so the manual guide in the UXML stays untouched
        // and can still be shown as a fallback).
        private VisualElement autoCreateBlock;
        private VisualElement autoHeaderRow;
        private TextField newOutputNameField;
        private Button autoCreateButton;
        private Label autoMessageLabel;

        // The back button's original row in the UXML, so it can be put back for the manual guide.
        private VisualElement backButtonHome;

        // Set when the automatic path fails at runtime, so the window falls back to the guide.
        private bool forceManualMode;

        private enum WindowLabel
        {
            Main,
            CreateOutput
        }

        [MenuItem("Tools/Sounds Good/Output Manager", false, 52)]
        public static void ShowWindow ()
        {
            var window = GetWindow(typeof(OutputManagerWindow));
            window.titleContent = new GUIContent("Output Manager");
            window.minSize = new Vector2(360, 420);
        }

        void CreateGUI ()
        {
            tree.CloneTree(rootVisualElement);

            openCreateLabelButton = rootVisualElement.Q<Button>("OpenCreateLabelButton");
            refreshButton = rootVisualElement.Q<Button>("RefreshButton");
            mainOpenAudioMixerButton = rootVisualElement.Q<Button>("MainOpenAudioMixerButton");
            masterOutputContainer = rootVisualElement.Q<VisualElement>("MasterOutputContainer");
            outputsList = rootVisualElement.Q<ListView>("OutputsList");
            outputsListContainer = outputsList.Q<VisualElement>("unity-content-container");
            backButton = rootVisualElement.Q<Button>("BackButton");
            backButtonHome = backButton?.parent;
            openAudioMixerButton = rootVisualElement.Q<Button>("OpenAudioMixerButton");
            createOutputButton = rootVisualElement.Q<Button>("CreateOutputButton");
            mainLabel = rootVisualElement.Q<VisualElement>("MainLabel");
            createOutputLabel = rootVisualElement.Q<VisualElement>("CreateOutputLabel");
            createGroupImage = rootVisualElement.Q<VisualElement>("CreateGroupImage");
            renameGroupImage = rootVisualElement.Q<VisualElement>("RenameGroupImage");
            exposeVolumeImage = rootVisualElement.Q<VisualElement>("ExposeVolumeImage");
            locateExposedParamImage = rootVisualElement.Q<VisualElement>("LocateExposedParamImage");
            renameExposedParamImage = rootVisualElement.Q<VisualElement>("RenameExposedParamImage");
            instructionsList = rootVisualElement.Q<ListView>("InstructionsList");
            instructionsTitle = rootVisualElement.Q<Label>("Label");
            rootVisualElement.Q<ListView>("InstructionsList").Q<VisualElement>("unity-content-container")
                .Add(rootVisualElement.Q<VisualElement>("Instructions"));

            BuildAutoCreateBlock();
            RegisterEvents();
            ApplyCreationMode();

            ChangeLabel(WindowLabel.Main);
        }

        void OnDisable ()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        // ----- Automatic creation UI

        private void BuildAutoCreateBlock ()
        {
            autoCreateBlock = new VisualElement { style = { marginTop = 4, marginLeft = 6, marginRight = 6 } };

            // The back button is moved in here by ApplyCreationMode, so it sits on the same line as
            // the title instead of floating on a row of its own above it.
            autoHeaderRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 }
            };

            var title = new Label("Name your new output:");
            title.AddToClassList("subtitle");
            title.style.paddingLeft = 0;
            // Grows so the back button, added after it, is pushed to the far right of the row.
            title.style.flexGrow = 1;
            autoHeaderRow.Add(title);

            autoCreateBlock.Add(autoHeaderRow);

            newOutputNameField = new TextField
            {
                tooltip = "This becomes the value you use in code (Output.YourName)."
            };
            newOutputNameField.AddToClassList("text-field");
            newOutputNameField.style.height = 31;
            newOutputNameField.RegisterValueChangedCallback(evt => OnChangeNewOutputName(evt.newValue));
            autoCreateBlock.Add(newOutputNameField);

            autoCreateButton = new Button(OnAutoCreateClicked) { text = "CREATE" };
            autoCreateButton.AddToClassList("create-button");
            autoCreateButton.style.height = 34;
            autoCreateButton.style.marginTop = 8;
            autoCreateBlock.Add(autoCreateButton);

            var hint = new Label("Creates the group in the Audio Mixer and exposes its volume for you.");
            hint.AddToClassList("info-box");
            autoCreateBlock.Add(hint);

            autoMessageLabel = new Label { style = { display = DisplayStyle.None } };
            autoMessageLabel.AddToClassList("info-box");
            autoCreateBlock.Add(autoMessageLabel);

            // Right below the back button row, above the manual guide.
            createOutputLabel.Insert(1, autoCreateBlock);

            UpdateAutoCreateButton();
        }

        /// <summary> Automatic block or manual guide, depending on what this Unity version allows. </summary>
        private void ApplyCreationMode ()
        {
            bool automatic = MixerOutputAuthoring.IsSupported && !forceManualMode;

            autoCreateBlock.style.display = automatic ? DisplayStyle.Flex : DisplayStyle.None;

            DisplayStyle manual = automatic ? DisplayStyle.None : DisplayStyle.Flex;
            if (instructionsList != null) instructionsList.style.display = manual;
            if (instructionsTitle != null) instructionsTitle.style.display = manual;
            if (openAudioMixerButton != null) openAudioMixerButton.style.display = manual;
            if (createOutputButton != null) createOutputButton.style.display = manual;

            // Automatic mode puts the back button on the title's line and drops its original row —
            // which grows to fill the panel and would otherwise leave a big gap where the manual
            // instructions used to be.
            if (automatic) autoHeaderRow.Add(backButton);
            else backButtonHome?.Insert(0, backButton);

            if (backButtonHome != null)
                backButtonHome.style.display = automatic ? DisplayStyle.None : DisplayStyle.Flex;

            openCreateLabelButton.text = automatic ? "Create New Output" : "Create New Output Guide";
        }

        private void OnChangeNewOutputName (string newName)
        {
            if (EditorHelper.IsTagValid(newName)) newOutputNameField.RemoveFromClassList("text-field-error");
            else newOutputNameField.AddToClassList("text-field-error");

            UpdateAutoCreateButton();
        }

        private void UpdateAutoCreateButton ()
        {
            bool valid = EditorHelper.IsTagValid(newOutputNameField.value);
            autoCreateButton.SetEnabled(valid);
            if (valid) autoCreateButton.AddToClassList("create-button");
            else autoCreateButton.RemoveFromClassList("create-button");
        }

        private void OnAutoCreateClicked ()
        {
            string name = newOutputNameField.value?.Trim() ?? "";

            if (!EditorHelper.IsTagValid(name))
            {
                ShowAutoMessage("Invalid name. Use only letters and numbers, and don't start with a number.");
                return;
            }

            if (OutputExists(name))
            {
                ShowAutoMessage($"An output named '{name}' already exists.");
                return;
            }

            if (!MixerOutputAuthoring.TryCreateOutput(name, out string error))
            {
                ShowAutoMessage(error);
                // Something in Unity's internals didn't answer: let the user finish it by hand.
                forceManualMode = true;
                ApplyCreationMode();
                return;
            }

            EditorHelper.ReloadOutputsDatabase();

            newOutputNameField.SetValueWithoutNotify("");
            newOutputNameField.RemoveFromClassList("text-field-error");
            UpdateAutoCreateButton();
            autoMessageLabel.style.display = DisplayStyle.None;

            ChangeLabel(WindowLabel.Main);
        }

        private void ShowAutoMessage (string message)
        {
            autoMessageLabel.text = message;
            autoMessageLabel.style.display = DisplayStyle.Flex;
        }

        private bool OutputExists (string name)
        {
            var collection = AssetLocator.Instance.OutputDataCollection;
            if (collection == null) return false;

            return collection.Outputs.Any(output =>
                string.Equals(output.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        // ----- Rename / delete

        private void RenameOutput (string oldName, string newName)
        {
            if (!EditorHelper.IsTagValid(newName))
            {
                EditorUtility.DisplayDialog("Invalid name",
                    "Use only letters and numbers, and don't start with a number.", "Ok");
                return;
            }

            if (OutputExists(newName))
            {
                EditorUtility.DisplayDialog("Name already used",
                    $"An output named '{newName}' already exists.", "Ok");
                return;
            }

            if (!EditorUtility.DisplayDialog("Rename output",
                    $"Rename '{oldName}' to '{newName}'?\n\n" +
                    $"References to Output.{oldName} in your code will stop compiling.",
                    "Rename", "Cancel"))
                return;

            AudioMixerGroup group = AssetLocator.Instance.OutputDataCollection.GetOutput(oldName);
            if (group == null) return;

            if (!MixerOutputAuthoring.TryRenameOutput(group, newName, out string error))
            {
                EditorUtility.DisplayDialog("Couldn't rename the output", error, "Ok");
                return;
            }

            EditorHelper.ReloadOutputsDatabase();
            DrawOutputs();
        }

        private void RemoveOutput (string name)
        {
            if (!EditorUtility.DisplayDialog("Delete output",
                    $"Do you want to remove '{name}' permanently?\n\n" +
                    $"References to Output.{name} in your code will stop compiling, and any audio " +
                    "routed through it will fall back to the default output.",
                    "Delete", "Cancel"))
                return;

            AudioMixerGroup group = AssetLocator.Instance.OutputDataCollection.GetOutput(name);
            if (group == null) return;

            if (!MixerOutputAuthoring.TryDeleteOutput(group, out string error))
            {
                EditorUtility.DisplayDialog("Couldn't delete the output", error, "Ok");
                return;
            }

            EditorHelper.ReloadOutputsDatabase();
            DrawOutputs();
        }

        // ----- Existing flow

        private void RegisterEvents ()
        {
            // Buttons
            openCreateLabelButton.clicked += OpenCreateLabel;
            refreshButton.clicked += CreateNewOutput;
            backButton.clicked += () => ChangeLabel(WindowLabel.Main);
            mainOpenAudioMixerButton.clicked += OpenAudioMixer;
            openAudioMixerButton.clicked += OpenAudioMixer;
            createOutputButton.clicked += CreateNewOutput;

            // Popup images
            createGroupImage.RegisterCallback<MouseUpEvent>(evt => {
                if (evt.button == 0) ImagePopupWindow.Show(AssetLocator.Instance.CreateGroupImage);
            });
            renameGroupImage.RegisterCallback<MouseUpEvent>(evt => {
                if (evt.button == 0) ImagePopupWindow.Show(AssetLocator.Instance.RenameGroupImage);
            });
            exposeVolumeImage.RegisterCallback<MouseUpEvent>(evt => {
                if (evt.button == 0) ImagePopupWindow.Show(AssetLocator.Instance.ExposeVolumeImage);
            });
            locateExposedParamImage.RegisterCallback<MouseUpEvent>(evt => {
                if (evt.button == 0) ImagePopupWindow.Show(AssetLocator.Instance.LocateExposedParamImage);
            });
            renameExposedParamImage.RegisterCallback<MouseUpEvent>(evt => {
                if (evt.button == 0) ImagePopupWindow.Show(AssetLocator.Instance.RenameExposedParamImage);
            });

            // Play Mode
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged (PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                openCreateLabelButton.SetEnabled(false);
                refreshButton.SetEnabled(false);
                mainOpenAudioMixerButton.SetEnabled(false);
                openAudioMixerButton.SetEnabled(false);
                createOutputButton.SetEnabled(false);
                autoCreateButton.SetEnabled(false);
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                openCreateLabelButton.SetEnabled(true);
                refreshButton.SetEnabled(true);
                mainOpenAudioMixerButton.SetEnabled(true);
                openAudioMixerButton.SetEnabled(true);
                createOutputButton.SetEnabled(true);
                UpdateAutoCreateButton();
            }
        }

        private void DrawOutputs ()
        {
            outputsListContainer.hierarchy.Clear();
            masterOutputContainer.hierarchy.Clear();

            var outputCollection = AssetLocator.Instance.OutputDataCollection;
            bool canAuthor = MixerOutputAuthoring.IsSupported;
            int i = 0;
            foreach (OutputData outputData in outputCollection.Outputs)
            {
                bool exposed = outputData.Output.audioMixer
                    .GetFloat(outputData.Name.Replace(" ", ""), out float db);

                float volume = SoundsGoodManager.GetSavedOutputVolume(outputData.Name);

                // Master is the mixer's root: it can't be renamed or removed.
                bool isMaster = i == 0;

                Action<string, string> onRename = null;
                Action<string> onRemove = null;
                if (canAuthor && !isMaster)
                {
                    onRename = RenameOutput;
                    onRemove = RemoveOutput;
                }

                var outputTemplate = new OutputTemplate(outputData.Name, exposed ? volume : -1,
                    ChangeOutputVolume, onRename, onRemove);

                if (isMaster)
                {
                    masterOutputContainer.Add(outputTemplate);
                    outputTemplate.SetBoldName();
                }
                else outputsListContainer.Add(outputTemplate);
                i++;
            }
        }

        private void ChangeOutputVolume (string outputName, float volume)
        {
            SoundsGoodManager.ChangeOutputVolume(outputName, volume);
        }

        private void ChangeLabel (WindowLabel windowLabel)
        {
            mainLabel.style.display =
                windowLabel == WindowLabel.Main ? DisplayStyle.Flex : DisplayStyle.None;
            createOutputLabel.style.display =
                windowLabel == WindowLabel.CreateOutput ? DisplayStyle.Flex : DisplayStyle.None;

            if (windowLabel == WindowLabel.Main) DrawOutputs();
            else ApplyCreationMode();
        }

        private void OpenCreateLabel ()
        {
            ChangeLabel(WindowLabel.CreateOutput);
        }

        private void OpenAudioMixer ()
        {
            AudioMixer mixer = AssetLocator.Instance.MasterAudioMixer;

            if (mixer == null)
            {
                Debug.LogWarning("AudioMixer no asignado.");
                return;
            }

            EditorApplication.ExecuteMenuItem("Window/Audio/Audio Mixer");

            Selection.activeObject = mixer;

            EditorGUIUtility.PingObject(mixer);
        }

        private void CreateNewOutput ()
        {
            EditorHelper.ReloadOutputsDatabase();
            ChangeLabel(WindowLabel.Main);
        }
    }
}
