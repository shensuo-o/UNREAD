/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using MelenitasDev.SoundsGood.Domain;

namespace MelenitasDev.SoundsGood
{
    public partial class DynamicMusic // Fields
    {
        private SoundsGoodAudioSource referenceSoundsGoodAudioSource = null;
        private readonly Dictionary<string, SoundsGoodAudioSource> sourcePoolElementDictionary 
            = new Dictionary<string, SoundsGoodAudioSource>();

        private readonly Dictionary<string, float> volumeDictionary = new Dictionary<string, float>();
        // Scales every track's own volume, so the whole music can be raised or lowered without
        // touching the blend between layers.
        private float masterVolume = 1f;
        private float minHearDistance = 3;
        private float maxHearDistance = 500;
        private AudioRolloffMode audioRolloffMode;
        private AnimationCurve customVolumeCurve;
        private float pitch = 1;
        private float dopplerLevel = 1;
        private string id = null;
        private Vector3 position = Vector3.zero;
        private Transform followTarget = null;
        private bool loop = false;
        private bool spatialSound = false;
        private bool useOcclusion = false;
        private TimeMode timeMode = TimeMode.Unscaled;
        private AudioEffectPreset effectPreset = null;
        private float effectIntensity = 1f;
        private float fadeOutTime = 0;
        private AudioClip[] clips;
        private string[] cachedClipsTags = Array.Empty<string>();
        private AudioMixerGroup output = null;
    }

    public partial class DynamicMusic // Fields (Callbacks)
    {
        private Action onPlay;
        private Action onComplete;
        private Action onLoopCycleComplete;
        private Action onPause;
        private Action onPauseComplete;
        private Action onResume;
    }

    public partial class DynamicMusic // Properties
    {
        /// <summary>It's true when it's being used. When it's paused, it's true as well</summary>
        public bool Using => referenceSoundsGoodAudioSource != null;
        /// <summary>It's true when audio is playing.</summary>
        public bool Playing => Using && referenceSoundsGoodAudioSource.Playing;
        /// <summary>It's true when audio paused (it ignore the fade out time).</summary>
        public bool Paused => Using && referenceSoundsGoodAudioSource.Paused;
        /// <summary>Volume level between [0,1].</summary>
        public float Volume => Using ? referenceSoundsGoodAudioSource.Volume : volumeDictionary.ElementAt(0).Value;
        /// <summary>Master volume between [0,1], applied on top of every track's own volume.</summary>
        public float MasterVolume => masterVolume;
        /// <summary>Pitch level.</summary>
        public float Pitch => Using ? referenceSoundsGoodAudioSource.Pitch : pitch;
        /// <summary>Total time in seconds that it have been playing.</summary>
        public float PlayingTime => Using ? referenceSoundsGoodAudioSource.PlayingTime : 0;
        /// <summary>Reproduced time in seconds of current loop cycle.</summary>
        public float CurrentLoopCycleTime => Using ? referenceSoundsGoodAudioSource.CurrentLoopCycleTime : 0;
        /// <summary>Times it has looped.</summary>
        public int CompletedLoopCycles => Using ? referenceSoundsGoodAudioSource.CompletedLoopCycles : 0;
        /// <summary>Duration in seconds of matched clip (use the first clip of the array because they should have the same duration).</summary>
        public float ClipDuration => clips.Length > 0 ? clips[0].length : 0;
        /// <summary>Matched clip.</summary>
        public AudioClip[] Clips => clips;
    }

    public partial class DynamicMusic // Public Methods
    {
        /// <summary>
        /// Create new Dynamic Music object.
        /// </summary>
        public DynamicMusic () { }
        
        /// <summary>
        /// Create new Dynamic Music object given a Tracks array.
        /// </summary>
        /// <param name="tracks">Track array with all music tracks that you want to reproduce at the same time</param>
        public DynamicMusic (params Track[] tracks)
        {
            CacheClipsTags(tracks);
        }
        
        /// <summary>
        /// Create new Dynamic Music object given a tags array.
        /// </summary>
        /// <param name="tag">Track array with all music tracks tags that you want to reproduce at the same time</param>
        public DynamicMusic (params string[] tags)
        {
            CacheClipsTags(tags);
        }
        
