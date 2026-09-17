using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MelenitasDev.SoundsGood.Domain;

namespace MelenitasDev.SoundsGood.Editor
{
    public class SoundsGoodSettingsWindow : EditorWindow
    {
        // ----- Serialized
        [SerializeField] private VisualTreeAsset tree;

        // ----- UI Query
        private SG_StringField dataRootPathField => rootVisualElement.Q<SG_StringField>("DataRootPathField");

        private SG_DropdownField dspBufferSizeField => rootVisualElement.Q<SG_DropdownField>("DspBufferSizeField");
        private Label dspLatencyInfoLabel => rootVisualElement.Q<Label>("DspLatencyInfoLabel");

        private SG_BoolField enableOcclusionField => rootVisualElement.Q<SG_BoolField>("EnableOcclusionField");

        private SG_LayerMaskField occlusionLayersField =>
            rootVisualElement.Q<SG_LayerMaskField>("OcclusionLayersField");

        private SG_FloatField maxDistanceField => rootVisualElement.Q<SG_FloatField>("MaxDistanceField");
        private SG_FloatField minCutoffField => rootVisualElement.Q<SG_FloatField>("MinCutoffField");
        private SG_FloatField maxCutoffField => rootVisualElement.Q<SG_FloatField>("MaxCutoffField");

        private SG_Slider minVolumeSlider => rootVisualElement.Q<SG_Slider>("MinVolumeMultiplierSlider");

        private SG_IntSlider maxOcclusionLayersSlider =>
            rootVisualElement.Q<SG_IntSlider>("MaxOcclusionLayersSlider");

        private SG_Slider extraDepthVolumeSlider =>
            rootVisualElement.Q<SG_Slider>("ExtraDepthVolumeMultiplierSlider");

        private SG_Slider extraDepthCutoffSlider =>
            rootVisualElement.Q<SG_Slider>("ExtraDepthCutoffMultiplierSlider");

        private SG_IntSlider audioBouncesSlider => rootVisualElement.Q<SG_IntSlider>("AudioBouncesSlider");
        private SG_FloatField bounceRadiusMinField => rootVisualElement.Q<SG_FloatField>("BounceRadiusMinField");
        private SG_IntSlider bounceRaysPerCircleSlider => rootVisualElement.Q<SG_IntSlider>("BouncesPerCircleSlider");

        private SG_Slider checkIntervalSlider => rootVisualElement.Q<SG_Slider>("CheckIntervalSlider");
        private SG_FloatField lerpSpeedSlider => rootVisualElement.Q<SG_FloatField>("LerpSpeedField");

        private VisualElement acceptChangePathLabel => rootVisualElement.Q<VisualElement>("AcceptChangePathLabel");
        private Button updateButton => rootVisualElement.Q<Button>("UpdateButton");
        private Button cancelButton => rootVisualElement.Q<Button>("CancelButton");

        // ----- Fields
        private string originalDataRootPath;
        private string pendingDataRootPath;

        private const string ERROR_CLASS = "text-field-error";
        private const string BUTTON_CLASS = "create-button";

        // ----- Menu
        [MenuItem("Tools/Sounds Good/Settings", false, 150)]
        public static void ShowWindow ()
        {
            var window = GetWindow<SoundsGoodSettingsWindow>();
            window.titleContent = new GUIContent("Sounds Good Settings");
            window.minSize = new Vector2(360, 420);
        }

        // ----- Unity Callbacks
        private void CreateGUI ()
        {
            if (tree == null)
            {
                Debug.LogError("[SoundsGood] Settings window VisualTreeAsset is not assigned.");
                return;
            }

            tree.CloneTree(rootVisualElement);

            MoveSettingsContentIntoScroll();

            var settings = AssetLocator.SoundsGoodSettings;
            if (settings == null)
            {
                Debug.LogError("[SoundsGood] SoundsGoodSettings asset not found. Make sure it exists in Resources.");
                return;
            }

            InitializeFields(settings);
            RegisterCallbacks(settings);
            RegisterDelayedSaveHandlers(settings);
            RegisterPathButtons(settings);
        }

