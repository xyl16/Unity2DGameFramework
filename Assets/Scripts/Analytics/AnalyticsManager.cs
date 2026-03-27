using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 数据分析管理器
/// 收集用户行为、游戏数据、错误日志等信息
/// </summary>
public class AnalyticsManager : MonoBehaviour
{
    private static AnalyticsManager instance;
    private static bool isApplicationQuitting = false;

    public static AnalyticsManager Instance
    {
        get
        {
            if (instance == null && !isApplicationQuitting)
            {
                GameObject obj = new GameObject("AnalyticsManager");
                instance = obj.AddComponent<AnalyticsManager>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }



    // 配置
    public bool enableAnalytics = true;
    public float sendInterval = 30f; // 每多少秒发送一次数据
    public int maxBufferSize = 100; // 最大缓冲区大小

    // 数据缓冲区
    private Queue<AnalyticsEvent> eventQueue = new Queue<AnalyticsEvent>();
    private float lastSendTime;

    // 会话数据
    private string sessionId;
    private DateTime sessionStartTime;

    // 错误日志
    private List<ErrorLog> errorLogs = new List<ErrorLog>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSession();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSession()
    {
        sessionId = Guid.NewGuid().ToString();
        sessionStartTime = DateTime.Now;
        LogEvent("SessionStart", new Dictionary<string, object>
        {
            { "SessionId", sessionId },
            { "StartTime", sessionStartTime.ToString() },
            { "DeviceModel", SystemInfo.deviceModel },
            { "OperatingSystem", SystemInfo.operatingSystem },
            { "GameVersion", Application.version }
        });
    }

    private void Update()
    {
        if (!enableAnalytics) return;

        // 定时发送数据
        if (Time.time - lastSendTime >= sendInterval || eventQueue.Count >= maxBufferSize)
        {
            SendEvents();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            // 发送剩余的事件
            if (eventQueue.Count > 0)
            {
                SendEvents();
            }

            LogEvent("SessionEnd", new Dictionary<string, object>
            {
                { "SessionId", sessionId },
                { "Duration", (DateTime.Now - sessionStartTime).TotalSeconds },
                { "EndTime", DateTime.Now.ToString() }
            });

            instance = null;
        }
    }

    /// <summary>
    /// 记录事件
    /// </summary>
    public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        if (!enableAnalytics) return;

        AnalyticsEvent analyticsEvent = new AnalyticsEvent
        {
            eventId = Guid.NewGuid().ToString(),
            eventName = eventName,
            sessionId = sessionId,
            timestamp = DateTime.Now,
            parameters = parameters ?? new Dictionary<string, object>()
        };

        eventQueue.Enqueue(analyticsEvent);
        Debug.Log($"[Analytics] 记录事件: {eventName}");
    }

    /// <summary>
    /// 记录用户行为
    /// </summary>
    public void LogUserAction(string actionName, string screenName, Dictionary<string, object> additionalData = null)
    {
        var parameters = new Dictionary<string, object>
        {
            { "ActionName", actionName },
            { "ScreenName", screenName },
            { "Timestamp", DateTime.Now.ToString() }
        };

        if (additionalData != null)
        {
            foreach (var kvp in additionalData)
            {
                parameters[kvp.Key] = kvp.Value;
            }
        }

        LogEvent("UserAction", parameters);
    }

    /// <summary>
    /// 记录游戏数据
    /// </summary>
    public void LogGameData(string dataType, Dictionary<string, object> gameData)
    {
        var parameters = new Dictionary<string, object>
        {
            { "DataType", dataType },
            { "Timestamp", DateTime.Now.ToString() }
        };

        foreach (var kvp in gameData)
        {
            parameters[kvp.Key] = kvp.Value;
        }

        LogEvent("GameData", parameters);
    }

    /// <summary>
    /// 记录错误日志
    /// </summary>
    public void LogError(string errorMessage, string stackTrace, string context = "")
    {
        ErrorLog errorLog = new ErrorLog
        {
            errorId = Guid.NewGuid().ToString(),
            sessionId = sessionId,
            timestamp = DateTime.Now,
            message = errorMessage,
            stackTrace = stackTrace,
            context = context
        };

        errorLogs.Add(errorLog);
        Debug.LogWarning($"[Analytics] 记录错误: {errorMessage}");

        // 同时记录为事件
        LogEvent("Error", new Dictionary<string, object>
        {
            { "ErrorId", errorLog.errorId },
            { "Message", errorMessage },
            { "Context", context },
            { "Timestamp", errorLog.timestamp.ToString() }
        });
    }

    /// <summary>
    /// 发送事件数据
    /// </summary>
    private void SendEvents()
    {
        if (eventQueue.Count == 0) return;

        List<AnalyticsEvent> eventsToSend = new List<AnalyticsEvent>();
        while (eventQueue.Count > 0 && eventsToSend.Count < maxBufferSize)
        {
            eventsToSend.Add(eventQueue.Dequeue());
        }

        // 这里可以发送到服务器
        string jsonData = SerializeEvents(eventsToSend);
        SendToServer(jsonData);

        lastSendTime = Time.time;
        Debug.Log($"[Analytics] 已发送 {eventsToSend.Count} 个事件");
    }

    /// <summary>
    /// 序列化事件数据
    /// </summary>
    private string SerializeEvents(List<AnalyticsEvent> events)
    {
        string json = "[";
        for (int i = 0; i < events.Count; i++)
        {
            json += SerializeEvent(events[i]);
            if (i < events.Count - 1) json += ",";
        }
        json += "]";
        return json;
    }

    private string SerializeEvent(AnalyticsEvent analyticsEvent)
    {
        string json = $"{{\"eventId\":\"{analyticsEvent.eventId}\",\"eventName\":\"{analyticsEvent.eventName}\",\"sessionId\":\"{analyticsEvent.sessionId}\",\"timestamp\":\"{analyticsEvent.timestamp.ToString("yyyy-MM-dd HH:mm:ss")}\",";

        json += "\"parameters\":{";
        var paramList = new List<string>();
        foreach (var kvp in analyticsEvent.parameters)
        {
            paramList.Add($"\"{kvp.Key}\":\"{kvp.Value}\"");
        }
        json += string.Join(",", paramList);
        json += "}}";

        return json;
    }

    /// <summary>
    /// 发送数据到服务器（示例）
    /// </summary>
    private void SendToServer(string jsonData)
    {
        // 这里使用网络管理器发送到服务器
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
        {
            // NetworkManager.Instance.SendMessage(analyticsMsgId, jsonData);
            Debug.Log("[Analytics] 发送分析数据到服务器");
        }
        else
        {
            // 离线存储
            SaveOfflineData(jsonData);
        }
    }

    /// <summary>
    /// 保存离线数据
    /// </summary>
    private void SaveOfflineData(string jsonData)
    {
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "analytics_offline.txt");
        System.IO.File.AppendAllText(filePath, jsonData + "\n");
        Debug.Log("[Analytics] 数据已保存到本地");
    }

