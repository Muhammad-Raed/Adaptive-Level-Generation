using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using System.IO;
using System.Text;

public class TestingFramework : MonoBehaviour
{
    public enum Scenario { Static, Random, Adaptive }
    public enum BotArchetype { LootGreedy, Speedrun, Neutral }

    [Header("Test Configuration")]
    public bool runTests = false;
    public int runsPerCombination = 3;  

    [Header("References")]
    public LevelGenerator levelGenerator;
    public GameObject playerObject; 
    private struct RunResult
    {
        public Scenario scenario;
        public BotArchetype bot;
        public int runIndex;
        public float timeToEscape; 
        public int lootCollected;
        public bool escaped;
    }

    private List<RunResult> results = new List<RunResult>();
    private BotController activeBot;
    private float runStartTime;
    private bool runComplete;
    private bool runEscaped;

    void Start()
    {
        if (runTests)
        {
            levelGenerator.managedByTestFramework = true;
            if (GameUI.Instance != null)
            {
                if (GameUI.Instance.winScreen != null) GameUI.Instance.winScreen.SetActive(false);
                if (GameUI.Instance.loseScreen != null) GameUI.Instance.loseScreen.SetActive(false);
                if (GameUI.Instance.restartButton != null) GameUI.Instance.restartButton.gameObject.SetActive(false);
            }
            StartCoroutine(RunAllTests());
        }
    }

    private IEnumerator RunAllTests()
    {
        Debug.Log("=== TESTING FRAMEWORK STARTED ===");

        foreach (Scenario scenario in System.Enum.GetValues(typeof(Scenario)))
        {
            foreach (BotArchetype botType in System.Enum.GetValues(typeof(BotArchetype)))
            {
                for (int run = 0; run < runsPerCombination; run++)
                {
                    Debug.Log($"[Test] Scenario: {scenario} | Bot: {botType} | Run: {run + 1}/{runsPerCombination}");

                    yield return StartCoroutine(RunSingleTest(scenario, botType, run));
                }
            }
        }

        ExportResults();
        Debug.Log("TESTING FRAMEWORK COMPLETE, results saved to Assets/TestResults.csv ===");
    }

    private IEnumerator RunSingleTest(Scenario scenario, BotArchetype botType, int runIndex)
    {
        if (GameUI.Instance != null)
        {
            if (GameUI.Instance.winScreen != null) GameUI.Instance.winScreen.SetActive(false);
            if (GameUI.Instance.loseScreen != null) GameUI.Instance.loseScreen.SetActive(false);
            if (GameUI.Instance.restartButton != null) GameUI.Instance.restartButton.gameObject.SetActive(false);
        }
        ResetLevel();

        ApplyScenario(scenario);

        yield return new WaitUntil(() => GameManager.Instance != null && IsLevelReady());

        activeBot = AttachBot(botType);
        runStartTime = Time.time;
        runComplete  = false;
        runEscaped   = false;

        if (activeBot != null)
            activeBot.StartBot();
        yield return new WaitForSeconds(0.5f);
        var hunterAI = levelGenerator.hunter.GetComponent<HunterAI>();
        if (hunterAI != null) hunterAI.enabled = true;
        yield return new WaitUntil(() => runComplete);

        float elapsed = Time.time - runStartTime;
        int loot = playerObject != null
            ? playerObject.GetComponent<PlayerController>().totalLootValue
            : 0;

        results.Add(new RunResult
        {
            scenario      = scenario,
            bot           = botType,
            runIndex      = runIndex,
            timeToEscape  = runEscaped ? elapsed : -1f,
            lootCollected = loot,
            escaped       = runEscaped
        });
        if (activeBot != null)
        {
            activeBot.StopBot();
            Destroy(activeBot);
            activeBot = null;
        }
        yield return new WaitForSeconds(1f);
    }


    private void ApplyScenario(Scenario scenario)
    {
        if (DirectorManager.Instance == null) return;

        switch (scenario)
        {
            case Scenario.Static:
                DirectorManager.Instance.currentBudget = 0;
                break;

            case Scenario.Random:
                ApplyRandomDirector();
                break;

            case Scenario.Adaptive:
                break;
        }
    }

