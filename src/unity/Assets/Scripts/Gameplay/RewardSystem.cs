/**
 * 奖励系统
 * 负责奖励机制、连击系统、成就系统
 */

using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// 奖励事件
/// </summary>
[System.Serializable]
public class RewardEvents
{
    [Tooltip("连击激活事件")]
    public UnityEvent OnComboActivated;
    
    [Tooltip("连击结束事件")]
    public UnityEvent OnComboDeactivated;
    
    [Tooltip("成就解锁事件")]
    public UnityEvent<string> OnAchievementUnlocked;
    
    [Tooltip("分数增加事件")]
    public UnityEvent<int> OnScoreIncreased;
    
    [Tooltip("等级提升事件")]
    public UnityEvent<int> OnLevelUp;
}

/// <summary>
/// 成就定义
/// </summary>
[System.Serializable]
public class Achievement
{
    public string id;
    public string name;
    public string description;
    public bool unlocked;
    public int requirement;
}

/// <summary>
/// 奖励系统
/// 管理游戏中的奖励机制
/// </summary>
public class RewardSystem : MonoBehaviour
{
    #region 配置
    [Header("奖励设置")]
    [Tooltip("专注力阈值")]
    public float focusThreshold = 70f;
    
    [Tooltip("连击阈值（秒）")]
    public float comboThreshold = 10f;
    
    [Tooltip("连击奖励分数")]
    public int comboBonus = 20;
    
    [Tooltip("收集物分数")]
    public int collectibleScore = 10;
    
    [Tooltip("连击倍率")]
    public float comboMultiplier = 1.5f;
    
    [Header("事件")]
    public RewardEvents rewardEvents;
    
    [Header("成就列表")]
    public List<Achievement> achievements = new List<Achievement>();
    #endregion

    #region 状态
    [SerializeField]
    private float focusStreak = 0f;
    
    [SerializeField]
    private bool isComboActive = false;
    
    [SerializeField]
    private int score = 0;
    
    [SerializeField]
    private int comboCount = 0;
    
    [SerializeField]
    private int level = 1;
    
    [SerializeField]
    private int experience = 0;
    
    [SerializeField]
    private int experienceToNextLevel = 100;
    #endregion

    #region 私有变量
    private BCIManager bciManager;
    private GameManager gameManager;
    #endregion

    #region Unity生命周期
    void Start()
    {
        bciManager = BCIManager.Instance;
        gameManager = GameManager.Instance;
        
        if (bciManager != null)
        {
            bciManager.OnAttentionUpdated += OnAttentionUpdated;
        }
        
        // 初始化成就
        InitializeAchievements();
    }

    void Update()
    {
        UpdateCombo();
    }

    void OnDestroy()
    {
        if (bciManager != null)
        {
            bciManager.OnAttentionUpdated -= OnAttentionUpdated;
        }
    }
    #endregion

    #region 初始化
    /// <summary>
    /// 初始化成就系统
    /// </summary>
    private void InitializeAchievements()
    {
        // 如果没有预设成就，创建默认成就
        if (achievements.Count == 0)
        {
            achievements.Add(new Achievement
            {
                id = "first_focus",
                name = "初次专注",
                description = "首次达到专注状态",
                unlocked = false,
                requirement = 1
            });
            
            achievements.Add(new Achievement
            {
                id = "combo_master",
                name = "连击大师",
                description = "达成10次连击",
                unlocked = false,
                requirement = 10
            });
            
            achievements.Add(new Achievement
            {
                id = "star_collector",
                name = "星光收集者",
                description = "收集100颗星光",
                unlocked = false,
                requirement = 100
            });
            
            achievements.Add(new Achievement
            {
                id = "focus_master",
                name = "专注大师",
                description = "连续专注超过30秒",
                unlocked = false,
                requirement = 30
            });
            
            achievements.Add(new Achievement
            {
                id = "speed_demon",
                name = "速度恶魔",
                description = "达到最大速度",
                unlocked = false,
                requirement = 1
            });
        }
    }
    #endregion

    #region 奖励逻辑
    /// <summary>
    /// 专注力数据更新回调
    /// </summary>
    private void OnAttentionUpdated(float attention)
    {
        if (attention >= focusThreshold)
        {
            focusStreak += Time.deltaTime;
            
            // 检查连击激活
            if (focusStreak >= comboThreshold && !isComboActive)
            {
                ActivateCombo();
            }
            
            // 检查专注成就
            CheckFocusAchievements();
        }
        else
        {
            if (isComboActive)
            {
                DeactivateCombo();
            }
            focusStreak = 0f;
        }
    }

    /// <summary>
    /// 更新连击状态
    /// </summary>
    private void UpdateCombo()
    {
        // 连击状态已经在OnAttentionUpdated中处理
        // 这里可以添加额外的连击效果
    }

    /// <summary>
    /// 激活连击
    /// </summary>
    private void ActivateCombo()
    {
        isComboActive = true;
        comboCount++;
        
        rewardEvents.OnComboActivated?.Invoke();
        
        // 检查连击成就
        CheckComboAchievements();
        
        Debug.Log($"专注连击模式激活！连击次数: {comboCount}");
    }

