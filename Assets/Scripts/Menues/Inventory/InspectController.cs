using UnityEngine;
using UnityEngine.EventSystems;

public class InspectController : MonoBehaviour
{
    public Transform target;
    public float rotationSpeed = 5f;
    public float zoomSpeed = 2f;
    public float minDistance = 1f;
    public float maxDistance = 5f;

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

            float rotX = Input.GetAxis("Mouse X") * rotationSpeed;
            float rotY = Input.GetAxis("Mouse Y") * rotationSpeed;

            target.Rotate(Vector3.up, -rotX, Space.World);
            target.Rotate(Vector3.right, rotY, Space.World);
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
}
