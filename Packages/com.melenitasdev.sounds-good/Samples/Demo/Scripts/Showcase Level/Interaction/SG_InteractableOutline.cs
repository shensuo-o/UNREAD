using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MelenitasDev.SoundsGood.Demo
{
    [DisallowMultipleComponent]
    public class SG_InteractableOutline : MonoBehaviour
    {
        // ----- Serialized Fields
        [Header("Target")]
        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private bool includeChildRenderers = true;
        [SerializeField] private bool includeInactiveRenderers = true;

        [Header("Material")]
        [SerializeField] private Material outlineMaterial;

        [Header("Visual")]
        [SerializeField] private Color outlineColor = new Color(1f, 0.72f, 0.16f, 0.9f);
        [SerializeField, Min(0.001f)] private float outlineWidth = 0.035f;
        [SerializeField, Range(0f, 0.5f)] private float pulseStrength = 0.08f;
        [SerializeField, Min(0f)] private float pulseSpeed = 2.5f;
        [SerializeField] private bool visibleOnEnable;

        // ----- Fields
        private static readonly int OutlineColorProperty = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthProperty = Shader.PropertyToID("_OutlineWidth");
        private static readonly int PulseStrengthProperty = Shader.PropertyToID("_PulseStrength");
        private static readonly int PulseSpeedProperty = Shader.PropertyToID("_PulseSpeed");
        private const string OutlineShaderName = "Melenitas Dev/SG_InteractableOutline";

        private readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        private readonly List<Renderer> rendererBuffer = new List<Renderer>();

        private Material runtimeOutlineMaterial;
        private bool isVisible;

        // ----- Unity Events
        private void Awake ()
        {
            EnsureOutlineMaterial();

            if (visibleOnEnable)
            {
                SetVisible(true);
            }
        }

        private void OnDisable () { SetVisible(false); }

        private void OnDestroy ()
        {
            SetVisible(false);

            if (runtimeOutlineMaterial != null)
            {
                Destroy(runtimeOutlineMaterial);
                runtimeOutlineMaterial = null;
            }
        }

        // ----- Public Methods
        public void SetVisible (bool visible)
        {
            if (visible == isVisible)
            {
                return;
            }

            if (visible)
            {
                ApplyOutline();
                return;
            }

            RemoveOutline();
        }

        public void SetTargetRenderer (Renderer targetRenderer)
        {
            targetRenderers = targetRenderer == null ? null : new[] { targetRenderer };
            RefreshOutlineIfVisible();
        }

        public void SetOutlineMaterial (Material material)
        {
            if (outlineMaterial == material)
            {
                return;
            }

            outlineMaterial = material;

            if (runtimeOutlineMaterial != null)
            {
                Destroy(runtimeOutlineMaterial);
                runtimeOutlineMaterial = null;
            }

            EnsureOutlineMaterial();
            RefreshOutlineIfVisible();
        }

        public void SetVisual (Color color, float width, float pulse, float speed)
        {
            outlineColor = color;
            outlineWidth = Mathf.Max(0.001f, width);
            pulseStrength = Mathf.Clamp(pulse, 0f, 0.5f);
            pulseSpeed = Mathf.Max(0f, speed);
            ApplyMaterialProperties();
        }

        // ----- Private Methods
        private void ApplyOutline ()
        {
            Material material = EnsureOutlineMaterial();
            if (material == null)
            {
                return;
            }

            CollectRenderers(rendererBuffer);
            for (int i = 0; i < rendererBuffer.Count; i++)
            {
                Renderer targetRenderer = rendererBuffer[i];
                if (targetRenderer == null || originalMaterials.ContainsKey(targetRenderer))
                {
                    continue;
                }

                Material[] original = targetRenderer.sharedMaterials;
                Material[] outlined = new Material[original.Length + 1];
                original.CopyTo(outlined, 0);
                outlined[outlined.Length - 1] = material;

                originalMaterials[targetRenderer] = original;
                targetRenderer.sharedMaterials = outlined;
            }

            isVisible = originalMaterials.Count > 0;
        }

        private void RemoveOutline ()
        {
            foreach (KeyValuePair<Renderer, Material[]> entry in originalMaterials)
            {
                if (entry.Key != null)
                {
                    entry.Key.sharedMaterials = entry.Value;
                }
            }

            originalMaterials.Clear();
            isVisible = false;
        }

        private Material EnsureOutlineMaterial ()
        {
            if (runtimeOutlineMaterial != null)
            {
                return runtimeOutlineMaterial;
            }

            if (outlineMaterial != null)
            {
                runtimeOutlineMaterial = new Material(outlineMaterial);
            }
            else
            {
                Shader outlineShader = Shader.Find(OutlineShaderName);
                if (outlineShader == null)
                {
                    Debug.LogWarning($"{nameof(SG_InteractableOutline)} could not find shader '{OutlineShaderName}'.", this);
                    return null;
                }

                runtimeOutlineMaterial = new Material(outlineShader);
            }

            runtimeOutlineMaterial.hideFlags = HideFlags.HideAndDontSave;
            ApplyMaterialProperties();
            return runtimeOutlineMaterial;
        }

        private void ApplyMaterialProperties ()
        {
            if (runtimeOutlineMaterial == null)
            {
                return;
            }

            runtimeOutlineMaterial.SetColor(OutlineColorProperty, outlineColor);
            runtimeOutlineMaterial.SetFloat(OutlineWidthProperty, outlineWidth);
            runtimeOutlineMaterial.SetFloat(PulseStrengthProperty, pulseStrength);
            runtimeOutlineMaterial.SetFloat(PulseSpeedProperty, pulseSpeed);
        }

        private void RefreshOutlineIfVisible ()
        {
            if (!isVisible)
            {
                return;
            }

            RemoveOutline();
            ApplyOutline();
        }

        private void CollectRenderers (List<Renderer> renderers)
        {
            renderers.Clear();

            if (targetRenderers != null && targetRenderers.Length > 0)
            {
                AddValidRenderers(targetRenderers, renderers);
                return;
            }

            if (!includeChildRenderers)
            {
                Renderer localRenderer = GetComponent<Renderer>();
                if (IsValidOutlineRenderer(localRenderer))
                {
                    renderers.Add(localRenderer);
                }

                return;
            }

            AddValidRenderers(GetComponentsInChildren<Renderer>(includeInactiveRenderers), renderers);
        }

        private static void AddValidRenderers (Renderer[] candidates, List<Renderer> renderers)
        {
            if (candidates == null)
            {
                return;
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                Renderer candidate = candidates[i];
                if (IsValidOutlineRenderer(candidate) && !renderers.Contains(candidate))
                {
                    renderers.Add(candidate);
                }
            }
        }

        private static bool IsValidOutlineRenderer (Renderer targetRenderer)
        {
            if (targetRenderer == null)
            {
                return false;
            }

            if (!(targetRenderer is MeshRenderer) && !(targetRenderer is SkinnedMeshRenderer))
            {
                return false;
            }

            return targetRenderer.GetComponent<TMP_Text>() == null;
        }
    }
}
