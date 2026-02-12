using UnityEngine;

public class HeadBobbing : MonoBehaviour
{
    [SerializeField] private bool Enabled = true;

    [SerializeField, Range(0, 0.1f)] private float Amplitud = 0.015f;
    [SerializeField, Range(0, 30f)] private float Frecuencia = 10f;

    [SerializeField] private Transform Camara = null;
    [SerializeField] private Transform CameraHolder = null;

    [SerializeField] private float ToggleSpeed = 3;
    private Vector3 StartPos;
    private CharacterController Controller;

    void Start()
    {
        Controller = GetComponent<CharacterController>();
        StartPos = Camara.localPosition;
    }

    void Update()
    {

    }

    private Vector3 FootStepMotion()
    {
        Vector3 pos = Vector3.zero;
        pos.y += Mathf.Sin(Time.time * Frecuencia) * Amplitud;
        pos.x += Mathf.Cos(Time.time * Frecuencia / 2) * Amplitud * 2;
        return pos;
    }

    private void CheckMotion()
    {
        float speed = new Vector3(Controller.velocity.x, 0, Controller.velocity.z).magnitude;

        if (speed < ToggleSpeed)
        {
            return;
        }
        if (Controller.isGrounded == false)
        {
            return;
        }

        FootStepMotion();
    }

    private void ResetPosition()
    {
        
    }
}
