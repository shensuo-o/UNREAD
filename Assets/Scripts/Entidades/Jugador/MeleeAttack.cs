using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    [SerializeField] private Transform CastPoint1;
    [SerializeField] private float CastRadious;
    [SerializeField] private Animator WeaponAnimator;
    [SerializeField] private AnimationClip AttackClip;
    [SerializeField] private RaycastHit HitInfo;
    [SerializeField] private bool ShouldCast;
    [SerializeField] private LayerMask Mask;

    void Update()
    {
        Attack();
        HitBoxCast();
    }

    private void Attack()
    {
        if (Input.GetMouseButtonDown(0))
        {
            WeaponAnimator.SetTrigger(AttackClip.name);
        }
    }

    public void SetCast()
    {
        ShouldCast = !ShouldCast;
    }

    public void HitBoxCast()
    {
        if (ShouldCast)
        {
            if (Physics.CheckSphere(CastPoint1.position, CastRadious, Mask))
            {
                Debug.Log("La Sombra Was Hit.");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (ShouldCast)
        {
            Gizmos.color = Color.red;
            Vector3 origin = CastPoint1.position;
            Vector3 direction = Vector3.zero;
            Gizmos.DrawWireSphere(origin, CastRadious);
        }
    }
}
