using UnityEngine;

public class PlayerController : MonoBehaviour
{
    bool hasActedThisTurn = false;

    void Update()
    {
        if (TurnManager.Instance.currentTurn != TurnState.PlayerTurn)
        {
            return;
        }

        if (!hasActedThisTurn && Input.GetKeyDown(KeyCode.Space))
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
        Debug.Log("아군 턴 종료됨");
        TurnManager.Instance.EndTurn();
    }

    void Move() { }

    void Attack() { }

    bool CanAttack()
    {
        return true;
    }
        public void ResetTurn()
    {
        hasActedThisTurn = false;
    }
}