        // ----- Private Methods
        private void MoveSettingsContentIntoScroll ()
        {
            var scroll = rootVisualElement.Q<ScrollView>("Scroll");
            var content = rootVisualElement.Q<VisualElement>("SettingsContent");

            if (scroll == null || content == null) return;

            content.RemoveFromHierarchy();
            scroll.Add(content);
            scroll.style.display = DisplayStyle.Flex;
        }

        private void InitializeFields (SoundsGoodSettings settings)
        {
            if (dataRootPathField != null)
            {
                dataRootPathField.Value = settings.DataRootPath;
                dataRootPathField.TextField.RemoveFromClassList(ERROR_CLASS);
            }

            if (updateButton != null)
            {
                updateButton.SetEnabled(false);
                updateButton.RemoveFromClassList(BUTTON_CLASS);
            }

            originalDataRootPath = settings.DataRootPath;
            pendingDataRootPath = originalDataRootPath;
            SetAcceptChangePathVisible(false);

            // Not a Sounds Good setting: it's read straight from the project's AudioManager asset.
            if (dspBufferSizeField != null)
            {
                dspBufferSizeField.Choices = string.Join(",", DspBufferSettings.Labels);
                dspBufferSizeField.Index = DspBufferSettings.IndexOfSize(DspBufferSettings.GetBufferSize());
            }

            RefreshDspLatencyInfo();

            if (enableOcclusionField != null) enableOcclusionField.Value = settings.EnableOcclusion;
            if (occlusionLayersField != null) occlusionLayersField.Value = settings.OcclusionLayers.value;
            if (maxDistanceField != null) maxDistanceField.Value = settings.MaxDistance;
            if (minCutoffField != null) minCutoffField.Value = settings.MinCutoff;
            if (maxCutoffField != null) maxCutoffField.Value = settings.MaxCutoff;
            if (minVolumeSlider != null) minVolumeSlider.Value = settings.MinVolumeMultiplier;
            if (maxOcclusionLayersSlider != null) maxOcclusionLayersSlider.Value = settings.MaxOcclusionLayers;
            if (extraDepthVolumeSlider != null) extraDepthVolumeSlider.Value = settings.ExtraDepthMinVolumeMultiplier;
            if (extraDepthCutoffSlider != null) extraDepthCutoffSlider.Value = settings.ExtraDepthCutoffMultiplier;
            if (audioBouncesSlider != null) audioBouncesSlider.Value = settings.MaxBounces;
            if (bounceRadiusMinField != null) bounceRadiusMinField.Value = settings.BounceRadiusMin;
            if (bounceRaysPerCircleSlider != null) bounceRaysPerCircleSlider.Value = settings.BounceRaysPerCircle;
            if (checkIntervalSlider != null) checkIntervalSlider.Value = settings.CheckInterval;
            if (lerpSpeedSlider != null) lerpSpeedSlider.Value = settings.LerpSpeed;
        }

