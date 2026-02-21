using UnityEngine;
using UnityEngine.SceneManagement;

public class BackScener : MonoBehaviour
{
    public void LoadStartScene()
    {
       SceneManager.LoadScene("StartScene");
    }
}
