using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Simple animal AI that reacts to fire
/// </summary>
public class AnimalAI : MonoBehaviour
{
    [Header("AI Configuration")]
    [SerializeField] private bool isPredator = false;
    [SerializeField] private float detectionRadius = 20f;
    [SerializeField] private float fleeSpeed = 8f;
    [SerializeField] private float normalSpeed = 3f;

    private NavMeshAgent agent;
    private Vector3 fearTarget;
    private float fearLevel;
    private bool isFleeing;

    public bool IsPredator => isPredator;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        agent.speed = normalSpeed;
    }

    private void Update()
    {
        if (isFleeing)
        {
            UpdateFleeing();
        }
        else
        {
            UpdateNormalBehavior();
        }
    }

    public void SetFearTarget(Vector3 position, float intensity)
    {
        fearTarget = position;
        fearLevel = intensity;
        isFleeing = true;

        // Run away from fire
        Vector3 fleeDirection = (transform.position - position).normalized;
        Vector3 fleePosition = transform.position + fleeDirection * 30f;

        agent.speed = fleeSpeed;
        agent.SetDestination(fleePosition);
    }

    private void UpdateFleeing()
    {
        fearLevel -= Time.deltaTime * 0.2f;

        if (fearLevel <= 0 || Vector3.Distance(transform.position, fearTarget) > 50f)
        {
            isFleeing = false;
            agent.speed = normalSpeed;
        }
    }

    private void UpdateNormalBehavior()
    {
        // Simple wander behavior
        if (!agent.hasPath || agent.remainingDistance < 1f)
        {
            Vector3 randomDirection = Random.insideUnitSphere * 20f;
            randomDirection += transform.position;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, 20f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }
}