using UnityEngine;

public class ItemEquip : MonoBehaviour
{
    
    
    public static event System.Action<WeaponData> OnWeaponEquip;
    
    
    public Transform weaponSocket;

    public GameObject currentWeapon;
    public WeaponData currWeaponData;

    
    
    private void OnEnable()
    {
        WeaponManager.OnWeaponKeyboardSelect += WeaponSelected;
    }

    private void OnDisable()
    {
        WeaponManager.OnWeaponKeyboardSelect -= WeaponSelected;
    }
    
    
    public  void Equip(WeaponData data)
    {
        Unequip();

        currWeaponData = data;

        currentWeapon = Instantiate(
            data.equippedPrefab,
            weaponSocket.position,
            weaponSocket.rotation,
            weaponSocket
        );
        OnWeaponEquip?.Invoke(data);
    }

    private void Unequip()
    {
        if (currentWeapon != null)
        {
            Destroy(currentWeapon);
            currentWeapon = null;
        }

        if (currWeaponData != null)
        {

        }

        currWeaponData = null;
    }


    void WeaponSelected(WeaponData weaponEntry)
    {
        Unequip();

        currWeaponData = weaponEntry;

        currentWeapon = Instantiate(
            weaponEntry.equippedPrefab,
            weaponSocket.position,
            weaponSocket.rotation,
            weaponSocket
        );

        if (!WeaponManager.currentWeaponData.collected)
        {
            currentWeapon.SetActive(false);
        }
        else
        {
            currentWeapon.SetActive(true);
        }
    }
}