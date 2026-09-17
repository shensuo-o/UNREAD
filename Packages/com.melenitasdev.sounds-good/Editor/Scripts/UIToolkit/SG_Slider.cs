using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
#if UNITY_2023_2_OR_NEWER
    [UxmlElement]
    public partial class SG_Slider : VisualElement
#else
    public class SG_Slider : VisualElement
#endif
    {
#if !UNITY_2023_2_OR_NEWER
        public new class UxmlFactory : UxmlFactory<SG_Slider, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Label = new() { name = "label", defaultValue = "Label" };
            UxmlFloatAttributeDescription m_Min = new() { name = "min", defaultValue = 0f };
            UxmlFloatAttributeDescription m_Max = new() { name = "max", defaultValue = 1f };
            UxmlFloatAttributeDescription m_Value = new() { name = "value", defaultValue = 0f };
            UxmlStringAttributeDescription m_Suffix = new() { name = "suffix", defaultValue = "" };
            UxmlFloatAttributeDescription m_Space = new() { name = "space", defaultValue = 0f };
            UxmlFloatAttributeDescription m_RightSpace = new() { name = "right-space", defaultValue = 0f };
            UxmlIntAttributeDescription m_Decimals = new() { name = "decimals", defaultValue = 2 };
            UxmlBoolAttributeDescription m_PercentageMode = new() { name = "percentage-mode", defaultValue = false };
            UxmlFloatAttributeDescription m_DefaultValue = new() { name = "default-value", defaultValue = float.NaN };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var control = (SG_Slider)ve;

                control.LabelText = m_Label.GetValueFromBag(bag, cc);
                control.MinValue = m_Min.GetValueFromBag(bag, cc);
                control.MaxValue = m_Max.GetValueFromBag(bag, cc);
                control.Value = m_Value.GetValueFromBag(bag, cc);
                control.Suffix = m_Suffix.GetValueFromBag(bag, cc);
                control.Space = m_Space.GetValueFromBag(bag, cc);
                control.RightSpace = m_RightSpace.GetValueFromBag(bag, cc);
                control.Decimals = m_Decimals.GetValueFromBag(bag, cc);
                control.DefaultValue = m_DefaultValue.GetValueFromBag(bag, cc);
                control.PercentageMode = m_PercentageMode.GetValueFromBag(bag, cc);
            }
        }
#endif

        private readonly Label titleLabel;
        private readonly Slider slider;
        private readonly Label valueLabel;
        private readonly TextField editField;
        private readonly Label resetButton;

        private int decimals = 2;
        private bool percentageMode;
        private string suffix = "";
        private float defaultValue = float.NaN;
        private bool showReset;
        private bool editing;
        private bool mixed;

        private const string MixedLabel = "—";

        public event System.Action<float> OnValueChanged;

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("label")]
#endif
        public string LabelText { get => titleLabel.text; set => titleLabel.text = value; }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("suffix")]
#endif
        public string Suffix
        {
            get => suffix;
            set
            {
                suffix = value ?? "";
                UpdateValueLabel(Value);
            }
        }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("min")]
#endif
        public float MinValue { get => slider.lowValue; set => slider.lowValue = value; }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("max")]
#endif
        public float MaxValue { get => slider.highValue; set => slider.highValue = value; }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("value")]
#endif
        public float Value
        {
            get => slider.value;
            set
            {
                slider.SetValueWithoutNotify(value);
                UpdateValueLabel(value);
            }
        }

        /// <summary>
        /// Shows "—" instead of the value (and the native mixed slider) when the edited objects
        /// differ. Cleared automatically as soon as the user drags or types a value.
        /// </summary>
        public bool ShowMixedValue
        {
            set
            {
                mixed = value;
                slider.showMixedValue = value;
                UpdateValueLabel(slider.value);
            }
        }

        /// <summary>
        /// The value the reset button restores, and setting it is what turns the button on. Left
        /// unset, no reset is shown — there has to be a meaningful value to go back to.
        /// </summary>
#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("default-value")]
#endif
        public float DefaultValue
        {
            get => defaultValue;
            set
            {
                defaultValue = value;
                // NaN means "no default", so a slider that never declares one shows no button.
                ShowReset = !float.IsNaN(value);
            }
        }

        public bool ShowReset
        {
            get => showReset;
            set
            {
                showReset = value;
                resetButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                RefreshResetButton(Value);
            }
        }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("space")]
#endif
        public float Space
        {
            get => titleLabel.style.marginRight.value.value;
            set => titleLabel.style.marginRight = value;
        }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("right-space")]
#endif
        public float RightSpace
        {
            get => valueLabel.style.marginLeft.value.value;
            set => valueLabel.style.marginLeft = value;
        }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("decimals")]
#endif
        public int Decimals
        {
            get => decimals;
            set
            {
                decimals = value < 0 ? 0 : value;
                UpdateValueLabel(Value);
            }
        }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("percentage-mode")]
