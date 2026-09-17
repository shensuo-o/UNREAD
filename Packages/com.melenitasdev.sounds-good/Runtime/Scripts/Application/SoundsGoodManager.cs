/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Audio;
using MelenitasDev.SoundsGood.Domain;

[assembly: InternalsVisibleTo("SoundsGood.Editor")]

namespace MelenitasDev.SoundsGood
{
    public partial class SoundsGoodManager // Fields
    {
        private static readonly List<SoundsGoodAudioSource> audioSourcePool = new List<SoundsGoodAudioSource>();

        private static GameObject audioSourcesPoolParent;
        private static AudioListener cachedListener;

        // Raycast buffers used only by the occlusion engine.
//#SG_PRO_BEGIN
        private static readonly RaycastHit[] occlusionDepthHits = new RaycastHit[16];
        private static readonly RaycastHit[] occlusionRayHits = new RaycastHit[16];
//#SG_PRO_END
    }

    public partial class SoundsGoodManager : MonoBehaviour
    {
    }

    public partial class SoundsGoodManager // Internal Static Methods
    {
        internal static SoundsGoodAudioSource GetSource () { return GetSoundSourceFromPool(); }

        /// <summary> Every pooled source, in use or not. Used by AudioEffectZone to find the ones inside it. </summary>
        internal static List<SoundsGoodAudioSource> AudioSourcePool => audioSourcePool;

        internal static AudioMixerGroup GetOutput (Output output) { return GetOutput(output.ToString()); }

        internal static AudioClip GetSFX (string tag, int index = -1)
        {
            if (AssetLocator.Instance.SoundDataCollection == null) return null;
            SoundData soundData = AssetLocator.Instance.SoundDataCollection.GetSound(tag);
            return soundData.GetClip(index);
        }

        internal static AudioClip GetTrack (string tag, int index = -1)
        {
            if (AssetLocator.Instance.MusicDataCollection == null) return null;
            SoundData soundData = AssetLocator.Instance.MusicDataCollection.GetMusicTrack(tag);
            return soundData.GetClip(index);
        }

        // Effect and occlusion material lookups.
//#SG_PRO_BEGIN
        internal static AudioEffectPreset GetEffect (Effect effect)
        {
            return effect.IsNull ? null : GetEffect(effect.ToString());
        }

        internal static AudioEffectPreset GetEffect (string tag)
        {
            if (AssetLocator.Instance.EffectDataCollection == null) return null;
            return AssetLocator.Instance.EffectDataCollection.GetEffect(tag);
        }

        internal static AudioOcclusionMaterial GetOcclusionMaterial (OcclusionMaterial material)
        {
            return material.IsNull ? null : GetOcclusionMaterial(material.ToString());
        }

        internal static AudioOcclusionMaterial GetOcclusionMaterial (string tag)
        {
            if (AssetLocator.Instance.OcclusionMaterialCollection == null) return null;
            return AssetLocator.Instance.OcclusionMaterialCollection.GetMaterial(tag);
        }
//#SG_PRO_END

        internal static float GetSavedOutputVolume (string outputName)
        {
            if (PlayerPrefs.HasKey(outputName)) return PlayerPrefs.GetFloat(outputName);
            PlayerPrefs.SetFloat(outputName, 1f);
            return 1f;
        }

        internal static bool TryGetListener (out AudioListener listener)
        {
            if (cachedListener == null)
            {
#if UNITY_6000_0_OR_NEWER
                cachedListener = FindAnyObjectByType<AudioListener>(FindObjectsInactive.Include);
#else
                cachedListener = FindObjectOfType<AudioListener>();
#endif
            }

            listener = cachedListener;
            return listener != null;
        }

