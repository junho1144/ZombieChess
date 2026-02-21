using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum PieceType
{
    Pawn,   // 폰 (1칸)
    Prince, // 왕자 (1~2칸)
    Knight, // 나이트 (1칸 또는 3칸)
    Bishop, // 비숍 (아군 맨해튼 거리 1칸 옆)
    Rook    // 룩 (상하좌우 무제한)
}
public class UnitBase : MonoBehaviour
{
    [Header("기본 스탯 설정")]
    public PieceType pieceType; // 이 캐릭터가 어떤 기물인지 (유니티 인스펙터에서 설정)
    public bool isPlayerTeam;   // 아군인지 적군인지 판별
    public int attackRange = 1; // 기본 공격 사거리

    // ★ 체력 관련 변수 추가
    public int currentHP;
    public int maxHP;

    [Header("UI 자동 생성 설정")]
    public Sprite heartSprite; // ★ 하트 이미지 딱 1개만 드래그 앤 드롭!
    public float heartSpacing = 0.1f; // 하트 사이 간격
    public Vector3 healthBarOffset = new Vector3(0, 0.6f, 0); // 캐릭터 머리 위 오프셋
    public float heartScale = 0.5f; // 하트 크기
    [Header("모션 관련")]
    protected bool isShaking = false;
    public float hitShakeDuration = 0.2f;
    public float hitShakeAmount = 0.1f;

    // 코드로 자동 생성된 하트들을 담아둘 리스트
    private List<GameObject> heartIcons = new List<GameObject>();

    [HideInInspector]
    public Vector2Int currentGridPos; // 현재 좌표

    // 초기화 함수 (GridManager가 캐릭터를 스폰할 때 호출)
    public virtual void Initialize(Vector2Int startPos, bool isPlayer)
    {
        currentGridPos = startPos;
        isPlayerTeam = isPlayer;
        heartSprite = Resources.Load<Sprite>("Heart");

        // --- 체력바 자동 생성 로직 ---
        if (heartSprite != null && maxHP > 0)
        {
            // 1. 하트들을 묶어둘 빈 부모 객체 생성
            GameObject healthBar = new GameObject("HealthBar");
            healthBar.transform.SetParent(this.transform);
            healthBar.transform.localPosition = healthBarOffset;

            // 2. 예쁘게 가운데 정렬하기 위한 시작 X 좌표 계산
            float startX = -(maxHP - 1) * heartSpacing / 2f;

            // 3. maxHP 개수만큼 하트 자동 생성!
            for (int i = 0; i < maxHP; i++)
            {
                GameObject heart = new GameObject($"Heart_{i + 1}");
                heart.transform.SetParent(healthBar.transform);
                heart.transform.localPosition = new Vector3(startX + (i * heartSpacing), 0, 0);
                heart.transform.localScale = new Vector3(heartScale, heartScale, 1f);

                SpriteRenderer sr = heart.AddComponent<SpriteRenderer>();
                sr.sprite = heartSprite;
                sr.sortingOrder = 10; // 캐릭터보다 앞에 보이도록 설정

                // ★ 적군은 초록색, 아군은 흰색(원본 색상) 적용
                if (isPlayerTeam) sr.color = Color.white;
                else sr.color = Color.green;

                heartIcons.Add(heart);
            }
        }

        UpdateHealthUI();
    }

    // ★ 핵심: 목표 좌표가 이 기물의 이동 규칙에 맞는지 검사하는 함수
    public bool IsValidMove(Vector2Int targetPos)
    {
        // 제자리 이동은 불가능
        if (currentGridPos == targetPos) return false;

        // ★ [추가된 부분] 가려는 목적지에 아군이든 적군이든 이미 서 있다면 이동 불가!
        if (TurnManager.Instance.GetUnitAt(targetPos) != null) return false;

        int distX = Mathf.Abs(currentGridPos.x - targetPos.x);
        int distY = Mathf.Abs(currentGridPos.y - targetPos.y);
        int manhattanDist = distX + distY;

        switch (pieceType)
        {
            case PieceType.Pawn:
                return manhattanDist == 1;

            case PieceType.Prince:
                return manhattanDist == 1 || manhattanDist == 2;

            case PieceType.Knight:
                return manhattanDist == 1 || manhattanDist == 3;

            case PieceType.Rook:
                // X나 Y 중 하나가 같아야 상하좌우 직선 이동
                // (나중에는 중간에 장애물이 있는지 검사하는 로직이 추가되어야 완벽해집니다)
                return distX == 0 || distY == 0;

            case PieceType.Bishop:
                return IsValidBishopMove(targetPos);
        }

        return false;
    }

    // 비숍 전용 특수 이동 로직 (목표 지점이 내 편의 맨해튼 거리 1칸 이내인가?)
    private bool IsValidBishopMove(Vector2Int targetPos)
    {
        // TurnManager를 통해 내 편(아군이면 아군 리스트, 적이면 적 리스트)을 가져옵니다.
        List<UnitBase> myTeam = TurnManager.Instance.GetTeamList(isPlayerTeam);

        foreach (UnitBase ally in myTeam)
        {
            // 자기 자신 옆으로 이동하는 것은 제외
            if (ally == this) continue;

            int allyDistX = Mathf.Abs(ally.currentGridPos.x - targetPos.x);
            int allyDistY = Mathf.Abs(ally.currentGridPos.y - targetPos.y);

            // 목표 지점이 어떤 아군과 맨해튼 거리 1칸 차이라면 이동 가능!
            if (allyDistX + allyDistY == 1)
            {
                return true;
            }
        }
        return false;
    }

    // 공격 사거리 검사 로직 (공통: 1칸)
    public bool IsValidAttack(Vector2Int targetPos)
    {
        int distX = Mathf.Abs(currentGridPos.x - targetPos.x);
        int distY = Mathf.Abs(currentGridPos.y - targetPos.y);
        return (distX + distY) <= attackRange;
    }

    // 피격 로직 (자식 클래스인 PlayerController와 EnemyController에서 다르게 구현할 예정)
    public virtual void TakeDamage(int damage)
    {
        currentHP -= damage;
        Debug.Log($"{gameObject.name}가 {damage}의 데미지를 입었습니다! (남은 체력: {currentHP}/{maxHP})");

        // ★ 데미지를 입었을 때 UI를 갱신하여 오른쪽 하트를 끕니다.
        UpdateHealthUI();
        StartCoroutine(HitShake());

        if (currentHP <= 0)
        {
            Die();
        }
    }
    private IEnumerator HitShake()
    {
        if (isShaking) yield break;

        isShaking = true;

        Vector3 originalPos = transform.position;
        float time = 0f;

        while (time < hitShakeDuration)
        {
            time += Time.deltaTime;

            float offsetX = Random.Range(-hitShakeAmount, hitShakeAmount);
            transform.position = originalPos + new Vector3(offsetX, 0f, 0f);

            yield return null;
        }

        transform.position = originalPos;
        isShaking = false;
    }

    // ★ 체력에 맞춰 하트 이미지를 켜고 끄는 전용 함수
    protected void UpdateHealthUI()
    {


        for (int i = 0; i < heartIcons.Count; i++)
        {
            // 현재 체력보다 인덱스가 크면 하트를 끕니다 (오른쪽부터 꺼짐)
            heartIcons[i].SetActive(i < currentHP);
        }
    }

    protected virtual void Die()
    {
        Debug.Log($"💀 {gameObject.name} 파괴됨!");
        TurnManager.Instance.RemoveUnit(this, isPlayerTeam); // 턴 매니저 명단에서 삭제
        Destroy(gameObject); // 화면에서 삭제
    }
}
