using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
using System.Collections;
public class WeaponUI : MonoBehaviour
{
    public Image swordImage;
    public Image bowImage;
    public Image boomerangImage;
    
    public Image swordImageFill;
    public Image bowImageFill;
    public Image boomerangImageFill;

    public float fill;
    
    
    public Coroutine currentFillRoutine;
    
    private Coroutine switchRoutine;
    private Coroutine cooldownRoutine;
    
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    
    private void OnEnable()
    {
        WeaponManager.OnWeaponChanged += WeaponSwitchUpdateUI;
        WeaponManager.OnWeaponAttackStarted += WeaponAttackUpdateUI;
    }

    private void OnDisable()
    {
        WeaponManager.OnWeaponChanged -= WeaponSwitchUpdateUI;
        WeaponManager.OnWeaponAttackStarted -= WeaponAttackUpdateUI;
    }

    private void WeaponSwitchUpdateUI(WeaponEntry weapon,float runningCooldown,float cooldown)
    {
        // reset all to full
        swordImageFill.fillAmount = 1;
        bowImageFill.fillAmount = 1;
        boomerangImageFill.fillAmount = 1;
    
        // stop previous animation if running
        if (currentFillRoutine != null)
            StopCoroutine(currentFillRoutine);
    
        // choose which one to animate
        Image targetImage = null;
    
        switch (weapon.type)
        {
            case WeaponType.Sword:
                targetImage = swordImageFill;
                break;
    
            case WeaponType.Bow:
                targetImage = bowImageFill;
                break;
    
            case WeaponType.Boomerang:
                targetImage = boomerangImageFill;
                break;
        }
    
        if (targetImage != null)
        {
            //targetImage.fillAmount = 1;
            
            float normalizedValue = Mathf.Clamp01(runningCooldown / cooldown);
            fill = normalizedValue;
        
            targetImage.fillAmount = fill;
            currentFillRoutine = StartCoroutine(AnimateFill(targetImage, 0, 1f,fill));
        }
        
    }
    
    private void WeaponAttackUpdateUI(WeaponEntry weapon, float runningCooldown,float cooldown)
    {
        Debug.Log("WeaponAttackUpdateUI Called !!");
        
        
        Image targetImage = null;

        switch (weapon.type)
        {
            case WeaponType.Sword:      targetImage = swordImageFill;      break;
            case WeaponType.Bow:        targetImage = bowImageFill;        break;
            case WeaponType.Boomerang:  targetImage = boomerangImageFill;  break;
        }

        if (targetImage == null) return;

        if (currentFillRoutine != null)
            StopCoroutine(currentFillRoutine);
        
        
        float normalizedValue = Mathf.Clamp01(runningCooldown / cooldown);
        fill = normalizedValue;
        
        targetImage.fillAmount = fill;
        currentFillRoutine = StartCoroutine(AnimateFill(targetImage,0 , runningCooldown,fill));
        
    }
    
    
    private IEnumerator AnimateFill(Image img, float target, float duration,float startTime)
    {
        
        float time = 0f;
        //float start = img.fillAmount;
    
        float start = startTime;
        
        while (time < duration)
        {
            
            time += Time.deltaTime;
            float t = time / duration;
            t = t * t * (3f - 2f * t); // smoothstep
    
            img.fillAmount = Mathf.Lerp(start, target, t);
    
            yield return null;
        }
    
        img.fillAmount = target;
    }
    
    private void WeaponRefreshUpdateUI(WeaponEntry weapon)
    {
        
    }
   
}
