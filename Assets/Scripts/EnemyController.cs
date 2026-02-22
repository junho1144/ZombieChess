using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyController : UnitBase
{
    private float moveDuration = 0.25f;
    private float jumpHeight = 4f;
    private bool facingRight = false;
    private float squashAmount = 0.75f;
    private float squashDuration = 0.035f;


    [SerializeField] private Transform visualRoot;

    public override void Initialize(Vector2Int startPos, bool isPlayer)
    {
        // ★ 기물별 체력 및 대미지 설정
        if (pieceType == PieceType.Pawn)
        {
            
            attackDamage = 1;
        }
        else if (pieceType == PieceType.Boss)
        {
                     
            attackDamage = 2;  // ★ 보스 대미지 2
        }
        else
        {
            
            attackDamage = 1;
        }
        
        base.Initialize(startPos, isPlayer);
    }

    private int GetPiecePriority(PieceType type)
    {
        switch (type)
        {
            case PieceType.Boss: return 5;
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

        Debug.Log($"{gameObject.name}: {target.name} 방향으로 이동 탐색 중...");

        // ★ 적이 이동하기 전에 자신의 이동 가능 타일을 보라색으로 표시합니다.
        //ShowMovableTiles(Color.green);
        ShowMovableTiles(new Color(0.5f,1f,0.5f,1f));

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

        yield return new WaitForSeconds(0.5f);

        if (bestMove != currentGridPos)
        {
            Vector3 newWorldPos = GridManager.Instance.GetWorldPosition(bestMove.x, bestMove.y);
            newWorldPos.z = -1f;
            yield return JumpSpinMove(bestMove);
            currentGridPos = bestMove;
        }

        // ★ 이동이 끝났으므로 타일 하이라이트를 모두 끕니다.
        GridManager.Instance.ClearAllTileHighlights();
        
        yield return new WaitForSeconds(0.3f);
    }

    private IEnumerator JumpSpinMove(Vector2Int targetPos)
    {
        if (visualRoot == null)
            visualRoot = transform;

        // ★ 이동 방향(그리드 기준) → 화면 좌/우 계열 판정
        Vector2Int dir = targetPos - currentGridPos;
        bool moveRightSide = (dir.x + dir.y) >= 0;
        bool doFlip = (moveRightSide != facingRight);

        Vector3 startPos = transform.position;
        Vector3 endPos = GridManager.Instance.GetWorldPosition(targetPos.x, targetPos.y);
        endPos.z = -1f;

        Vector3 originalScale = visualRoot.localScale;
        Vector3 squashScale = new Vector3(
            originalScale.x * 1.1f,
            originalScale.y * squashAmount,
            originalScale.z
        );
        float time = 0f;

        // 1️⃣ 점프 전 스쿼시
        while (time < squashDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / squashDuration);
            visualRoot.localScale = Vector3.Lerp(originalScale, squashScale, t);
            yield return null;
        }

        // 이동 sfx by junho
        AudioManager.instance?.PlaySfx(AudioManager.Sfx.CharacterMove);

        // 2️⃣ 점프 직전 복구
        time = 0f;
        while (time < squashDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / squashDuration);
            visualRoot.localScale = Vector3.Lerp(squashScale, originalScale, t);
            yield return null;
        }

        // 3️⃣ 점프 이동 + 회전
        time = 0f;

        float startYRot = visualRoot.localEulerAngles.y;
        float targetYRot = startYRot;
        if (doFlip) targetYRot += 180f;

        while (time < 1f)
        {
            time += Time.deltaTime / moveDuration;
            float t = Mathf.Clamp01(time);

            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;
            transform.position = pos;

            // ✅ 회전 규칙
            float yRot;
            if (doFlip)
            {
                // 반대 방향 이동: 0 → 180
                yRot = Mathf.Lerp(startYRot, targetYRot, t);
            }
            else
            {
                // 같은 방향 이동: 0 → 90 → 0
                float arc = Mathf.Sin(t * Mathf.PI) * 90f;
                yRot = startYRot + arc;
            }

            visualRoot.localRotation = Quaternion.Euler(0f, yRot, 0f);

            yield return null;
        }

        // 착지 보정
        transform.position = endPos;

        // 착지 회전 확정(뒤집기면 180 유지, 아니면 원래 각도)
        visualRoot.localRotation = Quaternion.Euler(0f, doFlip ? targetYRot : startYRot, 0f);

        // 방향 상태 갱신(뒤집은 경우에만)
        if (doFlip)
            facingRight = moveRightSide;

        // 4️⃣ 착지 스쿼시
        time = 0f;
        while (time < squashDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / squashDuration);
            visualRoot.localScale = Vector3.Lerp(originalScale, squashScale, t);
            yield return null;
        }

        // 5️⃣ 착지 후 복구
        time = 0f;
        while (time < squashDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / squashDuration);
            visualRoot.localScale = Vector3.Lerp(squashScale, originalScale, t);
            yield return null;
        }

        visualRoot.localScale = originalScale;

        currentGridPos = targetPos;

    }

    private IEnumerator AttackTarget(UnitBase target)
{
    if (target == null) yield break;

    if (visualRoot == null)
        visualRoot = transform;

    // ===== 설정값 (원하면 SerializeField로 빼도 됨) =====
    float backStepDistance = 0.25f;   // 살짝 빠지는 거리(월드 단위)
    float backStepDuration = 0.08f;   // 백스텝 시간
    float dashDuration = 0.10f;       // 돌진 시간
    float returnMoveDuration = moveDuration; // 복귀는 "한 칸 이동 모션"과 동일하게
    float hitPause = 0.05f;           // 타격 후 잠깐 멈춤(원하면 0으로)
    float landingZ = -1f;             // 너희 프로젝트 규칙 유지

    Vector3 originWorld = transform.position;

    // 타겟 월드 위치 (그리드->월드)
    Vector3 targetWorld = GridManager.Instance.GetWorldPosition(target.currentGridPos.x, target.currentGridPos.y);
    targetWorld.z = landingZ;

    // 방향(그리드 기준, 인접 1칸 전제)
    Vector2Int dir = target.currentGridPos - currentGridPos;

    // (dir.x + dir.y) 기준 우측/좌측 계열 판정(이동과 동일)
    bool targetRightSide = (dir.x + dir.y) >= 0;
    bool doFlip = (targetRightSide != facingRight);

    // 백스텝 방향(월드): 타겟 반대 방향 = -dir
    // GridManager가 아이소메트릭 월드 변환을 하고 있으니,
    // '월드 오프셋'은 "한 칸 뒤 타일의 월드 위치"를 샘플링해서 방향을 잡는 게 가장 안정적임.
    Vector2Int backGrid = currentGridPos - dir; // 한 칸 뒤(보드 밖이면 그래도 방향 계산용으로만 씀)
    Vector3 backWorld = GridManager.Instance.GetWorldPosition(backGrid.x, backGrid.y);
    backWorld.z = landingZ;

    Vector3 backDirWorld = (backWorld - originWorld);
    backDirWorld.z = 0f;
    if (backDirWorld.sqrMagnitude < 0.0001f)
        backDirWorld = -(targetWorld - originWorld); // 혹시 같은 점이면 fallback

    backDirWorld.z = 0f;
    backDirWorld = backDirWorld.normalized;

    Vector3 backStepWorld = originWorld + backDirWorld * backStepDistance;
    backStepWorld.z = originWorld.z;

    // =========================
    // 과정 1) 백스텝 + 회전
    // =========================
    float t = 0f;
    float baseYRot = visualRoot.localEulerAngles.y;

    while (t < 1f)
    {
        t += Time.deltaTime / backStepDuration;
        float tt = Mathf.Clamp01(t);

        // 백스텝 이동(직선)
        transform.position = Vector3.Lerp(originWorld, backStepWorld, tt);

        // 회전 규칙:
        // doFlip이면 180, 아니면 0→90→0
        float yRot;
        if (doFlip)
        {
            yRot = Mathf.Lerp(baseYRot, baseYRot + 180f, tt);
        }
        else
        {
            float arc = Mathf.Sin(tt * Mathf.PI) * 90f; // 0→90→0
            yRot = baseYRot + arc;
        }

        visualRoot.localRotation = Quaternion.Euler(0f, yRot, 0f);
        yield return null;
    }

    // 백스텝 종료 위치 확정
    transform.position = backStepWorld;

    // doFlip이면 여기서 180 유지, 아니면 원래로
    visualRoot.localRotation = Quaternion.Euler(0f, doFlip ? baseYRot + 180f : baseYRot, 0f);

    // facingRight 갱신(뒤집혔으면 방향 바뀐 걸로 확정)
    if (doFlip) facingRight = targetRightSide;

    // =========================
    // 과정 2) 돌진(직선) + 타격(HP-1)
    // =========================
    t = 0f;
    while (t < 1f)
    {
        t += Time.deltaTime / dashDuration;
        float tt = Mathf.Clamp01(t);

        transform.position = Vector3.Lerp(backStepWorld, targetWorld, tt);

        yield return null;
    }

        // 공격 사운드 by junho
        switch (pieceType)
        {
            case PieceType.Pawn:
                AudioManager.instance?.PlaySfx(AudioManager.Sfx.PonAtk);
                break;

            case PieceType.Prince:
                AudioManager.instance?.PlaySfx(AudioManager.Sfx.SonAtk);
                break;

            case PieceType.Knight:
                AudioManager.instance?.PlaySfx(AudioManager.Sfx.KnightAtk);
                break;

            case PieceType.Bishop:
                AudioManager.instance?.PlaySfx(AudioManager.Sfx.BishopAtk);
                break;

            case PieceType.Rook:
                AudioManager.instance?.PlaySfx(AudioManager.Sfx.LookAtk);
                break;

            case PieceType.Boss:
                AudioManager.instance?.PlaySfx(AudioManager.Sfx.KQAtk);
                break;
        }

        transform.position = targetWorld;

    // 타격 적용(요구사항: 이 구간에 넣기)
    target.TakeDamage(attackDamage);

    if (hitPause > 0f)
        yield return new WaitForSeconds(hitPause);

    // =========================
    // 과정 3) 복귀(한 칸 이동 모션과 동일) + 0→90→0 회전
    // =========================
    // 복귀는 "한 칸 이동 모션"처럼: 스쿼시(출발) -> 점프 -> 착지 스쿼시
    Vector3 startPos = transform.position; // 현재 타겟 칸
    Vector3 endPos = originWorld;          // 내 원래 칸

    Vector3 originalScale = visualRoot.localScale;
    Vector3 squashScale = new Vector3(
        originalScale.x * 1.1f,
        originalScale.y * squashAmount,
        originalScale.z
    );

    // 3-1) 복귀 시작 스쿼시
    float time = 0f;
    while (time < squashDuration)
    {
        time += Time.deltaTime;
        float s = Mathf.Clamp01(time / squashDuration);
        visualRoot.localScale = Vector3.Lerp(originalScale, squashScale, s);
        yield return null;
    }

    // 3-2) 스쿼시 복구
    time = 0f;
    while (time < squashDuration)
    {
        time += Time.deltaTime;
        float s = Mathf.Clamp01(time / squashDuration);
        visualRoot.localScale = Vector3.Lerp(squashScale, originalScale, s);
        yield return null;
    }

    // 3-3) 점프 복귀 + (무조건) 0→90→0 회전
    time = 0f;
    float returnBaseY = visualRoot.localEulerAngles.y;

    while (time < 1f)
    {
        time += Time.deltaTime / returnMoveDuration;
        float tt = Mathf.Clamp01(time);

        Vector3 pos = Vector3.Lerp(startPos, endPos, tt);
        pos.y += Mathf.Sin(tt * Mathf.PI) * jumpHeight;
        transform.position = pos;

        float arc = Mathf.Sin(tt * Mathf.PI) * 90f; // 무조건 0→90→0
        visualRoot.localRotation = Quaternion.Euler(0f, returnBaseY + arc, 0f);

        yield return null;
    }

    transform.position = endPos;
    visualRoot.localRotation = Quaternion.Euler(0f, returnBaseY, 0f);

    // 3-4) 착지 스쿼시
    time = 0f;
    while (time < squashDuration)
    {
        time += Time.deltaTime;
        float s = Mathf.Clamp01(time / squashDuration);
        visualRoot.localScale = Vector3.Lerp(originalScale, squashScale, s);
        yield return null;
    }

    // 3-5) 착지 복구
    time = 0f;
    while (time < squashDuration)
    {
        time += Time.deltaTime;
        float s = Mathf.Clamp01(time / squashDuration);
        visualRoot.localScale = Vector3.Lerp(squashScale, originalScale, s);
        yield return null;
    }

    visualRoot.localScale = originalScale;

    // 마지막 안전 보정
    transform.position = originWorld;
}
}