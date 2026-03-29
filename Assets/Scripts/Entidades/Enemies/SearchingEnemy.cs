using UnityEngine;

public class SearchingEnemy : IEnemyState
{
    private Enemy enemy;
    private float timer = 2f;

    public SearchingEnemy(Enemy enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        enemy.OnPlayerDetected += GoToChase;
        timer = 2f;
    }

    public void Exit()
    {
        enemy.OnPlayerDetected -= GoToChase;
    }

    public void Tick()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            enemy.stateMachine.ChangeState(new LookingEnemy(enemy));
        }
    }

    void GoToChase()
    {
        enemy.stateMachine.ChangeState(new ChaseEnemy(enemy));
    }
}
