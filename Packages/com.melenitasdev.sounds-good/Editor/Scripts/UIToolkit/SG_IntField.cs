using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
    /// <summary>
    /// The integer counterpart of <see cref="SG_FloatField"/>: same row layout, label scrubbing and
    /// reset button, so an int setting doesn't stand out as Unity's default field among them.
    /// </summary>
#if UNITY_2023_2_OR_NEWER
    [UxmlElement]
    public partial class SG_IntField : VisualElement
#else
    public class SG_IntField : VisualElement
#endif
    {
#if !UNITY_2023_2_OR_NEWER
        public new class UxmlFactory : UxmlFactory<SG_IntField, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Label = new() { name = "label", defaultValue = "Label" };
            UxmlIntAttributeDescription m_Value = new() { name = "value", defaultValue = 0 };
            UxmlStringAttributeDescription m_Suffix = new() { name = "suffix", defaultValue = "" };
            UxmlFloatAttributeDescription m_LeftSpace = new() { name = "left-space", defaultValue = 0f };
            UxmlFloatAttributeDescription m_RightSpace = new() { name = "right-space", defaultValue = 0f };
            UxmlIntAttributeDescription m_DefaultValue = new() { name = "default-value", defaultValue = int.MinValue };

            public override void Init (VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var control = (SG_IntField)ve;

                control.LabelText = m_Label.GetValueFromBag(bag, cc);
                control.Value = m_Value.GetValueFromBag(bag, cc);
                control.Suffix = m_Suffix.GetValueFromBag(bag, cc);
                control.LeftSpace = m_LeftSpace.GetValueFromBag(bag, cc);
                control.RightSpace = m_RightSpace.GetValueFromBag(bag, cc);
                control.DefaultValue = m_DefaultValue.GetValueFromBag(bag, cc);
            }
        }
#endif

        private readonly Label titleLabel;
        private readonly IntegerField field;
        private readonly Label suffixLabel;
        private readonly Label resetButton;

        // int.MinValue stands for "no default", the way NaN does on the float controls.
        private int defaultValue = int.MinValue;

        public event System.Action<int> OnValueChanged;

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
        public int Value
        {
            get => field.value;
            set
            {
                field.SetValueWithoutNotify(value);
                RefreshResetButton();
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

        public SG_IntField ()
        {
            AddToClassList("sg-setting-row");

            var row = new VisualElement();
            row.AddToClassList("sg-setting-row__content");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            titleLabel = new Label("Label");
            titleLabel.AddToClassList("sg-setting-row__title");
            titleLabel.AddToClassList("sg-scrub-label");

            field = new IntegerField
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
                RefreshResetButton();
                OnValueChanged?.Invoke(evt.newValue);
            });

            RegisterLabelScrub();
        }

        // Dim the reset button when the value already matches the default (nothing to reset).
        private void RefreshResetButton ()
        {
            if (defaultValue == int.MinValue) return;
            resetButton.style.opacity = field.value == defaultValue ? 0.3f : 0.6f;
        }

        // ----- Click-and-drag scrubbing on the label, mirroring Unity's serialized number fields.
        private bool scrubbing;
        private int scrubPointerId;
        // Drag distance not yet worth a whole step. Without it a slow drag would never move at all,
        // since every individual frame rounds down to zero.
        private float scrubRemainder;

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
            scrubRemainder = 0f;
            titleLabel.CapturePointer(e.pointerId);
            e.StopPropagation();
        }

        private void OnScrubMove (PointerMoveEvent e)
        {
            if (!scrubbing) return;

            scrubRemainder += e.deltaPosition.x * 0.1f;
            int steps = (int)scrubRemainder;
            if (steps != 0)
            {
                scrubRemainder -= steps;
                // Setting value (with notify) runs the same path as typing, so clamps still apply.
                field.value += steps;
            }

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
