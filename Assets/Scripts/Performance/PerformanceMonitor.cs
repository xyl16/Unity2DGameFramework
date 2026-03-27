using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 性能监控系统
/// 监控FPS、内存使用、加载时间等性能指标
/// </summary>
public class PerformanceMonitor : MonoBehaviour
{
    private static PerformanceMonitor instance;
    private static bool isApplicationQuitting = false;

    public static PerformanceMonitor Instance
    {
        get
        {
            if (instance == null && !isApplicationQuitting)
            {
                GameObject obj = new GameObject("PerformanceMonitor");
                instance = obj.AddComponent<PerformanceMonitor>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
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

    [System.Serializable]
    public class PerformanceData
    {
        public float fps;
        public long memoryUsage;
        public float frameTime;
        public int objectCount;
        public float drawCalls;
        public int triangles;
        public float audioMemory;
        public float textureMemory;
        public float meshMemory;
        public int materialCount;

        public override string ToString()
        {
            return $"FPS: {fps:F1} | 内存: {memoryUsage / 1024 / 1024:F1}MB | 帧时间: {frameTime:F1}ms | 对象数量: {objectCount}";
        }
    }

    // 配置
    public bool enableMonitor = true;
    public float updateInterval = 0.5f;
    public bool showInGame = false;
    public int logDataInterval = 60; // 每多少秒记录一次日志

    // 性能数据
    private PerformanceData currentData = new PerformanceData();

    // FPS 计算
    private float deltaTime;
    private float fps;
    private int framesCount;
    private float fpsUpdateTime;

    // 加载时间统计
    private Dictionary<string, float> loadTimes = new Dictionary<string, float>();
    private string currentLoadingItem;
    private float loadingStartTime;

    // 日志记录
    private float lastLogTime;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!enableMonitor) return;

        UpdateFPS();
        UpdatePerformanceData();
        LogPerformanceData();
    }

    private void UpdateFPS()
    {
        deltaTime += Time.unscaledDeltaTime;
        framesCount++;

        if (deltaTime >= fpsUpdateTime)
        {
            fps = framesCount / deltaTime;
            currentData.fps = fps;
            framesCount = 0;
            deltaTime -= fpsUpdateTime;

            if (showInGame)
            {
                ShowPerformanceInfo();
            }
        }
    }

    private void UpdatePerformanceData()
    {
        currentData.frameTime = Time.unscaledDeltaTime * 1000f;
        currentData.drawCalls = 0f;  // Unity 无法直接获取DrawCalls，设为0
        currentData.triangles = 0;
        currentData.memoryUsage = System.GC.GetTotalMemory(false);
        currentData.audioMemory = 0f;
        currentData.textureMemory = 0f;
        currentData.meshMemory = 0f;
        currentData.materialCount = 0;
        currentData.objectCount = UnityEngine.Object.FindObjectsOfType<UnityEngine.Object>().Length;
    }

    private void LogPerformanceData()
    {
        if (Time.time - lastLogTime >= logDataInterval)
        {
            lastLogTime = Time.time;
            Logger.Instance.LogInfo($"性能数据: {currentData}", "Performance");
        }
    }

    /// <summary>
    /// 开始加载计时
    /// </summary>
    public void StartLoadTimer(string itemName)
    {
        currentLoadingItem = itemName;
        loadingStartTime = Time.realtimeSinceStartup;
        Debug.Log($"[Performance] 开始加载: {itemName}");
    }

    /// <summary>
    /// 结束加载计时
    /// </summary>
    public void EndLoadTimer()
    {
        if (!string.IsNullOrEmpty(currentLoadingItem))
        {
            float loadTime = Time.realtimeSinceStartup - loadingStartTime;
            loadTimes[currentLoadingItem] = loadTime;
            Debug.Log($"[Performance] 加载完成: {currentLoadingItem} - 耗时: {loadTime:F3}秒");
            currentLoadingItem = null;
        }
    }

    /// <summary>
    /// 获取加载时间
    /// </summary>
    public float GetLoadTime(string itemName)
    {
        if (loadTimes.ContainsKey(itemName))
        {
            return loadTimes[itemName];
        }
        return 0f;
    }

    /// <summary>
    /// 获取所有加载时间
    /// </summary>
    public Dictionary<string, float> GetAllLoadTimes()
    {
        return new Dictionary<string, float>(loadTimes);
    }

    /// <summary>
    /// 清除加载时间记录
    /// </summary>
    public void ClearLoadTimes()
    {
        loadTimes.Clear();
        Debug.Log("[Performance] 已清除所有加载时间记录");
    }

    /// <summary>
    /// 获取当前性能数据
    /// </summary>
    public PerformanceData GetCurrentData()
    {
        return currentData;
    }

    /// <summary>
    /// 在屏幕上显示性能信息
    /// </summary>
    private void ShowPerformanceInfo()
    {
        // 如果需要显示，可以创建一个Debug UI
        Debug.Log($"[Performance] {currentData.ToString()}");
    }

    /// <summary>
    /// 导出性能报告
    /// </summary>
    public void ExportPerformanceReport()
    {
        string report = "=== 性能监控报告 ===\n\n";
        report += $"生成时间: {System.DateTime.Now}\n\n";
        report += "=== 当前性能数据 ===\n";
        report += currentData.ToString() + "\n\n";
        report += "=== 加载时间统计 ===\n";
        foreach (var kvp in loadTimes)
        {
            report += $"{kvp.Key}: {kvp.Value:F3}秒\n";
        }

        string filePath = System.IO.Path.Combine(Application.persistentDataPath, $"PerformanceReport_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");
        System.IO.File.WriteAllText(filePath, report);
        Debug.Log($"[Performance] 性能报告已导出: {filePath}");
    }

    /// <summary>
    /// 设置是否显示性能信息
    /// </summary>
    public void SetShowInGame(bool show)
    {
        showInGame = show;
        Debug.Log($"[Performance] 性能显示: {(show ? "开启" : "关闭")}");
    }

    /// <summary>
    /// 开启/关闭监控
    /// </summary>
    public void SetEnableMonitor(bool enable)
    {
        enableMonitor = enable;
        Debug.Log($"[Performance] 性能监控: {(enable ? "开启" : "关闭")}");
    }
}
