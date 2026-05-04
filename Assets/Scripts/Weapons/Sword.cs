using UnityEngine;

public class Sword : MonoBehaviour, IWeapon
{
    public WeaponData data;
    private Animator animator;

    public bool isAttacking;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Attack()
    {
        isAttacking = true;
        animator.SetBool("isAttacking", true);
        Debug.Log("Swing Sword");
    }

    void Update()
    {
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        if (state.IsName("Attack") && state.normalizedTime >= 1f)
        {
            isAttacking = false;
            animator.SetBool("isAttacking", false);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isAttacking) return;

        if (collision.CompareTag("Enemy"))
        {
            IDamageable damageable = collision.GetComponent<IDamageable>();
            damageable?.TakeDamage(data.damage);
        }
    }
}