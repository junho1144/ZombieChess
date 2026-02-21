using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class CutsceneController : MonoBehaviour
{
    [SerializeField] private GameObject Cut1;
    [SerializeField] private GameObject Cut2;

    void Start()
    {
        StartCoroutine(PlayCutscene());
    }

    IEnumerator PlayCutscene()
    {
        // 1번 장면 표시
        Cut1.SetActive(true);
        Cut2.SetActive(false);

        yield return new WaitForSeconds(3f);

        // 2번 장면으로 전환
        Cut1.SetActive(false);
        Cut2.SetActive(true);
        yield return new WaitForSeconds(3f);

        SceneManager.LoadScene("SampleScene");
    }
}
