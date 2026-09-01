using UnityEngine;

public class Jugador : EntidadBase
{
    #region FisrtPersonMovement

    [SerializeField] private Transform Camara;
    [SerializeField][Range(0.0f, 0.5f)] private float MouseSmoothTime = 0.03f;
    [SerializeField] private bool CursorLock = true;
    [SerializeField] private float MouseSensitivity = 3.5f;
    [SerializeField] private float Gravity = -30;
    [SerializeField] private Transform GroundCheck;
    [SerializeField] private LayerMask Ground;

    [SerializeField] private float JumpHeight = 6;
    private float VerticalVelocity;
    private bool IsGrounded;

    private float CameraCap;
    private Vector2 CurrentMouseDelta;
    private Vector2 CurrentCameraVelocity;

    private CharacterController CharacterController;
    private Vector2 Direction;
    private Vector2 DirVelocity;
    private Vector3 Velocity;

    public HeadBobbing HeadBobbing;

    #endregion


    #region ShadowLookDetection

    [Header("Shadow Look Detection")]

    [SerializeField] private TheShadow Shadow;

    [SerializeField] private ShadowDistortionController DistortionController;

    [SerializeField] private float ShadowLookDistance = 2000f;

    [SerializeField] private float ShadowLookTimeToMax = 5f;

    [SerializeField] private float ShadowLookRecoverySpeed = 1f;


    [Header("Vision Angle")]

    // Ángulo máximo cuando La Sombra está lejos.
    [SerializeField] private float ShadowLookAngleFar = 45f;

    // Distancia desde donde usamos el ángulo máximo.
    [SerializeField] private float ShadowLookAngleFarDistance = 50f;

    // Ángulo máximo cuando La Sombra está cerca.
    [SerializeField] private float ShadowLookAngleNear = 8f;

    // Distancia donde comienza a cerrarse el ángulo.
    [SerializeField] private float ShadowLookAngleNearDistance = 10f;



    [Header("Shadow Visibility")]


    [SerializeField]
    [Range(0f, 1f)]
    private float ShadowLookIntensity;

    private float ShadowLookTimer;

    #endregion

    #region Animations

    [Header("Animator")]
    [SerializeField] private Animator animator;

    #endregion

    private void Awake()
    {
        NormalSpeed = Speed;
    }


    void Start()
    {
        CharacterController = GetComponent<CharacterController>();
        HeadBobbing = GetComponent<HeadBobbing>();

        if (CursorLock && InventoryManager.InvInstance.PauseGame == false)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }


    void Update()
    {
        if (InventoryManager.InvInstance.PauseGame == false)
        {
            MoveCamara();
            Movement();
            SprintAndCrouch();
            UpdateAnimatorParams();
            CheckShadowLook();
        }
    }


    #region MovementFunctions

    private void MoveCamara()
    {
        Vector2 targetMouseDelta = new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")
        );

        CurrentMouseDelta = Vector2.SmoothDamp(
            CurrentMouseDelta,
            targetMouseDelta,
            ref CurrentCameraVelocity,
            MouseSmoothTime
        );

        CameraCap -= CurrentMouseDelta.y * MouseSensitivity;

        CameraCap = Mathf.Clamp(
            CameraCap,
            -85f,
            85f
        );

        Camara.localEulerAngles =
            Vector3.right * CameraCap;

