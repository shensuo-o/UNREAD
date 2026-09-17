using System;
using System.Collections;
using UnityEngine;

namespace MelenitasDev.SoundsGood.Demo
{
    /// <summary>
    /// Drops a set of cubes from a random height so they hit the floor at different speeds, showing
    /// the Collision Sound component scale each impact's volume with how hard it lands.
    /// Every cube needs its own Collider, Rigidbody, Sound Emitter and Collision Sound: this script
    /// only throws them, the sound is entirely the component's doing.
    /// </summary>
    public class SG_CollisionSoundShowcase : MonoBehaviour, IShowcaseResettable
    {
        [Serializable]
        private class FallingCube
        {
            [SerializeField]
            [Tooltip("The cube to drop. Needs a Collider, a Rigidbody, a Sound Emitter and a Collision Sound.")]
            private Rigidbody body;
            [SerializeField, Min(0.01f)]
            [Tooltip("Mass applied when it's dropped. Changes how it shoves the other cubes around, " +
                     "not how loud its impact is.")]
            private float mass = 1f;
            [SerializeField, Min(0f)]
            [Tooltip("Extra downward speed at the moment it's released, on top of the fall itself. " +
                     "This is what makes one cube land louder than another.")]
            private float extraDropSpeed = 0f;

            public Rigidbody Body => body;
            public float Mass => mass;
            public float ExtraDropSpeed => extraDropSpeed;
        }

        // ----- Serialized Fields
        [SerializeField] private FallingCube[] cubes = new FallingCube[4];
        [SerializeField]
        [Tooltip("Height above its resting position each cube is lifted to before being released. " +
                 "A random value in this range is picked per cube, on every drop.")]
        private Vector2 dropHeightRange = new Vector2(4f, 8f);
        [SerializeField, Min(0f)]
        [Tooltip("Seconds between one cube being released and the next. Without a gap the impacts " +
                 "pile into a single noise instead of four distinct sounds.")]
        private float dropInterval = 0.35f;

        // ----- Fields
        private Vector3[] restPositions;
        private Quaternion[] restRotations;
        private Coroutine dropRoutine;
        private bool initialized;

        // ----- Unity Events
        void Awake ()
        {
            Initialize();
        }

        // ----- Public Methods
        /// <summary> Hook this up to the button's On Pressed event. </summary>
        public void DropCubes ()
        {
            Initialize();

            if (dropRoutine != null)
            {
                StopCoroutine(dropRoutine);
            }

            dropRoutine = StartCoroutine(DropRoutine());
        }

        public void ResetShowcaseState ()
        {
            Initialize();

            if (dropRoutine != null)
            {
                StopCoroutine(dropRoutine);
                dropRoutine = null;
            }

            for (int i = 0; i < cubes.Length; i++)
            {
                PlaceAtRest(i);
            }
        }

        // ----- Private Methods
        private void Initialize ()
        {
            if (initialized)
            {
                return;
            }

            cubes ??= Array.Empty<FallingCube>();
            restPositions = new Vector3[cubes.Length];
            restRotations = new Quaternion[cubes.Length];

            for (int i = 0; i < cubes.Length; i++)
            {
                if (cubes[i] == null || cubes[i].Body == null)
                {
                    continue;
                }

                // Where a cube sits in the scene is where it goes back to, so the showcase can be
                // replayed as many times as the visitor wants.
                restPositions[i] = cubes[i].Body.transform.position;
                restRotations[i] = cubes[i].Body.transform.rotation;

                PlaceAtRest(i);
            }

            initialized = true;
        }

        private IEnumerator DropRoutine ()
        {
            for (int i = 0; i < cubes.Length; i++)
            {
                Drop(i);

                if (dropInterval > 0f && i < cubes.Length - 1)
                {
                    yield return new WaitForSeconds(dropInterval);
                }
            }

            dropRoutine = null;
        }

        private void Drop (int index)
        {
            FallingCube cube = cubes[index];
            if (cube == null || cube.Body == null)
            {
                return;
            }

            Rigidbody body = cube.Body;

            // Kinematic first: it clears whatever motion was left from the previous drop, and lets
            // the cube be teleported without fighting the physics step.
            body.isKinematic = true;

            float height = UnityEngine.Random.Range(dropHeightRange.x, dropHeightRange.y);
            // A fresh orientation every drop, so the cubes never fall the same way twice and land
            // on a corner as often as they land flat. Reset still restores the authored rotation.
            body.transform.SetPositionAndRotation(restPositions[index] + Vector3.up * height,
                UnityEngine.Random.rotation);

            body.mass = cube.Mass;
            body.isKinematic = false;

            // AddForce instead of writing the velocity: that property was renamed in Unity 6, and
            // this reads the same on every version.
            if (cube.ExtraDropSpeed > 0f)
            {
                body.AddForce(Vector3.down * cube.ExtraDropSpeed, ForceMode.VelocityChange);
            }
        }

        private void PlaceAtRest (int index)
        {
            FallingCube cube = cubes[index];
            if (cube == null || cube.Body == null)
            {
                return;
            }

            cube.Body.isKinematic = true;
            cube.Body.transform.SetPositionAndRotation(restPositions[index], restRotations[index]);
        }
    }
}