    private void ApplyRandomDirector()
    {
        if (DirectorManager.Instance == null) return;

        InteractiveObject[] allObjects = FindObjectsByType<InteractiveObject>(FindObjectsSortMode.None);
        int budget = DirectorManager.Instance.currentBudget;

        foreach (var obj in allObjects)
        {
            if (budget <= 0) break;
            if (Random.value > 0.5f) continue; 

            int cost = obj.type == InteractiveObject.ObjectType.Door
                ? DirectorManager.Instance.doorBlockCost
                : DirectorManager.Instance.windowBoardCost;

            if (budget >= cost && obj.currentState == InteractiveObject.ObjectState.Open)
            {
                obj.currentState = InteractiveObject.ObjectState.Blocked;
                obj.UpdateCollider();
                budget -= cost;
            }
        }
    }

    private BotController AttachBot(BotArchetype botType)
    {
        if (playerObject == null) return null;

        BotController existing = playerObject.GetComponent<BotController>();
        if (existing != null) Destroy(existing);

        BotController bot = null;
        switch (botType)
        {
            case BotArchetype.LootGreedy: bot = playerObject.AddComponent<LootGreedyBot>(); break;
            case BotArchetype.Speedrun:   bot = playerObject.AddComponent<SpeedrunBot>();   break;
            case BotArchetype.Neutral:    bot = playerObject.AddComponent<NeutralBot>();     break;
        }

        if (bot != null) bot.botEnabled = true;
        return bot;
    }

    private void ResetLevel()
    {
        Debug.Log($"Hunter position before reset: {levelGenerator.hunter.transform.position}");
        var hunterAI = levelGenerator.hunter.GetComponent<HunterAI>();
        if (hunterAI != null) hunterAI.enabled = false;

        var hunterAgent = levelGenerator.hunter.GetComponent<NavMeshAgent>();
        if (hunterAgent != null) hunterAgent.enabled = false;
        if (levelGenerator != null)
        {
            foreach (Transform child in levelGenerator.transform)
                Destroy(child.gameObject);

            DirectorManager.Instance?.ResetLocalHeatMap();

            PlayerController pc = playerObject?.GetComponent<PlayerController>();
            if (pc != null) pc.ResetForNewLevel();

            levelGenerator.ResetGeneratorState();
            levelGenerator.StartCoroutine(RegenerateLevel());
        }
    }

    private IEnumerator RegenerateLevel()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        levelGenerator.GenerateLevel();

        yield return new WaitForSeconds(0.5f);
        if (levelGenerator.surface != null)
            levelGenerator.surface.BuildNavMesh();

        yield return new WaitForFixedUpdate();

        var spawnedRooms = levelGenerator.GetSpawnedRooms();
        if (levelGenerator.hunter != null && spawnedRooms.Count > 0)
        {
            var hunterAgent = levelGenerator.hunter.GetComponent<NavMeshAgent>();
            hunterAgent.enabled = true;
            Vector3 hunterSpawn = spawnedRooms[spawnedRooms.Count - 1].transform.position;
            hunterSpawn.z = 0;

            if (NavMesh.SamplePosition(hunterSpawn, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
                hunterAgent.Warp(hit.position);
                yield return new WaitForFixedUpdate();

            var aiScript = levelGenerator.hunter.GetComponent<HunterAI>();
            if (aiScript != null) aiScript.enabled = false;
        }

        GameManager.Instance?.OnLevelReady(spawnedRooms[0].transform.position);

        yield return new WaitForEndOfFrame();
        DirectorManager.Instance?.RunPreMatchBudget();
    }

    public void NotifyEscaped()
    {
        runEscaped   = true;
        runComplete  = true;
    }

    public void NotifyFailed()
    {
        runEscaped  = false;
        runComplete = true;
    }
    private bool levelReady = false;
    public void NotifyLevelReady() { levelReady = true; }
    private bool IsLevelReady()    { bool r = levelReady; levelReady = false; return r; }


    private void ExportResults()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Scenario,Bot,Run,TimeToEscape,LootCollected,Escaped");

        foreach (var r in results)
        {
            sb.AppendLine($"{r.scenario},{r.bot},{r.runIndex + 1}," +
                          $"{(r.escaped ? r.timeToEscape.ToString("F2") : "FAILED")}," +
                          $"{r.lootCollected},{r.escaped}");
        }

        string path = Path.Combine(Application.dataPath, "TestResults.csv");
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"Results saved to: {path}");
    }
}