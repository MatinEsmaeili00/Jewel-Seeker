using UnityEditor.Animations;
using UnityEngine;

public class Bow : MonoBehaviour, IWeapon
{
    public WeaponData data;
    private Animator animator;

    public bool isAttacking;
    
    void Awake()
    {
        animator = GetComponent<Animator>();
    }
    public bool AttackStatus()
    {
        return isAttacking;
    }
    public void Attack()
    {
        animator.SetTrigger("Attack");
        Debug.Log("Swing Bow");

    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Bow Collision");
        if(collision.gameObject.CompareTag("Enemy"))
        {
            IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
            
            if (damageable == null) {return;}
            else damageable.TakeDamage(data.damage);
        }
    }
    public void StartAttack()
    {
        isAttacking = true;
        Debug.Log("Attack Started");
    }
    public void EndAttack()
    {
        isAttacking = false;
        Debug.Log("Attacking ended");
    }
}
