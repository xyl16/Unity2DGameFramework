using System;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    private static NetworkManager instance;
    private static bool isApplicationQuitting = false;

    public static NetworkManager Instance
    {
        get
        {
            if (instance == null && !isApplicationQuitting)
            {
                GameObject obj = new GameObject("NetworkManager");
                instance = obj.AddComponent<NetworkManager>();
            }
            return instance;
        }
    }

    private SocketClient socketClient;
    private Dictionary<ushort, Action<byte[]>> messageHandlers = new Dictionary<ushort, Action<byte[]>>();
    private Queue<Action> mainThreadQueue = new Queue<Action>();

    public Action OnConnected;
    public Action OnDisconnected;
    public Action<string> OnError;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            socketClient = gameObject.AddComponent<SocketClient>();
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
    }

    private void HandleConnected()
    {
        if (!isApplicationQuitting)
        {
            Logger.Instance?.LogInfo("Connected to server", "Network");
            EventManager.Instance?.InvokeEvent("NetworkConnected");
            OnConnected?.Invoke();
        }
    }

    private void HandleDisconnected()
    {
        if (!isApplicationQuitting)
        {
            Logger.Instance?.LogWarning("Disconnected from server", "Network");
            EventManager.Instance?.InvokeEvent("NetworkDisconnected");
            OnDisconnected?.Invoke();
        }
    }

    private void HandleDataReceived(byte[] data)
    {
        ushort msgId = MessageProtocol.GetMessageId(data);
        mainThreadQueue.Enqueue(() =>
        {
            if (messageHandlers.TryGetValue(msgId, out Action<byte[]> handler))
            {
                handler(data);
            }
            else
            {
                Logger.Instance.LogWarning($"No handler for message ID: {msgId}", "Network");
            }
        });
    }

    public void RegisterMessageHandler(ushort msgId, Action<byte[]> handler)
    {
        if (!messageHandlers.ContainsKey(msgId))
        {
            messageHandlers[msgId] = handler;
            Logger.Instance.LogInfo($"Registered handler for message ID: {msgId}", "Network");
        }
        else
        {
            Logger.Instance.LogWarning($"Handler already registered for message ID: {msgId}", "Network");
        }
    }

    public void UnregisterMessageHandler(ushort msgId)
    {
        if (messageHandlers.ContainsKey(msgId))
        {
            messageHandlers.Remove(msgId);
            Logger.Instance.LogInfo($"Unregistered handler for message ID: {msgId}", "Network");
        }
    }

    public void SendMessage(ushort msgId, object data)
    {
        if (socketClient != null && socketClient.IsConnected)
        {
            try
            {
                byte[] message = MessageProtocol.Serialize(msgId, data);
                socketClient.Send(message);
                Logger.Instance.LogInfo($"Sending message: {msgId}", "Network");
            }
            catch (Exception e)
            {
                Logger.Instance.LogError($"Failed to send message: {e.Message}", "Network");
            }
        }
        else
        {
            Logger.Instance.LogWarning("Cannot send message: not connected to server", "Network");
        }
    }

    public void Connect(string ip, int port)
    {
        if (socketClient != null)
        {
            Logger.Instance.LogInfo($"Connecting to {ip}:{port}", "Network");
            socketClient.Connect(ip, port);
        }
    }

    public void Disconnect()
    {
        if (socketClient != null)
        {
            socketClient.Disconnect();
        }
    }

    public void Reconnect()
    {
        if (socketClient != null && !socketClient.IsConnected)
        {
            ConfigManager.AppConfig config = ConfigManager.Instance.GetConfig();
            Logger.Instance.LogInfo($"Reconnecting to {config.serverIP}:{config.serverPort}", "Network");
            socketClient.Connect(config.serverIP, config.serverPort);
        }
    }

    public bool IsConnected
    {
        get { return socketClient != null && socketClient.IsConnected; }
    }

    public void ClearAllHandlers()
    {
        messageHandlers.Clear();
        Logger.Instance.LogInfo("Cleared all message handlers", "Network");
    }

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            isApplicationQuitting = true;
            Disconnect();
            instance = null;
        }
    }
}
