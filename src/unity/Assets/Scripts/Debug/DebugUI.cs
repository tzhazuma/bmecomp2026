/**
 * 调试UI
 * 显示游戏调试信息
 */

using UnityEngine;

/// <summary>
/// 调试UI
/// 在游戏运行时显示调试信息
/// </summary>
public class DebugUI : MonoBehaviour
{
    #region 配置
    [Header("调试设置")]
    [Tooltip("是否显示调试UI")]
    public bool showDebugUI = true;
    
    [Tooltip("切换按键")]
    public KeyCode toggleKey = KeyCode.F1;
    
    [Tooltip("UI位置")]
    public UIPosition position = UIPosition.TopLeft;
    
    [Tooltip("UI缩放")]
    public float scale = 1f;
    #endregion

    #region 枚举
    public enum UIPosition
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
    #endregion

    #region 私有变量
    private bool isVisible = true;
    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private GUIStyle headerStyle;
    private BCIManager bciManager;
    private GameManager gameManager;
    private PlayerController playerController;
    private RewardSystem rewardSystem;
    #endregion

    #region Unity生命周期
    void Start()
    {
        bciManager = BCIManager.Instance;
        gameManager = GameManager.Instance;
        
        // 查找玩家控制器
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
        }
        
        // 查找奖励系统
        rewardSystem = FindObjectOfType<RewardSystem>();
    }

    void Update()
    {
        // 切换显示
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
        }
    }

    void OnGUI()
    {
        if (!showDebugUI || !isVisible) return;
        
        // 初始化样式
        InitializeStyles();
        
        // 计算位置
        Rect rect = CalculateRect();
        
        // 绘制UI
        GUILayout.BeginArea(rect);
        GUILayout.BeginVertical(boxStyle);
        
        DrawHeader();
        DrawBCIInfo();
        DrawGameInfo();
        DrawPlayerInfo();
        DrawRewardInfo();
        DrawPerformanceInfo();
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    #endregion

    #region UI绘制
    /// <summary>
    /// 初始化样式
    /// </summary>
    private void InitializeStyles()
    {
        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTexture(2, 2, new Color(0, 0, 0, 0.8f));
            boxStyle.padding = new RectOffset(10, 10, 10, 10);
        }
        
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = Mathf.RoundToInt(14 * scale);
        }
        
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.normal.textColor = Color.yellow;
            headerStyle.fontSize = Mathf.RoundToInt(16 * scale);
            headerStyle.fontStyle = FontStyle.Bold;
        }
    }

    /// <summary>
    /// 计算UI位置
    /// </summary>
    private Rect CalculateRect()
    {
        float width = 300 * scale;
        float height = 500 * scale;
        float margin = 10;
        
        switch (position)
        {
            case UIPosition.TopLeft:
                return new Rect(margin, margin, width, height);
            case UIPosition.TopRight:
                return new Rect(Screen.width - width - margin, margin, width, height);
            case UIPosition.BottomLeft:
                return new Rect(margin, Screen.height - height - margin, width, height);
            case UIPosition.BottomRight:
                return new Rect(Screen.width - width - margin, 
                    Screen.height - height - margin, width, height);
            default:
                return new Rect(margin, margin, width, height);
        }
    }

    /// <summary>
    /// 绘制头部
    /// </summary>
    private void DrawHeader()
    {
        GUILayout.Label("=== BCI-VR 调试信息 ===", headerStyle);
        GUILayout.Space(5);
    }

    /// <summary>
    /// 绘制BCI信息
    /// </summary>
    private void DrawBCIInfo()
    {
        GUILayout.Label("[ BCI 状态 ]", headerStyle);
        
        if (bciManager != null)
        {
            GUILayout.Label($"连接状态: {(bciManager.IsConnected() ? "已连接" : "未连接")}", labelStyle);
            GUILayout.Label($"服务器: {bciManager.serverHost}:{bciManager.serverPort}", labelStyle);
            GUILayout.Label($"接收速率: {bciManager.GetReceiveRate():F1} 包/秒", labelStyle);
            GUILayout.Label($"专注力: {bciManager.GetAttention():F1}", labelStyle);
            GUILayout.Label($"偏航角: {bciManager.GetYaw():F1}°", labelStyle);
            GUILayout.Label($"俯仰角: {bciManager.GetPitch():F1}°", labelStyle);
        }
        else
        {
            GUILayout.Label("BCI管理器未找到", labelStyle);
        }
        
        GUILayout.Space(5);
    }

    /// <summary>
    /// 绘制游戏信息
    /// </summary>
    private void DrawGameInfo()
    {
        GUILayout.Label("[ 游戏状态 ]", headerStyle);
        
        if (gameManager != null)
        {
            GUILayout.Label($"游戏状态: {gameManager.GetCurrentState()}", labelStyle);
            GUILayout.Label($"分数: {gameManager.GetScore()}", labelStyle);
            GUILayout.Label($"时间: {gameManager.GetGameTimer():F1}s / {gameManager.sessionDuration:F0}s", labelStyle);
            GUILayout.Label($"剩余时间: {gameManager.GetRemainingTime():F1}s", labelStyle);
            GUILayout.Label($"平均专注力: {gameManager.GetAverageAttention():F1}", labelStyle);
            GUILayout.Label($"最大专注力: {gameManager.GetMaxAttention():F1}", labelStyle);
            GUILayout.Label($"连续专注: {gameManager.GetFocusStreak():F1}s", labelStyle);
            GUILayout.Label($"最大连续专注: {gameManager.GetMaxFocusStreak():F1}s", labelStyle);
            GUILayout.Label($"收集物: {gameManager.GetTotalCollectibles()}", labelStyle);
            GUILayout.Label($"障碍物碰撞: {gameManager.GetTotalObstaclesHit()}", labelStyle);
        }
        else
        {
            GUILayout.Label("游戏管理器未找到", labelStyle);
        }
        
        GUILayout.Space(5);
    }

    /// <summary>
    /// 绘制玩家信息
    /// </summary>
    private void DrawPlayerInfo()
    {
        GUILayout.Label("[ 玩家状态 ]", headerStyle);
        
        if (playerController != null)
        {
            GUILayout.Label($"当前速度: {playerController.GetCurrentSpeed():F1}", labelStyle);
            GUILayout.Label($"速度比例: {playerController.GetSpeedRatio():P1}", labelStyle);
            GUILayout.Label($"专注状态: {(playerController.IsFocused() ? "是" : "否")}", labelStyle);
            GUILayout.Label($"位置: {playerController.transform.position}", labelStyle);
        }
        else
        {
            GUILayout.Label("玩家控制器未找到", labelStyle);
        }
        
        GUILayout.Space(5);
    }

    /// <summary>
    /// 绘制奖励信息
    /// </summary>
    private void DrawRewardInfo()
    {
        GUILayout.Label("[ 奖励系统 ]", headerStyle);
        
        if (rewardSystem != null)
        {
            GUILayout.Label($"连击状态: {(rewardSystem.IsComboActive() ? "激活" : "未激活")}", labelStyle);
            GUILayout.Label($"连击次数: {rewardSystem.GetComboCount()}", labelStyle);
            GUILayout.Label($"等级: {rewardSystem.GetLevel()}", labelStyle);
            GUILayout.Label($"经验: {rewardSystem.GetExperience()} / {rewardSystem.GetExperienceToNextLevel()}", labelStyle);
            GUILayout.Label($"成就: {rewardSystem.GetUnlockedAchievementCount()} / {rewardSystem.GetAchievements().Count}", labelStyle);
        }
        else
        {
            GUILayout.Label("奖励系统未找到", labelStyle);
        }
        
        GUILayout.Space(5);
    }

    /// <summary>
    /// 绘制性能信息
    /// </summary>
    private void DrawPerformanceInfo()
    {
        GUILayout.Label("[ 性能信息 ]", headerStyle);
        
        float fps = 1.0f / Time.deltaTime;
        GUILayout.Label($"FPS: {fps:F1}", labelStyle);
        GUILayout.Label($"内存: {System.GC.GetTotalMemory(false) / 1024 / 1024} MB", labelStyle);
        GUILayout.Label($"帧时间: {Time.deltaTime * 1000:F1} ms", labelStyle);
    }
    #endregion

    #region 工具方法
    /// <summary>
    /// 创建纹理
    /// </summary>
    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        
        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        
        return texture;
    }
    #endregion
}
