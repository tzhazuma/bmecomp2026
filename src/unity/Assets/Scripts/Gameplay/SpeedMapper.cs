/**
 * 速度映射器
 * 负责将专注力值映射为游戏速度
 */

using UnityEngine;

/// <summary>
/// 速度映射配置
/// </summary>
[System.Serializable]
public class SpeedMappingConfig
{
    [Tooltip("最小专注力值")]
    public float minAttention = 0f;
    
    [Tooltip("最大专注力值")]
    public float maxAttention = 100f;
    
    [Tooltip("最小速度")]
    public float minSpeed = 5f;
    
    [Tooltip("最大速度")]
    public float maxSpeed = 30f;
    
    [Tooltip("响应曲线")]
    public AnimationCurve responseCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    [Tooltip("速度平滑系数")]
    public float smoothingSpeed = 5f;
}

/// <summary>
/// 速度映射器
/// 将专注力值映射为游戏速度
/// </summary>
public class SpeedMapper : MonoBehaviour
{
    #region 配置
    [Header("速度映射配置")]
    public SpeedMappingConfig config = new SpeedMappingConfig();
    #endregion

    #region 状态
    [SerializeField]
    private float currentSpeed = 0f;
    
    [SerializeField]
    private float targetSpeed = 0f;
    
    [SerializeField]
    private float normalizedAttention = 0f;
    #endregion

    #region 私有变量
    private BCIManager bciManager;
    #endregion

    #region Unity生命周期
    void Start()
    {
        bciManager = BCIManager.Instance;
        
        if (bciManager != null)
        {
            bciManager.OnAttentionUpdated += OnAttentionUpdated;
        }
        
        // 初始化速度
        currentSpeed = config.minSpeed;
        targetSpeed = config.minSpeed;
    }

    void Update()
    {
        // 平滑过渡速度
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, 
            Time.deltaTime * config.smoothingSpeed);
    }

    void OnDestroy()
    {
        if (bciManager != null)
        {
            bciManager.OnAttentionUpdated -= OnAttentionUpdated;
        }
    }
    #endregion

    #region 事件处理
    /// <summary>
    /// 专注力数据更新回调
    /// </summary>
    private void OnAttentionUpdated(float attention)
    {
        // 归一化专注力值
        normalizedAttention = Mathf.InverseLerp(
            config.minAttention,
            config.maxAttention,
            attention
        );
        
        // 应用响应曲线
        float curvedAttention = config.responseCurve.Evaluate(normalizedAttention);
        
        // 映射到速度
        targetSpeed = Mathf.Lerp(
            config.minSpeed,
            config.maxSpeed,
            curvedAttention
        );
    }
    #endregion

    #region 公共接口
    /// <summary>
    /// 获取当前速度
    /// </summary>
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    /// <summary>
    /// 获取目标速度
    /// </summary>
    public float GetTargetSpeed()
    {
        return targetSpeed;
    }

    /// <summary>
    /// 获取速度比例 (0-1)
    /// </summary>
    public float GetSpeedRatio()
    {
        return (currentSpeed - config.minSpeed) / (config.maxSpeed - config.minSpeed);
    }

    /// <summary>
    /// 获取归一化专注力值
    /// </summary>
    public float GetNormalizedAttention()
    {
        return normalizedAttention;
    }

    /// <summary>
    /// 设置配置
    /// </summary>
    public void SetConfig(SpeedMappingConfig newConfig)
    {
        config = newConfig;
    }
    #endregion
}
