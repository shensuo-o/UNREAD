/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using MelenitasDev.SoundsGood.Domain;

namespace MelenitasDev.SoundsGood
{
    /// <summary>
    /// Applies a global (or per-Output) audio effect to every active Sounds Good source each frame,
    /// fading it in and out. Used for states like pausing (muffle everything). Unlike an
    /// AudioEffectZone it isn't spatial, so it reaches 2D sounds too, and it wins over any zone.
    /// Auto-created on demand and kept across scenes. Driven from
    /// <see cref="SoundsGoodManager.SetGlobalEffect"/> and its Output/Remove siblings.
    /// </summary>
    internal class GlobalEffectDriver : MonoBehaviour
    {
        private class Entry
        {
            public AudioEffectPreset preset;
            public float target;   // intensity to reach (0..1)
            public float current;  // intensity applied right now
            public float fadeTime; // seconds for the current ramp; 0 = instant
        }

        private static GlobalEffectDriver instance;

        private Entry global;
        private readonly Dictionary<AudioMixerGroup, Entry> byOutput = new Dictionary<AudioMixerGroup, Entry>();
        private readonly List<AudioMixerGroup> finishedOutputs = new List<AudioMixerGroup>();

        internal static GlobalEffectDriver GetOrCreate ()
        {
            if (instance != null) return instance;

            var go = new GameObject("[Sounds Good Global Effect]") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GlobalEffectDriver>();
            return instance;
        }

        internal void SetGlobal (AudioEffectPreset preset, float intensity, float fadeTime)
        {
            global ??= new Entry();
            SetEntry(global, preset, intensity, fadeTime);
        }

        internal void RemoveGlobal (float fadeTime)
        {
            if (global != null) FadeOut(global, fadeTime);
        }

        internal void SetOutput (AudioMixerGroup group, AudioEffectPreset preset, float intensity, float fadeTime)
        {
            // No group means "no specific output" — fall back to the global effect.
            if (group == null) { SetGlobal(preset, intensity, fadeTime); return; }

            if (!byOutput.TryGetValue(group, out Entry entry))
            {
                entry = new Entry();
                byOutput[group] = entry;
            }
            SetEntry(entry, preset, intensity, fadeTime);
        }

        internal void RemoveOutput (AudioMixerGroup group, float fadeTime)
        {
            if (group == null) { RemoveGlobal(fadeTime); return; }
            if (byOutput.TryGetValue(group, out Entry entry)) FadeOut(entry, fadeTime);
        }

        private static void SetEntry (Entry entry, AudioEffectPreset preset, float intensity, float fadeTime)
        {
            entry.preset = preset;
            entry.target = Mathf.Clamp01(intensity);
            entry.fadeTime = fadeTime;
            if (fadeTime <= 0f) entry.current = entry.target;
        }

        private static void FadeOut (Entry entry, float fadeTime)
        {
            entry.target = 0f;
            entry.fadeTime = fadeTime;
            if (fadeTime <= 0f) entry.current = 0f;
        }

        // Claims run in Update so they're gathered before any source resolves them in LateUpdate,
        // matching how AudioEffectZones claim. Unscaled time keeps fades working while paused
        // (Time.timeScale = 0).
        void Update ()
        {
            Advance(global);
            if (global != null && global.target == 0f && global.current == 0f) global = null;

            finishedOutputs.Clear();
            foreach (KeyValuePair<AudioMixerGroup, Entry> pair in byOutput)
            {
                Advance(pair.Value);
                if (pair.Value.target == 0f && pair.Value.current == 0f) finishedOutputs.Add(pair.Key);
            }
            foreach (AudioMixerGroup group in finishedOutputs) byOutput.Remove(group);

            if (global == null && byOutput.Count == 0) return;

            foreach (SoundsGoodAudioSource source in SoundsGoodManager.AudioSourcePool)
            {
                if (source == null || !source.Using) continue;

                Entry entry = Resolve(source);
                if (entry != null && entry.current > 0f && entry.preset != null)
                    source.ClaimGlobalEffect(entry.preset, entry.current);
            }
        }

        // The source's own Output effect wins over the global one; otherwise the global applies.
        private Entry Resolve (SoundsGoodAudioSource source)
        {
            AudioMixerGroup output = source.Output;
            if (output != null && byOutput.TryGetValue(output, out Entry entry) && entry.current > 0f)
                return entry;
            return global;
        }

        private static void Advance (Entry entry)
        {
            if (entry == null) return;
            float step = entry.fadeTime > 0f ? Time.unscaledDeltaTime / entry.fadeTime : 1f;
            entry.current = Mathf.MoveTowards(entry.current, entry.target, step);
        }
    }
}
