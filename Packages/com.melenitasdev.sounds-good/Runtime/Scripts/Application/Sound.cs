/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */
using System;
using UnityEngine;
using UnityEngine.Audio;
using MelenitasDev.SoundsGood.Domain;
using Random = UnityEngine.Random;

namespace MelenitasDev.SoundsGood
{
    public partial class Sound // Fields
    {
        private SoundsGoodAudioSource soundsGoodAudioSource;

        private float volume = 1;
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
        private bool spatialSound = true;
        //#SG_PRO_BEGIN
        private bool useOcclusion = false;
        //#SG_PRO_END
//#SG_PRO_BEGIN
        private TimeMode timeMode = TimeMode.Scaled;
//#SG_PRO_END
        // Effects are a paid feature: both the state and the API below are cut
        // from the Free edition.
//#SG_PRO_BEGIN
        private AudioEffectPreset effectPreset = null;
        private float effectIntensity = 1f;
//#SG_PRO_END
        private float fadeOutTime = 0;
        private bool randomClip = true;
        private int clipIndex = -1;
        private float playProbability = 1;
        private bool forgetSourcePoolOnStop = false;
        private AudioClip clip = null;
        private AudioMixerGroup output = null;
        private string cachedSoundTag;
    }

    public partial class Sound // Fields (Callbacks)
    {
        private Action onPlay;
        private Action onComplete;
        private Action onLoopCycleComplete;
        private Action onPause;
        private Action onPauseComplete;
        private Action onResume;
    }

    public partial class Sound // Properties
    {
        /// <summary>It's true when it's being used. When it's paused, it's true as well</summary>
        public bool Using => soundsGoodAudioSource != null;
        /// <summary>It's true when audio is playing.</summary>
        public bool Playing => Using && soundsGoodAudioSource.Playing;
        /// <summary>It's true when audio paused (it ignore the fade out time).</summary>
        public bool Paused => Using && soundsGoodAudioSource.Paused;
        /// <summary>Volume level between [0,1].</summary>
        public float Volume => Using ? soundsGoodAudioSource.Volume : volume;
        /// <summary>Pitch level.</summary>
        public float Pitch => Using ? soundsGoodAudioSource.Pitch : pitch;
        /// <summary>Clip index in Sound clips array. Returns -1 when isn't reproducing a specific clip.</summary>
        public int ClipIndex => clipIndex;
        /// <summary>Total time in seconds that it have been playing.</summary>
        public float PlayingTime => Using ? soundsGoodAudioSource.PlayingTime : 0;
        /// <summary>Reproduced time in seconds of current loop cycle.</summary>
        public float CurrentLoopCycleTime => Using ? soundsGoodAudioSource.CurrentLoopCycleTime : 0;
        /// <summary>Times it has looped.</summary>
        public int CompletedLoopCycles => Using ? soundsGoodAudioSource.CompletedLoopCycles : 0;
        /// <summary>Duration in seconds of matched clip.</summary>
        public float ClipDuration => clip != null ? clip.length : 0;
        /// <summary>Matched clip.</summary>
        public AudioClip Clip => clip;
    }

    public partial class Sound // Public Methods
    {
        /// <summary>
        /// Create new Sound object.
        /// </summary>
        public Sound () { }
        
        /// <summary>
        /// Create new Sound object given a clip.
        /// </summary>
        /// <param name="sfx">Sound you've created before on Audio Creator window</param>
        public Sound (SFX sfx)
        {
            cachedSoundTag = sfx.ToString();
        }
        
        /// <summary>
        /// Create new Sound object given a tag.
        /// </summary>
        /// <param name="tag">The tag you've used to create the sound on Audio Creator window</param>
        public Sound (string tag)
        {
            cachedSoundTag = tag;
        }

        /// <summary>
        /// Store volume parameters BEFORE play sound.
        /// </summary>
        /// <param name="volume">Volume: min 0, Max 1</param>
        public Sound SetVolume (float volume)
        {
            this.volume = volume;
            return this;
        }
        
        /// <summary>
        /// Store volume parameters BEFORE play sound.
        /// </summary>
        /// <param name="volume">Volume: min 0, Max 1</param>
        /// <param name="hearDistance">Distance range to hear sound</param>
        [Obsolete("This method has been deprecated. If you need to change the hear distance, " +
                  "use the method SetHearDistance(float minHearDistance, float maxHearDistance) instead.")]
        public Sound SetVolume (float volume, Vector2 hearDistance)
        {
            this.volume = volume;
            minHearDistance = hearDistance.x;
            maxHearDistance = hearDistance.y;
            return this;
        }
        
