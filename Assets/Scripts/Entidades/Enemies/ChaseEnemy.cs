using UnityEngine;

public class ChaseEnemy : IEnemyState
{
    private Enemy enemy;

    public ChaseEnemy(Enemy enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        enemy.agent.isStopped = false;
    }

    public void Tick()
    {
        enemy.agent.SetDestination(enemy.player.position);
        enemy.currentEnergy -= enemy.energyDrain * Time.deltaTime;

        if (enemy.currentEnergy <= 0)
        {
            enemy.stateMachine.ChangeState(new ExhaustedEnemy(enemy));
            return;
        }

        float energyPercent = enemy.currentEnergy / enemy.maxEnergy;
        enemy.agent.speed = Mathf.Lerp(1f, 5f, energyPercent);

    }

    public void Exit()
    {
    }
}
