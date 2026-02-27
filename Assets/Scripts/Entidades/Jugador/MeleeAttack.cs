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
    public Collider[] Hits;
    public float Damage;

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
        if (ShouldCast == false )
        {
            Hits = null;
        }
    }

    public void HitBoxCast()
    {
        if (ShouldCast)
        {
            Hits = Physics.OverlapSphere(CastPoint1.position, CastRadious, Mask);

            if (Hits != null)
            {
                foreach (Collider c in Hits)
                {
                    if (c.gameObject.GetComponent<BaseEnemy>())
                    {
                        c.gameObject.GetComponent<BaseEnemy>().TakeDamage(Damage);
                    }
                }
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
