using UnityEngine;
using System.Collections.Generic;

// ★ 핵심 포인트 1: 프리팹과 좌표를 묶어주는 나만의 데이터 상자 만들기
// [System.Serializable]을 적어주어야 유니티 인스펙터 창에서 우리가 직접 수정할 수 있습니다.
[System.Serializable]
public struct CharacterSpawnInfo
{
    public GameObject characterPrefab; // 소환할 캐릭터의 종류 (프리팹)
    public Vector2Int spawnPosition;   // 소환할 위치 (행렬 좌표)
}

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("타일 설정")]
    public GameObject tilePrefab; // 방금 만든 Tile 프리팹을 넣을 곳
    public int gridSize = 8;      // 8x8 체스판

    [Header("타일 간격 조절")]
    public float tileWidth = 1f;  // 마름모 타일의 가로 너비
    public float tileHeight = 0.5f; // 마름모 타일의 세로 높이


    [Header("스테이지 배치 좌표")]
    // ★ 핵심 포인트 2: 기존의 단일 프리팹 변수를 지우고, 방금 만든 구조체의 리스트로 대체합니다.
    public List<CharacterSpawnInfo> playerSpawns = new List<CharacterSpawnInfo>();
    public List<CharacterSpawnInfo> enemySpawns = new List<CharacterSpawnInfo>();

    public Dictionary<Vector2Int, Vector3> tilePositions = new Dictionary<Vector2Int, Vector3>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        GenerateGrid();
        SpawnCharacters();
    }

    void GenerateGrid()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                // 1. 아이소메트릭(쿼터뷰) 좌표 계산 핵심 공식
                float posX = (x+y) * (tileWidth / 2f) - 7 * (tileWidth / 2f);
                float posY = (y-x) * (tileHeight / 2f); // 아래로 그려지도록 y값에 -를 붙임

                // 2. 타일 생성 및 위치 지정
                Vector3 spawnPosition = new Vector3(posX, posY, 0);
                GameObject spawnedTile = Instantiate(tilePrefab, spawnPosition, Quaternion.identity);


                // 타일 이름 예쁘게 정리 (예: Tile_0_0)
                spawnedTile.name = $"Tile_{x}_{y}";
                // GridManager의 자식 객체로 깔끔하게 묶기
                spawnedTile.transform.parent = this.transform;

                tilePositions[new Vector2Int(x, y)] = spawnPosition;


                Tile tileScript = spawnedTile.GetComponent<Tile>();
                if (tileScript != null)
                {
                    tileScript.SetCoordinate(x, y);
                }
                // 3. 체스판처럼 번갈아가며 색상 입히기
                bool isOffset = (x + y) % 2 == 1;
                SpriteRenderer renderer = spawnedTile.GetComponent<SpriteRenderer>();

                if (isOffset)
                {
                    // 완전 검은색이면 배경과 구분 안 될 수 있으니 어두운 회색 적용
                    renderer.color = new Color(0.3f, 0.3f, 0.3f);
                }


            }


        }
    }

    void SpawnCharacters()
    {
        // 1. 플레이어 캐릭터들 생성 (리스트로 관리)
        List<PlayerController> spawnedPlayers = new List<PlayerController>();

        for (int i = 0; i < playerSpawns.Count; i++)
        {
            // 리스트에서 i번째 상자를 꺼내옵니다.
            CharacterSpawnInfo info = playerSpawns[i];

            // 상자 안의 좌표 정보를 사용합니다.
            Vector3 pWorldPos = GetWorldPosition(info.spawnPosition.x, info.spawnPosition.y);
            pWorldPos.z = -1f;

            // ★ 상자 안의 프리팹 정보를 사용하여 생성합니다!
            GameObject playerObj = Instantiate(info.characterPrefab, pWorldPos, Quaternion.identity);
            playerObj.name = $"Player_{i + 1}";
            PlayerController playerScript = playerObj.GetComponent<PlayerController>();

            spawnedPlayers.Add(playerScript);
        }

        // 2. 적 캐릭터들 생성
        List<EnemyController> spawnedEnemies = new List<EnemyController>();

        for (int i = 0; i < enemySpawns.Count; i++)
        {
            CharacterSpawnInfo info = enemySpawns[i];

            Vector3 eWorldPos = GetWorldPosition(info.spawnPosition.x, info.spawnPosition.y);
            eWorldPos.z = -1f;

            GameObject enemyObj = Instantiate(info.characterPrefab, eWorldPos, Quaternion.identity);
            enemyObj.name = $"Enemy_{i + 1}";
            EnemyController enemyScript = enemyObj.GetComponent<EnemyController>();

            spawnedEnemies.Add(enemyScript);
        }

        // 3. TurnManager에게 리스트 2개를 모두 넘겨줍니다.
        TurnManager.Instance.InitAndStartGame(spawnedPlayers, spawnedEnemies);
    }

    public Vector3 GetWorldPosition(int x, int y)
    {
        Vector2Int gridPos = new Vector2Int(x, y);

        // Dictionary에 우리가 찾는 좌표가 있는지 확인
        if (tilePositions.ContainsKey(gridPos))
        {
            return tilePositions[gridPos]; // 실제 씬(Scene)에서의 Vector3 위치를 반환
        }
        else
        {
            Debug.LogWarning($"[{x}, {y}]는 맵을 벗어난 잘못된 좌표입니다!");
            return Vector3.zero;
        }
    }

}
