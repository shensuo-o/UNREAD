using UnityEngine;
using TMPro;

namespace MelenitasDev.SoundsGood.Demo
{
    public class SG_PlayerInteract : MonoBehaviour
    {
        // ----- Serialized Fields
        [Header("Raycast")]
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private float interactDistance = 3f;

        [Header("Input")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private bool useMouseButton = true;
        [SerializeField] private int interactMouseButton = 0;

        [Header("Audio")]
        [SerializeField] private string interactionSound = "click";

        [Header("Outline")]
        [SerializeField] private bool outlineInteractiveOnLook = true;
        [SerializeField] private bool createOutlineTargetIfMissing = true;
        [SerializeField] private Material outlineMaterial;
        [SerializeField] private Color outlineColor = new Color(1f, 0.72f, 0.16f, 0.9f);
        [SerializeField, Min(0.001f)] private float outlineWidth = 0.035f;
        [SerializeField, Range(0f, 0.5f)] private float outlinePulseStrength = 0.08f;
        [SerializeField, Min(0f)] private float outlinePulseSpeed = 2.5f;

        // ----- Fields
        private IInteractive currentInteractive;
        private IHoldInteractive activeHoldInteractive;
        private SG_InteractableOutline currentOutline;
        private RaycastHit lastHit;

        // ----- Unity Events
        void Awake ()
        {
            if (rayOrigin == null && Camera.main != null)
            {
                rayOrigin = Camera.main.transform;
            }
        }

        void Update ()
        {
            UpdateInteractiveTarget();
            HandleInteractionInput();
        }

        void OnDisable ()
        {
            EndHoldInteraction();
            SetCurrentOutline(null);
        }

        // ----- Private Methods
        private void UpdateInteractiveTarget ()
        {
            currentInteractive = null;
            SG_InteractableOutline outline = null;

            if (rayOrigin == null)
            {
                SetCurrentOutline(null);
                return;
            }

            Vector3 origin = rayOrigin.position;
            Vector3 direction = rayOrigin.forward;

            if (Physics.Raycast(origin, direction, out lastHit, interactDistance))
            {
                currentInteractive = lastHit.collider.GetComponentInParent<IInteractive>();
                outline = GetOutlineForCurrentHit();
            }

            SetCurrentOutline(outline);
        }

        private void HandleInteractionInput ()
        {
            if (activeHoldInteractive != null)
            {
                if (IsInteractHeld())
                {
                    activeHoldInteractive.HoldInteract();
                    return;
                }

                EndHoldInteraction();
                return;
            }

            if (!IsInteractPressed() || currentInteractive == null)
            {
                return;
            }

            IHoldInteractive holdInteractive = currentInteractive as IHoldInteractive;
            if (holdInteractive != null)
            {
                activeHoldInteractive = holdInteractive;
                PlayInteractionSound();
                activeHoldInteractive.BeginInteract();
                return;
            }

            PlayInteractionSound();
            currentInteractive.Interact();
        }

        private void PlayInteractionSound ()
        {
            if (string.IsNullOrEmpty(interactionSound))
            {
                return;
            }

            new Sound(interactionSound)
                .SetSpatialSound(false)
                .SetRandomPitch()
                .Play();
        }

        private SG_InteractableOutline GetOutlineForCurrentHit ()
        {
            if (!outlineInteractiveOnLook || currentInteractive == null)
            {
                return null;
            }

            Component interactiveComponent = currentInteractive as Component;
            if (interactiveComponent == null)
            {
                return null;
            }

            SG_InteractableOutline outline = interactiveComponent.GetComponent<SG_InteractableOutline>();
            if (outline == null)
            {
                outline = interactiveComponent.GetComponentInChildren<SG_InteractableOutline>();
            }

            if (outline == null)
            {
                outline = interactiveComponent.GetComponentInParent<SG_InteractableOutline>();
            }

            if (outline == null && createOutlineTargetIfMissing)
            {
                outline = interactiveComponent.gameObject.AddComponent<SG_InteractableOutline>();
                outline.SetTargetRenderer(FindBestOutlineRenderer(interactiveComponent));
            }

            if (outline != null)
            {
                outline.SetOutlineMaterial(outlineMaterial);
                outline.SetVisual(outlineColor, outlineWidth, outlinePulseStrength, outlinePulseSpeed);
            }

            return outline;
        }

        private Renderer FindBestOutlineRenderer (Component interactiveComponent)
        {
            Renderer hitRenderer = lastHit.collider != null ? lastHit.collider.GetComponent<Renderer>() : null;
            if (IsValidOutlineRenderer(hitRenderer))
            {
                return hitRenderer;
            }

            hitRenderer = lastHit.collider != null ? lastHit.collider.GetComponentInParent<Renderer>() : null;
            if (IsValidOutlineRenderer(hitRenderer))
            {
                return hitRenderer;
            }

            hitRenderer = lastHit.collider != null ? lastHit.collider.GetComponentInChildren<Renderer>() : null;
            if (IsValidOutlineRenderer(hitRenderer))
            {
                return hitRenderer;
            }

            hitRenderer = interactiveComponent.GetComponent<Renderer>();
            if (IsValidOutlineRenderer(hitRenderer))
            {
                return hitRenderer;
            }

            hitRenderer = interactiveComponent.GetComponentInChildren<Renderer>();
            if (IsValidOutlineRenderer(hitRenderer))
            {
                return hitRenderer;
            }

            return null;
        }

        private void SetCurrentOutline (SG_InteractableOutline outline)
        {
            if (currentOutline == outline)
            {
                return;
            }

            if (currentOutline != null)
            {
                currentOutline.SetVisible(false);
            }

            currentOutline = outline;

            if (currentOutline != null)
            {
                currentOutline.SetVisible(true);
            }
        }

        private static bool IsValidOutlineRenderer (Renderer targetRenderer)
        {
            return (targetRenderer is MeshRenderer || targetRenderer is SkinnedMeshRenderer)
                && targetRenderer.GetComponent<TMP_Text>() == null;
        }

        private bool IsInteractPressed ()
        {
            return SG_Input.GetKeyDown(interactKey) || IsMouseButtonDown();
        }

        private bool IsInteractHeld ()
        {
            return SG_Input.GetKey(interactKey) || IsMouseButtonHeld();
        }

        private bool IsMouseButtonDown ()
        {
            return useMouseButton && interactMouseButton >= 0 && SG_Input.GetMouseButtonDown(interactMouseButton);
        }

        private bool IsMouseButtonHeld ()
        {
            return useMouseButton && interactMouseButton >= 0 && SG_Input.GetMouseButton(interactMouseButton);
        }

        private void EndHoldInteraction ()
        {
            if (activeHoldInteractive == null)
            {
                return;
            }

            activeHoldInteractive.EndInteract();
            activeHoldInteractive = null;
        }
    }
}