        // CalculateOcclusion: the occlusion engine.
//#SG_PRO_BEGIN
        internal static void CalculateOcclusion (Vector3 listenerPosition, Vector3 sourcePosition,
            out float factor, out float depth)
        {
            factor = 0f;
            depth = 0f;

            if (!AssetLocator.SoundsGoodSettings.EnableOcclusion) return;

            Vector3 toSource = sourcePosition - listenerPosition;
            float distance = toSource.magnitude;
            if (distance <= 0.1f || distance > AssetLocator.SoundsGoodSettings.MaxDistance) return;

            Vector3 dir = toSource / distance;
            LayerMask mask = AssetLocator.SoundsGoodSettings.OcclusionLayers;

            // Build a basis perpendicular to the line of sight so the probe rays spread
            // across it. A partial obstacle then blocks only some rays, giving a gradual
            // occlusion factor instead of an all-or-nothing result.
            Vector3 perpRight = Vector3.Cross(dir, Vector3.up);
            if (perpRight.sqrMagnitude < 0.0001f) perpRight = Vector3.Cross(dir, Vector3.forward);
            perpRight.Normalize();
            Vector3 perpUp = Vector3.Cross(dir, perpRight).normalized;

            float directSpread = Mathf.Max(AssetLocator.SoundsGoodSettings.BounceRadiusMin, 0.1f) * 0.5f;

            // How much a ray is blocked, accounting for every surface it crosses — not just the
            // first one. Each surface only lets (1 - density) of what reaches it through, so the
            // whole stack transmits the product of those: a 50% window behind another 50% window
            // lets 25% pass, i.e. 75% blocked. Densities are never summed, which would let two
            // light surfaces claim a full block. A clear ray contributes 0, and a single solid
            // collider (density 1, the default without an Occlusion Surface) still blocks fully,
            // so scenes with no materials behave exactly as before.
            float RayDensity (Vector3 origin)
            {
                int count = Physics.RaycastNonAlloc(origin, dir, occlusionRayHits, distance, mask,
                    QueryTriggerInteraction.Ignore);

                float transmission = 1f;
                for (int i = 0; i < count; i++)
                {
                    transmission *= 1f - OcclusionSurfaceRegistry.GetDensity(occlusionRayHits[i].collider);
                    // Already fully blocked: nothing behind it can matter.
                    if (transmission <= 0f) return 1f;
                }

                return 1f - transmission;
            }

            const int directSamples = 5;
            float directDensitySum = 0f;
            directDensitySum += RayDensity(listenerPosition);
            directDensitySum += RayDensity(listenerPosition + perpRight * directSpread);
            directDensitySum += RayDensity(listenerPosition - perpRight * directSpread);
            directDensitySum += RayDensity(listenerPosition + perpUp * directSpread);
            directDensitySum += RayDensity(listenerPosition - perpUp * directSpread);

            float directRatio = directDensitySum / directSamples;

            float bounceRatio = 0f;
            int bounceSamples = 0;
            float bounceDensitySum = 0f;

            int maxBounces = AssetLocator.SoundsGoodSettings.MaxBounces;
            int raysPerCircle = AssetLocator.SoundsGoodSettings.BounceRaysPerCircle;

            if (maxBounces > 0 && raysPerCircle > 0)
            {
                float baseRadius = Mathf.Max(AssetLocator.SoundsGoodSettings.BounceRadiusMin, 0.1f);

                for (int ring = 1; ring <= maxBounces; ring++)
                {
                    float r = baseRadius * ring;
                    for (int i = 0; i < raysPerCircle; i++)
                    {
                        float angle = (Mathf.PI * 2f) * (i / (float)raysPerCircle);
                        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * r;
                        Vector3 origin = listenerPosition + offset;
                        bounceSamples++;

                        bounceDensitySum += RayDensity(origin);
                    }
                }

                if (bounceSamples > 0)
                {
                    bounceRatio = bounceDensitySum / bounceSamples;
                }
            }

            factor = Mathf.Clamp01(directRatio * 0.75f + bounceRatio * 0.25f);

            int hitCount = Physics.RaycastNonAlloc(listenerPosition, dir, occlusionDepthHits, distance, mask,
                QueryTriggerInteraction.Ignore);

            float depthDensitySum = 0f;
            for (int i = 0; i < hitCount; i++)
                depthDensitySum += OcclusionSurfaceRegistry.GetDensity(occlusionDepthHits[i].collider);

            // Layers beyond the first obstacle, weighted by how much each blocks: a solid wall counts
            // as ~1 layer, three thin windows barely add depth. With every density at 1 this is just
            // the extra obstacle count, so scenes without materials behave exactly as before.
            float extraLayers = Mathf.Max(0f, depthDensitySum - 1f);

            int maxLayers = Mathf.Max(1, AssetLocator.SoundsGoodSettings.MaxOcclusionLayers);
            if (maxLayers <= 1)
            {
                depth = extraLayers > 0f ? 1f : 0f;
            }
            else
            {
                depth = Mathf.Clamp01(extraLayers / (maxLayers - 1));
            }
        }
//#SG_PRO_END
    }

