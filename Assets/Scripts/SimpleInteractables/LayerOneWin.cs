using UnityEngine;

public class LayerOneWin : MonoBehaviour
{
    private LoadLevels loader;
    void Awake()
    {
        loader = GetComponent<LoadLevels>();
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("Player"))
        {
            return;
        }

        WeaponManager wm = other.gameObject.GetComponent<WeaponManager>();
        if (wm != null && true)
        {
            loader.LoadLevel_2();
        }
    }
    //hi
}
