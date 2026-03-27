using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 日志工具类 - 提供统一的日志输出和管理功能
/// </summary>
public static class DebugLogger
{
    private static bool _enableLog = true;
    private static bool _enableWarning = true;
    private static bool _enableError = true;
    private static bool _enableFileLog = false;
    private static string _logFilePath;

    /// <summary>
    /// 是否启用普通日志
    /// </summary>
    public static bool EnableLog
    {
        get => _enableLog;
        set => _enableLog = value;
    }

    /// <summary>
    /// 是否启用警告日志
    /// </summary>
    public static bool EnableWarning
    {
        get => _enableWarning;
        set => _enableWarning = value;
    }

    /// <summary>
    /// 是否启用错误日志
    /// </summary>
    public static bool EnableError
    {
        get => _enableError;
        set => _enableError = value;
    }

    /// <summary>
    /// 是否启用文件日志
    /// </summary>
    public static bool EnableFileLog
    {
        get => _enableFileLog;
        set => _enableFileLog = value;
    }

    /// <summary>
    /// 初始化日志系统
    /// </summary>
    public static void Initialize(bool enableFileLog = false, string customLogPath = null)
    {
        _enableFileLog = enableFileLog;

        if (enableFileLog)
        {
            if (string.IsNullOrEmpty(customLogPath))
            {
                string logDir = Path.Combine(Application.persistentDataPath, "Logs");
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                _logFilePath = Path.Combine(logDir, $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            }
            else
            {
                _logFilePath = customLogPath;
            }

            Log("日志系统已初始化", "System");
        }
    }

    /// <summary>
    /// 输出普通日志
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message, string tag = "Default", UnityEngine.Object context = null)
    {
        if (!_enableLog) return;

        string logMessage = FormatMessage(message, tag, LogType.Log);
        UnityEngine.Debug.Log(logMessage, context);
        WriteToFile(logMessage);
    }

    /// <summary>
    /// 输出警告日志
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(object message, string tag = "Warning", UnityEngine.Object context = null)
    {
        if (!_enableWarning) return;

        string logMessage = FormatMessage(message, tag, LogType.Warning);
        UnityEngine.Debug.LogWarning(logMessage, context);
        WriteToFile(logMessage);
    }

    /// <summary>
    /// 输出错误日志
    /// </summary>
    public static void LogError(object message, string tag = "Error", UnityEngine.Object context = null)
    {
        if (!_enableError) return;

        string logMessage = FormatMessage(message, tag, LogType.Error);
        UnityEngine.Debug.LogError(logMessage, context);
        WriteToFile(logMessage);
    }

    /// <summary>
    /// 输出带颜色的日志
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogColor(object message, string color = "white", string tag = "Color")
    {
        if (!_enableLog) return;

        string coloredMessage = $"<color={color}>{message}</color>";
        string logMessage = FormatMessage(coloredMessage, tag, LogType.Log);
        UnityEngine.Debug.Log(logMessage);
        WriteToFile(logMessage);
    }

    /// <summary>
    /// 输出格式化日志
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogFormat(string format, params object[] args)
    {
        if (!_enableLog) return;

        string message = string.Format(format, args);
        Log(message);
    }

    /// <summary>
    /// 输出分割线
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogSeparator(string title = "", char separator = '=')
    {
        if (!_enableLog) return;

        string line = new string(separator, 50);
        if (string.IsNullOrEmpty(title))
            Log(line);
        else
            Log($"{line} {title} {line}");
    }

    /// <summary>
    /// 输出带堆栈信息的日志
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWithStackTrace(object message, string tag = "Trace")
    {
        if (!_enableLog) return;

        string stackTrace = StackTraceUtility.ExtractStackTrace();
        string logMessage = FormatMessage(message, tag, LogType.Log);
        UnityEngine.Debug.Log(logMessage + "\n" + stackTrace);
        WriteToFile(logMessage);
    }

    /// <summary>
    /// 格式化日志消息
    /// </summary>
    private static string FormatMessage(object message, string tag, LogType logType)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string typeTag = logType.ToString().ToUpper();

        return $"[{timestamp}][{typeTag}][{tag}] {message}";
    }

    /// <summary>
    /// 写入日志到文件
    /// </summary>
    private static void WriteToFile(string message)
    {
        if (!_enableFileLog || string.IsNullOrEmpty(_logFilePath)) return;

        try
        {
            File.AppendAllText(_logFilePath, message + "\n", Encoding.UTF8);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"写入日志文件失败: {e.Message}");
        }
    }

    /// <summary>
    /// 清空日志文件
    /// </summary>
    public static void ClearLogFile()
    {
        if (!string.IsNullOrEmpty(_logFilePath) && File.Exists(_logFilePath))
        {
            File.Delete(_logFilePath);
            Log("日志文件已清空", "System");
        }
    }

    /// <summary>
    /// 获取日志文件路径
    /// </summary>
    public static string GetLogFilePath()
    {
        return _logFilePath;
    }

    /// <summary>
    /// 断言检查
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public static void Assert(bool condition, string message = "断言失败")
    {
        if (!condition)
        {
            LogError(message, "Assert");
        }
    }
}
