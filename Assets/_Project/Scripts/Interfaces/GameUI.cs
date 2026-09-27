using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; private set; }

    [Header("HUD Elements")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI lootText;
    public TextMeshProUGUI lootThresholdText;
    public GameObject lootUnlockedBanner;

    [Header("End Screens")]
    public GameObject winScreen;
    public GameObject loseScreen;
    public TextMeshProUGUI winLevelsText;
    public TextMeshProUGUI loseReasonText;
    public Button restartButton;

    private bool bannerVisible = false;
    private float bannerTimer = 0f;
    private const float BANNER_DURATION = 3f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        HideEndScreens();
        if (lootUnlockedBanner != null) lootUnlockedBanner.SetActive(false);

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false);
            restartButton.onClick.AddListener(OnRestartClicked);
        }
    }

    void Update()
    {
        UpdateHUD();

        if (bannerVisible)
        {
            bannerTimer -= Time.deltaTime;
            if (bannerTimer <= 0f)
            {
                bannerVisible = false;
                if (lootUnlockedBanner != null) lootUnlockedBanner.SetActive(false);
            }
        }
    }

    private void UpdateHUD()
    {
        if (GameManager.Instance == null) return;

        // Timer
        if (timerText != null)
        {
            float t = Mathf.Max(0f, GameManager.Instance.timeRemaining);
            int minutes = Mathf.FloorToInt(t / 60f);
            int seconds = Mathf.FloorToInt(t % 60f);
            timerText.text = $"{minutes}:{seconds:00}";

            timerText.color = t <= 20f ? Color.red : Color.white;
        }

        if (lootText != null && GameManager.Instance.player != null)
        {
            int currentLoot = GameManager.Instance.player.totalLootValue;
            int minLoot = GameManager.Instance.minimumLootRequired;
            lootText.text = $"Loot: {currentLoot}";
            lootText.color = currentLoot >= minLoot ? Color.green : Color.white;
        }

        if (lootThresholdText != null)
        {
            lootThresholdText.text = $"Required: {GameManager.Instance.minimumLootRequired}";
        }
    }

    public void OnEscapeUnlocked()
    {
        if (lootUnlockedBanner != null)
        {
            lootUnlockedBanner.SetActive(true);
            bannerVisible = true;
            bannerTimer = BANNER_DURATION;
        }
    }


    public void ShowWinScreen()
    {
        if (winScreen != null) winScreen.SetActive(true);
        if (restartButton != null) restartButton.gameObject.SetActive(true);
        if (winLevelsText != null && GameManager.Instance != null)
            winLevelsText.text = $"Levels Escaped: {GameManager.Instance.levelsEscaped}";
    }

    public void ShowLoseScreen(string reason)
    {
        if (loseScreen != null) loseScreen.SetActive(true);
        if (restartButton != null) restartButton.gameObject.SetActive(true);
        if (loseReasonText != null)
            loseReasonText.text = reason;
    }

    private void HideEndScreens()
    {
        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);
    }

    private void OnRestartClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}