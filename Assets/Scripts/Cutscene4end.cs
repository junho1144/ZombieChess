using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Cutscene4end : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(PlayCutscene());
    }
    IEnumerator PlayCutscene()
    {
        yield return new WaitForSeconds(10f);
        SceneManager.LoadScene("StartScene");
    }
}