    public partial class SoundsGoodManager // Public Static Methods
    {
        /// <summary>
        /// Get last saved output volume.
        /// </summary>
        /// <param name="output">Target output</param>
        [Obsolete("The output volume is now auto-updated to the last saved volume. " +
                  "There is no longer any reason to use this method.")]
        public static float GetLastSavedOutputVolume (Output output)
        {
            if (PlayerPrefs.HasKey(output.ToString())) return PlayerPrefs.GetFloat(output.ToString());
            Debug.LogWarning($"The {output.ToString()}'s volume has not been saved yet");
            return 0.5f;
        }

        /// <summary>
        /// Change Output volume.
        /// </summary>
        /// <param name="output">Target output</param>
        /// <param name="value">Target volume: min 0, Max: 1</param>
        public static void ChangeOutputVolume (Output output, float value)
        {
            ChangeOutputVolume(output.ToString(), value);
        }

        /// <summary>
        /// Change Output volume.
        /// </summary>
        /// <param name="outputName">Target output name</param>
        /// <param name="value">Target volume: min 0, Max: 1</param>
        public static void ChangeOutputVolume (string outputName, float value)
        {
            AudioMixerGroup outputGroup = GetOutput(outputName);

            if (outputGroup == null || outputGroup.audioMixer == null)
            {
                Debug.LogError($"Can't change mixer volume because {outputName} don't exist." +
                               $"Make sure you have updated the outputs database on Outputs Manager window");
                return;
            }

            AudioMixer mixer = outputGroup.audioMixer;

            if (Application.isPlaying) SetAudioMixerLinearVolume(mixer, outputName, value);
            PlayerPrefs.SetFloat(outputName, value);
        }

        // Global and per-Output effects: the whole paid effects API.
//#SG_PRO_BEGIN
        /// <summary>
        /// Apply an audio effect to every Sounds Good sound at once (2D and 3D), fading it in.
        /// Great for global states like pausing (muffle everything) or hit/stun effects. It wins
        /// over any Audio Effect Zone while active. Call <see cref="RemoveGlobalEffect"/> to clear it.
        /// Note: this affects Sounds Good sounds only, not arbitrary AudioSources.
        /// </summary>
        /// <param name="effect">Effect created in the Effect Creator window.</param>
        /// <param name="intensity">How strongly to apply it (0 none, 1 the effect as saved).</param>
        /// <param name="fadeTime">Seconds to fade the effect in.</param>
        public static void SetGlobalEffect (Effect effect, float intensity = 1f, float fadeTime = 0f)
        {
            GlobalEffectDriver.GetOrCreate().SetGlobal(GetEffect(effect), Mathf.Clamp01(intensity), fadeTime);
        }

        /// <summary>
        /// Remove the global effect set with <see cref="SetGlobalEffect"/>, fading it out.
        /// </summary>
        /// <param name="fadeTime">Seconds to fade the effect out.</param>
        public static void RemoveGlobalEffect (float fadeTime = 0f)
        {
            GlobalEffectDriver.GetOrCreate().RemoveGlobal(fadeTime);
        }

        /// <summary>
        /// Like <see cref="SetGlobalEffect"/>, but only for the sounds routed through a given Output
        /// (e.g. muffle just the SFX bus while the menu music stays clean).
        /// </summary>
        /// <param name="output">Target output.</param>
        /// <param name="effect">Effect created in the Effect Creator window.</param>
        /// <param name="intensity">How strongly to apply it (0 none, 1 the effect as saved).</param>
        /// <param name="fadeTime">Seconds to fade the effect in.</param>
        public static void SetOutputEffect (Output output, Effect effect, float intensity = 1f, float fadeTime = 0f)
        {
            GlobalEffectDriver.GetOrCreate().SetOutput(GetOutput(output), GetEffect(effect), Mathf.Clamp01(intensity), fadeTime);
        }

        /// <summary>
        /// Remove the effect set on an Output with <see cref="SetOutputEffect"/>, fading it out.
        /// </summary>
        /// <param name="output">Target output.</param>
        /// <param name="fadeTime">Seconds to fade the effect out.</param>
        public static void RemoveOutputEffect (Output output, float fadeTime = 0f)
        {
            GlobalEffectDriver.GetOrCreate().RemoveOutput(GetOutput(output), fadeTime);
        }

        /// <summary>
        /// Like <see cref="SetGlobalEffect(Effect,float,float)"/> but taking the effect tag as a
        /// string, so the call survives even if that effect is later removed (e.g. demo scripts).
        /// </summary>
        public static void SetGlobalEffect (string effectTag, float intensity = 1f, float fadeTime = 0f)
        {
            GlobalEffectDriver.GetOrCreate().SetGlobal(GetEffect(effectTag), Mathf.Clamp01(intensity), fadeTime);
        }

