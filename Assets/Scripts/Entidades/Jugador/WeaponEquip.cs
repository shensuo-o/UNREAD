using UnityEngine;

public class WeaponEquip : MonoBehaviour
{
    public static WeaponEquip Instance;

    [SerializeField] private Animator animator;
    [SerializeField] private string ArmOverlayLayerName = "ArmOverlay";
    private int armLayerIndex;

    public int CurrentlyEquipped { get; private set; } = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        armLayerIndex = animator.GetLayerIndex(ArmOverlayLayerName);
        Equip(0);
    }

    public void Equip(int type)
    {
        CurrentlyEquipped = type;
        animator.SetInteger("Equipped", type);
        animator.SetLayerWeight(armLayerIndex, type == 0 ? 0f : 1f);
    }
}
