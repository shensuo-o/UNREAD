using UnityEngine;

public class HeadBobbing : MonoBehaviour
{
    [SerializeField] private bool Enabled = true;

    [Range(0, 0.1f)] public float Amplitud = 0.015f;
    [Range(0, 30f)] public float Frecuencia = 10f;

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
        if (!Enabled)
        {
            return;
        }
        else
        {
            CheckMotion();
            ResetPosition();
            Camara.LookAt(FocusTarget());
        }
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

        PlayMotion(FootStepMotion());
    }

    private void PlayMotion(Vector3 motion)
    {
        Camara.localPosition += motion;
    }

    private void ResetPosition()
    {
        if (Camara.localPosition == StartPos) return;
        Camara.localPosition = Vector3.Lerp (Camara.localPosition, StartPos, 1 * Time.deltaTime);
    }

    private Vector3 FocusTarget()
    {
        Vector3 pos = new Vector3 (transform.position.x, transform.position.y + CameraHolder.localPosition.y, transform.position.z);
        pos += CameraHolder.forward * 15f;
        return pos;
    }
}
