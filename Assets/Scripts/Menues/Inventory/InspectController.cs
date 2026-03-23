using UnityEngine;

public class InspectController : MonoBehaviour
{
    public Transform target;

    public float rotationSpeed = 5f;
    public float zoomSpeed = 2f;
    public float minDistance = 1f;
    public float maxDistance = 5f;
    public float autoRotateSpeed = 10f;

    [Header("Inertia")]
    public float inertiaDamping = 5f;

    private float currentDistance = 2f;

    private Vector2 rotationVelocity;

    void Update()
    {
        if (target == null) return;

        HandleRotation();
        HandleZoom();
        HandleExit();
    }

    void HandleRotation()
    {
        bool isDragging = Input.GetMouseButton(0);

        if (isDragging)
        {
            float rotX = Input.GetAxisRaw("Mouse X");
            float rotY = Input.GetAxisRaw("Mouse Y");

            if (Mathf.Abs(rotX) > 0.01f || Mathf.Abs(rotY) > 0.01f)
            {
                rotationVelocity.x += rotX * rotationSpeed;
                rotationVelocity.y += rotY * rotationSpeed;
            }
        }
        else
        {
            rotationVelocity = Vector2.Lerp(rotationVelocity, Vector2.zero, inertiaDamping * Time.deltaTime);
        }

        float finalRotX = (isDragging ? 0f : autoRotateSpeed) + rotationVelocity.x;
        float finalRotY = rotationVelocity.y;

        target.Rotate(Vector3.up, -finalRotX * Time.deltaTime, Space.Self);
        target.Rotate(Vector3.right, finalRotY * Time.deltaTime, Space.Self);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0f)
        {
            currentDistance -= scroll * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

            target.localPosition = new Vector3(0, 0, currentDistance);
        }
    }

    void HandleExit()
    {
        if (Input.GetMouseButtonDown(1))
        {
            InventoryManager.InvInstance.ExitInspection();
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        if (target != null)
        {
            target.localRotation = Quaternion.identity;
            target.localPosition = new Vector3(0, 0, currentDistance);

            rotationVelocity = Vector2.zero;
        }
    }
}
