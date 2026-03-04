using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager InvInstance;
    public GameObject Inventory;
    public bool PauseGame;
    public List<GameObject> ObjectsInInv = new List<GameObject>();

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
                Cursor.lockState = CursorLockMode.Locked; //Cursor no se mueve.
                Cursor.visible = false; //Cursor no se ve.
            }
            
        }
    }
}