        /// <summary>
        /// Like <see cref="SetOutputEffect(Output,Effect,float,float)"/> but taking the effect tag as
        /// a string, so the call survives even if that effect is later removed (e.g. demo scripts).
        /// </summary>
        public static void SetOutputEffect (Output output, string effectTag, float intensity = 1f, float fadeTime = 0f)
        {
            GlobalEffectDriver.GetOrCreate().SetOutput(GetOutput(output), GetEffect(effectTag), Mathf.Clamp01(intensity), fadeTime);
        }
//#SG_PRO_END

        /// <summary>
        /// Pause all sounds, music, dynamic music and playlists.
        /// </summary>
        /// <param name="fadeOutTime">Seconds that fade out will last</param>
        public static void PauseAll (float fadeOutTime = 0)
        {
            foreach (SoundsGoodAudioSource sourcePoolElement in audioSourcePool)
            {
                sourcePoolElement.Pause(fadeOutTime);
            }
        }

        /// <summary>
        /// Pause specific sound, music, dynamic music or playlist without having the sound reference.
        /// </summary>
        /// <param name="id">The Id you've set to the music</param>
        /// <param name="fadeOutTime">Seconds that fade out will last</param>
        public static void Pause (string id, float fadeOutTime = 0)
        {
            var sourcePoolElement = audioSourcePool.FirstOrDefault(sourceElement => sourceElement.Id == id);
            if (sourcePoolElement == null || !sourcePoolElement.Using)
            {
                Debug.LogWarning($"There is no music with the id '{id}'");
                return;
            }

            sourcePoolElement.Pause(fadeOutTime);
        }


        /// <summary>
        /// Stop all sounds, music, dynamic music and playlists.
        /// </summary>
        /// <param name="fadeOutTime">Seconds that fade out will last</param>
        public static void StopAll (float fadeOutTime = 0)
        {
            foreach (SoundsGoodAudioSource sourcePoolElement in audioSourcePool)
            {
                sourcePoolElement.Stop(fadeOutTime);
            }
        }

        /// <summary>
        /// Stop specific sound, music, dynamic music or playlist without having the sound reference.
        /// </summary>
        /// <param name="id">The Id you've set to the sound</param>
        /// <param name="fadeOutTime">Seconds that fade out will last</param>
        public static void Stop (string id, float fadeOutTime = 0)
        {
            var sourcePoolElement = audioSourcePool.FirstOrDefault(sourceElement => sourceElement.Id == id);
            if (sourcePoolElement == null || !sourcePoolElement.Using)
            {
                Debug.LogWarning($"There is no sound reproducing with the id '{id}'");
                return;
            }

            sourcePoolElement.Stop(fadeOutTime);
        }

        /// <summary>
        /// Resume all sounds, music, dynamic music and playlists.
        /// </summary>
        /// <param name="fadeInTime">Seconds that fade in will last</param>
        public static void ResumeAll (float fadeInTime = 0)
        {
            foreach (SoundsGoodAudioSource sourcePoolElement in audioSourcePool)
            {
                if (sourcePoolElement.Paused) sourcePoolElement.Resume(fadeInTime);
            }
        }

        /// <summary>
        /// Resume specific sound, music, dynamic music or playlist without having the sound reference.
        /// </summary>
        /// <param name="id">The Id you've set to the sound</param>
        /// <param name="fadeInTime">Seconds that fade in will last</param>
        public static void Resume (string id, float fadeInTime = 0)
        {
            var sourcePoolElement = audioSourcePool.FirstOrDefault(s => s.Id == id);
            if (sourcePoolElement == null || !sourcePoolElement.Paused)
            {
                Debug.LogWarning($"There is no sound paused with the id '{id}'");
                return;
            }

            sourcePoolElement.Resume(fadeInTime);
        }

        // ----- Obsoletes methods

        /// <summary> [OBSOLETE] Use <see cref="Stop(string,float)"/>. </summary>
        [Obsolete("Use SoundsGoodManager.Stop(id, fadeOutTime) instead. " +
                  "This method will be removed in a future version.")]
        public static void StopSound (string id, float fadeOutTime = 0) => Stop(id, fadeOutTime);

