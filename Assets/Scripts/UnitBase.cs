using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum PieceType
{
    Pawn,   // 폰 (1칸)
    Prince, // 왕자 (1~2칸)
    Knight, // 나이트 (1칸 또는 3칸)
    Bishop, // 비숍 (아군 맨해튼 거리 1칸 옆)
    Rook,    // 룩 (상하좌우 무제한)
    Boss
}

public class UnitBase : MonoBehaviour
{
    [Header("기본 스탯 설정")]
    public PieceType pieceType; // 이 캐릭터가 어떤 기물인지 (유니티 인스펙터에서 설정)
    public bool isPlayerTeam;   // 아군인지 적군인지 판별
    public int attackRange = 1; // 기본 공격 사거리
    public int attackDamage = 1;

    
    // ★ 체력 관련 변수 추가
    
    public int maxHP;
    public int currentHP;

    
    [Header("하트 크기, 위치, 간격")]
    public Sprite heartSprite; // ★ 하트 이미지 딱 1개만 드래그 앤 드롭!
    public float heartSpacing = 1f; // 하트 사이 간격
    public Vector3 healthBarOffset = new Vector3(0, 6.5f, 0); // 캐릭터 머리 위 오프셋
    public float heartScale = 3f; // 하트 크기

    /*
    Sprite heartSprite; // ★ 하트 이미지 딱 1개만 드래그 앤 드롭!
    float heartSpacing = 1f; // 하트 사이 간격
    Vector3 healthBarOffset = new Vector3(0, 6.5f, 0); // 캐릭터 머리 위 오프셋
    float heartScale = 3f; // 하트 크기
    */

    [Header("모션 관련")]
    protected bool isShaking = false;
    private float hitShakeDuration = 0.3f;
    private float hitShakeAmount = 0.7f;
    private Transform shakeRoot;

    // 코드로 자동 생성된 하트들을 담아둘 리스트
    private List<GameObject> heartIcons = new List<GameObject>();

    [HideInInspector]
    public Vector2Int currentGridPos; // 현재 좌표

    // ★ 시각적 피드백을 위한 변수 추가
    protected SpriteRenderer mainSpriteRenderer;

    // 초기화 함수 (GridManager가 캐릭터를 스폰할 때 호출)
    public virtual void Initialize(Vector2Int startPos, bool isPlayer)
    {
        currentGridPos = startPos;
        isPlayerTeam = isPlayer;


        if (shakeRoot == null) shakeRoot = transform;


        currentHP = maxHP;
        
        // ★ 1. 내 캐릭터의 스프라이트 렌더러 가져오기
        mainSpriteRenderer = GetComponent<SpriteRenderer>();

        

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

    

    // ★ 턴 시작/종료 시 호출될 하이라이트 함수
    public void SetHighlight(bool isMyTurn)
    {
        
        if (mainSpriteRenderer != null)
        {
            // 내 턴이면 이미지를 어둡게(회색), 턴이 끝나면 원래 색(흰색)으로 복구
            mainSpriteRenderer.color = isMyTurn ? new Color(1f, 1f, 100/255f, 1f) : Color.white;
        }
    }

    // ★ 턴 순서를 결정하기 위한 기물별 우선순위 점수
    public int TurnPriority
    {
        get
        {
            switch (pieceType)
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
    }


    public bool IsValidMove(Vector2Int targetPos)
    {
        // ★ [수정] 목적지가 내 현재 위치가 '아닐 때'만 겹침 검사를 합니다. 
        // (그래야 제자리 클릭을 정상적인 타일로 인식할 수 있습니다)
        if (currentGridPos != targetPos && TurnManager.Instance.GetUnitAt(targetPos) != null)
            return false;

        int distX = Mathf.Abs(currentGridPos.x - targetPos.x);
        int distY = Mathf.Abs(currentGridPos.y - targetPos.y);
        int manhattanDist = distX + distY;

        switch (pieceType)
        {
            case PieceType.Pawn:
                return manhattanDist == 0 || manhattanDist == 1; // 0칸(제자리) 허용

            case PieceType.Prince:
                return manhattanDist == 0 || manhattanDist == 1 || manhattanDist == 2; // 0칸 허용

            case PieceType.Knight:
                // ★ 나이트는 0칸(제자리) 유지 불가, 오직 1 또는 3만 가능!
                return manhattanDist == 1 || manhattanDist == 3;

            case PieceType.Rook:
                // X와 Y가 모두 0인 경우(제자리)도 distX==0 조건에 맞아 자연스럽게 허용됨
                return distX == 0 || distY == 0;

            case PieceType.Bishop:
                // ★ 비숍 전용 함수 내부에 "아군 옆 1칸" 조건이 있으므로, 
                // 제자리(0칸)일 때 내 옆에 아군이 있으면 true, 없으면 false가 자동으로 반환됩니다!
                return IsValidBishopMove(targetPos);

            case PieceType.Boss:
                return manhattanDist == 0 || manhattanDist == 1; // 0칸 허용
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

    // ★ 맵 전체를 돌면서 내가 '이동'할 수 있는 타일에 색을 칠합니다.
    public void ShowMovableTiles(Color highlightColor)
    {
        GridManager.Instance.ClearAllTileHighlights();
        for (int x = 0; x < GridManager.Instance.gridSize; x++)
        {
            for (int y = 0; y < GridManager.Instance.gridSize; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (IsValidMove(pos))
                {
                    GridManager.Instance.HighlightTile(pos, highlightColor);
                }
            }
        }
    }

    // ★ 맵 전체를 돌면서 내가 '공격'할 수 있는 타일에 색을 칠합니다.
    public void ShowAttackableTiles(Color highlightColor)
    {
        GridManager.Instance.ClearAllTileHighlights();
        for (int x = 0; x < GridManager.Instance.gridSize; x++)
        {
            for (int y = 0; y < GridManager.Instance.gridSize; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                // 자신의 위치는 공격 범위 하이라이트에서 제외합니다.
                if (pos != currentGridPos && IsValidAttack(pos))
                {
                    GridManager.Instance.HighlightTile(pos, highlightColor);
                }
            }
        }
    }

    // 피격 로직 (자식 클래스인 PlayerController와 EnemyController에서 다르게 구현할 예정)
    public virtual void TakeDamage(int damage)
    {
        currentHP -= damage;
        Debug.Log($"{gameObject.name}가 {damage}의 데미지를 입었습니다! (남은 체력: {currentHP}/{maxHP})");

        // ★ 데미지를 입었을 때 UI를 갱신하여 오른쪽 하트를 끕니다.
        UpdateHealthUI();
        StartCoroutine(HitShake());

        // ★ [추가] 아군의 피가 딱 1 남았을 때 이벤트 로그 출력
        if (isPlayerTeam && currentHP == 1)
        {
            Debug.Log($"🚨 [이벤트] {gameObject.name}의 체력이 1 남았습니다! 위기 상황!");
            // 추후 여기에 이벤트 컷씬 호출, 대사 출력 등의 코드를 넣으시면 됩니다.
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }
    private IEnumerator HitShake()
    {
        if (isShaking) yield break;
        isShaking = true;

        Vector3 originalLocalPos = shakeRoot.localPosition;
        float time = 0f;

        while (time < hitShakeDuration)
        {
            time += Time.deltaTime;

            float offsetX = Random.Range(-hitShakeAmount, hitShakeAmount);
            shakeRoot.localPosition = originalLocalPos + new Vector3(offsetX, 0f, 0f);

            yield return null;
        }

        shakeRoot.localPosition = originalLocalPos;
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
