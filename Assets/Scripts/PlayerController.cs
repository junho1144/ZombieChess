using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : UnitBase
{
    private bool canInput = false;
    private bool isMoving = false;
    private bool facingRight = true;

    public enum ActionState
    {
        Idle,
        WaitingToMove,
        WaitingToAttack,
        ActionComplete
    }

    public ActionState currentState = ActionState.Idle;

    [SerializeField] private Transform visualRoot;
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private float jumpHeight = 0.5f;
    [SerializeField] private float squashAmount = 0.75f;
    [SerializeField] private float squashDuration = 0.035f;

    public override void Initialize(Vector2Int startPos, bool isPlayer)
    {
        maxHP = 3;
        currentHP = 3;
        attackRange = 1;
        base.Initialize(startPos, isPlayer);
    }

    public void EnableInput(bool value)
    {
        canInput = value;

        if (value)
        {
            currentState = ActionState.WaitingToMove;
            Debug.Log($"{gameObject.name}: 턴 시작");
        }
        else
        {
            currentState = ActionState.Idle;
        }
    }

    void Update()
    {
        if (!canInput || isMoving) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            CompleteAction();
        }
    }

    public void OnTileClicked(int x, int y)
    {
        if (!canInput || isMoving) return;

        if (currentState == ActionState.WaitingToMove)
            TryMove(x, y);
        else if (currentState == ActionState.WaitingToAttack)
            TryAttack(x, y);
    }

    private bool HasEnemyInRange()
    {
        List<UnitBase> enemies = TurnManager.Instance.GetTeamList(false);

        foreach (UnitBase enemy in enemies)
        {
            if (enemy != null && enemy.currentHP > 0 && IsValidAttack(enemy.currentGridPos))
                return true;
        }
        return false;
    }

    private void TryMove(int targetX, int targetY)
    {
        Vector2Int targetPos = new Vector2Int(targetX, targetY);

        if (currentGridPos == targetPos)
        {
            if (HasEnemyInRange())
            {
                currentState = ActionState.WaitingToAttack;
            }
            else
            {
                CompleteAction();
            }
            return;
        }

        if (!IsValidMove(targetPos))
        {
            Debug.Log("이동 불가");
            return;
        }

        // (요청대로 유지) Vector 변환/좌표계 관련 부분은 건드리지 않음
        StartCoroutine(MoveRoutine(targetPos));
    }

    // ✅ same side: 0→90→0 / flip: 0→180(착지 후 유지) + 스쿼시 + 점프
    private IEnumerator MoveRoutine(Vector2Int targetPos)
    {
        isMoving = true;
        canInput = false;

        if (visualRoot == null)
            visualRoot = transform;

        Vector2Int dir = targetPos - currentGridPos;

        bool moveRightSide = (dir.x + dir.y) >= 0;
        bool doFlip = moveRightSide != facingRight;

        Vector3 startPos = transform.position;
        Vector3 endPos = GridManager.Instance.GetWorldPosition(targetPos.x, targetPos.y);
        endPos.z = -1f;

        float time = 0f;

        Vector3 originalScale = visualRoot.localScale;
        Vector3 squashScale = new Vector3(
            originalScale.x * 1.1f,
            originalScale.y * squashAmount,
            originalScale.z
        );

        // 1️⃣ 출발 스쿼시
        while (time < squashDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / squashDuration);
            visualRoot.localScale = Vector3.Lerp(originalScale, squashScale, t);
            yield return null;
        }

        // 출발 스쿼시 복구
        time = 0f;
        while (time < squashDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / squashDuration);
            visualRoot.localScale = Vector3.Lerp(squashScale, originalScale, t);
            yield return null;
        }

        // 2️⃣ 점프 + 회전
        time = 0f;

        float startYRot = visualRoot.localEulerAngles.y;
        float targetYRot = startYRot;
        if (doFlip) targetYRot += 180f;

        while (time < 1f)
        {
            time += Time.deltaTime / moveDuration;
            float t = Mathf.Clamp01(time);

            // 위치
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;
            transform.position = pos;

            // 회전: 뒤집기면 180, 아니면 0→90→0
            float yRot;
            if (doFlip)
            {
                yRot = Mathf.Lerp(startYRot, targetYRot, t);
            }
            else
            {
                float arc = Mathf.Sin(t * Mathf.PI) * 90f; // 0 → 90 → 0
                yRot = startYRot + arc;
            }

            visualRoot.localRotation = Quaternion.Euler(0f, yRot, 0f);

            yield return null;
        }

        // 착지 보정
        transform.position = endPos;

        // 착지 회전 확정: 뒤집기면 180 유지, 아니면 원래 각도
        visualRoot.localRotation = Quaternion.Euler(0f, doFlip ? targetYRot : startYRot, 0f);

        if (doFlip)
            facingRight = moveRightSide;

        // 3️⃣ 착지 스쿼시
        time = 0f;
        while (time < squashDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / squashDuration);
            visualRoot.localScale = Vector3.Lerp(originalScale, squashScale, t);
            yield return null;
        }

        // 착지 복구
        time = 0f;
        while (time < squashDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / squashDuration);
            visualRoot.localScale = Vector3.Lerp(squashScale, originalScale, t);
            yield return null;
        }

        visualRoot.localScale = originalScale;

        // ★ Grid 위치 확정
        currentGridPos = targetPos;

        isMoving = false;
        canInput = true;

        if (HasEnemyInRange())
            currentState = ActionState.WaitingToAttack;
        else
            CompleteAction();
    }

    private void TryAttack(int targetX, int targetY)
    {
        Vector2Int targetPos = new Vector2Int(targetX, targetY);

        if (currentGridPos == targetPos)
        {
            CompleteAction();
            return;
        }

        if (!IsValidAttack(targetPos))
            return;

        List<UnitBase> enemies = TurnManager.Instance.GetTeamList(false);
        UnitBase targetEnemy = enemies.Find(e => e.currentGridPos == targetPos);

        if (targetEnemy != null)
        {
            StartCoroutine(AttackEnemy(targetEnemy));
        }
        CompleteAction();
    }
    private IEnumerator AttackEnemy(UnitBase target)
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

        transform.position = targetWorld;

        // 타격 적용(요구사항: 이 구간에 넣기)
        target.TakeDamage(1);

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
    private void CompleteAction()
    {
        currentState = ActionState.ActionComplete;
        TurnManager.Instance.NextPlayerAction();
    }
}