#endif
        public bool PercentageMode
        {
            get => percentageMode;
            set
            {
                percentageMode = value;
                UpdateValueLabel(Value);
            }
        }

        public SG_Slider ()
        {
            AddToClassList("sg-setting-row");

            var row = new VisualElement();
            row.AddToClassList("sg-setting-row__content");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            titleLabel = new Label("Label");
            titleLabel.AddToClassList("sg-setting-row__title");

            slider = new Slider();
            slider.AddToClassList("sg-setting-row__slider");
            slider.style.flexGrow = 1f;

            valueLabel = new Label();
            valueLabel.AddToClassList("sg-setting-row__value");

            // Click the value to type it in directly. A left gap + explicit hit area keep the click
            // from landing on the slider's handle (which sits under the value at high values), and
            // pickingMode Position makes sure the label itself receives the pointer.
            valueLabel.tooltip = "Click to type a value";
            valueLabel.pickingMode = PickingMode.Position;
            valueLabel.style.marginLeft = 6;
            valueLabel.style.paddingLeft = 2;
            valueLabel.style.paddingRight = 2;
            valueLabel.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return;
                BeginEdit();
                e.StopPropagation();
                e.StopImmediatePropagation();
            });

            editField = new TextField { isDelayed = false };
            editField.style.display = DisplayStyle.None;
            editField.style.minWidth = 44;
            editField.style.marginLeft = 4;
            editField.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    CommitEdit();
                    e.StopPropagation();
                }
                else if (e.keyCode == KeyCode.Escape)
                {
                    CancelEdit();
                    e.StopPropagation();
                }
            });
            editField.RegisterCallback<FocusOutEvent>(_ => { if (editing) CommitEdit(); });

            resetButton = new Label("×") { tooltip = "Reset to default" };
            resetButton.style.display = DisplayStyle.None;
            resetButton.style.marginLeft = 4;
            resetButton.style.paddingLeft = 2;
            resetButton.style.paddingRight = 2;
            resetButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            resetButton.style.opacity = 0.5f;
            resetButton.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return;
                slider.value = Mathf.Clamp(defaultValue, slider.lowValue, slider.highValue);
                e.StopPropagation();
            });
            resetButton.RegisterCallback<PointerEnterEvent>(_ => resetButton.style.opacity = 1f);
            resetButton.RegisterCallback<PointerLeaveEvent>(_ => RefreshResetButton(slider.value));

            row.Add(titleLabel);
            row.Add(slider);
            row.Add(valueLabel);
            row.Add(editField);
            row.Add(resetButton);

            Add(row);

            slider.RegisterValueChangedCallback(evt =>
            {
                mixed = false;
                UpdateValueLabel(evt.newValue);
                OnValueChanged?.Invoke(evt.newValue);
            });

            UpdateValueLabel(slider.value);
        }

        private void UpdateValueLabel (float val)
        {
            valueLabel.text = mixed ? MixedLabel : FormatDisplay(val);
            RefreshResetButton(val);
        }

        private string FormatDisplay (float val)
        {
            float displayValue = percentageMode ? val * 100f : val;
            string format = "F" + (decimals < 0 ? 0 : decimals);
            return displayValue.ToString(format) + suffix;
        }

        // Dim the reset button when the value already matches the default (nothing to reset).
        private void RefreshResetButton (float val)
        {
            if (!showReset) return;
            resetButton.style.opacity = Mathf.Approximately(val, defaultValue) ? 0.3f : 0.6f;
        }

        // ----- Type-a-value editing on the value label -----

        private void BeginEdit ()
        {
            if (editing) return;
            editing = true;

            float display = percentageMode ? slider.value * 100f : slider.value;
            editField.SetValueWithoutNotify(display.ToString("F" + (decimals < 0 ? 0 : decimals),
                CultureInfo.CurrentCulture));

            valueLabel.style.display = DisplayStyle.None;
            editField.style.display = DisplayStyle.Flex;

            // Focus only works once the field has been laid out (it was display:none until now),
            // so defer it a frame, then select the text for quick overwriting.
            editField.schedule.Execute(() =>
            {
                editField.Focus();
                editField.SelectAll();
            }).StartingIn(0);
        }

        private void CommitEdit ()
        {
            if (!editing) return;
            editing = false;

            string text = editField.value;
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out float parsed) ||
                float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                float raw = percentageMode ? parsed / 100f : parsed;
                raw = Mathf.Clamp(raw, slider.lowValue, slider.highValue);
                slider.value = raw; // notify → OnValueChanged + label refresh
            }

            editField.style.display = DisplayStyle.None;
            valueLabel.style.display = DisplayStyle.Flex;
            UpdateValueLabel(slider.value);
        }

        private void CancelEdit ()
        {
            if (!editing) return;
            editing = false;

            editField.style.display = DisplayStyle.None;
            valueLabel.style.display = DisplayStyle.Flex;
        }
    }
}
