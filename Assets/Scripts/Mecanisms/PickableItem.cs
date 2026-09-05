using UnityEngine;

public class PickableItem : MonoBehaviour
{
    public Texture2D ItemIcon;
    public Vector3 WorkingPosition;
    public Behaviour ActiveComponent;
    public Transform Parent;
    public string clueForPlayer;
    public string repeatClue;

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

        CluesManager.instance.GiveClue(clueForPlayer, repeatClue);

        this.gameObject.SetActive(false);
    }
}
