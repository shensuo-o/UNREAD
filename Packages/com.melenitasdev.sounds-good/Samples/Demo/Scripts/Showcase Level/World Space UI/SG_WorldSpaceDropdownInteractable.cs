using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MelenitasDev.SoundsGood.Demo
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public class SG_WorldSpaceDropdownInteractable : MonoBehaviour, IInteractive
    {
        // ----- Serialized Fields
        [SerializeField] private TMP_Dropdown dropdown;
        [SerializeField] private float colliderDepth = 1f;

        // ----- Fields
        private Coroutine setupItemsRoutine;

        // ----- Unity Events
        void Reset ()
        {
            FindDropdown();
            SyncColliderToRect();
        }

        void Awake ()
        {
            FindDropdown();
            SyncColliderToRect();
        }

        // ----- Public Methods
        public void Interact ()
        {
            FindDropdown();

            if (dropdown == null || !dropdown.IsActive() || !dropdown.interactable)
            {
                return;
            }

            Transform dropdownList = FindDropdownList();
            if (dropdownList != null)
            {
                dropdown.Hide();
                return;
            }

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(dropdown.gameObject);
            }

            dropdown.Show();

            if (setupItemsRoutine != null)
            {
                StopCoroutine(setupItemsRoutine);
            }

            setupItemsRoutine = StartCoroutine(SetupItemsNextFrame());
        }

        public void SetDropdown (TMP_Dropdown assignedDropdown)
        {
            dropdown = assignedDropdown;
            SyncColliderToRect();
        }

        public void SyncColliderToRect ()
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

        // ----- Private Methods
        private IEnumerator SetupItemsNextFrame ()
        {
            yield return null;

            Canvas.ForceUpdateCanvases();
            SetupDropdownItems();
            setupItemsRoutine = null;
        }

        private void SetupDropdownItems ()
        {
            Transform dropdownList = FindDropdownList();
            if (dropdownList == null)
            {
                return;
            }

            Toggle[] optionToggles = dropdownList.GetComponentsInChildren<Toggle>(false);
            foreach (Toggle optionToggle in optionToggles)
            {
                SG_WorldSpaceDropdownItemInteractable itemInteractable =
                    optionToggle.GetComponent<SG_WorldSpaceDropdownItemInteractable>();
                if (itemInteractable == null)
                {
                    itemInteractable = optionToggle.gameObject.AddComponent<SG_WorldSpaceDropdownItemInteractable>();
                }

                itemInteractable.SetToggle(optionToggle, colliderDepth);
            }
        }

        private Transform FindDropdownList ()
        {
            if (dropdown == null)
            {
                return null;
            }

            Transform directChild = dropdown.transform.Find("Dropdown List");
            if (directChild != null && directChild.gameObject.activeInHierarchy)
            {
                return directChild;
            }

            Canvas canvas = dropdown.GetComponentInParent<Canvas>();
            Transform searchRoot = canvas != null ? canvas.transform : dropdown.transform.root;
            RectTransform[] rectTransforms = searchRoot.GetComponentsInChildren<RectTransform>(false);
            foreach (RectTransform rectTransform in rectTransforms)
            {
                if (rectTransform.name == "Dropdown List" && rectTransform.gameObject.activeInHierarchy)
                {
                    return rectTransform;
                }
            }

            return null;
        }

        private void FindDropdown ()
        {
            if (dropdown != null)
            {
                return;
            }

            dropdown = GetComponent<TMP_Dropdown>();
            if (dropdown == null)
            {
                dropdown = GetComponentInParent<TMP_Dropdown>();
            }

            if (dropdown == null)
            {
                dropdown = GetComponentInChildren<TMP_Dropdown>();
            }
        }
    }
}
