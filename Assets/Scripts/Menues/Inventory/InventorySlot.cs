using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IDropHandler, IPointerDownHandler, IPointerUpHandler
{
    public bool IsEquipment;
    [SerializeField] private GameObject inspectButton;
    private bool isRightClickHolding = false;
    private GameObject currentItem;

    public void OnDrop(PointerEventData eventData)
    {
        if (transform.childCount == 0)
        {
            GameObject dropped = eventData.pointerDrag;
            DragableItem draggableItem = dropped.GetComponent<DragableItem>();
            draggableItem.ParentAfterDrag = transform;

            foreach (GameObject obj in InventoryManager.InvInstance.ObjectsInInv)
            {
                if (obj.name.Contains(draggableItem.Image.texture.name))
                {
                    if (IsEquipment)
                    {
                        obj.SetActive(true);
                    }
                    else
                    {
                        obj.SetActive(false);
                    }
                }
            }
            
            currentItem = draggableItem.gameObject;
        }

        if (eventData.pointerDrag == null) return;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            isRightClickHolding = true;

            inspectButton.transform.position = Input.mousePosition;
            inspectButton.SetActive(true);

            inspectButton.GetComponent<InspectButtonUI>().SetSlot(this);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            isRightClickHolding = false;

            if (!RectTransformUtility.RectangleContainsScreenPoint(inspectButton.GetComponent<RectTransform>(),Input.mousePosition,null))
            {
                inspectButton.SetActive(false);
            }
        }
    }
    public GameObject GetItem()
    {
        return currentItem;
    }

    public string GetItemName()
    {
        if (transform.childCount == 0) return null;

        DragableItem item = GetComponentInChildren<DragableItem>();
        return item.Image.texture.name;
    }
}
