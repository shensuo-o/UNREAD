using System;
using System.Linq;
using MelenitasDev.SoundsGood.Domain;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
    public class OcclusionMaterialCreatorWindow : EditorWindow
    {
        private const string SG_LOGO_PATH =
            "Packages/com.melenitasdev.sounds-good/Runtime/Graphics/Sounds Good Logo.png";
        private const string MELENITAS_LOGO_PATH =
            "Packages/com.melenitasdev.sounds-good/Runtime/Graphics/Unity Splash Screen.png";

        private static readonly Color HeaderBorder = new Color(1f, 0.722f, 0.467f);
        private static readonly Color HeaderTint = new Color(0.529f, 0.251f, 0f, 0.13f);
        private static readonly Color FooterBorder = new Color(0.565f, 0.4f, 0.247f);
        private static readonly Color FooterTint = new Color(0.208f, 0.208f, 0.208f, 0.47f);
        private static readonly Color CardBorder = new Color(0.424f, 0.314f, 0.216f);
        private static readonly Color CardBackground = new Color(0.180f, 0.180f, 0.180f);

        private AudioOcclusionMaterial draft;

        private Button createTabButton;
        private Button manageTabButton;
        private VisualElement createScroll;
        private VisualElement manageScroll;

        private TextField tagField;
        private Button createButton;
        private VisualElement densityContainer;
        private Label messageLabel;
        private VisualElement listContainer;
        private readonly System.Collections.Generic.List<AudioOcclusionMaterial> editClones = new();

        private ObjectField clipField;
        private Button previewButton;
        private GameObject previewGameObject;
        private AudioSource previewSource;
        private AudioLowPassFilter previewLowPass;
        private bool isPreviewing;

        [MenuItem("Tools/Sounds Good/Occlusion Material Creator", false, 54)]
        public static void ShowWindow ()
        {
            var window = GetWindow<OcclusionMaterialCreatorWindow>();
            window.titleContent = new GUIContent("Occlusion Material Creator");
            window.minSize = new Vector2(370, 480);
        }

        void OnEnable ()
        {
            CreateDraft();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        void OnDisable ()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            StopPreview();
            DestroyEditClones();
            if (previewGameObject != null) DestroyImmediate(previewGameObject);
            if (draft != null && !AssetDatabase.Contains(draft)) DestroyImmediate(draft);
        }

        // Stop the preview across play-mode transitions (see EffectCreatorWindow for the rationale).
        private void OnPlayModeChanged (PlayModeStateChange change) => StopPreview();

        void CreateGUI ()
        {
            var root = rootVisualElement;
            SG_InspectorElements.LoadStyles(root);
            root.style.flexGrow = 1;
            SetPadding(root, 10);

            root.Add(BuildHeader());
            root.Add(BuildTabs());

            var content = new VisualElement { style = { flexGrow = 1, paddingTop = 5, paddingBottom = 5, paddingLeft = 5, paddingRight = 5 } };
            SetBorder(content, HeaderBorder, 2);
            SetRadius(content, 5);
            root.Add(content);

            createScroll = new ScrollView { style = { flexGrow = 1 } };
            manageScroll = new ScrollView { style = { flexGrow = 1, display = DisplayStyle.None } };
            content.Add(createScroll);
            content.Add(manageScroll);

            BuildCreatePanel(createScroll);
            BuildManagePanel(manageScroll);

            root.Add(BuildFooter());

            SetTab(create: true);
            RefreshList();
        }

        // ================= Header / Tabs / Footer =================
        private VisualElement BuildHeader ()
        {
            var header = new VisualElement
            {
                style =
                {
                    flexGrow = 0, flexShrink = 0, minHeight = 100,
                    flexDirection = FlexDirection.Column,
                    justifyContent = Justify.Center, alignItems = Align.Center,
                    backgroundColor = HeaderTint
                }
            };
            SetBorder(header, HeaderBorder, 4);
            SetRadius(header, 5);

            var title = new Label("OCCLUSION MATERIALS");
            title.AddToClassList("title");
            header.Add(title);

            var subtitle = new Label("Make your occlusion realistic")
            {
                style =
                {
                    color = new Color(1f, 0.871f, 0.702f),
                    unityTextAlign = TextAnchor.MiddleCenter,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 15, height = 14, marginTop = 2, marginRight = 2
                }
            };
            header.Add(subtitle);

            return header;
        }

        private VisualElement BuildTabs ()
        {
            var tabs = new VisualElement
            {
                style = { flexGrow = 0, flexShrink = 0, flexDirection = FlexDirection.Row, justifyContent = Justify.Center, marginTop = 5, marginBottom = 5, marginLeft = 5, marginRight = 5 }
            };

            createTabButton = BuildTabButton("Create", () => SetTab(true));
            manageTabButton = BuildTabButton("Manage", () => SetTab(false));

            tabs.Add(createTabButton);
            tabs.Add(manageTabButton);
            return tabs;
        }

        private void SetTab (bool create)
        {
            createScroll.style.display = create ? DisplayStyle.Flex : DisplayStyle.None;
            manageScroll.style.display = create ? DisplayStyle.None : DisplayStyle.Flex;

            StyleTab(createTabButton, create);
            StyleTab(manageTabButton, !create);

            if (!create) RefreshList();
        }

        private Button BuildTabButton (string text, Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.AddToClassList("button");
            button.style.flexGrow = 1;
            button.style.flexBasis = 0;
            button.style.height = 30;
            button.style.marginTop = 2; button.style.marginBottom = 2;
            button.style.marginLeft = 2; button.style.marginRight = 2;
            button.style.color = Color.black;
            return button;
        }

        private void StyleTab (Button button, bool active)
        {
            button.style.unityBackgroundImageTintColor =
                new StyleColor(active ? EditorHelper.ORANGE_COLOR : EditorHelper.GREY_COLOR);
        }

        private VisualElement BuildFooter ()
        {
            var footer = new VisualElement
            {
                style =
                {
                    flexGrow = 0, flexShrink = 0, height = 50, marginTop = 8, paddingBottom = 5,
                    flexDirection = FlexDirection.Row, backgroundColor = FooterTint
                }
            };
            SetBorder(footer, FooterBorder, 2);
            SetRadius(footer, 5);

            var sgLogo = LogoElement(SG_LOGO_PATH);
            sgLogo.style.marginRight = -30;
            footer.Add(sgLogo);

            var melenitasLogo = LogoElement(MELENITAS_LOGO_PATH);
            melenitasLogo.style.marginTop = -25;
            melenitasLogo.style.marginBottom = -30;
            melenitasLogo.style.marginLeft = -30;
            footer.Add(melenitasLogo);

            return footer;
        }

        private VisualElement LogoElement (string path)
        {
            var element = new VisualElement { style = { flexGrow = 1 } };
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
            {
                element.style.backgroundImage = new StyleBackground(texture);
                // background-size replaces the deprecated scale mode; centred and
                // unrepeated are already the defaults, so contain is all that is left.
                element.style.backgroundSize =
                    new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
            }
            return element;
        }

        // ================= Create panel =================
        private void BuildCreatePanel (VisualElement parent)
        {
            parent.Add(Subtitle("1. Write a TAG to identify your material:"));
            tagField = new TextField
            {
                tooltip = "This is the name you will use to identify and access this material in the code."
            };
            tagField.AddToClassList("text-field");
            tagField.style.height = 31;
            tagField.RegisterValueChangedCallback(e => OnChangeTag(e.newValue));
            parent.Add(tagField);

            parent.Add(Space(7));

            parent.Add(Subtitle("2. Pick a clip to preview the occlusion:"));
            clipField = new ObjectField { objectType = typeof(AudioClip), allowSceneObjects = false };
            clipField.style.marginLeft = 3; clipField.style.marginRight = 3;
            parent.Add(clipField);
            previewButton = new Button(TogglePreview) { text = "▶ Play (loop)" };
            previewButton.AddToClassList("orange-button");
            previewButton.style.height = 28;
            previewButton.style.marginTop = 3;
            previewButton.style.marginLeft = 3; previewButton.style.marginRight = 3;
            parent.Add(previewButton);

            parent.Add(Space(7));

            parent.Add(Subtitle("3. Set how much it occludes:"));
            densityContainer = new VisualElement { style = { marginTop = 2, marginLeft = 3, marginRight = 3 } };
            parent.Add(densityContainer);
            BuildDensitySection();

            var hint = new Label("0% = sound passes through, 100% = fully blocked (like a plain collider).");
            hint.AddToClassList("info-box");
            parent.Add(hint);

            createButton = new Button(OnCreateClicked) { text = "CREATE" };
            createButton.AddToClassList("create-button");
            createButton.style.height = 34;
            createButton.style.marginTop = 8;
            createButton.style.marginLeft = 4;
            createButton.style.marginRight = 4;
            parent.Add(createButton);

            messageLabel = new Label { style = { display = DisplayStyle.None } };
            messageLabel.AddToClassList("info-box");
            parent.Add(messageLabel);

            UpdateCreateButton();
        }

        private void BuildDensitySection ()
        {
            densityContainer.Clear();
            densityContainer.Add(DensitySlider(draft));
        }

        private SG_Slider DensitySlider (AudioOcclusionMaterial material)
        {
            var field = new SG_Slider
            {
                LabelText = "Density",
                MinValue = 0f,
                MaxValue = 1f,
                PercentageMode = true,
                Decimals = 0,
                Suffix = "%",
                Value = material.Density,
                DefaultValue = 0.5f
            };
            field.OnValueChanged += v => material.Density = v;
            return field;
        }

        // ================= Manage panel =================
        private void BuildManagePanel (VisualElement parent)
        {
            parent.Add(SectionLabel("Created Materials"));
            listContainer = new VisualElement();
            parent.Add(listContainer);
        }

        private void RefreshList ()
        {
            if (listContainer == null) return;
            DestroyEditClones();
            listContainer.Clear();

            var collection = AssetLocator.Instance.OcclusionMaterialCollection;
            if (collection == null || collection.Materials.Length == 0)
            {
                var empty = new Label("No occlusion materials created yet.");
                empty.AddToClassList("info-box");
                listContainer.Add(empty);
                return;
            }

            foreach (OcclusionMaterialData materialData in collection.Materials)
            {
                var captured = materialData;

                var card = new VisualElement { style = { marginBottom = 5, backgroundColor = CardBackground } };
                SetBorder(card, CardBorder, 3);
                SetRadius(card, 5);
                card.style.paddingTop = 5; card.style.paddingBottom = 5; card.style.paddingLeft = 8; card.style.paddingRight = 8;

                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

                var tagPrefix = new Label("Tag:");
                tagPrefix.AddToClassList("subtitle");
                tagPrefix.style.paddingLeft = 0; tagPrefix.style.paddingRight = 0;
                tagPrefix.style.marginRight = 6;
                row.Add(tagPrefix);

                var tagLabel = new Label(materialData.Tag) { style = { flexGrow = 1, fontSize = 15 } };
                tagLabel.AddToClassList("sg-setting-row");
                row.Add(tagLabel);

                var tagEditField = new TextField
                {
                    value = materialData.Tag,
                    tooltip = "Renaming the tag may break references to this material in your code.",
                    style = { flexGrow = 1, display = DisplayStyle.None, marginRight = 4, fontSize = 14 }
                };
                tagEditField.AddToClassList("text-field");
                tagEditField.RegisterValueChangedCallback(e =>
                {
                    if (!EditorHelper.IsTagValid(e.newValue)) tagEditField.AddToClassList("text-field-error");
                    else tagEditField.RemoveFromClassList("text-field-error");
                });
                row.Add(tagEditField);

                int percent = materialData.Material != null ? Mathf.RoundToInt(materialData.Material.Density * 100f) : 100;
                var densityLabel = new Label($"{percent}%") { style = { width = 44, unityTextAlign = TextAnchor.MiddleRight, marginRight = 6 } };
                row.Add(densityLabel);

                var editButton = new Button { text = "Edit", style = { width = 54, height = 22, marginRight = 3 } };
                editButton.AddToClassList("orange-button");
                row.Add(editButton);

                var removeButton = new Button(() => RemoveMaterial(captured)) { text = "X", style = { width = 26, height = 22 } };
                removeButton.AddToClassList("light-orange-button");
                row.Add(removeButton);

                card.Add(row);

                var editor = new VisualElement { style = { display = DisplayStyle.None, marginTop = 6, marginLeft = 3, marginRight = 3 } };
                card.Add(editor);

                editButton.clicked += () => ToggleEditor(captured, editButton, editor, tagLabel, tagEditField);

                listContainer.Add(card);
            }
        }

        // ================= Tag validation =================
        private void OnChangeTag (string newTag)
        {
            if (!EditorHelper.IsTagValid(newTag)) tagField.AddToClassList("text-field-error");
            else tagField.RemoveFromClassList("text-field-error");

            UpdateCreateButton();
        }

        private void UpdateCreateButton ()
        {
            bool valid = EditorHelper.IsTagValid(tagField.value);
            createButton.SetEnabled(valid);
            if (valid) createButton.AddToClassList("create-button");
            else createButton.RemoveFromClassList("create-button");
        }

        // ================= Actions =================
        private void OnCreateClicked ()
        {
            var collection = AssetLocator.Instance.OcclusionMaterialCollection;
            if (collection == null)
            {
                ShowMessage("Occlusion material data is not ready yet. Try again in a moment.");
                return;
            }

            string tag = tagField.value?.Trim() ?? "";

            if (string.IsNullOrEmpty(tag)) { ShowMessage("Tag required! Write a tag to identify this material."); return; }
            if (!EditorHelper.IsTagValid(tag)) { ShowMessage("Invalid tag. Use only letters and numbers, and don't start with a number."); return; }
            if (collection.Materials.Any(m => m.Tag == tag)) { ShowMessage($"The tag '{tag}' already exists!"); return; }

            string materialsFolder = EnsureMaterialsFolder();
            draft.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(draft, $"{materialsFolder}/{tag}.asset");
            collection.CreateMaterial(tag, draft, out string result);
            EditorUtility.SetDirty(collection);
            EditorHelper.SaveOcclusionMaterialCollectionChanges();

            ShowMessage(result);

            CreateDraft();
            BuildDensitySection();
            tagField.SetValueWithoutNotify("");
            tagField.RemoveFromClassList("text-field-error");
            UpdateCreateButton();
            RefreshList();
        }

        private void ToggleEditor (OcclusionMaterialData materialData, Button editButton, VisualElement editor,
            Label tagLabel, TextField tagEditField)
        {
            if (editor.userData is AudioOcclusionMaterial)
            {
                DestroyWorkingCopy(editor);
                editor.Clear();
                editor.style.display = DisplayStyle.None;

                tagEditField.style.display = DisplayStyle.None;
                tagEditField.RemoveFromClassList("text-field-error");
                tagLabel.style.display = DisplayStyle.Flex;
                editButton.text = "Edit";
                return;
            }

            if (materialData.Material != null)
            {
                var working = Instantiate(materialData.Material);
                working.hideFlags = HideFlags.HideAndDontSave;
                editClones.Add(working);
                editor.userData = working;

                editor.Add(DensitySlider(working));

                var saveMessage = new Label { style = { display = DisplayStyle.None } };
                saveMessage.AddToClassList("info-box");

                var saveButton = new Button(() => SaveMaterial(materialData, working, tagEditField, saveMessage)) { text = "SAVE" };
                saveButton.AddToClassList("create-button");
                saveButton.style.height = 30;
                saveButton.style.marginTop = 8;
                editor.Add(saveButton);
                editor.Add(saveMessage);
            }

            tagEditField.SetValueWithoutNotify(materialData.Tag);
            tagEditField.RemoveFromClassList("text-field-error");
            tagLabel.style.display = DisplayStyle.None;
            tagEditField.style.display = DisplayStyle.Flex;

            editor.style.display = DisplayStyle.Flex;
            editButton.text = "Close";
        }

        private void SaveMaterial (OcclusionMaterialData materialData, AudioOcclusionMaterial working,
            TextField tagField, Label saveMessage)
        {
            if (materialData.Material == null) return;

            var collection = AssetLocator.Instance.OcclusionMaterialCollection;
            string oldTag = materialData.Tag;
            string newTag = tagField.value?.Trim() ?? "";

            if (!EditorHelper.IsTagValid(newTag)) { ShowSaveMessage(saveMessage, "Invalid tag. Use only letters and numbers, and don't start with a number."); return; }

            bool renaming = newTag != oldTag;
            if (renaming && collection.Materials.Any(m => m != materialData && m.Tag == newTag))
            {
                ShowSaveMessage(saveMessage, $"The tag '{newTag}' already exists!");
                return;
            }

            materialData.Material.Density = working.Density;
            materialData.Material.EditorColor = working.EditorColor;

            if (renaming)
            {
                collection.EditMaterial(oldTag, newTag, out _);
                string path = AssetDatabase.GetAssetPath(materialData.Material);
                if (!string.IsNullOrEmpty(path)) AssetDatabase.RenameAsset(path, newTag);
                materialData.Material.name = newTag;
                EditorHelper.SaveOcclusionMaterialCollectionChanges();
                RefreshList();
                return;
            }

            materialData.Material.name = oldTag;
            EditorUtility.SetDirty(materialData.Material);
            AssetDatabase.SaveAssets();

            ShowSaveMessage(saveMessage, $"Changes to '{oldTag}' saved!");
            RefreshList();
        }

        private void ShowSaveMessage (Label saveMessage, string message)
        {
            saveMessage.text = message;
            saveMessage.style.display = DisplayStyle.Flex;
        }

        private void DestroyWorkingCopy (VisualElement editor)
        {
            if (editor.userData is AudioOcclusionMaterial working)
            {
                editClones.Remove(working);
                DestroyImmediate(working);
            }
            editor.userData = null;
        }

        private void DestroyEditClones ()
        {
            foreach (var clone in editClones)
                if (clone != null) DestroyImmediate(clone);
            editClones.Clear();
        }

        private void RemoveMaterial (OcclusionMaterialData materialData)
        {
            if (!EditorUtility.DisplayDialog("Delete material",
                    $"Do you want to remove '{materialData.Tag}' permanently?\n\n" +
                    "References to this material in the code could be lost.",
                    "Delete", "Cancel"))
                return;

            var collection = AssetLocator.Instance.OcclusionMaterialCollection;
            string presetPath = materialData.Material != null ? AssetDatabase.GetAssetPath(materialData.Material) : null;

            collection.RemoveMaterial(materialData.Tag);
            EditorUtility.SetDirty(collection);
            if (!string.IsNullOrEmpty(presetPath)) AssetDatabase.DeleteAsset(presetPath);

            EditorHelper.SaveOcclusionMaterialCollectionChanges();
            RefreshList();
        }

        // ================= Live preview =================
        private void TogglePreview ()
        {
            if (isPreviewing) StopPreview();
            else StartPreview();
        }

        private void StartPreview ()
        {
            var clip = clipField.value as AudioClip;
            if (clip == null) { ShowMessage("Pick a preview clip first."); return; }

            RecreatePreviewSource();
            previewSource.clip = clip;
            previewSource.loop = true;
            ApplyToPreview();
            previewSource.Play();

            isPreviewing = true;
            previewButton.text = "■ Stop";
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private void StopPreview ()
        {
            EditorApplication.update -= OnEditorUpdate;
            if (previewSource != null) previewSource.Stop();
            isPreviewing = false;
            if (previewButton != null) previewButton.text = "▶ Play (loop)";
        }

        private void OnEditorUpdate ()
        {
            if (!isPreviewing || previewSource == null) return;
            ApplyToPreview();
        }

        /// <summary>
        /// Reproduces what a fully occluded sound would sound like for this density: the same cutoff
        /// and volume the runtime applies (see SoundsGoodAudioSource.ApplyOcclusionToFilters), mapping
        /// density through the global occlusion settings. So density 0 is untouched and 1 is the most
        /// muffled a wall can make a sound with the current settings.
        /// </summary>
        private void ApplyToPreview ()
        {
            var settings = AssetLocator.SoundsGoodSettings;
            float density = Mathf.Clamp01(draft.Density);

            float cutoff = AudioEffectApplier.LogLerpCutoff(settings.MaxCutoff, settings.MinCutoff, density);
            if (cutoff < AudioEffectApplier.NO_LOW_PASS_CUTOFF)
            {
                if (previewLowPass == null)
                    previewLowPass = previewGameObject.AddComponent<AudioLowPassFilter>();
                previewLowPass.enabled = true;
                previewLowPass.cutoffFrequency = cutoff;
            }
            else if (previewLowPass != null)
            {
                previewLowPass.enabled = false;
            }

            previewSource.volume = Mathf.Lerp(1f, settings.MinVolumeMultiplier, density);
        }

        private void RecreatePreviewSource ()
        {
            if (previewGameObject != null) DestroyImmediate(previewGameObject);
            previewLowPass = null;

            previewGameObject = new GameObject("SG_OcclusionPreview") { hideFlags = HideFlags.HideAndDontSave };
            previewSource = previewGameObject.AddComponent<AudioSource>();
            previewSource.playOnAwake = false;
            previewSource.spatialBlend = 0f;
        }

        // ================= Helpers =================
        private void CreateDraft ()
        {
            draft = CreateInstance<AudioOcclusionMaterial>();
            draft.hideFlags = HideFlags.HideAndDontSave;
        }

        private string EnsureMaterialsFolder ()
        {
            string root = AssetLocator.SoundsGoodSettings.GetNormalizedDataRootPath().TrimEnd('/');
            string materialsFolder = root + "/OcclusionMaterials";
            if (!AssetDatabase.IsValidFolder(materialsFolder)) AssetDatabase.CreateFolder(root, "OcclusionMaterials");
            return materialsFolder;
        }

        private void ShowMessage (string message)
        {
            messageLabel.text = message;
            messageLabel.style.display = DisplayStyle.Flex;
        }

        private Label Subtitle (string text)
        {
            var label = new Label(text);
            label.AddToClassList("subtitle");
            return label;
        }

        private VisualElement Space (float height) =>
            new VisualElement { style = { flexGrow = 0, height = height } };

        private Label SectionLabel (string text)
        {
            var label = new Label(text);
            label.AddToClassList("subtitle");
            label.style.marginTop = 6;
            return label;
        }

        private static void SetPadding (VisualElement e, float p)
        {
            e.style.paddingTop = p; e.style.paddingBottom = p; e.style.paddingLeft = p; e.style.paddingRight = p;
        }

        private static void SetBorder (VisualElement e, Color color, float width)
        {
            e.style.borderTopWidth = width; e.style.borderBottomWidth = width; e.style.borderLeftWidth = width; e.style.borderRightWidth = width;
            e.style.borderTopColor = color; e.style.borderBottomColor = color; e.style.borderLeftColor = color; e.style.borderRightColor = color;
        }

        private static void SetRadius (VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r; e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
        }
    }
}