        /// <summary>
        /// Store the master volume BEFORE play Dynamic Music. Unlike <see cref="SetAllVolumes"/>,
        /// which overwrites each track's own volume, this one scales all of them, so the blend
        /// between layers is kept and you can raise or lower the whole music at once.
        /// </summary>
        /// <param name="volume">Volume: min 0, Max 1</param>
        public DynamicMusic SetMasterVolume (float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            return this;
        }

        /// <summary>
        /// Store volume parameters of all tracks BEFORE play Dynamic Music.
        /// </summary>
        /// <param name="volume">Volume: min 0, Max 1</param>
        public DynamicMusic SetAllVolumes (float volume)
        {
            if (!TrySetCachedClips()) return this;
            
            foreach (var cachedTag in cachedClipsTags)
            {
                volumeDictionary[cachedTag] = volume;
            }
            return this;
        }
        
        /// <summary>
        /// Store volume parameters of all tracks BEFORE play Dynamic Music.
        /// </summary>
        /// <param name="volume">Volume: min 0, Max 1.</param>
        /// <param name="hearDistance">Distance range to hear music</param>
        [Obsolete("This method has been deprecated. If you need to change the hear distance, " +
        "use the method SetHearDistance(float minHearDistance, float maxHearDistance) instead.")]
        public DynamicMusic SetAllVolumes (float volume, Vector2 hearDistance)
        {
            if (!TrySetCachedClips()) return this;
            
            foreach (var cachedTag in cachedClipsTags)
            {
                volumeDictionary[cachedTag] = volume;
            }
            minHearDistance = hearDistance.x;
            maxHearDistance = hearDistance.y;
            return this;
        }
        
        /// <summary>
        /// Store volume parameters of specific track BEFORE play Dynamic Music.
        /// </summary>
        /// /// <param name="track">Track you want modify.</param>
        /// <param name="volume">Volume: min 0, Max 1.</param>
        public DynamicMusic SetTrackVolume (Track track, float volume)
        {
            return SetTrackVolume(track.ToString(), volume);
        }
        
        /// <summary>
        /// Store volume parameters of specific track BEFORE play Dynamic Music.
        /// </summary>
        /// /// <param name="tag">Track you want modify.</param>
        /// <param name="volume">Volume: min 0, Max 1.</param>
        public DynamicMusic SetTrackVolume (string tag, float volume)
        {
            if (!TrySetCachedClips()) return this;
            
            if (cachedClipsTags.Contains(tag))
            {
                volumeDictionary[tag] = volume;
            }
            return this;
        }
        
        /// <summary>
        /// Set volume parameters BEFORE play music.
        /// </summary>
        /// /// <param name="track">Track you want modify.</param>
        /// <param name="volume">Volume: min 0, Max 1.</param>
        /// <param name="hearDistance">min and Max distance to hear music</param>
        [Obsolete("This method has been deprecated. If you need to change the hear distance, " +
                  "use the method SetHearDistance(float minHearDistance, float maxHearDistance) instead.")]
        public DynamicMusic SetTrackVolume (Track track, float volume, Vector2 hearDistance)
        {
            return SetTrackVolume(track.ToString(), volume, hearDistance);
        }
        
        /// <summary>
        /// Set volume parameters BEFORE play music.
        /// </summary>
        /// /// <param name="tag">Track you want modify.</param>
        /// <param name="volume">Volume: min 0, Max 1.</param>
        /// <param name="hearDistance">min and Max distance to hear music</param>
        [Obsolete("This method has been deprecated. If you need to change the hear distance, " +
                  "use the method SetHearDistance(float minHearDistance, float maxHearDistance) instead.")]
        public DynamicMusic SetTrackVolume (string tag, float volume, Vector2 hearDistance)
        {
            if (!TrySetCachedClips()) return this;
            
            if (cachedClipsTags.Contains(tag))
            {
                volumeDictionary[tag] = volume;
            }
            return this;
        }

        /// <summary>
        /// Sets the minimum and maximum hearing distances for the AudioSource.
        /// Music will start to fade in at the maximum distance and be fully audible until the minimum distance is reached.
        /// </summary>
        /// <param name="minHearDistance">Distance at which the music be fully audible.</param>
        /// <param name="maxHearDistance">Distance at which the music starts becoming audible.</param>
        public DynamicMusic SetHearDistance (float minHearDistance, float maxHearDistance)
        {
            this.minHearDistance = minHearDistance;
            this.maxHearDistance = maxHearDistance;
            return this;
        }
        
