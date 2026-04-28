/**
 * BCI管理器
 * 负责与Python BCI服务器的通信，管理BCI数据
 */

using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;

/// <summary>
/// BCI数据结构
/// </summary>
[System.Serializable]
public class BCIData
{
    public float attention;      // 专注力值 (0-100)
    public float yaw;           // 偏航角
    public float pitch;         // 俯仰角
    public float roll;          // 横滚角
    public float battery;       // 电池电量
    public float signalQuality; // 信号质量
    public float alphaPower;    // Alpha波功率
    public float betaPower;     // Beta波功率
    public long timestamp;      // 时间戳
}

/// <summary>
/// BCI管理器
/// 单例模式，负责BCI数据的接收和管理
/// </summary>
public class BCIManager : MonoBehaviour
{
    #region 单例
    public static BCIManager Instance { get; private set; }
    #endregion

    #region 配置
    [Header("连接设置")]
    [Tooltip("BCI服务器地址")]
    public string serverHost = "127.0.0.1";
    
    [Tooltip("BCI服务器端口")]
    public int serverPort = 5555;
    
    [Tooltip("自动重连")]
    public bool autoReconnect = true;
    
    [Tooltip("重连间隔（秒）")]
    public float reconnectInterval = 5f;
    #endregion

    #region 数据
    [Header("BCI数据")]
    [SerializeField]
    private BCIData currentData = new BCIData();
    
    [SerializeField]
    private bool isConnected = false;
    
    [SerializeField]
    private float receiveRate = 0f;
    #endregion

    #region 事件
    /// <summary>
    /// 专注力数据更新事件
    /// </summary>
    public event Action<float> OnAttentionUpdated;
    
    /// <summary>
    /// IMU数据更新事件
    /// </summary>
    public event Action<float, float, float> OnIMUUpdated;
    
    /// <summary>
    /// 连接状态变化事件
    /// </summary>
    public event Action<bool> OnConnectionChanged;
    
    /// <summary>
    /// BCI数据更新事件
    /// </summary>
    public event Action<BCIData> OnDataUpdated;
    #endregion

    #region 私有变量
    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool shouldStop = false;
    private object lockObject = new object();
    
