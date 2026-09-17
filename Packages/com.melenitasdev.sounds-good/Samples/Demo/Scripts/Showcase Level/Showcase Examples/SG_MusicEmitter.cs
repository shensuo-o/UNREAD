using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MelenitasDev.SoundsGood.Demo
{
    public class SG_MusicEmitter : MonoBehaviour, IShowcaseResettable
    {
        //Config Panel
        private bool spatial = true;
        private bool playbackRequested;

        [SerializeField] private SG_AlternatingButton playStopButton;
        [SerializeField] private TMP_Text spatialSoundButtonText;

        [SerializeField] private Slider pitchSlider;
        [SerializeField] private TMP_Text pitchValueText;


        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Text volumeValueText;
        
        [SerializeField] private MusicEmitter musicEmitter;
        [SerializeField] private ParticleSystem[] playbackParticles;

        private bool stopRequested;

        void Start ()
        {
            playbackRequested = musicEmitter != null && musicEmitter.IsPlaying;
            stopRequested = false;
            SetPlaybackParticles(playbackRequested);
            UpdateSpatialText();
            SyncPlayStopButton();
        }

        void Update ()
        {
            RefreshPlaybackState();
        }

        public void PlayMusic ()
        {
            if (musicEmitter == null)
            {
                return;
            }

            musicEmitter.Play();
            playbackRequested = true;
            stopRequested = false;
            SetPlaybackParticles(true);
            SyncPlayStopButton();
        }

        public void StopMusic ()
        {
            if (musicEmitter == null)
            {
                return;
            }

            musicEmitter.Stop();
            playbackRequested = false;
            stopRequested = true;
            SetPlaybackParticles(false);
            SyncPlayStopButton();
        }

        public void ToggleMusic ()
        {
            if (playbackRequested)
            {
                StopMusic();
                return;
            }

            PlayMusic();
        }

        public void SetVolume ()
        {
            float normalizedVolume = Mathf.Clamp01(volumeSlider.value / 100f);
            musicEmitter.Music.ChangeVolume(normalizedVolume);
            volumeValueText.text = volumeSlider.value.ToString("F0") + "%";
        }

        public void RestVolume()
        {
            volumeSlider.value = 100;
            musicEmitter.Music.ChangeVolume(1);
        }

        public void SetPitch ()
        {
            musicEmitter.Music.ChangePitch(pitchSlider.value);
            pitchValueText.text = pitchSlider.value.ToString("F2");
        }

        public void RestPitch()
        {
            musicEmitter.Music.ChangePitch(1);
            pitchSlider.value = 1;
            pitchValueText.text = pitchSlider.value.ToString("F2");
        }

        public void SetSpatial ()
        {
            bool stopAfterApplyingSpatial = musicEmitter != null && (playbackRequested || musicEmitter.IsPlaying);

            spatial = !spatial;
            ApplySpatial();

            if (stopAfterApplyingSpatial)
            {
                StopMusic();
            }
        }

        public void RestAll()
        {
            RestVolume();
            RestPitch();
            spatial = true;
            ApplySpatial();
            StopMusic();
        }

        public void StopAll()
        {
            StopMusic();
        }

        public void ResetShowcaseState ()
        {
            RestAll();
        }

        private void ApplySpatial ()
        {
            UpdateSpatialText();

            if (musicEmitter == null)
            {
                return;
            }

            musicEmitter.Music.SetSpatialSound(spatial);
            SyncPlayStopButton();
        }

        private void RefreshPlaybackState ()
        {
            if (musicEmitter == null)
            {
                return;
            }

            if (musicEmitter.IsPlaying)
            {
                if (!stopRequested && !playbackRequested)
                {
                    playbackRequested = true;
                    SetPlaybackParticles(true);
                    SyncPlayStopButton();
                }

                return;
            }

            if (!playbackRequested && !stopRequested)
            {
                return;
            }

            playbackRequested = false;
            stopRequested = false;
            SetPlaybackParticles(false);
            SyncPlayStopButton();
        }

        private void SetPlaybackParticles (bool active)
        {
            if (playbackParticles == null)
            {
                return;
            }

            foreach (ParticleSystem particles in playbackParticles)
            {
                if (particles == null)
                {
                    continue;
                }

                if (active)
                {
                    particles.Play(true);
                    continue;
                }

                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void UpdateSpatialText ()
        {
            if (spatialSoundButtonText == null)
            {
                return;
            }

            spatialSoundButtonText.text = spatial ? "Spatial Sound: ON" : "Spatial Sound: OFF";
        }

        private void SyncPlayStopButton ()
        {
            if (playStopButton == null)
            {
                return;
            }

            playStopButton.SetNextAction(!playbackRequested);
        }
    }
}
