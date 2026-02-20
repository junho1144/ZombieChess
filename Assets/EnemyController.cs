using UnityEngine;

public class EnemyController : MonoBehaviour
{
    bool hasActedThisTurn = false;

    void Update()
    {
        if (TurnManager.Instance.currentTurn != TurnState.EnemyTurn)
        {
            hasActedThisTurn = false;  // 턴이 바뀌면 리셋
            return;
        }

        if (!hasActedThisTurn)
        {
            hasActedThisTurn = true;
            ExecuteTurn();
        }
    }

    void ExecuteTurn()
    {
        Move();

        if (CanAttack())
        {
            Attack();
        }
        Debug.Log("상대 턴 종료됨");
        TurnManager.Instance.EndTurn();
    }

    void Move() { }

    void Attack() { }

    bool CanAttack()
    {
        return true;
    }
}
