using UnityEngine;
using System;
using UnityEngine.UI;

public class PlayerStamina : MonoBehaviour
{
    public float maxStamina = 100f;
    public float currentStamina;

    public float regenRate = 15f;        // per second
    public float regenDelay = 1.5f;      // delay after last use

    private float regenTimer;
    
    public Slider mySlider;

    public event Action<float, float> OnStaminaChanged;

    void Awake()
    {
        currentStamina = 0;
    }

    void Update()
    {
        mySlider.value = currentStamina/maxStamina;
        HandleRegen();
    }

    void HandleRegen()
    {
        if (currentStamina >= maxStamina) return;

        regenTimer += Time.deltaTime;

        if (regenTimer >= regenDelay)
        {
            currentStamina += regenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }

    public bool UseStamina(float amount)
    {
        if(currentStamina == 0)
            return false;
        else if(currentStamina < amount)
        {
            currentStamina = 0;
            regenTimer = 0f;
        }
        else
        {
            currentStamina -= amount;
            regenTimer = 0f;
        }

        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        return true;
    }
}