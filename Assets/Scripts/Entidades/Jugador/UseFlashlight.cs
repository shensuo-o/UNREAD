using UnityEngine;

public class UseFlashlight : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private bool canSwitch;

    void Start()
    {
        animator.SetBool("OnOrOff", false);
    }

    void Update()
    {
        if (canSwitch)
        {
            if(Input.GetMouseButtonDown(0))
            {
                animator.SetBool("OnOrOff", !animator.GetBool("OnOrOff"));
                canSwitch = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            canSwitch = false;
            animator.SetTrigger("Reload");
        }
    }

    public void EnableSwitch()
    {
        canSwitch = true;
    }
}