        /// <summary> [OBSOLETE] Use <see cref="Stop(string,float)"/>. </summary>
        [Obsolete("Use SoundsGoodManager.Stop(id, fadeOutTime) instead. " +
                  "This method will be removed in a future version.")]
        public static void StopMusic (string id, float fadeOutTime = 0) => Stop(id, fadeOutTime);

        /// <summary> [OBSOLETE] Use <see cref="StopAll(float)"/>. </summary>
        [Obsolete("Use SoundsGoodManager.StopAll(fadeOutTime) instead. " +
                  "This method will be removed in a future version.")]
        public static void StopAllSounds (float fadeOutTime = 0) => StopAll(fadeOutTime);

        /// <summary> [OBSOLETE] Use <see cref="StopAll(float)"/>. </summary>
        [Obsolete("Use SoundsGoodManager.StopAll(fadeOutTime) instead. " +
                  "This method will be removed in a future version.")]
        public static void StopAllMusic (float fadeOutTime = 0) => StopAll(fadeOutTime);

        /// <summary> [OBSOLETE] Use <see cref="Pause(string,float)"/>. </summary>
        [Obsolete("Use SoundsGoodManager.Pause(id, fadeOutTime) instead. " +
                  "This method will be removed in a future version.")]
        public static void PauseSound (string id, float fadeOutTime = 0) => Pause(id, fadeOutTime);

        /// <summary> [OBSOLETE] Use <see cref="Pause(string,float)"/>. </summary>
        [Obsolete("Use SoundsGoodManager.Pause(id, fadeOutTime) instead. " +
                  "This method will be removed in a future version.")]
        public static void PauseMusic (string id, float fadeOutTime = 0) => Pause(id, fadeOutTime);

        /// <summary> [OBSOLETE] Use <see cref="PauseAll(float)"/>. </summary>
        [Obsolete("Use SoundsGoodManager.PauseAll(fadeOutTime) instead. " +
                  "This method will be removed in a future version.")]
        public static void PauseAllSounds (float fadeOutTime = 0) => PauseAll(fadeOutTime);

        /// <summary> [OBSOLETE] Use <see cref="PauseAll(float)"/>. </summary>
        [Obsolete("Use SoundsGoodManager.PauseAll(fadeOutTime) instead. " +
                  "This method will be removed in a future version.")]
        public static void PauseAllMusic (float fadeOutTime = 0) => PauseAll(fadeOutTime);
    }

    public partial class SoundsGoodManager // Private Methods
    {
        private static SoundsGoodAudioSource GetSoundSourceFromPool ()
        {
            if (audioSourcesPoolParent == null || !audioSourcesPoolParent.activeInHierarchy)
            {
                audioSourcesPoolParent = new GameObject("Sources Pool Parent");
                DontDestroyOnLoad(audioSourcesPoolParent);
            }

            if (audioSourcePool.Count != audioSourcesPoolParent.transform.childCount)
            {
                audioSourcePool.Clear();
            }

            foreach (SoundsGoodAudioSource element in audioSourcePool)
            {
                if (!element.Using) return element;
            }

            GameObject newSourceInstance = new GameObject($"Audio Source {audioSourcePool.Count}");
            DontDestroyOnLoad(newSourceInstance);
            newSourceInstance.transform.SetParent(audioSourcesPoolParent.transform);

            AudioSource source = newSourceInstance.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.dopplerLevel = 0;
            SoundsGoodAudioSource soundsGoodAudioSourceElement =
                newSourceInstance.AddComponent<SoundsGoodAudioSource>().Init(source);

            audioSourcePool.Add(soundsGoodAudioSourceElement);
            return soundsGoodAudioSourceElement;
        }

        private static AudioMixerGroup GetOutput (string outputName)
        {
            if (AssetLocator.Instance.OutputDataCollection == null) return null;

            AudioMixerGroup audioMixerGroup = AssetLocator.Instance.OutputDataCollection.GetOutput(outputName);

            if (audioMixerGroup == null) return null;

            float lastSavedVolume = GetSavedOutputVolume(outputName);
            SetAudioMixerLinearVolume(audioMixerGroup.audioMixer, outputName, lastSavedVolume);
            return audioMixerGroup;
        }

        private static void SetAudioMixerLinearVolume (AudioMixer audioMixer, string volumeParameterName, float volume)
        {
            audioMixer.SetFloat(volumeParameterName,
                Mathf.Log10(Mathf.Clamp(volume, 0.001f, 0.99f)) * 20);
        }
    }
}