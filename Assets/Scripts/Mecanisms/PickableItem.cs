using UnityEngine;

public class PickableItem : MonoBehaviour
{
    public Texture2D ItemIcon;
    public Vector3 WorkingPosition;
    public Quaternion WorkingRotation;
    public Behaviour ActiveComponent;
    public Transform Parent;
    public string clueForPlayer;
    public string repeatClue;

    public void Picked()
    {
        transform.SetParent(Parent);
        transform.localPosition = WorkingPosition;
        transform.localRotation = WorkingRotation;
        if (ActiveComponent != null)
        {
            ActiveComponent.enabled = true;
        }
        if (this.gameObject.GetComponentInChildren<Animator>())
        {
            this.gameObject.GetComponentInChildren<Animator>().enabled = true;
        }

        CluesManager.instance.GiveClue(clueForPlayer, repeatClue);

        this.gameObject.SetActive(false);
    }
}
