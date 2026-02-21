using UnityEngine;
//using UnityEngine.InputSystem; // ★ 유니티의 새로운 입력 시스템 사용
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

        // 현재 활성화된(턴을 진행 중인) 플레이어를 가져옵니다.
        PlayerController activePlayer = TurnManager.Instance.GetActivePlayer();

        if (activePlayer != null)
        {
            // ★ MoveToTile 대신 방금 만든 통합 클릭 함수를 호출합니다.
            activePlayer.OnTileClicked(gridX, gridY);
        }

        Debug.Log($"클릭한 타일 좌표: [{gridX}, {gridY}]");

        
    }


}
