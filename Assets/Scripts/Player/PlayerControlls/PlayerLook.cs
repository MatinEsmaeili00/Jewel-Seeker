using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    public Camera cam;
    public InputActionReference look;
    public Transform weaponSocket;

    public float radius = 0.05f;
    
    private ItemEquip curItem;
    private IWeapon weapon;

    void Start()
    {
        curItem = GetComponent<ItemEquip>();
        if(curItem.currentWeapon != null)
        {weapon = curItem.currentWeapon.GetComponent<IWeapon>();}
        
    }
    private void OnEnable()
    {
        look.action.Enable();
    }
    private void OnDisable()
    {
        look.action.Disable();
    }
    void Update()
    {
        if(curItem.currentWeapon != null)
        {
            weapon = curItem.currentWeapon.GetComponent<IWeapon>();
            if(weapon.AttackStatus()){return;}
        }
        
        Vector2 input = look.action.ReadValue<Vector2>();
        Vector2 direction;

        if (input.magnitude <= 1f)
        {
            if (input.sqrMagnitude < 0.01f)
                return;

            direction = input;
        }
        else
        {
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(
                input.x,
                input.y,
                -cam.transform.position.z
            ));

            direction = (Vector2)(world - transform.position);
        }

        direction.Normalize();

        weaponSocket.localPosition = direction * radius;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        weaponSocket.rotation = Quaternion.Euler(0, 0, angle);
    }
}