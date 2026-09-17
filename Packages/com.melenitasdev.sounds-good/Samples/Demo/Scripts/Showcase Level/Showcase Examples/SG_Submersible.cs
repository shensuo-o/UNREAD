using System.Collections;
using UnityEngine;

namespace MelenitasDev.SoundsGood.Demo
{
    public class SG_Submersible : MonoBehaviour, IShowcaseResettable
    {
        // ----- Serialized Fields
        [Header("Movement")]
        [SerializeField, Min(0f)] private float downwardDistance = 1f;
        [SerializeField, Min(0f)] private float sidewaysDistance = 1f;
        [SerializeField, Min(1)] private int sideToSideCycles = 2;
        [SerializeField] private bool useLocalPosition = true;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float downDuration = 0.6f;
        [SerializeField, Min(0f)] private float waitDuration = 1f;
        [SerializeField, Min(0f)] private float sideMoveDuration = 0.35f;
        [SerializeField, Min(0f)] private float upDuration = 0.6f;
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // ----- Fields
        private Vector3 originalPosition;
        private bool initialized;
        private bool isAnimating;
        private Coroutine animationRoutine;

        // ----- Unity Events
        void Awake () { Initialize(); }

        void OnDisable ()
        {
            StopAnimation();
        }

        // ----- Public Methods
        public void Submerge ()
        {
            Initialize();

            if (isAnimating)
            {
                return;
            }

            animationRoutine = StartCoroutine(SubmergeRoutine());
        }

        public void ResetToOriginalPosition ()
        {
            Initialize();
            StopAnimation();
            SetPosition(originalPosition);
        }

        public void ResetShowcaseState ()
        {
            ResetToOriginalPosition();
        }

        // ----- Private Methods
        private void Initialize ()
        {
            if (initialized)
            {
                return;
            }

            originalPosition = GetPosition();
            initialized = true;
        }

        private IEnumerator SubmergeRoutine ()
        {
            isAnimating = true;

            Vector3 loweredPosition = originalPosition + Vector3.down * downwardDistance;
            yield return MoveTo(loweredPosition, downDuration);

            if (waitDuration > 0f)
            {
                yield return new WaitForSeconds(waitDuration);
            }

            Vector3 leftPosition = loweredPosition + Vector3.left * sidewaysDistance;
            Vector3 rightPosition = loweredPosition + Vector3.right * sidewaysDistance;

            for (int i = 0; i < sideToSideCycles; i++)
            {
                yield return MoveTo(leftPosition, sideMoveDuration);
                yield return MoveTo(rightPosition, sideMoveDuration);
            }

            yield return MoveTo(loweredPosition, sideMoveDuration);
            yield return MoveTo(originalPosition, upDuration);

            SetPosition(originalPosition);
            isAnimating = false;
            animationRoutine = null;
        }

        private IEnumerator MoveTo (Vector3 targetPosition, float duration)
        {
            Vector3 startPosition = GetPosition();

            if (duration <= 0f)
            {
                SetPosition(targetPosition);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveT = movementCurve != null ? movementCurve.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);
                SetPosition(Vector3.LerpUnclamped(startPosition, targetPosition, curveT));
                yield return null;
            }

            SetPosition(targetPosition);
        }

        private Vector3 GetPosition ()
        {
            return useLocalPosition ? transform.localPosition : transform.position;
        }

        private void SetPosition (Vector3 position)
        {
            if (useLocalPosition)
            {
                transform.localPosition = position;
                return;
            }

            transform.position = position;
        }

        private void StopAnimation ()
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }

            isAnimating = false;
        }
    }
}
