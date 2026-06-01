using UnityEngine;

public class LoopTrap : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject playerBody;
    [SerializeField] private Transform LoopPoint;

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null && rb.gameObject == playerBody)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            cc.enabled = false;
            player.transform.position = LoopPoint.position;
            cc.enabled = true;
            Debug.Log("funca");
        }
    }
}

