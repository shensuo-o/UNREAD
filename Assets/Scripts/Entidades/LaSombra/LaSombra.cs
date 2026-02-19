using UnityEngine;
using UnityEngine.AI;

public class LaSombra : EntidadBase
{
    [SerializeField] private Transform Player;
    [SerializeField] private NavMeshAgent Agent;

    
    void Start()
    {
        Agent = GetComponent<NavMeshAgent>();

        if (Player == null )
        {
            Player = GameObject.Find("Jugador").transform;
        }
    }

    void Update()
    {
        if (Player != null && Agent.enabled)
        {
            Agent.SetDestination(Player.position);
        }
    }
}
