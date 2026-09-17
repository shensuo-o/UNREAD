using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
#if UNITY_2023_2_OR_NEWER
    [UxmlElement]
    public partial class SG_StringField : VisualElement
#else
    public class SG_StringField : VisualElement
#endif
    {
#if !UNITY_2023_2_OR_NEWER
        public new class UxmlFactory : UxmlFactory<SG_StringField, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Label = new() { name = "label", defaultValue = "Label" };
            UxmlStringAttributeDescription m_Value = new() { name = "value", defaultValue = "" };
            UxmlStringAttributeDescription m_Suffix = new() { name = "suffix", defaultValue = "" };
            UxmlFloatAttributeDescription m_LeftSpace = new() { name = "left-space", defaultValue = 0f };
            UxmlFloatAttributeDescription m_RightSpace = new() { name = "right-space", defaultValue = 0f };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var control = (SG_StringField)ve;

                control.LabelText = m_Label.GetValueFromBag(bag, cc);
                control.Value = m_Value.GetValueFromBag(bag, cc);
                control.Suffix = m_Suffix.GetValueFromBag(bag, cc);
                control.LeftSpace = m_LeftSpace.GetValueFromBag(bag, cc);
                control.RightSpace = m_RightSpace.GetValueFromBag(bag, cc);
            }
        }
#endif

        private readonly Label titleLabel;
        private readonly TextField field;
        private readonly Label suffixLabel;

        public VisualElement TextField => this.Q<VisualElement>("unity-text-input");
        public event System.Action<string> OnValueChanged;

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
        public string Value { get => field.value; set => field.SetValueWithoutNotify(value); }

        /// <summary> Shows the native "—" placeholder when the edited objects have different values. </summary>
        public bool ShowMixedValue { set => field.showMixedValue = value; }

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

        public SG_StringField ()
        {
            AddToClassList("sg-setting-row");

            var row = new VisualElement();
            row.AddToClassList("sg-setting-row__content");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            titleLabel = new Label("Label");
            titleLabel.AddToClassList("sg-setting-row__title");

            field = new TextField
            {
                style =
                {
                    flexShrink = 1,
                    flexGrow = 1
                }
            };
            field.AddToClassList("sg-setting-row__text-field");

            suffixLabel = new Label();
            suffixLabel.AddToClassList("sg-setting-row__suffix");

            row.Add(titleLabel);
            row.Add(field);
            row.Add(suffixLabel);

            Add(row);

            field.RegisterValueChangedCallback(evt => { OnValueChanged?.Invoke(evt.newValue); });
        }
    }
}