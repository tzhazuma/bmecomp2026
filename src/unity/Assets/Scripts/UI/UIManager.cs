using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public Slider attentionBar;
    public TextMeshProUGUI attentionValueText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI comboText;
    public Image comboProgressFill;
    public TextMeshProUGUI connectionStatus;
    public TextMeshProUGUI debugInfo;

    public GameObject pausePanel;
    public GameObject gameOverPanel;

    public Gradient attentionGradient;

    private BCIManager bciManager;
    private GameManager gameManager;
    private LevelManager levelManager;

    void Start()
    {
        bciManager = BCIManager.Instance;
        gameManager = GameManager.Instance;
        levelManager = LevelManager.Instance;

        if (bciManager)
        {
            bciManager.OnAttentionUpdated += OnAttentionUpdate;
            bciManager.OnConnectionChanged += OnConnectionChange;
        }
        if (gameManager)
        {
            gameManager.OnScoreChanged += OnScoreChange;
            gameManager.OnTimerUpdated += OnTimerUpdate;
            gameManager.OnGameStateChanged += OnGameStateChange;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            if (gameManager && gameManager.GetCurrentState() == GameState.Playing)
                gameManager.PauseGame();
            else if (gameManager && gameManager.GetCurrentState() == GameState.Paused)
                gameManager.ResumeGame();
        }

        UpdateDebugInfo();
    }

    private void OnAttentionUpdate(float attention)
    {
        float ratio = Mathf.Clamp01(attention / 100f);
        if (attentionBar)
        {
            attentionBar.value = ratio;
            attentionBar.fillRect.GetComponent<Image>().color = attentionGradient.Evaluate(ratio);
        }
        if (attentionValueText)
        {
            attentionValueText.text = $"{attention:F0}";
            attentionValueText.color = attentionGradient.Evaluate(ratio);
        }
    }

    private void OnConnectionChange(bool connected)
    {
        if (connectionStatus)
        {
            connectionStatus.text = connected ? "BCI: 已连接" : "BCI: 未连接";
            connectionStatus.color = connected ? Color.green : Color.red;
        }
    }

    private void OnScoreChange(int score)
    {
        if (scoreText) scoreText.text = $"得分: {score}";
    }

    private void OnTimerUpdate(float time)
    {
        if (timerText)
        {
            int remaining = Mathf.Max(0, Mathf.RoundToInt(
                (gameManager ? gameManager.sessionDuration : 300f) - time));
            timerText.text = $"{remaining / 60}:{remaining % 60:D2}";
        }
    }

    private void OnGameStateChange(GameState state)
    {
        if (pausePanel) pausePanel.SetActive(state == GameState.Paused);
        if (gameOverPanel) gameOverPanel.SetActive(state == GameState.GameOver);
    }

    public void UpdateComboDisplay(float progress, bool active, float streak)
    {
        if (comboProgressFill) comboProgressFill.fillAmount = progress;
        if (comboText)
        {
            if (active)
            {
                comboText.text = $"专注连击! {streak:F0}s";
                comboText.color = Color.yellow;
            }
            else if (progress > 0)
            {
                comboText.text = $"保持专注... {(progress * 10f):F0}/10s";
                comboText.color = Color.Lerp(Color.white, Color.yellow, progress);
            }
            else
            {
                comboText.text = "";
            }
        }
    }

    private void UpdateDebugInfo()
    {
        if (!debugInfo || !debugInfo.gameObject.activeInHierarchy) return;
        string info = "";
        if (bciManager)
        {
            var data = bciManager.GetCurrentData();
            info += $"注意力: {data.attention:F1}\n";
            info += $"Yaw: {data.yaw:F1}  Pitch: {data.pitch:F1}\n";
            info += $"质量: {data.signalQuality:F2}\n";
        }
        if (levelManager)
            info += $"关卡: {levelManager.GetCurrentLevel()}/{levelManager.GetMaxLevel()}\n";
        if (gameManager)
            info += $"已收集: {gameManager.GetTotalCollectibles()}  碰撞: {gameManager.GetTotalObstaclesHit()}";
        debugInfo.text = info;
    }

    void OnDestroy()
    {
        if (bciManager)
        {
            bciManager.OnAttentionUpdated -= OnAttentionUpdate;
            bciManager.OnConnectionChanged -= OnConnectionChange;
        }
        if (gameManager)
        {
            gameManager.OnScoreChanged -= OnScoreChange;
            gameManager.OnTimerUpdated -= OnTimerUpdate;
            gameManager.OnGameStateChanged -= OnGameStateChange;
        }
    }
}
