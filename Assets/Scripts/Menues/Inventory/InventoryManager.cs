using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager InvInstance;

    public GameObject eventSystem;
    public GameObject Inventory;
    public bool PauseGame;
    public List<GameObject> ObjectsInInv = new List<GameObject>();

    [Header("Cameras")]
    public Camera mainCamera;
    public Camera inspectionCamera;

    [Header("Inspection System")]
    public Transform inspectionPoint;
    public CanvasGroup inventoryCanvasGroup;
    public InspectController controller;
    private bool isInspecting = false;
    public Vector3 inspectPosition = new Vector3(0, 0, 0f);
    public float inspectScale;

    private List<GameObject> inspectedObjects = new List<GameObject>();

    private void Awake()
    {
        InvInstance = this;

        inspectionCamera.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isInspecting)
            {
                ExitInspection();
                return;
            }

            PauseGame = !PauseGame;
            Inventory.SetActive(PauseGame);

            if (PauseGame)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if (isInspecting && Input.GetMouseButtonDown(1))
        {
            ExitInspection();
        }
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    public void InspectObject(string itemName)
    {
        isInspecting = true;

        eventSystem.SetActive(false);
        Inventory.SetActive(false);

        mainCamera.gameObject.SetActive(false);
        inspectionCamera.gameObject.SetActive(true);

        foreach (GameObject obj in inspectedObjects)
        {
            if (obj.name.Contains(itemName))
            {
                obj.SetActive(true);
                AssignController(obj);
                return;
            }
        }

        GameObject prefab = Resources.Load<GameObject>(itemName);

        if (prefab == null)
        {
            Debug.LogError("No se encontró prefab: " + itemName);
            return;
        }

        GameObject newObj = Instantiate(prefab, inspectionPoint);
        newObj.transform.localPosition = inspectPosition;
        newObj.transform.localRotation = Quaternion.identity;
        newObj.transform.localScale = Vector3.one * inspectScale;

        SetLayerRecursively(newObj, LayerMask.NameToLayer("Inspectable"));

        inspectedObjects.Add(newObj);

        AssignController(newObj);
    }

    void AssignController(GameObject obj)
    {
        if (controller != null)
        {
            controller.target = obj.transform;
        }
    }

    public void ExitInspection()
    {
        isInspecting = false;

        eventSystem.SetActive(true);

        mainCamera.gameObject.SetActive(true);
        inspectionCamera.gameObject.SetActive(false);

        foreach (GameObject obj in inspectedObjects)
        {
            obj.SetActive(false);
        }

        if (controller != null)
        {
            controller.target = null;
        }

        if (PauseGame)
        {
            Inventory.SetActive(true);
        }
    }
}