        private void RegisterCallbacks (SoundsGoodSettings settings)
        {
            if (dataRootPathField != null)
            {
                dataRootPathField.OnValueChanged += path =>
                {
                    pendingDataRootPath = path;

                    bool hasChange = !string.IsNullOrWhiteSpace(pendingDataRootPath) &&
                                     pendingDataRootPath.Trim() != originalDataRootPath.Trim();

                    bool isValid = PathUtility.TrySanitizeDataRootPath(
                        pendingDataRootPath, out _, out _);

                    SetAcceptChangePathVisible(hasChange);
                    UpdatePathValidationUI(isValid, hasChange);
                };
            }

            if (dspBufferSizeField != null)
            {
                dspBufferSizeField.OnValueChanged += index =>
                {
                    DspBufferSettings.SetBufferSize(DspBufferSettings.Sizes[index]);
                    RefreshDspLatencyInfo();
                };
            }

            if (enableOcclusionField != null)
            {
                enableOcclusionField.OnValueChanged += enabled =>
                {
                    settings.EnableOcclusion = enabled;
                    SaveAndApply(settings);
                };
            }

            if (occlusionLayersField != null)
            {
                occlusionLayersField.OnValueChanged += maskValue =>
                {
                    settings.OcclusionLayers = maskValue;
                    SaveAndApply(settings);
                };
            }

            if (maxDistanceField != null)
            {
                maxDistanceField.OnValueChanged += dist => { settings.MaxDistance = dist; };
            }

            if (minCutoffField != null)
            {
                minCutoffField.OnValueChanged += cutoff => { settings.MinCutoff = cutoff; };
            }

            if (maxCutoffField != null)
            {
                maxCutoffField.OnValueChanged += cutoff => { settings.MaxCutoff = cutoff; };
            }

            if (minVolumeSlider != null)
            {
                minVolumeSlider.OnValueChanged += mult => { settings.MinVolumeMultiplier = mult; };
            }

            if (maxOcclusionLayersSlider != null)
            {
                maxOcclusionLayersSlider.OnValueChanged += layers => { settings.MaxOcclusionLayers = layers; };
            }

            if (extraDepthVolumeSlider != null)
            {
                extraDepthVolumeSlider.OnValueChanged += mult => { settings.ExtraDepthMinVolumeMultiplier = mult; };
            }

            if (extraDepthCutoffSlider != null)
            {
                extraDepthCutoffSlider.OnValueChanged += mult => { settings.ExtraDepthCutoffMultiplier = mult; };
            }

            if (audioBouncesSlider != null)
            {
                audioBouncesSlider.OnValueChanged += bounces => { settings.MaxBounces = bounces; };
            }

            if (bounceRadiusMinField != null)
            {
                bounceRadiusMinField.OnValueChanged += radius => { settings.BounceRadiusMin = radius; };
            }

            if (bounceRaysPerCircleSlider != null)
            {
                bounceRaysPerCircleSlider.OnValueChanged += rays => { settings.BounceRaysPerCircle = rays; };
            }

            if (checkIntervalSlider != null)
            {
                checkIntervalSlider.OnValueChanged += secs => { settings.CheckInterval = secs; };
            }

            if (lerpSpeedSlider != null)
            {
                lerpSpeedSlider.OnValueChanged += speed => { settings.LerpSpeed = speed; };
            }
        }

        private void RegisterDelayedSaveHandlers (SoundsGoodSettings settings)
        {
            if (maxDistanceField != null)
            {
                var field = maxDistanceField.Q<FloatField>();
                if (field != null)
                    field.RegisterCallback<FocusOutEvent>(_ => SaveAndApply(settings));
            }

            if (minCutoffField != null)
            {
                var field = minCutoffField.Q<FloatField>();
                if (field != null)
                    field.RegisterCallback<FocusOutEvent>(_ => SaveAndApply(settings));
            }

            if (maxCutoffField != null)
            {
                var field = maxCutoffField.Q<FloatField>();
                if (field != null)
                    field.RegisterCallback<FocusOutEvent>(_ => SaveAndApply(settings));
            }

            if (bounceRadiusMinField != null)
            {
                var field = bounceRadiusMinField.Q<FloatField>();
                if (field != null)
                    field.RegisterCallback<FocusOutEvent>(_ => SaveAndApply(settings));
            }

            if (minVolumeSlider != null)
            {
                var slider = minVolumeSlider.Q<Slider>();
                if (slider != null)
                    slider.RegisterCallback<PointerUpEvent>(_ => SaveAndApply(settings));
            }

            if (extraDepthVolumeSlider != null)
            {
                var slider = extraDepthVolumeSlider.Q<Slider>();
                if (slider != null)
                    slider.RegisterCallback<PointerUpEvent>(_ => SaveAndApply(settings));
            }

            if (extraDepthCutoffSlider != null)
            {
                var slider = extraDepthCutoffSlider.Q<Slider>();
                if (slider != null)
                    slider.RegisterCallback<PointerUpEvent>(_ => SaveAndApply(settings));
            }

            if (maxOcclusionLayersSlider != null)
            {
                var slider = maxOcclusionLayersSlider.Q<SliderInt>();
                if (slider != null)
                    slider.RegisterCallback<PointerUpEvent>(_ => SaveAndApply(settings));
            }

            if (checkIntervalSlider != null)
            {
                var slider = checkIntervalSlider.Q<Slider>();
                if (slider != null)
                    slider.RegisterCallback<PointerUpEvent>(_ => SaveAndApply(settings));
            }

            if (lerpSpeedSlider != null)
            {
                var field = lerpSpeedSlider.Q<FloatField>();
                if (field != null)
                    field.RegisterCallback<FocusOutEvent>(_ => SaveAndApply(settings));
            }

            if (audioBouncesSlider != null)
            {
                var slider = audioBouncesSlider.Q<SliderInt>();
                if (slider != null)
                    slider.RegisterCallback<PointerUpEvent>(_ => SaveAndApply(settings));
            }

            if (bounceRaysPerCircleSlider != null)
            {
                var slider = bounceRaysPerCircleSlider.Q<SliderInt>();
                if (slider != null)
                    slider.RegisterCallback<PointerUpEvent>(_ => SaveAndApply(settings));
            }
        }

