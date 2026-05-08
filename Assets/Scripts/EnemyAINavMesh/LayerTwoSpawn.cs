using UnityEngine;

public class LayerTwoSpawn : MonoBehaviour
{
    public Transform[] spawnPoints;
    public GameObject observer;

    void Awake()
    {
        foreach (Transform t in spawnPoints)
        {
            Instantiate(observer, t);
        }
    }
    
}
