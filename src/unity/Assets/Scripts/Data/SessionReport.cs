using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class SessionReport : MonoBehaviour
{
    public GameObject reportPanel;
    public TextMeshProUGUI sessionTitleText;
    public TextMeshProUGUI durationText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI avgAttentionText;
    public TextMeshProUGUI maxAttentionText;
    public TextMeshProUGUI focusTimeText;
    public TextMeshProUGUI maxStreakText;
    public TextMeshProUGUI collectiblesText;
    public TextMeshProUGUI obstaclesText;
    public TextMeshProUGUI starsText;
    public TextMeshProUGUI encouragementText;
    public Image[] starImages;

    public Button continueButton;
    public Button restartButton;
    public Button menuButton;

    public Color starActiveColor = Color.yellow;
    public Color starInactiveColor = Color.gray;

    private SessionRecorder sessionRecorder;
    private LevelManager levelManager;

    void Start()
    {
        sessionRecorder = SessionRecorder.Instance;
        levelManager = LevelManager.Instance;

        if (sessionRecorder)
            sessionRecorder.OnSessionCompleted += OnSessionEnd;

        if (continueButton) continueButton.onClick.AddListener(OnContinue);
        if (restartButton) restartButton.onClick.AddListener(OnRestart);
        if (menuButton) menuButton.onClick.AddListener(OnMenu);

        if (reportPanel) reportPanel.SetActive(false);
    }

    private void OnSessionEnd(TrainingSession session)
    {
        ShowReport(session);
    }

    public void ShowReport(TrainingSession session)
    {
        if (reportPanel) reportPanel.SetActive(true);
        if (Time.timeScale < 1f) Time.timeScale = 1f;

        if (sessionTitleText)
            sessionTitleText.text = $"{session.levelName} - 训练报告";

        if (durationText)
        {
            int mins = Mathf.FloorToInt(session.duration / 60);
            int secs = Mathf.FloorToInt(session.duration % 60);
            durationText.text = $"{mins}分{secs}秒";
        }

        if (scoreText) scoreText.text = session.finalScore.ToString();
        if (avgAttentionText) avgAttentionText.text = $"{session.averageAttention:F1}";
        if (maxAttentionText) maxAttentionText.text = $"{session.maxAttention:F1}";
        if (focusTimeText) focusTimeText.text = $"{session.focusTimePercent:F0}%";
        if (maxStreakText) maxStreakText.text = $"{session.maxFocusStreak:F0}s";
        if (collectiblesText) collectiblesText.text = session.totalCollectibles.ToString();
        if (obstaclesText) obstaclesText.text = session.totalObstaclesHit.ToString();

        UpdateStars(session.stars);

        if (encouragementText)
        {
            if (session.stars >= 3)
                encouragementText.text = "太棒了！完美表现！继续保持！";
            else if (session.stars >= 2)
                encouragementText.text = "表现不错！继续加油可以获得三颗星！";
            else if (session.stars >= 1)
                encouragementText.text = "好的开始！试着提高专注力获得更多星星！";
            else
                encouragementText.text = "专注力还可以提升，注意调整头环和呼吸，再试一次！";
        }

        var adaptiveDiff = FindObjectOfType<AdaptiveDifficulty>();
        if (adaptiveDiff)
            adaptiveDiff.OnTrainingSessionEnded(session.averageAttention);
    }

    private void UpdateStars(int starCount)
    {
        if (starImages == null) return;
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i])
                starImages[i].color = i < starCount ? starActiveColor : starInactiveColor;
        }
        if (starsText) starsText.text = $"{starCount}/{starImages.Length}";
    }

    private void OnContinue()
    {
        if (levelManager)
        {
            levelManager.NextLevel();
            if (GameManager.Instance)
                GameManager.Instance.StartGame();
        }
        if (reportPanel) reportPanel.SetActive(false);
    }

    private void OnRestart()
    {
        if (levelManager)
            levelManager.RestartLevel();
        if (GameManager.Instance)
            GameManager.Instance.StartGame();
        if (reportPanel) reportPanel.SetActive(false);
    }

    private void OnMenu()
    {
        if (GameManager.Instance)
            GameManager.Instance.ReturnToMenu();
        if (reportPanel) reportPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (sessionRecorder)
            sessionRecorder.OnSessionCompleted -= OnSessionEnd;
    }
}
