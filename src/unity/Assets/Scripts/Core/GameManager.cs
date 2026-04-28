/**
 * 游戏管理器
 * 负责游戏状态管理、分数统计、会话记录
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;

/// <summary>
/// 游戏状态枚举
/// </summary>
public enum GameState
{
    Menu,       // 主菜单
    Tutorial,   // 教程
    Calibrating,// 校准中
    Playing,    // 游戏中
    Paused,     // 暂停
    GameOver    // 游戏结束
}

/// <summary>
/// 会话数据
/// </summary>
[System.Serializable]
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
}

/// <summary>
/// 游戏管理器
/// 单例模式，管理游戏全局状态
/// </summary>
public class GameManager : MonoBehaviour
{
    #region 单例
    public static GameManager Instance { get; private set; }
    #endregion

    #region 游戏配置
    [Header("游戏设置")]
    [Tooltip("游戏时长（秒）")]
    public float sessionDuration = 300f; // 5分钟
    
    [Tooltip("目标帧率")]
    public int targetFrameRate = 72;
    
    [Tooltip("是否记录会话数据")]
    public bool recordSession = true;
    #endregion

    #region 游戏状态
    [Header("游戏状态")]
    [SerializeField]
    private GameState currentState = GameState.Menu;
    
    [SerializeField]
    private int score = 0;
    
    [SerializeField]
    private float gameTimer = 0f;
    
    [SerializeField]
    private float averageAttention = 0f;
    
    [SerializeField]
    private float maxAttention = 0f;
    
    [SerializeField]
    private float focusStreak = 0f;
    
    [SerializeField]
    private float maxFocusStreak = 0f;
    
    [SerializeField]
    private int totalCollectibles = 0;
    
    [SerializeField]
    private int totalObstaclesHit = 0;
    #endregion

    #region 事件
    /// <summary>
    /// 游戏状态变化事件
    /// </summary>
    public event Action<GameState> OnGameStateChanged;
    
    /// <summary>
    /// 分数变化事件
    /// </summary>
    public event Action<int> OnScoreChanged;
    
    /// <summary>
    /// 游戏结束事件
    /// </summary>
    public event Action<SessionData> OnGameOver;
    
    /// <summary>
    /// 计时器更新事件
    /// </summary>
    public event Action<float> OnTimerUpdated;
    #endregion

    #region 私有变量
    private float attentionSum = 0f;
    private int attentionCount = 0;
    private SessionData currentSession;
    private BCIManager bciManager;
    #endregion

    #region Unity生命周期
    void Awake()
    {
        // 单例模式
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

        // 设置帧率
        Application.targetFrameRate = targetFrameRate;
    }

    void Start()
    {
        bciManager = BCIManager.Instance;
        if (bciManager != null)
        {
            bciManager.OnAttentionUpdated += OnAttentionUpdated;
        }
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
        {
            bciManager.OnAttentionUpdated -= OnAttentionUpdated;
        }
    }
    #endregion

    #region 游戏状态管理
    /// <summary>
    /// 开始新游戏
    /// </summary>
    public void StartGame()
    {
        // 重置游戏状态
        score = 0;
        gameTimer = 0f;
        averageAttention = 0f;
        maxAttention = 0f;
        focusStreak = 0f;
        maxFocusStreak = 0f;
        totalCollectibles = 0;
        totalObstaclesHit = 0;
        attentionSum = 0f;
        attentionCount = 0;

        // 创建会话数据
        if (recordSession)
        {
            currentSession = new SessionData
            {
                sessionId = Guid.NewGuid().ToString(),
                startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        // 切换到游戏场景
        SetGameState(GameState.Playing);
        SceneManager.LoadScene("StarGuardian");
        
        Debug.Log("游戏开始");
    }

    /// <summary>
    /// 暂停游戏
    /// </summary>
    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;
        
        SetGameState(GameState.Paused);
        Time.timeScale = 0f;
        Debug.Log("游戏暂停");
    }

    /// <summary>
    /// 恢复游戏
    /// </summary>
    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;
        
        SetGameState(GameState.Playing);
        Time.timeScale = 1f;
        Debug.Log("游戏恢复");
    }

    /// <summary>
    /// 结束游戏
    /// </summary>
    public void EndGame()
    {
        SetGameState(GameState.GameOver);
        Time.timeScale = 1f;

        // 计算统计数据
        CalculateStatistics();

        // 保存会话数据
        if (recordSession && currentSession != null)
        {
            currentSession.endTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            currentSession.duration = gameTimer;
            currentSession.score = score;
            currentSession.averageAttention = averageAttention;
            currentSession.maxAttention = maxAttention;
            currentSession.maxFocusStreak = maxFocusStreak;
            currentSession.totalCollectibles = totalCollectibles;
            currentSession.totalObstaclesHit = totalObstaclesHit;

            SaveSessionData(currentSession);
            OnGameOver?.Invoke(currentSession);
        }

        Debug.Log($"游戏结束 - 分数: {score}, 平均专注力: {averageAttention:F1}");
    }

    /// <summary>
    /// 返回主菜单
    /// </summary>
    public void ReturnToMenu()
    {
        SetGameState(GameState.Menu);
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// 设置游戏状态
    /// </summary>
    private void SetGameState(GameState newState)
    {
        currentState = newState;
        OnGameStateChanged?.Invoke(currentState);
    }
    #endregion

    #region 游戏逻辑
    /// <summary>
    /// 更新游戏计时器
    /// </summary>
    private void UpdateGameTimer()
    {
        gameTimer += Time.deltaTime;
        OnTimerUpdated?.Invoke(gameTimer);

        // 检查游戏是否结束
        if (gameTimer >= sessionDuration)
        {
            EndGame();
        }
    }

    /// <summary>
    /// 更新连续专注时间
    /// </summary>
    private void UpdateFocusStreak()
    {
        if (bciManager == null) return;

        float attention = bciManager.GetAttention();
        float focusThreshold = 70f;

        if (attention >= focusThreshold)
        {
            focusStreak += Time.deltaTime;
            maxFocusStreak = Mathf.Max(maxFocusStreak, focusStreak);
        }
        else
        {
            focusStreak = 0f;
        }
    }

    /// <summary>
    /// 专注力数据更新回调
    /// </summary>
    private void OnAttentionUpdated(float attention)
    {
        attentionSum += attention;
        attentionCount++;
        averageAttention = attentionSum / attentionCount;
        maxAttention = Mathf.Max(maxAttention, attention);
    }

    /// <summary>
    /// 玩家碰到障碍物
    /// </summary>
    public void OnPlayerHitObstacle()
    {
        totalObstaclesHit++;
        score = Mathf.Max(0, score - 10);
        OnScoreChanged?.Invoke(score);
        Debug.Log("碰到障碍物！分数 -10");
    }

    /// <summary>
    /// 收集物品
    /// </summary>
    public void OnCollectiblePickedUp()
    {
        totalCollectibles++;
        int bonus = 10;

        // 专注连击加分
        if (focusStreak >= 10f)
        {
            bonus += 20;
            Debug.Log("专注连击！额外 +20");
        }

        score += bonus;
        OnScoreChanged?.Invoke(score);
        Debug.Log($"收集物品！分数 +{bonus}");
    }

    /// <summary>
    /// 计算统计数据
    /// </summary>
    private void CalculateStatistics()
    {
        if (attentionCount > 0)
        {
            averageAttention = attentionSum / attentionCount;
        }
    }
    #endregion

    #region 数据记录
    /// <summary>
    /// 保存会话数据
    /// </summary>
    private void SaveSessionData(SessionData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            string filename = $"session_{data.sessionId}.json";
            string directory = Path.Combine(Application.persistentDataPath, "Sessions");
            
            // 创建目录
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string path = Path.Combine(directory, filename);
            File.WriteAllText(path, json);
            Debug.Log($"会话数据已保存: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"保存会话数据失败: {e.Message}");
        }
    }
    #endregion

    #region 公共接口
    /// <summary>
    /// 获取当前游戏状态
    /// </summary>
    public GameState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// 获取当前分数
    /// </summary>
    public int GetScore()
    {
        return score;
    }

    /// <summary>
    /// 获取游戏时间
    /// </summary>
    public float GetGameTimer()
    {
        return gameTimer;
    }

    /// <summary>
    /// 获取剩余时间
    /// </summary>
    public float GetRemainingTime()
    {
        return Mathf.Max(0, sessionDuration - gameTimer);
    }

    /// <summary>
    /// 获取平均专注力
    /// </summary>
    public float GetAverageAttention()
    {
        return averageAttention;
    }

    /// <summary>
    /// 获取最大专注力
    /// </summary>
    public float GetMaxAttention()
    {
        return maxAttention;
    }

    /// <summary>
    /// 获取连续专注时间
    /// </summary>
    public float GetFocusStreak()
    {
        return focusStreak;
    }

    /// <summary>
    /// 获取最大连续专注时间
    /// </summary>
    public float GetMaxFocusStreak()
    {
        return maxFocusStreak;
    }

    /// <summary>
    /// 获取收集物品总数
    /// </summary>
    public int GetTotalCollectibles()
    {
        return totalCollectibles;
    }

    /// <summary>
    /// 获取碰到障碍物总数
    /// </summary>
    public int GetTotalObstaclesHit()
    {
        return totalObstaclesHit;
    }

    /// <summary>
    /// 是否处于专注状态
    /// </summary>
    public bool IsFocused()
    {
        return focusStreak >= 10f;
    }
    #endregion
}
