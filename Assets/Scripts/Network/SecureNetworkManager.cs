using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 安全网络管理器 - 支持加密和完整消息协议
/// </summary>
public class SecureNetworkManager : MonoBehaviour
{
    private static SecureNetworkManager instance;
    private static bool isApplicationQuitting = false;

    public static SecureNetworkManager Instance
    {
        get
        {
            if (instance == null && !isApplicationQuitting)
            {
                GameObject obj = new GameObject("SecureNetworkManager");
                instance = obj.AddComponent<SecureNetworkManager>();
            }
            return instance;
        }
    }

    private SocketClient socketClient;
    private CryptoManager cryptoManager;
    private MessageSerializer messageSerializer;
    private Dictionary<ushort, Action<NetworkMessage>> messageHandlers = new Dictionary<ushort, Action<NetworkMessage>>();
    private Queue<Action> mainThreadQueue = new Queue<Action>();

    public Action OnConnected;
    public Action OnDisconnected;
    public Action<string> OnError;

    /// <summary>
    /// 消息序列化类型
    /// </summary>
    public MessageSerializer.SerializeType SerializeType = MessageSerializer.SerializeType.JSON;

    /// <summary>
    /// 是否启用加密
    /// </summary>
    public bool EnableEncryption = false;

    /// <summary>
    /// 是否启用压缩
    /// </summary>
    public bool EnableCompression = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            cryptoManager = gameObject.AddComponent<CryptoManager>();
            socketClient = gameObject.AddComponent<SocketClient>();
            messageSerializer = new MessageSerializer(SerializeType);

            socketClient.OnConnected += HandleConnected;
            socketClient.OnDisconnected += HandleDisconnected;
            socketClient.OnError += (error) => OnError?.Invoke(error);
            socketClient.OnDataReceived += HandleDataReceived;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        while (mainThreadQueue.Count > 0)
        {
            Action action = mainThreadQueue.Dequeue();
            action?.Invoke();
        }