        transform.Rotate(
            Vector3.up *
            CurrentMouseDelta.x *
            MouseSensitivity
        );
    }


    private void Movement()
    {
        IsGrounded = Physics.CheckSphere(
            GroundCheck.position,
            0.2f,
            Ground
        );


        Vector2 targetDirection = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        targetDirection.Normalize();


        Direction = Vector2.SmoothDamp(
            Direction,
            targetDirection,
            ref DirVelocity,
            MovementSmoothTime
        );


        VerticalVelocity += Gravity * 2f * Time.deltaTime;


        Velocity =
            (transform.forward * Direction.y +
             transform.right * Direction.x) * Speed +
             Vector3.up * VerticalVelocity;


        CharacterController.Move(
            Velocity * Time.deltaTime
        );


        if (IsGrounded && Input.GetButtonDown("Jump"))
        {
            VerticalVelocity = Mathf.Sqrt(
                JumpHeight * -2f * Gravity
            );
        }


        if (IsGrounded! &&
            CharacterController.velocity.y < -1)
        {
            VerticalVelocity = -8;
        }
    }


    private void SprintAndCrouch()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            Speed = HighSpeed;

            HeadBobbing.Frecuencia = 25;
            HeadBobbing.Amplitud = 0.015f;
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            Speed = LowSpeed;

            HeadBobbing.Frecuencia = 10;
            HeadBobbing.Amplitud = 0.005f;
        }
        else
        {
            Speed = NormalSpeed;

            HeadBobbing.Frecuencia = 15;
            HeadBobbing.Amplitud = 0.01f;
        }
    }

    #endregion


    #region ShadowLookFunctions

    private void CheckShadowLook()
    {
        if (Shadow == null)
        {
            ShadowLookIntensity = 0f;
            ShadowLookTimer = 0f;

            SendLookIntensity();

            return;
        }


        // =====================================================
        // DISTANCIA
        // =====================================================

        Vector3 shadowCenter =
            Shadow.transform.position;

        float distanceToShadow =
            Vector3.Distance(
                Camara.position,
                shadowCenter
            );


        if (distanceToShadow > ShadowLookDistance)
        {
            RecoverShadowLook();

            return;
        }


        // =====================================================
        // ÁNGULO DINÁMICO
        // =====================================================

        float currentLookAngle = Mathf.Lerp(
            ShadowLookAngleNear,
            ShadowLookAngleFar,
            Mathf.InverseLerp(
                ShadowLookAngleNearDistance,
                ShadowLookAngleFarDistance,
                distanceToShadow
            )
        );


        // =====================================================
        // COMPROBAR VARIOS PUNTOS DE LA SOMBRA
        // =====================================================

        bool canSeeShadow = CheckShadowVisibility(
            shadowCenter,
            currentLookAngle
        );


        if (!canSeeShadow)
        {
            RecoverShadowLook();

            return;
        }


        // =====================================================
        // ESTAMOS MIRANDO A LA SOMBRA
        // =====================================================

        ShadowLookTimer += Time.deltaTime;


        ShadowLookIntensity = Mathf.Clamp01(
            ShadowLookTimer /
            ShadowLookTimeToMax
        );


        SendLookIntensity();
    }


    private bool CheckShadowVisibility(
    Vector3 shadowCenter,
    float lookAngle
)
    {
        // =====================================================
        // BUSCAR LOS COLLIDERS DE LA SOMBRA
        // =====================================================

        Collider[] shadowColliders =
            Shadow.GetComponentsInChildren<Collider>();


        // =====================================================
        // COMPROBAR CADA COLLIDER
        // =====================================================

        foreach (Collider shadowCollider in shadowColliders)
        {
            if (shadowCollider == null)
                continue;


            // Punto del collider más cercano a la cámara.
            Vector3 closestPoint =
                shadowCollider.ClosestPoint(
                    Camara.position
                );


            Vector3 direction =
                closestPoint - Camara.position;


            float distance =
                direction.magnitude;


            if (distance <= 0.01f)
                continue;


            // =================================================
            // ÁNGULO
            // =================================================

            direction.Normalize();


            float angle = Vector3.Angle(
                Camara.forward,
                direction
            );


            // Está dentro de nuestro campo de visión.
            if (angle <= lookAngle)
            {
                return true;
            }
        }


        return false;
    }


    private void RecoverShadowLook()
    {
        ShadowLookTimer = Mathf.MoveTowards(
            ShadowLookTimer,
            0f,
            ShadowLookRecoverySpeed *
            Time.deltaTime
        );


        ShadowLookIntensity = Mathf.Clamp01(
            ShadowLookTimer /
            ShadowLookTimeToMax
        );


        SendLookIntensity();
    }


    private void SendLookIntensity()
    {
        if (DistortionController == null)
            return;


        DistortionController.SetLookIntensity(
            ShadowLookIntensity
        );
    }

    #endregion

    #region AnimationFunctions

    private void UpdateAnimatorParams()
    {
        bool isMoving = Direction.magnitude > 0.1f;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        bool isCrouching = Input.GetKey(KeyCode.LeftControl);

        animator.SetBool("Walking", isMoving && !isSprinting);
        animator.SetBool("Running", isMoving && isSprinting);
        animator.SetBool("IsCrouch", isCrouching);
        animator.SetBool("Jump", !IsGrounded);
    }

    #endregion

    public void TakeDamage(int damage)
    {
        HP -= damage;

        if (HP <= 0)
        {
            animator.SetBool("DeathAnim", true);
            enabled = false;
        }
    }
}