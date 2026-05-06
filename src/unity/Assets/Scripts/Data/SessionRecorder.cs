using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

[Serializable]
public class TrainingSample
{
    public float timestamp;
    public float attention;
    public float yaw;
    public float pitch;
    public float roll;
    public int score;
    public string eventTag;
}

[Serializable]
public class TrainingSession
{
    public string sessionId;
    public string startTime;
    public string endTime;
    public float duration;
    public int levelIndex;
    public string levelName;
    public int finalScore;
    public float averageAttention;
    public float maxAttention;
    public float minAttention;
    public float maxFocusStreak;
    public float focusTimePercent;
    public int totalCollectibles;
    public int totalObstaclesHit;
    public int difficultyLevel;
    public int stars;
    public List<TrainingSample> samples = new List<TrainingSample>();
}

public class SessionRecorder : MonoBehaviour
{
    public bool recordEnabled = true;
    public float sampleInterval = 0.1f;
    public string eventTag = "";

    private TrainingSession currentSession;
    private float lastSampleTime;
    private BCIManager bciManager;
    private GameManager gameManager;
    private LevelManager levelManager;

    public static SessionRecorder Instance { get; private set; }

    public event Action<TrainingSession> OnSessionCompleted;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        bciManager = BCIManager.Instance;
        gameManager = GameManager.Instance;
        levelManager = LevelManager.Instance;

        if (gameManager)
            gameManager.OnGameStateChanged += OnGameStateChange;
    }

    private void OnGameStateChange(GameState state)
    {
        if (state == GameState.Playing)
            StartNewSession();
        else if (state == GameState.GameOver)
            EndSession();
    }

    void Update()
    {
        if (!recordEnabled || currentSession == null) return;
        if (Time.time - lastSampleTime >= sampleInterval)
        {
            RecordSample();
            lastSampleTime = Time.time;
        }
    }

    public void StartNewSession()
    {
        currentSession = new TrainingSession
        {
            sessionId = Guid.NewGuid().ToString(),
            startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            levelIndex = levelManager ? levelManager.GetCurrentLevel() - 1 : 0,
            levelName = levelManager ? levelManager.GetCurrentLevelConfig().levelName : "",
            difficultyLevel = FindObjectOfType<AdaptiveDifficulty>()?.GetDifficulty() ?? 3,
            samples = new List<TrainingSample>()
        };
        lastSampleTime = Time.time;
        Debug.Log($"会话开始: {currentSession.sessionId}");
    }

    private void RecordSample()
    {
        if (currentSession == null) return;
        var sample = new TrainingSample
        {
            timestamp = Time.time,
            attention = bciManager ? bciManager.GetAttention() : 0f,
            yaw = bciManager ? bciManager.GetYaw() : 0f,
            pitch = bciManager ? bciManager.GetPitch() : 0f,
            score = gameManager ? gameManager.GetScore() : 0,
            eventTag = eventTag,
        };
        currentSession.samples.Add(sample);
        eventTag = "";
    }

    public void RecordEvent(string tag)
    {
        eventTag = tag;
    }

    public void EndSession()
    {
        if (currentSession == null) return;

        currentSession.endTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        currentSession.duration = Time.time;

        if (gameManager)
        {
            currentSession.finalScore = gameManager.GetScore();
            currentSession.totalCollectibles = gameManager.GetTotalCollectibles();
            currentSession.totalObstaclesHit = gameManager.GetTotalObstaclesHit();
            currentSession.maxAttention = gameManager.GetMaxAttention();
            currentSession.averageAttention = gameManager.GetAverageAttention();
            currentSession.maxFocusStreak = gameManager.GetMaxFocusStreak();
        }

        if (currentSession.samples.Count > 0)
        {
            currentSession.minAttention = float.MaxValue;
            float focusCount = 0;
            foreach (var s in currentSession.samples)
            {
                currentSession.minAttention = Mathf.Min(currentSession.minAttention, s.attention);
                if (s.attention >= 70f) focusCount++;
            }
            currentSession.focusTimePercent = (focusCount / currentSession.samples.Count) * 100f;
        }

        SaveSession();
        OnSessionCompleted?.Invoke(currentSession);
        Debug.Log($"会话结束: 平均专注力={currentSession.averageAttention:F1}, 得分={currentSession.finalScore}");
    }

    private void SaveSession()
    {
        try
        {
            string json = JsonUtility.ToJson(currentSession, true);
            string dir = Path.Combine(Application.persistentDataPath, "TrainingSessions");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"session_{currentSession.sessionId}.json");
            File.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"保存会话数据失败: {e.Message}");
        }
    }

    public TrainingSession GetCurrentSession() => currentSession;

    void OnDestroy()
    {
        if (gameManager)
            gameManager.OnGameStateChanged -= OnGameStateChange;
    }
}
