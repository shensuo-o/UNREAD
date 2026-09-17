using System.Globalization;
using TMPro;
using UnityEngine;

namespace MelenitasDev.SoundsGood.Demo
{
    [RequireComponent(typeof(CharacterController))]
    public class SG_PlayerController : MonoBehaviour
    {
        // ----- Serialized Fields
        [Header("References")]
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private Transform cameraTransform;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float runSpeed = 7f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float gravity = -18f;
        [SerializeField] private float jumpHeight = 1.5f;

        [Header("Mouse Look")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float verticalClamp = 85f;
        [SerializeField] private float mouseSmooth = 12f;

        [Header("Head Bobbing")]
        [SerializeField] private float bobFrequency = 1.8f;
        [SerializeField] private float bobAmplitude = 0.05f;
        [SerializeField] private float bobSmoothing = 10f;

        [Header("Tilt & Sway")]
        [SerializeField] private float tiltAngle = 5f;
        [SerializeField] private float maxTiltAngle = 6f;
        [SerializeField] private float swayAmount = 1.5f;
        [SerializeField] private float maxSwayAngle = 2.5f;
        [SerializeField] private float swaySmoothing = 10f;

        [Header("Time Scale UI")]
        [SerializeField] private GameObject timeScaleIndicator;
        [SerializeField] private TMP_Text timeScaleIndicatorText;

        // ----- Fields
        // Invariant, not the machine's locale: the readout has to say "x1.5" everywhere.
        private static readonly CultureInfo TimeScaleCulture = CultureInfo.InvariantCulture;

        private CharacterController controller;

        private float yaw;
        private float pitch;
        private float smoothedYaw;
        private float smoothedPitch;

        private Vector3 velocity;
        private Vector3 moveVelocity;

        private float bobTimer;
        private Vector3 cameraRootDefaultLocalPos;

        private Vector3 targetLocalEuler;
        private Vector3 currentLocalEuler;

        private bool timeScaleIndicatorVisible;
        private float displayedTimeScale = -1f;

        // ----- Unity Events
        void Awake ()
        {
            controller = GetComponent<CharacterController>();

            if (cameraRoot == null && Camera.main != null)
            {
                cameraRoot = Camera.main.transform;
            }

            if (cameraTransform == null && cameraRoot != null)
            {
                cameraTransform = cameraRoot;
            }

            if (cameraRoot != null)
            {
                cameraRootDefaultLocalPos = cameraRoot.localPosition;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            float startYaw = transform.eulerAngles.y;
            yaw = startYaw;
            smoothedYaw = startYaw;
            pitch = 0f;
            smoothedPitch = 0f;
            currentLocalEuler = Vector3.zero;

            SetTimeScaleIndicatorVisible(false);
        }

        void Update ()
        {
            HandleMouseLook();
            HandleMovement();
            HandleHeadBob();
            HandleTiltAndSway();
            UpdateTimeScaleIndicator();
        }

        // ----- Public Methods
        public void TeleportTo (Transform target)
        {
            if (target == null)
            {
                return;
            }

            TeleportTo(target.position, target.rotation);
        }

        public void TeleportTo (Vector3 position, Quaternion rotation)
        {
            if (controller == null)
            {
                controller = GetComponent<CharacterController>();
            }

            bool controllerWasEnabled = controller != null && controller.enabled;
            if (controllerWasEnabled)
            {
                controller.enabled = false;
            }

            float targetYaw = rotation.eulerAngles.y;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, targetYaw, 0f));

            ResetMovement();
            ResetLook(targetYaw);

            if (controllerWasEnabled)
            {
                controller.enabled = true;
            }
        }

        public void SetTimeScaleIndicatorVisible (bool visible)
        {
            timeScaleIndicatorVisible = visible;

            GameObject indicator = GetTimeScaleIndicator();
            if (indicator != null)
            {
                indicator.SetActive(visible);
            }

            if (visible)
            {
                displayedTimeScale = -1f;
                UpdateTimeScaleIndicator();
            }
        }

        // ----- Private Methods
        private GameObject GetTimeScaleIndicator ()
        {
            if (timeScaleIndicator != null)
            {
                return timeScaleIndicator;
            }

            return timeScaleIndicatorText != null ? timeScaleIndicatorText.gameObject : null;
        }

        private void UpdateTimeScaleIndicator ()
        {
            if (!timeScaleIndicatorVisible || timeScaleIndicatorText == null)
            {
                return;
            }

            float roundedTimeScale = Mathf.Round(Time.timeScale * 10f) * 0.1f;
            if (Mathf.Approximately(roundedTimeScale, displayedTimeScale))
            {
                return;
            }

            displayedTimeScale = roundedTimeScale;
            timeScaleIndicatorText.text = "x" + roundedTimeScale.ToString("0.0", TimeScaleCulture);
        }

