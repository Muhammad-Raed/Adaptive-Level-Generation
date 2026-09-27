using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using NavMeshPlus.Components;

public class HunterAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Door Interaction")]
    public float doorOpenRange = 2f;

    [Header("Utility Weights")]
    public float chaseWeight = 1f;
    public float sabotageWeight = 1f;
    private HashSet<string> recentlySabotaged = new HashSet<string>();

    [Header("Chase Settings")]
    public float losRadius = 8f;
    public float chaseExponentBase = 2f;

    [Header("Timing")]
    public float decisionInterval = 1f;

    [Header("Speed")]
    public float baseSpeed = 3.5f;
    public static HunterAI Instance { get; private set; }

    private NavMeshAgent agent;
    private DirectorManager director;
    private float decisionTimer = 0f;
    private string currentMode = "chase";

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        director = DirectorManager.Instance;
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    void Update()
    {
        if (player == null || !agent.enabled || !agent.isOnNavMesh) return;

        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
        {
            decisionTimer = decisionInterval;
            MakeDecision();
        }

        if (currentMode == "chase")
            agent.SetDestination(player.position);

        TryOpenNearbyDoors();
    }

    private void TryOpenNearbyDoors()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position, doorOpenRange, 
            LayerMask.GetMask("Interoperable"));

        foreach (var hit in hits)
        {
            InteractiveObject obj = hit.GetComponent<InteractiveObject>();
            if (obj != null && obj.currentState == InteractiveObject.ObjectState.Blocked)
            {
                NavMeshObstacle obstacle = hit.GetComponent<NavMeshObstacle>();
                if (obstacle != null) obstacle.carving = false;

                obj.ForceOpen();

                StartCoroutine(ReenableCarveAfterDelay(obstacle));
            }
        }
    }

    private IEnumerator ReenableCarveAfterDelay(NavMeshObstacle obstacle)
    {
        yield return new WaitForSeconds(0.2f);
        if (obstacle != null) obstacle.carving = true;
    }

    private void MakeDecision()
    {
        float chaseUtility = CalculateChaseUtility();
        float sabotageUtility = CalculateSabotageUtility(
            out string bestTargetID, 
            out Vector3 bestTargetPos
        );

        if (chaseUtility * chaseWeight >= sabotageUtility * sabotageWeight)
        {
            currentMode = "chase";
            Debug.Log($"Hunter: CHASING (chase={chaseUtility:F2}, sabotage={sabotageUtility:F2})");
        }
        else
        {
            currentMode = "sabotage";
            agent.SetDestination(bestTargetPos);
            recentlySabotaged.Add(bestTargetID);
            StartCoroutine(ClearSabotageTarget(bestTargetID));
            Debug.Log($"Hunter: SABOTAGING {bestTargetID}");
        }
    }

    private IEnumerator ClearSabotageTarget(string id)
    {
        yield return new WaitForSeconds(5f);
        recentlySabotaged.Remove(id);
    }

    private float CalculateChaseUtility()
    {
        if (player == null) return 0f;

        float distance = Vector3.Distance(transform.position, player.position);
        bool hasLOS = HasLineOfSight();
        float distanceScore = 1f / (1f + distance * 0.1f);
        float losBonus = hasLOS ? 2f : 1f;

        return distanceScore * losBonus;
    }

    private float CalculateSabotageUtility(out string bestID, out Vector3 bestPos)
    {
        bestID = "";
        bestPos = transform.position;
        float bestScore = 0f;

        if (director == null) return 0f;

        foreach (var kvp in director.localHeatMap)
        {
            string id = kvp.Key;
            float heat = kvp.Value;
            if (recentlySabotaged.Contains(id)) continue;

            if (heat <= 0f) continue;
            Vector3 pos = director.GetObjectPosition(id);
            if (pos == Vector3.zero) continue;

            float distToObj = Vector3.Distance(transform.position, pos);
            float distScore = 1f / (1f + distToObj);

            float score = heat * distScore;

            if (score > bestScore)
            {
                bestScore = score;
                bestID = id;
                bestPos = pos;
            }
        }

        return bestScore;
    }

    private bool HasLineOfSight()
    {
        if (player == null) return false;
        Vector2 dir = player.position - transform.position;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir.normalized, losRadius);
        return hit.collider != null && hit.collider.CompareTag("Player");
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            col.gameObject.GetComponent<PlayerController>()?.GetCaught();
        }
    }

    void Awake()
    {
        Instance = this;
    }

    public void ApplySpeedBoost(int unspentPoints)
    {
        float boost = unspentPoints * 0.02f; 
        agent.speed = baseSpeed + boost;
        Debug.Log($"Hunter speed boosted to {agent.speed}");
    }
}