    /// <summary>
    /// 停用连击
    /// </summary>
    private void DeactivateCombo()
    {
        isComboActive = false;
        rewardEvents.OnComboDeactivated?.Invoke();
        
        Debug.Log("专注连击模式结束");
    }

    /// <summary>
    /// 收集物品
    /// </summary>
    public void OnCollectiblePickedUp()
    {
        int baseScore = collectibleScore;
        int bonus = 0;
        
        // 连击奖励
        if (isComboActive)
        {
            bonus = Mathf.RoundToInt(baseScore * (comboMultiplier - 1f));
        }
        
        int totalScore = baseScore + bonus;
        AddScore(totalScore);
        AddExperience(totalScore);
        
        // 检查收集成就
        CheckCollectAchievements();
        
        Debug.Log($"收集物品！分数 +{totalScore} (基础: {baseScore}, 连击奖励: {bonus})");
    }

    /// <summary>
    /// 碰到障碍物
    /// </summary>
    public void OnHitObstacle()
    {
        int penalty = 10;
        AddScore(-penalty);
        
        Debug.Log($"碰到障碍物！分数 -{penalty}");
    }

    /// <summary>
    /// 添加分数
    /// </summary>
    public void AddScore(int amount)
    {
        score += amount;
        score = Mathf.Max(0, score);
        
        rewardEvents.OnScoreIncreased?.Invoke(score);
        
        // 通知游戏管理器
        if (gameManager != null)
        {
            // gameManager.UpdateScore(score);
        }
    }

    /// <summary>
    /// 添加经验值
    /// </summary>
    public void AddExperience(int amount)
    {
        experience += amount;
        
        // 检查升级
        while (experience >= experienceToNextLevel)
        {
            experience -= experienceToNextLevel;
            LevelUp();
        }
    }

    /// <summary>
    /// 升级
    /// </summary>
    private void LevelUp()
    {
        level++;
        experienceToNextLevel = Mathf.RoundToInt(experienceToNextLevel * 1.5f);
        
        rewardEvents.OnLevelUp?.Invoke(level);
        
        Debug.Log($"升级！当前等级: {level}");
    }
    #endregion

    #region 成就系统
    /// <summary>
    /// 检查专注成就
    /// </summary>
    private void CheckFocusAchievements()
    {
        // 初次专注
        UnlockAchievement("first_focus");
        
        // 专注大师
        if (focusStreak >= 30f)
        {
            UnlockAchievement("focus_master");
        }
    }

    /// <summary>
    /// 检查连击成就
    /// </summary>
    private void CheckComboAchievements()
    {
        if (comboCount >= 10)
        {
            UnlockAchievement("combo_master");
        }
    }

    /// <summary>
    /// 检查收集成就
    /// </summary>
    private void CheckCollectAchievements()
    {
        // 这里需要跟踪收集物数量
        // 暂时跳过
    }

    /// <summary>
    /// 解锁成就
    /// </summary>
    public void UnlockAchievement(string achievementId)
    {
        Achievement achievement = achievements.Find(a => a.id == achievementId);
        
        if (achievement != null && !achievement.unlocked)
        {
            achievement.unlocked = true;
            rewardEvents.OnAchievementUnlocked?.Invoke(achievement.name);
            
            Debug.Log($"成就解锁: {achievement.name} - {achievement.description}");
        }
    }
    #endregion

    #region 公共接口
    /// <summary>
    /// 获取当前分数
    /// </summary>
    public int GetScore()
    {
        return score;
    }

    /// <summary>
    /// 获取连击状态
    /// </summary>
    public bool IsComboActive()
    {
        return isComboActive;
    }

    /// <summary>
    /// 获取连击次数
    /// </summary>
    public int GetComboCount()
    {
        return comboCount;
    }

    /// <summary>
    /// 获取连续专注时间
    /// </summary>
    public float GetFocusStreak()
    {
        return focusStreak;
    }

    /// <summary>
    /// 获取当前等级
    /// </summary>
    public int GetLevel()
    {
        return level;
    }

    /// <summary>
    /// 获取经验值
    /// </summary>
    public int GetExperience()
    {
        return experience;
    }

    /// <summary>
    /// 获取下一级所需经验值
    /// </summary>
    public int GetExperienceToNextLevel()
    {
        return experienceToNextLevel;
    }

    /// <summary>
    /// 获取经验比例 (0-1)
    /// </summary>
    public float GetExperienceRatio()
    {
        return (float)experience / experienceToNextLevel;
    }

    /// <summary>
    /// 获取成就列表
    /// </summary>
    public List<Achievement> GetAchievements()
    {
        return achievements;
    }

    /// <summary>
    /// 获取已解锁成就数量
    /// </summary>
    public int GetUnlockedAchievementCount()
    {
        return achievements.FindAll(a => a.unlocked).Count;
    }

    /// <summary>
    /// 重置奖励系统
    /// </summary>
    public void Reset()
    {
        score = 0;
        comboCount = 0;
        focusStreak = 0f;
        isComboActive = false;
        level = 1;
        experience = 0;
        experienceToNextLevel = 100;
        
        // 重置成就
        foreach (var achievement in achievements)
        {
            achievement.unlocked = false;
        }
    }
    #endregion
}
