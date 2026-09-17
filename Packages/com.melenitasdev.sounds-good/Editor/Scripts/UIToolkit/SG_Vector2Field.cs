using UnityEngine;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
    /// <summary>
    /// A two-component row in the same style as the rest of the SG_ controls, with a reset button.
    /// <para>
    /// The sub-labels are configurable because every <c>Vector2</c> in Sounds Good is really a
    /// <b>range</b>: "Min" and "Max" say what the two numbers mean, where Unity's stock "X" and "Y"
    /// say nothing.
    /// </para>
    /// </summary>
#if UNITY_2023_2_OR_NEWER
    [UxmlElement]
    public partial class SG_Vector2Field : VisualElement
#else
    public class SG_Vector2Field : VisualElement
#endif
    {
#if !UNITY_2023_2_OR_NEWER
        public new class UxmlFactory : UxmlFactory<SG_Vector2Field, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Label = new() { name = "label", defaultValue = "Label" };
            UxmlStringAttributeDescription m_XLabel = new() { name = "x-label", defaultValue = "X" };
            UxmlStringAttributeDescription m_YLabel = new() { name = "y-label", defaultValue = "Y" };
            UxmlFloatAttributeDescription m_X = new() { name = "x", defaultValue = 0f };
            UxmlFloatAttributeDescription m_Y = new() { name = "y", defaultValue = 0f };
            UxmlFloatAttributeDescription m_LeftSpace = new() { name = "left-space", defaultValue = 0f };
            UxmlIntAttributeDescription m_Decimals = new() { name = "decimals", defaultValue = 2 };

            public override void Init (VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var control = (SG_Vector2Field)ve;

                control.LabelText = m_Label.GetValueFromBag(bag, cc);
                control.XLabel = m_XLabel.GetValueFromBag(bag, cc);
                control.YLabel = m_YLabel.GetValueFromBag(bag, cc);
                control.Decimals = m_Decimals.GetValueFromBag(bag, cc);
                control.Value = new Vector2(m_X.GetValueFromBag(bag, cc), m_Y.GetValueFromBag(bag, cc));
                control.LeftSpace = m_LeftSpace.GetValueFromBag(bag, cc);
            }
        }
#endif

        private readonly Label titleLabel;
        private readonly Label xLabel;
        private readonly Label yLabel;
        private readonly FloatField xField;
        private readonly FloatField yField;
        private readonly Label resetButton;

        private int decimals = 2;
        private Vector2 defaultValue = new Vector2(float.NaN, float.NaN);

        public event System.Action<Vector2> OnValueChanged;

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("label")]
#endif
        public string LabelText { get => titleLabel.text; set => titleLabel.text = value; }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("x-label")]
#endif
        public string XLabel { get => xLabel.text; set => xLabel.text = value; }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("y-label")]
#endif
        public string YLabel { get => yLabel.text; set => yLabel.text = value; }

        public Vector2 Value
        {
            get => new Vector2(xField.value, yField.value);
            set
            {
                xField.SetValueWithoutNotify(Round(value.x));
                yField.SetValueWithoutNotify(Round(value.y));
                RefreshResetButton();
            }
        }

        /// <summary> Rounded like <see cref="SG_FloatField"/>, for the same reason. </summary>
#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("decimals")]
#endif
        public int Decimals
        {
            get => decimals;
            set
            {
                decimals = value < 0 ? 0 : value;
                Value = Value;
            }
        }

        /// <summary>
        /// The value the reset button restores. Left unset (NaN), no reset button is shown.
        /// </summary>
        public Vector2 DefaultValue
        {
            get => defaultValue;
            set
            {
                defaultValue = value;
                bool hasDefault = !float.IsNaN(value.x) && !float.IsNaN(value.y);
                resetButton.style.display = hasDefault ? DisplayStyle.Flex : DisplayStyle.None;
                RefreshResetButton();
            }
        }

        /// <summary> Shows the native "—" placeholder when the edited objects have different values. </summary>
        public bool ShowMixedValue
        {
            set
            {
                xField.showMixedValue = value;
                yField.showMixedValue = value;
            }
        }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("left-space")]
#endif
        public float LeftSpace
        {
            get => titleLabel.style.marginRight.value.value;
            set => titleLabel.style.marginRight = value;
        }

        public SG_Vector2Field ()
        {
            AddToClassList("sg-setting-row");

            var row = new VisualElement();
            row.AddToClassList("sg-setting-row__content");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            titleLabel = new Label("Label");
            titleLabel.AddToClassList("sg-setting-row__title");

            xLabel = BuildSubLabel("X");
            yLabel = BuildSubLabel("Y");
            xField = BuildSubField();
            yField = BuildSubField();

            resetButton = new Label("×") { tooltip = "Reset to default" };
            resetButton.style.display = DisplayStyle.None;
            resetButton.style.marginLeft = 4;
            resetButton.style.paddingLeft = 2;
            resetButton.style.paddingRight = 2;
            resetButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            resetButton.style.opacity = 0.5f;
            resetButton.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0 || float.IsNaN(defaultValue.x)) return;
                // With notify on both, so the listener writes the whole restored vector out.
                xField.value = defaultValue.x;
                yField.value = defaultValue.y;
                e.StopPropagation();
            });
            resetButton.RegisterCallback<PointerEnterEvent>(_ => resetButton.style.opacity = 1f);
            resetButton.RegisterCallback<PointerLeaveEvent>(_ => RefreshResetButton());

            row.Add(titleLabel);
            row.Add(xLabel);
            row.Add(xField);
            row.Add(yLabel);
            row.Add(yField);
            row.Add(resetButton);

            Add(row);

            xField.RegisterValueChangedCallback(evt => OnSubFieldChanged(xField, evt.newValue));
            yField.RegisterValueChangedCallback(evt => OnSubFieldChanged(yField, evt.newValue));
        }

        private void OnSubFieldChanged (FloatField changed, float newValue)
        {
            float rounded = Round(newValue);
            // Without notify, so this doesn't re-enter; listeners get the rounded pair below.
            if (!Mathf.Approximately(rounded, newValue)) changed.SetValueWithoutNotify(rounded);

            RefreshResetButton();
            OnValueChanged?.Invoke(Value);
        }

        private static Label BuildSubLabel (string text)
        {
            var label = new Label(text);
            label.style.marginRight = 3;
            label.style.marginLeft = 4;
            label.style.opacity = 0.7f;
            label.style.flexShrink = 0;
            return label;
        }

        private static FloatField BuildSubField ()
        {
            var subField = new FloatField
            {
                style =
                {
                    flexShrink = 1,
                    flexGrow = 1,
                    minWidth = 30
                }
            };
            subField.AddToClassList("sg-setting-row__float-field");
            return subField;
        }

        private float Round (float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return value;
            return (float)System.Math.Round(value, decimals, System.MidpointRounding.AwayFromZero);
        }

        // Dim the reset button when the value already matches the default (nothing to reset).
        private void RefreshResetButton ()
        {
            if (float.IsNaN(defaultValue.x)) return;

            bool atDefault = Mathf.Approximately(xField.value, defaultValue.x)
                             && Mathf.Approximately(yField.value, defaultValue.y);
            resetButton.style.opacity = atDefault ? 0.3f : 0.6f;
        }
    }
}
