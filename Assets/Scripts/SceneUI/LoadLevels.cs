using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadLevels : MonoBehaviour
{
    public void LoadLevel_1()
    {
        SceneManager.LoadScene(1);
    }
    public void LoadLevel_2()
    {
        SceneManager.LoadScene(2);        
    }

    public void LoadLevel_3()
    {
        SceneManager.LoadScene(3);
    }
    public void LoadWin()
    {
        SceneManager.LoadScene(4);
    }
}
