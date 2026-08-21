using UnityEngine;
using UnityEngine.AI;

public class TheShadow : EntidadBase
{
    [SerializeField] private Jugador Player;
    [SerializeField] private NavMeshAgent Agent;
    [SerializeField] private ShadowDistortionController DistortionController;

    [SerializeField] private float Effect;


    void Start()
    {
        Agent = GetComponent<NavMeshAgent>();

        if (Player == null)
        {
            Player = GameObject.Find("Jugador").GetComponent<Jugador>();
        }
    }


    void Update()
    {
        if (Player == null)
            return;


        // =====================================================
        // MOVIMIENTO
        // =====================================================

        if (Agent.enabled)
        {
            Agent.SetDestination(
                Player.transform.position
            );
        }


        float dist = Vector3.Distance(
            transform.position,
            Player.transform.position
        );


        // =====================================================
        // DISTORSIÓN DE LA SOMBRA
        // =====================================================

        if (DistortionController != null)
        {
            // Más de 35m = apagado.
            // 35m = comienza.
            // 20m = medio.
            // 10m = máximo.

            bool distortionEnabled = dist <= 35f;

            DistortionController.SetDistortionEnabled(
                distortionEnabled
            );


            if (distortionEnabled)
            {
                // 35m = 0
                // 10m = 1

                float rawIntensity = Mathf.InverseLerp(
                    35f,
                    10f,
                    dist
                );


                // Hace que el efecto aparezca
                // lentamente al principio.
                float intensity = Mathf.Pow(
                    rawIntensity,
                    2.5f
                );


                DistortionController.SetDistortionIntensity(
                    intensity
                );
            }
            else
            {
                DistortionController.SetDistortionBlend(
                    0f
                );
            }
        }


        // =====================================================
        // MOVIMIENTO DE LA SOMBRA
        // =====================================================

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