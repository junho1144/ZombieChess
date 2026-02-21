using UnityEngine;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour, IPointerClickHandler
{
    public int gridX;
    public int gridY;

    // GridManager가 타일을 생성할 때 좌표를 입력해 줄 함수
    public void SetCoordinate(int x, int y)
    {
        gridX = x;
        gridY = y;
    }

    // 마우스 클릭 시 자동으로 실행되는 유니티 내장 함수
    public void OnPointerClick(PointerEventData eventData)
    {
        // ★ 플레이어 턴이 아닐 때는 클릭 무시
        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn)
        {
            Debug.Log("지금은 내 턴이 아닙니다!");
            return;
        }

        Debug.Log($"클릭한 타일 좌표: [{gridX}, {gridY}]");

        
    }


}
