using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractableObject : MonoBehaviour
{
    [SerializeField] private Animator Animator;
    [SerializeField] private AnimationClip Clip;
    public bool NeedItem;
    public GameObject ItemNeeded;

    public void Action(bool hasItem)
    {
        if (NeedItem == true)
        {
            if (hasItem == true)
            {
                Animator.SetTrigger(Clip.name);
            }
            else if (hasItem == false)
            {
                ErrorPromptManager.EPMinstance.timer = 0;
                ErrorPromptManager.EPMinstance.ErrorText.text = "You need " + ItemNeeded.name + " to perform this action.";
            }
        }
        else
        {
            Animator.SetTrigger(Clip.name);
        }
    }
}
