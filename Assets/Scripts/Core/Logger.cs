using System;
using System.Collections.Generic;
using UnityEngine;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public class Logger
{
    private static Logger instance;
    public static Logger Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new Logger();
            }
            return instance;
        }
    }

    private LogLevel logLevel = LogLevel.Debug;
    private List<LogEntry> logHistory = new List<LogEntry>();
    private int maxHistorySize = 100;

    public LogLevel LogLevel
    {
        get { return logLevel; }
        set { logLevel = value; }
    }

    public List<LogEntry> LogHistory
    {
        get { return new List<LogEntry>(logHistory); }
    }

    public void SetLogLevel(LogLevel level)
    {
        logLevel = level;
    }

    public void DebugLog(string message, string category = "General")
    {
        Log(LogLevel.Debug, message, category);
    }

    public void LogInfo(string message, string category = "General")
    {
        Log(LogLevel.Info, message, category);
    }

    public void LogWarning(string message, string category = "General")
    {
        Log(LogLevel.Warning, message, category);
    }

    public void LogError(string message, string category = "General")
    {
        Log(LogLevel.Error, message, category);
    }

    public void Log(LogLevel level, string message, string category = "General")
    {
        if (level < logLevel)
        {
            return;
        }

        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string formattedMessage = $"[{timestamp}] [{level}] [{category}] {message}";

        switch (level)
        {
            case LogLevel.Debug:
                UnityEngine.Debug.Log(formattedMessage);
                break;
            case LogLevel.Info:
                UnityEngine.Debug.Log(formattedMessage);
                break;
            case LogLevel.Warning:
                UnityEngine.Debug.LogWarning(formattedMessage);
                break;
            case LogLevel.Error:
                UnityEngine.Debug.LogError(formattedMessage);
                break;
        }

        AddToHistory(level, message, category, timestamp);
    }

    private void AddToHistory(LogLevel level, string message, string category, string timestamp)
    {
        LogEntry entry = new LogEntry
        {
            level = level,
            message = message,
            category = category,
            timestamp = timestamp
        };

        logHistory.Add(entry);

        if (logHistory.Count > maxHistorySize)
        {
            logHistory.RemoveAt(0);
        }
    }

    public void ClearHistory()
    {
        logHistory.Clear();
    }

    public List<LogEntry> GetLogsByCategory(string category)
    {
        List<LogEntry> filteredLogs = new List<LogEntry>();
        foreach (var entry in logHistory)
        {
            if (entry.category == category)
            {
                filteredLogs.Add(entry);
            }
        }
        return filteredLogs;
    }

    public List<LogEntry> GetLogsByLevel(LogLevel level)
    {
        List<LogEntry> filteredLogs = new List<LogEntry>();
        foreach (var entry in logHistory)
        {
            if (entry.level == level)
            {
                filteredLogs.Add(entry);
            }
        }
        return filteredLogs;
    }

    public void ExportLogs()
    {
        string logFilePath = Application.persistentDataPath + "/logs/";
        if (!System.IO.Directory.Exists(logFilePath))
        {
            System.IO.Directory.CreateDirectory(logFilePath);
        }

        string fileName = logFilePath + $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        System.IO.File.WriteAllLines(fileName, GetFormattedLogs());
        Debug.Log($"Logs exported to: {fileName}");
    }

    private string[] GetFormattedLogs()
    {
        string[] formattedLogs = new string[logHistory.Count];
        for (int i = 0; i < logHistory.Count; i++)
        {
            var entry = logHistory[i];
            formattedLogs[i] = $"[{entry.timestamp}] [{entry.level}] [{entry.category}] {entry.message}";
        }
        return formattedLogs;
    }

    [Serializable]
    public class LogEntry
    {
        public LogLevel level;
        public string message;
        public string category;
        public string timestamp;
    }
}
