using System.Collections;
using UnityEngine;

namespace MelenitasDev.SoundsGood.Demo
{
    public class SG_OcclusionObstaclesShowcase : MonoBehaviour, IShowcaseResettable
    {
        // ----- Serialized Fields
        [SerializeField] private Transform[] obstacles = new Transform[4];
        [SerializeField] private float moveDistance = 2f;
        [SerializeField, Min(0f)] private float moveDuration = 0.5f;

        // ----- Fields
        private Vector3[] downPositions;
        private bool[] isUp;
        private Coroutine[] moveRoutines;
        private bool initialized;

        // ----- Unity Events
        void Awake ()
        {
            Initialize();
        }

        // ----- Public Methods
        public void SwitchObstacle1 () { SwitchObstacle(0); }
        public void SwitchObstacle2 () { SwitchObstacle(1); }
        public void SwitchObstacle3 () { SwitchObstacle(2); }
        public void SwitchObstacle4 () { SwitchObstacle(3); }

        public void ResetShowcaseState ()
        {
            Initialize();

            if (obstacles == null)
            {
                return;
            }

            for (int i = 0; i < obstacles.Length; i++)
            {
                if (moveRoutines[i] != null)
                {
                    StopCoroutine(moveRoutines[i]);
                    moveRoutines[i] = null;
                }

                isUp[i] = false;

                if (obstacles[i] != null)
                {
                    obstacles[i].localPosition = downPositions[i];
                }
            }
        }

        // ----- Private Methods
        private void Initialize ()
        {
            if (initialized)
            {
                return;
            }

            obstacles ??= new Transform[0];
            downPositions = new Vector3[obstacles.Length];
            isUp = new bool[obstacles.Length];
            moveRoutines = new Coroutine[obstacles.Length];

            for (int i = 0; i < obstacles.Length; i++)
            {
                if (obstacles[i] != null)
                {
                    downPositions[i] = obstacles[i].localPosition;
                }
            }

            initialized = true;
        }

        private void SwitchObstacle (int index)
        {
            Initialize();

            if (index < 0 || index >= obstacles.Length || obstacles[index] == null)
            {
                return;
            }

            if (moveRoutines[index] != null)
            {
                StopCoroutine(moveRoutines[index]);
            }

            isUp[index] = !isUp[index];
            Vector3 targetPosition = downPositions[index] + (isUp[index] ? Vector3.up * moveDistance : Vector3.zero);

            if (moveDuration <= 0f)
            {
                obstacles[index].localPosition = targetPosition;
                return;
            }

            moveRoutines[index] = StartCoroutine(MoveObstacleRoutine(index, targetPosition));
        }

        private IEnumerator MoveObstacleRoutine (int index, Vector3 targetPosition)
        {
            Transform obstacle = obstacles[index];
            Vector3 startPosition = obstacle.localPosition;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(moveDuration, 0.0001f);
                float lerp = Mathf.SmoothStep(0f, 1f, t);
                obstacle.localPosition = Vector3.Lerp(startPosition, targetPosition, lerp);
                yield return null;
            }

            obstacle.localPosition = targetPosition;
            moveRoutines[index] = null;
        }
    }
}
