using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MelenitasDev.SoundsGood.Demo
{
    public class SG_DynamicMusicEmitter : MonoBehaviour, IShowcaseResettable
    {
        [Serializable]
        private class TrackVolumeControl
        {
            private const float ResetNormalizedVolume = 0.5f;

            [SerializeField] private Track track = Track.Null;
            [SerializeField] private string fallbackTrackTag;
            [SerializeField] private Slider slider;
            [SerializeField] private TMP_Text valueText;

            private UnityAction<float> sliderListener;

            public TrackVolumeControl () { }

            public TrackVolumeControl (string fallbackTrackTag)
            {
                this.fallbackTrackTag = fallbackTrackTag;
            }

            public bool HasSlider => slider != null;
            public float SliderValue => slider != null ? slider.value : GetFullVolumeSliderValue();

            public string TrackTag
            {
                get
                {
                    if (!track.IsNull)
                    {
                        return track.ToString();
                    }

                    return fallbackTrackTag;
                }
            }

            public bool HasTrack => !string.IsNullOrEmpty(TrackTag) && TrackTag != "__NULL__";

            public void RegisterSliderListener (UnityAction<float> onValueChanged)
            {
                UnregisterSliderListener();

                if (slider == null)
                {
                    return;
                }

                sliderListener = onValueChanged;
                slider.onValueChanged.AddListener(sliderListener);
            }

            public void UnregisterSliderListener ()
            {
                if (slider != null && sliderListener != null)
                {
                    slider.onValueChanged.RemoveListener(sliderListener);
                }

                sliderListener = null;
            }

            public float GetNormalizedVolume (float sliderValue)
            {
                if (slider != null && slider.maxValue <= 1.0001f)
                {
                    return Mathf.Clamp01(sliderValue);
                }

                return Mathf.Clamp01(sliderValue / 100f);
            }

            public float GetNormalizedVolume () => GetNormalizedVolume(SliderValue);

            public float ResetSliderToDefaultVolume ()
            {
                float sliderValue = GetSliderValue(ResetNormalizedVolume);

                if (slider != null)
                {
                    slider.SetValueWithoutNotify(sliderValue);
                }

                float normalizedVolume = GetNormalizedVolume(sliderValue);
                UpdateValueText(normalizedVolume);
                return normalizedVolume;
            }

            public void UpdateValueText (float normalizedVolume)
            {
                if (valueText == null)
                {
                    return;
                }

                valueText.text = $"{Mathf.Clamp01(normalizedVolume) * 100f:F0}%";
            }

            private float GetFullVolumeSliderValue ()
            {
                return GetSliderValue(1f);
            }

            private float GetSliderValue (float normalizedVolume)
            {
                if (slider == null)
                {
                    return Mathf.Clamp01(normalizedVolume) * 100f;
                }

                float volumeScale = slider.maxValue <= 1.0001f ? 1f : 100f;
                float sliderValue = Mathf.Clamp01(normalizedVolume) * volumeScale;
                return Mathf.Clamp(sliderValue, slider.minValue, slider.maxValue);
            }
        }

        [Header("References")]
        [SerializeField] private DynamicMusicEmitter dynamicMusicEmitter;
        [SerializeField] private SG_AlternatingButton playStopButton;
        [SerializeField] private ParticleSystem[] playbackParticles;

        [Header("Track Volumes")]
        [SerializeField] private TrackVolumeControl[] trackVolumeControls =
        {
            new TrackVolumeControl("gravityBass"),
            new TrackVolumeControl("gravityKick"),
            new TrackVolumeControl("gravityPercussion"),
            new TrackVolumeControl("gravitySnare"),
            new TrackVolumeControl("gravityHitHat"),
            new TrackVolumeControl("gravityGuitar"),
            new TrackVolumeControl("gravityCyberline"),
            new TrackVolumeControl("gravityAlert")
        };

        private bool playbackRequested;
        private bool stopRequested;

        void Awake ()
        {
            if (dynamicMusicEmitter == null)
            {
                dynamicMusicEmitter = GetComponent<DynamicMusicEmitter>();
            }
        }

        void OnEnable ()
        {
            RegisterSliderListeners();
        }

        void Start ()
        {
            playbackRequested = dynamicMusicEmitter != null && dynamicMusicEmitter.IsPlaying;
            stopRequested = false;

            EnsureDynamicMusicTracks();
            ApplyAllTrackVolumes();
            SetPlaybackParticles(playbackRequested);
            SyncPlayStopButton();
        }

        void Update ()
        {
            RefreshPlaybackState();
        }

        void OnDisable ()
        {
            UnregisterSliderListeners();
        }

        public void PlayDynamicMusic ()
        {
            if (dynamicMusicEmitter == null)
            {
                return;
            }

            DynamicMusic dynamicMusic = dynamicMusicEmitter.DynamicMusic;
            if (dynamicMusic.Paused)
            {
                dynamicMusicEmitter.Resume();
            }
            else
            {
                EnsureDynamicMusicTracks();
                ApplyAllTrackVolumes();
                dynamicMusicEmitter.Play();
            }

            playbackRequested = true;
            stopRequested = false;
            SetPlaybackParticles(true);
            SyncPlayStopButton();
        }

        public void StopDynamicMusic ()
        {
            if (dynamicMusicEmitter == null)
            {
                return;
            }

            dynamicMusicEmitter.Stop();
            playbackRequested = false;
            stopRequested = true;
            SetPlaybackParticles(false);
            SyncPlayStopButton();
        }

        public void ToggleDynamicMusic ()
        {
            if (playbackRequested)
            {
                StopDynamicMusic();
                return;
            }

            PlayDynamicMusic();
        }

        public void SetTrackVolume (int trackIndex)
        {
            if (!IsValidTrackIndex(trackIndex))
            {
                return;
            }

            TrackVolumeControl control = trackVolumeControls[trackIndex];
            SetTrackVolume(trackIndex, control.SliderValue);
        }

        public void SetTrackVolume (int trackIndex, float sliderValue)
        {
            if (!IsValidTrackIndex(trackIndex))
            {
                return;
            }

            TrackVolumeControl control = trackVolumeControls[trackIndex];
            float normalizedVolume = control.GetNormalizedVolume(sliderValue);

            control.UpdateValueText(normalizedVolume);
            ApplyTrackVolume(control, normalizedVolume);
        }

        public void ResetTrackVolume (int trackIndex)
        {
            if (!IsValidTrackIndex(trackIndex))
            {
                return;
            }

            TrackVolumeControl control = trackVolumeControls[trackIndex];
            float normalizedVolume = control.ResetSliderToDefaultVolume();
            ApplyTrackVolume(control, normalizedVolume);
        }

        public void RestTrackVolume (int trackIndex) => ResetTrackVolume(trackIndex);

        public void ResetVolumes ()
        {
            if (trackVolumeControls == null)
            {
                return;
            }

            for (int i = 0; i < trackVolumeControls.Length; i++)
            {
                ResetTrackVolume(i);
            }
        }

        public void RestVolumes () => ResetVolumes();

        public void ResetAll ()
        {
            ResetVolumes();
            StopDynamicMusic();
        }

        public void RestAll () => ResetAll();

        public void StopAll ()
        {
            StopDynamicMusic();
        }

        public void ResetShowcaseState ()
        {
            ResetAll();
        }

        private void RegisterSliderListeners ()
        {
            if (trackVolumeControls == null)
            {
                return;
            }

            for (int i = 0; i < trackVolumeControls.Length; i++)
            {
                int trackIndex = i;
                trackVolumeControls[i]?.RegisterSliderListener(value => SetTrackVolume(trackIndex, value));
            }
        }

        private void UnregisterSliderListeners ()
        {
            if (trackVolumeControls == null)
            {
                return;
            }

            foreach (TrackVolumeControl control in trackVolumeControls)
            {
                control?.UnregisterSliderListener();
            }
        }

        private void EnsureDynamicMusicTracks ()
        {
            if (dynamicMusicEmitter == null || dynamicMusicEmitter.DynamicMusic.Using)
            {
                return;
            }

            string[] trackTags = GetUniqueTrackTags();
            if (trackTags.Length == 0)
            {
                return;
            }

            dynamicMusicEmitter.DynamicMusic.SetClips(trackTags);
        }

        private void ApplyAllTrackVolumes ()
        {
            if (trackVolumeControls == null)
            {
                return;
            }

            foreach (TrackVolumeControl control in trackVolumeControls)
            {
                if (control == null)
                {
                    continue;
                }

                float normalizedVolume = control.GetNormalizedVolume();
                control.UpdateValueText(normalizedVolume);
                ApplyTrackVolume(control, normalizedVolume);
            }
        }

        private void ApplyTrackVolume (TrackVolumeControl control, float normalizedVolume)
        {
            if (dynamicMusicEmitter == null || control == null || !control.HasTrack)
            {
                return;
            }

            DynamicMusic dynamicMusic = dynamicMusicEmitter.DynamicMusic;
            string trackTag = control.TrackTag;

            if (dynamicMusic.Using)
            {
                dynamicMusic.ChangeTrackVolume(trackTag, normalizedVolume);
                return;
            }

            dynamicMusic.SetTrackVolume(trackTag, normalizedVolume);
        }

        private string[] GetUniqueTrackTags ()
        {
            if (trackVolumeControls == null)
            {
                return Array.Empty<string>();
            }

            List<string> trackTags = new List<string>();
            HashSet<string> usedTags = new HashSet<string>();

            foreach (TrackVolumeControl control in trackVolumeControls)
            {
                if (control == null || !control.HasTrack)
                {
                    continue;
                }

                string trackTag = control.TrackTag;
                if (usedTags.Add(trackTag))
                {
                    trackTags.Add(trackTag);
                }
            }

            return trackTags.ToArray();
        }

        private void RefreshPlaybackState ()
        {
            if (dynamicMusicEmitter == null)
            {
                return;
            }

            DynamicMusic dynamicMusic = dynamicMusicEmitter.DynamicMusic;

            if (dynamicMusic.Using)
            {
                if (!stopRequested && !playbackRequested && (dynamicMusic.Playing || dynamicMusic.Paused))
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

        private bool IsValidTrackIndex (int trackIndex)
        {
            return trackVolumeControls != null && trackIndex >= 0 && trackIndex < trackVolumeControls.Length &&
                   trackVolumeControls[trackIndex] != null;
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