        /// <summary>
        /// Sets the minimum and maximum hearing distances for the AudioSource.
        /// Sounds will start to fade in at the maximum distance and be fully audible until the minimum distance is reached.
        /// </summary>
        /// <param name="minHearDistance">Distance at which the sound be fully audible.</param>
        /// <param name="maxHearDistance">Distance at which the sound starts becoming audible.</param>
        public Sound SetHearDistance (float minHearDistance, float maxHearDistance)
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
        public Sound SetVolumeRolloffCurve (VolumeRolloffCurve volumeRolloffCurve)
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
        public Sound SetCustomVolumeRolloffCurve (AnimationCurve customVolumeCurve)
        {
            audioRolloffMode = AudioRolloffMode.Custom;
            this.customVolumeCurve = customVolumeCurve;
            return this;
        }

        /// <summary>
        /// Change volume while sound is reproducing.
        /// </summary>
        /// <param name="newVolume">New volume: min 0, Max 1</param>
        /// <param name="lerpTime">Time to lerp current to new volume</param>
        public void ChangeVolume (float newVolume, float lerpTime = 0)
        {
            if (volume == newVolume) return;
            
            volume = newVolume;
            
            if (!Using) return;
            
            soundsGoodAudioSource.SetVolume(newVolume, lerpTime);
        }
        
        /// <summary>
        /// Change pitch while sound is reproducing.
        /// </summary>
        /// <param name="newPitch">New pitch multiplier.</param>
        /// <param name="lerpTime">Time to lerp current to new pitch.</param>
        public void ChangePitch (float newPitch, float lerpTime = 0)
        {
            if (pitch == newPitch) return;
            
            pitch = newPitch;
            
            if (!Using) return;
            
            soundsGoodAudioSource.SetPitch(newPitch, lerpTime);
        }

        /// <summary>
        /// Set given pitch. Make your sounds sound different :)
        /// </summary>
        public Sound SetPitch (float pitch)
        {
            this.pitch = pitch;
            return this;
        }

//#SG_PRO_BEGIN
        /// <summary>
        /// Choose whether this sound follows <c>Time.timeScale</c> (<see cref="TimeMode.Scaled"/>,
        /// the default: it speeds up and finishes sooner in fast motion, slows down in slow motion,
        /// and freezes on pause) or ignores it (<see cref="TimeMode.Unscaled"/>: it always plays at
        /// normal speed, e.g. for UI or music that must keep going while the game is paused).
        /// </summary>
        public Sound SetTimeMode (TimeMode timeMode)
        {
            this.timeMode = timeMode;
            if (Using) soundsGoodAudioSource.SetUseScaledTime(timeMode == TimeMode.Scaled);
            return this;
        }
//#SG_PRO_END
        
        /// <summary>
        /// Set my recommended random pitch. Range is (0.85, 1.15). It's useful to avoid sounds be repetitive.
        /// </summary>
        public Sound SetRandomPitch ()
        {
            pitch = Random.Range(0.85f, 1.15f);
            return this;
        }
        
        /// <summary>
        /// Set random pitch between given range. It's useful to avoid sounds be repetitive.
        /// </summary>
        /// <param name="pitchRange">Pitch range (min, Max)</param>
        public Sound SetRandomPitch (Vector2 pitchRange)
        {
            pitch = Random.Range(pitchRange.x, pitchRange.y);
            return this;
        }

        /// <summary>
        /// Set random pitch between given range. It's useful to avoid sounds be repetitive.
        /// </summary>
        /// <param name="minPitch">Minimum pitch</param>
        /// <param name="maxPitch">Maximum pitch</param>
        public Sound SetRandomPitch (float minPitch, float maxPitch)
        {
            pitch = Random.Range(minPitch, maxPitch);
            return this;
        }

        /// <summary>
        /// Set my recommended random volume. Range is (0.8, 1). It's useful to avoid sounds be repetitive.
        /// </summary>
        public Sound SetRandomVolume ()
        {
            volume = Random.Range(0.8f, 1f);
            return this;
        }

        /// <summary>
        /// Set random volume between given range. It's useful to avoid sounds be repetitive.
        /// </summary>
        /// <param name="minVolume">Minimum volume</param>
        /// <param name="maxVolume">Maximum volume</param>
        public Sound SetRandomVolume (float minVolume, float maxVolume)
        {
            volume = Random.Range(minVolume, maxVolume);
            return this;
        }