        // 心跳检测
        if (IsConnected && Time.time - lastHeartbeatTime >= heartbeatInterval)
        {
            SendHeartbeat();
            lastHeartbeatTime = Time.time;
        }
    }

    private void HandleConnected()
    {
        Logger.Instance.LogInfo("Connected to server (Secure)", "SecureNetwork");
        EventManager.Instance.InvokeEvent("SecureNetworkConnected");
        OnConnected?.Invoke();
    }

    private void HandleDisconnected()
    {
        Logger.Instance.LogWarning("Disconnected from server (Secure)", "SecureNetwork");
        EventManager.Instance.InvokeEvent("SecureNetworkDisconnected");
        OnDisconnected?.Invoke();
    }

    private void HandleDataReceived(byte[] data)
    {
        mainThreadQueue.Enqueue(() =>
        {
            try
            {
                // 解密（如果启用）
                byte[] decryptedData = data;
                if (EnableEncryption && cryptoManager.IsEncryptionEnabled)
                {
                    decryptedData = cryptoManager.DecryptSession(data);
                    if (decryptedData == null)
                    {
                        Logger.Instance.LogError("Failed to decrypt message", "SecureNetwork");
                        return;
                    }
                }

                // 解压（如果启用）
                if (EnableCompression)
                {
                    messageSerializer.Decompress(decryptedData);
                }

                // 解析消息包
                NetworkMessage message = MessagePacketBuilder.ParsePacket(decryptedData);

                // 验证校验和
                if (!message.VerifyChecksum())
                {
                    Logger.Instance.LogWarning($"Message checksum verification failed: {message.messageId}", "SecureNetwork");
                    return;
                }

                // 分发消息
                if (messageHandlers.TryGetValue(message.messageId, out Action<NetworkMessage> handler))
                {
                    handler(message);
                }
                else
                {
                    Logger.Instance.LogWarning($"No handler for message ID: {message.messageId}", "SecureNetwork");
                }
            }
            catch (Exception e)
            {
                Logger.Instance.LogError($"Failed to process received data: {e.Message}", "SecureNetwork");
            }
        });
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    public void SendMessage<T>(ushort messageId, T data) where T : class, new()
    {
        if (!socketClient.IsConnected)
        {
            Logger.Instance.LogWarning("Cannot send message: not connected to server", "SecureNetwork");
            return;
        }

        try
        {
            // 构建消息包
            byte[] packet = MessagePacketBuilder.BuildPacket(messageId, data, SerializeType);

            // 压缩（如果启用）
            if (EnableCompression)
            {
                packet = messageSerializer.Compress(packet);
            }

            // 加密（如果启用）
            if (EnableEncryption && cryptoManager.IsEncryptionEnabled)
            {
                packet = cryptoManager.EncryptSession(packet);
                if (packet == null)
                {
                    Logger.Instance.LogError("Failed to encrypt message", "SecureNetwork");
                    return;
                }
            }

            // 发送
            socketClient.Send(packet);
            Logger.Instance.LogInfo($"Sending secure message: {messageId}", "SecureNetwork");
        }
        catch (Exception e)
        {
            Logger.Instance.LogError($"Failed to send message: {e.Message}", "SecureNetwork");
        }
    }

    /// <summary>
    /// 注册消息处理器
    /// </summary>
    public void RegisterMessageHandler<T>(ushort messageId, Action<T> handler) where T : class, new()
    {
        Action<NetworkMessage> wrapper = (message) =>
        {
            try
            {
                T data = MessagePacketBuilder.ExtractData<T>(message, SerializeType);
                handler(data);
            }
            catch (Exception e)
            {
                Logger.Instance.LogError($"Failed to deserialize message {messageId}: {e.Message}", "SecureNetwork");
            }
        };

        if (!messageHandlers.ContainsKey(messageId))
        {
            messageHandlers[messageId] = wrapper;
            Logger.Instance.LogInfo($"Registered handler for message ID: {messageId}", "SecureNetwork");
        }
        else
        {
            Logger.Instance.LogWarning($"Handler already registered for message ID: {messageId}", "SecureNetwork");
        }
    }

    /// <summary>
    /// 注销消息处理器
    /// </summary>
    public void UnregisterMessageHandler(ushort messageId)
    {
        if (messageHandlers.ContainsKey(messageId))
        {
            messageHandlers.Remove(messageId);
            Logger.Instance.LogInfo($"Unregistered handler for message ID: {messageId}", "SecureNetwork");
        }
    }

    /// <summary>
    /// 连接服务器
    /// </summary>
    public void Connect(string ip, int port, bool enableEncryption = false)
    {
        Logger.Instance.LogInfo($"Connecting to {ip}:{port} (Encryption: {enableEncryption})", "SecureNetwork");
        EnableEncryption = enableEncryption;

        if (enableEncryption)
        {
            // 生成临时会话密钥
            byte[] key = cryptoManager.GenerateRandomKey(32);
            byte[] iv = cryptoManager.GenerateRandomIV(16);
            cryptoManager.InitializeSessionEncryption(key, iv);
        }

        socketClient.Connect(ip, port);
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public void Disconnect()
    {
        if (socketClient != null)
        {
            socketClient.Disconnect();
        }

        if (EnableEncryption)
        {
            cryptoManager.DisableEncryption();
        }
    }

    /// <summary>
    /// 重连
    /// </summary>
    public void Reconnect()
    {
        if (socketClient != null && !socketClient.IsConnected)
        {
            ConfigManager.AppConfig config = ConfigManager.Instance.GetConfig();
            Logger.Instance.LogInfo($"Reconnecting to {config.serverIP}:{config.serverPort}", "SecureNetwork");
            Connect(config.serverIP, config.serverPort, EnableEncryption);
        }
    }

    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected
    {
        get { return socketClient != null && socketClient.IsConnected; }
    }

    /// <summary>
    /// 清除所有处理器
    /// </summary>
    public void ClearAllHandlers()
    {
        messageHandlers.Clear();
        Logger.Instance.LogInfo("Cleared all message handlers", "SecureNetwork");
    }

    /// <summary>
    /// 设置序列化类型
    /// </summary>
    public void SetSerializeType(MessageSerializer.SerializeType type)
    {
        SerializeType = type;
        messageSerializer.SetSerializeType(type);
        Logger.Instance.LogInfo($"Serialize type set to: {type}", "SecureNetwork");
    }

    /// <summary>
    /// 心跳检测
    /// </summary>
    private float lastHeartbeatTime = 0f;
    private float heartbeatInterval = 30f; // 心跳间隔

    /// <summary>
    /// 发送心跳
    /// </summary>
    private void SendHeartbeat()
    {
        HeartbeatMessage heartbeat = new HeartbeatMessage
        {
            timestamp = DateTime.UtcNow.Ticks
        };
        SendMessage(1000, heartbeat);
    }

    /// <summary>
    /// 获取加密管理器
    /// </summary>
    public CryptoManager CryptoManager
    {
        get { return cryptoManager; }
    }

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}

/// <summary>
/// 心跳消息
/// </summary>
[Serializable]
public class HeartbeatMessage
{
    public long timestamp;
}
