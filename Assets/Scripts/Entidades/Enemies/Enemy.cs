using System;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;
using System.Linq;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{
    public EnemyMachine stateMachine;

    [Header("Target")]
    public Transform player;

    [Header("Detection")]
    public float detectionRange = 5f;
    private bool isPlayerInRange;

    [Header("Energy")]
    public float maxEnergy = 10f;
    public float currentEnergy;
    public float energyDrain = 1f;
    public float recoverTime = 3f;

    [Header("Vision")]
    public float viewAngle = 90f;
    public float viewDistance = 7f;
    public LayerMask obstacleMask;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public int PatrolDistance = 5;

    [Header("Hidden Points")]
    public Transform[] hiddenPoints;

    [Header("Behavior")]
    public bool canPatrol = true;
    public bool canHide = true;

    public NavMeshAgent agent;

    public Action OnPlayerDetected;
    public Action OnPlayerLost;
    public Action OnStartSearch;
    public Action OnExhausted;


    public List<Transform> listap = new List<Transform>();



    private void Awake()
    {
        stateMachine = new EnemyMachine();
        agent = GetComponent<NavMeshAgent>();
        currentEnergy = maxEnergy;
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        ChooseInitialState();
    }

    void ChooseInitialState()
    {
        if (canHide && UnityEngine.Random.value > 0.5f)
        {
            stateMachine.ChangeState(new HiddenEnemy(this));
        }
        else
        {
            stateMachine.ChangeState(new IdleEnemy(this));
        }
    }

    private void Update()
    {
        stateMachine.Update();
        DetectPlayer();
    }

    void DetectPlayer()
    {
        bool canSee = CanSeePlayer();

        if (canSee && !isPlayerInRange)
        {
            OnPlayerDetected?.Invoke();
        }
        else if (!canSee && isPlayerInRange)
        {
            OnPlayerLost?.Invoke();
        }

        isPlayerInRange = canSee;
    }

    public Transform GetClosestPatrolPoint()
    {
        Transform bestPoint = null;
        float bestDistance = Mathf.Infinity;

        foreach (Transform point in patrolPoints)
        {
            float dist = Vector3.Distance(transform.position, point.position);

            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestPoint = point;
            }
        }

        return bestPoint;
    }

    public Transform GetRandomClosePatrolPoint()
    {
       List<Transform> sorted = patrolPoints
            .OrderBy(p => Vector3.Distance(transform.position, p.position))
            .ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            if(Vector3.Distance (transform.position, sorted[i].position) > PatrolDistance)
            {
                sorted.RemoveAt(i);
            }
        }
            //listap = patrolPoints;

        //int max = Mathf.Min(2, listap.Count);
        return sorted[UnityEngine.Random.Range(0, sorted.Count)];
    }

    public bool CanSeePlayer()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > viewDistance)
            return false;

        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle > viewAngle / 2f)
            return false;

        if (Physics.Raycast(transform.position, dirToPlayer, distance, obstacleMask))
            return false;

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Vector3 left = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + left * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + right * viewDistance);
    }
}
