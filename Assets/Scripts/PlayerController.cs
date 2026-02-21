using UnityEngine;
using System.Collections.Generic;

public class PlayerController : UnitBase
{
    private bool canInput = false;

    public enum ActionState
    {
        Idle,
        WaitingToMove,
        WaitingToAttack,
        ActionComplete
    }

    public ActionState currentState = ActionState.Idle;

    public override void Initialize(Vector2Int startPos, bool isPlayer)
    {
        maxHP = 3;
        currentHP = 3;
        base.Initialize(startPos, isPlayer);
    }

    public void EnableInput(bool value)
    {
        canInput = value;
        if (value)
        {
            currentState = ActionState.WaitingToMove;
            Debug.Log($"{gameObject.name} ({pieceType}): 턴 시작! 이동할 타일을 클릭하거나, 이동 없이 제자리를 클릭하세요.");
        }
        else currentState = ActionState.Idle;
    }

    void Update()
    {
        if (!canInput) return;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CompleteAction();
        }
    }

    public void OnTileClicked(int x, int y)
    {
        if (!canInput) return;
        if (currentState == ActionState.WaitingToMove) TryMove(x, y);
        else if (currentState == ActionState.WaitingToAttack) TryAttack(x, y);
    }

    // ★ 1. 내 주변(공격 사거리 내)에 공격할 수 있는 적이 있는지 확인하는 헬퍼 함수
    private bool HasEnemyInRange()
    {
        List<UnitBase> enemies = TurnManager.Instance.GetTeamList(false); // 적 리스트 가져오기
        foreach (UnitBase enemy in enemies)
        {
            // 살아있는 적이 내 사거리(IsValidAttack) 안에 있다면 true 반환
            if (enemy != null && enemy.currentHP > 0 && IsValidAttack(enemy.currentGridPos))
            {
                return true;
            }
        }
        return false;
    }

    private void TryMove(int targetX, int targetY)
    {
        Vector2Int targetPos = new Vector2Int(targetX, targetY);

        // ★ 2. 이동 페이즈에서 '자기 자신(제자리)'을 클릭했을 때의 스마트 처리
        if (currentGridPos == targetPos)
        {
            if (HasEnemyInRange())
            {
                // 주변에 적이 있다면 -> 이동 생략, 공격 준비!
                currentState = ActionState.WaitingToAttack;
                Debug.Log($"{gameObject.name}: 이동 생략! 공격 대상 타일을 클릭하거나 제자리를 다시 클릭해 행동을 포기하세요.");
            }
            else
            {
                // 주변에 적도 없다면 -> 불필요한 공격 페이즈를 건너뛰고 바로 턴 종료!
                Debug.Log($"{gameObject.name}: 이동 생략. 사거리 내에 적이 없어 행동을 바로 완료합니다.");
                CompleteAction();
            }
            return;
        }

        // 기존 이동 로직
        if (IsValidMove(targetPos))
        {
            currentGridPos = targetPos;
            Vector3 newWorldPos = GridManager.Instance.GetWorldPosition(targetX, targetY);
            newWorldPos.z = -1f;
            transform.position = newWorldPos;

            // ★ 3. 이동을 마친 후에도 스마트하게 상태 확인
            if (HasEnemyInRange())
            {
                currentState = ActionState.WaitingToAttack;
                Debug.Log($"{gameObject.name}: 이동 완료! 공격 대상 타일을 클릭하거나 제자리를 클릭하세요.");
            }
            else
            {
                Debug.Log($"{gameObject.name}: 이동 완료! 사거리 내에 타격 가능한 적이 없어 행동을 바로 완료합니다.");
                CompleteAction();
            }
        }
        else Debug.Log($"{gameObject.name}: 거기로는 이동할 수 없습니다! (기물 규칙 위반 또는 장애물 있음)");
    }

    private void TryAttack(int targetX, int targetY)
    {
        Vector2Int targetPos = new Vector2Int(targetX, targetY);

        // 공격 페이즈에서 제자리 클릭 시 공격 포기
        if (currentGridPos == targetPos)
        {
            Debug.Log($"{gameObject.name}: 제자리를 클릭하여 공격을 포기했습니다.");
            CompleteAction();
            return;
        }

        if (IsValidAttack(targetPos))
        {
            Debug.Log($"{gameObject.name}: [{targetX}, {targetY}] 공격 실행!");

            List<UnitBase> enemies = TurnManager.Instance.GetTeamList(false);
            UnitBase targetEnemy = enemies.Find(e => e.currentGridPos == targetPos);

            if (targetEnemy != null)
            {
                targetEnemy.TakeDamage(1);
            }
            else
            {
                Debug.Log("공격했지만 해당 타일에는 적이 없습니다.");
            }

            CompleteAction();
        }
        else Debug.Log($"{gameObject.name}: 사거리 밖입니다! 다시 클릭하세요.");
    }

    private void CompleteAction()
    {
        currentState = ActionState.ActionComplete;
        Debug.Log($"{gameObject.name}: 행동 완료. 턴을 넘깁니다.");
        TurnManager.Instance.NextPlayerAction();
    }
}