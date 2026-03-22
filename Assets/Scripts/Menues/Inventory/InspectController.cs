using UnityEngine;

public class InspectController : MonoBehaviour
{
    public Transform target;

    public float rotationSpeed = 5f;
    public float zoomSpeed = 2f;
    public float minDistance = 1f;
    public float maxDistance = 5f;
    public float autoRotateSpeed = 10f;

    private float currentDistance = 2f;

    void Update()
    {
        if (target == null) return;

        HandleRotation();
        HandleZoom();
        HandleExit();
    }

    void HandleRotation()
    {
        if (Input.GetMouseButton(0))
        {
            float rotX = Input.GetAxisRaw("Mouse X") * rotationSpeed;
            float rotY = Input.GetAxisRaw("Mouse Y") * rotationSpeed;

            target.Rotate(Vector3.up, -rotX, Space.Self);
            target.Rotate(Vector3.right, rotY, Space.Self);
        }
        else
        {
            target.Rotate(Vector3.up, autoRotateSpeed * Time.deltaTime, Space.Self);
        }
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
        }
    }
}
