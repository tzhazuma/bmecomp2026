using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public enum GameState
{
    Menu,
    Calibrating,
    Playing,
    Paused,
    GameOver
}

[Serializable]
public class SessionData
{
    public string sessionId;
    public string startTime;
    public string endTime;
    public float duration;
    public int score;
    public float averageAttention;
    public float maxAttention;
    public float maxFocusStreak;
    public int totalCollectibles;
    public int totalObstaclesHit;
    public int shieldBlocks;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public float sessionDuration = 180f;
    public int targetFrameRate = 72;
    public bool recordSession = true;

    [SerializeField] private GameState currentState = GameState.Menu;
    [SerializeField] private int score = 0;
    [SerializeField] private float gameTimer = 0f;
    [SerializeField] private float averageAttention = 0f;
    [SerializeField] private float maxAttention = 0f;
    [SerializeField] private float focusStreak = 0f;
    [SerializeField] private float maxFocusStreak = 0f;
    [SerializeField] private int totalCollectibles = 0;
    [SerializeField] private int totalObstaclesHit = 0;
    [SerializeField] private int shieldBlocks = 0;

    public event Action<GameState> OnGameStateChanged;
    public event Action<int> OnScoreChanged;
    public event Action<float> OnTimerUpdated;
    public event Action<SessionData> OnGameOver;

    private float attentionSum = 0f;
    private int attentionCount = 0;
    private BCIManager bciManager;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        Application.targetFrameRate = targetFrameRate;
    }

    void Start()
    {
        bciManager = BCIManager.Instance;
        if (bciManager != null)
            bciManager.OnAttentionUpdated += OnAttentionUpdated;
    }

    void Update()
    {
        if (currentState == GameState.Playing)
        {
            UpdateGameTimer();
            UpdateFocusStreak();
        }
    }

    void OnDestroy()
    {
        if (bciManager != null)
            bciManager.OnAttentionUpdated -= OnAttentionUpdated;
    }

    public void StartGame()
    {
        score = 0;
        gameTimer = 0f;
        averageAttention = 0f;
        maxAttention = 0f;
        focusStreak = 0f;
        maxFocusStreak = 0f;
        totalCollectibles = 0;
        totalObstaclesHit = 0;
        shieldBlocks = 0;
        attentionSum = 0f;
        attentionCount = 0;

        var levelMgr = LevelManager.Instance;
        if (levelMgr)
        {
            levelMgr.LoadLevel(levelMgr.GetCurrentLevel() - 1);
            sessionDuration = levelMgr.GetCurrentLevelConfig().duration;
        }

        SetGameState(GameState.Playing);
        SceneManager.LoadScene("StarGuardian");
    }

    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;
        SetGameState(GameState.Paused);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;
        SetGameState(GameState.Playing);
        Time.timeScale = 1f;
    }

    public void EndGame()
    {
        SetGameState(GameState.GameOver);
        Time.timeScale = 1f;
        CalculateStatistics();

        if (recordSession)
        {
            var data = new SessionData
            {
                sessionId = Guid.NewGuid().ToString(),
                startTime = "Unknown",
                endTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                duration = gameTimer,
                score = score,
                averageAttention = averageAttention,
                maxAttention = maxAttention,
                maxFocusStreak = maxFocusStreak,
                totalCollectibles = totalCollectibles,
                totalObstaclesHit = totalObstaclesHit,
                shieldBlocks = shieldBlocks,
            };
            OnGameOver?.Invoke(data);
        }

        var levelMgr = LevelManager.Instance;
        if (levelMgr)
            levelMgr.CompleteLevel(averageAttention, score);
    }

    public void ReturnToMenu()
    {
        SetGameState(GameState.Menu);
        SceneManager.LoadScene("MainMenu");
    }

    private void SetGameState(GameState newState)
    {
        currentState = newState;
        OnGameStateChanged?.Invoke(currentState);
    }

    private void UpdateGameTimer()
    {
        gameTimer += Time.deltaTime;
        OnTimerUpdated?.Invoke(gameTimer);
        if (gameTimer >= sessionDuration)
            EndGame();
    }

    private void UpdateFocusStreak()
    {
        if (bciManager == null) return;
        float attention = bciManager.GetAttention();

        if (attention >= 70f)
        {
            focusStreak += Time.deltaTime;
            maxFocusStreak = Mathf.Max(maxFocusStreak, focusStreak);
        }
        else
        {
            focusStreak = 0f;
        }
    }

    private void OnAttentionUpdated(float attention)
    {
        attentionSum += attention;
        attentionCount++;
        averageAttention = attentionSum / attentionCount;
        maxAttention = Mathf.Max(maxAttention, attention);
    }

    public void OnPlayerHitObstacle()
    {
        totalObstaclesHit++;
        score = Mathf.Max(0, score - 10);
        OnScoreChanged?.Invoke(score);

        var recorder = SessionRecorder.Instance;
        if (recorder) recorder.RecordEvent("hit_obstacle");

        var feedback = FindObjectOfType<VisualFeedback>();
        if (feedback) feedback.enabled = false;
    }

    public void OnShieldBlockObstacle()
    {
        shieldBlocks++;
        score += 5;
        OnScoreChanged?.Invoke(score);

        var recorder = SessionRecorder.Instance;
        if (recorder) recorder.RecordEvent("shield_block");
    }

    public void OnCollectiblePickedUp()
    {
        totalCollectibles++;
        int bonus = 10;

        var feedback = FindObjectOfType<VisualFeedback>();
        if (feedback && feedback.IsComboActive())
        {
            bonus += 20;
            var collectMgr = FindObjectOfType<CollectibleManager>();
            if (collectMgr)
            {
                var player = FindObjectOfType<PlayerController>();
                if (player)
                    collectMgr.SpawnComboEffect(player.transform.position + Vector3.up * 2f);
            }
        }

        score += bonus;
        OnScoreChanged?.Invoke(score);

        var recorder = SessionRecorder.Instance;
        if (recorder) recorder.RecordEvent("collect_pickup");
    }

    private void CalculateStatistics()
    {
        if (attentionCount > 0)
            averageAttention = attentionSum / attentionCount;
    }

    public GameState GetCurrentState() => currentState;
    public int GetScore() => score;
    public float GetGameTimer() => gameTimer;
    public float GetRemainingTime() => Mathf.Max(0, sessionDuration - gameTimer);
    public float GetAverageAttention() => averageAttention;
    public float GetMaxAttention() => maxAttention;
    public float GetFocusStreak() => focusStreak;
    public float GetMaxFocusStreak() => maxFocusStreak;
    public int GetTotalCollectibles() => totalCollectibles;
    public int GetTotalObstaclesHit() => totalObstaclesHit;
    public int GetShieldBlocks() => shieldBlocks;
}
