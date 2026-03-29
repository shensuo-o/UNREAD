using UnityEngine;

public class HiddenEnemy : IEnemyState
{
    private Enemy enemy;
    private Transform targetPoint;
    private bool reachedPoint = false;

    public HiddenEnemy(Enemy enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        enemy.OnPlayerDetected += GoToChase;

        if (enemy.hiddenPoints.Length > 0)
        {
            targetPoint = enemy.hiddenPoints[Random.Range(0, enemy.hiddenPoints.Length)];
        }
    }

    public void Exit()
    {
        enemy.OnPlayerDetected -= GoToChase;
    }

    public void Tick()
    {
        if (targetPoint == null) return;

        if (!reachedPoint)
        {
            MoveToPoint();

            float dist = Vector3.Distance(enemy.transform.position, targetPoint.position);

            if (dist < 0.5f)
            {
                reachedPoint = true;

                //quedarse quieto, animación escondido
                Debug.Log("Hidden and waiting");
            }
        }
    }

    void MoveToPoint()
    {
        Vector3 dir = (targetPoint.position - enemy.transform.position).normalized;
        enemy.transform.position += dir * 2f * Time.deltaTime;
    }

    void GoToChase()
    {
        enemy.stateMachine.ChangeState(new ChaseEnemy(enemy));
    }
}