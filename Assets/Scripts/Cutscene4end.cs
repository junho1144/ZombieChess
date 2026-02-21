using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class Cutscene4end : MonoBehaviour
{
    async void Start()
    {
        await Task.Delay(8000);
        Debug.Log("끝");
        SceneManager.LoadScene("StartScene");
    }
}