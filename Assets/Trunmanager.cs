using UnityEngine;

public enum TurnState
{
    PlayerTurn,
    EnemyTurn,
    Busy
}

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public TurnState currentTurn;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartPlayerTurn();
    }
    
    public void StartPlayerTurn()
    {
        currentTurn = TurnState.PlayerTurn;
        Debug.Log("플레이어 턴 시작");
        PlayerController player = FindObjectOfType<PlayerController>();
        player.ResetTurn();
    }

    public void StartEnemyTurn()
    {
        currentTurn = TurnState.EnemyTurn;
        Debug.Log("적 턴 시작");
    }

    public void EndTurn()
    {
        if (currentTurn == TurnState.PlayerTurn)
            StartEnemyTurn();
        else
            StartPlayerTurn();
    }
}