        /// <summary>
        /// Sets how the sound volume fades over distance using one of the predefined curve types.
        /// </summary>
        /// <param name="Logarithmic">fades more naturally, similar to real-world sounds.</param>
        /// <param name="Linear">fades at a steady, constant rate.</param>
        public DynamicMusic SetVolumeRolloffCurve (VolumeRolloffCurve volumeRolloffCurve)
        {
            audioRolloffMode = volumeRolloffCurve switch
            {
                VolumeRolloffCurve.Logarithmic => AudioRolloffMode.Logarithmic,
                VolumeRolloffCurve.Linear => AudioRolloffMode.Linear,
                _ => AudioRolloffMode.Logarithmic
            };
            return this;
        }
        
        /// <summary>
        /// Sets a custom curve that controls how the sound volume fades with distance.  
        /// Use this if you want full control over how the fade behaves.
        /// </summary>
        /// <param name="customVolumeCurve">
        /// An AnimationCurve that defines how the sound volume decreases as the listener moves away.</param>
        public DynamicMusic SetCustomVolumeRolloffCurve (AnimationCurve customVolumeCurve)
        {
            audioRolloffMode = AudioRolloffMode.Custom;
            this.customVolumeCurve = customVolumeCurve;
            return this;
        }
        
        /// <summary>
        /// Change the master volume while music is reproducing. Every track keeps its own volume,
        /// so the blend between layers is untouched — use it as the music's general volume control.
        /// </summary>
        /// <param name="newVolume">New master volume: min 0, Max 1</param>
        /// <param name="lerpTime">Time to lerp current to new volume</param>
        public void ChangeMasterVolume (float newVolume, float lerpTime = 0)
        {
            newVolume = Mathf.Clamp01(newVolume);
            if (masterVolume == newVolume) return;
            masterVolume = newVolume;

            if (!Using) return;

            foreach (var tagSourcePair in sourcePoolElementDictionary)
            {
                tagSourcePair.Value.SetVolume(PlaybackVolume(tagSourcePair.Key), lerpTime);
            }
        }

        /// <summary>
        /// Change all tracks volume while music is reproducing.
        /// </summary>
        /// <param name="newVolume">New volume: min 0, Max 1</param>
        /// <param name="lerpTime">Time to lerp current to new volume</param>
        public void ChangeAllVolumes (float newVolume, float lerpTime = 0)
        {
            foreach (var tagSourcePair in sourcePoolElementDictionary)
            {
                if (volumeDictionary[tagSourcePair.Key] == newVolume) continue;
                volumeDictionary[tagSourcePair.Key] = newVolume;

                if (!Using) return;

                tagSourcePair.Value.SetVolume(PlaybackVolume(tagSourcePair.Key), lerpTime);
            }
        }

        /// <summary>
        /// Change volume while music is reproducing.
        /// </summary>
        /// <param name="track">Track you want modify.</param>
        /// <param name="newVolume">New volume: min 0, Max 1.</param>
        /// <param name="lerpTime">Time to lerp current to new volume.</param>
        public void ChangeTrackVolume (Track track, float newVolume, float lerpTime = 0)
        {
            ChangeTrackVolume(track.ToString(), newVolume, lerpTime);
        }
        
        /// <summary>
        /// Change volume while music is reproducing.
        /// </summary>
        /// <param name="track">Track you want modify.</param>
        /// <param name="newVolume">New volume: min 0, Max 1.</param>
        /// <param name="lerpTime">Time to lerp current to new volume.</param>
        public void ChangeTrackVolume (string tag, float newVolume, float lerpTime = 0)
        {
            foreach (var tagSourcePair in sourcePoolElementDictionary)
            {
                if (!tagSourcePair.Key.Equals(tag)) continue;
                
                if (volumeDictionary[tagSourcePair.Key] == newVolume) return;
                volumeDictionary[tagSourcePair.Key] = newVolume;

                if (!Using) return;

                tagSourcePair.Value.SetVolume(PlaybackVolume(tagSourcePair.Key), lerpTime);
                return;
            }
        }
        
