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
    public string name;
    public GameObject weaponObject;
    public float damage;
    public float cooldown;
    public float runningCooldown;
    public bool collected;
    public float lastAttackTime;
}

public class WeaponManager : MonoBehaviour
{
 
    public static event System.Action<WeaponEntry,float,float> OnWeaponChanged;
    
    public static event System.Action<WeaponEntry,float,float> OnWeaponAttackStarted;
    
    
    public static event System.Action<WeaponEntry,float,float> OnWeaponCooldownUpdated;
    
    // public static event Action<WeaponEntry, float, float> OnWeaponChanged;
    // public static event Action<WeaponEntry, float, float> OnWeaponAttackStarted;
    
    

    public WeaponEntry[] weapons;
    
    public WeaponEntry weaponsOldDateHolder;
    
    private WeaponType currentWeapon;
    public WeaponEntry currentWeaponData;
    
    private int currentWeaponIndex = 0;

    public float timeSinceLastAttack;

    public bool isReadyToAttack;

    //private Dictionary<WeaponType, float> lastAttackTimes = new Dictionary<WeaponType, float>();

    //private float lastAttackTime = -9999;

    private void OnEnable()
    {
        PlayerController.OnWeaponSelected += SelectWeapon;
        PlayerController.OnWeaponAttackButton += TryAttack;
        ItemEquip.OnWeaponEquip += WeaponEquip;
        PlayerAttack.OnWeaponMouseClick += WeaponAttackMouseClick;
    }

    private void OnDisable()
    {
        PlayerController.OnWeaponSelected -= SelectWeapon;
        PlayerController.OnWeaponAttackButton -= TryAttack;
        ItemEquip.OnWeaponEquip -= WeaponEquip;
    }
    
    private void Update() // it is streaming data to the Weapon ui !! need to be optimize
    {
        timeSinceLastAttack = Time.time - currentWeaponData.lastAttackTime;

        if (timeSinceLastAttack < currentWeaponData.cooldown)
        {
            currentWeaponData.runningCooldown = timeSinceLastAttack;
            isReadyToAttack = false;

            // OnWeaponCooldownUpdated?.Invoke(
            //     currentWeaponData,
            //     currentWeaponData.runningCooldown,
            //     currentWeaponData.cooldown
            // );
        }
        else
        {
            currentWeaponData.runningCooldown = currentWeaponData.cooldown;
            isReadyToAttack =  true;

            // OnWeaponCooldownUpdated?.Invoke(
            //     currentWeaponData,
            //     currentWeaponData.runningCooldown,
            //     currentWeaponData.cooldown
            // );
        }
        
        
        // IMPORTANT:
        // because WeaponEntry is a struct, currentWeaponData is only a copy.
        // So we write the updated data back into the real array.
        weapons[currentWeaponIndex] = currentWeaponData;

        OnWeaponCooldownUpdated?.Invoke(
            currentWeaponData,
            currentWeaponData.runningCooldown,
            currentWeaponData.cooldown
        );
    }
    
    private void Start()
    {
        SwitchWeapon(WeaponType.Sword);
    }
    
    private void SelectWeapon(int index)
    {
        Debug.Log("it is calling select weapon!");
        if (!Enum.IsDefined(typeof(WeaponType), index))
        {
            Debug.Log("it is failing select weapon!");
            return;
        }    
        SwitchWeapon((WeaponType)index);
    }
    
    private void SwitchWeapon(WeaponType type)
    {
        Debug.Log("switch weapon has been called!");
        for (int i = 0; i < weapons.Length; i++)
        {
            bool isSelected = weapons[i].type == type;
    
            if (weapons[i].weaponObject != null)
                weapons[i].weaponObject.SetActive(isSelected);

            if (isSelected)
            {
                currentWeaponIndex = i;
                currentWeaponData.collected = true;
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
        isReadyToAttack = false;
        timeSinceLastAttack = Time.time - currentWeaponData.lastAttackTime;
    
        //currentWeaponData.runningCooldown = ;
        if (timeSinceLastAttack < currentWeaponData.cooldown)
        {
            currentWeaponData.runningCooldown = timeSinceLastAttack;
            
            weaponsOldDateHolder.runningCooldown = currentWeaponData.runningCooldown;
            weaponsOldDateHolder.type = currentWeaponData.type;
            //OnWeaponAttackStarted?.Invoke(currentWeaponData,currentWeaponData.runningCooldown,currentWeaponData.cooldown);
            
            weapons[currentWeaponIndex] = currentWeaponData;
            
            //Debug.Log("Weapon still on cooldown!");
            return;
        }
        
        weaponsOldDateHolder.type = currentWeaponData.type;
        //currentWeaponData.runningCooldown = currentWeaponData.cooldown;
        currentWeaponData.runningCooldown = 0;
        weaponsOldDateHolder.runningCooldown = currentWeaponData.runningCooldown;
        
        weapons[currentWeaponIndex] = currentWeaponData;
        
        currentWeaponData.lastAttackTime = Time.time;
        
        //Debug.Log($"Attacked with {currentWeaponData.type}, Damage: {currentWeaponData.damage}");
    
        OnWeaponAttackStarted?.Invoke(currentWeaponData,currentWeaponData.runningCooldown,currentWeaponData.cooldown);
            
        
    
    }

    private void WeaponEquip(WeaponData data)
    {
        
        Debug.Log("weapon Equip called ");
        for (int i = 0; i < weapons.Length; i++)
        {
            Debug.Log("weapon Equip before for loop "+ weapons[i].name+"  nextt "+ data.itemName);
            
            if (weapons[i].name == data.itemName)
            {
                Debug.Log("weapon Equip in for loop ");
                SelectWeapon(i);
            }
            else
            {
                Debug.Log("weapon Equip failed for loop "+ weapons[i].name);
            }
            
        }
        
    }

    void WeaponAttackMouseClick(ItemEquip item )
    {
        TryAttack();
        item.currWeaponData.isReadyToAttack = isReadyToAttack;
        
    }
    
}
