using UnityEngine;

namespace MelenitasDev.SoundsGood.Demo
{
    [DisallowMultipleComponent]
    public class SG_TimeScaleShowcase : MonoBehaviour, IShowcaseResettable
    {
        // ----- Serialized Fields
        [Header("Time Scale")]
        [SerializeField, Min(0.001f)] private float timeScaleStep = 0.1f;
        [SerializeField, Min(0.001f)] private float minimumTimeScale = 0.1f;
        [SerializeField, Min(0.001f)] private float normalTimeScale = 1f;
        [SerializeField, Min(0.001f)] private float maximumTimeScale = 3f;
        [SerializeField] private bool updateFixedDeltaTime = true;

        [Header("Speakers")]
        [SerializeField] private MusicEmitter timeScaleSpeaker;
        [SerializeField] private MusicEmitter noTimeScaleSpeaker;

        [Header("Trigger Reset")]
        [SerializeField] private bool resetOnTriggerExit = true;
        [SerializeField] private bool resetOnDisable = true;
        [SerializeField] private SG_PlayerController player;

        // ----- Fields
        private float defaultFixedDeltaTime;
        private bool initialized;
        private bool timeScaleChanged;
        private SG_PlayerController activePlayer;

        // ----- Unity Events
        void Awake () { Initialize(); }

        void OnTriggerEnter (Collider other)
        {
            SG_PlayerController targetPlayer = GetTargetPlayer(other);
            if (targetPlayer == null)
            {
                return;
            }

            activePlayer = targetPlayer;
            activePlayer.SetTimeScaleIndicatorVisible(true);
        }

        void OnTriggerExit (Collider other)
        {
            SG_PlayerController targetPlayer = GetTargetPlayer(other);
            if (targetPlayer == null)
            {
                return;
            }

            if (resetOnTriggerExit)
            {
                SetNormalTimeScale();
            }

            targetPlayer.SetTimeScaleIndicatorVisible(false);

            if (activePlayer == targetPlayer)
            {
                activePlayer = null;
            }
        }

        void OnDisable ()
        {
            HideActiveTimeScaleIndicator();

            if (!resetOnDisable || !timeScaleChanged)
            {
                return;
            }

            SetNormalTimeScale();
        }

        // ----- Public Methods
        public void IncreaseTimeScale () { SetTimeScale(GetSteppedTimeScale(timeScaleStep)); }

        public void DecreaseTimeScale () { SetTimeScale(GetSteppedTimeScale(-timeScaleStep)); }

        public void SetNormalTimeScale () { SetTimeScale(normalTimeScale); }

        public void SetFastTimeScale () { IncreaseTimeScale(); }

        public void SetSlowTimeScale () { DecreaseTimeScale(); }

        public void SpeedUpTimeScale () { IncreaseTimeScale(); }

        public void SlowDownTimeScale () { DecreaseTimeScale(); }

        public void ResetTimeScale () { SetNormalTimeScale(); }

        public void ResetShowcaseState ()
        {
            SetNormalTimeScale();
            StopSpeaker(timeScaleSpeaker);
            StopSpeaker(noTimeScaleSpeaker);
        }

        public void ActivateTimeScaleSpeaker () { PlaySpeaker(timeScaleSpeaker, noTimeScaleSpeaker); }

        public void ActivateNoTimeScaleSpeaker () { PlaySpeaker(noTimeScaleSpeaker, timeScaleSpeaker); }

        public void PlayTimeScaleSpeaker () { ActivateTimeScaleSpeaker(); }

        public void PlayNoTimeScaleSpeaker () { ActivateNoTimeScaleSpeaker(); }

        // ----- Private Methods
        private void Initialize ()
        {
            if (initialized)
            {
                return;
            }

            defaultFixedDeltaTime = Time.fixedDeltaTime;
            initialized = true;
        }

        private float GetSteppedTimeScale (float step)
        {
            float nextScale = Time.timeScale + step;
            float scaleStep = Mathf.Max(0.001f, timeScaleStep);
            return Mathf.Round(nextScale / scaleStep) * scaleStep;
        }

        private void SetTimeScale (float value)
        {
            Initialize();

            float minScale = Mathf.Max(0.001f, minimumTimeScale);
            float maxScale = Mathf.Max(minScale, maximumTimeScale);
            float scale = Mathf.Clamp(value, minScale, maxScale);
            Time.timeScale = scale;

            if (updateFixedDeltaTime)
            {
                Time.fixedDeltaTime = defaultFixedDeltaTime * scale;
            }

            timeScaleChanged = !Mathf.Approximately(scale, normalTimeScale);
        }

        private static void PlaySpeaker (MusicEmitter speakerToPlay, MusicEmitter speakerToStop)
        {
            if (speakerToStop != null)
            {
                speakerToStop.Stop();
            }

            if (speakerToPlay != null)
            {
                speakerToPlay.Play();
            }
        }

        private static void StopSpeaker (MusicEmitter speaker)
        {
            if (speaker != null)
            {
                speaker.Stop();
            }
        }

        private SG_PlayerController GetTargetPlayer (Collider other)
        {
            if (other == null)
            {
                return null;
            }

            SG_PlayerController otherPlayer = FindPlayerController(other);
            if (player == null)
            {
                return otherPlayer;
            }

            return otherPlayer == player ? player : null;
        }

        private void HideActiveTimeScaleIndicator ()
        {
            SG_PlayerController targetPlayer = activePlayer != null ? activePlayer : player;
            if (targetPlayer != null)
            {
                targetPlayer.SetTimeScaleIndicatorVisible(false);
            }

            activePlayer = null;
        }

        private static SG_PlayerController FindPlayerController (Collider other)
        {
            SG_PlayerController playerController = other.GetComponentInParent<SG_PlayerController>();
            if (playerController != null)
            {
                return playerController;
            }

            Rigidbody attachedRigidbody = other.attachedRigidbody;
            if (attachedRigidbody != null)
            {
                playerController = attachedRigidbody.GetComponentInParent<SG_PlayerController>();
                if (playerController != null)
                {
                    return playerController;
                }

                playerController = attachedRigidbody.GetComponentInChildren<SG_PlayerController>();
                if (playerController != null)
                {
                    return playerController;
                }
            }

            return other.transform.root.GetComponentInChildren<SG_PlayerController>();
        }
    }
}
