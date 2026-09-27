using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
using System.Collections;
using NavMeshPlus.Components;

public class LevelGenerator : MonoBehaviour
{
    [Header("AI Navigation")]
    public NavMeshSurface surface;
    public HunterAI hunter;

    [Header("Cycle Settings")]
    [Range(0f, 1f)]
    public float cycleChance = 0.3f;

    private List<Edge> rejectedEdges = new List<Edge>();

    [Header("Interoperables")]
    public GameObject doorPrefab;
    public GameObject windowPrefab;

    [Header("Corridors")]
    public GameObject corridorPrefab;

    [Header("Prefabs")]
    public GameObject roomPrefab;
    public GameObject escapeDoorPrefab;

    [Header("Generation Settings")]
    public int levelsEscaped = 0;
    public float spawnRadius = 20f;

    private List<GameObject> spawnedRooms = new List<GameObject>();
    private List<Edge> edges = new List<Edge>();

    [HideInInspector] public bool managedByTestFramework = false;

    void Start()
    {
        if (!managedByTestFramework)
            StartCoroutine(GenerateRoutine());
    }

    IEnumerator GenerateRoutine()
    {
        GenerateLevel();

        yield return new WaitForSeconds(0.5f);

        if (surface != null)
        {
            surface.BuildNavMesh();
            Debug.Log("NavMesh Built!");
        }
        yield return new WaitForFixedUpdate();

        if (hunter != null && spawnedRooms.Count > 0)
        {
            var agent = hunter.GetComponent<NavMeshAgent>();
            agent.enabled = true;
            Vector3 hunterSpawn = spawnedRooms[spawnedRooms.Count - 1].transform.position;
            hunterSpawn.z = 0;

            if (NavMesh.SamplePosition(hunterSpawn, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                Debug.Log("Hunter placed on NavMesh successfully.");
            }
            else
            {
                Debug.LogError("Hunter could not find NavMesh at spawn position!");
            }

            var aiScript = hunter.GetComponent<HunterAI>();
            if (aiScript != null) aiScript.enabled = true;
        }
        Vector3 playerSpawn = spawnedRooms[0].transform.position;
        playerSpawn.z = 0;
        GameManager.Instance?.OnLevelReady(playerSpawn);

        yield return new WaitForEndOfFrame();
        DirectorManager.Instance?.RunPreMatchBudget();
    }

    public void GenerateLevel()
    {
        int roomCount = 1 + 9 + (2 * levelsEscaped);
        Debug.Log($"Generating {roomCount} rooms.");

        for (int i = 0; i < roomCount; i++)
        {
            Vector2 randomPos = Random.insideUnitCircle * spawnRadius;
            GameObject newRoom = Instantiate(roomPrefab, new Vector3(randomPos.x, randomPos.y, 0), Quaternion.identity);
            newRoom.transform.parent = this.transform;
            spawnedRooms.Add(newRoom);
        }

        ResolveRoomOverlaps();
        GenerateConnections(spawnedRooms);
        SpawnInteroperables(edges);
        SpawnCorridors(edges);
        ApplyRoomWallGaps();
        SpawnLootInRooms();
        SpawnEscapeDoor();
    }

    private void SpawnLootInRooms()
    {
        Dictionary<GameObject, int> connectionCount = new Dictionary<GameObject, int>();
        foreach (var room in spawnedRooms)
            connectionCount[room] = 0;

        foreach (var edge in edges)
        {
            foreach (var room in spawnedRooms)
            {
                float distU = Vector3.Distance(room.transform.position, edge.U);
                float distV = Vector3.Distance(room.transform.position, edge.V);
                if (distU < 1f) connectionCount[room]++;
                if (distV < 1f) connectionCount[room]++;
            }
        }

        foreach (var room in spawnedRooms)
        {
            RoomLootSpawner spawner = room.GetComponent<RoomLootSpawner>();
            if (spawner != null)
            {
                bool isLeaf = connectionCount[room] <= 1;
                spawner.SpawnLoot(isLeaf);
            }
        }
    }

    public void GenerateConnections(List<GameObject> rooms)
    {
        if (rooms.Count < 2) return;

        rejectedEdges.Clear();
        List<float> mstEdgeLengths = new List<float>();

        HashSet<(int, int)> connectedPairs = new HashSet<(int, int)>();

        List<GameObject> reached   = new List<GameObject>(rooms);
        List<GameObject> unreached = new List<GameObject>(rooms);

        reached.Clear();
        reached.Add(unreached[0]);
        unreached.RemoveAt(0);

        while (unreached.Count > 0)
        {
            float minDist      = float.MaxValue;
            int reachedIndex   = 0;
            int unreachedIndex = 0;

            for (int i = 0; i < reached.Count; i++)
            {
                for (int j = 0; j < unreached.Count; j++)
                {
                    float d = Vector3.Distance(
                        reached[i].transform.position,
                        unreached[j].transform.position
                    );
                    if (d < minDist)
                    {
                        minDist        = d;
                        reachedIndex   = i;
                        unreachedIndex = j;
                    }
                }
            }
            int idA = rooms.IndexOf(reached[reachedIndex]);
            int idB = rooms.IndexOf(unreached[unreachedIndex]);
            var pair = idA < idB ? (idA, idB) : (idB, idA);

            if (!connectedPairs.Contains(pair))
            {
                connectedPairs.Add(pair);
                Edge chosenEdge = new Edge(
                    reached[reachedIndex].transform.position,
                    unreached[unreachedIndex].transform.position
                );
                edges.Add(chosenEdge);
                mstEdgeLengths.Add(minDist);

                Vector3 newNodePos = unreached[unreachedIndex].transform.position;
                for (int i = 0; i < reached.Count; i++)
                {
                    if (i == reachedIndex) continue;
                    Edge candidate = new Edge(reached[i].transform.position, newNodePos);
                    bool alreadyCaptured = rejectedEdges.Any(e => Edge.AreEqual(e, candidate));
                    if (!alreadyCaptured)
                        rejectedEdges.Add(candidate);
                }
            }

            reached.Add(unreached[unreachedIndex]);
            unreached.RemoveAt(unreachedIndex);
        }

        AddCycleEdges(mstEdgeLengths);

        foreach (var edge in edges)
            Debug.DrawLine(edge.U, edge.V, Color.green, 100f);
    }

    private void AddCycleEdges(List<float> mstEdgeLengths)
    {
        float avgMstLength = mstEdgeLengths.Sum() / mstEdgeLengths.Count;
        float maxCycleLength = avgMstLength * 1.2f;

        rejectedEdges.Sort((a, b) =>
            Vector3.Distance(a.U, a.V).CompareTo(Vector3.Distance(b.U, b.V))
        );

        foreach (Edge candidate in rejectedEdges)
        {
            float len = Vector3.Distance(candidate.U, candidate.V);
            if (len > maxCycleLength) continue;      // too far — would skip rooms
            if (Random.value > cycleChance) continue;

            bool duplicate = edges.Any(e => Edge.AreEqual(e, candidate));
            if (!duplicate)
            {
                edges.Add(candidate);
                Debug.DrawLine(candidate.U, candidate.V, Color.cyan, 100f);
            }
        }
    }

    private void SpawnCorridors(List<Edge> edges)
    {
        float roomHalfSize = 5f;
        foreach (Edge edge in edges)
        {
            Vector3 start = edge.U;
            Vector3 end = edge.V;

            Vector3 direction = (end - start).normalized;
            Vector3 adjustedStart = start + direction * roomHalfSize;
            Vector3 adjustedEnd   = end   - direction * roomHalfSize;
            float length = Vector3.Distance(adjustedStart, adjustedEnd);
            if (length <= 0f) continue;

            Vector3 midPoint = (adjustedStart + adjustedEnd) / 2f;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            GameObject corridor = Instantiate(corridorPrefab, midPoint, Quaternion.identity);
            corridor.transform.parent = this.transform;
            corridor.transform.rotation = Quaternion.Euler(0, 0, angle);
            corridor.transform.localScale = new Vector3(length, 3f, 1f);
        }
    }

    private void ApplyRoomWallGaps()
    {
        Dictionary<GameObject, List<Vector3>> roomExitPoints =
            new Dictionary<GameObject, List<Vector3>>();

        foreach (var room in spawnedRooms)
            roomExitPoints[room] = new List<Vector3>();

        foreach (var edge in edges)
        {
            GameObject roomU = GetRoomAtPosition(edge.U);
            GameObject roomV = GetRoomAtPosition(edge.V);
            if (roomU == null || roomV == null) continue;

            Vector3 direction = (edge.V - edge.U).normalized;

            Vector3 exitFromU = ClampExitToFace(edge.U, direction,  5.0f);
            Vector3 exitFromV = ClampExitToFace(edge.V, -direction, 5.0f);

            roomExitPoints[roomU].Add(exitFromU);
            roomExitPoints[roomV].Add(exitFromV);
        }

        foreach (var room in spawnedRooms)
        {
            RoomWalls walls = room.GetComponent<RoomWalls>();
            if (walls != null)
                walls.ApplyCorridorGaps(roomExitPoints[room]);
        }
    }

    private Vector3 ClampExitToFace(Vector3 roomCentre, Vector3 direction, float halfSize)
    {
        float tX = Mathf.Abs(direction.x) > 0.0001f
            ? (halfSize * Mathf.Sign(direction.x) - 0f) / direction.x
            : float.MaxValue;
        float tY = Mathf.Abs(direction.y) > 0.0001f
            ? (halfSize * Mathf.Sign(direction.y) - 0f) / direction.y
            : float.MaxValue;

        float t = Mathf.Min(tX, tY);
        Vector3 localExit = direction * t;

        localExit.x = Mathf.Clamp(localExit.x, -halfSize, halfSize);
        localExit.y = Mathf.Clamp(localExit.y, -halfSize, halfSize);

        return roomCentre + localExit;
    }

    private GameObject GetRoomAtPosition(Vector3 pos)
    {
        return spawnedRooms.FirstOrDefault(
            r => Vector3.Distance(r.transform.position, pos) < 1f);
    }

    private void SpawnEscapeDoor()
    {
        if (escapeDoorPrefab == null) return;

        GameObject lastRoom = spawnedRooms[spawnedRooms.Count - 1];
        Vector3 spawnPos = lastRoom.transform.position;
        spawnPos.z = 0f;

        GameObject door = Instantiate(escapeDoorPrefab, spawnPos, Quaternion.identity);
        door.transform.parent = this.transform;

        GameManager.Instance?.RegisterEscapeDoor(door.GetComponent<EscapeDoor>());
    }


    private void ResolveRoomOverlaps()
    {
        bool overlapping = true;
        int maxIterations = 50;
        int currentIteration = 0;

        while (overlapping && currentIteration < maxIterations)
        {
            overlapping = false;
            currentIteration++;

            for (int i = 0; i < spawnedRooms.Count; i++)
            {
                for (int j = i + 1; j < spawnedRooms.Count; j++)
                {
                    Transform a = spawnedRooms[i].transform;
                    Transform b = spawnedRooms[j].transform;

                    float distance = Vector3.Distance(a.position, b.position);
                    float minSafeDistance = 15f;

                    if (distance < minSafeDistance)
                    {
                        overlapping = true;
                        Vector3 pushDir = (a.position - b.position).normalized;
                        if (pushDir == Vector3.zero) pushDir = Random.insideUnitCircle.normalized;
                        a.position += pushDir * 0.5f;
                        b.position -= pushDir * 0.5f;
                    }
                }
            }
        }
    }

    public void ResetGeneratorState()
    {
        spawnedRooms.Clear();
        edges.Clear();
        rejectedEdges.Clear();
    }

    private void SpawnInteroperables(List<Edge> edges)
    {
        foreach (Edge edge in edges)
        {
            Vector3 middlePoint = (edge.U + edge.V) / 2f;
            GameObject prefabToSpawn = (Random.value > 0.3f) ? doorPrefab : windowPrefab;
            GameObject spawnedObj = Instantiate(prefabToSpawn, middlePoint, Quaternion.identity);
            spawnedObj.transform.parent = this.transform;

            Vector3 direction = (edge.U - edge.V).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            spawnedObj.transform.rotation = Quaternion.Euler(0, 0, angle);

            InteractiveObject interactScript = spawnedObj.GetComponent<InteractiveObject>();
            if (interactScript != null)
                interactScript.startRoomDirection = (edge.U - middlePoint).normalized;
        }
    }

    public List<GameObject> GetSpawnedRooms() => spawnedRooms;
}