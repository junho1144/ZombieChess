using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    public IEnumerator PerformEnemyAction()
    {
        Debug.Log("적 행동 실행");
        yield return new WaitForSeconds(1f);
    }
}