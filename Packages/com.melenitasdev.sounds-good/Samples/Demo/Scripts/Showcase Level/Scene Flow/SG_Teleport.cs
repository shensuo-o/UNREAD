using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using MelenitasDev.SoundsGood;

namespace MelenitasDev.SoundsGood.Demo
{
    public class SG_Teleport : MonoBehaviour
    {
        // ----- Serialized Fields
        [Header("References")]
        [SerializeField] private Transform player;

        [Header("Rooms")]
        [SerializeField] private Transform lobby;
        [SerializeField] private Transform soundEmittersRoom;
        [SerializeField] private Transform soundEffectsRoom;
        [SerializeField] private Transform occlusionRoom;

        [Header("Transition")]
        [SerializeField] private CanvasGroup transitionCanvasGroup;
        [SerializeField] private float fadeToBlackDuration = 0.2f;
        [SerializeField] private float fadeFromBlackDuration = 0.2f;

        [Header("Audio")]
        [SerializeField] private string teleportSound = "teleport";

        [Header("Reset")]
        [SerializeField] private bool resetShowcaseStateOnTeleport = true;
        [SerializeField, Min(0f)] private float audioFadeOutTime = 0f;

        // ----- Fields
        private SG_PlayerController playerController;
        private CharacterController characterController;
        private bool isTransitioning;
        private readonly List<IShowcaseResettable> resetBuffer = new List<IShowcaseResettable>();
        private static readonly Dictionary<System.Type, FieldInfo> PlayOnStartFields =
            new Dictionary<System.Type, FieldInfo>();

        // ----- Unity Events
        private void Awake ()
        {
            FindPlayerIfNeeded();
            CachePlayerReferences();
        }

        // ----- Public Methods
        public void TeleportToLobby ()
        {
            TeleportTo(lobby);
        }

        public void TeleportToSoundEmittersRoom ()
        {
            TeleportTo(soundEmittersRoom);
        }

        public void TeleportToSoundEffectsRoom ()
        {
            TeleportTo(soundEffectsRoom);
        }

        public void TeleportToOcclusionRoom ()
        {
            TeleportTo(occlusionRoom);
        }

        // ----- Private Methods
        private void TeleportTo (Transform target)
        {
            if (isTransitioning)
            {
                return;
            }

            if (target == null)
            {
                Debug.LogWarning($"{nameof(SG_Teleport)} is missing a teleport target.", this);
                return;
            }

            FindPlayerIfNeeded();
            if (player == null)
            {
                Debug.LogWarning($"{nameof(SG_Teleport)} needs a player Transform.", this);
                return;
            }

            CachePlayerReferences();
            if (transitionCanvasGroup == null)
            {
                ResetShowcaseState();
                ExecuteTeleport(target);
                RestoreInitialAutoPlayEmitters();
                PlayTeleportSound();
                return;
            }

            StartCoroutine(TeleportRoutine(target));
        }

        private IEnumerator TeleportRoutine (Transform target)
        {
            isTransitioning = true;
            SetTransitionBlocking(true);

            yield return FadeTransition(1f, fadeToBlackDuration);

            ResetShowcaseState();
            ExecuteTeleport(target);
            RestoreInitialAutoPlayEmitters();
            PlayTeleportSound();

            yield return FadeTransition(0f, fadeFromBlackDuration);

            SetTransitionBlocking(false);
            isTransitioning = false;
        }

        private void ExecuteTeleport (Transform target)
        {
            if (playerController != null)
            {
                playerController.TeleportTo(target);
                return;
            }

            bool controllerWasEnabled = characterController != null && characterController.enabled;
            if (controllerWasEnabled)
            {
                characterController.enabled = false;
            }

            player.SetPositionAndRotation(target.position, target.rotation);

            if (controllerWasEnabled)
            {
                characterController.enabled = true;
            }
        }

        private void PlayTeleportSound ()
        {
            if (string.IsNullOrEmpty(teleportSound))
            {
                return;
            }

            new Sound(teleportSound)
                .SetSpatialSound(false)
                .SetVolume(0.7f)
                .SetRandomPitch()
                .Play();
        }

        private void ResetShowcaseState ()
        {
            if (!resetShowcaseStateOnTeleport)
            {
                return;
            }

            SoundsGoodManager.StopAll(audioFadeOutTime);
            FindShowcaseResettables();

            foreach (IShowcaseResettable resettable in resetBuffer)
            {
                if (IsButtonResettable(resettable))
                {
                    resettable.ResetShowcaseState();
                }
            }

            foreach (IShowcaseResettable resettable in resetBuffer)
            {
                if (!IsButtonResettable(resettable))
                {
                    resettable.ResetShowcaseState();
                }
            }

            resetBuffer.Clear();
        }

        private void RestoreInitialAutoPlayEmitters ()
        {
            if (!resetShowcaseStateOnTeleport)
            {
                return;
            }

#if UNITY_6000_0_OR_NEWER
            AudioEmitter[] emitters =
                FindObjectsByType<AudioEmitter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            AudioEmitter[] emitters = FindObjectsOfType<AudioEmitter>();
#endif

            foreach (AudioEmitter emitter in emitters)
            {
                if (emitter == null)
                {
                    continue;
                }

                bool isMusicZoneEmitter = emitter.GetComponent<MusicZone>() != null;
                if (!isMusicZoneEmitter && !HasPlayOnStartEnabled(emitter))
                {
                    continue;
                }

                emitter.Play();

                if (isMusicZoneEmitter)
                {
                    emitter.SetZoneVolume(0f);
                }
            }
        }

        private static bool HasPlayOnStartEnabled (AudioEmitter emitter)
        {
            System.Type emitterType = emitter.GetType();
            if (!PlayOnStartFields.TryGetValue(emitterType, out FieldInfo playOnStartField))
            {
                playOnStartField = emitterType.GetField("playOnStart", BindingFlags.Instance | BindingFlags.NonPublic);
                PlayOnStartFields.Add(emitterType, playOnStartField);
            }

            return playOnStartField != null
                && playOnStartField.FieldType == typeof(bool)
                && (bool)playOnStartField.GetValue(emitter);
        }

        private static bool IsButtonResettable (IShowcaseResettable resettable)
        {
            return resettable is SG_Button || resettable is SG_AlternatingButton;
        }

        private void FindShowcaseResettables ()
        {
            resetBuffer.Clear();

#if UNITY_6000_0_OR_NEWER
            MonoBehaviour[] behaviours =
                FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>(true);
#endif

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IShowcaseResettable resettable)
                {
                    resetBuffer.Add(resettable);
                }
            }
        }

        private IEnumerator FadeTransition (float targetAlpha, float duration)
        {
            if (transitionCanvasGroup == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                transitionCanvasGroup.alpha = targetAlpha;
                yield break;
            }

            float startAlpha = transitionCanvasGroup.alpha;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                transitionCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
                yield return null;
            }

            transitionCanvasGroup.alpha = targetAlpha;
        }

        private void SetTransitionBlocking (bool blocking)
        {
            if (transitionCanvasGroup == null)
            {
                return;
            }

            transitionCanvasGroup.blocksRaycasts = blocking;
        }

        private void FindPlayerIfNeeded ()
        {
            if (player != null)
            {
                return;
            }

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        private void CachePlayerReferences ()
        {
            if (player == null)
            {
                playerController = null;
                characterController = null;
                return;
            }

            playerController = player.GetComponent<SG_PlayerController>();
            characterController = player.GetComponent<CharacterController>();
        }
    }
}
