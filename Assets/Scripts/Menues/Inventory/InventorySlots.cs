using UnityEngine;
using UnityEngine.UI;

public class InventorySlots : MonoBehaviour
{
    public GameObject ObjectHeld;
    public Image ObjectPicture;
    public string ObjectName;
    public bool IsEquipSlot;

    public void OnGetItem(string itemName, GameObject item, Image itemPicture)
    {
        itemName = ObjectName;
        ObjectPicture = itemPicture;
        ObjectHeld = item;
    }
}