        /// <summary>
        /// Sets how strongly the Doppler effect is applied to the sound when the listener or sound source is moving.
        /// </summary>
        /// <param name="dopplerLevel">Value between 0 and 5; 1 is the default and recommended for realistic results.</param>
        public Sound SetDopplerLevel (float dopplerLevel)
        {
            this.dopplerLevel = Mathf.Clamp(dopplerLevel, 0, 5);
            return this;
        }

        /// <summary>
        /// Set an id to identify this sound on AudioManager static methods.
        /// </summary>
        public Sound SetId (string id)
        {
            this.id = id;
            return this;
        }

        /// <summary>
        /// Make your sound loops for infinite time. If you need to stop it, use Stop() method.
        /// </summary>
        public Sound SetLoop (bool loop = true)
        {
            this.loop = loop;
            return this;
        }
        
        /// <summary>
        /// Change the AudioClip of this Sound BEFORE play it.
        /// </summary>
        /// <param name="tag">The tag you've used to create the sound on Audio Creator</param>
        public Sound SetClip (string tag)
        {
            cachedSoundTag = tag;
            if (!string.Equals(tag, "__NULL__"))
            {
                clip = SoundsGoodManager.GetSFX(tag);
            }
            return this;
        }
        
        /// <summary>
        /// Change the AudioClip of this Sound BEFORE play it.
        /// </summary>
        /// <param name="sfx">Sound you've created before on Audio Creator</param>
        public Sound SetClip (SFX sfx)
        {
            SetClip(sfx.ToString());
            return this;
        }
        
        /// <summary>
        /// Make the sound clip change with each new Play().
        /// It'll choose a random sound from those you have added with the same tag in the Audio Creator.
        /// </summary>
        /// <param name="random">Use random clip</param>
        public Sound SetRandomClip (bool random = true)
        {
            randomClip = random;
            return this;
        }

        /// <summary>
        /// Set a specific clip using the index in clips you have added with the same tag in the Audio Creator.
        /// Useful to reproduce clips in a specific order.
        /// </summary>
        /// <param name="index">Index in Sound clips array that you've created in the Audio Creator</param>
        public Sound SetClipByIndex (int index)
        {
            if (index < 0)
            {
                Debug.LogWarning("Clip index can't be lower than 0");
                return this;
            }

            if (string.IsNullOrEmpty(cachedSoundTag))
            {
                Debug.LogWarning("You need to set a Sound before selecting one of its clips.");
                return this;
            }
            
            clipIndex = index;
            clip = SoundsGoodManager.GetSFX(cachedSoundTag, index);
            SetRandomClip(false);
            return this;
        }

        /// <summary>
        /// Sets the probability (0 to 1) that this sound will play when Play() is called.
        /// Useful for adding random variation (e.g., footsteps with a chance of creaking wood).
        /// </summary>
        /// <param name="playProbability">A value between 0 (never plays) and 1 (always plays).</param>
        public Sound SetPlayProbability (float playProbability)
        {
            this.playProbability = Mathf.Clamp01(playProbability);
            return this;
        }
        
        /// <summary>
        /// Set the position of the sound emitter.
        /// </summary>
        public Sound SetPosition (Vector3 position)
        {
            this.position = position;
            return this;
        }
        
        /// <summary>
        /// Set a target to follow. Audio source will update its position every frame.
        /// </summary>
        /// <param name="followTarget">Transform to follow</param>
        public Sound SetFollowTarget (Transform followTarget)
        {
            this.followTarget = followTarget;
            return this;
        }

        /// <summary>
        /// Set spatial sound.
        /// </summary>
        /// <param name="true">Your sound will be 3D</param>
        /// <param name="false">Your sound will be global / 2D</param>
        public Sound SetSpatialSound (bool activate = true)
        {
            spatialSound = activate;
            return this;
        }
        
//#SG_PRO_BEGIN
        /// <summary>
        /// Enables 3D occlusion for this audio.
        /// When enabled, the audio auto-switches to spatial mode to allow raycast-based occlusion.
        /// </summary>
        /// <param name="activate">True to enable occlusion, false to disable it</param>
        public Sound SetOcclusion (bool activate = true)
        {
            useOcclusion = activate;

            // Occlusion needs spatial (3D) sound to raycast against. Only force it when
            // occlusion is actually enabled globally, so a disabled system doesn't
            // silently turn a 2D sound into a 3D one for no effect.
            bool occlusionAvailable = AssetLocator.SoundsGoodSettings != null &&
                                      AssetLocator.SoundsGoodSettings.EnableOcclusion;
            if (activate && occlusionAvailable)
            {
                spatialSound = true;
            }
            return this;
        }
//#SG_PRO_END


//#SG_PRO_BEGIN
        /// <summary>
        /// Apply an audio effect you've created in the Effect Creator window.
        /// </summary>
        /// <param name="effect">Effect tag created in the Effect Creator</param>
        /// <param name="intensity">How much of the effect to apply: 0 none, 1 the effect
        /// exactly as you saved it. Values in between scale it down gradually.</param>
        public Sound SetEffect (Effect effect, float intensity = 1f)
        {
            effectPreset = SoundsGoodManager.GetEffect(effect);
            effectIntensity = Mathf.Clamp01(intensity);
            return this;
        }

