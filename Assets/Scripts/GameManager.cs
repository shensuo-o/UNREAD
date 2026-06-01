using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private GameObject targetItem;
    [SerializeField] private List<GameObject> lightsToTurnOff;
    [SerializeField] private AudioSource triggerSound;

    private void Awake()
    {
        Instance = this;
    }

    public void OnItemPickedUp(GameObject item)
    {
        if (item == targetItem)
        {
            TurnOffLights();
            triggerSound.Play();
        }
    }

    private void TurnOffLights()
    {
        foreach (GameObject lightObj in lightsToTurnOff)
        {
            Light[] lights = lightObj.GetComponentsInChildren<Light>();
            foreach (Light light in lights)
            {
                light.color = Color.black;
            }
        }
    }
}
