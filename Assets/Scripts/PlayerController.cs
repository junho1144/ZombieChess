using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private TurnManager turnManager;

    private bool canInput = false;

    public void EnableInput(bool value)
    {
        canInput = value;
    }

    void Update()
    {
        if (!canInput) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("플레이어 행동 실행(턴 넘김)");
            turnManager.EndPlayerTurn();
        }
    }

    public void MoveToTile(int x, int y)
    {
        if (!canInput) return;

        Debug.Log($"플레이어: [{x}, {y}] 좌표로 이동!");

        // TODO: 실제 이동 로직 구현

        // 이동 후 공격할 대상이 있는지 확인하거나 턴을 종료하는 흐름으로 이어집니다.
        // turnManager.EndPlayerTurn(); // 임시로 주석 처리
    }

    public void AttackTarget()
    {
        if (!canInput) return;

        Debug.Log("플레이어: 적 공격!");

        // TODO: 실제 공격 로직 구현
    }

}