        /// <summary>
        /// Apply an audio effect by its tag (string). Use this instead of the Effect pseudo-enum when
        /// you want the reference to survive even if that effect is later removed (e.g. demo scripts).
        /// </summary>
        /// <param name="effectTag">Effect tag created in the Effect Creator</param>
        /// <param name="intensity">How much of the effect to apply: 0 none, 1 the effect
        /// exactly as you saved it. Values in between scale it down gradually.</param>
        public Sound SetEffect (string effectTag, float intensity = 1f)
        {
            effectPreset = SoundsGoodManager.GetEffect(effectTag);
            effectIntensity = Mathf.Clamp01(intensity);
            return this;
        }

        /// <summary>
        /// Change the current effect's intensity while the sound is playing, optionally over a lerp
        /// time. Use it to fade an effect in or out at runtime.
        /// </summary>
        /// <param name="newIntensity">Target intensity: 0 none, 1 the effect exactly as saved.</param>
        /// <param name="lerpTime">Time in seconds to lerp to the new intensity (0 = instant).</param>
        public void ChangeEffect (float newIntensity, float lerpTime = 0)
        {
            effectIntensity = Mathf.Clamp01(newIntensity);
            if (!Using) return;
            soundsGoodAudioSource.ChangeEffect(effectIntensity, lerpTime);
        }

        /// <summary>
        /// Swap the effect while the sound is playing and lerp its intensity to the given value.
        /// </summary>
        /// <param name="effect">Effect tag created in the Effect Creator.</param>
        /// <param name="intensity">Target intensity: 0 none, 1 the effect exactly as saved.</param>
        /// <param name="lerpTime">Time in seconds to lerp the effect in (0 = instant).</param>
        public void ChangeEffect (Effect effect, float intensity = 1f, float lerpTime = 0)
        {
            effectPreset = SoundsGoodManager.GetEffect(effect);
            effectIntensity = Mathf.Clamp01(intensity);
            if (!Using) return;
            soundsGoodAudioSource.ChangeEffect(effectPreset, effectIntensity, lerpTime);
        }

        /// <summary>
        /// Swap the effect (by tag) while the sound is playing and lerp its intensity to the value.
        /// </summary>
        /// <param name="effectTag">Effect tag created in the Effect Creator.</param>
        /// <param name="intensity">Target intensity: 0 none, 1 the effect exactly as saved.</param>
        /// <param name="lerpTime">Time in seconds to lerp the effect in (0 = instant).</param>
        public void ChangeEffect (string effectTag, float intensity = 1f, float lerpTime = 0)
        {
            effectPreset = SoundsGoodManager.GetEffect(effectTag);
            effectIntensity = Mathf.Clamp01(intensity);
            if (!Using) return;
            soundsGoodAudioSource.ChangeEffect(effectPreset, effectIntensity, lerpTime);
        }
//#SG_PRO_END

        /// <summary>
        /// Set fade out duration. It'll be used when sound ends.
        /// </summary>
        /// <param name="fadeOutTime">Seconds that fade out will last</param>
        public Sound SetFadeOut (float fadeOutTime)
        {
            this.fadeOutTime = fadeOutTime;
            return this;
        }
        
        /// <summary>
        /// Set the audio output to manage the volume using the Audio Mixers.
        /// </summary>
        /// <param name="output">Output you've created before inside Master AudioMixer
        /// (Remember reload the outputs database on Output Manager Window)</param>
        public Sound SetOutput (Output output)
        {
            this.output = output.IsNull ? null : SoundsGoodManager.GetOutput(output);
            return this;
        }
        
        /// <summary>
        /// Define a callback that will be invoked on sound start playing.
        /// </summary>
        /// <param name="onPlay">Method will be invoked</param>
        public Sound OnPlay (Action onPlay)
        {
            this.onPlay = onPlay;
            return this;
        }
        
