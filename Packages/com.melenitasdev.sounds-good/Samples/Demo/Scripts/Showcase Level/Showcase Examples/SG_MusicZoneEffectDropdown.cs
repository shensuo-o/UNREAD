using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MelenitasDev.SoundsGood;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MelenitasDev.SoundsGood.Demo
{
    [DisallowMultipleComponent]
    public class SG_MusicZoneEffectDropdown : MonoBehaviour, IShowcaseResettable
    {
        private const string NullOptionLabel = "Null";

        [SerializeField] private MusicZone musicZone;
        [SerializeField] private TMP_Dropdown tmpDropdown;
        [SerializeField] private Dropdown dropdown;
        [SerializeField, Range(0f, 1f)] private float intensity = 1f;
        [SerializeField, Min(0f)] private float fadeTime = 0.15f;

        private readonly List<Effect> effects = new List<Effect>();
        private bool listenersRegistered;

        void Reset()
        {
            ResolveDropdown();
        }

        void Awake()
        {
            ResolveDropdown();
            RefreshOptions();
        }

        void OnEnable()
        {
            RegisterListeners();
        }

        void OnDisable()
        {
            UnregisterListeners();
        }

        public void SetMusicZone(MusicZone targetMusicZone)
        {
            musicZone = targetMusicZone;
        }

        public void ResetShowcaseState()
        {
            ResolveDropdown();

            if (effects.Count == 0)
            {
                RefreshOptions();
            }

            SetDropdownValueWithoutNotify(0);
            ApplyOption(0);
        }

        public void RefreshOptions()
        {
            string previousSelection = GetSelectedEffectTag();
            effects.Clear();

            List<string> labels = new List<string> { NullOptionLabel };
            effects.Add(Effect.Null);

            foreach (Effect effect in GetAvailableEffects())
            {
                effects.Add(effect);
                labels.Add(effect.ToString());
            }

            int selectedIndex = FindEffectIndex(previousSelection);
            SetDropdownOptions(labels, selectedIndex);
        }

        private static IEnumerable<Effect> GetAvailableEffects()
        {
            HashSet<string> seenTags = new HashSet<string>(StringComparer.Ordinal);

            return typeof(Effect)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(Effect))
                .Select(field => (Effect)field.GetValue(null))
                .Where(effect => !effect.IsNull)
                .Where(effect => seenTags.Add(effect.ToString()))
                .OrderBy(effect => effect.ToString(), StringComparer.OrdinalIgnoreCase);
        }

        private void OnDropdownValueChanged(int optionIndex)
        {
            ApplyOption(optionIndex);
        }

        private void ApplyOption(int optionIndex)
        {
            if (optionIndex < 0 || optionIndex >= effects.Count) return;

            if (musicZone == null)
            {
                Debug.LogWarning("[MusicZoneEffectDropdown] Assign a MusicZone before selecting an effect.", this);
                return;
            }

            AudioEmitter emitter = musicZone.GetComponent<AudioEmitter>();
            if (emitter == null)
            {
                Debug.LogWarning("[MusicZoneEffectDropdown] The assigned MusicZone needs a music emitter on the same GameObject.", musicZone);
                return;
            }

            ApplyEffectToEmitter(emitter, effects[optionIndex]);
        }

        private void ApplyEffectToEmitter(AudioEmitter emitter, Effect effect)
        {
            float targetIntensity = effect.IsNull ? 0f : intensity;

            switch (emitter)
            {
                case MusicEmitter musicEmitter:
                    ApplyToMusic(musicEmitter.Music, effect, targetIntensity);
                    break;
                case PlaylistEmitter playlistEmitter:
                    ApplyToPlaylist(playlistEmitter.Playlist, effect, targetIntensity);
                    break;
                case DynamicMusicEmitter dynamicMusicEmitter:
                    ApplyToDynamicMusic(dynamicMusicEmitter.DynamicMusic, effect, targetIntensity);
                    break;
                default:
                    Debug.LogWarning($"[MusicZoneEffectDropdown] '{emitter.GetType().Name}' is not a supported music emitter.", emitter);
                    break;
            }
        }

        private void ApplyToMusic(Music music, Effect effect, float targetIntensity)
        {
            if (music.Using)
            {
                if (effect.IsNull) music.ChangeEffect(0f, fadeTime);
                else music.ChangeEffect(effect, targetIntensity, fadeTime);
            }
            else
            {
                music.SetEffect(effect, targetIntensity);
            }
        }

        private void ApplyToPlaylist(Playlist playlist, Effect effect, float targetIntensity)
        {
            if (playlist.Using)
            {
                if (effect.IsNull) playlist.ChangeEffect(0f, fadeTime);
                else playlist.ChangeEffect(effect, targetIntensity, fadeTime);
            }
            else
            {
                playlist.SetEffect(effect, targetIntensity);
            }
        }

        private void ApplyToDynamicMusic(DynamicMusic dynamicMusic, Effect effect, float targetIntensity)
        {
            if (dynamicMusic.Using)
            {
                if (effect.IsNull) dynamicMusic.ChangeEffect(0f, fadeTime);
                else dynamicMusic.ChangeEffect(effect, targetIntensity, fadeTime);
            }
            else
            {
                dynamicMusic.SetEffect(effect, targetIntensity);
            }
        }

        private void ResolveDropdown()
        {
            if (tmpDropdown == null) tmpDropdown = GetComponent<TMP_Dropdown>();
            if (dropdown == null) dropdown = GetComponent<Dropdown>();
        }

        private void RegisterListeners()
        {
            if (listenersRegistered) return;

            bool registeredAny = false;

            if (tmpDropdown != null)
            {
                tmpDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
                registeredAny = true;
            }

            if (dropdown != null)
            {
                dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
                registeredAny = true;
            }

            listenersRegistered = registeredAny;
        }

        private void UnregisterListeners()
        {
            if (!listenersRegistered) return;

            if (tmpDropdown != null) tmpDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
            if (dropdown != null) dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);

            listenersRegistered = false;
        }

        private void SetDropdownOptions(List<string> labels, int selectedIndex)
        {
            UnregisterListeners();

            if (tmpDropdown != null)
            {
                tmpDropdown.ClearOptions();
                tmpDropdown.AddOptions(labels);
                tmpDropdown.SetValueWithoutNotify(selectedIndex);
                tmpDropdown.RefreshShownValue();
            }

            if (dropdown != null)
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(labels);
                dropdown.SetValueWithoutNotify(selectedIndex);
                dropdown.RefreshShownValue();
            }

            RegisterListeners();
        }

        private void SetDropdownValueWithoutNotify(int selectedIndex)
        {
            if (tmpDropdown != null)
            {
                tmpDropdown.SetValueWithoutNotify(selectedIndex);
                tmpDropdown.RefreshShownValue();
            }

            if (dropdown != null)
            {
                dropdown.SetValueWithoutNotify(selectedIndex);
                dropdown.RefreshShownValue();
            }
        }

        private int FindEffectIndex(string effectTag)
        {
            if (string.IsNullOrEmpty(effectTag)) return 0;

            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].ToString() == effectTag) return i;
            }

            return 0;
        }

        private string GetSelectedEffectTag()
        {
            int selectedIndex = GetSelectedIndex();
            if (selectedIndex < 0 || selectedIndex >= effects.Count) return null;

            return effects[selectedIndex].ToString();
        }

        private int GetSelectedIndex()
        {
            if (tmpDropdown != null) return tmpDropdown.value;
            if (dropdown != null) return dropdown.value;
            return -1;
        }
    }
}
