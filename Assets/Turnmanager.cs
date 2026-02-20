using UnityEngine;
using System.Collections;

public class TurnManager : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private EnemyController enemyController;

    void StartPlayerTurn()
    {
        Debug.Log("플레이어 턴 시작");
        isPlayerTurn = true;
        playerController.EnableInput(true);
    }

    public void EndPlayerTurn()
    {
        Debug.Log("플레이어 턴 종료");
        isPlayerTurn = false;
        playerController.EnableInput(false);
        StartCoroutine(EnemyTurn());
    }

    IEnumerator EnemyTurn()
    {
        Debug.Log("적 턴 시작");

        yield return enemyController.PerformEnemyAction();

        Debug.Log("적 턴 종료");
        StartPlayerTurn();
    }

    private bool isPlayerTurn;

    void Start()
    {
        StartPlayerTurn();
    }
}