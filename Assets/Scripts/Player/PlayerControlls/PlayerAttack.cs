using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    public static event System.Action<ItemEquip> OnWeaponMouseClick;
    
    
    public InputActionReference attackInput;

    private ItemEquip itemEquip;

    public bool WeaponStatus;

    void Start()
    {
        itemEquip = GetComponent<ItemEquip>();
    }

    private void OnEnable()
    {
        attackInput.action.Enable();
    }

    private void OnDisable()
    {
        attackInput.action.Disable();
    }

    void Update()
    {
        if (attackInput.action.WasPressedThisFrame())
        {
            if(itemEquip.currentWeapon == null)
            {
                return;
            }
            OnWeaponMouseClick?.Invoke(itemEquip);
            WeaponStatus = itemEquip.currWeaponData.isReadyToAttack;
            if (itemEquip.currWeaponData.isReadyToAttack)
            {
                IWeapon weapon = itemEquip.currentWeapon?.GetComponent<IWeapon>();
                weapon?.Attack();
            }

            
        }
    }
}