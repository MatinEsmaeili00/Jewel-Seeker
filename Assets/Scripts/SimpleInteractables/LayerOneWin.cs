using UnityEngine;

public class LayerOneWin : MonoBehaviour
{
    private LoadLevels loader;
    public static bool isReadyToTransition;
    
    public AudioClip winSound;

    private AudioSource cameraAudioSource;
    
    public static event System.Action OnWeaponCheck;
    void Awake()
    {
        loader = GetComponent<LoadLevels>();
        
        Camera currentCamera = Camera.main;

        if (currentCamera == null)
        {
            Debug.LogError("No Main Camera found! Make sure your camera has the tag MainCamera.");
            return;
        }

        cameraAudioSource = currentCamera.GetComponent<AudioSource>();

        if (cameraAudioSource == null)
        {
            Debug.LogError("Main Camera does not have an AudioSource component.");
            return;
        }
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        PlayWinSound();
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
    
    
    public void PlayWinSound()
    {
        cameraAudioSource.clip = winSound;
        cameraAudioSource.Play();
    }
}
