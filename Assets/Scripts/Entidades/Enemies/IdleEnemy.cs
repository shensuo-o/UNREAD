using UnityEngine;

public class IdleEnemy : IEnemyState
{
    private Enemy enemy;
    private Transform currentTarget;

    public IdleEnemy(Enemy enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        SetNewDestination();
    }

    public void Tick()
    {
        // Si llegó al destino
        if (!enemy.agent.pathPending && enemy.agent.remainingDistance < 0.5f)
        {
            SetNewDestination();
        }

        // Detectar jugador
        if (enemy.CanSeePlayer())
        {
            Debug.Log("Chase");
            enemy.stateMachine.ChangeState(new ChaseEnemy(enemy));
        }
    }

    public void Exit()
    {
    }

    void SetNewDestination()
    {
        currentTarget = enemy.GetRandomClosePatrolPoint();
        enemy.agent.SetDestination(currentTarget.position);
    }
}
