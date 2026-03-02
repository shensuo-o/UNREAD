using UnityEngine;

public class BaseEnemy : EntidadBase
{
    [SerializeField] private Transform Target;

    private void Start()
    {
        Target = GameObject.Find("Jugador").transform;
    }

    private void Update()
    {
        if (GotHit)
        {
            Timer += Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<Jugador>())
        {
            Debug.Log("choque al jugador");
            other.gameObject.GetComponent<Jugador>().TakeDamage(Damage, false);
        }
    }
}
