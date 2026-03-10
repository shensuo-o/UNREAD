using UnityEngine;

public class PickableItem : MonoBehaviour
{
    public Texture2D ItemIcon;
    public Vector3 WorkingPosition;
    public Behaviour ActiveComponent;
    public Transform Parent;

    public void Picked()
    {
        transform.SetParent(Parent);
        transform.position = WorkingPosition;
        if (ActiveComponent != null)
        {
            ActiveComponent.enabled = true;
        }
        if (this.gameObject.GetComponent<Animator>())
        {
            this.gameObject.GetComponent<Animator>().enabled = true;
        }
        this.gameObject.SetActive(false);
    }
}
