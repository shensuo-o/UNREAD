/*
 * All rights to the Sounds Good plugin, © Created by Melenitas Dev, are reserved.
 * Distribution of the standalone asset is strictly prohibited.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MelenitasDev.SoundsGood
{
    public partial class RandomAmbience // Fields
    {
        private Coroutine ambienceRoutine;
    }

    // Settings are serialized but kept private: the custom editor binds to them by name and the
    // Inspector edits them, while game code can't reach in and desync the ambience.
    public partial class RandomAmbience // Properties
    {
        public enum EmissionMode { FixedAtTransform, RandomWithinRadius, FollowTransform }

        [SerializeField] private List<SFX> sounds = new List<SFX>();

        [SerializeField] private float minInterval = 3f;
        [SerializeField] private float maxInterval = 10f;
        [SerializeField] private bool playImmediatelyOnEnable = false;

        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private bool randomVolume = false;
        [SerializeField] private Vector2 volumeRange = new Vector2(0.8f, 1f);
        [SerializeField] private bool randomPitch = true;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.85f, 1.15f);
        [SerializeField] private Output output = default;

        [SerializeField] private bool spatialSound = true;
        [SerializeField] private bool useOcclusion = false;
        [SerializeField] private TimeMode timeMode = TimeMode.Scaled;
        [SerializeField, Range(0f, 5f)] private float dopplerLevel = 1f;
        [SerializeField] private float hearDistanceMin = 3f;
        [SerializeField] private float hearDistanceMax = 500f;
        [SerializeField] private ComponentRolloff volumeRolloff = ComponentRolloff.Logarithmic;
        [SerializeField] private AnimationCurve customVolumeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        [SerializeField, Range(-3f, 3f)] private float pitch = 1f;
        [SerializeField] private Effect effect = default;
        [SerializeField, Range(0f, 1f)] private float effectIntensity = 1f;

        [SerializeField] private EmissionMode emissionMode = EmissionMode.FixedAtTransform;
        [SerializeField] private float emissionRadius = 5f;
        [SerializeField] private Color areaColor = new Color(0.992f, 0.694f, 0.012f);
    }

    [AddComponentMenu("Sounds Good/Random Ambience", 42)]
    public partial class RandomAmbience : MonoBehaviour
    {
        void OnEnable ()
        {
            Play();
        }

        void OnDisable ()
        {
            Stop();
        }

        void OnDrawGizmosSelected ()
        {
            if (!spatialSound || emissionMode != EmissionMode.RandomWithinRadius) return;

            Gizmos.color = areaColor;
            Gizmos.DrawWireSphere(transform.position, emissionRadius);
        }
    }

    public partial class RandomAmbience // Public Methods
    {
        public void Play ()
        {
            if (ambienceRoutine != null) return;

            ambienceRoutine = StartCoroutine(AmbienceRoutine());
        }

        public void Stop ()
        {
            if (ambienceRoutine == null) return;

            StopCoroutine(ambienceRoutine);
            ambienceRoutine = null;
        }
    }

    public partial class RandomAmbience // Private Methods
    {
        private IEnumerator AmbienceRoutine ()
        {
            if (!playImmediatelyOnEnable)
                yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

            while (true)
            {
                PlayRandomSound();
                yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            }
        }

        /// <summary>
        /// Plays one random sound from the configured list right now. Exposed for the editor's test button.
        /// </summary>
        internal void PlayRandomSound ()
        {
            if (sounds.Count == 0) return;

            SFX chosen = sounds[Random.Range(0, sounds.Count)];
            Sound sound = new Sound(chosen)
                .SetOutput(output)
                .SetSpatialSound(spatialSound)
                .SetOcclusion(useOcclusion)
                .SetTimeMode(timeMode)
                .SetDopplerLevel(dopplerLevel);

            if (randomVolume) sound.SetRandomVolume(volumeRange.x, volumeRange.y);
            else sound.SetVolume(volume);

            if (randomPitch) sound.SetRandomPitch(pitchRange);
            else sound.SetPitch(pitch);
            ComponentAudioSettings.Apply(sound, hearDistanceMin, hearDistanceMax, volumeRolloff, customVolumeCurve, effect, effectIntensity);

            if (emissionMode == EmissionMode.FollowTransform)
            {
                sound.SetFollowTarget(transform);
            }
            else
            {
                Vector3 position = emissionMode == EmissionMode.RandomWithinRadius
                    ? transform.position + Random.insideUnitSphere * emissionRadius
                    : transform.position;
                sound.SetPosition(position);
            }

            sound.Play();
        }
    }
}