        /// <summary>
        /// Change all tracks pitch while music is reproducing.
        /// </summary>
        /// <param name="newPitch">New pitch multiplier.</param>
        /// <param name="lerpTime">Time to lerp current to new pitch.</param>
        public void ChangePitch (float newPitch, float lerpTime = 0)
        {
            if (pitch == newPitch) return;
            
            pitch = newPitch;
            
            if (!Using) return;

            foreach (var tagSourcePair in sourcePoolElementDictionary)
            {
                tagSourcePair.Value.SetPitch(newPitch, lerpTime);
            }
        }

        /// <summary>
        /// Set given pitch. Make your music sound different :)
        /// </summary>
        public DynamicMusic SetPitch (float pitch)
        {
            this.pitch = pitch;
            return this;
        }

        /// <summary>
        /// Sets how strongly the Doppler effect is applied to the music when the listener or sound source is moving.
        /// </summary>
        /// <param name="dopplerLevel">Value between 0 and 5; 1 is the default and recommended for realistic results.</param>
        public DynamicMusic SetDopplerLevel (float dopplerLevel)
        {
            this.dopplerLevel = Mathf.Clamp(dopplerLevel, 0, 5);
            return this;
        }

        /// <summary>
        /// Choose whether this dynamic music follows <c>Time.timeScale</c> (<see cref="TimeMode.Scaled"/>,
        /// the default) or ignores it (<see cref="TimeMode.Unscaled"/>). Applies to every layer, so
        /// they stay in sync. Unscaled keeps them playing at normal speed while paused or in slow/fast motion.
        /// </summary>
        public DynamicMusic SetTimeMode (TimeMode timeMode)
        {
            this.timeMode = timeMode;
            if (Using)
                foreach (var tagSourcePair in sourcePoolElementDictionary)
                    tagSourcePair.Value.SetUseScaledTime(timeMode == TimeMode.Scaled);
            return this;
        }
        
        /// <summary>
        /// Set an id to identify this music on AudioManager static methods.
        /// </summary>
        public DynamicMusic SetId (string id)
        {
            this.id = id;
            return this;
        }
        
        /// <summary>
        /// Make your music loops for infinite time. If you need to stop it, use Stop() method.
        /// </summary>
        public DynamicMusic SetLoop (bool loop = true)
        {
            this.loop = loop;
            return this;
        }

        /// <summary>
        /// Set new music tracks to this Dynamic Music BEFORE play it.
        /// </summary>
        /// <param name="tracks">Tracks array with all music tracks that you want to reproduce at the same time</param>
        public DynamicMusic SetClips (params Track[] tracks)
        {
            var tracksTags = tracks.Select(track => track.ToString()).ToArray();
            return SetClips(tracksTags);
        }
        
        /// <summary>
        /// Set new music tracks to this Dynamic Music BEFORE play it.
        /// </summary>
        /// <param name="tracksTags">Tracks tags array with all music tracks that you want to reproduce at the same time</param>
        public DynamicMusic SetClips (params string[] tracksTags)
        {
            if (!CheckClipsAreValid(tracksTags)) return this;
            
            cachedClipsTags = new string[tracksTags.Length];
            Array.Copy(tracksTags, cachedClipsTags, tracksTags.Length);
            
            sourcePoolElementDictionary.Clear();
            clips = new AudioClip[tracksTags.Length];
            int i = 0;
            foreach (string track in tracksTags)
            {
                clips[i] = SoundsGoodManager.GetTrack(track);
                volumeDictionary.TryAdd(track, 1f);
                i++;
            }

            return this;
        }
        
        /// <summary>
        /// Set the position of the sound emitter.
        /// </summary>
        public DynamicMusic SetPosition (Vector3 position)
        {
            this.position = position;
            return this;
        }
        
        /// <summary>
        /// Set a target to follow. Audio source will update its position every frame.
        /// </summary>
        /// <param name="followTarget">Transform to follow. Null to follow Main Camera transform.</param>
        public DynamicMusic SetFollowTarget (Transform followTarget)
        {
            this.followTarget = followTarget;
            return this;
        }
        
        /// <summary>
        /// Set spatial sound.
        /// </summary>
        /// <param name="true">Your sound will be 3D</param>
        /// <param name="false">Your sound will be global / 2D</param>
        public DynamicMusic SetSpatialSound (bool activate = true)
        {
            spatialSound = activate;
            return this;
        }
        
