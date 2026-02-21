using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    public IEnumerator PerformEnemyAction()
    {
        Debug.Log("적 행동 실행");
        yield return new WaitForSeconds(0.5f);

        bool isPlayerInRange = CheckPlayerInRange();

        if (!isPlayerInRange)
        {
            // 사거리 밖: 이동만 함
            yield return MoveTowardsPlayer();
        }
        else
        {
            // 사거리 안: 이동(위치 조정) 후 공격
            yield return MoveTowardsPlayer();
            yield return AttackPlayer();
        }

        yield return new WaitForSeconds(0.5f);
    }

    // ★ 빈껍데기 함수들
    private bool CheckPlayerInRange()
    {
        // TODO: GridManager를 통해 실제 맨해튼 거리 등을 계산해서 반환
        // 임시로 false(사거리 밖) 반환
        return false;
    }

    private IEnumerator MoveTowardsPlayer()
    {
        Debug.Log("적: 플레이어 방향으로 이동");
        // TODO: 실제 이동 로직 및 애니메이션 대기
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator AttackPlayer()
    {
        Debug.Log("적: 플레이어 공격!");
        // TODO: 실제 타격 로직 및 이펙트 대기
        yield return new WaitForSeconds(0.5f);
    }
}