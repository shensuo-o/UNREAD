using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightsOff : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private List<GameObject> lightsGroup1;
    [SerializeField] private List<GameObject> lightsGroup2;
    [SerializeField] private List<GameObject> lightsGroup3;
    [SerializeField] private AudioSource lightSound1;
    [SerializeField] private AudioSource lightSound2;
    [SerializeField] private AudioSource lightSound3;
    [SerializeField] private GameObject blockingObject;
    [SerializeField] private float timeBetweenGroups = 0.5f;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == player && !triggered)
        {
            triggered = true;
            blockingObject.SetActive(true);
            StartCoroutine(TurnOffLights());
        }
    }

    private IEnumerator TurnOffLights()
    {
        TurnOffGroup(lightsGroup1);
        lightSound1.Play();
        yield return new WaitForSeconds(timeBetweenGroups);

        TurnOffGroup(lightsGroup2);
        lightSound2.Play();
        yield return new WaitForSeconds(timeBetweenGroups);

        TurnOffGroup(lightsGroup3);
        lightSound3.Play();
    }

    private void TurnOffGroup(List<GameObject> group)
    {
        foreach (GameObject prefab in group)
        {
            Light[] lights = prefab.GetComponentsInChildren<Light>();
            foreach (Light light in lights)
            {
                light.color = Color.black;
            }
        }
    }
}