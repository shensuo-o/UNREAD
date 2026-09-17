using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace MelenitasDev.SoundsGood.Demo
{
    public class SG_AlternatingButton : MonoBehaviour, IInteractive, IShowcaseResettable
    {
        // ----- Serialized Fields
        [Header("Visual")]
        [SerializeField] private Transform buttonVisual;

        [Header("Text")]
        [SerializeField] private TMP_Text buttonText;
        [SerializeField] private string firstActionText = "Action 1";
        [SerializeField] private string secondActionText = "Action 2";

        [Header("Press Settings")]
        [SerializeField] private Vector3 localPressOffset = new Vector3(0f, -0.02f, 0f);
        [SerializeField] private float pressDuration = 0.08f;
        [SerializeField] private float releaseDuration = 0.12f;
        [SerializeField] private bool canBePressedWhileAnimating = false;
        [SerializeField] private bool startsWithFirstAction = true;

        [Header("Events")]
        [SerializeField] private UnityEvent onFirstPressed = new UnityEvent();
        [SerializeField] private UnityEvent onSecondPressed = new UnityEvent();

        // ----- Fields
        private Vector3 originalLocalPosition;
        private bool initialized;
        private bool isAnimating;
        private bool useFirstAction;
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
            useFirstAction = startsWithFirstAction;
            UpdateButtonText();
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

            bool invokedFirstAction = useFirstAction;
            InvokeCurrentAction();

            if (useFirstAction == invokedFirstAction)
            {
                useFirstAction = !useFirstAction;
            }

            UpdateButtonText();

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

        private void InvokeCurrentAction ()
        {
            if (useFirstAction)
            {
                onFirstPressed?.Invoke();
                return;
            }

            onSecondPressed?.Invoke();
        }

        private void UpdateButtonText ()
        {
            if (buttonText == null)
            {
                return;
            }

            buttonText.text = useFirstAction ? firstActionText : secondActionText;
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

        public void SetNextAction (bool nextActionIsFirst)
        {
            Initialize();

            useFirstAction = nextActionIsFirst;
            UpdateButtonText();
        }

        public void SetNextActionToFirst () { SetNextAction(true); }
        public void SetNextActionToSecond () { SetNextAction(false); }

        public void AddOnFirstPressedListener (UnityAction action) { onFirstPressed.AddListener(action); }
        public void RemoveOnFirstPressedListener (UnityAction action) { onFirstPressed.RemoveListener(action); }
        public void AddOnSecondPressedListener (UnityAction action) { onSecondPressed.AddListener(action); }
        public void RemoveOnSecondPressedListener (UnityAction action) { onSecondPressed.RemoveListener(action); }

        public void ResetShowcaseState ()
        {
            Initialize();

            if (pressRoutine != null)
            {
                StopCoroutine(pressRoutine);
                pressRoutine = null;
            }

            isAnimating = false;
            useFirstAction = startsWithFirstAction;
            buttonVisual.localPosition = originalLocalPosition;
            UpdateButtonText();
        }
    }
}
