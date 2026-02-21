using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;


public class TurnManager : MonoBehaviour
{


    // ★ 어디서든 턴 매니저에 쉽게 접근할 수 있도록 싱글톤 패턴 적용
    public static TurnManager Instance { get; private set; }

    // 외부에서는 읽기만 가능하게 설정
    public bool IsPlayerTurn { get; private set; }

    // ★ 1. 현재 몇 번째 턴인지 추적하는 변수를 추가합니다. (1턴부터 시작)
    public int currentTurn = 1;

    [Header("스테이지 UI 및 씬 설정")]
    public GameObject victoryUI;       // 승리 시 띄울 UI 패널
    public GameObject defeatUI;        // 패배 시 띄울 UI 패널
    public string nextCutsceneName;    // 승리 후 이동할 다음 컷씬 씬의 이름

    private bool isGameOver = false;

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

        // ★ C#의 Sort 기능을 이용해 리스트를 TurnPriority 기준 내림차순 정렬합니다.
        // (b.TurnPriority.CompareTo(a...) 로 작성해야 숫자가 큰 기물이 리스트의 앞(0번)으로 옵니다.)
        playerList.Sort((a, b) => b.TurnPriority.CompareTo(a.TurnPriority));
        enemyList.Sort((a, b) => b.TurnPriority.CompareTo(a.TurnPriority));

        // 게임 시작 시 UI가 켜져있다면 강제로 끕니다.
        if (victoryUI != null) victoryUI.SetActive(false);
        if (defeatUI != null) defeatUI.SetActive(false);
        isGameOver = false;

        currentTurn = 1; // 게임 시작 시 1턴으로 초기화
        StartPlayerTurn(); // 기본적으로 아군 선 턴 시작

        /*
        // (나중에 추가하실 때 참고용 팁입니다!)
        if (isEnemyFirst)
            StartCoroutine(EnemyTurnRoutine());
        else
            StartPlayerTurn();
        */
    }

    // ★ 기물이 죽었을 때 명단에서 빼주는 함수
    public void RemoveUnit(UnitBase unit, bool isPlayerTeam)
    {
        if (isGameOver) return; // 이미 끝났으면 무시

        if (isPlayerTeam)
        {
            playerList.Remove(unit as PlayerController);
            // 패배 조건: 아군이 한 명이라도 죽으면 즉시 패배
            TriggerDefeat();
        }
        else
        {
            enemyList.Remove(unit as EnemyController);
            // 승리 조건: 적 리스트가 0이 되면 승리
            if (enemyList.Count == 0)
            {
                TriggerVictory();
            }
        }


    }

    private void TriggerVictory()
    {
        isGameOver = true;
        Debug.Log("스테이지 클리어! 모든 적을 처치했습니다.");
        if (victoryUI != null) victoryUI.SetActive(true);
    }

    private void TriggerDefeat()
    {
        isGameOver = true;
        Debug.Log("스테이지 패배... 아군이 당했습니다.");
        if (defeatUI != null) defeatUI.SetActive(true);
    }

    // ★ UI 버튼에서 호출할 재시작 함수
    public void RetryStage()
    {
        // 현재 활성화된 씬을 다시 로드합니다.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ★ UI 버튼에서 호출할 다음 컷씬 이동 함수
    public void LoadNextCutscene()
    {
        if (!string.IsNullOrEmpty(nextCutsceneName))
        {
            SceneManager.LoadScene(nextCutsceneName);
        }
        else
        {
            Debug.LogWarning("다음 컷씬 이름이 설정되지 않았습니다!");
        }
    }

    public List<UnitBase> GetTeamList(bool isPlayerTeam)
    {
        List<UnitBase> teamList = new List<UnitBase>();
        if (isPlayerTeam)
        {
            foreach (var p in playerList) teamList.Add(p);
        }
        else
        {
            foreach (var e in enemyList) teamList.Add(e);
        }
        return teamList;
    }



    void StartPlayerTurn()
    {
        Debug.Log($"==== [ {currentTurn} 턴 ] 플레이어 페이즈 시작 ====");
        IsPlayerTurn = true;
        currentPlayerIndex = 0;
        ActivateCurrentPlayer();
        
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
        Debug.Log($"==== [ {currentTurn} 턴 ] 플레이어 페이즈 종료 ====");
        IsPlayerTurn = false;
        StartCoroutine(EnemyTurnRoutine());
        
    }

    IEnumerator EnemyTurnRoutine()
    {
        Debug.Log($"==== [ {currentTurn} 턴 ] 적 페이즈 시작 ====");

        for (int i = 0; i < enemyList.Count; i++)
        {
            if (enemyList[i] != null)
            {
                Debug.Log($"[적 {i + 1}번] 행동 시작");
                yield return enemyList[i].PerformEnemyAction();
            }
        }

        Debug.Log($"==== [ {currentTurn} 턴 ] 적 페이즈 종료 ====");

        // ★ 3. 적의 행동까지 모두 끝났으므로 턴 수를 1 증가시킵니다.
        currentTurn++;

        StartPlayerTurn(); // 다시 플레이어 턴으로
    }

    // ★ 특정 좌표에 살아있는 기물이 있는지 확인해서 돌려주는 함수 (겹침 방지용)
    public UnitBase GetUnitAt(Vector2Int pos)
    {
        // 1. 아군 중에 해당 좌표에 서 있는 사람이 있는지 확인
        foreach (var p in playerList)
        {
            if (p != null && p.currentHP > 0 && p.currentGridPos == pos)
                return p;
        }

        // 2. 적군 중에 해당 좌표에 서 있는 사람이 있는지 확인
        foreach (var e in enemyList)
        {
            if (e != null && e.currentHP > 0 && e.currentGridPos == pos)
                return e;
        }

        // 아무도 없다면 null 반환
        return null;
    }
}