        /// <summary>
        /// Define a callback that will be invoked on sound complete.
        /// If "loop" is active, it'll be called when you Stop the sound manually.
        /// </summary>
        /// <param name="onComplete">Method will be invoked</param>
        public Sound OnComplete (Action onComplete)
        {
            this.onComplete = onComplete;
            return this;
        }
        
        /// <summary>
        /// Define a callback that will be invoked on loop cycle complete.
        /// You need to set loop on true to use it.
        /// </summary>
        /// <param name="onLoopCycleComplete">Method will be invoked</param>
        public Sound OnLoopCycleComplete (Action onLoopCycleComplete)
        {
            this.onLoopCycleComplete = onLoopCycleComplete;
            return this;
        }
        
        /// <summary>
        /// Define a callback that will be invoked on sound pause.
        /// It will ignore the fade out time.
        /// </summary>
        /// <param name="onPause">Method will be invoked</param>
        public Sound OnPause (Action onPause)
        {
            this.onPause = onPause;
            return this;
        }
        
        /// <summary>
        /// Define a callback that will be invoked on sound pause and fade out ends.
        /// </summary>
        /// <param name="onPauseComplete">Method will be invoked</param>
        public Sound OnPauseComplete (Action onPauseComplete)
        {
            this.onPauseComplete = onPauseComplete;
            return this;
        }
        
        /// <summary>
        /// Define a callback that will be invoked on resume/unpause sound.
        /// </summary>
        /// <param name="onResume">Method will be invoked</param>
        public Sound OnResume (Action onResume)
        {
            this.onResume = onResume;
            return this;
        }

        /// <summary>
        /// Reproduce sound.
        /// </summary>
        /// <param name="fadeInTime">Seconds that fade in will last</param>
        public void Play (float fadeInTime = 0)
        {
            if (clip == null && string.IsNullOrEmpty(cachedSoundTag))
            {
                Debug.LogError("You need to set a clip before reproduce this");
                return;
            }

            if (string.Equals(cachedSoundTag, "__NULL__"))
            {
                return;
            }
            
            if (Using && Playing && loop)
            {
                Stop();
                forgetSourcePoolOnStop = true;
            }
            
            if (randomClip || clip == null)
            {
                SetClip(cachedSoundTag);
            }
            else
            {
                if (clipIndex != -1)
                {
                    SetClipByIndex(clipIndex);
                }
            }
            
            if (Random.value > playProbability) return;

            soundsGoodAudioSource = SoundsGoodManager.GetSource();
            soundsGoodAudioSource
                .SetVolume(volume)
                .SetHearDistance(minHearDistance, maxHearDistance)
                .SetVolumeRolloffCurve(audioRolloffMode, customVolumeCurve)
                .SetPitch(pitch)
                .SetDopplerLevel(dopplerLevel)
                .SetLoop(loop)
                .SetClip(clip)
                .SetPosition(position)
                .SetFollowTarget(followTarget)
                .SetSpatialSound(spatialSound)
//#SG_PRO_BEGIN
                .SetOcclusion(useOcclusion)
//#SG_PRO_END
//#SG_PRO_BEGIN
                .SetUseScaledTime(timeMode == TimeMode.Scaled)
//#SG_PRO_END
                // Effect link in the chain, Pro only.
//#SG_PRO_BEGIN
                .SetEffect(effectPreset, effectIntensity)
//#SG_PRO_END
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
        }

        /// <summary>
        /// Pause sound.
        /// </summary>
        /// <param name="fadeOutTime">Seconds that fade out will last before pause</param>
        public void Pause (float fadeOutTime = 0)
        {
            if (!Using) return;
            
            soundsGoodAudioSource.Pause(fadeOutTime);
        }

        /// <summary>
        /// Resume/Unpause sound.
        /// </summary>
        /// <param name="fadeInTime">Seconds that fade in will last</param>
        public void Resume (float fadeInTime = 0)
        {
            if (!Using) return;
            
            soundsGoodAudioSource.Resume(fadeInTime);
        }

        /// <summary>
        /// Stop sound.
        /// </summary>
        /// <param name="fadeOutTime">Seconds that fade out will last before stop</param>
        public void Stop (float fadeOutTime = 0)
        {
            if (!Using) return;
            
            if (forgetSourcePoolOnStop)
            {
                soundsGoodAudioSource.Stop(fadeOutTime);
                soundsGoodAudioSource = null;
                forgetSourcePoolOnStop = false;
                return;
            }
            soundsGoodAudioSource.Stop(fadeOutTime, () => soundsGoodAudioSource = null);
        }
    }
}