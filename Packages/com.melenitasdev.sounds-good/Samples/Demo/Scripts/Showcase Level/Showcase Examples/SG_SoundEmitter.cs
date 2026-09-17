using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MelenitasDev.SoundsGood.Demo
{
    public class SG_SoundEmitter : MonoBehaviour, IShowcaseResettable
    {
        
        //Config Panel
        bool loop = false;
        [SerializeField] private TMP_Text loopButtonText;

        [SerializeField] private Slider pitchSlider;
        [SerializeField] private TMP_Text pitchValueText;


        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Text volumeValueText;
        
        
        
        [SerializeField] private SoundEmitter soundEmitter;

        public void SetVolume ()
        {
            float normalizedVolume = Mathf.Clamp01(volumeSlider.value / 100f);
            soundEmitter.Sound.ChangeVolume(normalizedVolume);
            volumeValueText.text = volumeSlider.value.ToString("F0") + "%";
        }

        public void RestVolume()
        {
            volumeSlider.value = 100;
            soundEmitter.Sound.ChangeVolume(1);
        }

        public void SetPitch ()
        {
            soundEmitter.Sound.ChangePitch(pitchSlider.value);
            pitchValueText.text = pitchSlider.value.ToString("F2");
        }

        public void RestPitch()
        {
            soundEmitter.Sound.ChangePitch(1);
            pitchSlider.value = 1;
            pitchValueText.text = pitchSlider.value.ToString("F2");
        }

        
        public void SetLoop ()
        {
            if (soundEmitter.IsPlaying) soundEmitter.Stop();

            SetLoopState(!loop);
        }

        private void SetLoopState (bool enabled)
        {
            loop = enabled;
            loopButtonText.text = loop ? "Loop: ON" : "Loop: OFF";
            soundEmitter.Sound.SetLoop(loop);
        }

        public void RestAll()
        {
            RestVolume();
            RestPitch();
            SetLoopState(false);
        }

        public void StopAll()
        {
            soundEmitter.Stop();
        }

        public void ResetShowcaseState ()
        {
            StopAll();
            RestAll();
        }
    }
}
