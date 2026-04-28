/**
 * 玩家控制器
 * 负责飞船移动、碰撞检测、视觉效果
 */

using UnityEngine;

/// <summary>
/// 移动配置
/// </summary>
[System.Serializable]
public class MovementConfig
{
    [Header("速度设置")]
    [Tooltip("最小飞行速度")]
    public float minSpeed = 5f;
    
    [Tooltip("最大飞行速度")]
    public float maxSpeed = 30f;
    
    [Tooltip("速度平滑系数")]
    public float speedSmoothing = 5f;
    
    [Header("方向控制")]
    [Tooltip("偏航灵敏度")]
    public float yawSensitivity = 2f;
    
    [Tooltip("俯仰灵敏度")]
    public float pitchSensitivity = 1.5f;
    
    [Tooltip("方向平滑系数")]
    public float directionSmoothing = 0.1f;
    
    [Tooltip("死区阈值")]
    public float deadzone = 5f;
    
    [Header("边界限制")]
    [Tooltip("水平边界")]
    public float horizontalLimit = 20f;
    
    [Tooltip("垂直边界")]
    public float verticalLimit = 10f;
}

/// <summary>
/// 玩家控制器
/// 控制飞船的移动和交互
/// </summary>
public class PlayerController : MonoBehaviour
{
    #region 配置
    [Header("移动配置")]
    public MovementConfig movementConfig = new MovementConfig();
    
    [Header("组件引用")]
    [Tooltip("飞船模型")]
    public Transform shipModel;
    
    [Tooltip("粒子特效")]
    public ParticleSystem speedParticles;
    
    [Tooltip("专注特效")]
    public ParticleSystem focusParticles;
    #endregion

    #region 状态
    [SerializeField]
    private float currentSpeed = 0f;
    
    [SerializeField]
    private float targetSpeed = 0f;
    
    [SerializeField]
    private float currentYaw = 0f;
    
    [SerializeField]
    private float currentPitch = 0f;
    
    [SerializeField]
    private float targetYaw = 0f;
    
    [SerializeField]
    private float targetPitch = 0f;
    
    [SerializeField]
    private bool isFocused = false;
    #endregion

    #region 私有变量
    private Vector3 startPosition;
    private BCIManager bciManager;
    private GameManager gameManager;
    private Rigidbody rb;
    #endregion

    #region Unity生命周期
    void Start()
    {
        startPosition = transform.position;
        bciManager = BCIManager.Instance;
        gameManager = GameManager.Instance;
        rb = GetComponent<Rigidbody>();

        // 注册BCI事件
        if (bciManager != null)
        {
            bciManager.OnAttentionUpdated += OnAttentionUpdated;
            bciManager.OnIMUUpdated += OnIMUUpdated;
        }

        // 初始化速度
        currentSpeed = movementConfig.minSpeed;
        targetSpeed = movementConfig.minSpeed;
    }

    void Update()
    {
        if (gameManager != null && gameManager.GetCurrentState() != GameState.Playing)
            return;

        UpdateSpeed();
        UpdateDirection();
        UpdatePosition();
        UpdateVisuals();
    }

    void OnDestroy()
    {
        // 取消事件注册
        if (bciManager != null)
        {
            bciManager.OnAttentionUpdated -= OnAttentionUpdated;
            bciManager.OnIMUUpdated -= OnIMUUpdated;
        }
    }
    #endregion

    #region 移动控制
    /// <summary>
    /// 更新速度
    /// </summary>
    private void UpdateSpeed()
    {
        // 平滑过渡速度
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, 
            Time.deltaTime * movementConfig.speedSmoothing);

        // 向前移动
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 更新方向
    /// </summary>
    private void UpdateDirection()
    {
        // 平滑过渡方向
        currentYaw = Mathf.Lerp(currentYaw, targetYaw, 
            movementConfig.directionSmoothing);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, 
            movementConfig.directionSmoothing);

