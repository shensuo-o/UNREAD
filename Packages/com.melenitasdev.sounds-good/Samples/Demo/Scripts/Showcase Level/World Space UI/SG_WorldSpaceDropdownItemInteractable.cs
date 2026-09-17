using UnityEngine;
using UnityEngine.UI;

namespace MelenitasDev.SoundsGood.Demo
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public class SG_WorldSpaceDropdownItemInteractable : MonoBehaviour, IInteractive
    {
        // ----- Serialized Fields
        [SerializeField] private Toggle toggle;
        [SerializeField] private float colliderDepth = 1f;

        // ----- Unity Events
        void Reset ()
        {
            FindToggle();
            SyncColliderToRect();
        }

        void Awake ()
        {
            FindToggle();
            SyncColliderToRect();
        }

        // ----- Public Methods
        public void Interact ()
        {
            FindToggle();

            if (toggle == null || !toggle.IsActive() || !toggle.interactable)
            {
                return;
            }

            if (toggle.isOn)
            {
                toggle.onValueChanged.Invoke(true);
                return;
            }

            toggle.isOn = true;
        }

        public void SetToggle (Toggle assignedToggle, float depth)
        {
            toggle = assignedToggle;
            colliderDepth = depth;
            SyncColliderToRect();
        }

        // ----- Private Methods
        private void SyncColliderToRect ()
        {
            RectTransform rectTransform = transform as RectTransform;
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (rectTransform == null || boxCollider == null)
            {
                return;
            }

            Rect rect = rectTransform.rect;
            boxCollider.size = new Vector3(rect.width, rect.height, colliderDepth);
            boxCollider.center = new Vector3(rect.center.x, rect.center.y, 0f);
        }

        private void FindToggle ()
        {
            if (toggle != null)
            {
                return;
            }

            toggle = GetComponent<Toggle>();
            if (toggle == null)
            {
                toggle = GetComponentInParent<Toggle>();
            }

            if (toggle == null)
            {
                toggle = GetComponentInChildren<Toggle>();
            }
        }
    }
}
