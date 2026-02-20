using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private TurnManager turnManager;

    private bool canInput = false;

    void Update()
    {
        if (!canInput) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("플레이어 행동 실행");
            turnManager.EndPlayerTurn();
        }
    }

    public void EnableInput(bool value)
    {
        canInput = value;
    }
}