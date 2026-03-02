using UnityEngine;

public class RangedAttack : MonoBehaviour
{
    [SerializeField] private Transform CastPoint1;
    [SerializeField] private Animator WeaponAnimator;
    [SerializeField] private AnimationClip AttackClip;
    [SerializeField] private RaycastHit HitInfo;
    [SerializeField] private bool ShouldCast;
    [SerializeField] private LayerMask Mask;
    public float Damage;
    [SerializeField] private float AmmoCapacity;
    [SerializeField] private float AmmoLeft;
    [SerializeField] private float TotalAmmo;

    void Update()
    {
        if (InventoryManager.InvInstance.PauseGame == false)
        {
            Attack();
            if (Input.GetKeyDown(KeyCode.R) && ShouldCast && AmmoLeft < AmmoCapacity && TotalAmmo > 0)
            {
                Reload();
            }

        }
    }

    private void Attack()
    {
        if (Input.GetMouseButtonDown(0) && ShouldCast && AmmoLeft > 0)
        {
            AmmoLeft--;
            WeaponAnimator.SetTrigger(AttackClip.name);
            HitBoxCast();
        }
        else if (Input.GetMouseButtonDown(0) && ShouldCast && AmmoLeft == 0 && TotalAmmo > 0)
        {
            Reload();
        }
    }

    public void SetCast()
    {
        ShouldCast = !ShouldCast;
    }

    public void HitBoxCast()
    {
        if (Physics.Raycast(CastPoint1.position, CastPoint1.forward, out HitInfo, Mask))
        {
            if (HitInfo.collider.gameObject.GetComponent<BaseEnemy>())
            {
                HitInfo.collider.gameObject.GetComponent<BaseEnemy>().TakeDamage(Damage, true);
            }
        }
    }

    private void Reload()
    {
        TotalAmmo -= (AmmoCapacity - AmmoLeft);

        if (TotalAmmo < 0)
        {
            AmmoLeft = AmmoCapacity;
            AmmoLeft += TotalAmmo;
        }
        else if (TotalAmmo >= 0)
        {
            AmmoLeft = AmmoCapacity;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 origin = CastPoint1.position;
        Vector3 direction = CastPoint1.forward;
        Gizmos.DrawRay(origin, direction);
    }
}
