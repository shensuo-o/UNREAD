using UnityEngine;

public class EnemyMachine
{
    private IEnemyState currentState;

    public void ChangeState(IEnemyState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    public void Update()
    {
        currentState?.Tick();
    }
}
