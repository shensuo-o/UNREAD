/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using UnityEngine;

namespace MelenitasDev.SoundsGood
{
    // Settings are serialized but kept private: the custom editor binds to them by name and the
    // Inspector edits them, while game code can't reach in and desync the trigger.
    public partial class CollisionSound // Properties
    {
        [SerializeField]
        [Tooltip("Only these layers can trigger a sound.")]
        private LayerMask filterLayers = ~0;
        [SerializeField]
        [Tooltip("Also require the other collider to have a specific tag.")]
        private bool useTagFilter = false;
        [SerializeField]
        private string requiredTag = "Untagged";

        [SerializeField]
        [Tooltip("Relative-velocity below which no sound plays.")]
        private float minImpact = 1f;
        [SerializeField]
        [Tooltip("Relative-velocity at or above which the sound plays at full volume.")]
        private float maxImpact = 10f;
        [SerializeField, Range(0f, 1f)]
        [Tooltip("Volume multiplier at the softest audible impact (Min Impact), scaling up to full volume at Max Impact.")]
        private float minImpactVolumeScale = 0.3f;
        [SerializeField]
        [Tooltip("Minimum seconds between two impact sounds, to avoid rattling during a long scrape.")]
        private float cooldown = 0.05f;
    }

    /// <summary>
    /// Plays the <see cref="SoundEmitter"/> on this GameObject on physical collisions, scaling its
    /// volume with the impact strength (relative velocity). All audio configuration (clip, pitch,
    /// spatialization, effect, output…) lives on the emitter — add a Sound Emitter next to this
    /// component. Requires a Collider and a Rigidbody on this object (or the one it collides with).
    /// </summary>
    [AddComponentMenu("Sounds Good/Collision Sound", 43)]
    [RequireComponent(typeof(SoundEmitter))]
    public partial class CollisionSound : MonoBehaviour
    {
        private SoundEmitter emitter;
        private float lastPlayTime = -Mathf.Infinity;

        void Awake ()
        {
            emitter = GetComponent<SoundEmitter>();
            if (emitter == null)
                Debug.LogError("[Sounds Good] Collision Sound needs a Sound Emitter " +
                               "on the same GameObject.", this);
        }

        void OnCollisionEnter (Collision collision)
        {
            if (emitter == null) return;
            if (!PassesFilter(collision.collider)) return;

            float impact = collision.relativeVelocity.magnitude;
            if (impact < minImpact) return;
            if (Time.time - lastPlayTime < cooldown) return;
            lastPlayTime = Time.time;

            // Play at the exact contact point, then scale whatever volume the emitter decided
            // (fixed or freshly re-rolled by Random Volume) down by the impact strength.
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            emitter.Sound.SetPosition(point);
            emitter.Play();

            float t = Mathf.Clamp01((impact - minImpact) / Mathf.Max(0.01f, maxImpact - minImpact));
            float baseVolume = emitter.Sound.Volume;
            emitter.Sound.ChangeVolume(Mathf.Lerp(baseVolume * minImpactVolumeScale, baseVolume, t));
        }
    }

    public partial class CollisionSound // Private Methods
    {
        private bool PassesFilter (Collider other)
        {
            if (((1 << other.gameObject.layer) & filterLayers) == 0) return false;
            if (useTagFilter && !other.CompareTag(requiredTag)) return false;
            return true;
        }
    }
}
