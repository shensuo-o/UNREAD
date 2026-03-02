using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IDropHandler
{
    public bool IsEquipment;
    public void OnDrop(PointerEventData eventData)
    {
        if (transform.childCount == 0)
        {
            GameObject dropped = eventData.pointerDrag;
            DragableItem draggableItem = dropped.GetComponent<DragableItem>();
            draggableItem.ParentAfterDrag = transform;

            if (IsEquipment)
            {
                foreach(GameObject obj in InventoryManager.InvInstance.ObjectsInInv)
                {
                    if (obj.name == draggableItem.Image.texture.name)
                    {
                        obj.SetActive(true);
                    }
                }
            }else if (!IsEquipment)
            {
                foreach (GameObject obj in InventoryManager.InvInstance.ObjectsInInv)
                {
                    if (obj.name == draggableItem.Image.texture.name)
                    {
                        obj.SetActive(false);
                    }
                }
            }
        }
    }
}