        /// <summary>
        /// Enables 3D occlusion for this audio.
        /// When enabled, the audio auto-switches to spatial mode to allow raycast-based occlusion.
        /// </summary>
        /// <param name="activate">True to enable occlusion, false to disable it</param>
        public DynamicMusic SetOcclusion (bool activate = true)
        {
            useOcclusion = activate;

            bool occlusionAvailable = AssetLocator.SoundsGoodSettings != null &&
                                      AssetLocator.SoundsGoodSettings.EnableOcclusion;
            if (activate && occlusionAvailable)
            {
                spatialSound = true;
            }
            return this;
        }


        /// <summary>
        /// Apply an audio effect you've created in the Effect Creator window to all its tracks.
        /// </summary>
        /// <param name="effect">Effect tag created in the Effect Creator</param>
        /// <param name="intensity">How much of the effect to apply: 0 none, 1 the effect
        /// exactly as you saved it. Values in between scale it down gradually.</param>
        public DynamicMusic SetEffect (Effect effect, float intensity = 1f)
        {
            effectPreset = SoundsGoodManager.GetEffect(effect);
            effectIntensity = Mathf.Clamp01(intensity);
            return this;
        }

        /// <summary>
        /// Apply an audio effect by its tag (string) to all its tracks. Use this instead of the
        /// Effect pseudo-enum when you want the reference to survive even if that effect is later
        /// removed (e.g. demo scripts).
        /// </summary>
        /// <param name="effectTag">Effect tag created in the Effect Creator</param>
        /// <param name="intensity">How much of the effect to apply: 0 none, 1 the effect
        /// exactly as you saved it. Values in between scale it down gradually.</param>
        public DynamicMusic SetEffect (string effectTag, float intensity = 1f)
        {
            effectPreset = SoundsGoodManager.GetEffect(effectTag);
            effectIntensity = Mathf.Clamp01(intensity);
            return this;
        }

        /// <summary>
        /// Change the current effect's intensity on every track while playing, optionally over a
        /// lerp time. Use it to fade an effect in or out at runtime.
        /// </summary>
        /// <param name="newIntensity">Target intensity: 0 none, 1 the effect exactly as saved.</param>
        /// <param name="lerpTime">Time in seconds to lerp to the new intensity (0 = instant).</param>
        public void ChangeEffect (float newIntensity, float lerpTime = 0)
        {
            effectIntensity = Mathf.Clamp01(newIntensity);
            if (!Using) return;
            foreach (var source in sourcePoolElementDictionary.Values)
                source.ChangeEffect(effectIntensity, lerpTime);
        }

        /// <summary>
        /// Swap the effect on every track while playing and lerp its intensity to the given value.
        /// </summary>
        /// <param name="effect">Effect tag created in the Effect Creator.</param>
        /// <param name="intensity">Target intensity: 0 none, 1 the effect exactly as saved.</param>
        /// <param name="lerpTime">Time in seconds to lerp the effect in (0 = instant).</param>
        public void ChangeEffect (Effect effect, float intensity = 1f, float lerpTime = 0)
        {
            effectPreset = SoundsGoodManager.GetEffect(effect);
            effectIntensity = Mathf.Clamp01(intensity);
            if (!Using) return;
            foreach (var source in sourcePoolElementDictionary.Values)
                source.ChangeEffect(effectPreset, effectIntensity, lerpTime);
        }

        /// <summary>
        /// Swap the effect (by tag) on every track while playing and lerp its intensity to the value.
        /// </summary>
        /// <param name="effectTag">Effect tag created in the Effect Creator.</param>
        /// <param name="intensity">Target intensity: 0 none, 1 the effect exactly as saved.</param>
        /// <param name="lerpTime">Time in seconds to lerp the effect in (0 = instant).</param>
        public void ChangeEffect (string effectTag, float intensity = 1f, float lerpTime = 0)
        {
            effectPreset = SoundsGoodManager.GetEffect(effectTag);
            effectIntensity = Mathf.Clamp01(intensity);
            if (!Using) return;
            foreach (var source in sourcePoolElementDictionary.Values)
                source.ChangeEffect(effectPreset, effectIntensity, lerpTime);
        }

