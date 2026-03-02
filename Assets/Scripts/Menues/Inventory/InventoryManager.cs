using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager InvInstance;
    public GameObject Inventory;
    public bool PauseGame;
    public GameObject[] ObjectsInInv;

    private void Awake()
    {
        InvInstance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
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
