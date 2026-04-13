using UnityEngine;

public class LookingEnemy : IEnemyState
{
    private Enemy enemy;

    private Quaternion initialRotation;

    private int step = 0;

    private float rotationSpeed = 120f;
    public LookingEnemy(Enemy enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        enemy.OnPlayerDetected += GoToChase;

        initialRotation = enemy.transform.rotation;

        step = 0;

        enemy.agent.isStopped = true; 
    }

    public void Exit()
    {
        enemy.OnPlayerDetected -= GoToChase;

        enemy.agent.isStopped = false;
    }

    public void Tick()
    {
        Quaternion targetRotation = GetTargetRotation();

        enemy.transform.rotation = Quaternion.RotateTowards(
            enemy.transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        if (Quaternion.Angle(enemy.transform.rotation, targetRotation) < 1f)
        {
            step++;
        }

        if (step > 3)
        {
            enemy.stateMachine.ChangeState(new IdleEnemy(enemy));
        }
    }

    Quaternion GetTargetRotation()
    {
        switch (step)
        {
            case 0:
                return initialRotation * Quaternion.Euler(0, -45f, 0);

            case 1: 
                return initialRotation;

            case 2: 
                return initialRotation * Quaternion.Euler(0, 45f, 0);

            case 3:
                return initialRotation;

            default:
                return initialRotation;
        }
    }

    void GoToChase()
    {
        enemy.stateMachine.ChangeState(new ChaseEnemy(enemy));
    }
}
