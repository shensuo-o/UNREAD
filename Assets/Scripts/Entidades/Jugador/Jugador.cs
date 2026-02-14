using UnityEngine;

public class Jugador : EntidadBase
{
    [SerializeField] private Transform Camara; //Transform de la camara del Jugador.
    [SerializeField] [Range(0.0f, 0.5f)] private float MouseSmoothTime = 0.03f; //Suavizado del movimiento del mause. Con un slider.
    [SerializeField] private bool CursorLock = true; //Variable para setear el cursor fijo.
    [SerializeField] private float MouseSensitivity = 3.5f; //Sensibilidad del mause.
    [SerializeField] private float Gravity = -30; //Gravedad.
    [SerializeField] private Transform GroundCheck; //Transform del sensor del piso.
    [SerializeField] private LayerMask Ground; //Layers que cuentan como piso.

    [SerializeField] private float JumpHeight = 6; //Altura de salto.
    private float VerticalVelocity; //Velocidad vertical.
    private bool IsGrounded; //Si esta, o no, en el piso.

    private float CameraCap; 
    private Vector2 CurrentMouseDelta;
    private Vector2 CurrentCameraVelocity;

    private CharacterController CharacterController;
    private Vector2 Direction;
    private Vector2 DirVelocity;
    private Vector3 Velocity;

    [SerializeField] private HeadBobbing HeadBobbing;

    private void Awake()
    {
        NormalSpeed = Speed; //Set normal speed.
    }

    void Start()
    {
        CharacterController = GetComponent<CharacterController>(); //Setear Character Controller.
        HeadBobbing = GetComponent<HeadBobbing>();

        if (CursorLock)
        {
            Cursor.lockState = CursorLockMode.Locked; //Cursor no se mueve.
            Cursor.visible = false; //Cursor no se ve.
        }
    }

    void Update()
    {
        MoveCamara();
        Movement();
        SprintAndCrouch();
    }

    private void MoveCamara() //Movimiento de Camara.
    {
        Vector2 targetMouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); //Input del movimiento del mouse.
        CurrentMouseDelta = Vector2.SmoothDamp(CurrentMouseDelta, targetMouseDelta, ref CurrentCameraVelocity, MouseSmoothTime); //Calcular el movimiento relativo a la velocidad de la camara y del movmiento del mouse.
        CameraCap -= CurrentMouseDelta.y * MouseSensitivity; //Aplicar sensibilidad.
        CameraCap = Mathf.Clamp(CameraCap, -85f, 85f); //Clamp para que no se pase de largo la camara en Y.
        Camara.localEulerAngles = Vector3.right * CameraCap; //Mover la camara en Y.
        transform.Rotate(Vector3.up * CurrentMouseDelta.x * MouseSensitivity); //Mover la camara en X.
    }

    private void Movement()
    {
        IsGrounded = Physics.CheckSphere(GroundCheck.position, 0.2f, Ground); //Check de si esta tocando el piso.

        Vector2 targetDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")); //Input de WASD, vertial y horizntal.
        targetDirection.Normalize();
        Direction = Vector2.SmoothDamp(Direction, targetDirection, ref DirVelocity, MovementSmoothTime); //Calcular direccion del movimiento.
        VerticalVelocity += Gravity * 2f * Time.deltaTime; //Velocidad de salto.
        Velocity = (transform.forward * Direction.y + transform.right * Direction.x) * Speed + Vector3.up * VerticalVelocity; //Velocidad de movimiento total.
        CharacterController.Move(Velocity*Time.deltaTime); //Mover al personaje.

        if (IsGrounded && Input.GetButtonDown("Jump"))
        {
            VerticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
        }

        if(IsGrounded! && CharacterController.velocity.y < -1)
        {
            VerticalVelocity = -8;
        }
    }

    private void SprintAndCrouch() //Se utiliza para agacharse y correr. Aca se llamaria a los cambios de animacion y efectos de cada uno.
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
}
