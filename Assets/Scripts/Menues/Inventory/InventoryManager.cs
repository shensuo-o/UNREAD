using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager InvInstance;
    public GameObject Inventory;
    public bool PauseGame;
    public List<GameObject> ObjectsInInv = new List<GameObject>();

    [Header("Inspection System")]
    public GameObject inspectionPanel; // UI donde se ve el objeto
    public Transform inspectionPoint;  // donde aparece el objeto
    public Camera inspectionCamera;
    public InspectController controller;
    public CanvasGroup inventoryCanvasGroup;

    private List<GameObject> inspectedObjects = new List<GameObject>();

    private void Awake()
    {
        InvInstance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            PauseGame = !PauseGame;
            Inventory.SetActive(PauseGame);
            if (PauseGame)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
            }else if (!PauseGame)
            {
                Cursor.lockState = CursorLockMode.Locked; //Cursor no se mueve
                Cursor.visible = false; //Cursor no se ve
                ExitInspection();
            }
            
        }

        if (inspectionPanel.activeSelf && Input.GetMouseButtonDown(1))
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
        inventoryCanvasGroup.blocksRaycasts = false;
        inventoryCanvasGroup.interactable = false;

        inspectionPanel.SetActive(true);
        inspectionCamera.gameObject.SetActive(true);

        foreach (GameObject obj in inspectedObjects)
        {
            if (obj.name.Contains(itemName))
            {
                obj.SetActive(true);
                return;
            }
        }

        GameObject prefab = Resources.Load<GameObject>(itemName);

        if (prefab == null)
        {
            Debug.LogError("No se encontró prefab:" + itemName);
            return;
        }

        GameObject newObj = Instantiate(prefab, inspectionPoint.position, Quaternion.identity);

        newObj.transform.SetParent(inspectionPoint);
        newObj.transform.localPosition = new Vector3(0, 0, 2f);
        newObj.transform.localRotation = Quaternion.identity;
        newObj.transform.localScale = new Vector3(100,100,100);

        SetLayerRecursively(newObj, LayerMask.NameToLayer("Inspectable"));

        inspectedObjects.Add(newObj);

        InspectController controller = inspectionCamera.GetComponent<InspectController>();

        if (controller != null)
        {
            controller.target = newObj.transform;
        }
    }

    public void ExitInspection()
    {
        inspectionPanel.SetActive(false);
        inspectionCamera.gameObject.SetActive(false);

        RenderTexture.active = inspectionCamera.targetTexture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = null;

        inventoryCanvasGroup.blocksRaycasts = true;
        inventoryCanvasGroup.interactable = true;

        foreach (GameObject obj in inspectedObjects)
        {
            obj.SetActive(false);
        }

        InspectController controller = inspectionCamera.GetComponent<InspectController>();
        if (controller != null)
        {
            controller.target = null;
        }
    }

}
