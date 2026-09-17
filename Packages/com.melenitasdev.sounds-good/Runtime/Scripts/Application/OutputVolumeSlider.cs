using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MelenitasDev.SoundsGood
{
    public partial class OutputVolumeSlider // Serialized Fields
    {
        [Header("Settings")]
        [SerializeField] private Output targetAudioOutput;

        [Header("References")]
        [SerializeField] private TextMeshProUGUI outputTitleLabel;
        [SerializeField] private TextMeshProUGUI percentageLabel;
    }

    public partial class OutputVolumeSlider // Serialized Fields
    {
        private Slider volumeSlider;
    }

    [RequireComponent(typeof(Slider))]
    [AddComponentMenu("Sounds Good/UI/Output Volume Slider", 60)]
    public partial class OutputVolumeSlider : MonoBehaviour
    {
        void Awake ()
        {
            volumeSlider = GetComponent<Slider>();
            volumeSlider.onValueChanged.AddListener(ChangeVolume);
        }

        void Start ()
        {
            string outputName = Regex.Replace(targetAudioOutput.ToString(), @"(\p{Ll})(\p{Lu})", "$1 $2");
            outputTitleLabel.text = outputName;

            SetLastSavedVolume();
        }
    }

    public partial class OutputVolumeSlider // Public Methods
    {
        public void ChangeVolume (float volume)
        {
            SoundsGoodManager.ChangeOutputVolume(targetAudioOutput, volume);
            RefreshPercentage(volume);
        }
    }

    public partial class OutputVolumeSlider // Private Methods
    {
        private void SetLastSavedVolume ()
        {
            float volume = SoundsGoodManager.GetSavedOutputVolume(targetAudioOutput.ToString());
            ChangeVolume(volume);
            volumeSlider.value = volume;
        }

        private void RefreshPercentage (float volume)
        {
            percentageLabel.text = $"{volume * 100:F0}%";
        }
    }
}
