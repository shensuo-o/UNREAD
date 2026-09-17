/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using MelenitasDev.SoundsGood.SystemUtilities;
using MelenitasDev.SoundsGood.Domain;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace MelenitasDev.SoundsGood
{
    public partial class Playlist // Fields
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
        private bool spatialSound = false;
        //#SG_PRO_BEGIN
        private bool useOcclusion = false;
        //#SG_PRO_END
//#SG_PRO_BEGIN
        private TimeMode timeMode = TimeMode.Unscaled;
//#SG_PRO_END
        // Effects are a paid feature: both the state and the API below are cut
        // from the Free edition.
//#SG_PRO_BEGIN
        private AudioEffectPreset effectPreset = null;
        private float effectIntensity = 1f;
//#SG_PRO_END
        private float fadeOutTime = 0;
        private float fadeInTime = 0;
        private Queue<AudioClip> playlist = new Queue<AudioClip>();
        private Queue<string> cachedPlaylistTags = new Queue<string>();
        private AudioMixerGroup output = null;
    }

    public partial class Playlist // Fields (Callbacks)
    {
        private Action onPlay;
        private Action onComplete;
        private Action onLoopCycleComplete;
        private Action onNextTrackStart;
        private Action onPause;
        private Action onPauseComplete;
        private Action onResume;
    }

    public partial class Playlist // Properties
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
        /// <summary>Total time in seconds that it have been playing.</summary>
        public float PlayingTime => Using ? soundsGoodAudioSource.PlayingTime : 0;
        /// <summary>Reproduced time in seconds of current loop cycle.</summary>
        public float CurrentLoopCycleTime => Using ? soundsGoodAudioSource.CurrentLoopCycleTime : 0;
        /// <summary>Times it has looped.</summary>
        public int CompletedLoopCycles => Using ? soundsGoodAudioSource.CompletedLoopCycles : 0;
        /// <summary>Duration in seconds of current playing clip.</summary>
        public float CurrentClipDuration => Using ? soundsGoodAudioSource.CurrentClipDuration : 0;
        /// <summary>Total duration in seconds of entire playlist.</summary>
        public float PlayListDuration
        {
            get
            {
                float playlistDuration = 0;
                foreach (AudioClip clip in playlist) playlistDuration += clip.length;
                return playlistDuration;
            }
        }
        /// <summary>Reproduced tracks in this playlist.</summary>
        public float ReproducedTracks => Using ? soundsGoodAudioSource.ReproducedTracks : 0;
        /// <summary>Current clip that is playing.</summary>
        public AudioClip CurrentPlaylistClip => soundsGoodAudioSource.CurrentClip;
        /// <summary>The next clip of current playlist.</summary>
        public AudioClip NextPlaylistClip => soundsGoodAudioSource.NextPlaylistClip;
        /// <summary>The clips tags in the current playlist in playback order.</summary>
        public string[] PlaylistClipsTags => cachedPlaylistTags.ToArray();
    }

    public partial class Playlist // Public Methods
    {
        /// <summary>
        /// Create new Playlist object.
        /// </summary>
        public Playlist () { }
        
        /// <summary>
        /// Create new Playlist object given a Tracks array.
        /// </summary>
        /// <param name="playlistTracks">Track array with all music tracks that you want to reproduce in order</param>
        public Playlist (params Track[] playlistTracks)
        {
            cachedPlaylistTags.Clear();
            foreach (Track track in playlistTracks)
                cachedPlaylistTags.Enqueue(track.ToString());
        }
        
        /// <summary>
        /// Create new Playlist object given a tags array.
        /// </summary>
        /// <param name="playlistTags">A music tracks tags array that you want to reproduce in order</param>
        public Playlist (params string[] playlistTags)
        {
            cachedPlaylistTags.Clear();
            foreach (string track in playlistTags)
                cachedPlaylistTags.Enqueue(track);
        }

        /// <summary>
        /// Store volume parameters BEFORE play playlist.
        /// </summary>
        /// <param name="volume">Volume: min 0, Max 1</param>
        public Playlist SetVolume (float volume)
        {
            this.volume = volume;
            return this;
        }
        
        /// <summary>
        /// Store volume parameters BEFORE play playlist.
        /// </summary>
        /// <param name="volume">Volume: min 0, Max 1</param>
        /// <param name="hearDistance">min and Max distance to hear music</param>
        [Obsolete("This method has been deprecated. If you need to change the hear distance, " +
                  "use the method SetHearDistance(float minHearDistance, float maxHearDistance) instead.")]
        public Playlist SetVolume (float volume, Vector2 hearDistance)
        {
            this.volume = volume;
            minHearDistance = hearDistance.x;
            maxHearDistance = hearDistance.y;
            return this;
        }
        
        /// <summary>
        /// Set my recommended random volume. Range is (0.8, 1). It's useful to avoid the playlist be repetitive.
        /// </summary>
        public Playlist SetRandomVolume ()
        {
            volume = Random.Range(0.8f, 1f);
            return this;
        }

        /// <summary>
        /// Set random volume between given range. It's useful to avoid the playlist be repetitive.
        /// </summary>
        /// <param name="minVolume">Minimum volume</param>
        /// <param name="maxVolume">Maximum volume</param>
        public Playlist SetRandomVolume (float minVolume, float maxVolume)
        {
            volume = Random.Range(minVolume, maxVolume);
            return this;
        }

        /// <summary>
        /// Sets the minimum and maximum hearing distances for the AudioSource.
        /// Music will start to fade in at the maximum distance and be fully audible until the minimum distance is reached.
        /// </summary>
        /// <param name="minHearDistance">Distance at which the music be fully audible.</param>
        /// <param name="maxHearDistance">Distance at which the music starts becoming audible.</param>
        public Playlist SetHearDistance (float minHearDistance, float maxHearDistance)
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
        public Playlist SetVolumeRolloffCurve (VolumeRolloffCurve volumeRolloffCurve)
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
        public Playlist SetCustomVolumeRolloffCurve (AnimationCurve customVolumeCurve)
        {
            audioRolloffMode = AudioRolloffMode.Custom;
            this.customVolumeCurve = customVolumeCurve;
            return this;
        }
        
        /// <summary>
        /// Change volume while music is reproducing.
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
        /// Change pitch while playlist is reproducing.
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
        /// Set given pitch. Make your music sound different :)
        /// </summary>
        public Playlist SetPitch (float pitch)
        {
            this.pitch = pitch;
            return this;
        }

//#SG_PRO_BEGIN
        /// <summary>
        /// Choose whether this playlist follows <c>Time.timeScale</c> (<see cref="TimeMode.Scaled"/>)
        /// or ignores it (<see cref="TimeMode.Unscaled"/>, the default: it keeps playing at normal
        /// speed while paused or in slow/fast motion).
        /// </summary>
        public Playlist SetTimeMode (TimeMode timeMode)
        {
            this.timeMode = timeMode;
            if (Using) soundsGoodAudioSource.SetUseScaledTime(timeMode == TimeMode.Scaled);
            return this;
        }
//#SG_PRO_END
        
        /// <summary>
        /// Sets how strongly the Doppler effect is applied to the music when the listener or sound source is moving.
        /// </summary>
        /// <param name="dopplerLevel">Value between 0 and 5; 1 is the default and recommended for realistic results.</param>
        public Playlist SetDopplerLevel (float dopplerLevel)
        {
            this.dopplerLevel = Mathf.Clamp(dopplerLevel, 0, 5);
            return this;
        }

        /// <summary>
        /// Set an id to identify this music on AudioManager static methods.
        /// </summary>
        public Playlist SetId (string id)
        {
            this.id = id;
            return this;
        }
        
        /// <summary>
        /// Make your playlist loops for infinite time. If you need to stop it, use Stop() method.
        /// </summary>
        public Playlist SetLoop (bool loop = true)
        {
            this.loop = loop;
            return this;
        }

        /// <summary>
        /// Set a new playlist BEFORE play it.
        /// </summary>
        /// <param name="playlistTracks">A music tracks array in order</param>
        public Playlist SetPlaylist (params Track[] playlistTracks)
        {
            return SetPlaylist(playlistTracks.Select(track => track.ToString()).ToArray());
        }
        
        /// <summary>
        /// Set a new playlist BEFORE play it.
        /// </summary>
        /// <param name="playlistTags">A music tracks tags array in order</param>
        public Playlist SetPlaylist (params string[] playlistTags)
        {
            if (!CheckClipsAreValid(playlistTags)) return this;
    
            playlist.Clear();
            cachedPlaylistTags.Clear();
            foreach (string tag in playlistTags)
            {
                cachedPlaylistTags.Enqueue(tag);
                playlist.Enqueue(SoundsGoodManager.GetTrack(tag));
            }
            return this;
        }
        
        /// <summary>
        /// Enqueue a new track to the existing playlist.
        /// </summary>
        /// <param name="addedTrack">The new track you want to add at the end of the playlist</param>
        public void AddToPlaylist (Track addedTrack)
        {
            AddToPlaylist(addedTrack.ToString());
        }
        
        /// <summary>
        /// Enqueue a new track to the existing playlist.
        /// </summary>
        /// <param name="addedTrackTag">The new track's tag you want to add at the end of the playlist</param>
        public void AddToPlaylist (string addedTrackTag)
        {
            playlist.Enqueue(SoundsGoodManager.GetTrack(addedTrackTag));
            soundsGoodAudioSource.AddToPlaylist(SoundsGoodManager.GetTrack(addedTrackTag));
        }
        
        /// <summary>
        /// Shuffles the clips in this playlist to play them in random order.
        /// </summary>
        public void Shuffle ()
        {
            if (!TrySetCachedClips()) return;

            cachedPlaylistTags.Shuffle();
            SetPlaylist(cachedPlaylistTags.ToArray());
            
            if (!Using) return;

            soundsGoodAudioSource.SetPlaylist(playlist);
        }

        /// <summary>
        /// Set the position of the sound emitter.
        /// </summary>
        public Playlist SetPosition (Vector3 position)
        {
            this.position = position;
            return this;
        }
        
        /// <summary>
        /// Set a target to follow. Audio source will update its position every frame.
        /// </summary>
        /// <param name="followTarget">Transform to follow. Null to follow Main Camera transform.</param>
        public Playlist SetFollowTarget (Transform followTarget)
        {
            this.followTarget = followTarget;
            return this;
        }
        
        /// <summary>
        /// Set spatial sound.
        /// </summary>
        /// <param name="true">Your sound will be 3D</param>
        /// <param name="false">Your sound will be global / 2D</param>
        public Playlist SetSpatialSound (bool activate = true)
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
        public Playlist SetOcclusion (bool activate = true)
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
//#SG_PRO_END


//#SG_PRO_BEGIN
        /// <summary>
        /// Apply an audio effect you've created in the Effect Creator window.
        /// </summary>
        /// <param name="effect">Effect tag created in the Effect Creator</param>
        /// <param name="intensity">How much of the effect to apply: 0 none, 1 the effect
        /// exactly as you saved it. Values in between scale it down gradually.</param>
        public Playlist SetEffect (Effect effect, float intensity = 1f)
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
        public Playlist SetEffect (string effectTag, float intensity = 1f)
        {
            effectPreset = SoundsGoodManager.GetEffect(effectTag);
            effectIntensity = Mathf.Clamp01(intensity);
            return this;
        }

        /// <summary>
        /// Change the current effect's intensity while the playlist is playing, optionally over a
        /// lerp time. Use it to fade an effect in or out at runtime.
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
        /// Swap the effect while the playlist is playing and lerp its intensity to the given value.
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
        /// Swap the effect (by tag) while the playlist is playing and lerp its intensity to the value.
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
        /// Set fade out duration for all tracks.
        /// </summary>
        /// <param name="fadeOutTime">Seconds that fade out will last</param>
        public Playlist SetFadeOut (float fadeOutTime)
        {
            this.fadeOutTime = fadeOutTime;
            return this;
        }
        
        /// <summary>
        /// Set fade in duration for all tracks.
        /// </summary>
        /// <param name="fadeInTime">Seconds that fade in will last</param>
        public Playlist SetFadeIn (float fadeInTime)
        {
            this.fadeInTime = fadeInTime;
            return this;
        }
        
        /// <summary>
        /// Set the audio output to manage the volume using the Audio Mixers.
        /// </summary>
        /// <param name="output">Output you've created before inside Master AudioMixer
        /// (Remember reload the outputs database on Output Manager Window)</param>
        public Playlist SetOutput (Output output)
        {
            this.output = output.IsNull ? null : SoundsGoodManager.GetOutput(output);
            return this;
        }
        
        /// <summary>
        /// Define a callback that will be invoked on playlist starts.
        /// </summary>
        /// <param name="onPlay">Method will be invoked</param>
        public Playlist OnPlay (Action onPlay)
        {
            this.onPlay = onPlay;
            return this;
        }
        
        /// <summary>
        /// Define a callback that will be invoked on playlist complete.
        /// If "loop" is active, it'll be called when you Stop the playlist manually.
        /// </summary>
        /// <param name="onComplete">Method will be invoked</param>
        public Playlist OnComplete (Action onComplete)
        {
            this.onComplete = onComplete;
            return this;
        }
        
        /// <summary>
        /// Define a callback that will be invoked on playlist loop cycle complete.
        /// You need to set loop on true to use it.
        /// </summary>
        /// <param name="onLoopCycleComplete">Method will be invoked</param>
        public Playlist OnLoopCycleComplete (Action onLoopCycleComplete)
        {
            this.onLoopCycleComplete = onLoopCycleComplete;
            return this;
        }
        
        /// <summary>
        /// Define a callback that will be invoked on finish track and start the next one.
        /// </summary>
        /// <param name="onNextTrackStart">Method will be invoked</param>
        public Playlist OnNextTrackStart (Action onNextTrackStart)
        {
            this.onNextTrackStart = onNextTrackStart;
            return this;
        }
        
        /// <summary>
        /// Define a callback that will be invoked on playlist pause.
        /// It will ignore the fade out time.
        /// </summary>
        /// <param name="onPause">Method will be invoked</param>
        public Playlist OnPause (Action onPause)
        {
            this.onPause = onPause;
            return this;
        }
        
        /// <summary>
        /// Define a callback that will be invoked on playlist pause and fade out ends.
        /// </summary>
        /// <param name="onPauseComplete">Method will be invoked</param>
        public Playlist OnPauseComplete (Action onPauseComplete)
        {
            this.onPauseComplete = onPauseComplete;
            return this;
        }
        
        /// <summary>
        /// Define a callback that will be invoked on resume/unpause playlist.
        /// </summary>
        /// <param name="onResume">Method will be invoked</param>
        public Playlist OnResume (Action onResume)
        {
            this.onResume = onResume;
            return this;
        }

        /// <summary>
        /// Reproduce playlist.
        /// </summary>
        public void Play ()
        {
            if (!TrySetCachedClips()) return;
            
            if (Using && Playing)
            {
                Stop();
            }

            soundsGoodAudioSource = SoundsGoodManager.GetSource();
            soundsGoodAudioSource
                .MarkAsPlaylist()
                .SetVolume(volume)
                .SetHearDistance(minHearDistance, maxHearDistance)
                .SetVolumeRolloffCurve(audioRolloffMode, customVolumeCurve)
                .SetPitch(pitch)
                .SetDopplerLevel(dopplerLevel)
                .SetLoop(loop)
                .SetPlaylist(playlist)
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
                .SetFadeIn(fadeInTime)
                .SetFadeOut(fadeOutTime)
                .SetId(id)
                .SetOutput(output)
                .OnPlay(onPlay)
                .OnComplete(onComplete)
                .OnLoopCycleComplete(onLoopCycleComplete)
                .OnNextTrackStart(onNextTrackStart)
                .OnPause(onPause)
                .OnPauseComplete(onPauseComplete)
                .OnResume(onResume)
                .PlayPlaylist(fadeInTime);
        }

        /// <summary>
        /// Pause playlist.
        /// </summary>
        /// <param name="fadeOutTime">Seconds that fade out will last before pause</param>
        public void Pause (float fadeOutTime = 0)
        {
            if (!Using) return;
            
            soundsGoodAudioSource.Pause(fadeOutTime);
        }

        /// <summary>
        /// Resume/Unpause playlist.
        /// </summary>
        /// <param name="fadeInTime">Seconds that fade in will last</param>
        public void Resume (float fadeInTime = 0)
        {
            if (!Using) return;
            
            soundsGoodAudioSource.Resume(fadeInTime);
        }

        /// <summary>
        /// Stop playlist.
        /// </summary>
        /// <param name="fadeOutTime">Seconds that fade out will last before stop</param>
        public void Stop (float fadeOutTime = 0)
        {
            if (!Using) return;
            
            soundsGoodAudioSource.Stop(fadeOutTime, () => soundsGoodAudioSource = null);
        }
        
        // ----- Private Methods
        private bool TrySetCachedClips ()
        {
            if (playlist is { Count: > 0 }) return true;

            if (!CheckClipsAreValid(cachedPlaylistTags.ToArray())) return false;
            
            SetPlaylist(cachedPlaylistTags.ToArray());
            return true;
        }
        
        private bool CheckClipsAreValid (string[] tracks)
        {
            bool anyTrackIsNullOrEmpty = tracks.Any(t => 
                string.IsNullOrEmpty(t) || string.Equals(t, "__NULL__"));
            
            if (tracks != Array.Empty<string>() && tracks.Length > 0 && !anyTrackIsNullOrEmpty) return true;
            
            Debug.LogError("Some of the music tracks you're trying to set to this Playlist are null or empty.");
            return false;
        }
    }
}