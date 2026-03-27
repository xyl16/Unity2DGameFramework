using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 自动化性能分析工具 - 自动记录和分析游戏性能数据
/// </summary>
public class PerformanceAnalyzer : EditorWindow
{
    private enum AnalysisDuration
    {
        _30秒,
        _1分钟,
        _2分钟,
        _5分钟,
        自定义
    }

    private enum AnalysisMode
    {
        运行时分析,
        场景加载分析,
        帧率稳定性分析,
        内存占用分析
    }

    private Vector2 scrollPosition;
    private bool isAnalyzing = false;
    private float analysisStartTime;
    private float customDuration = 60f;
    private AnalysisDuration duration = AnalysisDuration._1分钟;
    private AnalysisMode mode = AnalysisMode.运行时分析;
    private bool autoExport = true;
    private string outputPath = "";

    // 分析数据
    private List<float> fpsSamples = new List<float>();
    private List<float> frameTimeSamples = new List<float>();
    private List<long> memorySamples = new List<long>();
    private List<int> objectCountSamples = new List<int>();

    // 分析结果
    private float avgFps;
    private float minFps;
    private float maxFps;
    private float avgFrameTime;
    private long avgMemory;
    private long maxMemory;
    private float frameDropRate; // 掉帧率

    // 场景加载分析
    private string sceneToAnalyze = "";
    private float sceneLoadTime = 0f;

    [MenuItem("Tools/性能分析/自动化性能分析")]
    public static void ShowWindow()
    {
        GetWindow<PerformanceAnalyzer>("自动化性能分析");
    }

    [MenuItem("Tools/性能分析/快速分析当前场景")]
    public static void QuickAnalyzeCurrentScene()
    {
        PerformanceAnalyzer window = GetWindow<PerformanceAnalyzer>("自动化性能分析");
        window.mode = AnalysisMode.运行时分析;
        window.duration = AnalysisDuration._30秒;
        window.StartAnalysis();
    }

    [MenuItem("Tools/性能分析/分析场景加载时间")]
    public static void AnalyzeSceneLoadTime()
    {
        PerformanceAnalyzer window = GetWindow<PerformanceAnalyzer>("自动化性能分析");
        window.mode = AnalysisMode.场景加载分析;
        window.sceneToAnalyze = EditorSceneManager.GetActiveScene().name;
        window.StartAnalysis();
    }

