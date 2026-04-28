/**
 * UI管理器
 * 负责管理游戏UI显示
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI管理器
/// 管理游戏中的所有UI元素
/// </summary>
public class UIManager : MonoBehaviour
{
    #region 单例
    public static UIManager Instance { get; private set; }
    #endregion

    #region UI引用
    [Header("HUD元素")]
    [Tooltip("分数文本")]
    public TextMeshProUGUI scoreText;
    
    [Tooltip("时间文本")]
    public TextMeshProUGUI timerText;
    
    [Tooltip("专注力条")]
    public Slider attentionBar;
    
    [Tooltip("速度条")]
    public Slider speedBar;
    
    [Tooltip("连击指示器")]
    public GameObject comboIndicator;
    
    [Tooltip("连击文本")]
    public TextMeshProUGUI comboText;
    
    [Tooltip("等级文本")]
    public TextMeshProUGUI levelText;
    
    [Tooltip("经验条")]
    public Slider experienceBar;
    
    [Header("状态面板")]
    [Tooltip("暂停面板")]
    public GameObject pausePanel;
    
    [Tooltip("游戏结束面板")]
    public GameObject gameOverPanel;
    
    [Tooltip("最终分数")]
    public TextMeshProUGUI finalScoreText;
    
    [Tooltip("最终统计")]
    public TextMeshProUGUI finalStatsText;
    
    [Header("成就通知")]
    [Tooltip("成就通知面板")]
    public GameObject achievementNotification;
    
    [Tooltip("成就名称")]
    public TextMeshProUGUI achievementNameText;
    
    [Tooltip("成就描述")]
    public TextMeshProUGUI achievementDescText;
    
    [Header("校准UI")]
    [Tooltip("校准面板")]
    public GameObject calibrationPanel;
    
    [Tooltip("校准进度条")]
    public Slider calibrationProgress;
    
    [Tooltip("校准提示文本")]
    public TextMeshProUGUI calibrationText;
    #endregion

    #region 私有变量
    private BCIManager bciManager;
    private GameManager gameManager;
    private RewardSystem rewardSystem;
    private PlayerController playerController;
    private float achievementNotificationTimer = 0f;
    #endregion

    #region Unity生命周期
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 获取管理器引用
        bciManager = BCIManager.Instance;
        gameManager = GameManager.Instance;
        rewardSystem = FindObjectOfType<RewardSystem>();
        playerController = FindObjectOfType<PlayerController>();
        
        // 注册事件
        RegisterEvents();
        
        // 初始化UI
        InitializeUI();
    }

    void Update()
    {
        UpdateHUD();
        UpdateAchievementNotification();
    }

    void OnDestroy()
    {
        UnregisterEvents();
    }
    #endregion

    #region 初始化
    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        // 隐藏所有面板
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (achievementNotification != null) achievementNotification.SetActive(false);
        if (calibrationPanel != null) calibrationPanel.SetActive(false);
        
        // 初始化分数
        UpdateScore(0);
        
        // 初始化时间
        if (gameManager != null)
        {
            UpdateTimer(gameManager.sessionDuration);
        }
    }

    /// <summary>
    /// 注册事件
    /// </summary>
    private void RegisterEvents()
    {
        if (bciManager != null)
        {
            bciManager.OnAttentionUpdated += OnAttentionUpdated;
        }
        
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += OnGameStateChanged;
            gameManager.OnScoreChanged += UpdateScore;
            gameManager.OnTimerUpdated += OnTimerUpdated;
            gameManager.OnGameOver += OnGameOver;
        }
        
        if (rewardSystem != null)
        {
            rewardSystem.rewardEvents.OnComboActivated += OnComboActivated;
            rewardSystem.rewardEvents.OnComboDeactivated += OnComboDeactivated;
            rewardSystem.rewardEvents.OnAchievementUnlocked += OnAchievementUnlocked;
            rewardSystem.rewardEvents.OnLevelUp += OnLevelUp;
        }
    }

    /// <summary>
    /// 取消事件注册
    /// </summary>
    private void UnregisterEvents()
    {
        if (bciManager != null)
        {
            bciManager.OnAttentionUpdated -= OnAttentionUpdated;
        }
        
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -= OnGameStateChanged;
            gameManager.OnScoreChanged -= UpdateScore;
            gameManager.OnTimerUpdated -= OnTimerUpdated;
            gameManager.OnGameOver -= OnGameOver;
        }
        
        if (rewardSystem != null)
        {
            rewardSystem.rewardEvents.OnComboActivated -= OnComboActivated;
            rewardSystem.rewardEvents.OnComboDeactivated -= OnComboDeactivated;
            rewardSystem.rewardEvents.OnAchievementUnlocked -= OnAchievementUnlocked;
            rewardSystem.rewardEvents.OnLevelUp -= OnLevelUp;
        }
    }
    #endregion

    #region UI更新
    /// <summary>
    /// 更新HUD
    /// </summary>
    private void UpdateHUD()
    {
        // 更新专注力条
        if (attentionBar != null && bciManager != null)
        {
            attentionBar.value = bciManager.GetAttention() / 100f;
        }
        
        // 更新速度条
        if (speedBar != null && playerController != null)
        {
            speedBar.value = playerController.GetSpeedRatio();
        }
        
        // 更新等级和经验
        if (rewardSystem != null)
        {
            if (levelText != null)
            {
                levelText.text = $"Lv.{rewardSystem.GetLevel()}";
            }
            
            if (experienceBar != null)
            {
                experienceBar.value = rewardSystem.GetExperienceRatio();
            }
        }
    }

    /// <summary>
    /// 更新分数
    /// </summary>
    private void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"分数: {score}";
        }
    }

    /// <summary>
    /// 更新计时器
    /// </summary>
    private void OnTimerUpdated(float gameTime)
    {
        if (timerText != null && gameManager != null)
        {
            float remaining = gameManager.GetRemainingTime();
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
    #endregion

    #region 事件处理
    /// <summary>
    /// 专注力更新
    /// </summary>
    private void OnAttentionUpdated(float attention)
    {
        // UI更新在UpdateHUD中处理
    }

    /// <summary>
    /// 游戏状态变化
    /// </summary>
    private void OnGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                ShowGameUI();
                break;
            case GameState.Paused:
                ShowPauseUI();
                break;
            case GameState.GameOver:
                ShowGameOverUI();
                break;
        }
    }

    /// <summary>
    /// 游戏结束
    /// </summary>
    private void OnGameOver(SessionData data)
    {
        ShowGameOverUI(data);
    }

    /// <summary>
    /// 连击激活
    /// </summary>
    private void OnComboActivated()
    {
        if (comboIndicator != null)
        {
            comboIndicator.SetActive(true);
        }
        
        if (comboText != null)
        {
            comboText.text = "专注连击!";
        }
    }

    /// <summary>
    /// 连击结束
    /// </summary>
    private void OnComboDeactivated()
    {
        if (comboIndicator != null)
        {
            comboIndicator.SetActive(false);
        }
    }

    /// <summary>
    /// 成就解锁
    /// </summary>
    private void OnAchievementUnlocked(string achievementName)
    {
        ShowAchievementNotification(achievementName, "成就已解锁！");
    }

    /// <summary>
    /// 升级
    /// </summary>
    private void OnLevelUp(int level)
    {
        ShowAchievementNotification($"升级！", $"恭喜达到 {level} 级！");
    }
    #endregion

    #region UI显示控制
    /// <summary>
    /// 显示游戏UI
    /// </summary>
    private void ShowGameUI()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    /// <summary>
    /// 显示暂停UI
    /// </summary>
    private void ShowPauseUI()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    /// <summary>
    /// 显示游戏结束UI
    /// </summary>
    private void ShowGameOverUI()
    {
        ShowGameOverUI(null);
    }

    /// <summary>
    /// 显示游戏结束UI
    /// </summary>
    private void ShowGameOverUI(SessionData data)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            // 更新最终分数
            if (finalScoreText != null && gameManager != null)
            {
                finalScoreText.text = $"最终分数: {gameManager.GetScore()}";
            }
            
            // 更新最终统计
            if (finalStatsText != null && data != null)
            {
                finalStatsText.text = $"平均专注力: {data.averageAttention:F1}%\n" +
                                     $"最大专注力: {data.maxAttention:F1}%\n" +
                                     $"最大连续专注: {data.maxFocusStreak:F1}秒\n" +
                                     $"收集物: {data.totalCollectibles}\n" +
                                     $"障碍物碰撞: {data.totalObstaclesHit}";
            }
        }
    }

    /// <summary>
    /// 显示成就通知
    /// </summary>
    private void ShowAchievementNotification(string title, string description)
    {
        if (achievementNotification != null)
        {
            achievementNotification.SetActive(true);
            
            if (achievementNameText != null)
            {
                achievementNameText.text = title;
            }
            
            if (achievementDescText != null)
            {
                achievementDescText.text = description;
            }
            
            achievementNotificationTimer = 3f;
        }
    }

    /// <summary>
    /// 更新成就通知
    /// </summary>
    private void UpdateAchievementNotification()
    {
        if (achievementNotificationTimer > 0)
        {
            achievementNotificationTimer -= Time.deltaTime;
            
            if (achievementNotificationTimer <= 0)
            {
                if (achievementNotification != null)
                {
                    achievementNotification.SetActive(false);
                }
            }
        }
    }
    #endregion

    #region 按钮回调
    /// <summary>
    /// 暂停按钮点击
    /// </summary>
    public void OnPauseButtonClicked()
    {
        if (gameManager != null)
        {
            gameManager.PauseGame();
        }
    }

    /// <summary>
    /// 恢复按钮点击
    /// </summary>
    public void OnResumeButtonClicked()
    {
        if (gameManager != null)
        {
            gameManager.ResumeGame();
        }
    }

    /// <summary>
    /// 重新开始按钮点击
    /// </summary>
    public void OnRestartButtonClicked()
    {
        if (gameManager != null)
        {
            gameManager.StartGame();
        }
    }

    /// <summary>
    /// 返回菜单按钮点击
    /// </summary>
    public void OnReturnToMenuButtonClicked()
    {
        if (gameManager != null)
        {
            gameManager.ReturnToMenu();
        }
    }
    #endregion

    #region 公共接口
    /// <summary>
    /// 显示校准UI
    /// </summary>
    public void ShowCalibrationUI(float progress, string message)
    {
        if (calibrationPanel != null)
        {
            calibrationPanel.SetActive(true);
            
            if (calibrationProgress != null)
            {
                calibrationProgress.value = progress;
            }
            
            if (calibrationText != null)
            {
                calibrationText.text = message;
            }
        }
    }

    /// <summary>
    /// 隐藏校准UI
    /// </summary>
    public void HideCalibrationUI()
    {
        if (calibrationPanel != null)
        {
            calibrationPanel.SetActive(false);
        }
    }
    #endregion
}
