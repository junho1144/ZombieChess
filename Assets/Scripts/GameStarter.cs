using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStarter : MonoBehaviour
{
    public void LoadPlayScene()
    {
       SceneManager.LoadScene("Cutscene0");
    }
}