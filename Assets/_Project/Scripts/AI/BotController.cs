using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(NavMeshAgent))]
public abstract class BotController : MonoBehaviour
{
    [Header("Bot Settings")]
    public bool botEnabled = false;
    public float reachThreshold = 0.5f;
    public float doorInteractRange = 1.5f;

    [Header("Hunter Avoidance")]
    public float hunterDetectRange = 4f;
    public float avoidanceStrength = 2f;

    private Transform hunterTransform;

    protected PlayerController player;
    protected NavMeshAgent agent;
    protected NavMeshPath path;

    protected List<LootItem> availableLoot = new List<LootItem>();
    protected EscapeDoor escapeDoor;

    protected enum BotState { Idle, CollectingLoot, Escaping, Done }
    protected BotState state = BotState.Idle;
    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    private const float STUCK_TIMEOUT = 1f;

    protected virtual void Awake()
    {
        player = GetComponent<PlayerController>();
        agent = GetComponent<NavMeshAgent>();
        path = new NavMeshPath();
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.enabled = false; 
    }

    void LateUpdate()
    {
        if (agent.enabled)
            agent.nextPosition = transform.position;
    }

    public void StartBot()
    {
        if (!botEnabled) return;
        availableLoot = FindObjectsByType<LootItem>(FindObjectsSortMode.None).ToList();
        escapeDoor = FindObjectsByType<EscapeDoor>(FindObjectsSortMode.None)[0];
        HunterAI hunter = FindFirstObjectByType<HunterAI>();
        if (hunter != null) hunterTransform = hunter.transform;
        agent.enabled = true;
        state = BotState.CollectingLoot;
        StartCoroutine(BotLoop());
    }

    public void StopBot()
    {
        StopAllCoroutines();
        if (agent.enabled) agent.ResetPath();
        agent.enabled = false;
        state = BotState.Done;
    }

    private IEnumerator BotLoop()
    {
        while (state != BotState.Done)
        {
            BotDecision();
            DriveMovement();
            if (Vector3.Distance(transform.position, lastPosition) < 0.05f)
            {
                stuckTimer += Time.fixedDeltaTime;
                if (stuckTimer >= STUCK_TIMEOUT)
                {
                    HandleBlockedDoors();
                    agent.ResetPath();
                    agent.enabled = false;
                    stuckTimer = 0f;
                    Vector2 pushDir = Random.insideUnitCircle.normalized;
                    float pushEnd = Time.time + 0.4f;
                    while (Time.time < pushEnd)
                    {
                        player.SetBotInput(pushDir);
                        yield return new WaitForFixedUpdate();
                    }

                    agent.enabled = true;
                    yield return new WaitForFixedUpdate();
                    BotDecision();
                }
            }
            else
            {
                stuckTimer = 0f;
            }
            lastPosition = transform.position;
            HandleBlockedDoors();
            yield return new WaitForFixedUpdate();
        }
    }

    protected abstract void BotDecision();

    private void DriveMovement()
    {
        if (!agent.enabled || agent.pathPending) return;

        Vector2 desired = new Vector2(agent.desiredVelocity.x, agent.desiredVelocity.y);
        if (hunterTransform != null)
        {
            float distToHunter = Vector2.Distance(transform.position, hunterTransform.position);
            if (distToHunter < hunterDetectRange && distToHunter > 0.1f)
                {
                    Vector2 awayFromHunter = ((Vector2)transform.position - 
                                            (Vector2)hunterTransform.position).normalized;
                    float weight = 1f - (distToHunter / hunterDetectRange);
                    desired += awayFromHunter * weight * avoidanceStrength;
                }
        }

        if (desired.magnitude > 0.1f)
            desired = desired.normalized;
        player.SetBotInput(desired);
    }

    private void HandleBlockedDoors()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, doorInteractRange);
        foreach (var hit in hits)
        {
            InteractiveObject obj = hit.GetComponent<InteractiveObject>();
            if (obj != null && obj.currentState == InteractiveObject.ObjectState.Blocked)
            {
                obj.ForceOpen();
            }
        }
    }

    protected LootItem GetNearestLoot()
    {
        availableLoot.RemoveAll(l => l == null); 

        LootItem nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var loot in availableLoot)
        {
            float dist = Vector2.Distance(transform.position, loot.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = loot;
            }
        }
        return nearest;
    }

    protected int TotalRemainingLootValue()
    {
        availableLoot.RemoveAll(l => l == null);
        int total = 0;
        foreach (var l in availableLoot)
            total += l.lootValue;
        return total;
    }

    protected bool HasMinimumLoot()
    {
        return GameManager.Instance != null &&
               player.totalLootValue >= GameManager.Instance.minimumLootRequired;
    }

    protected void SetDestination(Vector3 target)
    {
        if (agent.enabled)
            agent.SetDestination(target);
    }

    protected bool ReachedDestination()
    {
        if (!agent.enabled || agent.pathPending) return false;
        return agent.remainingDistance <= reachThreshold;
    }
}