        /// <summary>
        /// Set fade out duration. It'll be used when sound ends.
        /// </summary>
        /// <param name="fadeOutTime">Seconds that fade out will last</param>
        public DynamicMusic SetFadeOut (float fadeOutTime)
        {
            this.fadeOutTime = fadeOutTime;
            return this;
        }
        
        /// <summary>
        /// Set the audio output to manage the volume using the Audio Mixers.
        /// </summary>
        /// <param name="output">Output you've created before inside Master AudioMixer
        /// (Remember reload the outputs database on Output Manager Window)</param>
        public DynamicMusic SetOutput (Output output)
        {
            this.output = output.IsNull ? null : SoundsGoodManager.GetOutput(output);
            return this;
        }
        
        /// <summary>
        /// Define a callback that will be invoked on music start playing.
        /// </summary>
        /// <param name="onPlay">Method will be invoked</param>
        public DynamicMusic OnPlay (Action onPlay)
        {
            this.onPlay = onPlay;
            return this;
        }
        
        /// <summary>
        /// Define a callback that will be invoked on music complete.
        /// If "loop" is active, it'll be called when you Stop the sound manually.
        /// </summary>
        /// <param name="onComplete">Method will be invoked</param>
        public DynamicMusic OnComplete (Action onComplete)
        {
            this.onComplete = onComplete;
            return this;
        }
        
        /// <summary>
        /// Define a callback that will be invoked on loop cycle complete.
        /// You need to set loop on true to use it.
        /// </summary>
        /// <param name="onLoopCycleComplete">Method will be invoked</param>
        public DynamicMusic OnLoopCycleComplete (Action onLoopCycleComplete)
        {
            this.onLoopCycleComplete = onLoopCycleComplete;
            return this;
        }
        
        /// <summary>
        /// Define a callback that will be invoked on music pause.
        /// It will ignore the fade out time.
        /// </summary>
        /// <param name="onPause">Method will be invoked</param>
        public DynamicMusic OnPause (Action onPause)
        {
            this.onPause = onPause;
            return this;
        }
        
        /// <summary>
        /// Define a callback that will be invoked on music pause and fade out ends.
        /// </summary>
        /// <param name="onPauseComplete">Method will be invoked</param>
        public DynamicMusic OnPauseComplete (Action onPauseComplete)
        {
            this.onPauseComplete = onPauseComplete;
            return this;
        }
        
        /// <summary>
        /// Define a callback that will be invoked on resume/unpause music.
        /// </summary>
        /// <param name="onResume">Method will be invoked</param>
        public DynamicMusic OnResume (Action onResume)
        {
            this.onResume = onResume;
            return this;
        }

        /// <summary>
        /// Reproduce music.
        /// </summary>
        /// <param name="fadeInTime">Seconds that fade in will last</param>
        public void Play (float fadeInTime = 0)
        {
            if (!TrySetCachedClips()) return;
            
            if (Using && Playing)
            {
                Stop();
            }
            
            for (int i = 0; i < cachedClipsTags.Length; i++)
            {
                string tag = cachedClipsTags[i];
                
                SoundsGoodAudioSource newSoundsGoodAudioSource = SoundsGoodManager.GetSource();
                sourcePoolElementDictionary.TryAdd(tag, newSoundsGoodAudioSource);
                if (i == 0)
                {
                    referenceSoundsGoodAudioSource = newSoundsGoodAudioSource;
                    referenceSoundsGoodAudioSource
                        .SetVolume(PlaybackVolume(tag))
                        .SetHearDistance(minHearDistance, maxHearDistance)
                        .SetVolumeRolloffCurve(audioRolloffMode, customVolumeCurve)
                        .SetPitch(pitch)
                        .SetDopplerLevel(dopplerLevel)
                        .SetLoop(loop)
                        .SetClip(clips[i])
                        .SetPosition(position)
                        .SetFollowTarget(followTarget)
                        .SetSpatialSound(spatialSound)
                        .SetOcclusion(useOcclusion)
                        .SetUseScaledTime(timeMode == TimeMode.Scaled)
                        .SetEffect(effectPreset, effectIntensity)
                        .SetFadeOut(fadeOutTime)
                        .SetId(id)
                        .SetOutput(output)
                        .OnPlay(onPlay)
                        .OnComplete(onComplete)
                        .OnLoopCycleComplete(onLoopCycleComplete)
                        .OnPause(onPause)
                        .OnPauseComplete(onPauseComplete)
                        .OnResume(onResume)
                        .Play(fadeInTime);
                    continue;
                }
                
                newSoundsGoodAudioSource
                    .SetVolume(PlaybackVolume(tag))
                    .SetHearDistance(minHearDistance, maxHearDistance)
                    .SetVolumeRolloffCurve(audioRolloffMode, customVolumeCurve)
                    .SetPitch(pitch)
                    .SetDopplerLevel(dopplerLevel)
                    .SetLoop(loop)
                    .SetClip(clips[i])
                    .SetPosition(position)
                    .SetFollowTarget(followTarget)
                    .SetSpatialSound(spatialSound)
                    .SetOcclusion(useOcclusion)
                    .SetUseScaledTime(timeMode == TimeMode.Scaled)
                    .SetEffect(effectPreset, effectIntensity)
                    .SetFadeOut(fadeOutTime)
                    .SetId(id)
                    .SetOutput(output)
                    .Play(fadeInTime);
            }
        }