        private void RegisterPathButtons (SoundsGoodSettings settings)
        {
            if (updateButton != null)
            {
                updateButton.clicked += () =>
                {
                    if (string.IsNullOrWhiteSpace(pendingDataRootPath)) return;

                    if (!PathUtility.TrySanitizeDataRootPath(pendingDataRootPath, 
                            out var safePath, out var err))
                    {
                        EditorUtility.DisplayDialog(
                            "Invalid Data Path",
                            "That path can't be used:\n" + err,
                            "Ok"
                        );

                        pendingDataRootPath = originalDataRootPath;
                        if (dataRootPathField != null) dataRootPathField.Value = originalDataRootPath;

                        SetAcceptChangePathVisible(false);
                        UpdatePathValidationUI(true, false);
                        return;
                    }

                    pendingDataRootPath = safePath;
                    settings.DataRootPath = safePath;
                    originalDataRootPath = safePath;
                    SaveAndApply(settings);
                    SetAcceptChangePathVisible(false);
                    UpdatePathValidationUI(true, false);
                };
            }

            if (cancelButton != null)
            {
                cancelButton.clicked += () =>
                {
                    pendingDataRootPath = originalDataRootPath;

                    if (dataRootPathField != null)
                        dataRootPathField.Value = originalDataRootPath;

                    SetAcceptChangePathVisible(false);
                    UpdatePathValidationUI(true, false);
                };
            }
        }

        private void SetAcceptChangePathVisible (bool visible)
        {
            if (acceptChangePathLabel == null) return;

            acceptChangePathLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdatePathValidationUI (bool isValid, bool hasChange)
        {
            if (dataRootPathField != null)
            {
                if (isValid) dataRootPathField.TextField.RemoveFromClassList(ERROR_CLASS);
                else dataRootPathField.TextField.AddToClassList(ERROR_CLASS);
            }

            if (updateButton == null) return;
            
            if (isValid && hasChange)
            {
                updateButton.SetEnabled(true);
                updateButton.AddToClassList(BUTTON_CLASS);
                return;
            }
            
            updateButton.SetEnabled(false);
            updateButton.RemoveFromClassList(BUTTON_CLASS);
        }

        private void RefreshDspLatencyInfo ()
        {
            if (dspLatencyInfoLabel == null) return;

            dspLatencyInfoLabel.text = DspBufferSettings.DescribeLatency(DspBufferSettings.GetBufferSize());
        }

        private void SaveAndApply (SoundsGoodSettings settings)
        {
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            // The data root may have just changed, which is the one setting that makes the
            // initializer's work stale: the folders it created and the data it migrated now
            // belong somewhere else.
            DefaultDataInitializer.Run();
        }
    }
}