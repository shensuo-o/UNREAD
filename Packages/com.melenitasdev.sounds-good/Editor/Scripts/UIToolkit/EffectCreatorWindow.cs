using System;
using System.Linq;
using MelenitasDev.SoundsGood.Domain;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
    public class EffectCreatorWindow : EditorWindow
    {
        private const string SG_LOGO_PATH =
            "Packages/com.melenitasdev.sounds-good/Runtime/Graphics/Sounds Good Logo.png";
        private const string MELENITAS_LOGO_PATH =
            "Packages/com.melenitasdev.sounds-good/Runtime/Graphics/Unity Splash Screen.png";

        private static readonly Color HeaderBorder = new Color(1f, 0.722f, 0.467f);
        private static readonly Color HeaderTint = new Color(0.529f, 0.251f, 0f, 0.13f);
        private static readonly Color FooterBorder = new Color(0.565f, 0.4f, 0.247f);
        private static readonly Color FooterTint = new Color(0.208f, 0.208f, 0.208f, 0.47f);
        private static readonly Color CardBorder = new Color(0.424f, 0.314f, 0.216f);   // rgb(108, 80, 55)
        private static readonly Color CardBackground = new Color(0.180f, 0.180f, 0.180f); // rgb(46, 46, 46)

        private AudioEffectPreset draft;

        private Button createTabButton;
        private Button manageTabButton;
        private VisualElement createScroll;
        private VisualElement manageScroll;

        private TextField tagField;
        private Button createButton;
        private VisualElement effectsContainer;
        private Label messageLabel;
        private VisualElement listContainer;
        private readonly System.Collections.Generic.List<AudioEffectPreset> editClones = new();

        private ObjectField clipField;
        private Button previewButton;
        private GameObject previewGameObject;
        private AudioSource previewSource;
        private AudioLowPassFilter previewLowPass;
        private bool isPreviewing;

        [MenuItem("Tools/Sounds Good/Effect Creator", false, 53)]
        public static void ShowWindow ()
        {
            var window = GetWindow<EffectCreatorWindow>();
            window.titleContent = new GUIContent("Effect Creator");
            window.minSize = new Vector2(370, 520);
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

        /// <summary>
        /// Stop the preview across play-mode transitions so it doesn't keep driving audio into (or out
        /// of) play mode. We only stop here, never destroy: DestroyImmediate is unreliable inside a
        /// play-mode transition callback. The next Play rebuilds the preview object from scratch (see
        /// <see cref="RecreatePreviewSource"/>), which handles whatever state the round trip left behind.
        /// </summary>
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

            var title = new Label("EFFECT CREATOR");
            title.AddToClassList("title");
            header.Add(title);

            var subtitle = new Label("Shape how your audio sounds")
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
            parent.Add(Subtitle("1. Write a TAG to identify your effect:"));
            tagField = new TextField
            {
                tooltip = "This is the name you will use to identify and access this effect in the code."
            };
            tagField.AddToClassList("text-field");
            tagField.style.height = 31;
            tagField.RegisterValueChangedCallback(e => OnChangeTag(e.newValue));
            parent.Add(tagField);

            parent.Add(Space(7));

            parent.Add(Subtitle("2. Pick a clip to preview your effect:"));
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

            parent.Add(Subtitle("3. Configure your effects:"));
            effectsContainer = new VisualElement { style = { marginTop = 2, marginLeft = 3, marginRight = 3 } };
            parent.Add(effectsContainer);
            BuildEffectSections();

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

        private void BuildEffectSections ()
        {
            effectsContainer.Clear();
            effectsContainer.Add(BuildEffectSections(draft));
        }

        private VisualElement BuildEffectSections (AudioEffectPreset p)
        {
            var container = new VisualElement();
            container.Add(BuildReverbSection(p));
            container.Add(BuildEchoSection(p));
            container.Add(BuildChorusSection(p));
            container.Add(BuildDistortionSection(p));
            container.Add(BuildLowPassSection(p));
            container.Add(BuildHighPassSection(p));
            container.Add(BuildVolumeSection(p));
            container.Add(BuildPitchSection(p));
            return container;
        }

        private VisualElement BuildReverbSection (AudioEffectPreset p)
        {
            var box = new VisualElement();
            var paramsBox = new VisualElement();

            var preset = new EnumField("Reverb Preset", p.ReverbPreset);
            preset.RegisterValueChangedCallback(e => p.ReverbPreset = (AudioReverbPreset)e.newValue);
            paramsBox.Add(preset);

            paramsBox.style.display = p.UseReverb ? DisplayStyle.Flex : DisplayStyle.None;

            void Reset () { p.ReverbPreset = AudioReverbPreset.Generic; preset.SetValueWithoutNotify(AudioReverbPreset.Generic); }

            box.Add(SectionHeader("Reverb",
                () => p.UseReverb,
                v => { p.UseReverb = v; paramsBox.style.display = v ? DisplayStyle.Flex : DisplayStyle.None; },
                Reset));
            box.Add(paramsBox);
            return box;
        }

        private VisualElement BuildEchoSection (AudioEffectPreset p)
        {
            var box = new VisualElement();
            var paramsBox = new VisualElement();

            var delay = Slider("Delay", 10, 5000, 0, () => p.EchoDelay, v => p.EchoDelay = v);
            var decay = Slider("Decay Ratio", 0, 1, 2, () => p.EchoDecayRatio, v => p.EchoDecayRatio = v);
            var wet = Slider("Wet Mix", 0, 1, 2, () => p.EchoWetMix, v => p.EchoWetMix = v);
            var dry = Slider("Dry Mix", 0, 1, 2, () => p.EchoDryMix, v => p.EchoDryMix = v);
            paramsBox.Add(delay); paramsBox.Add(decay); paramsBox.Add(wet); paramsBox.Add(dry);

            paramsBox.style.display = p.UseEcho ? DisplayStyle.Flex : DisplayStyle.None;

            void Reset ()
            {
                p.EchoDelay = 500; p.EchoDecayRatio = 0.5f; p.EchoWetMix = 1f; p.EchoDryMix = 1f;
                delay.Value = 500; decay.Value = 0.5f; wet.Value = 1f; dry.Value = 1f;
            }

            box.Add(SectionHeader("Echo",
                () => p.UseEcho,
                v => { p.UseEcho = v; paramsBox.style.display = v ? DisplayStyle.Flex : DisplayStyle.None; },
                Reset));
            box.Add(paramsBox);
            return box;
        }

        private VisualElement BuildChorusSection (AudioEffectPreset p)
        {
            var box = new VisualElement();
            var paramsBox = new VisualElement();

            var dryMix = Slider("Dry Mix", 0, 1, 2, () => p.ChorusDryMix, v => p.ChorusDryMix = v);
            var wet1 = Slider("Wet Mix 1", 0, 1, 2, () => p.ChorusWetMix1, v => p.ChorusWetMix1 = v);
            var wet2 = Slider("Wet Mix 2", 0, 1, 2, () => p.ChorusWetMix2, v => p.ChorusWetMix2 = v);
            var wet3 = Slider("Wet Mix 3", 0, 1, 2, () => p.ChorusWetMix3, v => p.ChorusWetMix3 = v);
            var delay = Slider("Delay", 0.1f, 100, 1, () => p.ChorusDelay, v => p.ChorusDelay = v);
            var rate = Slider("Rate", 0, 20, 2, () => p.ChorusRate, v => p.ChorusRate = v);
            var depth = Slider("Depth", 0, 1, 2, () => p.ChorusDepth, v => p.ChorusDepth = v);
            paramsBox.Add(dryMix); paramsBox.Add(wet1); paramsBox.Add(wet2); paramsBox.Add(wet3);
            paramsBox.Add(delay); paramsBox.Add(rate); paramsBox.Add(depth);

            paramsBox.style.display = p.UseChorus ? DisplayStyle.Flex : DisplayStyle.None;

            void Reset ()
            {
                p.ChorusDryMix = 0.5f; p.ChorusWetMix1 = 0.5f; p.ChorusWetMix2 = 0.5f; p.ChorusWetMix3 = 0.5f;
                p.ChorusDelay = 40f; p.ChorusRate = 0.8f; p.ChorusDepth = 0.03f;
                dryMix.Value = 0.5f; wet1.Value = 0.5f; wet2.Value = 0.5f; wet3.Value = 0.5f;
                delay.Value = 40f; rate.Value = 0.8f; depth.Value = 0.03f;
            }

            box.Add(SectionHeader("Chorus",
                () => p.UseChorus,
                v => { p.UseChorus = v; paramsBox.style.display = v ? DisplayStyle.Flex : DisplayStyle.None; },
                Reset));
            box.Add(paramsBox);
            return box;
        }

        private VisualElement BuildDistortionSection (AudioEffectPreset p)
        {
            var box = new VisualElement();
            var paramsBox = new VisualElement();

            var level = Slider("Distortion Level", 0, 1, 2, () => p.DistortionLevel, v => p.DistortionLevel = v);
            paramsBox.Add(level);

            paramsBox.style.display = p.UseDistortion ? DisplayStyle.Flex : DisplayStyle.None;

            void Reset () { p.DistortionLevel = 0.5f; level.Value = 0.5f; }

            box.Add(SectionHeader("Distortion",
                () => p.UseDistortion,
                v => { p.UseDistortion = v; paramsBox.style.display = v ? DisplayStyle.Flex : DisplayStyle.None; },
                Reset));
            box.Add(paramsBox);
            return box;
        }

        private VisualElement BuildLowPassSection (AudioEffectPreset p)
        {
            var box = new VisualElement();
            var paramsBox = new VisualElement();

            var cutoff = Slider("Cutoff", 10, 22000, 0, () => p.LowPassCutoff, v => p.LowPassCutoff = v);
            var resonance = Slider("Resonance", 1, 10, 2, () => p.LowPassResonance, v => p.LowPassResonance = v);
            paramsBox.Add(cutoff); paramsBox.Add(resonance);

            paramsBox.style.display = p.UseLowPass ? DisplayStyle.Flex : DisplayStyle.None;

            void Reset ()
            {
                p.LowPassCutoff = 1000f; p.LowPassResonance = 1f;
                cutoff.Value = 1000f; resonance.Value = 1f;
            }

            box.Add(SectionHeader("Low Pass",
                () => p.UseLowPass,
                v => { p.UseLowPass = v; paramsBox.style.display = v ? DisplayStyle.Flex : DisplayStyle.None; },
                Reset));
            box.Add(paramsBox);
            return box;
        }

        private VisualElement BuildHighPassSection (AudioEffectPreset p)
        {
            var box = new VisualElement();
            var paramsBox = new VisualElement();

            var cutoff = Slider("Cutoff", 10, 22000, 0, () => p.HighPassCutoff, v => p.HighPassCutoff = v);
            var resonance = Slider("Resonance", 1, 10, 2, () => p.HighPassResonance, v => p.HighPassResonance = v);
            paramsBox.Add(cutoff); paramsBox.Add(resonance);

            paramsBox.style.display = p.UseHighPass ? DisplayStyle.Flex : DisplayStyle.None;

            void Reset ()
            {
                p.HighPassCutoff = 3000f; p.HighPassResonance = 1f;
                cutoff.Value = 3000f; resonance.Value = 1f;
            }

            box.Add(SectionHeader("High Pass",
                () => p.UseHighPass,
                v => { p.UseHighPass = v; paramsBox.style.display = v ? DisplayStyle.Flex : DisplayStyle.None; },
                Reset));
            box.Add(paramsBox);
            return box;
        }

        private VisualElement BuildVolumeSection (AudioEffectPreset p)
        {
            var box = new VisualElement();
            var paramsBox = new VisualElement();

            var volume = Slider("Volume", 0, 1, 2, () => p.Volume, v => p.Volume = v);
            paramsBox.Add(volume);

            paramsBox.style.display = p.UseVolume ? DisplayStyle.Flex : DisplayStyle.None;

            void Reset () { p.Volume = 1f; volume.Value = 1f; }

            box.Add(SectionHeader("Volume",
                () => p.UseVolume,
                v => { p.UseVolume = v; paramsBox.style.display = v ? DisplayStyle.Flex : DisplayStyle.None; },
                Reset));
            box.Add(paramsBox);
            return box;
        }

        private VisualElement BuildPitchSection (AudioEffectPreset p)
        {
            var box = new VisualElement();
            var paramsBox = new VisualElement();

            var pitch = Slider("Pitch", -3, 3, 2, () => p.Pitch, v => p.Pitch = v);
            paramsBox.Add(pitch);

            paramsBox.style.display = p.UsePitch ? DisplayStyle.Flex : DisplayStyle.None;

            void Reset () { p.Pitch = 1f; pitch.Value = 1f; }

            box.Add(SectionHeader("Pitch",
                () => p.UsePitch,
                v => { p.UsePitch = v; paramsBox.style.display = v ? DisplayStyle.Flex : DisplayStyle.None; },
                Reset));
            box.Add(paramsBox);
            return box;
        }

        // ================= Manage panel =================
        private void BuildManagePanel (VisualElement parent)
        {
            parent.Add(SectionLabel("Created Effects"));
            listContainer = new VisualElement();
            parent.Add(listContainer);
        }

        private void RefreshList ()
        {
            if (listContainer == null) return;
            DestroyEditClones();
            listContainer.Clear();

            var collection = AssetLocator.Instance.EffectDataCollection;
            if (collection == null || collection.Effects.Length == 0)
            {
                var empty = new Label("No effects created yet.");
                empty.AddToClassList("info-box");
                listContainer.Add(empty);
                return;
            }

            foreach (EffectData effectData in collection.Effects)
            {
                var captured = effectData;

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

                var tagLabel = new Label(effectData.Tag) { style = { flexGrow = 1, fontSize = 15 } };
                tagLabel.AddToClassList("sg-setting-row");
                row.Add(tagLabel);

                var tagEditField = new TextField
                {
                    value = effectData.Tag,
                    tooltip = "Renaming the tag may break references to this effect in your code.",
                    style = { flexGrow = 1, display = DisplayStyle.None, marginRight = 4, fontSize = 14 }
                };
                tagEditField.AddToClassList("text-field");
                tagEditField.RegisterValueChangedCallback(e =>
                {
                    if (!EditorHelper.IsTagValid(e.newValue)) tagEditField.AddToClassList("text-field-error");
                    else tagEditField.RemoveFromClassList("text-field-error");
                });
                row.Add(tagEditField);

                var editButton = new Button { text = "Edit", style = { width = 54, height = 22, marginRight = 3 } };
                editButton.AddToClassList("orange-button");
                row.Add(editButton);

                var removeButton = new Button(() => RemoveEffect(captured)) { text = "X", style = { width = 26, height = 22 } };
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
            var collection = AssetLocator.Instance.EffectDataCollection;
            if (collection == null)
            {
                ShowMessage("Effect data is not ready yet. Try again in a moment.");
                return;
            }

            string tag = tagField.value?.Trim() ?? "";

            if (string.IsNullOrEmpty(tag)) { ShowMessage("Tag required! Write a tag to identify this effect."); return; }
            if (!EditorHelper.IsTagValid(tag)) { ShowMessage("Invalid tag. Use only letters and numbers, and don't start with a number."); return; }
            if (collection.Effects.Any(e => e.Tag == tag)) { ShowMessage($"The tag '{tag}' already exists!"); return; }

            string effectsFolder = EnsureEffectsFolder();
            // Clear the DontSave flag set in CreateDraft before persisting it as an asset.
            draft.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(draft, $"{effectsFolder}/{tag}.asset");
            collection.CreateEffect(tag, draft, out string result);
            EditorUtility.SetDirty(collection);
            EditorHelper.SaveEffectCollectionChanges();

            ShowMessage(result);

            CreateDraft();
            BuildEffectSections();
            tagField.SetValueWithoutNotify("");
            tagField.RemoveFromClassList("text-field-error");
            UpdateCreateButton();
            RefreshList();
        }

        private void ToggleEditor (EffectData effectData, Button editButton, VisualElement editor,
            Label tagLabel, TextField tagEditField)
        {
            // The editor binds to a working copy; the original asset is only touched on Save.
            // Expanded state is tracked by whether a working copy is stored in userData.
            if (editor.userData is AudioEffectPreset)
            {
                // Close -> discard the working copy (unsaved changes are dropped, original untouched).
                DestroyWorkingCopy(editor);
                editor.Clear();
                editor.style.display = DisplayStyle.None;

                tagEditField.style.display = DisplayStyle.None;
                tagEditField.RemoveFromClassList("text-field-error");
                tagLabel.style.display = DisplayStyle.Flex;
                editButton.text = "Edit";
                return;
            }

            if (effectData.Preset != null)
            {
                var working = Instantiate(effectData.Preset);
                working.hideFlags = HideFlags.HideAndDontSave;
                editClones.Add(working);
                editor.userData = working;

                editor.Add(BuildEffectSections(working));

                var saveMessage = new Label { style = { display = DisplayStyle.None } };
                saveMessage.AddToClassList("info-box");

                var saveButton = new Button(() => SaveEffect(effectData, working, tagEditField, saveMessage)) { text = "SAVE" };
                saveButton.AddToClassList("create-button");
                saveButton.style.height = 30;
                saveButton.style.marginTop = 8;
                editor.Add(saveButton);
                editor.Add(saveMessage);
            }

            // Turn the top name into an editable field.
            tagEditField.SetValueWithoutNotify(effectData.Tag);
            tagEditField.RemoveFromClassList("text-field-error");
            tagLabel.style.display = DisplayStyle.None;
            tagEditField.style.display = DisplayStyle.Flex;

            editor.style.display = DisplayStyle.Flex;
            editButton.text = "Close";
        }

        private void SaveEffect (EffectData effectData, AudioEffectPreset working, TextField tagField, Label saveMessage)
        {
            if (effectData.Preset == null) return;

            var collection = AssetLocator.Instance.EffectDataCollection;
            string oldTag = effectData.Tag;
            string newTag = tagField.value?.Trim() ?? "";

            if (!EditorHelper.IsTagValid(newTag)) { ShowSaveMessage(saveMessage, "Invalid tag. Use only letters and numbers, and don't start with a number."); return; }

            bool renaming = newTag != oldTag;
            if (renaming && collection.Effects.Any(e => e != effectData && e.Tag == newTag))
            {
                ShowSaveMessage(saveMessage, $"The tag '{newTag}' already exists!");
                return;
            }

            // Copy only the effect values (not the object name) into the real asset.
            CopyEffectValues(working, effectData.Preset);

            if (renaming)
            {
                collection.EditEffect(oldTag, newTag, out _);
                string path = AssetDatabase.GetAssetPath(effectData.Preset);
                if (!string.IsNullOrEmpty(path)) AssetDatabase.RenameAsset(path, newTag);
                effectData.Preset.name = newTag;
                EditorHelper.SaveEffectCollectionChanges(); // persists collection + regenerates Effect enum
                RefreshList(); // reflect the new tag in the list (rebuilds cards)
                return;
            }

            effectData.Preset.name = oldTag;
            EditorUtility.SetDirty(effectData.Preset);
            AssetDatabase.SaveAssets();

            ShowSaveMessage(saveMessage, $"Changes to '{oldTag}' saved!");
        }

        private void ShowSaveMessage (Label saveMessage, string message)
        {
            saveMessage.text = message;
            saveMessage.style.display = DisplayStyle.Flex;
        }

        private static void CopyEffectValues (AudioEffectPreset src, AudioEffectPreset dst)
        {
            dst.UseReverb = src.UseReverb;
            dst.ReverbPreset = src.ReverbPreset;

            dst.UseEcho = src.UseEcho;
            dst.EchoDelay = src.EchoDelay;
            dst.EchoDecayRatio = src.EchoDecayRatio;
            dst.EchoWetMix = src.EchoWetMix;
            dst.EchoDryMix = src.EchoDryMix;

            dst.UseChorus = src.UseChorus;
            dst.ChorusDryMix = src.ChorusDryMix;
            dst.ChorusWetMix1 = src.ChorusWetMix1;
            dst.ChorusWetMix2 = src.ChorusWetMix2;
            dst.ChorusWetMix3 = src.ChorusWetMix3;
            dst.ChorusDelay = src.ChorusDelay;
            dst.ChorusRate = src.ChorusRate;
            dst.ChorusDepth = src.ChorusDepth;

            dst.UseDistortion = src.UseDistortion;
            dst.DistortionLevel = src.DistortionLevel;

            dst.UseLowPass = src.UseLowPass;
            dst.LowPassCutoff = src.LowPassCutoff;
            dst.LowPassResonance = src.LowPassResonance;

            dst.UseHighPass = src.UseHighPass;
            dst.HighPassCutoff = src.HighPassCutoff;
            dst.HighPassResonance = src.HighPassResonance;

            dst.UseVolume = src.UseVolume;
            dst.Volume = src.Volume;

            dst.UsePitch = src.UsePitch;
            dst.Pitch = src.Pitch;
        }

        private void DestroyWorkingCopy (VisualElement editor)
        {
            if (editor.userData is AudioEffectPreset working)
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

        private void RemoveEffect (EffectData effectData)
        {
            if (!EditorUtility.DisplayDialog("Delete effect",
                    $"Do you want to remove '{effectData.Tag}' permanently?\n\n" +
                    "References to this effect in the code could be lost.",
                    "Delete", "Cancel"))
                return;

            var collection = AssetLocator.Instance.EffectDataCollection;
            string presetPath = effectData.Preset != null ? AssetDatabase.GetAssetPath(effectData.Preset) : null;

            collection.RemoveEffect(effectData.Tag);
            EditorUtility.SetDirty(collection);
            if (!string.IsNullOrEmpty(presetPath)) AssetDatabase.DeleteAsset(presetPath);

            EditorHelper.SaveEffectCollectionChanges();
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

            // Always build a brand-new preview object. A play-mode round trip can leave the previous
            // one destroyed or in an unusable state (especially with domain/scene reload turned off),
            // so recreating from scratch on every Play guarantees a clean source with fresh filters.
            RecreatePreviewSource();
            previewSource.clip = clip;
            previewSource.loop = true;
            ApplyToPreview();
            previewSource.Play();

            isPreviewing = true;
            previewButton.text = "■ Stop";
            EditorApplication.update -= OnEditorUpdate; // never stack duplicate subscriptions
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
        /// Low pass, volume and pitch aren't part of AudioEffectApplier.Apply (on a real source the
        /// occlusion / the source's own pitch share them), so the preview applies them itself to stay
        /// faithful. High pass and the other filters are handled by Apply directly.
        /// </summary>
        private void ApplyToPreview ()
        {
            AudioEffectApplier.Apply(previewGameObject, draft);

            float cutoff = AudioEffectApplier.GetLowPassCutoff(draft, 1f);
            if (cutoff < AudioEffectApplier.NO_LOW_PASS_CUTOFF)
            {
                if (previewLowPass == null)
                    previewLowPass = previewGameObject.AddComponent<AudioLowPassFilter>();
                previewLowPass.enabled = true;
                previewLowPass.cutoffFrequency = cutoff;
                previewLowPass.lowpassResonanceQ = AudioEffectApplier.GetLowPassResonance(draft, 1f);
            }
            else if (previewLowPass != null)
            {
                previewLowPass.enabled = false;
            }

            previewSource.volume = AudioEffectApplier.GetVolumeMultiplier(draft, 1f);
            previewSource.pitch = AudioEffectApplier.GetPitchMultiplier(draft, 1f);
        }

        private void RecreatePreviewSource ()
        {
            // Destroy any leftover object first (safe here — this runs from a button click, not from
            // a play-mode transition callback where DestroyImmediate is rejected).
            if (previewGameObject != null) DestroyImmediate(previewGameObject);
            previewLowPass = null;

            previewGameObject = new GameObject("SG_EffectPreview") { hideFlags = HideFlags.HideAndDontSave };
            previewSource = previewGameObject.AddComponent<AudioSource>();
            previewSource.playOnAwake = false;
            previewSource.spatialBlend = 0f;
        }

        // ================= Helpers =================
        private void CreateDraft ()
        {
            draft = CreateInstance<AudioEffectPreset>();
            // Survive play-mode transitions: an unsaved ScriptableObject without this flag is
            // destroyed when entering/exiting play mode (even with domain reload off), leaving the
            // preview reading a null draft so no effects apply until the next recompile.
            draft.hideFlags = HideFlags.HideAndDontSave;
        }

        private string EnsureEffectsFolder ()
        {
            string root = AssetLocator.SoundsGoodSettings.GetNormalizedDataRootPath().TrimEnd('/');
            string effectsFolder = root + "/Effects";
            if (!AssetDatabase.IsValidFolder(effectsFolder)) AssetDatabase.CreateFolder(root, "Effects");
            return effectsFolder;
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

        private VisualElement SectionHeader (string title, Func<bool> isOn, Action<bool> onToggle, Action onReset)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 6 } };

            var toggle = new Toggle { value = isOn() };
            toggle.RegisterValueChangedCallback(e => onToggle(e.newValue));
            toggle.style.marginRight = 4;
            toggle.style.marginLeft = 1;
            row.Add(toggle);

            var label = new Label(title);
            label.AddToClassList("subtitle");
            label.style.flexGrow = 1;
            label.style.paddingLeft = 0;
            row.Add(label);

            var reset = new Button(() => onReset()) { text = "Reset" };
            reset.AddToClassList("light-orange-button");
            reset.style.height = 20;
            reset.style.width = 60;
            reset.style.fontSize = 11;
            row.Add(reset);

            return row;
        }

        private SG_Slider Slider (string label, float min, float max, int decimals, Func<float> get, Action<float> set)
        {
            var field = new SG_Slider
            {
                LabelText = label, MinValue = min, MaxValue = max,
                PercentageMode = false, Decimals = decimals, Value = get()
            };
            field.OnValueChanged += set;
            return field;
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
