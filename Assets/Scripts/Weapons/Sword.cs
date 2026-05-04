using UnityEditor.Animations;
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
    public bool AttackStatus()
    {
        return isAttacking;
    }
    public void Attack()
    {
        animator.SetTrigger("Attack");
        Debug.Log("Swing Sword");

    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Sword Collision");
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
