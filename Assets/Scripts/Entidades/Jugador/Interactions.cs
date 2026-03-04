using Unity.VisualScripting;
using UnityEngine;

public class Interactions : MonoBehaviour
{
    private RaycastHit HitInfo;
    [SerializeField] private LayerMask Mask;
    [SerializeField] private GameObject ItemDetected;
    [SerializeField] private float RayLenght;
    [SerializeField] private Transform CastPoint;
    [SerializeField] private GameObject PressE;

    [SerializeField] private DragableItem ItemPrefab;
    [SerializeField] private InventorySlot[] Slots;

    void Start()
    {
        
    }

    void Update()
    {
        if (Physics.Raycast(CastPoint.position, CastPoint.forward, out HitInfo, RayLenght, Mask))
        {
            ItemDetected = HitInfo.collider.gameObject;
        }
        else
        {
            ItemDetected = null;
            PressE.SetActive(false);
        }

        if (ItemDetected != null)
        {
            if (ItemDetected.layer == 9)
            {
                PressE.SetActive(true);

                if (Input.GetKeyDown(KeyCode.E))
                {
                    ItemDetected.GetComponent<InteractableObject>().Action();
                }
            }
            else if (ItemDetected.layer == 10)
            {
                PressE.SetActive(true);

                if (Input.GetKeyDown(KeyCode.E))
                {
                    for (int i = 0; i < Slots.Length; i++)
                    {
                        if (Slots[i].gameObject.transform.childCount == 0)
                        {
                            InventoryManager.InvInstance.ObjectsInInv.Add(ItemDetected);
                            DragableItem newItem = Instantiate(ItemPrefab, Slots[i].transform);
                            newItem.Image.texture = ItemDetected.GetComponent<PickableItem>().ItemIcon;
                            ItemDetected.GetComponent<PickableItem>().Picked();
                            break;
                        }
                    }
                }
            }
            else
            {
                PressE.SetActive(false);
            }
        }
    }
}