        private void HandleMouseLook ()
        {
            Vector2 look = SG_Input.GetLookDelta();
            float mouseX = look.x * mouseSensitivity;
            float mouseY = look.y * mouseSensitivity;
            float smoothingFactor = GetSmoothingFactor(mouseSmooth, Time.unscaledDeltaTime);
            
            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -verticalClamp, verticalClamp);
            
            smoothedYaw = Mathf.LerpAngle(smoothedYaw, yaw, smoothingFactor);
            smoothedPitch = Mathf.Lerp(smoothedPitch, pitch, smoothingFactor);
            
            transform.rotation = Quaternion.Euler(0f, smoothedYaw, 0f);
        }

        private void HandleMovement ()
        {
            bool isGrounded = controller.isGrounded;

            Vector2 move = SG_Input.GetMoveAxes();
            float inputX = move.x;
            float inputZ = move.y;

            Vector3 inputDir = new Vector3(inputX, 0f, inputZ);
            inputDir = Vector3.ClampMagnitude(inputDir, 1f);

            float targetSpeed = SG_Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
            Vector3 targetVelocity = transform.TransformDirection(inputDir) * targetSpeed;
            
            moveVelocity = Vector3.Lerp(moveVelocity, targetVelocity, acceleration * Time.deltaTime);
            
            if (isGrounded && velocity.y < 0f)
            {
                velocity.y = -2f;
            }

            if (isGrounded && SG_Input.GetJumpDown())
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            velocity.y += gravity * Time.deltaTime;

            Vector3 finalMove = moveVelocity;
            finalMove.y = velocity.y;

            controller.Move(finalMove * Time.deltaTime);
        }

        private void HandleHeadBob ()
        {
            if (cameraRoot == null) return;

            Vector3 horizontalVel = moveVelocity;
            horizontalVel.y = 0f;

            float speed = horizontalVel.magnitude;
            bool isMoving = speed > 0.1f && controller.isGrounded;
            
            if (isMoving)
            {
                float speedFactor = speed / walkSpeed;
                bobTimer += Time.deltaTime * bobFrequency * Mathf.Max(speedFactor, 0.1f);
            }

            float bobOffset = isMoving
                ? Mathf.Sin(bobTimer * Mathf.PI * 2f) * bobAmplitude
                : 0f;

            Vector3 targetPos = cameraRootDefaultLocalPos + new Vector3(0f, bobOffset, 0f);

            cameraRoot.localPosition = Vector3.Lerp(
                cameraRoot.localPosition,
                targetPos,
                Time.deltaTime * bobSmoothing
            );
        }

        private void HandleTiltAndSway ()
        {
            if (cameraRoot == null) return;
            
            Vector2 look = SG_Input.GetLookDelta();
            float mouseX = look.x;
            float mouseY = look.y;
            float tiltLimit = Mathf.Max(0f, maxTiltAngle);
            float swayLimit = Mathf.Max(0f, maxSwayAngle);
            
            float targetRoll = Mathf.Clamp(-mouseX * tiltAngle, -tiltLimit, tiltLimit);
            
            float targetSwayYaw = Mathf.Clamp(mouseX * swayAmount, -swayLimit, swayLimit);
            float targetSwayPitch = Mathf.Clamp(-mouseY * swayAmount * 0.5f, -swayLimit, swayLimit);
            
            targetLocalEuler.x = Mathf.Clamp(smoothedPitch + targetSwayPitch, -verticalClamp, verticalClamp);
            targetLocalEuler.y = targetSwayYaw;
            targetLocalEuler.z = targetRoll;

            currentLocalEuler = Vector3.Lerp(
                currentLocalEuler,
                targetLocalEuler,
                GetSmoothingFactor(swaySmoothing, Time.unscaledDeltaTime)
            );

            cameraRoot.localRotation = Quaternion.Euler(currentLocalEuler);
        }

        private static float GetSmoothingFactor (float smoothing, float deltaTime)
        {
            if (smoothing <= 0f || deltaTime <= 0f)
            {
                return 0f;
            }

            return 1f - Mathf.Exp(-smoothing * deltaTime);
        }

        private void ResetMovement ()
        {
            velocity = Vector3.zero;
            moveVelocity = Vector3.zero;
            bobTimer = 0f;

            if (cameraRoot != null)
            {
                cameraRoot.localPosition = cameraRootDefaultLocalPos;
            }
        }

        private void ResetLook (float targetYaw)
        {
            yaw = targetYaw;
            smoothedYaw = targetYaw;
            pitch = 0f;
            smoothedPitch = 0f;
            targetLocalEuler = Vector3.zero;
            currentLocalEuler = Vector3.zero;

            if (cameraRoot != null)
            {
                cameraRoot.localRotation = Quaternion.identity;
            }
        }
    }
}
