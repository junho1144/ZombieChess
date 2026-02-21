using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{


    // ★ 어디서든 턴 매니저에 쉽게 접근할 수 있도록 싱글톤 패턴 적용
    public static TurnManager Instance { get; private set; }

    // 외부에서는 읽기만 가능하게 설정
    public bool IsPlayerTurn { get; private set; }


    private List<PlayerController> playerList = new List<PlayerController>();
    private List<EnemyController> enemyList = new List<EnemyController>();

    // ★ 현재 조작 중인 플레이어의 순서(인덱스)
    private int currentPlayerIndex = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ★ GridManager에서 캐릭터 배치가 모두 끝나면 이 함수를 호출하여 게임을 시작합니다.
    public void InitAndStartGame(List<PlayerController> players, List<EnemyController> enemies)
    {
        playerList = players;
        enemyList = enemies;
        StartPlayerTurn();
    }



    void StartPlayerTurn()
    {
        Debug.Log("==== 플레이어 턴 시작 ====");
        IsPlayerTurn = true;
        currentPlayerIndex = 0;
        ActivateCurrentPlayer();
        //playerList.EnableInput(true);
    }

    // ★ 현재 순서의 플레이어에게 조작 권한을 줍니다.
    void ActivateCurrentPlayer()
    {
        if (currentPlayerIndex < playerList.Count)
        {
            Debug.Log($"[플레이어 {currentPlayerIndex + 1}번] 행동 대기 중...");
            playerList[currentPlayerIndex].EnableInput(true);
        }
        else
        {
            // 모든 플레이어가 행동을 마쳤다면 턴을 종료하고 적에게 넘깁니다.
            EndPlayerTurn();
        }
    }

    // ★ 플레이어 한 명이 행동을 끝냈을 때 호출되는 함수
    public void NextPlayerAction()
    {
        // 현재 플레이어 조작 잠금
        if (currentPlayerIndex < playerList.Count)
        {
            playerList[currentPlayerIndex].EnableInput(false);
        }

        // 다음 플레이어로 순서 넘기기
        currentPlayerIndex++;
        ActivateCurrentPlayer();
    }

    // ★ Tile에서 클릭했을 때 "지금 누구한테 명령을 내려야 하는지" 알려주는 함수
    public PlayerController GetActivePlayer()
    {
        if (IsPlayerTurn && currentPlayerIndex < playerList.Count)
        {
            return playerList[currentPlayerIndex];
        }
        return null;
    }

    public void EndPlayerTurn()
    {
        Debug.Log("==== 플레이어 턴 종료 ====");
        IsPlayerTurn = false;
        StartCoroutine(EnemyTurnRoutine());
        //playerList.EnableInput(false);
        
    }

    IEnumerator EnemyTurnRoutine()
    {
        Debug.Log("==== 적 턴 시작 ====");

        for (int i = 0; i < enemyList.Count; i++)
        {
            if (enemyList[i] != null)
            {
                Debug.Log($"[적 {i + 1}번] 행동 시작");
                yield return enemyList[i].PerformEnemyAction();
            }
        }

        Debug.Log("==== 적 턴 종료 ====");
        StartPlayerTurn(); // 다시 플레이어 턴으로
    }
   
}