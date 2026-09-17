/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using UnityEngine;

namespace MelenitasDev.SoundsGood
{
    public partial class AudioTrigger // Fields
    {
        private AudioEmitter emitter;

        private float lastPlayTime = -Mathf.Infinity;
        private bool hasPlayedOnce;
    }

    // Settings are serialized but kept private: the custom editor binds to them by name and the
    // Inspector edits them, while game code drives the trigger only through Play/Stop/Toggle.
    public partial class AudioTrigger // Properties
    {
        public enum TriggerAction { Play, Stop, TogglePlayStop }

        [SerializeField]
        [Tooltip("Fire when this component is enabled.")]
        private bool triggerOnEnable = false;
        [SerializeField]
        private TriggerAction actionOnEnable = TriggerAction.Play;

        [SerializeField]
        [Tooltip("Fire when the scene starts (on Start).")]
        private bool triggerOnStart = false;
        [SerializeField]
        private TriggerAction actionOnStart = TriggerAction.Play;

        [SerializeField]
        [Tooltip("Fire when this object is destroyed.")]
        private bool triggerOnDestroy = false;
        [SerializeField]
        private TriggerAction actionOnDestroy = TriggerAction.Stop;

        [SerializeField]
        [Tooltip("Fire on a physical collision enter (needs Colliders + a Rigidbody).")]
        private bool triggerOnCollisionEnter = false;
        [SerializeField]
        private TriggerAction actionOnCollisionEnter = TriggerAction.Play;

        [SerializeField]
        [Tooltip("Fire when a physical collision ends.")]
        private bool triggerOnCollisionExit = false;
        [SerializeField]
        private TriggerAction actionOnCollisionExit = TriggerAction.Stop;

        [SerializeField]
        [Tooltip("Fire when another collider enters this trigger (needs an 'Is Trigger' Collider).")]
        private bool triggerOnTriggerEnter = false;
        [SerializeField]
        private TriggerAction actionOnTriggerEnter = TriggerAction.Play;

        [SerializeField]
        [Tooltip("Fire when another collider leaves this trigger.")]
        private bool triggerOnTriggerExit = false;
        [SerializeField]
        private TriggerAction actionOnTriggerExit = TriggerAction.Stop;

        [SerializeField]
        [Tooltip("Only these layers can fire the physics triggers.")]
        private LayerMask filterLayers = ~0;
        [SerializeField]
        [Tooltip("Also require the other collider to have a specific tag.")]
        private bool useTagFilter = false;
        [SerializeField]
        private string requiredTag = "Player";

        [SerializeField]
        [Tooltip("Minimum seconds between two plays. 0 = no limit.")]
        private float cooldown = 0f;
        [SerializeField]
        [Tooltip("Play only the first time; further plays are ignored (Stop/Toggle still work).")]
        private bool playOnce = false;
    }

    /// <summary>
    /// Drives an <see cref="AudioEmitter"/> on the same GameObject from physics, lifecycle or manual
    /// events. All audio configuration lives on the emitter; this component only decides *when* to
    /// Play/Stop/Toggle it. Add a Sound/Music/Playlist/Dynamic Music Emitter next to this component.
    /// </summary>
    [AddComponentMenu("Sounds Good/Audio Trigger", 41)]
    public partial class AudioTrigger : MonoBehaviour
    {
        void Awake ()
        {
            emitter = GetComponent<AudioEmitter>();
            if (emitter == null)
                Debug.LogError("[Sounds Good] Audio Trigger needs an Audio Emitter " +
                               "(Sound/Music/Playlist/Dynamic Music) on the same GameObject.", this);
        }

        void OnEnable ()  { HandleAction(triggerOnEnable, actionOnEnable); }
        void Start ()     { HandleAction(triggerOnStart, actionOnStart); }
        void OnDestroy () { HandleAction(triggerOnDestroy, actionOnDestroy); }

        void OnCollisionEnter (Collision collision)
        {
            if (triggerOnCollisionEnter && PassesFilter(collision.collider)) HandleAction(true, actionOnCollisionEnter);
        }

        void OnCollisionExit (Collision collision)
        {
            if (triggerOnCollisionExit && PassesFilter(collision.collider)) HandleAction(true, actionOnCollisionExit);
        }

        void OnTriggerEnter (Collider other)
        {
            if (triggerOnTriggerEnter && PassesFilter(other)) HandleAction(true, actionOnTriggerEnter);
        }

        void OnTriggerExit (Collider other)
        {
            if (triggerOnTriggerExit && PassesFilter(other)) HandleAction(true, actionOnTriggerExit);
        }

        /// <summary>
        /// Plays the emitter. Safe to hook from UI buttons, Animation Events or other scripts.
        /// </summary>
        public void Play () => TriggerPlay();

        /// <summary>
        /// Stops the emitter. Safe to hook from UI buttons, Animation Events or other scripts.
        /// </summary>
        public void Stop () => TriggerStop();

        /// <summary>
        /// Plays if not currently playing, stops otherwise.
        /// </summary>
        public void Toggle ()
        {
            if (emitter != null && emitter.IsPlaying) TriggerStop();
            else TriggerPlay();
        }
    }

    public partial class AudioTrigger // Private Methods
    {
        private void HandleAction (bool enabledFlag, TriggerAction action)
        {
            if (!enabledFlag) return;

            switch (action)
            {
                case TriggerAction.Play: TriggerPlay(); break;
                case TriggerAction.Stop: TriggerStop(); break;
                case TriggerAction.TogglePlayStop: Toggle(); break;
            }
        }

        private void TriggerPlay ()
        {
            if (emitter == null) return;
            // playOnce only blocks further Play calls; Stop/Toggle triggered afterwards still work.
            if (playOnce && hasPlayedOnce) return;
            if (Time.time - lastPlayTime < cooldown) return;

            lastPlayTime = Time.time;
            hasPlayedOnce = true;

            emitter.Play();
        }

        private void TriggerStop ()
        {
            if (emitter != null) emitter.Stop();
        }

        private bool PassesFilter (Collider other)
        {
            if (((1 << other.gameObject.layer) & filterLayers) == 0) return false;
            if (useTagFilter && !other.CompareTag(requiredTag)) return false;
            return true;
        }
    }
}
