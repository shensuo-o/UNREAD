using UnityEngine;

public class LookingEnemy : IEnemyState
{
    private Enemy enemy;
    private float timer = 2f;

    public LookingEnemy(Enemy enemy)
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
        // mirar alrededor
        enemy.transform.Rotate(Vector3.up, 50f * Time.deltaTime);

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            enemy.stateMachine.ChangeState(new IdleEnemy(enemy));
        }
    }

    void GoToChase()
    {
        enemy.stateMachine.ChangeState(new ChaseEnemy(enemy));
    }
}
