/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using UnityEngine;

namespace MelenitasDev.SoundsGood
{
    public partial class MusicZone // Fields
    {
        private AudioEmitter emitter;
        private Transform listener;
    }

    /// <summary>
    /// Fades an <see cref="AudioEmitter"/> on the same GameObject in and out based on the listener's
    /// position inside a spherical or box zone: full volume inside, fading to silence across the
    /// fade margin, silent outside. All audio configuration (tracks, loop, output…) lives on the
    /// emitter — add a Music/Playlist/Dynamic Music Emitter next to this component. The zone's shape,
    /// weight math and gizmos come from <see cref="AudioZone"/>.
    /// </summary>
    [AddComponentMenu("Sounds Good/Music Zone", 40)]
    public partial class MusicZone : AudioZone
    {
        void Awake ()
        {
            emitter = GetComponent<AudioEmitter>();
            if (emitter == null)
                Debug.LogError("[Sounds Good] Music Zone needs an Audio Emitter " +
                               "(Music/Playlist/Dynamic Music) on the same GameObject.", this);
        }

        void Start ()
        {
            if (emitter == null) return;

            // The zone owns the volume: play muted and let Update raise it as the listener approaches.
            emitter.Play();
            emitter.SetZoneVolume(0f);
        }

        void Update ()
        {
            if (emitter == null) return;
            if (!TryGetListenerPosition(out Vector3 position)) return;

            emitter.SetZoneVolume(GetWeight(position));
        }
    }

    public partial class MusicZone // Private Methods
    {
        private bool TryGetListenerPosition (out Vector3 position)
        {
            if (listener == null && SoundsGoodManager.TryGetListener(out AudioListener found))
                listener = found.transform;

            if (listener == null && Camera.main != null)
                listener = Camera.main.transform;

            if (listener == null)
            {
                position = default;
                return false;
            }

            position = listener.position;
            return true;
        }
    }
}
