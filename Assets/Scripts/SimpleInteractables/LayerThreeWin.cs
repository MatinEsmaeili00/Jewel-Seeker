using UnityEditor.Analytics;
using UnityEngine;

public class LayerThreeWin : MonoBehaviour
{
    private LoadLevels loader;

    void Awake()
    {
        loader = GetComponent<LoadLevels>();
    }
    void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Player"))
        {
            return;
        }
        
        GemPickUp gems = other.GetComponent<GemPickUp>();
        if (gems.isGoalLayer3)
        {
            loader.LoadWin();
        }
    }
}
