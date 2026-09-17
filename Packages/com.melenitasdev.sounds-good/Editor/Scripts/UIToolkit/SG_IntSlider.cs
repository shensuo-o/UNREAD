using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
#if UNITY_2023_2_OR_NEWER
    [UxmlElement]
    public partial class SG_IntSlider : VisualElement
#else
    public class SG_IntSlider : VisualElement
#endif
    {
#if !UNITY_2023_2_OR_NEWER
        public new class UxmlFactory : UxmlFactory<SG_IntSlider, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Label = new() { name = "label", defaultValue = "Label" };
            UxmlIntAttributeDescription m_Min = new() { name = "min", defaultValue = 0 };
            UxmlIntAttributeDescription m_Max = new() { name = "max", defaultValue = 10 };
            UxmlIntAttributeDescription m_Value = new() { name = "value", defaultValue = 0 };
            UxmlStringAttributeDescription m_Suffix = new() { name = "suffix", defaultValue = "" };
            UxmlFloatAttributeDescription m_Space = new() { name = "space", defaultValue = 0f };
            UxmlFloatAttributeDescription m_RightSpace = new() { name = "right-space", defaultValue = 0f };
            UxmlIntAttributeDescription m_DefaultValue = new() { name = "default-value", defaultValue = int.MinValue };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var control = (SG_IntSlider)ve;

                control.LabelText = m_Label.GetValueFromBag(bag, cc);
                control.MinValue = m_Min.GetValueFromBag(bag, cc);
                control.MaxValue = m_Max.GetValueFromBag(bag, cc);
                control.Value = m_Value.GetValueFromBag(bag, cc);
                control.Suffix = m_Suffix.GetValueFromBag(bag, cc);
                control.Space = m_Space.GetValueFromBag(bag, cc);
                control.RightSpace = m_RightSpace.GetValueFromBag(bag, cc);
                control.DefaultValue = m_DefaultValue.GetValueFromBag(bag, cc);
            }
        }
#endif

        private readonly Label titleLabel;
        private readonly SliderInt slider;
        private readonly Label valueLabel;
        private readonly Label resetButton;

        private string suffix = "";
        private bool mixed;
        // int.MinValue stands for "no default", the way NaN does on the float controls.
        private int defaultValue = int.MinValue;

        private const string MixedLabel = "—";

        public event System.Action<int> OnValueChanged;

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
        public int MinValue { get => slider.lowValue; set => slider.lowValue = value; }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("max")]
#endif
        public int MaxValue { get => slider.highValue; set => slider.highValue = value; }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("value")]
#endif
        public int Value
        {
            get => slider.value;
            set
            {
                slider.SetValueWithoutNotify(value);
                UpdateValueLabel(value);
            }
        }

        /// <summary>
        /// The value the reset button restores. Left unset, no reset button is shown — there has to
        /// be a meaningful value to go back to for the button to mean anything.
        /// </summary>
#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("default-value")]
#endif
        public int DefaultValue
        {
            get => defaultValue;
            set
            {
                defaultValue = value;
                resetButton.style.display = value == int.MinValue ? DisplayStyle.None : DisplayStyle.Flex;
                RefreshResetButton(slider.value);
            }
        }

        /// <summary> Shows "—" instead of the value when the edited objects differ. </summary>
        public bool ShowMixedValue
        {
            set
            {
                mixed = value;
                slider.showMixedValue = value;
                UpdateValueLabel(slider.value);
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

        public SG_IntSlider ()
        {
            AddToClassList("sg-setting-row");

            var row = new VisualElement();
            row.AddToClassList("sg-setting-row__content");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            titleLabel = new Label("Label");
            titleLabel.AddToClassList("sg-setting-row__title");

            slider = new SliderInt();
            slider.AddToClassList("sg-setting-row__slider");
            slider.style.flexGrow = 1f;

            valueLabel = new Label();
            valueLabel.AddToClassList("sg-setting-row__value");

            // Same "×" affordance as the float controls, so a reset reads identically everywhere.
            resetButton = new Label("×") { tooltip = "Reset to default" };
            resetButton.style.display = DisplayStyle.None;
            resetButton.style.marginLeft = 4;
            resetButton.style.paddingLeft = 2;
            resetButton.style.paddingRight = 2;
            resetButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            resetButton.style.opacity = 0.5f;
            resetButton.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0 || defaultValue == int.MinValue) return;
                slider.value = Mathf.Clamp(defaultValue, slider.lowValue, slider.highValue);
                e.StopPropagation();
            });
            resetButton.RegisterCallback<PointerEnterEvent>(_ => resetButton.style.opacity = 1f);
            resetButton.RegisterCallback<PointerLeaveEvent>(_ => RefreshResetButton(slider.value));

            row.Add(titleLabel);
            row.Add(slider);
            row.Add(valueLabel);
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

        private void UpdateValueLabel (int val)
        {
            valueLabel.text = mixed ? MixedLabel : $"{val}{suffix}";
            RefreshResetButton(val);
        }

        // Dim the reset button when the value already matches the default (nothing to reset).
        private void RefreshResetButton (int val)
        {
            if (defaultValue == int.MinValue) return;
            resetButton.style.opacity = val == defaultValue ? 0.3f : 0.6f;
        }
    }
}