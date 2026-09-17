using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MelenitasDev.SoundsGood.Demo
{
    public class SG_PlayerControlHints : MonoBehaviour
    {
        // ----- Serialized Fields
        [Header("UI")]
        [SerializeField] private Transform hintsParent;
        [SerializeField] private GameObject[] defaultHintPrefabs;
        [SerializeField] private bool hideParentWhenEmpty = true;

        [Header("Fade")]
        [SerializeField, Min(0f)] private float fadeInDuration = 0.15f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.15f;
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Look Detection")]
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private float lookDistance = 3f;
        [SerializeField] private LayerMask lookMask = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        // ----- Fields
        private readonly List<SG_ControlHintSource> activeTriggerSources = new List<SG_ControlHintSource>();
        private readonly List<HintInstance> spawnedHints = new List<HintInstance>();
        private readonly Dictionary<GameObject, HintInstance> spawnedHintLookup = new Dictionary<GameObject, HintInstance>();
        private readonly List<GameObject> pendingHintPrefabs = new List<GameObject>();
        private readonly HashSet<GameObject> pendingHintLookup = new HashSet<GameObject>();

        private SG_ControlHintSource currentLookSource;
        private bool rebuildRequested = true;

        private class HintInstance
        {
            public GameObject Prefab;
            public GameObject GameObject;
            public CanvasGroup CanvasGroup;
            public Coroutine FadeRoutine;
        }

        // ----- Unity Events
        void Awake ()
        {
            if (rayOrigin == null && Camera.main != null)
            {
                rayOrigin = Camera.main.transform;
            }
        }

        void OnEnable () { RequestRebuild(); }

        void Update ()
        {
            UpdateLookSource();

            if (rebuildRequested)
            {
                RebuildHints();
            }
        }

        void OnDisable ()
        {
            activeTriggerSources.Clear();
            currentLookSource = null;
            ClearSpawnedHints();
            UpdateParentVisibility();
        }

        // ----- Public Methods
        public void AddTriggerSource (SG_ControlHintSource source)
        {
            if (source == null || activeTriggerSources.Contains(source))
            {
                return;
            }

            activeTriggerSources.Add(source);
            RequestRebuild();
        }

        public void RemoveTriggerSource (SG_ControlHintSource source)
        {
            if (source == null || !activeTriggerSources.Remove(source))
            {
                return;
            }

            RequestRebuild();
        }

        public void RequestRebuild () { rebuildRequested = true; }

        // ----- Private Methods
        private void UpdateLookSource ()
        {
            SG_ControlHintSource lookSource = FindLookSource();

            if (lookSource == currentLookSource)
            {
                return;
            }

            currentLookSource = lookSource;
            RequestRebuild();
        }

        private SG_ControlHintSource FindLookSource ()
        {
            if (rayOrigin == null)
            {
                return null;
            }

            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            RaycastHit hit;

            if (!Physics.Raycast(ray, out hit, lookDistance, lookMask, triggerInteraction))
            {
                return null;
            }

            SG_ControlHintSource source = hit.collider.GetComponentInParent<SG_ControlHintSource>();
            return source != null && source.ShowWhenLookedAt ? source : null;
        }

        private void RebuildHints ()
        {
            rebuildRequested = false;

            CollectHintPrefabs();
            SyncSpawnedHints();
            UpdateParentVisibility();
        }

        private void CollectHintPrefabs ()
        {
            pendingHintPrefabs.Clear();
            pendingHintLookup.Clear();

            AddHintPrefabs(defaultHintPrefabs);

            for (int i = 0; i < activeTriggerSources.Count; i++)
            {
                SG_ControlHintSource source = activeTriggerSources[i];
                if (source == null)
                {
                    activeTriggerSources.RemoveAt(i);
                    i--;
                    continue;
                }

                AddHintPrefabs(source.ControlHintPrefabs);
            }

            if (currentLookSource != null)
            {
                AddHintPrefabs(currentLookSource.ControlHintPrefabs);
            }
        }

        private void AddHintPrefabs (GameObject[] prefabs)
        {
            if (prefabs == null)
            {
                return;
            }

            foreach (GameObject prefab in prefabs)
            {
                if (prefab == null || !pendingHintLookup.Add(prefab))
                {
                    continue;
                }

                pendingHintPrefabs.Add(prefab);
            }
        }

        private void SyncSpawnedHints ()
        {
            if (hintsParent == null)
            {
                ClearSpawnedHints();
                return;
            }

            if (pendingHintPrefabs.Count > 0)
            {
                SetParentVisible(true);
            }

            for (int i = spawnedHints.Count - 1; i >= 0; i--)
            {
                HintInstance hint = spawnedHints[i];
                if (hint == null || hint.GameObject == null || hint.CanvasGroup == null)
                {
                    RemoveHintInstance(hint, i);
                    continue;
                }

                if (hint.Prefab == null || !pendingHintLookup.Contains(hint.Prefab))
                {
                    FadeHint(hint, 0f, fadeOutDuration, true);
                }
            }

            for (int i = 0; i < pendingHintPrefabs.Count; i++)
            {
                GameObject prefab = pendingHintPrefabs[i];
                HintInstance hint;

                if (spawnedHintLookup.TryGetValue(prefab, out hint))
                {
                    if (hint.GameObject != null)
                    {
                        hint.GameObject.transform.SetSiblingIndex(i);
                    }

                    FadeHint(hint, 1f, fadeInDuration, false);
                    continue;
                }

                CreateHint(prefab, i);
            }
        }

        private void CreateHint (GameObject prefab, int siblingIndex)
        {
            GameObject hintObject = Instantiate(prefab, hintsParent);
            CanvasGroup canvasGroup = GetOrAddCanvasGroup(hintObject);
            canvasGroup.alpha = 0f;
            hintObject.transform.SetSiblingIndex(siblingIndex);
            hintObject.SetActive(true);

            HintInstance hint = new HintInstance
            {
                Prefab = prefab,
                GameObject = hintObject,
                CanvasGroup = canvasGroup
            };

            spawnedHints.Add(hint);
            spawnedHintLookup[prefab] = hint;
            FadeHint(hint, 1f, fadeInDuration, false);
        }

        private void ClearSpawnedHints ()
        {
            foreach (HintInstance hint in spawnedHints)
            {
                if (hint == null)
                {
                    continue;
                }

                if (hint.FadeRoutine != null)
                {
                    StopCoroutine(hint.FadeRoutine);
                }

                if (hint.GameObject != null)
                {
                    hint.GameObject.SetActive(false);
                    Destroy(hint.GameObject);
                }
            }

            spawnedHints.Clear();
            spawnedHintLookup.Clear();
        }

        private void FadeHint (HintInstance hint, float targetAlpha, float duration, bool destroyAfterFade)
        {
            if (hint == null)
            {
                return;
            }

            if (hint.CanvasGroup == null)
            {
                if (destroyAfterFade)
                {
                    DestroyHint(hint);
                }

                return;
            }

            if (hint.FadeRoutine != null)
            {
                StopCoroutine(hint.FadeRoutine);
            }

            hint.FadeRoutine = StartCoroutine(FadeHintRoutine(hint, targetAlpha, duration, destroyAfterFade));
        }

        private IEnumerator FadeHintRoutine (HintInstance hint, float targetAlpha, float duration, bool destroyAfterFade)
        {
            CanvasGroup canvasGroup = hint.CanvasGroup;
            float startAlpha = canvasGroup.alpha;

            if (duration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < duration && canvasGroup != null)
                {
                    elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
                    yield return null;
                }
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = targetAlpha;
            }

            hint.FadeRoutine = null;

            if (destroyAfterFade)
            {
                DestroyHint(hint);
            }
        }

        private void DestroyHint (HintInstance hint)
        {
            if (hint == null)
            {
                return;
            }

            if (hint.FadeRoutine != null)
            {
                StopCoroutine(hint.FadeRoutine);
                hint.FadeRoutine = null;
            }

            spawnedHints.Remove(hint);

            if (hint.Prefab != null)
            {
                spawnedHintLookup.Remove(hint.Prefab);
            }

            if (hint.GameObject != null)
            {
                Destroy(hint.GameObject);
            }

            UpdateParentVisibility();
        }

        private void RemoveHintInstance (HintInstance hint, int index)
        {
            spawnedHints.RemoveAt(index);

            if (hint != null && hint.Prefab != null)
            {
                spawnedHintLookup.Remove(hint.Prefab);
            }
        }

        private static CanvasGroup GetOrAddCanvasGroup (GameObject hintObject)
        {
            CanvasGroup canvasGroup = hintObject.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                return canvasGroup;
            }

            canvasGroup = hintObject.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            return canvasGroup;
        }

        private void UpdateParentVisibility ()
        {
            if (!hideParentWhenEmpty || hintsParent == null || hintsParent == transform)
            {
                return;
            }

            SetParentVisible(spawnedHints.Count > 0);
        }

        private void SetParentVisible (bool visible)
        {
            if (!hideParentWhenEmpty || hintsParent == null || hintsParent == transform)
            {
                return;
            }

            hintsParent.gameObject.SetActive(visible);
        }
    }
}
