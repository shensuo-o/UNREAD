using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
#if UNITY_2023_2_OR_NEWER
    [UxmlElement]
    public partial class SG_FloatField : VisualElement
#else
    public class SG_FloatField : VisualElement
#endif
    {
#if !UNITY_2023_2_OR_NEWER
        public new class UxmlFactory : UxmlFactory<SG_FloatField, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Label = new() { name = "label", defaultValue = "Label" };
            UxmlFloatAttributeDescription m_Value = new() { name = "value", defaultValue = 0f };
            UxmlStringAttributeDescription m_Suffix = new() { name = "suffix", defaultValue = "" };
            UxmlFloatAttributeDescription m_LeftSpace = new() { name = "left-space", defaultValue = 0f };
            UxmlFloatAttributeDescription m_RightSpace = new() { name = "right-space", defaultValue = 0f };
            UxmlIntAttributeDescription m_Decimals = new() { name = "decimals", defaultValue = 2 };
            UxmlFloatAttributeDescription m_DefaultValue = new() { name = "default-value", defaultValue = float.NaN };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var control = (SG_FloatField)ve;

                control.LabelText = m_Label.GetValueFromBag(bag, cc);
                control.Decimals = m_Decimals.GetValueFromBag(bag, cc);
                control.Value = m_Value.GetValueFromBag(bag, cc);
                control.Suffix = m_Suffix.GetValueFromBag(bag, cc);
                control.LeftSpace = m_LeftSpace.GetValueFromBag(bag, cc);
                control.RightSpace = m_RightSpace.GetValueFromBag(bag, cc);
                control.DefaultValue = m_DefaultValue.GetValueFromBag(bag, cc);
            }
        }
#endif

        private readonly Label titleLabel;
        private readonly FloatField field;
        private readonly Label suffixLabel;
        private readonly Label resetButton;

        private int decimals = 2;
        private float defaultValue = float.NaN;

        public event System.Action<float> OnValueChanged;

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("label")]
#endif
        public string LabelText { get => titleLabel.text; set => titleLabel.text = value; }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("suffix")]
#endif
        public string Suffix { get => suffixLabel.text; set => suffixLabel.text = value; }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("value")]
#endif
        public float Value
        {
            get => field.value;
            set
            {
                field.SetValueWithoutNotify(Round(value));
                RefreshResetButton();
            }
        }

        /// <summary>
        /// Decimal places the value is rounded to on commit. Typing or scrubbing a longer number is
        /// allowed, it just lands on the nearest step — a field showing 0.30000001 helps nobody.
        /// </summary>
#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("decimals")]
#endif
        public int Decimals
        {
            get => decimals;
            set
            {
                decimals = value < 0 ? 0 : value;
                Value = field.value;
            }
        }

        /// <summary>
        /// The value the reset button restores. Leave it unset (NaN) and no reset button is shown,
        /// which is the right call wherever there's no meaningful value to go back to.
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
                bool hasDefault = !float.IsNaN(value);
                resetButton.style.display = hasDefault ? DisplayStyle.Flex : DisplayStyle.None;
                RefreshResetButton();
            }
        }

        /// <summary> Shows the native "—" placeholder when the edited objects have different values. </summary>
        public bool ShowMixedValue { set => field.showMixedValue = value; }

        /// <summary>
        /// When true the field only commits its value on Enter or focus loss instead of on every
        /// keystroke, so a min/max clamp can't fight the user mid-typing.
        /// </summary>
        public bool Delayed { get => field.isDelayed; set => field.isDelayed = value; }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("left-space")]
#endif
        public float LeftSpace
        {
            get => titleLabel.style.marginRight.value.value;
            set => titleLabel.style.marginRight = value;
        }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("right-space")]
