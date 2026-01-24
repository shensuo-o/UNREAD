using UnityEngine;

public class Jugador : EntidadBase
{
    [SerializeField] private Transform Camara; //Transform de la camara del Jugador.
    [SerializeField] [Range(0.0f, 0.5f)] private float MouseSmoothTime = 0.03f; //Suavizado del movimiento del mause. Con un slider.
    [SerializeField] private bool CursorLock = true; //Variable para setear el cursor fijo.
    [SerializeField] private float MouseSensitivity = 3.5f; //Sensibilidad del mause.
    private float HorizontalInput;
    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