        /// <summary>
        /// Pause music.
        /// </summary>
        /// <param name="fadeOutTime">Seconds that fade out will last before pause</param>
        public void Pause (float fadeOutTime = 0)
        {
            if (!Using) return;
            
            foreach (var source in sourcePoolElementDictionary.Values)
            {
                source.Pause(fadeOutTime);
            }
        }

        /// <summary>
        /// Resume/Unpause music.
        /// </summary>
        /// <param name="fadeInTime">Seconds that fade in will last</param>
        public void Resume (float fadeInTime = 0)
        {
            if (!Using) return;
            
            foreach (var source in sourcePoolElementDictionary.Values)
            {
                source.Resume(fadeInTime);
            }
        }

        /// <summary>
        /// Stop music.
        /// </summary>
        /// <param name="fadeOutTime">Seconds that fade out will last before stop</param>
        public void Stop (float fadeOutTime = 0)
        {
            if (!Using) return;

            for (int i = 0; i < sourcePoolElementDictionary.Count; i++)
            {
                string tag = sourcePoolElementDictionary.ElementAt(i).Key;
                
                if (i < sourcePoolElementDictionary.Count - 1)
                {
                    sourcePoolElementDictionary[tag].Stop(fadeOutTime);
                    continue;
                }
                
                sourcePoolElementDictionary[tag].Stop(fadeOutTime, () =>
                {
                    referenceSoundsGoodAudioSource = null;
                    sourcePoolElementDictionary.Clear();
                });
            }
        }
    }

    public partial class DynamicMusic // Private Methods
    {
        /// <summary>
        /// What a track's source actually plays at: its own volume scaled by the master volume.
        /// </summary>
        private float PlaybackVolume (string tag) => volumeDictionary[tag] * masterVolume;

        private void CacheClipsTags (params Track[] tracks)
        {
            CacheClipsTags(tracks.Select(track => track.ToString()).ToArray());
        }
        
        private void CacheClipsTags (params string[] tracks)
        {
            if (!CheckClipsAreValid(tracks)) return;
            
            cachedClipsTags = new string[tracks.Length];
            int i = 0;
            foreach (string track in tracks)
            {
                cachedClipsTags[i] = track;
                i++;
            }
        }

        private bool TrySetCachedClips ()
        {
            if (clips != null && clips.Length != 0) return true;

            if (!CheckClipsAreValid(cachedClipsTags)) return false;
            
            SetClips(cachedClipsTags);
            return true;
        }

        private bool CheckClipsAreValid (string[] tracks)
        {
            bool anyTrackIsNullOrEmpty = tracks.Any(t => 
                string.IsNullOrEmpty(t) || string.Equals(t, "__NULL__"));
            bool anyTrackIsDuplicated = HasDuplicates(tracks);
            if (tracks != Array.Empty<string>() && tracks.Length != 0 && !anyTrackIsNullOrEmpty &&
                !anyTrackIsDuplicated)
                return true;
            
            Debug.LogError("Some of the music tracks you're trying to set " +
                           "to this Dynamic Music are null, empty or duplicated.");
            return false;
        }
        
        private bool HasDuplicates<T>(IEnumerable<T> array)
        {
            HashSet<T> seen = new HashSet<T>();
            return array.Any(item => !seen.Add(item));
        }
    }
}