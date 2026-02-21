using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyController : UnitBase
{
    public override void Initialize(Vector2Int startPos, bool isPlayer)
    {
       
        // ★ 적 체력: 폰은 1, 나머지는 3 설정
        if (pieceType == PieceType.Pawn) maxHP = 1;
        else maxHP = 3;

        currentHP = maxHP;

        base.Initialize(startPos, isPlayer);
    }

    // ★ 1. 기물별 우선순위 점수 부여
    private int GetPiecePriority(PieceType type)
    {
        switch (type)
        {
            case PieceType.Prince: return 4;
            case PieceType.Knight: return 3;
            case PieceType.Bishop: return 2;
            case PieceType.Rook: return 1;
            case PieceType.Pawn: return 0;
            default: return 0;
        }
    }

    // ★ 2. 가장 최적의 타겟(거리 우선 -> 우선순위 점수 비교) 찾기
    private UnitBase FindBestTarget()
    {
        List<UnitBase> playerTeam = TurnManager.Instance.GetTeamList(true);
        UnitBase bestTarget = null;
        int minDistance = int.MaxValue;
        int bestPriority = -1;

        foreach (UnitBase player in playerTeam)
        {
            int dist = Mathf.Abs(currentGridPos.x - player.currentGridPos.x) +
                       Mathf.Abs(currentGridPos.y - player.currentGridPos.y);

            if (dist < minDistance) // 거리가 더 가깝다면 무조건 타겟 변경
            {
                minDistance = dist;
                bestTarget = player;
                bestPriority = GetPiecePriority(player.pieceType);
            }
            else if (dist == minDistance) // 거리가 똑같다면 우선순위 비교
            {
                int playerPriority = GetPiecePriority(player.pieceType);
                if (playerPriority > bestPriority)
                {
                    bestTarget = player;
                    bestPriority = playerPriority;
                }
            }
        }
        return bestTarget;
    }

    public IEnumerator PerformEnemyAction()
    {
        Debug.Log($"{gameObject.name} ({pieceType}) 행동 계산 시작...");
        yield return new WaitForSeconds(0.5f);

        UnitBase targetPlayer = FindBestTarget();

        if (targetPlayer == null)
        {
            Debug.Log($"{gameObject.name}: 맵에 아군이 없습니다. 대기.");
            yield break;
        }

        // 이미 사거리 내에 있다면 이동 생략
        if (!IsValidAttack(targetPlayer.currentGridPos))
        {
            yield return MoveTowardsTarget(targetPlayer);
        }

        // 공격 범위 안에 있으면 실제 타격
        if (IsValidAttack(targetPlayer.currentGridPos))
        {
            yield return AttackTarget(targetPlayer);
        }

        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator MoveTowardsTarget(UnitBase target)
    {
        Debug.Log($"{gameObject.name}: {target.name} 방향으로 이동 탐색 중...");

        // ★ 적이 이동하기 전에 자신의 이동 가능 타일을 보라색으로 표시합니다.
        ShowMovableTiles(Color.magenta);

        Vector2Int bestMove = currentGridPos;
        int minDistanceToTarget = Mathf.Abs(currentGridPos.x - target.currentGridPos.x) +
                                  Mathf.Abs(currentGridPos.y - target.currentGridPos.y);

        for (int x = 0; x < GridManager.Instance.gridSize; x++)
        {
            for (int y = 0; y < GridManager.Instance.gridSize; y++)
            {
                Vector2Int checkPos = new Vector2Int(x, y);

                if (IsValidMove(checkPos))
                {
                    int distToTarget = Mathf.Abs(checkPos.x - target.currentGridPos.x) +
                                       Mathf.Abs(checkPos.y - target.currentGridPos.y);

                    if (distToTarget < minDistanceToTarget)
                    {
                        minDistanceToTarget = distToTarget;
                        bestMove = checkPos;
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.5f);

        if (bestMove != currentGridPos)
        {
            currentGridPos = bestMove;
            Vector3 newWorldPos = GridManager.Instance.GetWorldPosition(bestMove.x, bestMove.y);
            newWorldPos.z = -1f;
            transform.position = newWorldPos;
            Debug.Log($"{gameObject.name}: [{bestMove.x}, {bestMove.y}]로 이동 완료!");
        }

        // ★ 이동이 끝났으므로 타일 하이라이트를 모두 끕니다.
        GridManager.Instance.ClearAllTileHighlights();
    }

    private IEnumerator AttackTarget(UnitBase target)
    {
        Debug.Log($"⚔️ {gameObject.name}: {target.name} 공격!");
        target.TakeDamage(1); // 1 데미지 부여
        yield return new WaitForSeconds(0.5f);
    }
}