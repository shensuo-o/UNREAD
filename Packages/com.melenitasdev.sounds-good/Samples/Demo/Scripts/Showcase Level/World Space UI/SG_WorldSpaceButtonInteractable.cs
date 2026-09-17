using UnityEngine;
using UnityEngine.UI;

namespace MelenitasDev.SoundsGood.Demo
{
    [DisallowMultipleComponent]
    public class SG_WorldSpaceButtonInteractable : MonoBehaviour, IInteractive
    {
        // ----- Serialized Fields
        [SerializeField] private Button button;

        // ----- Unity Events
        void Reset () { FindButton(); }
        void Awake () { FindButton(); }

        // ----- Public Methods
        public void Interact ()
        {
            FindButton();

            if (button == null || !button.IsActive() || !button.interactable)
            {
                return;
            }

            button.onClick.Invoke();
        }

        // ----- Private Methods
        private void FindButton ()
        {
            if (button != null)
            {
                return;
            }

            button = GetComponent<Button>();
            if (button == null)
            {
                button = GetComponentInParent<Button>();
            }

            if (button == null)
            {
                button = GetComponentInChildren<Button>();
            }
        }
    }
}