#endif
        public float RightSpace
        {
            get => suffixLabel.style.marginLeft.value.value;
            set => suffixLabel.style.marginLeft = value;
        }

        public SG_FloatField ()
        {
            AddToClassList("sg-setting-row");

            var row = new VisualElement();
            row.AddToClassList("sg-setting-row__content");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            titleLabel = new Label("Label");
            titleLabel.AddToClassList("sg-setting-row__title");
            titleLabel.AddToClassList("sg-scrub-label");

            field = new FloatField
            {
                style =
                {
                    flexShrink = 1,
                    flexGrow = 1
                }
            };
            field.AddToClassList("sg-setting-row__float-field");

            suffixLabel = new Label();
            suffixLabel.AddToClassList("sg-setting-row__suffix");

            // Same "×" affordance as SG_Slider, so a reset reads identically wherever it appears.
            resetButton = new Label("×") { tooltip = "Reset to default" };
            resetButton.style.display = DisplayStyle.None;
            resetButton.style.marginLeft = 4;
            resetButton.style.paddingLeft = 2;
            resetButton.style.paddingRight = 2;
            resetButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            resetButton.style.opacity = 0.5f;
            resetButton.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0 || float.IsNaN(defaultValue)) return;
                // With notify, so whoever is listening writes the restored value out.
                field.value = defaultValue;
                e.StopPropagation();
            });
            resetButton.RegisterCallback<PointerEnterEvent>(_ => resetButton.style.opacity = 1f);
            resetButton.RegisterCallback<PointerLeaveEvent>(_ => RefreshResetButton());

            row.Add(titleLabel);
            row.Add(field);
            row.Add(suffixLabel);
            row.Add(resetButton);

            Add(row);

            field.RegisterValueChangedCallback(evt =>
            {
                float rounded = Round(evt.newValue);
                // Writing back without notify avoids re-entering this callback; listeners are told
                // about the rounded value below either way.
                if (!Mathf.Approximately(rounded, evt.newValue)) field.SetValueWithoutNotify(rounded);

                RefreshResetButton();
                OnValueChanged?.Invoke(rounded);
            });

            RegisterLabelScrub();
        }

        private float Round (float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return value;
            return (float)System.Math.Round(value, decimals, System.MidpointRounding.AwayFromZero);
        }

        // Dim the reset button when the value already matches the default (nothing to reset).
        private void RefreshResetButton ()
        {
            if (float.IsNaN(defaultValue)) return;
            resetButton.style.opacity = Mathf.Approximately(field.value, defaultValue) ? 0.3f : 0.6f;
        }

        // ----- Click-and-drag scrubbing on the label, mirroring Unity's serialized number fields.
        private bool scrubbing;
        private int scrubPointerId;

        private void RegisterLabelScrub ()
        {
            titleLabel.RegisterCallback<PointerDownEvent>(OnScrubDown);
            titleLabel.RegisterCallback<PointerMoveEvent>(OnScrubMove);
            titleLabel.RegisterCallback<PointerUpEvent>(OnScrubUp);
        }

        private void OnScrubDown (PointerDownEvent e)
        {
            if (e.button != 0) return;

            scrubbing = true;
            scrubPointerId = e.pointerId;
            titleLabel.CapturePointer(e.pointerId);
            e.StopPropagation();
        }

        private void OnScrubMove (PointerMoveEvent e)
        {
            if (!scrubbing) return;

            // Unity scales the drag speed by the value's magnitude, so big numbers move fast and
            // small ones stay fine-grained.
            float sensitivity = Mathf.Max(1f, Mathf.Pow(Mathf.Abs(field.value), 0.5f)) * 0.03f;
            float newValue = field.value + e.deltaPosition.x * sensitivity;

            // Setting value (with notify) runs the same path as typing, so min clamps still apply.
            field.value = newValue;
            e.StopPropagation();
        }

        private void OnScrubUp (PointerUpEvent e)
        {
            if (!scrubbing || e.pointerId != scrubPointerId) return;

            scrubbing = false;
            titleLabel.ReleasePointer(e.pointerId);
            e.StopPropagation();
        }
    }
}