    // 统计
    private int packetCount = 0;
    private float lastRateTime = 0f;
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
    }

    void Start()
    {
        ConnectToServer();
    }

    void Update()
    {
        // 计算接收速率
        if (Time.time - lastRateTime >= 1f)
        {
            receiveRate = packetCount;
            packetCount = 0;
            lastRateTime = Time.time;
        }
    }

    void OnDestroy()
    {
        shouldStop = true;
        Disconnect();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Disconnect();
        }
        else if (autoReconnect)
        {
            ConnectToServer();
        }
    }
    #endregion

    #region 连接管理
    /// <summary>
    /// 连接到BCI服务器
    /// </summary>
    public void ConnectToServer()
    {
        if (isConnected) return;

        try
        {
            client = new TcpClient();
            client.Connect(serverHost, serverPort);
            stream = client.GetStream();
            isConnected = true;

            // 启动接收线程
            shouldStop = false;
            receiveThread = new Thread(ReceiveDataLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            OnConnectionChanged?.Invoke(true);
            Debug.Log($"已连接到BCI服务器 {serverHost}:{serverPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"连接BCI服务器失败: {e.Message}");
            isConnected = false;

            // 自动重连
            if (autoReconnect)
            {
                Invoke(nameof(ConnectToServer), reconnectInterval);
            }
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public void Disconnect()
    {
        shouldStop = true;
        isConnected = false;

        if (stream != null)
        {
            try { stream.Close(); } catch { }
            stream = null;
        }

        if (client != null)
        {
            try { client.Close(); } catch { }
            client = null;
        }

        if (receiveThread != null)
        {
            receiveThread.Join(1000);
            receiveThread = null;
        }

        OnConnectionChanged?.Invoke(false);
        Debug.Log("已断开BCI服务器连接");
    }
    #endregion

    #region 数据接收
    /// <summary>
    /// 数据接收循环
    /// </summary>
    private void ReceiveDataLoop()
    {
        byte[] buffer = new byte[4096];
        StringBuilder messageBuilder = new StringBuilder();

        while (!shouldStop && isConnected)
        {
            try
            {
                if (stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        messageBuilder.Append(data);

                        // 处理完整的消息
                        string messages = messageBuilder.ToString();
                        string[] lines = messages.Split('\n');

                        for (int i = 0; i < lines.Length - 1; i++)
                        {
                            if (!string.IsNullOrEmpty(lines[i]))
                            {
                                ProcessMessage(lines[i]);
                            }
                        }

                        // 保留最后一行（可能不完整）
                        messageBuilder.Clear();
                        if (!string.IsNullOrEmpty(lines[lines.Length - 1]))
                        {
                            messageBuilder.Append(lines[lines.Length - 1]);
                        }
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
            catch (Exception e)
            {
                if (!shouldStop)
                {
                    Debug.LogError($"接收数据错误: {e.Message}");
                }
                break;
            }
        }

        // 连接断开
        if (!shouldStop)
        {
            isConnected = false;
            OnConnectionChanged?.Invoke(false);

            // 自动重连
            if (autoReconnect)
            {
                UnityMainThread.Execute(() =>
                {
                    Invoke(nameof(ConnectToServer), reconnectInterval);
                });
            }
        }
    }

    /// <summary>
    /// 处理接收到的消息
    /// </summary>
    private void ProcessMessage(string message)
    {
        try
        {
            JObject json = JObject.Parse(message);
            string type = json["type"]?.ToString();

            if (type == "attention")
            {
                ProcessAttentionData(json);
            }
            else if (type == "imu")
            {
                ProcessIMUData(json);
            }
            else if (type == "status")
            {
                ProcessStatusData(json);
            }
            else if (type == "heartbeat")
            {
                // 心跳消息，忽略
            }

            packetCount++;
        }
        catch (Exception e)
        {
            Debug.LogError($"解析消息错误: {e.Message}");
        }
    }

    /// <summary>
    /// 处理专注力数据
    /// </summary>
    private void ProcessAttentionData(JObject json)
    {
        try
        {
            JObject data = json["data"]?.ToObject<JObject>();
            if (data != null)
            {
                lock (lockObject)
                {
                    currentData.attention = data["attention"]?.Value<float>() ?? 0f;
                    currentData.alphaPower = data["alpha_power"]?.Value<float>() ?? 0f;
                    currentData.betaPower = data["beta_power"]?.Value<float>() ?? 0f;
                    currentData.signalQuality = data["quality"]?.Value<float>() ?? 1f;
                    currentData.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }

                // 在主线程触发事件
                UnityMainThread.Execute(() =>
                {
                    OnAttentionUpdated?.Invoke(currentData.attention);
                    OnDataUpdated?.Invoke(currentData);
                });
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"处理专注力数据错误: {e.Message}");
        }
    }

    /// <summary>
    /// 处理IMU数据
    /// </summary>
    private void ProcessIMUData(JObject json)
    {
        try
        {
            JObject data = json["data"]?.ToObject<JObject>();
            if (data != null)
            {
                lock (lockObject)
                {
                    currentData.yaw = data["yaw"]?.Value<float>() ?? 0f;
                    currentData.pitch = data["pitch"]?.Value<float>() ?? 0f;
                    currentData.roll = data["roll"]?.Value<float>() ?? 0f;
                }

                UnityMainThread.Execute(() =>
                {
                    OnIMUUpdated?.Invoke(currentData.yaw, currentData.pitch, currentData.roll);
                });
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"处理IMU数据错误: {e.Message}");
        }
    }

    /// <summary>
    /// 处理状态数据
    /// </summary>
    private void ProcessStatusData(JObject json)
    {
        try
        {
            JObject data = json["data"]?.ToObject<JObject>();
            if (data != null)
            {
                lock (lockObject)
                {
                    currentData.battery = data["battery"]?.Value<float>() ?? 100f;
                    currentData.signalQuality = data["signal_quality"]?.Value<float>() ?? 1f;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"处理状态数据错误: {e.Message}");
        }
    }
    #endregion

    #region 公共接口
    /// <summary>
    /// 获取当前专注力值
    /// </summary>
    public float GetAttention()
    {
        lock (lockObject)
        {
            return currentData.attention;
        }
    }

    /// <summary>
    /// 获取当前IMU数据
    /// </summary>
    public void GetIMU(out float yaw, out float pitch, out float roll)
    {
        lock (lockObject)
        {
            yaw = currentData.yaw;
            pitch = currentData.pitch;
            roll = currentData.roll;
        }
    }

    /// <summary>
    /// 获取当前偏航角
    /// </summary>
    public float GetYaw()
    {
        lock (lockObject)
        {
            return currentData.yaw;
        }
    }

    /// <summary>
    /// 获取当前俯仰角
    /// </summary>
    public float GetPitch()
    {
        lock (lockObject)
        {
            return currentData.pitch;
        }
    }

    /// <summary>
    /// 获取完整BCI数据
    /// </summary>
    public BCIData GetCurrentData()
    {
        lock (lockObject)
        {
            return currentData;
        }
    }

    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected()
    {
        return isConnected;
    }

    /// <summary>
    /// 获取接收速率
    /// </summary>
    public float GetReceiveRate()
    {
        return receiveRate;
    }

    /// <summary>
    /// 获取连接状态信息
    /// </summary>
    public string GetConnectionInfo()
    {
        return $"连接状态: {(isConnected ? "已连接" : "未连接")}\n" +
               $"服务器: {serverHost}:{serverPort}\n" +
               $"接收速率: {receiveRate:F1} 包/秒\n" +
               $"专注力: {currentData.attention:F1}\n" +
               $"信号质量: {currentData.signalQuality:F2}";
    }
    #endregion
}

/// <summary>
/// 主线程执行器
/// 用于在子线程中调用Unity主线程的方法
/// </summary>
public class UnityMainThread : MonoBehaviour
{
    private static UnityMainThread instance;
    private static readonly object lockObj = new object();
    private static System.Collections.Generic.Queue<Action> actionQueue = 
        new System.Collections.Generic.Queue<Action>();

    void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        lock (lockObj)
        {
            while (actionQueue.Count > 0)
            {
                Action action = actionQueue.Dequeue();
                action?.Invoke();
            }
        }
    }

    /// <summary>
    /// 在主线程执行Action
    /// </summary>
    public static void Execute(Action action)
    {
        if (instance == null)
        {
            // 如果没有实例，创建一个
            GameObject go = new GameObject("UnityMainThread");
            instance = go.AddComponent<UnityMainThread>();
            DontDestroyOnLoad(go);
        }

        lock (lockObj)
        {
            actionQueue.Enqueue(action);
        }
    }
}
