using UnityEngine;
using UnityEngine.Serialization;

public class GemPickUp : MonoBehaviour
{ 
    public int curGemNum;
    public int goalNum1 = 10;
    public int goalNum2 = 20;
    public bool isGoalLayer2;
    public bool isGoalLayer3;

    void Awake()
    {
        curGemNum = 0;
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("shiiiiiiiii");
        if(collision.gameObject.CompareTag("Gem"))
        {
            curGemNum++;
            if (curGemNum >= goalNum1) isGoalLayer2 = true;
            if (curGemNum >= goalNum2) isGoalLayer3 = true;
            
            Destroy(collision.gameObject);
        }
    }
    
}
