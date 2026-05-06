using UnityEngine;

public class BCIData
{
    public float attention;
    public float yaw;
    public float pitch;
    public float roll;
    public float screenX;
    public float screenY;
    public int blink;
    public float signalQuality;
    public float battery;
    public long timestamp;
}

public class BCIManager : MonoBehaviour
{
    public static BCIManager Instance { get; private set; }

    public string serverHost = "127.0.0.1";
    public int serverPort = 5555;
    public bool autoReconnect = true;
    public float reconnectInterval = 5f;

    public event System.Action<float> OnAttentionUpdated;
    public event System.Action<float, float, float, float, float> OnIMUUpdated;
    public event System.Action<int> OnBlinkDetected;
    public event System.Action<bool> OnConnectionChanged;
    public event System.Action<BCIData> OnDataUpdated;

    private System.Net.Sockets.TcpClient client;
    private System.Net.Sockets.NetworkStream stream;
    private System.Threading.Thread receiveThread;
    private bool shouldStop = false;
    private readonly object lockObject = new object();

    private BCIData currentData = new BCIData();
    private bool isConnected = false;
    private int packetCount = 0;
    private float lastRateTime = 0f;
    private float receiveRate = 0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ConnectToServer();
    }

    void Update()
    {
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
            Disconnect();
        else if (autoReconnect)
            ConnectToServer();
    }

    public void ConnectToServer()
    {
        if (isConnected) return;
        try
        {
            client = new System.Net.Sockets.TcpClient();
            client.Connect(serverHost, serverPort);
            stream = client.GetStream();
            isConnected = true;
            shouldStop = false;
            receiveThread = new System.Threading.Thread(ReceiveDataLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            OnConnectionChanged?.Invoke(true);
            Debug.Log($"已连接到BCI服务器 {serverHost}:{serverPort}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"连接BCI服务器失败: {e.Message}");
            isConnected = false;
            if (autoReconnect)
                Invoke(nameof(ConnectToServer), reconnectInterval);
        }
    }

    public void Disconnect()
    {
        shouldStop = true;
        isConnected = false;
        if (stream != null) { try { stream.Close(); } catch { } stream = null; }
        if (client != null) { try { client.Close(); } catch { } client = null; }
        if (receiveThread != null) { receiveThread.Join(1000); receiveThread = null; }
        OnConnectionChanged?.Invoke(false);
    }

    private void ReceiveDataLoop()
    {
        byte[] buffer = new byte[4096];
        System.Text.StringBuilder messageBuilder = new System.Text.StringBuilder();
        while (!shouldStop && isConnected)
        {
            try
            {
                if (stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string data = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        messageBuilder.Append(data);
                        string messages = messageBuilder.ToString();
                        string[] lines = messages.Split('\n');
                        for (int i = 0; i < lines.Length - 1; i++)
                        {
                            if (!string.IsNullOrEmpty(lines[i]))
                                ProcessMessage(lines[i]);
                        }
                        messageBuilder.Clear();
                        if (!string.IsNullOrEmpty(lines[lines.Length - 1]))
                            messageBuilder.Append(lines[lines.Length - 1]);
                    }
                }
                else
                {
                    System.Threading.Thread.Sleep(10);
                }
            }
            catch (System.Exception e)
            {
                if (!shouldStop) Debug.LogError($"接收数据错误: {e.Message}");
                break;
            }
        }
        if (!shouldStop)
        {
            isConnected = false;
            OnConnectionChanged?.Invoke(false);
            if (autoReconnect)
                UnityMainThread.Execute(() => Invoke(nameof(ConnectToServer), reconnectInterval));
        }
    }

    private void ProcessMessage(string message)
    {
        try
        {
            var json = Newtonsoft.Json.Linq.JObject.Parse(message);
            string type = json["type"]?.ToString();

            if (type == "attention")
                ProcessAttentionData(json);
            else if (type == "imu")
                ProcessIMUData(json);
            else if (type == "status" || type == "heartbeat")
                return;

            packetCount++;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"解析消息错误: {e.Message}");
        }
    }

    private void ProcessAttentionData(Newtonsoft.Json.Linq.JObject json)
    {
        var data = json["data"]?.ToObject<Newtonsoft.Json.Linq.JObject>();
        if (data != null)
        {
            lock (lockObject)
            {
                currentData.attention = data["attention"]?.Value<float>() ?? 0f;
                currentData.signalQuality = data["quality"]?.Value<float>() ?? 1f;
                currentData.timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            UnityMainThread.Execute(() =>
            {
                OnAttentionUpdated?.Invoke(currentData.attention);
                OnDataUpdated?.Invoke(currentData);
            });
        }
    }

    private void ProcessIMUData(Newtonsoft.Json.Linq.JObject json)
    {
        var data = json["data"]?.ToObject<Newtonsoft.Json.Linq.JObject>();
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
                OnIMUUpdated?.Invoke(currentData.yaw, currentData.pitch, currentData.roll, currentData.screenX, currentData.screenY);
            });
        }
    }

    public float GetAttention()
    { lock (lockObject) { return currentData.attention; } }

    public float GetYaw()
    { lock (lockObject) { return currentData.yaw; } }

    public float GetPitch()
    { lock (lockObject) { return currentData.pitch; } }

    public BCIData GetCurrentData()
    { lock (lockObject) { return currentData; } }

    public bool IsConnected()
    { return isConnected; }

    public float GetReceiveRate()
    { return receiveRate; }
}

public class UnityMainThread : MonoBehaviour
{
    private static UnityMainThread instance;
    private static readonly object lockObj = new object();
    private static System.Collections.Generic.Queue<System.Action> actionQueue = new System.Collections.Generic.Queue<System.Action>();

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
                actionQueue.Dequeue()?.Invoke();
        }
    }

    public static void Execute(System.Action action)
    {
        if (instance == null)
        {
            var go = new GameObject("UnityMainThread");
            instance = go.AddComponent<UnityMainThread>();
            DontDestroyOnLoad(go);
        }
        lock (lockObj) { actionQueue.Enqueue(action); }
    }
}
