/**
 * 星空背景
 * 负责生成和管理星空背景效果
 */

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 星星配置
/// </summary>
[System.Serializable]
public class StarConfig
{
    [Tooltip("星星数量")]
    public int starCount = 200;
    
    [Tooltip("星星大小范围")]
    public Vector2 starSizeRange = new Vector2(0.1f, 0.5f);
    
    [Tooltip("星星颜色")]
    public Color starColor = Color.white;
    
    [Tooltip("闪烁速度")]
    public float twinkleSpeed = 2f;
    
    [Tooltip("闪烁强度")]
    public float twinkleIntensity = 0.3f;
}

/// <summary>
/// 星空背景
/// 生成动态星空背景效果
/// </summary>
public class StarField : MonoBehaviour
{
    #region 配置
    [Header("星空配置")]
    public StarConfig config = new StarConfig();
    
    [Header("生成范围")]
    [Tooltip("生成半径")]
    public float spawnRadius = 50f;
    
    [Tooltip("生成深度")]
    public float spawnDepth = 100f;
    
    [Header("移动设置")]
    [Tooltip("是否移动")]
    public bool isMoving = true;
    
    [Tooltip("移动速度")]
    public float moveSpeed = 10f;
    
    [Tooltip("速度倍率")]
    public float speedMultiplier = 1f;
    #endregion

    #region 私有变量
    private List<GameObject> stars = new List<GameObject>();
    private Material starMaterial;
    private PlayerController playerController;
    #endregion

    #region Unity生命周期
    void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        
        // 创建星星材质
        CreateStarMaterial();
        
        // 生成星空
        GenerateStarField();
    }

    void Update()
    {
        if (isMoving)
        {
            MoveStarField();
        }
        
        UpdateStarTwinkle();
    }
    #endregion

    #region 星空生成
    /// <summary>
    /// 创建星星材质
    /// </summary>
    private void CreateStarMaterial()
    {
        starMaterial = new Material(Shader.Find("Sprites/Default"));
        starMaterial.color = config.starColor;
    }

    /// <summary>
    /// 生成星空
    /// </summary>
    private void GenerateStarField()
    {
        for (int i = 0; i < config.starCount; i++)
        {
            CreateStar();
        }
    }

    /// <summary>
    /// 创建单个星星
    /// </summary>
    private void CreateStar()
    {
        // 随机位置
        Vector3 position = new Vector3(
            Random.Range(-spawnRadius, spawnRadius),
            Random.Range(-spawnRadius, spawnRadius),
            Random.Range(0, spawnDepth)
        );
        
        // 创建星星对象
        GameObject star = GameObject.CreatePrimitive(PrimitiveType.Quad);
        star.name = "Star";
        star.transform.SetParent(transform);
        star.transform.localPosition = position;
        star.transform.localScale = Vector3.one * Random.Range(config.starSizeRange.x, config.starSizeRange.y);
        
        // 随机朝向
        star.transform.rotation = Random.rotation;
        
        // 设置材质
        Renderer renderer = star.GetComponent<Renderer>();
        renderer.material = starMaterial;
        
        // 移除碰撞器
        Collider collider = star.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
        
        // 添加闪烁组件
        StarTwinkle twinkle = star.AddComponent<StarTwinkle>();
        twinkle.speed = config.twinkleSpeed * Random.Range(0.8f, 1.2f);
        twinkle.intensity = config.twinkleIntensity;
        
        stars.Add(star);
    }
    #endregion

    #region 星空移动
    /// <summary>
    /// 移动星空
    /// </summary>
    private void MoveStarField()
    {
        float speed = moveSpeed * speedMultiplier;
        
        // 如果有玩家控制器，使用玩家速度
        if (playerController != null)
        {
            speed = playerController.GetCurrentSpeed();
        }
        
        // 移动所有星星
        foreach (GameObject star in stars)
        {
            star.transform.Translate(Vector3.back * speed * Time.deltaTime);
            
            // 如果星星超出范围，重新定位
            if (star.transform.position.z < -10)
            {
                RepositionStar(star);
            }
        }
    }

    /// <summary>
    /// 重新定位星星
    /// </summary>
    private void RepositionStar(GameObject star)
    {
        Vector3 newPosition = new Vector3(
            Random.Range(-spawnRadius, spawnRadius),
            Random.Range(-spawnRadius, spawnRadius),
            spawnDepth
        );
        
        star.transform.localPosition = newPosition;
    }
    #endregion

    #region 视觉效果
    /// <summary>
    /// 更新星星闪烁
    /// </summary>
    private void UpdateStarTwinkle()
    {
        // 闪烁效果在StarTwinkle组件中处理
    }
    #endregion

    #region 公共接口
    /// <summary>
    /// 设置移动速度
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    /// <summary>
    /// 设置速度倍率
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }

    /// <summary>
    /// 是否移动
    /// </summary>
    public void SetMoving(bool moving)
    {
        isMoving = moving;
    }
    #endregion
}

/// <summary>
/// 星星闪烁效果
/// </summary>
public class StarTwinkle : MonoBehaviour
{
    [HideInInspector]
    public float speed = 2f;
    
    [HideInInspector]
    public float intensity = 0.3f;
    
    private Renderer renderer;
    private Color originalColor;
    private float offset;
    
    void Start()
    {
        renderer = GetComponent<Renderer>();
        originalColor = renderer.material.color;
        offset = Random.Range(0f, Mathf.PI * 2f);
    }
    
    void Update()
    {
        float alpha = 1f - intensity + Mathf.Sin(Time.time * speed + offset) * intensity;
        alpha = Mathf.Clamp01(alpha);
        
        Color color = originalColor;
        color.a = alpha;
        renderer.material.color = color;
    }
}
