using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : UnitBase
{
    private bool canInput = false;
    private bool isMoving = false;

    public float moveDuration = 0.4f;
    public float jumpHeight = 0.8f;

    public enum ActionState
    {
        Idle,
        WaitingToMove,
        WaitingToAttack,
        ActionComplete
    }

    public ActionState currentState = ActionState.Idle;

    [SerializeField] private Transform visualRoot;

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

        StartCoroutine(MoveRoutine(targetPos));
    }

    private IEnumerator MoveRoutine(Vector2Int targetPos)
    {
        isMoving = true;
        canInput = false;

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

        isMoving = false;
        canInput = true;

        if (HasEnemyInRange())
        {
            currentState = ActionState.WaitingToAttack;
        }
        else
        {
            CompleteAction();
        }
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
            targetEnemy.TakeDamage(1);

        CompleteAction();
    }

    private void CompleteAction()
    {
        currentState = ActionState.ActionComplete;
        TurnManager.Instance.NextPlayerAction();
    }
}