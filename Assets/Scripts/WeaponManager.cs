using System;
using UnityEngine;
using GameObject = UnityEngine.GameObject;
using System.Collections.Generic;


public enum WeaponType
{
    Sword,
    Bow,
    Boomerang
}

[System.Serializable]
public struct WeaponEntry
{
    public WeaponType type;
    public GameObject weaponObject;
    public float damage;
    public float cooldown;
    public float runningCooldown;
}

public class WeaponManager : MonoBehaviour
{
 
    public static event System.Action<WeaponEntry,float,float> OnWeaponChanged;
    
    public static event System.Action<WeaponEntry,float,float> OnWeaponAttackStarted;
    
    // public static event Action<WeaponEntry, float, float> OnWeaponChanged;
    // public static event Action<WeaponEntry, float, float> OnWeaponAttackStarted;
    
    

    public WeaponEntry[] weapons;
    
    public WeaponEntry weaponsOldDateHolder;
    
    private WeaponType currentWeapon;
    public WeaponEntry currentWeaponData;

    public float timeSinceLastAttack; 

    //private Dictionary<WeaponType, float> lastAttackTimes = new Dictionary<WeaponType, float>();

    private float lastAttackTime = -9999;

    private void OnEnable()
    {
        PlayerController.OnWeaponSelected += SelectWeapon;
        PlayerController.OnWeaponAttackButton += TryAttack;
    }

    private void OnDisable()
    {
        PlayerController.OnWeaponSelected -= SelectWeapon;
        PlayerController.OnWeaponAttackButton -= TryAttack;
    }
    
    private void Start()
    {
        SwitchWeapon(WeaponType.Sword);
    }
    
    private void SelectWeapon(int index)
    {
        if (!Enum.IsDefined(typeof(WeaponType), index))
            return;
    
        SwitchWeapon((WeaponType)index);
    }
    
    private void SwitchWeapon(WeaponType type)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            bool isSelected = weapons[i].type == type;
    
            if (weapons[i].weaponObject != null)
                weapons[i].weaponObject.SetActive(isSelected);

            if (isSelected)
            {
                
                currentWeaponData = weapons[i];
                if (currentWeaponData.type== weaponsOldDateHolder.type )
                {
                    currentWeaponData.runningCooldown = weaponsOldDateHolder.runningCooldown;
                }
            }
        }
    
        currentWeapon = type;
        OnWeaponChanged?.Invoke(currentWeaponData,currentWeaponData.runningCooldown,currentWeaponData.cooldown);
    }
    
    private void TryAttack()
    {
        timeSinceLastAttack = Time.time - lastAttackTime;
    
        //currentWeaponData.runningCooldown = ;
        if (timeSinceLastAttack < currentWeaponData.cooldown)
        {
            currentWeaponData.runningCooldown = timeSinceLastAttack;
            weaponsOldDateHolder.runningCooldown = currentWeaponData.runningCooldown;
            weaponsOldDateHolder.type = currentWeaponData.type;
            //OnWeaponAttackStarted?.Invoke(currentWeaponData,currentWeaponData.runningCooldown,currentWeaponData.cooldown);
            Debug.Log("Weapon still on cooldown!");
            return;
        }
        
        weaponsOldDateHolder.type = currentWeaponData.type;
        currentWeaponData.runningCooldown = currentWeaponData.cooldown;
        weaponsOldDateHolder.runningCooldown = currentWeaponData.runningCooldown;
        
        lastAttackTime = Time.time;
        
        Debug.Log($"Attacked with {currentWeaponData.type}, Damage: {currentWeaponData.damage}");
    
        OnWeaponAttackStarted?.Invoke(currentWeaponData,currentWeaponData.runningCooldown,currentWeaponData.cooldown);
            
        
    
    }
    
}