    private void OnGUI()
    {
        GUILayout.Label("自动化性能分析工具", EditorStyles.boldLabel);
        GUILayout.Label("自动记录并分析游戏性能数据", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        if (isAnalyzing)
        {
            DrawAnalyzingUI();
        }
        else
        {
            DrawSetupUI();
            DrawResultsUI();
        }
    }

    private void DrawSetupUI()
    {
        GUILayout.Label("分析设置", EditorStyles.label);

        mode = (AnalysisMode)EditorGUILayout.EnumPopup("分析模式", mode);

        switch (mode)
        {
            case AnalysisMode.运行时分析:
            case AnalysisMode.帧率稳定性分析:
            case AnalysisMode.内存占用分析:
                duration = (AnalysisDuration)EditorGUILayout.EnumPopup("分析时长", duration);
                if (duration == AnalysisDuration.自定义)
                {
                    customDuration = EditorGUILayout.FloatField("自定义时长(秒)", customDuration);
                    customDuration = Mathf.Max(1f, customDuration);
                }
                break;

            case AnalysisMode.场景加载分析:
                sceneToAnalyze = EditorGUILayout.TextField("场景名称", sceneToAnalyze);
                break;
        }

        autoExport = EditorGUILayout.Toggle("自动导出报告", autoExport);
        if (autoExport)
        {
            outputPath = EditorGUILayout.TextField("输出路径", outputPath);
            if (GUILayout.Button("选择输出路径"))
            {
                outputPath = EditorUtility.OpenFolderPanel("选择报告输出路径", outputPath, "");
            }
        }

        EditorGUILayout.Space();

        // 开始按钮
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("开始分析", GUILayout.Height(40)))
            {
                StartAnalysis();
            }
            if (GUILayout.Button("加载并分析", GUILayout.Height(40)))
            {
                StartAnalysisWithSceneLoad();
            }
        }
    }

    private void DrawAnalyzingUI()
    {
        float elapsed = Time.realtimeSinceStartup - analysisStartTime;
        float totalDuration = GetTotalDuration();
        float progress = Mathf.Clamp01(elapsed / totalDuration);

        GUILayout.Label("分析进行中...", EditorStyles.boldLabel);
        GUILayout.Label($"已用时间: {elapsed:F1}秒 / {totalDuration:F1}秒", EditorStyles.largeLabel);

        // 进度条
        Rect rect = EditorGUILayout.GetControlRect(false, 20);
        EditorGUI.ProgressBar(rect, progress, $"{progress * 100:F1}%");

        EditorGUILayout.Space();

        // 实时数据
        GUILayout.Label("实时数据", EditorStyles.label);
        float currentFps = fpsSamples.Count > 0 ? fpsSamples[fpsSamples.Count - 1] : 0f;
        GUILayout.Label($"FPS: {currentFps:F1}");
        float currentMemory = memorySamples.Count > 0 ? memorySamples[memorySamples.Count - 1] / 1024f / 1024f : 0f;
        GUILayout.Label($"内存: {currentMemory:F1}MB");
        int currentObjectCount = objectCountSamples.Count > 0 ? objectCountSamples[objectCountSamples.Count - 1] : 0;
        GUILayout.Label($"对象数量: {currentObjectCount}");

        EditorGUILayout.Space();

        if (GUILayout.Button("停止分析"))
        {
            StopAnalysis();
        }
    }

    private void DrawResultsUI()
    {
        if (fpsSamples.Count == 0) return;

        EditorGUILayout.Space();
        EditorGUILayout.Separator();
        GUILayout.Label("分析结果", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // FPS 统计
        GUILayout.Label("帧率(FPS)统计", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"平均帧率:", $"{avgFps:F1} FPS");
        EditorGUILayout.LabelField($"最高帧率:", $"{maxFps:F1} FPS");
        EditorGUILayout.LabelField($"最低帧率:", $"{minFps:F1} FPS");
        EditorGUILayout.LabelField($"掉帧率:", $"{frameDropRate:F2}% (低于30帧)");

        // 帧时间统计
        EditorGUILayout.Space();
        GUILayout.Label("帧时间统计", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"平均帧时间:", $"{avgFrameTime:F2} ms");
        EditorGUILayout.LabelField($"1/60帧标准:", $"{1000f / 60f:F2} ms");
        EditorGUILayout.LabelField($"是否达标:", avgFrameTime < 16.67f ? "✓ 是" : "✗ 否");

        // 内存统计
        EditorGUILayout.Space();
        GUILayout.Label("内存统计", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"平均内存:", $"{avgMemory / 1024 / 1024:F1} MB");
        EditorGUILayout.LabelField($"峰值内存:", $"{maxMemory / 1024 / 1024:F1} MB");

        // 场景加载时间
        if (mode == AnalysisMode.场景加载分析 && sceneLoadTime > 0)
        {
            EditorGUILayout.Space();
            GUILayout.Label("场景加载时间", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"加载耗时:", $"{sceneLoadTime:F3} 秒");

            // 性能评级
            string grade = sceneLoadTime < 1f ? "A (优秀)" :
                          sceneLoadTime < 3f ? "B (良好)" :
                          sceneLoadTime < 5f ? "C (一般)" : "D (较差)";
            EditorGUILayout.LabelField($"性能评级:", grade);
        }

        // 性能建议
        EditorGUILayout.Space();
        GUILayout.Label("性能建议", EditorStyles.boldLabel);
        DrawPerformanceSuggestions();

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // 导出按钮
        if (GUILayout.Button("导出详细报告", GUILayout.Height(30)))
        {
            ExportDetailedReport();
        }
    }

    private void DrawPerformanceSuggestions()
    {
        List<string> suggestions = new List<string>();

        if (avgFps < 30)
            suggestions.Add("• 平均帧率低于30，建议优化渲染和逻辑");
        if (minFps < 20)
            suggestions.Add("• 出现严重掉帧(低于20FPS)，检查是否有卡顿操作");
        if (avgFrameTime > 20)
            suggestions.Add("• 平均帧时间过高，建议减少Update中的计算量");
        if (frameDropRate > 10)
            suggestions.Add("• 掉帧率超过10%，需要找出导致卡顿的原因");
        if (maxMemory > 500 * 1024 * 1024)
            suggestions.Add("• 峰值内存超过500MB，注意内存泄漏");

        if (suggestions.Count == 0)
        {
            EditorGUILayout.HelpBox("性能表现良好！", MessageType.Info);
        }
        else
        {
            foreach (var suggestion in suggestions)
            {
                EditorGUILayout.LabelField(suggestion, EditorStyles.wordWrappedLabel);
            }
        }
    }

    private float GetTotalDuration()
    {
        switch (duration)
        {
            case AnalysisDuration._30秒: return 30f;
            case AnalysisDuration._1分钟: return 60f;
            case AnalysisDuration._2分钟: return 120f;
            case AnalysisDuration._5分钟: return 300f;
            case AnalysisDuration.自定义: return customDuration;
            default: return 60f;
        }
    }

    private void StartAnalysis()
    {
        // 检查是否在运行模式
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("提示", "请先进入Play模式后再开始分析", "确定");
            return;
        }

        // 清空数据
        ClearSamples();

        // 开始分析
        isAnalyzing = true;
        analysisStartTime = Time.realtimeSinceStartup;
        EditorApplication.update += OnEditorUpdate;

        Debug.Log("[PerformanceAnalyzer] 开始性能分析");
    }

    private void StartAnalysisWithSceneLoad()
    {
        if (string.IsNullOrEmpty(sceneToAnalyze))
        {
            EditorUtility.DisplayDialog("错误", "请输入场景名称", "确定");
            return;
        }

        // 尝试加载场景
        string scenePath = EditorBuildSettings.scenes
            .FirstOrDefault(s => s.path.Contains(sceneToAnalyze)).path;

        if (string.IsNullOrEmpty(scenePath))
        {
            EditorUtility.DisplayDialog("错误", $"未找到场景: {sceneToAnalyze}", "确定");
            return;
        }

        // 先开始分析
        StartAnalysis();

        // 然后加载场景
        float loadStartTime = Time.realtimeSinceStartup;
        EditorSceneManager.LoadSceneAsync(sceneToAnalyze, LoadSceneMode.Single);

        // 等待加载完成
        EditorApplication.update += () =>
        {
            if (!EditorApplication.isPlaying)
            {
                sceneLoadTime = Time.realtimeSinceStartup - loadStartTime;
                Debug.Log($"[PerformanceAnalyzer] 场景加载完成，耗时: {sceneLoadTime:F3}秒");
            }
        };
    }

    private void StopAnalysis()
    {
        isAnalyzing = false;
        EditorApplication.update -= OnEditorUpdate;

        // 计算分析结果
        CalculateResults();

        Debug.Log("[PerformanceAnalyzer] 性能分析完成");

        if (autoExport)
        {
            ExportReport();
        }
    }

    private void OnEditorUpdate()
    {
        if (!isAnalyzing) return;

        // 检查是否达到分析时长
        float elapsed = Time.realtimeSinceStartup - analysisStartTime;
        if (elapsed >= GetTotalDuration())
        {
            StopAnalysis();
            return;
        }

        // 收集性能数据
        CollectPerformanceData();
    }

    private void CollectPerformanceData()
    {
        // 这里需要从 PerformanceMonitor 获取数据
        // 由于 Editor 模式下无法直接访问运行时对象，我们使用其他方法

        // 模拟数据收集（实际应该通过反射或其他方式获取）
        // 注意：这是简化的实现，实际项目中需要更完善的数据收集机制

        // 计算估算的FPS（基于帧时间）
        float deltaTime = Time.unscaledDeltaTime;
        float fps = deltaTime > 0 ? 1f / deltaTime : 60f;

        fpsSamples.Add(fps);
        frameTimeSamples.Add(deltaTime * 1000f);
        memorySamples.Add(GC.GetTotalMemory(false));
        objectCountSamples.Add(FindObjectsOfType<UnityEngine.Object>().Length);
    }

    private void CalculateResults()
    {
        if (fpsSamples.Count == 0) return;

        avgFps = fpsSamples.Average();
        minFps = fpsSamples.Min();
        maxFps = fpsSamples.Max();

        avgFrameTime = frameTimeSamples.Average();

        avgMemory = (long)memorySamples.Average();
        maxMemory = memorySamples.Max();

        // 计算掉帧率（低于30帧的比例）
        int droppedFrames = fpsSamples.Count(f => f < 30f);
        frameDropRate = (float)droppedFrames / fpsSamples.Count * 100f;
    }

    private void ClearSamples()
    {
        fpsSamples.Clear();
        frameTimeSamples.Clear();
        memorySamples.Clear();
        objectCountSamples.Clear();
    }

    private void ExportReport()
    {
        string report = GenerateReport();

        if (string.IsNullOrEmpty(outputPath))
        {
            outputPath = Path.Combine(Application.dataPath, "../PerformanceReports");
        }

        Directory.CreateDirectory(outputPath);

        string filename = $"PerformanceReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        string filepath = Path.Combine(outputPath, filename);

        File.WriteAllText(filepath, report);

        EditorUtility.RevealInFinder(filepath);
        Debug.Log($"[PerformanceAnalyzer] 报告已导出: {filepath}");
    }

    private void ExportDetailedReport()
    {
        string report = GenerateDetailedReport();

        SaveFileDialog(report);
    }

    private string GenerateReport()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("=== 性能分析报告 ===");
        sb.AppendLine($"生成时间: {DateTime.Now}");
        sb.AppendLine($"分析模式: {mode}");
        sb.AppendLine($"分析时长: {GetTotalDuration()}秒");
        sb.AppendLine($"采样数量: {fpsSamples.Count}");
        sb.AppendLine();

        sb.AppendLine("=== FPS 统计 ===");
        sb.AppendLine($"平均帧率: {avgFps:F1} FPS");
        sb.AppendLine($"最高帧率: {maxFps:F1} FPS");
        sb.AppendLine($"最低帧率: {minFps:F1} FPS");
        sb.AppendLine($"掉帧率: {frameDropRate:F2}%");
        sb.AppendLine();

        sb.AppendLine("=== 帧时间统计 ===");
        sb.AppendLine($"平均帧时间: {avgFrameTime:F2} ms");
        sb.AppendLine($"1/60帧标准: {1000f / 60f:F2} ms");
        sb.AppendLine($"性能达标: {(avgFrameTime < 16.67f ? "是" : "否")}");
        sb.AppendLine();

        sb.AppendLine("=== 内存统计 ===");
        sb.AppendLine($"平均内存: {avgMemory / 1024 / 1024:F1} MB");
        sb.AppendLine($"峰值内存: {maxMemory / 1024 / 1024:F1} MB");
        sb.AppendLine();

        if (mode == AnalysisMode.场景加载分析 && sceneLoadTime > 0)
        {
            sb.AppendLine("=== 场景加载 ===");
            sb.AppendLine($"加载耗时: {sceneLoadTime:F3} 秒");
            sb.AppendLine($"性能评级: {GetPerformanceGrade(sceneLoadTime)}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GenerateDetailedReport()
    {
        var report = GenerateReport();

        report += "\n=== 详细数据采样 ===\n";
        report += "序号\tFPS\t帧时间(ms)\t内存(MB)\t对象数\n";

        for (int i = 0; i < fpsSamples.Count; i += Math.Max(1, fpsSamples.Count / 100))
        {
            report += $"{i}\t{fpsSamples[i]:F1}\t{frameTimeSamples[i]:F2}\t" +
                     $"{memorySamples[i] / 1024 / 1024:F1}\t{objectCountSamples[i]}\n";
        }

        return report;
    }

    private string GetPerformanceGrade(float loadTime)
    {
        if (loadTime < 1f) return "A (优秀)";
        if (loadTime < 3f) return "B (良好)";
        if (loadTime < 5f) return "C (一般)";
        return "D (较差)";
    }

    private void SaveFileDialog(string content)
    {
        string path = EditorUtility.SaveFilePanel(
            "保存性能报告",
            Application.dataPath,
            $"PerformanceReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
            "txt"
        );

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, content);
            EditorUtility.RevealInFinder(path);
            Debug.Log($"[PerformanceAnalyzer] 详细报告已导出: {path}");
        }
    }
}
