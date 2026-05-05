using UnityEngine;
using UnityEngine.SceneManagement;

public class BackHome : MonoBehaviour
{
    public void ToHome()
    {
        SceneManager.LoadScene(0);
    }
}