using UnityEngine;
using System.Collections.Generic;

public class LoopTrap : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject playerBody;
    [SerializeField] private List<GameObject> lights;
    [SerializeField] private List<GameObject> lightsToBlack;
    [SerializeField] private Transform LoopPoint;
    [SerializeField] private Color firstTimeColor;

    private bool firstTime = true;

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null && rb.gameObject == playerBody)
        {
            if (firstTime)
            {
                SetLightsColor(lights, firstTimeColor);
                SetLightsColor(lightsToBlack, Color.black);
                firstTime = false;
            }

            CharacterController cc = player.GetComponent<CharacterController>();
            cc.enabled = false;
            player.transform.position = LoopPoint.position;
            cc.enabled = true;
        }
    }

    private void SetLightsColor(List<GameObject> lightList, Color color)
    {
        foreach (GameObject lightObj in lightList)
        {
            Light[] lightComponents = lightObj.GetComponentsInChildren<Light>();
            foreach (Light light in lightComponents)
            {
                light.color = color;
            }
        }
    }
}

