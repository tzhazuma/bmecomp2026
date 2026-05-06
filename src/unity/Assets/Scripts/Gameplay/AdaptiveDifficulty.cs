using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AdaptiveDifficulty : MonoBehaviour
{
    public int adjustmentInterval = 3;
    public int minDifficulty = 1;
    public int maxDifficulty = 5;

    [Header("Difficulty Parameters")]
    public float[] speedMultipliers = { 0.6f, 0.8f, 1.0f, 1.2f, 1.4f };
    public float[] obstacleIntervalMultipliers = { 1.6f, 1.3f, 1.0f, 0.7f, 0.5f };
    public float[] obstacleSpeedMultipliers = { 0.7f, 0.85f, 1.0f, 1.15f, 1.3f };

    [Header("Evaluation Thresholds")]
    public float thresholdUp = 75f;
    public float thresholdDown = 40f;

    private int difficultyLevel = 3;
    private List<float> recentAttentionAverages = new List<float>();
    private int sessionCountAtCurrentDifficulty = 0;
    private BCIManager bciManager;

    void Start()
    {
        bciManager = BCIManager.Instance;
        LoadDifficulty();
    }

    public void OnTrainingSessionEnded(float averageAttention)
    {
        recentAttentionAverages.Add(averageAttention);
        sessionCountAtCurrentDifficulty++;

        if (sessionCountAtCurrentDifficulty >= adjustmentInterval)
        {
            EvaluateDifficulty();
            sessionCountAtCurrentDifficulty = 0;
        }
    }

    private void EvaluateDifficulty()
    {
        if (recentAttentionAverages.Count == 0) return;
        float median = recentAttentionAverages.OrderBy(v => v).ElementAt(recentAttentionAverages.Count / 2);

        if (median > thresholdUp && difficultyLevel < maxDifficulty)
        {
            difficultyLevel++;
            Debug.Log($"自适应难度: 提升至 {difficultyLevel}");
        }
        else if (median < thresholdDown && difficultyLevel > minDifficulty)
        {
            difficultyLevel--;
            Debug.Log($"自适应难度: 降低至 {difficultyLevel}");
        }

        recentAttentionAverages.Clear();
        UpdateGameParameters();
        SaveDifficulty();
    }

    private void UpdateGameParameters()
    {
        int idx = Mathf.Clamp(difficultyLevel - 1, 0, speedMultipliers.Length - 1);

        var obstacleMgr = FindObjectOfType<ObstacleManager>();
        if (obstacleMgr)
            obstacleMgr.SetDifficultyMultiplier(obstacleSpeedMultipliers[idx], obstacleIntervalMultipliers[idx]);

        var collectMgr = FindObjectOfType<CollectibleManager>();
        if (collectMgr)
            collectMgr.SetDifficultyMultiplier(speedMultipliers[idx]);
    }

    public void SetDifficulty(int level)
    {
        difficultyLevel = Mathf.Clamp(level, minDifficulty, maxDifficulty);
        UpdateGameParameters();
        SaveDifficulty();
    }

    public int GetDifficulty() => difficultyLevel;

    public string GetDifficultyName()
    {
        string[] names = { "简单", "较简单", "普通", "较难", "困难" };
        int idx = Mathf.Clamp(difficultyLevel - 1, 0, names.Length - 1);
        return names[idx];
    }

    private void SaveDifficulty()
    {
        PlayerPrefs.SetInt("BCIVR_Difficulty", difficultyLevel);
        PlayerPrefs.Save();
    }

    private void LoadDifficulty()
    {
        difficultyLevel = PlayerPrefs.GetInt("BCIVR_Difficulty", 3);
    }
}
