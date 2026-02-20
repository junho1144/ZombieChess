using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("타일 설정")]
    public GameObject tilePrefab; // 방금 만든 Tile 프리팹을 넣을 곳
    public int gridSize = 8;      // 8x8 체스판

    [Header("타일 간격 조절")]
    public float tileWidth = 1f;  // 마름모 타일의 가로 너비
    public float tileHeight = 0.5f; // 마름모 타일의 세로 높이

    public Dictionary<Vector2Int, Vector3> tilePositions = new Dictionary<Vector2Int, Vector3>();

    void Start()
    {
        GenerateGrid();
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
