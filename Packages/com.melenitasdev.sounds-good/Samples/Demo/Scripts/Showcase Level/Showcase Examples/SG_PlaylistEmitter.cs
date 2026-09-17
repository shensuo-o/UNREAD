using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MelenitasDev.SoundsGood.Demo
{
    public class SG_PlaylistEmitter : MonoBehaviour, IShowcaseResettable
    {
        [Serializable]
        private class PlaylistTrackView
        {
            [SerializeField] private TMP_Text trackNameText;
            [SerializeField] private TMP_Dropdown trackDropdown;
            [SerializeField] private Image backgroundImage;

            private UnityAction<int> dropdownListener;
            private float targetAlpha;

            public void SetName (string trackName)
            {
                if (trackNameText == null)
                {
                    return;
                }

                trackNameText.text = trackName;
            }

            public void SetDropdownOptions (string[] trackNames, int selectedTrackIndex, UnityAction<int> onValueChanged,
                bool addWorldSpaceInteractable)
            {
                if (trackDropdown == null)
                {
                    return;
                }

                if (dropdownListener != null)
                {
                    trackDropdown.onValueChanged.RemoveListener(dropdownListener);
                }

                trackDropdown.ClearOptions();
                trackDropdown.AddOptions(new List<string>(trackNames));
                trackDropdown.SetValueWithoutNotify(selectedTrackIndex);
                trackDropdown.RefreshShownValue();

                dropdownListener = onValueChanged;
                trackDropdown.onValueChanged.AddListener(dropdownListener);

                if (addWorldSpaceInteractable)
                {
                    EnsureWorldSpaceDropdownInteractable();
                }
            }

            public void SetDropdownValue (int selectedTrackIndex)
            {
                if (trackDropdown == null)
                {
                    return;
                }

                trackDropdown.SetValueWithoutNotify(selectedTrackIndex);
                trackDropdown.RefreshShownValue();
            }

            public void SetHighlighted (bool highlighted, float activeAlpha, float inactiveAlpha, bool instant)
            {
                if (backgroundImage == null)
                {
                    return;
                }

                targetAlpha = highlighted ? activeAlpha : inactiveAlpha;

                if (targetAlpha > 0f)
                {
                    backgroundImage.enabled = true;
                }

                if (instant)
                {
                    SetAlpha(targetAlpha);
                }
            }

            public void UpdateHighlight (float fadeDuration, float deltaTime)
            {
                if (backgroundImage == null)
                {
                    return;
                }

                float currentAlpha = backgroundImage.color.a;
                if (Mathf.Approximately(currentAlpha, targetAlpha))
                {
                    SetAlpha(targetAlpha);
                    return;
                }

                backgroundImage.enabled = true;

                if (fadeDuration <= 0f)
                {
                    SetAlpha(targetAlpha);
                    return;
                }

                float fadeSpeed = 1f / fadeDuration;
                SetAlpha(Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * deltaTime));
            }

            private void SetAlpha (float alpha)
            {
                Color color = backgroundImage.color;
                color.a = alpha;
                backgroundImage.color = color;
                backgroundImage.enabled = alpha > 0f;
            }

            private void EnsureWorldSpaceDropdownInteractable ()
            {
                SG_WorldSpaceDropdownInteractable interactable =
                    trackDropdown.GetComponent<SG_WorldSpaceDropdownInteractable>();
                if (interactable == null)
                {
                    interactable = trackDropdown.gameObject.AddComponent<SG_WorldSpaceDropdownInteractable>();
                }

                interactable.SetDropdown(trackDropdown);
            }
        }

        [Header("References")]
        [SerializeField] private PlaylistEmitter playlistEmitter;
        [SerializeField] private SG_AlternatingButton playStopButton;
        [SerializeField] private ParticleSystem[] playbackParticles;

        [Header("Pitch")]
        [SerializeField] private Slider pitchSlider;
        [SerializeField] private TMP_Text pitchValueText;

        [Header("Volume")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Text volumeValueText;

        [Header("Playlist UI")]
        [SerializeField] private PlaylistTrackView[] trackViews = new PlaylistTrackView[3];
        [SerializeField] private string[] availableTracks = new string[]
        {
            "DuckDescending",
            "LittleGreenMen",
            "StudyFirst"
        };
        [SerializeField] private bool updateTrackNames = true;
        [SerializeField] private bool addWorldSpaceDropdownInteractables = true;
        [SerializeField, Range(0f, 1f)] private float activeBackgroundAlpha = 0.65f;
        [SerializeField, Range(0f, 1f)] private float inactiveBackgroundAlpha = 0f;
        [SerializeField, Min(0f)] private float highlightFadeDuration = 0.2f;

        private const string NullTrackTag = "__NULL__";

        private bool playbackRequested;
        private bool stopRequested;
        private int currentTrackIndex = int.MinValue;
        private int[] selectedTrackIndexes = Array.Empty<int>();
        private int[] defaultTrackIndexes = Array.Empty<int>();

        void Awake ()
        {
            if (playlistEmitter == null)
            {
                playlistEmitter = GetComponent<PlaylistEmitter>();
            }
        }

        void Start ()
        {
            playbackRequested = playlistEmitter != null && playlistEmitter.IsPlaying;
            stopRequested = false;

            CacheInitialPlaylistSelection();
            SetupTrackDropdowns();
            ApplySelectedPlaylist(false);
            SyncVolumeSlider();
            SyncPitchSlider();
            SetPlaybackParticles(playbackRequested);
            SyncPlayStopButton();
            RefreshTrackHighlight();
        }

        void Update ()
        {
            RefreshPlaybackState();
            RefreshTrackHighlight();
            UpdateTrackHighlight();
        }

        public void PlayPlaylist ()
        {
            if (playlistEmitter == null)
            {
                return;
            }

            playlistEmitter.Play();
            playbackRequested = true;
            stopRequested = false;
            SetPlaybackParticles(true);
            SyncPlayStopButton();
            RefreshTrackHighlight(true);
        }

        public void StopPlaylist ()
        {
            if (playlistEmitter == null)
            {
                return;
            }

            playlistEmitter.Stop();
            playbackRequested = false;
            stopRequested = true;
            SetPlaybackParticles(false);
            SetTrackHighlight(-1);
            SyncPlayStopButton();
        }

        public void TogglePlaylist ()
        {
            if (playbackRequested)
            {
                StopPlaylist();
                return;
            }

            PlayPlaylist();
        }

        public void SetVolume ()
        {
            if (volumeSlider == null)
            {
                return;
            }

            SetVolume(volumeSlider.value);
        }

        public void SetVolume (float sliderValue)
        {
            float normalizedVolume = Mathf.Clamp01(sliderValue / 100f);

            if (playlistEmitter != null)
            {
                playlistEmitter.Playlist.ChangeVolume(normalizedVolume);
            }

            UpdateVolumeText(Mathf.Clamp(sliderValue, 0f, 100f));
        }

        public void RestVolume ()
        {
            if (volumeSlider != null)
            {
                volumeSlider.SetValueWithoutNotify(100f);
            }

            if (playlistEmitter != null)
            {
                playlistEmitter.Playlist.ChangeVolume(1f);
            }

            UpdateVolumeText(100f);
        }

        public void SetPitch ()
        {
            if (pitchSlider == null)
            {
                return;
            }

            SetPitch(pitchSlider.value);
        }

        public void SetPitch (float sliderValue)
        {
            if (playlistEmitter != null)
            {
                playlistEmitter.Playlist.ChangePitch(sliderValue);
            }

            UpdatePitchText(sliderValue);
        }

        public void RestPitch ()
        {
            if (pitchSlider != null)
            {
                pitchSlider.SetValueWithoutNotify(1f);
            }

            if (playlistEmitter != null)
            {
                playlistEmitter.Playlist.ChangePitch(1f);
            }

            UpdatePitchText(1f);
        }

        public void RestAll ()
        {
            RestVolume();
            RestPitch();
            RestoreDefaultPlaylistSelection();
        }

        public void StopAll ()
        {
            StopPlaylist();
        }

        public void ResetShowcaseState ()
        {
            StopPlaylist();
            RestAll();
            SetTrackHighlight(-1, true);
        }

        public void SetPlaylistTrack (int trackSlotIndex, int availableTrackIndex)
        {
            if (!IsValidTrackSlot(trackSlotIndex) || !HasAvailableTracks())
            {
                return;
            }

            EnsureSelectionCacheSize();

            selectedTrackIndexes[trackSlotIndex] = GetSafeAvailableTrackIndex(availableTrackIndex, trackSlotIndex);
            SyncTrackName(trackSlotIndex);
            ApplySelectedPlaylist(true);
        }

        private void RefreshPlaybackState ()
        {
            if (playlistEmitter == null)
            {
                return;
            }

            Playlist playlist = playlistEmitter.Playlist;

            if (playlist.Using)
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
            SetTrackHighlight(-1);
            SyncPlayStopButton();
        }

        private void RefreshTrackHighlight (bool instant = false)
        {
            if (playlistEmitter == null || stopRequested)
            {
                SetTrackHighlight(-1, instant);
                return;
            }

            Playlist playlist = playlistEmitter.Playlist;
            if (!playlist.Using || (!playlist.Playing && !playlist.Paused))
            {
                SetTrackHighlight(-1, instant);
                return;
            }

            SetTrackHighlight(GetCurrentTrackIndex(playlist), instant);
        }

        private int GetCurrentTrackIndex (Playlist playlist)
        {
            if (trackViews == null || trackViews.Length == 0)
            {
                return -1;
            }

            int reproducedTrackIndex = Mathf.FloorToInt(playlist.ReproducedTracks) - 1;
            if (reproducedTrackIndex < 0)
            {
                return -1;
            }

            string[] playlistTrackNames = playlist.PlaylistClipsTags;
            int trackCount = playlistTrackNames.Length > 0
                ? Mathf.Min(trackViews.Length, playlistTrackNames.Length)
                : trackViews.Length;

            if (trackCount == 0)
            {
                return -1;
            }

            return reproducedTrackIndex % trackCount;
        }

        private void SetTrackHighlight (int trackIndex, bool instant = false)
        {
            if (trackIndex == currentTrackIndex && !instant)
            {
                return;
            }

            currentTrackIndex = trackIndex;

            if (trackViews == null)
            {
                return;
            }

            for (int i = 0; i < trackViews.Length; i++)
            {
                trackViews[i]?.SetHighlighted(i == currentTrackIndex, activeBackgroundAlpha, inactiveBackgroundAlpha,
                    instant);
            }
        }

        private void UpdateTrackHighlight ()
        {
            if (trackViews == null)
            {
                return;
            }

            for (int i = 0; i < trackViews.Length; i++)
            {
                trackViews[i]?.UpdateHighlight(highlightFadeDuration, Time.deltaTime);
            }
        }

        private void CacheInitialPlaylistSelection ()
        {
            EnsureSelectionCacheSize();

            if (!HasAvailableTracks())
            {
                return;
            }

            string[] playlistTrackNames = playlistEmitter != null
                ? playlistEmitter.Playlist.PlaylistClipsTags
                : Array.Empty<string>();

            for (int i = 0; i < selectedTrackIndexes.Length; i++)
            {
                string playlistTrackName = i < playlistTrackNames.Length ? playlistTrackNames[i] : string.Empty;
                int trackIndex = GetAvailableTrackIndex(playlistTrackName, i);
                selectedTrackIndexes[i] = trackIndex;
                defaultTrackIndexes[i] = trackIndex;
                SyncTrackName(i);
            }
        }

        private void RestoreDefaultPlaylistSelection ()
        {
            if (trackViews == null || !HasAvailableTracks())
            {
                StopPlaylist();
                return;
            }

            EnsureSelectionCacheSize();

            for (int i = 0; i < selectedTrackIndexes.Length; i++)
            {
                selectedTrackIndexes[i] = GetSafeAvailableTrackIndex(defaultTrackIndexes[i], i);
                trackViews[i]?.SetDropdownValue(selectedTrackIndexes[i]);
                SyncTrackName(i);
            }

            ApplySelectedPlaylist(true);
        }

        private void SetupTrackDropdowns ()
        {
            if (trackViews == null || !HasAvailableTracks())
            {
                return;
            }

            EnsureSelectionCacheSize();

            for (int i = 0; i < trackViews.Length; i++)
            {
                int trackSlotIndex = i;
                trackViews[i]?.SetDropdownOptions(availableTracks, selectedTrackIndexes[i],
                    availableTrackIndex => SetPlaylistTrack(trackSlotIndex, availableTrackIndex),
                    addWorldSpaceDropdownInteractables);
            }
        }

        private void ApplySelectedPlaylist (bool stopPlayback)
        {
            if (playlistEmitter == null || !HasAvailableTracks())
            {
                return;
            }

            Playlist playlist = playlistEmitter.Playlist;
            bool wasUsing = playlist.Using;

            if (stopPlayback && wasUsing)
            {
                playlistEmitter.Stop();
            }

            playlist.SetPlaylist(GetSelectedPlaylist());

            if (!stopPlayback)
            {
                return;
            }

            playbackRequested = false;
            stopRequested = wasUsing;
            if (wasUsing)
            {
                SetPlaybackParticles(false);
            }

            SetTrackHighlight(-1);
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

        private string[] GetSelectedPlaylist ()
        {
            if (trackViews == null || !HasAvailableTracks())
            {
                return Array.Empty<string>();
            }

            EnsureSelectionCacheSize();

            string[] playlistTrackNames = new string[trackViews.Length];
            for (int i = 0; i < playlistTrackNames.Length; i++)
            {
                int availableTrackIndex = GetSafeAvailableTrackIndex(selectedTrackIndexes[i], i);
                playlistTrackNames[i] = availableTracks[availableTrackIndex];
            }

            return playlistTrackNames;
        }

        private void SyncTrackName (int trackSlotIndex)
        {
            if (!updateTrackNames || !IsValidTrackSlot(trackSlotIndex) || !HasAvailableTracks())
            {
                return;
            }

            int availableTrackIndex = GetSafeAvailableTrackIndex(selectedTrackIndexes[trackSlotIndex], trackSlotIndex);
            string trackName = availableTracks[availableTrackIndex];
            if (string.IsNullOrEmpty(trackName) || trackName == NullTrackTag)
            {
                return;
            }

            trackViews[trackSlotIndex]?.SetName(trackName);
        }

        private int GetAvailableTrackIndex (string trackName, int fallbackTrackSlotIndex)
        {
            if (HasAvailableTracks() && !string.IsNullOrEmpty(trackName) && trackName != NullTrackTag)
            {
                for (int i = 0; i < availableTracks.Length; i++)
                {
                    if (availableTracks[i] == trackName)
                    {
                        return i;
                    }
                }
            }

            return GetSafeAvailableTrackIndex(fallbackTrackSlotIndex, 0);
        }

        private int GetSafeAvailableTrackIndex (int availableTrackIndex, int fallbackTrackSlotIndex)
        {
            if (!HasAvailableTracks())
            {
                return 0;
            }

            if (availableTrackIndex >= 0 && availableTrackIndex < availableTracks.Length)
            {
                return availableTrackIndex;
            }

            return Mathf.Clamp(fallbackTrackSlotIndex, 0, availableTracks.Length - 1);
        }

        private void EnsureSelectionCacheSize ()
        {
            int trackCount = trackViews?.Length ?? 0;
            ResizeTrackIndexCache(ref selectedTrackIndexes, trackCount);
            ResizeTrackIndexCache(ref defaultTrackIndexes, trackCount);
        }

        private void ResizeTrackIndexCache (ref int[] trackIndexCache, int trackCount)
        {
            if (trackIndexCache.Length == trackCount)
            {
                return;
            }

            int[] newTrackIndexCache = new int[trackCount];
            int copiedValues = Mathf.Min(trackIndexCache.Length, newTrackIndexCache.Length);
            for (int i = 0; i < copiedValues; i++)
            {
                newTrackIndexCache[i] = trackIndexCache[i];
            }

            trackIndexCache = newTrackIndexCache;
        }

        private bool IsValidTrackSlot (int trackSlotIndex)
        {
            return trackViews != null && trackSlotIndex >= 0 && trackSlotIndex < trackViews.Length;
        }

        private bool HasAvailableTracks ()
        {
            return availableTracks != null && availableTracks.Length > 0;
        }

        private void SyncVolumeSlider ()
        {
            if (playlistEmitter == null)
            {
                return;
            }

            float volumePercent = Mathf.Clamp01(playlistEmitter.Playlist.Volume) * 100f;
            if (volumeSlider != null)
            {
                volumeSlider.SetValueWithoutNotify(volumePercent);
            }

            UpdateVolumeText(volumePercent);
        }

        private void SyncPitchSlider ()
        {
            if (playlistEmitter == null)
            {
                return;
            }

            float pitch = playlistEmitter.Playlist.Pitch;
            if (pitchSlider != null)
            {
                pitchSlider.SetValueWithoutNotify(pitch);
            }

            UpdatePitchText(pitch);
        }

        private void UpdateVolumeText (float volumePercent)
        {
            if (volumeValueText == null)
            {
                return;
            }

            volumeValueText.text = volumePercent.ToString("F0") + "%";
        }

        private void UpdatePitchText (float pitch)
        {
            if (pitchValueText == null)
            {
                return;
            }

            pitchValueText.text = pitch.ToString("F2");
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
