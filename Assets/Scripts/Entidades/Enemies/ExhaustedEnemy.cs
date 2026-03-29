using UnityEngine;

public class ExhaustedEnemy : IEnemyState
{
    private Enemy enemy;
    private float timer;

    public ExhaustedEnemy(Enemy enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        enemy.agent.isStopped = true;
        timer = 0f;
    }

    public void Tick()
    {
        timer += Time.deltaTime;

        enemy.currentEnergy += enemy.energyDrain * Time.deltaTime;
        enemy.currentEnergy = Mathf.Clamp(enemy.currentEnergy, 0, enemy.maxEnergy);

        if (timer >= enemy.recoverTime)
        {
            DecideNextState();
        }
    }

    public void Exit()
    {
        enemy.agent.isStopped = false;
    }

    void DecideNextState()
    {
        enemy.currentEnergy = enemy.maxEnergy;

        if (enemy.canHide && UnityEngine.Random.value > 0.5f)
        {
            enemy.stateMachine.ChangeState(new HiddenEnemy(enemy));
        }
        else
        {
            enemy.stateMachine.ChangeState(new IdleEnemy(enemy));
        }
    }
}
