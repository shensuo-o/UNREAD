using UnityEngine;
using UnityEngine.AI;

public class LaSombra : EntidadBase
{
    [SerializeField] private Jugador Player;
    [SerializeField] private NavMeshAgent Agent;

    [SerializeField] private float Effect;
    
    void Start()
    {
        Agent = GetComponent<NavMeshAgent>();

        if (Player == null )
        {
            Player = GameObject.Find("Jugador").GetComponent<Jugador>();
        }
    }

    void Update()
    {
        if (Player != null && Agent.enabled)
        {
            Agent.SetDestination(Player.transform.position);
        }

        float dist = Vector3.Distance(transform.position, Player.transform.position);

        if (dist <= 10)
        {
            Speed = LowSpeed;
            Effect = 1.1f;
        }
        else if (dist > 10 && dist <= 20)
        {
            Speed = LowSpeed * 4;
            Effect = 1.01f;
        }
        else if (dist > 20 && dist <= 35)
        {
            Speed = NormalSpeed;
            Effect = 1.001f;
        }
        else
        {
            Speed = HighSpeed;
            Effect = 1;
        }

        Agent.speed = Speed;
        Player.HeadBobbing.NegativeEffect = Effect;
    }
}
