using UnityEngine;

public class LayerOneWin : MonoBehaviour
{
    private LoadLevels loader;
    public static bool isReadyToTransition;
    
    public static event System.Action OnWeaponCheck;
    void Awake()
    {
        loader = GetComponent<LoadLevels>();
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        
        OnWeaponCheck?.Invoke();
        if (!other.gameObject.CompareTag("Player"))
        {
            return;
        }

        WeaponManager wm = other.gameObject.GetComponent<WeaponManager>();
        if (isReadyToTransition)
        {
            loader.LoadLevel_2();
        }
    }
    //hi
}
