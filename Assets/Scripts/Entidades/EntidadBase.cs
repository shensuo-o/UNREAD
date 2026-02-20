using UnityEngine;

public class EntidadBase : MonoBehaviour
{
    public bool Alive; //Indica si la entidad tiene mas de 0 de vida.
    public float HP; //Vida de la entidad.
    public float MaxHP; //Vida maxima de la entidad.
    public float Speed; //Velocidad de la entidad.
    public float NormalSpeed;
    public float HighSpeed; //Velocidad al ir mas rapido.
    public float LowSpeed; //Velocidad al agacharse o al ir mas lento.
    [Range(0.0f, 0.5f)] public float MovementSmoothTime; //Suavizado de movimiento.
    public float Damage; //Daño de la entidad.

    public void TakeDamage(float damage) //Función que se llama al recibir daño.
    {
        HP -= damage;
        if (HP <= 0)
        {
            Alive = false;
            Death();
        }
    }

    public void Death() //Función que se llama al morir.
    {
        this.gameObject.SetActive(false);
    }
}