        // 应用旋转
        if (shipModel != null)
        {
            shipModel.localRotation = Quaternion.Euler(
                -currentPitch * 0.5f, 
                currentYaw * 0.3f, 
                -currentYaw * 0.2f
            );
        }
    }

    /// <summary>
    /// 更新位置
    /// </summary>
    private void UpdatePosition()
    {
        // 计算水平和垂直移动
        float horizontalMove = currentYaw * movementConfig.yawSensitivity * Time.deltaTime;
        float verticalMove = currentPitch * movementConfig.pitchSensitivity * Time.deltaTime;

        // 计算新位置
        Vector3 newPosition = transform.position + new Vector3(horizontalMove, verticalMove, 0);

        // 限制边界
        newPosition.x = Mathf.Clamp(newPosition.x, 
            -movementConfig.horizontalLimit, 
            movementConfig.horizontalLimit);
        newPosition.y = Mathf.Clamp(newPosition.y, 
            -movementConfig.verticalLimit, 
            movementConfig.verticalLimit);

        // 应用位置
        if (rb != null)
        {
            rb.MovePosition(newPosition);
        }
        else
        {
            transform.position = newPosition;
        }
    }
    #endregion

    #region 事件处理
    /// <summary>
    /// 专注力数据更新回调
    /// </summary>
    private void OnAttentionUpdated(float attention)
    {
        // 将专注力映射为速度
        // attention范围: 0-100
        // speed范围: minSpeed-maxSpeed
        float normalizedAttention = attention / 100f;
        targetSpeed = Mathf.Lerp(movementConfig.minSpeed, movementConfig.maxSpeed, 
            normalizedAttention);

        // 更新专注状态
        isFocused = attention >= 70f;
    }

    /// <summary>
    /// IMU数据更新回调
    /// </summary>
    private void OnIMUUpdated(float yaw, float pitch, float roll)
    {
        // 死区处理
        if (Mathf.Abs(yaw) < movementConfig.deadzone) yaw = 0f;
        if (Mathf.Abs(pitch) < movementConfig.deadzone) pitch = 0f;

        // 更新目标方向
        targetYaw = yaw;
        targetPitch = pitch;
    }
    #endregion

    #region 碰撞检测
    void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        HandleTrigger(other.gameObject);
    }

    /// <summary>
    /// 处理碰撞
    /// </summary>
    private void HandleCollision(GameObject other)
    {
        if (other.CompareTag("Obstacle"))
        {
            // 碰到障碍物
            if (gameManager != null)
            {
                gameManager.OnPlayerHitObstacle();
            }

            // 播放碰撞特效
            PlayHitEffect();

            Debug.Log("碰到障碍物");
        }
    }

    /// <summary>
    /// 处理触发器
    /// </summary>
    private void HandleTrigger(GameObject other)
    {
        if (other.CompareTag("Collectible"))
        {
            // 收集物品
            if (gameManager != null)
            {
                gameManager.OnCollectiblePickedUp();
            }

            // 播放收集特效
            PlayCollectEffect(other.transform.position);

            // 销毁收集物
            Destroy(other.gameObject);

            Debug.Log("收集物品");
        }
    }
    #endregion

    #region 视觉效果
    /// <summary>
    /// 更新视觉效果
    /// </summary>
    private void UpdateVisuals()
    {
        // 更新速度粒子效果
        if (speedParticles != null)
        {
            var emission = speedParticles.emission;
            float speedRatio = (currentSpeed - movementConfig.minSpeed) / 
                (movementConfig.maxSpeed - movementConfig.minSpeed);
            emission.rateOverTime = Mathf.Lerp(10f, 100f, speedRatio);
        }

        // 更新专注特效
        if (focusParticles != null)
        {
            if (isFocused && !focusParticles.isPlaying)
            {
                focusParticles.Play();
            }
            else if (!isFocused && focusParticles.isPlaying)
            {
                focusParticles.Stop();
            }
        }
    }

    /// <summary>
    /// 播放碰撞特效
    /// </summary>
    private void PlayHitEffect()
    {
        // TODO: 实现碰撞特效
        // 可以添加屏幕震动、闪烁等效果
    }

    /// <summary>
    /// 播放收集特效
    /// </summary>
    private void PlayCollectEffect(Vector3 position)
    {
        // TODO: 实现收集特效
        // 可以添加粒子爆发、光效等
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
    /// 获取速度比例 (0-1)
    /// </summary>
    public float GetSpeedRatio()
    {
        return (currentSpeed - movementConfig.minSpeed) / 
            (movementConfig.maxSpeed - movementConfig.minSpeed);
    }

    /// <summary>
    /// 是否处于专注状态
    /// </summary>
    public bool IsFocused()
    {
        return isFocused;
    }

    /// <summary>
    /// 重置位置
    /// </summary>
    public void ResetPosition()
    {
        transform.position = startPosition;
        currentSpeed = movementConfig.minSpeed;
        targetSpeed = movementConfig.minSpeed;
        currentYaw = 0f;
        currentPitch = 0f;
        targetYaw = 0f;
        targetPitch = 0f;
    }
    #endregion
}
