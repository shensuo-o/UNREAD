using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
#if UNITY_2023_2_OR_NEWER
    [UxmlElement]
    public partial class SG_LayerMaskField : VisualElement
#else
    public class SG_LayerMaskField : VisualElement
#endif
    {
#if !UNITY_2023_2_OR_NEWER
        public new class UxmlFactory : UxmlFactory<SG_LayerMaskField, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Label = new() { name = "label", defaultValue = "Layer Mask" };
            UxmlIntAttributeDescription m_Value = new() { name = "value", defaultValue = 0 };
            UxmlFloatAttributeDescription m_Space = new() { name = "space", defaultValue = 0f };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var control = (SG_LayerMaskField)ve;

                control.LabelText = m_Label.GetValueFromBag(bag, cc);
                control.Value = m_Value.GetValueFromBag(bag, cc);
                control.Space = m_Space.GetValueFromBag(bag, cc);
            }
        }
#endif

        private readonly Label titleLabel;
        private readonly LayerMaskField layerMaskField;

        public event System.Action<int> OnValueChanged;

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("label")]
#endif
        public string LabelText { get => titleLabel.text; set => titleLabel.text = value; }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("value")]
#endif
        public int Value { get => layerMaskField.value; set => layerMaskField.SetValueWithoutNotify(value); }

        /// <summary> Shows the native mixed-value indicator when the edited objects differ. </summary>
        public bool ShowMixedValue { set => layerMaskField.showMixedValue = value; }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("space")]
#endif
        public float Space
        {
            get => titleLabel.style.marginRight.value.value;
            set => titleLabel.style.marginRight = value;
        }

        public SG_LayerMaskField ()
        {
            AddToClassList("sg-setting-row");

            var row = new VisualElement();
            row.AddToClassList("sg-setting-row__content");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            titleLabel = new Label("Layer Mask");
            titleLabel.AddToClassList("sg-setting-row__title");

            layerMaskField = new LayerMaskField
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1
                }
            };
            layerMaskField.AddToClassList("sg-setting-row__text-field");

            row.Add(titleLabel);
            row.Add(layerMaskField);
            Add(row);

            layerMaskField.RegisterValueChangedCallback(evt => { OnValueChanged?.Invoke(evt.newValue); });
        }
    }
}