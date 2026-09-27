using UnityEngine;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Rules")]
    public int levelsEscaped = 0;
    public float timeRemaining;
    public int minimumLootRequired;

    [Header("References")]
    public PlayerController player;
    private EscapeDoor escapeDoor;

    private bool gameActive = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void OnLevelReady(Vector3 playerSpawnPosition)
    {
        timeRemaining = 120f + (10f * levelsEscaped);
        minimumLootRequired = 100 + (15 * levelsEscaped);

        if (player != null)
        {
            player.transform.position = playerSpawnPosition;
            Debug.Log($"Player spawned at {playerSpawnPosition}");
            player.ResetForNewLevel();
            player.EnableCollision();
            Debug.Log($"Player Collision enabled");
        }

        gameActive = true;
        Debug.Log($"Level ready. Time: {timeRemaining}s, Min loot: {minimumLootRequired}");

        TestingFramework tf = FindObjectsByType<TestingFramework>(FindObjectsSortMode.None).FirstOrDefault();
        tf?.NotifyLevelReady();
    }

    void Update()
    {
        if (!gameActive) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            OnTimeUp();
        }
    }

    public void OnLootCollected(int totalLoot)
    {
        if (totalLoot >= minimumLootRequired && escapeDoor != null)
        {
            Debug.Log("Minimum loot reached! Escape door unlocked.");
            escapeDoor.Unlock();
            if (GameUI.Instance != null) GameUI.Instance.OnEscapeUnlocked();
        }
    }

    public void RegisterEscapeDoor(EscapeDoor door)
    {
        escapeDoor = door;
    }

    public void OnPlayerEscaped()
    {
        if (!gameActive) return;
        gameActive = false;
        levelsEscaped++;
        if (GameUI.Instance != null) GameUI.Instance.ShowWinScreen();
        escapeDoor = null;
        Debug.Log($"Escaped! Levels completed: {levelsEscaped}");
        TestingFramework tf = FindObjectsByType<TestingFramework>(FindObjectsSortMode.None).FirstOrDefault();
        tf?.NotifyEscaped();
    }

    public void OnPlayerCaught()
    {
        
        if (!gameActive) return;
        gameActive = false;
        if (GameUI.Instance != null) GameUI.Instance.ShowLoseScreen("You were caught.");
        Debug.Log("Game Over - Player caught.");
        TestingFramework tf = FindObjectsByType<TestingFramework>(FindObjectsSortMode.None).FirstOrDefault();
        tf?.NotifyFailed();
    }

    private void OnTimeUp()
    {
        
        if (!gameActive) return;
        gameActive = false;
        if (GameUI.Instance != null) GameUI.Instance.ShowLoseScreen("You ran out of time.");
        Debug.Log("Game Over - Time ran out.");
        TestingFramework tf = FindObjectsByType<TestingFramework>(FindObjectsSortMode.None).FirstOrDefault();
        tf?.NotifyFailed();
    }
}