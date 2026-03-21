using UnityEngine;

public class InspectButtonUI : MonoBehaviour
{
    private InventorySlot currentSlot;

    public void SetSlot(InventorySlot slot)
    {
        currentSlot = slot;
    }

    public void OnClickInspect()
    {
        if (currentSlot != null)
        {
            string itemName = currentSlot.GetItemName();
            InventoryManager.InvInstance.InspectObject(itemName);
        }

        gameObject.SetActive(false);
    }
}
