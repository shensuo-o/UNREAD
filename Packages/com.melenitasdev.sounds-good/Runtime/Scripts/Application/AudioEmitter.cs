/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using UnityEngine;

namespace MelenitasDev.SoundsGood
{
    /// <summary>
    /// Common contract for every emitter (Sound/Music/Playlist/Dynamic Music). Components that
    /// only need to drive playback — such as <see cref="AudioTrigger"/> and <see cref="MusicZone"/> —
    /// reference this base instead of duplicating each emitter's configuration and build logic.
    /// </summary>
    public abstract class AudioEmitter : MonoBehaviour
    {
        /// <summary>Plays the emitter's audio with its configured fade in.</summary>
        public abstract void Play ();

        /// <summary>Stops the emitter's audio with its configured fade out.</summary>
        public abstract void Stop ();

        /// <summary>Pauses the emitter's audio with its configured fade out.</summary>
        public abstract void Pause ();

        /// <summary>Resumes the emitter's audio with its configured fade in.</summary>
        public abstract void Resume ();

        /// <summary>Whether the emitter is currently using an audio source (playing or paused).</summary>
        public abstract bool IsPlaying { get; }

        /// <summary>
        /// Scales the emitter's configured volume by <paramref name="factor"/> (0..1). Used by a
        /// <see cref="MusicZone"/> to fade the whole mix by the listener's distance. Emitters that
        /// don't support live volume control (e.g. a one-shot sound) leave this as a no-op.
        /// </summary>
        public virtual void SetZoneVolume (float factor, float fadeTime = 0f) { }
    }
}
