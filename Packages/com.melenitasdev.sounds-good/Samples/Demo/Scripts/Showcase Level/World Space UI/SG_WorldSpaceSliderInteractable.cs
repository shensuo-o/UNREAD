using UnityEngine;
using UnityEngine.UI;

namespace MelenitasDev.SoundsGood.Demo
{
    [DisallowMultipleComponent]
    public class SG_WorldSpaceSliderInteractable : MonoBehaviour, IHoldInteractive
    {
        // ----- Serialized Fields
        [SerializeField] private Slider slider;

        [Header("Aim")]
        [SerializeField] private RectTransform aimRect;
        [SerializeField] private Camera eventCamera;

        // ----- Unity Events
        void Reset () { FindSlider(); }
        void Awake () { FindSlider(); }

        // ----- Public Methods
        public void Interact () { SetValueFromCameraAim(); }
        public void BeginInteract () { SetValueFromCameraAim(); }
        public void HoldInteract () { SetValueFromCameraAim(); }
        public void EndInteract () { }

        // ----- Private Methods
        private void SetValueFromCameraAim ()
        {
            FindSlider();

            if (slider == null || !slider.IsActive() || !slider.interactable)
            {
                return;
            }

            RectTransform targetRect = aimRect != null ? aimRect : slider.transform as RectTransform;
            if (targetRect == null)
            {
                return;
            }

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    targetRect,
                    screenCenter,
                    GetEventCamera(),
                    out Vector2 localPoint))
            {
                return;
            }

            slider.normalizedValue = GetNormalizedValue(targetRect.rect, localPoint);
        }

        private void FindSlider ()
        {
            if (slider != null)
            {
                return;
            }

            slider = GetComponent<Slider>();
            if (slider == null)
            {
                slider = GetComponentInParent<Slider>();
            }

            if (slider == null)
            {
                slider = GetComponentInChildren<Slider>();
            }
        }

        private Camera GetEventCamera ()
        {
            if (eventCamera != null)
            {
                return eventCamera;
            }

            Canvas canvas = slider != null ? slider.GetComponentInParent<Canvas>() : null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera != null)
            {
                return canvas.worldCamera;
            }

            return Camera.main;
        }

        private float GetNormalizedValue (Rect rect, Vector2 localPoint)
        {
            float normalizedValue;

            switch (slider.direction)
            {
                case Slider.Direction.RightToLeft:
                    normalizedValue = 1f - Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
                    break;
                case Slider.Direction.BottomToTop:
                    normalizedValue = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);
                    break;
                case Slider.Direction.TopToBottom:
                    normalizedValue = 1f - Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);
                    break;
                default:
                    normalizedValue = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
                    break;
            }

            return Mathf.Clamp01(normalizedValue);
        }
    }
}
