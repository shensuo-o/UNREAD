using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
#if UNITY_2023_2_OR_NEWER
    [UxmlElement]
    public partial class SG_BoolField : VisualElement
#else
    public class SG_BoolField : VisualElement
#endif
    {
#if !UNITY_2023_2_OR_NEWER
        public new class UxmlFactory : UxmlFactory<SG_BoolField, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Label = new() { name = "label", defaultValue = "Label" };
            UxmlBoolAttributeDescription m_Value = new() { name = "value", defaultValue = false };
            UxmlFloatAttributeDescription m_Space = new() { name = "space", defaultValue = 0f };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var control = (SG_BoolField)ve;

                control.LabelText = m_Label.GetValueFromBag(bag, cc);
                control.Value = m_Value.GetValueFromBag(bag, cc);
                control.Space = m_Space.GetValueFromBag(bag, cc);
            }
        }
#endif

        private readonly Label titleLabel;
        private readonly Toggle toggle;

        public event System.Action<bool> OnValueChanged;

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("label")]
#endif
        public string LabelText { get => titleLabel.text; set => titleLabel.text = value; }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("value")]
#endif
        public bool Value { get => toggle.value; set => toggle.SetValueWithoutNotify(value); }

        /// <summary> Shows the native mixed-value indicator when the edited objects differ. </summary>
        public bool ShowMixedValue { set => toggle.showMixedValue = value; }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("space")]
#endif
        public float Space
        {
            get => titleLabel.style.marginRight.value.value;
            set => titleLabel.style.marginRight = value;
        }

        public SG_BoolField ()
        {
            AddToClassList("sg-setting-row");

            var row = new VisualElement();
            row.AddToClassList("sg-setting-row__content");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            titleLabel = new Label("Label");
            titleLabel.AddToClassList("sg-setting-row__title");

            toggle = new Toggle
            {
                style =
                {
                    flexGrow = 0,
                    flexShrink = 0
                }
            };
            toggle.AddToClassList("sg-setting-row__toggle");

            row.Add(titleLabel);
            row.Add(toggle);
            Add(row);

            toggle.RegisterValueChangedCallback(evt => { OnValueChanged?.Invoke(evt.newValue); });
        }
    }
}