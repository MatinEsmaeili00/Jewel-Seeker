using System;
using UnityEngine;

public class LayerTwoWin : MonoBehaviour
{
    private LoadLevels loader;

    void Awake()
    {
        loader = GetComponent<LoadLevels>();
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("Player"))
        {
            return;
        }
        
        GemPickUp gems = other.GetComponent<GemPickUp>();
        
        if (gems.isGoalLayer2)
        {
            loader.LoadLevel_3();
        }
    }
}
