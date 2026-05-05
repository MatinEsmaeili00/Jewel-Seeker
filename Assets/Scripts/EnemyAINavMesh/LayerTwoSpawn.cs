using UnityEngine;

public class LayerTwoSpawn : MonoBehaviour
{
    public Transform[] spawnPoints;
    public GameObject observer;

    public void SpawnEnemies()
    {
        foreach (Transform t in spawnPoints)
        {
            Instantiate(observer, t);
        }
    }
    
}
