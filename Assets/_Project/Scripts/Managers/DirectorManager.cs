using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DirectorManager : MonoBehaviour
{
    public static DirectorManager Instance { get; private set; }

    [Header("Heat Map Data")]
    public Dictionary<string, int> globalTendency = new Dictionary<string, int>();
    public Dictionary<string, float> localHeatMap = new Dictionary<string, float>();

    [Header("Director Economy")]
    public int currentBudget;
    public int levelsEscaped = 0;

    [Header("Costs")]
    public int doorBlockCost = 20;
    public int windowBoardCost = 10;

    private Dictionary<string, Vector3> objectPositions = new Dictionary<string, Vector3>();
    private Dictionary<string, InteractiveObject> registeredObjects = new Dictionary<string, InteractiveObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterObject(string id, string type, Vector3 position, InteractiveObject obj)
    {
        if (!localHeatMap.ContainsKey(id))
        {
            float initialHeat = globalTendency.ContainsKey(type) ? globalTendency[type] * 0.1f : 0f;
            localHeatMap[id] = initialHeat;
            objectPositions[id] = position;
            registeredObjects[id] = obj;
        }
    }

    public void UpdateHeatMap(string id, string type, bool isStrategic)
    {
        if (localHeatMap.ContainsKey(id))
        {
            float increment = isStrategic ? 2.0f : 1.0f;
            localHeatMap[id] += increment;

            if (!globalTendency.ContainsKey(type)) globalTendency[type] = 0;
            globalTendency[type]++;
        }
    }

    public Vector3 GetObjectPosition(string id)
    {
        return objectPositions.ContainsKey(id) ? objectPositions[id] : Vector3.zero;
    }
    public void RunPreMatchBudget()
    {
        levelsEscaped = GameManager.Instance != null ? GameManager.Instance.levelsEscaped : 0;
        currentBudget = levelsEscaped * 20;
        Debug.Log($"Director pre-match budget: {currentBudget} points.");

        if (levelsEscaped == 0)
        {
            Debug.Log("Level 0 - no pre-match director actions.");
            return;
        }

        var sortedObjects = registeredObjects
            .OrderByDescending(kvp =>
                globalTendency.ContainsKey(registeredObjects[kvp.Key].type.ToString())
                ? globalTendency[registeredObjects[kvp.Key].type.ToString()]
                : 0)
            .ToList();

        int doorsBlocked    = 0;
        int windowsBoarded  = 0;
        int maxDoorsToBlock   = levelsEscaped - 2;
        int maxWindowsToBoard = levelsEscaped - 1;

        foreach (var kvp in sortedObjects)
        {
            if (currentBudget <= 0) break;

            InteractiveObject obj = kvp.Value;
            if (obj == null) continue;

            if (obj.type == InteractiveObject.ObjectType.Door
                && obj.currentState == InteractiveObject.ObjectState.Open
                && doorsBlocked < maxDoorsToBlock
                && currentBudget >= doorBlockCost)
            {
                obj.currentState = InteractiveObject.ObjectState.Blocked;
                obj.UpdateCollider();
                currentBudget -= doorBlockCost;
                doorsBlocked++;
                Debug.Log($"Director blocked door {obj.uniqueID}. Budget left: {currentBudget}");
            }
            else if (obj.type == InteractiveObject.ObjectType.Window
                && obj.currentState == InteractiveObject.ObjectState.Open
                && windowsBoarded < maxWindowsToBoard
                && currentBudget >= windowBoardCost)
            {
                obj.currentState = InteractiveObject.ObjectState.Blocked;
                obj.UpdateCollider();
                currentBudget -= windowBoardCost;
                windowsBoarded++;
                Debug.Log($"Director boarded window {obj.uniqueID}. Budget left: {currentBudget}");
            }
        }

        if (currentBudget > 0 && HunterAI.Instance != null)
        {
            HunterAI.Instance.ApplySpeedBoost(currentBudget);
            Debug.Log($"Director converted {currentBudget} unspent points to hunter speed boost.");
            currentBudget = 0;
        }
    }

    public void ResetLocalHeatMap()
    {
        localHeatMap.Clear();
        objectPositions.Clear();
        registeredObjects.Clear();
    }
}