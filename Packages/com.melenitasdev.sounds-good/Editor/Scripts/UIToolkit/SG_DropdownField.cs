using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace MelenitasDev.SoundsGood.Editor
{
#if UNITY_2023_2_OR_NEWER
    [UxmlElement]
    public partial class SG_DropdownField : VisualElement
#else
    public class SG_DropdownField : VisualElement
#endif
    {
#if !UNITY_2023_2_OR_NEWER
        public new class UxmlFactory : UxmlFactory<SG_DropdownField, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Label = new() { name = "label", defaultValue = "Label" };
            UxmlStringAttributeDescription m_Choices = new() { name = "choices", defaultValue = "" };
            UxmlIntAttributeDescription m_Index = new() { name = "index", defaultValue = 0 };
            UxmlFloatAttributeDescription m_Space = new() { name = "space", defaultValue = 0f };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var control = (SG_DropdownField)ve;

                control.LabelText = m_Label.GetValueFromBag(bag, cc);
                control.Choices = m_Choices.GetValueFromBag(bag, cc);
                control.Index = m_Index.GetValueFromBag(bag, cc);
                control.Space = m_Space.GetValueFromBag(bag, cc);
            }
        }
#endif

        private readonly Label titleLabel;
        private readonly DropdownField dropdown;

        public event System.Action<int> OnValueChanged;

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("label")]
#endif
        public string LabelText { get => titleLabel.text; set => titleLabel.text = value; }

        /// <summary> The options, as a comma-separated list so they can be authored in UXML. </summary>
#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("choices")]
#endif
        public string Choices
        {
            get => string.Join(",", dropdown.choices);
            set
            {
                List<string> parsed = string.IsNullOrWhiteSpace(value)
                    ? new List<string>()
                    : value.Split(',').Select(choice => choice.Trim()).ToList();

                dropdown.choices = parsed;
                if (parsed.Count > 0 && dropdown.index < 0) dropdown.SetValueWithoutNotify(parsed[0]);
            }
        }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("index")]
#endif
        public int Index
        {
            get => dropdown.index;
            set
            {
                if (dropdown.choices == null || dropdown.choices.Count == 0) return;
                int clamped = value < 0 ? 0 : (value >= dropdown.choices.Count ? dropdown.choices.Count - 1 : value);
                dropdown.SetValueWithoutNotify(dropdown.choices[clamped]);
            }
        }

        /// <summary> Shows the native mixed-value indicator when the edited objects differ. </summary>
        public bool ShowMixedValue { set => dropdown.showMixedValue = value; }

#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute("space")]
#endif
        public float Space
        {
            get => titleLabel.style.marginRight.value.value;
            set => titleLabel.style.marginRight = value;
        }

        public SG_DropdownField ()
        {
            AddToClassList("sg-setting-row");

            var row = new VisualElement();
            row.AddToClassList("sg-setting-row__content");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            titleLabel = new Label("Label");
            titleLabel.AddToClassList("sg-setting-row__title");

            dropdown = new DropdownField
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1
                }
            };
            dropdown.AddToClassList("sg-setting-row__text-field");

            row.Add(titleLabel);
            row.Add(dropdown);
            Add(row);

            dropdown.RegisterValueChangedCallback(_ => { OnValueChanged?.Invoke(dropdown.index); });
        }
    }
}
