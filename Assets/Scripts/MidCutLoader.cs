using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class TwoBubbleCutsceneToScene : MonoBehaviour
{
    [Header("Left Bubble")]
    [SerializeField] private GameObject leftPanel;
    [SerializeField] private TextMeshProUGUI leftTMP;
    [TextArea] [SerializeField] private string leftLine;

    [Header("Right Bubble")]
    [SerializeField] private GameObject rightPanel;
    [SerializeField] private TextMeshProUGUI rightTMP;
    [TextArea] [SerializeField] private string rightLine;

    [Header("Next Scene")]
    [SerializeField] private string nextSceneName; // 컷씬마다 다르게 입력

    // --- typing state ---
    private bool isTyping = false;
    private bool cancelled = false;

    private void Awake()
    {
        ShowNone();
    }

    private void OnDisable()
    {
        cancelled = true;
    }

    private async void Start()
    {
        cancelled = false;

        // 왼쪽
        ShowLeft("");
        await ShowLineWithSkip(leftTMP, leftLine);
        if (cancelled) return;

        // 오른쪽
        ShowRight("");
        await ShowLineWithSkip(rightTMP, rightLine);
        if (cancelled) return;

        // 정리 후 씬 이동
        ShowNone();
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError($"{nameof(TwoBubbleCutsceneToScene)}: nextSceneName이 비어있음");
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    // ---------------- UI helpers ----------------
    private void ShowLeft(string line)
    {
        if (leftPanel) leftPanel.SetActive(true);
        if (rightPanel) rightPanel.SetActive(false);

        if (leftTMP) leftTMP.text = line;
        if (rightTMP) rightTMP.text = "";
    }

    private void ShowRight(string line)
    {
        if (leftPanel) leftPanel.SetActive(false);
        if (rightPanel) rightPanel.SetActive(true);

        if (rightTMP) rightTMP.text = line;
        if (leftTMP) leftTMP.text = "";
    }

    private void ShowNone()
    {
        if (leftPanel) leftPanel.SetActive(false);
        if (rightPanel) rightPanel.SetActive(false);

        if (leftTMP) leftTMP.text = "";
        if (rightTMP) rightTMP.text = "";
    }

    // ---------------- click + typing ----------------
    private static async Task WaitUntilClick()
    {
        while (Input.GetMouseButton(0))
            await Task.Yield();

        while (!Input.GetMouseButtonDown(0))
            await Task.Yield();
    }

    private async Task TypeText(TextMeshProUGUI tmp, string line, float speedSecondsPerChar)
    {
        isTyping = true;
        tmp.text = "";

        foreach (char c in line)
        {
            if (!isTyping) break; // 클릭으로 스킵
            tmp.text += c;
            await Task.Delay((int)(speedSecondsPerChar * 1000));
        }

        tmp.text = line;
        isTyping = false;
    }

    private async Task ShowLineWithSkip(TextMeshProUGUI tmp, string line)
    {
        _ = TypeText(tmp, line, 0.03f);

        // 타이핑 중 클릭 → 즉시 완료
        while (isTyping)
        {
            await WaitUntilClick();
            isTyping = false;
        }

        // 완전히 출력된 뒤 클릭 → 다음으로
        await WaitUntilClick();
    }
}