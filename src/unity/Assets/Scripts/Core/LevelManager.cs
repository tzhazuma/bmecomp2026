using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LevelConfig
{
    public string levelName = "未命名关卡";
    [TextArea] public string description = "";
    public float duration = 180f;
    public float baseSpeedMin = 5f;
    public float baseSpeedMax = 10f;
    public float maxSpeedMult = 1f;

    public float obstacleSpawnMin = 0.8f;
    public float obstacleSpawnMax = 2.5f;
    public float obstacleSpeedMult = 1f;

    public float collectibleSpawnInterval = 1.5f;
    public int collectiblePoints = 10;

    public bool hasShield = false;
    public float specialThreshold = 0f; 

    public string specialMechanic = "none";
    public Color levelColor = Color.white;

    public int requiredStars = 0;
    public float minAvgAttention = 0f;
}

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public LevelConfig[] levels;
    public int maxStarsPerLevel = 3;
    public ParticleSystem levelCompleteEffect;

    private int currentLevelIndex = 0;
    private Dictionary<int, int> levelStars = new Dictionary<int, int>();
    private Dictionary<int, bool> unlockedCache = new Dictionary<int, bool>();

    public event System.Action<int> OnLevelChanged;
    public event System.Action<int, int> OnLevelCompleted;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        LoadProgress();
    }

    public void LoadLevel(int index)
    {
        if (index < 0 || index >= levels.Length) return;
        currentLevelIndex = index;
        var cfg = levels[index];

        if (GameManager.Instance)
        {
            GameManager.Instance.sessionDuration = cfg.duration;
        }

        var player = FindObjectOfType<PlayerController>();
        if (player)
        {
            player.minSpeed = cfg.baseSpeedMin;
            player.maxSpeed = cfg.baseSpeedMax * cfg.maxSpeedMult;
        }

        var obstacleMgr = FindObjectOfType<ObstacleManager>();
        if (obstacleMgr)
        {
            obstacleMgr.baseSpawnInterval = cfg.obstacleSpawnMax;
            obstacleMgr.minSpawnInterval = cfg.obstacleSpawnMin;
            obstacleMgr.SetDifficultyMultiplier(cfg.obstacleSpeedMult, 1f);
        }

        var collectMgr = FindObjectOfType<CollectibleManager>();
        if (collectMgr)
        {
            collectMgr.spawnInterval = cfg.collectibleSpawnInterval;
            collectMgr.pointsValue = cfg.collectiblePoints;
        }

        OnLevelChanged?.Invoke(index);
        Debug.Log($"加载关卡: {cfg.levelName}");
    }

    public void CompleteLevel(float avgAttention, int score)
    {
        int stars = CalculateStars(avgAttention, score);

        if (!levelStars.ContainsKey(currentLevelIndex) || levelStars[currentLevelIndex] < stars)
        {
            levelStars[currentLevelIndex] = stars;
            SaveProgress();
        }

        if (levelCompleteEffect)
            levelCompleteEffect.Play();

        OnLevelCompleted?.Invoke(currentLevelIndex, stars);

        if (currentLevelIndex + 1 < levels.Length)
            UnlockLevel(currentLevelIndex + 1);
    }

    private int CalculateStars(float avgAttention, int score)
    {
        var cfg = levels[currentLevelIndex];
        int baseScore = cfg.collectiblePoints * 20;

        if (avgAttention >= 75f && score >= baseScore * 2) return 3;
        if (avgAttention >= 60f || score >= baseScore) return 2;
        if (avgAttention >= 40f) return 1;
        return 0;
    }

    public void UnlockLevel(int index)
    {
        if (unlockedCache.ContainsKey(index))
            unlockedCache[index] = true;
        else
            unlockedCache.Add(index, true);
        SaveProgress();
    }

    public bool IsLevelUnlocked(int index)
    {
        if (index == 0) return true;
        if (unlockedCache.TryGetValue(index, out bool unlocked))
            return unlocked;
        return false;
    }

    public int GetStars(int index)
    {
        return levelStars.TryGetValue(index, out int stars) ? stars : 0;
    }

    public int GetCurrentLevel() => currentLevelIndex + 1;
    public int GetMaxLevel() => levels.Length;
    public LevelConfig GetCurrentLevelConfig() => levels[currentLevelIndex];

    public void NextLevel()
    {
        if (currentLevelIndex + 1 < levels.Length)
        {
            LoadLevel(currentLevelIndex + 1);
        }
    }

    public void RestartLevel()
    {
        LoadLevel(currentLevelIndex);
    }

    private void SaveProgress()
    {
        string json = JsonUtility.ToJson(new SaveData
        {
            levelStars = levelStars,
            unlockedLevels = new List<int>(unlockedCache.Where(kv => kv.Value).Select(kv => kv.Key))
        });
        PlayerPrefs.SetString("BCIVR_Progress", json);
        PlayerPrefs.Save();
    }

    private void LoadProgress()
    {
        string json = PlayerPrefs.GetString("BCIVR_Progress", "");
        if (string.IsNullOrEmpty(json)) return;
        try
        {
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data != null)
            {
                levelStars = data.levelStars ?? new Dictionary<int, int>();
                if (data.unlockedLevels != null)
                {
                    unlockedCache.Clear();
                    foreach (var idx in data.unlockedLevels)
                        unlockedCache[idx] = true;
                }
            }
        }
        catch { }
    }

    [System.Serializable]
    private class SaveData
    {
        public Dictionary<int, int> levelStars = new Dictionary<int, int>();
        public List<int> unlockedLevels = new List<int>();
    }
}
