using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace MelenitasDev.SoundsGood.Demo
{
    public class SG_Button : MonoBehaviour, IInteractive, IShowcaseResettable
    {
        // ----- Serialized Fields
        [Header("Visual")]
        [SerializeField] private Transform buttonVisual;

        [Header("Press Settings")]
        [SerializeField] private Vector3 localPressOffset = new Vector3(0f, -0.02f, 0f);
        [SerializeField] private float pressDuration = 0.08f;
        [SerializeField] private float releaseDuration = 0.12f;
        [SerializeField] private bool canBePressedWhileAnimating = false;

        [Header("Events")]
        [SerializeField] private UnityEvent onPressed;

        // ----- Fields
        private Vector3 originalLocalPosition;
        private bool initialized;
        private bool isAnimating;
        private Coroutine pressRoutine;

        // ----- Unity Events
        void Awake () { Initialize(); }

        // ----- Private Methods
        private void Initialize ()
        {
            if (initialized)
            {
                return;
            }

            if (buttonVisual == null)
            {
                buttonVisual = transform;
            }

            originalLocalPosition = buttonVisual.localPosition;
            initialized = true;
        }

        private IEnumerator PressAnimationRoutine ()
        {
            isAnimating = true;

            Vector3 startPos = originalLocalPosition;
            Vector3 pressedPos = originalLocalPosition + localPressOffset;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(pressDuration, 0.0001f);
                float lerp = Mathf.SmoothStep(0f, 1f, t);
                buttonVisual.localPosition = Vector3.Lerp(startPos, pressedPos, lerp);
                yield return null;
            }

            onPressed?.Invoke();

            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(releaseDuration, 0.0001f);
                float lerp = Mathf.SmoothStep(0f, 1f, t);
                buttonVisual.localPosition = Vector3.Lerp(pressedPos, originalLocalPosition, lerp);
                yield return null;
            }

            buttonVisual.localPosition = originalLocalPosition;
            isAnimating = false;
            pressRoutine = null;
        }


        // ----- Public Methods
        public void Interact ()
        {
            Initialize();

            if (isAnimating && !canBePressedWhileAnimating)
            {
                return;
            }

            if (pressRoutine != null)
            {
                StopCoroutine(pressRoutine);
            }

            pressRoutine = StartCoroutine(PressAnimationRoutine());
        }

        public void AddOnPressedListener (UnityAction action) { onPressed.AddListener(action); }
        public void RemoveOnPressedListener (UnityAction action) { onPressed.RemoveListener(action); }

        public void ResetShowcaseState ()
        {
            Initialize();

            if (pressRoutine != null)
            {
                StopCoroutine(pressRoutine);
                pressRoutine = null;
            }

            isAnimating = false;
            buttonVisual.localPosition = originalLocalPosition;
        }
    }
}