    /// <summary>
    /// 导出错误日志
    /// </summary>
    public void ExportErrorLogs()
    {
        string report = "=== 错误日志报告 ===\n\n";
        report += $"生成时间: {DateTime.Now}\n";
        report += $"会话ID: {sessionId}\n\n";

        foreach (var error in errorLogs)
        {
            report += $"错误ID: {error.errorId}\n";
            report += $"时间: {error.timestamp}\n";
            report += $"上下文: {error.context}\n";
            report += $"消息: {error.message}\n";
            report += $"堆栈:\n{error.stackTrace}\n\n";
        }

        string filePath = System.IO.Path.Combine(Application.persistentDataPath, $"ErrorLogs_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        System.IO.File.WriteAllText(filePath, report);
        Debug.Log($"[Analytics] 错误日志已导出: {filePath}");
    }

    /// <summary>
    /// 获取统计数据
    /// </summary>
    public Dictionary<string, object> GetStatistics()
    {
        return new Dictionary<string, object>
        {
            { "SessionId", sessionId },
            { "SessionDuration", (DateTime.Now - sessionStartTime).TotalSeconds },
            { "EventCount", eventQueue.Count },
            { "ErrorCount", errorLogs.Count }
        };
    }

    /// <summary>
    /// 清除错误日志
    /// </summary>
    public void ClearErrorLogs()
    {
        errorLogs.Clear();
        Debug.Log("[Analytics] 已清除错误日志");
    }
}

/// <summary>
/// 分析事件
/// </summary>
[System.Serializable]
public class AnalyticsEvent
{
    public string eventId;
    public string eventName;
    public string sessionId;
    public DateTime timestamp;
    public Dictionary<string, object> parameters;
}

/// <summary>
/// 错误日志
/// </summary>
[System.Serializable]
public class ErrorLog
{
    public string errorId;
    public string sessionId;
    public DateTime timestamp;
    public string message;
    public string stackTrace;
    public string context;
}
