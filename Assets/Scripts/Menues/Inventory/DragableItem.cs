using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler //IPointerClickHandler
{
    public Transform ParentAfterDrag;
    public RawImage Image;
    public Camera inspectionCamera;
    public GameObject worldPrefab;
    private Vector2 startPosition;
    private bool isDragging = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        /*ParentAfterDrag = transform.parent;
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();
        Image.raycastTarget = false;*/
        startPosition = eventData.position;
        isDragging = false;

    }

    public void OnDrag(PointerEventData eventData)
    {
        //transform.position = Input.mousePosition;

        if (!isDragging && Vector2.Distance(startPosition, eventData.position) > 10f)
        {
            isDragging = true;

            ParentAfterDrag = transform.parent;
            transform.SetParent(transform.root);
            transform.SetAsLastSibling();
            Image.raycastTarget = false;
        }

        if (isDragging)
        {
            transform.position = Input.mousePosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        /*transform.SetParent(ParentAfterDrag);
        Image.raycastTarget = true;*/
        if (!isDragging) return;
        transform.SetParent(ParentAfterDrag);
        Image.raycastTarget = true;
        isDragging = false;

    }

}

