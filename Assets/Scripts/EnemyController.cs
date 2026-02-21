using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyController : UnitBase
{
    public float moveDuration = 0.4f;
    public float jumpHeight = 0.8f;

    [SerializeField] private Transform visualRoot;

    public override void Initialize(Vector2Int startPos, bool isPlayer)
    {
        if (pieceType == PieceType.Pawn) maxHP = 1;
        else maxHP = 3;

        currentHP = maxHP;
        base.Initialize(startPos, isPlayer);
    }

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

            if (dist < minDistance)
            {
                minDistance = dist;
                bestTarget = player;
                bestPriority = GetPiecePriority(player.pieceType);
            }
            else if (dist == minDistance)
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
        yield return new WaitForSeconds(0.5f);

        UnitBase targetPlayer = FindBestTarget();
        if (targetPlayer == null) yield break;

        if (!IsValidAttack(targetPlayer.currentGridPos))
            yield return MoveTowardsTarget(targetPlayer);

        if (IsValidAttack(targetPlayer.currentGridPos))
            yield return AttackTarget(targetPlayer);

        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator MoveTowardsTarget(UnitBase target)
    {
        Vector2Int bestMove = currentGridPos;

        int minDistance =
            Mathf.Abs(currentGridPos.x - target.currentGridPos.x) +
            Mathf.Abs(currentGridPos.y - target.currentGridPos.y);

        for (int x = 0; x < GridManager.Instance.gridSize; x++)
        {
            for (int y = 0; y < GridManager.Instance.gridSize; y++)
            {
                Vector2Int checkPos = new Vector2Int(x, y);

                if (IsValidMove(checkPos))
                {
                    int dist =
                        Mathf.Abs(checkPos.x - target.currentGridPos.x) +
                        Mathf.Abs(checkPos.y - target.currentGridPos.y);

                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestMove = checkPos;
                    }
                }
            }
        }

        if (bestMove != currentGridPos)
            yield return JumpSpinMove(bestMove);

        yield return new WaitForSeconds(0.3f);
    }

    private IEnumerator JumpSpinMove(Vector2Int targetPos)
    {
        if (visualRoot == null)
            visualRoot = transform;

        Vector3 startPos = transform.position;
        Vector3 endPos = GridManager.Instance.GetWorldPosition(targetPos.x, targetPos.y);
        endPos.z = -1f;

        Quaternion startRot = transform.rotation;

        Vector3 originalScale = visualRoot.localScale;
        Vector3 squashScale = new Vector3(
            originalScale.x * 1.1f,
            originalScale.y * 0.75f,
            originalScale.z
        );

        float squashDuration = 0.08f;
        float time = 0f;

        // 1️⃣ 점프 전 스쿼시
        while (time < squashDuration)
        {
            time += Time.deltaTime;
            float t = time / squashDuration;
            visualRoot.localScale = Vector3.Lerp(originalScale, squashScale, t);
            yield return null;
        }

        // 2️⃣ 점프 직전 복구
        time = 0f;
        while (time < squashDuration)
        {
            time += Time.deltaTime;
            float t = time / squashDuration;
            visualRoot.localScale = Vector3.Lerp(squashScale, originalScale, t);
            yield return null;
        }

        // 3️⃣ 점프 이동
        time = 0f;
        while (time < 1f)
        {
            time += Time.deltaTime / moveDuration;
            float t = Mathf.Clamp01(time);

            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;
            transform.position = pos;

            float spin = 360f * t;
            visualRoot.localRotation = Quaternion.Euler(0f, spin, 0f);

            yield return null;
        }

        transform.position = endPos;
        visualRoot.localRotation = Quaternion.identity;

        // 4️⃣ 착지 스쿼시
        time = 0f;
        while (time < squashDuration)
        {
            time += Time.deltaTime;
            float t = time / squashDuration;
            visualRoot.localScale = Vector3.Lerp(originalScale, squashScale, t);
            yield return null;
        }

        // 5️⃣ 착지 후 복구
        time = 0f;
        while (time < squashDuration)
        {
            time += Time.deltaTime;
            float t = time / squashDuration;
            visualRoot.localScale = Vector3.Lerp(squashScale, originalScale, t);
            yield return null;
        }

        visualRoot.localScale = originalScale;

        currentGridPos = targetPos;
    }

    private IEnumerator AttackTarget(UnitBase target)
    {
        target.TakeDamage(1);
        yield return new WaitForSeconds(0.5f);
    }
}