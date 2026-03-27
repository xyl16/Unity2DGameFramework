using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 自动化打包流水线 - 支持多平台自动化打包
/// </summary>
public class AutoBuildPipeline : EditorWindow
{
    private enum PlatformTarget
    {
        Windows,
        macOS,
        Linux,
        Android,
        iOS,
        WebGL
    }

    private enum BuildType
    {
        Development,
        Release
    }

    private enum BuildCompression
    {
        Default,
        LZ4,
        LZMA
    }

    // 配置
    private PlatformTarget buildTarget = PlatformTarget.Windows;
    private BuildType buildType = BuildType.Release;
    private int compressionType = 1; // 0: Default, 1: LZ4, 2: LZMA
    private string version = "1.0.0";
    private int buildNumber = 1;
    private string outputFolder = "../Builds";
    private string productName = "MyGame";
    private string bundleIdentifier = "com.mycompany.game";

    // 构建选项
    private bool runAfterBuild = false;
    private bool openFolderAfterBuild = true;
    private bool compressAssets = true;
    private bool optimizeMeshData = true;
    private bool keepDebugSymbols = false;

    // 场景配置
    private List<EditorBuildSettingsScene> scenesToBuild = new List<EditorBuildSettingsScene>();

    // 构建状态
    private bool isBuilding = false;
    private string buildStatus = "";
    private float buildProgress = 0f;

    // 日志
    private Vector2 logScrollPosition;
    private List<string> buildLogs = new List<string>();

    [MenuItem("Tools/打包流水线/自动化打包工具")]
    public static void ShowWindow()
    {
        GetWindow<AutoBuildPipeline>("自动化打包工具");
    }

    [MenuItem("Tools/打包流水线/快速打包当前平台")]
    public static void QuickBuildCurrentPlatform()
    {
        AutoBuildPipeline window = GetWindow<AutoBuildPipeline>("自动化打包工具");
        window.buildTarget = GetPlatformFromUnityBuildTarget(EditorUserBuildSettings.activeBuildTarget);
        window.StartBuild();
    }

    [MenuItem("Tools/打包流水线/打包所有平台")]
    public static void BuildAllPlatforms()
    {
        AutoBuildPipeline window = GetWindow<AutoBuildPipeline>("自动化打包工具");
        window.BuildMultiplePlatforms();
    }

    private void OnGUI()
    {
        if (isBuilding)
        {
            DrawBuildingUI();
        }
        else
        {
            DrawSetupUI();
            DrawBuildButton();
            DrawLogUI();
        }
    }

    private void DrawSetupUI()
    {
        GUILayout.Label("打包配置", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 平台和类型
        GUILayout.Label("目标平台", EditorStyles.label);
        buildTarget = (PlatformTarget)EditorGUILayout.EnumPopup("平台", buildTarget);

        buildType = (BuildType)EditorGUILayout.EnumPopup("构建类型", buildType);

        string[] compressionOptions = { "Default", "LZ4", "LZMA" };
        compressionType = EditorGUILayout.Popup("压缩方式", compressionType, compressionOptions);

        EditorGUILayout.Space();

        // 版本信息
        GUILayout.Label("版本信息", EditorStyles.label);
        version = EditorGUILayout.TextField("版本号", version);
        buildNumber = EditorGUILayout.IntField("构建号", buildNumber);
        productName = EditorGUILayout.TextField("产品名称", productName);

        if (buildTarget == PlatformTarget.Android || buildTarget == PlatformTarget.iOS)
        {
            bundleIdentifier = EditorGUILayout.TextField("包名/Bundle ID", bundleIdentifier);
        }

        EditorGUILayout.Space();

        // 输出路径
        GUILayout.Label("输出设置", EditorStyles.label);
        outputFolder = EditorGUILayout.TextField("输出文件夹", outputFolder);
        if (GUILayout.Button("选择输出文件夹"))
        {
            string path = EditorUtility.OpenFolderPanel("选择输出文件夹", outputFolder, "");
            if (!string.IsNullOrEmpty(path))
            {
                outputFolder = path;
            }
        }

        EditorGUILayout.Space();

        // 构建选项
        GUILayout.Label("构建选项", EditorStyles.label);
        runAfterBuild = EditorGUILayout.Toggle("构建后运行", runAfterBuild);
        openFolderAfterBuild = EditorGUILayout.Toggle("构建后打开文件夹", openFolderAfterBuild);
        compressAssets = EditorGUILayout.Toggle("压缩资源", compressAssets);
        optimizeMeshData = EditorGUILayout.Toggle("优化网格数据", optimizeMeshData);
        keepDebugSymbols = EditorGUILayout.Toggle("保留调试符号", keepDebugSymbols);

        EditorGUILayout.Space();

        // 场景配置
        GUILayout.Label("场景配置", EditorStyles.label);
        DrawSceneList();
    }

    private void DrawSceneList()
    {
        // 显示当前选中的场景
        var allScenes = EditorBuildSettings.scenes;
        GUILayout.Label($"当前场景列表 ({allScenes.Length})", EditorStyles.miniLabel);

        for (int i = 0; i < allScenes.Length; i++)
        {
            var scene = allScenes[i];
            bool enabled = EditorGUILayout.ToggleLeft(
                Path.GetFileNameWithoutExtension(scene.path),
                scene.enabled
            );

            if (enabled != scene.enabled)
            {
                var newScenes = allScenes.ToList();
                newScenes[i] = new EditorBuildSettingsScene(scene.path, enabled);
                EditorBuildSettings.scenes = newScenes.ToArray();
            }
        }

        if (GUILayout.Button("刷新场景列表"))
        {
            EditorBuildSettings.scenes = EditorBuildSettings.scenes;
        }
    }

    private void DrawBuildButton()
    {
        EditorGUILayout.Space();
        EditorGUILayout.Separator();

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = !isBuilding;

            if (GUILayout.Button("开始打包", GUILayout.Height(40)))
            {
                StartBuild();
            }

            if (GUILayout.Button("清理构建缓存", GUILayout.Height(40)))
            {
                ClearBuildCache();
            }

            GUI.enabled = true;
        }
    }

    private void DrawBuildingUI()
    {
        GUILayout.Label("正在构建...", EditorStyles.boldLabel);
        GUILayout.Label(buildStatus, EditorStyles.largeLabel);

        // 进度条
        Rect rect = EditorGUILayout.GetControlRect(false, 30);
        EditorGUI.ProgressBar(rect, buildProgress, $"{buildProgress * 100:F1}%");

        EditorGUILayout.Space();

        if (GUILayout.Button("取消构建"))
        {
            isBuilding = false;
            AddLog("构建已取消", "警告");
        }
    }

    private void DrawLogUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.Separator();

        GUILayout.Label("构建日志", EditorStyles.label);

        logScrollPosition = EditorGUILayout.BeginScrollView(logScrollPosition, GUILayout.Height(150));

        foreach (var log in buildLogs)
        {
            EditorGUILayout.LabelField(log, EditorStyles.wordWrappedMiniLabel);
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("清空日志"))
        {
            buildLogs.Clear();
        }
    }

    private void StartBuild()
    {
        // 验证配置
        if (string.IsNullOrEmpty(version))
        {
            EditorUtility.DisplayDialog("错误", "请输入版本号", "确定");
            return;
        }

        if (string.IsNullOrEmpty(productName))
        {
            EditorUtility.DisplayDialog("错误", "请输入产品名称", "确定");
            return;
        }

        // 准备构建
        ClearLogs();
        AddLog($"开始构建: {productName} v{version}", "信息");
        AddLog($"平台: {buildTarget}, 类型: {buildType}", "信息");
        AddLog($"压缩方式: {GetCompressionName()}", "信息");

        isBuilding = true;
        buildProgress = 0f;
        buildStatus = "准备中...";

        // 异步执行构建
        EditorApplication.delayCall += ExecuteBuild;
    }

    private void ExecuteBuild()
    {
        try
        {
            // 配置构建设置
            ConfigureBuildSettings();

            // 获取场景列表
            scenesToBuild = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .ToList();

            if (scenesToBuild.Count == 0)
            {
                throw new Exception("没有启用任何场景进行构建");
            }

            AddLog($"包含 {scenesToBuild.Count} 个场景", "信息");

            // 构建应用
            BuildApp();

            // 完成
            buildProgress = 1f;
            buildStatus = "构建完成！";
            AddLog("构建成功完成！", "成功");

            // 构建后操作
            if (openFolderAfterBuild)
            {
                string outputPath = GetOutputPath();
                if (Directory.Exists(outputPath))
                {
                    EditorUtility.RevealInFinder(outputPath);
                }
            }

            EditorUtility.DisplayDialog("构建完成", $"应用已成功构建到:\n{GetOutputPath()}", "确定");
        }
        catch (Exception e)
        {
            buildStatus = "构建失败";
            AddLog($"构建失败: {e.Message}", "错误");
            AddLog(e.StackTrace, "错误");
            EditorUtility.DisplayDialog("构建失败", e.Message, "确定");
        }
        finally
        {
            isBuilding = false;
        }
    }

    private void ConfigureBuildSettings()
    {
        PlayerSettings.productName = productName;
        PlayerSettings.bundleVersion = version;
        PlayerSettings.macOS.buildNumber = buildNumber.ToString();
        PlayerSettings.iOS.buildNumber = buildNumber.ToString();
        PlayerSettings.Android.bundleVersionCode = buildNumber;

        if (buildTarget == PlatformTarget.Android || buildTarget == PlatformTarget.iOS)
        {
            BuildTargetGroup targetGroup = GetBuildTargetGroup();
            PlayerSettings.SetApplicationIdentifier(
                targetGroup,
                bundleIdentifier
            );
        }

        // 配置构建类型
        BuildTargetGroup targetGroup2 = GetBuildTargetGroup();
        PlayerSettings.SetIl2CppCompilerConfiguration(
            targetGroup2,
            buildType == BuildType.Development ?
                Il2CppCompilerConfiguration.Debug :
                Il2CppCompilerConfiguration.Release
        );

        // 其他设置
        PlayerSettings.SetIl2CppCompilerConfiguration(
            BuildTargetGroup.Standalone,
            buildType == BuildType.Development ?
                Il2CppCompilerConfiguration.Debug :
                Il2CppCompilerConfiguration.Release
        );
    }

    private void BuildApp()
    {
        buildStatus = "构建中...";
        buildProgress = 0.3f;

        // 设置构建平台
        BuildTarget target = GetUnityBuildTarget();
        EditorUserBuildSettings.SwitchActiveBuildTarget(
            BuildTargetGroup.Unknown,
            target
        );

        // 配置构建选项
        BuildOptions options = BuildOptions.None;
        if (buildType == BuildType.Development)
        {
            options |= BuildOptions.Development;
            options |= BuildOptions.AllowDebugging;
        }
        if (compressAssets && compressionType == 1)
        {
            options |= BuildOptions.CompressWithLz4;
        }
        // LZMA compression is not available as BuildOption, handled differently

        // 获取输出路径
        string outputPath = GetOutputPath();
        AddLog($"输出路径: {outputPath}", "信息");

        buildProgress = 0.5f;

        // 执行构建
        BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(
            new BuildPlayerOptions
            {
                scenes = scenesToBuild.Select(s => s.path).ToArray(),
                locationPathName = outputPath,
                target = target,
                options = options,
                subtarget = (int)StandaloneBuildSubtarget.Player
            }
        );

        buildProgress = 0.8f;

        // 检查构建结果
        if (report.summary.result == BuildResult.Succeeded)
        {
            AddLog($"构建成功! 耗时: {report.summary.totalTime.TotalSeconds:F1}秒", "成功");
            AddLog($"大小: {report.summary.totalSize / (1024f * 1024f):F2} MB", "信息");
        }
        else
        {
            throw new Exception($"构建失败: {report.summary.result}");
        }

        // 记录警告和错误
        foreach (var step in report.steps)
        {
            foreach (var message in step.messages)
            {
                if (message.type == LogType.Warning)
                {
                    AddLog($"警告: {message.content}", "警告");
                }
                else if (message.type == LogType.Error)
                {
                    AddLog($"错误: {message.content}", "错误");
                }
            }
        }

        buildProgress = 1f;
    }

    private UnityEditor.BuildTarget GetUnityBuildTarget()
    {
        switch (buildTarget)
        {
            case PlatformTarget.Windows: return UnityEditor.BuildTarget.StandaloneWindows64;
            case PlatformTarget.macOS: return UnityEditor.BuildTarget.StandaloneOSX;
            case PlatformTarget.Linux: return UnityEditor.BuildTarget.StandaloneLinux64;
            case PlatformTarget.Android: return UnityEditor.BuildTarget.Android;
            case PlatformTarget.iOS: return UnityEditor.BuildTarget.iOS;
            case PlatformTarget.WebGL: return UnityEditor.BuildTarget.WebGL;
            default: return UnityEditor.BuildTarget.StandaloneWindows64;
        }
    }

    private BuildTargetGroup GetBuildTargetGroup()
    {
        switch (buildTarget)
        {
            case PlatformTarget.Windows: return BuildTargetGroup.Standalone;
            case PlatformTarget.macOS: return BuildTargetGroup.Standalone;
            case PlatformTarget.Linux: return BuildTargetGroup.Standalone;
            case PlatformTarget.Android: return BuildTargetGroup.Android;
            case PlatformTarget.iOS: return BuildTargetGroup.iOS;
            case PlatformTarget.WebGL: return BuildTargetGroup.WebGL;
            default: return BuildTargetGroup.Standalone;
        }
    }

    private string GetCompressionName()
    {
        switch (compressionType)
        {
            case 0: return "Default";
            case 1: return "LZ4";
            case 2: return "LZMA";
            default: return "LZ4";
        }
    }

    private string GetOutputPath()
    {
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, outputFolder));
        Directory.CreateDirectory(folder);

        string filename = productName;
        if (buildTarget == PlatformTarget.Windows)
        {
            filename += ".exe";
        }
        else if (buildTarget == PlatformTarget.macOS)
        {
            filename += ".app";
        }
        else if (buildTarget == PlatformTarget.Android)
        {
            filename += ".apk";
        }

        return Path.Combine(folder, filename);
    }

    private PlatformTarget GetCurrentPlatform()
    {
        return GetPlatformFromUnityBuildTarget(EditorUserBuildSettings.activeBuildTarget);
    }

    private static PlatformTarget GetPlatformFromUnityBuildTarget(UnityEditor.BuildTarget unityTarget)
    {
        switch (unityTarget)
        {
            case UnityEditor.BuildTarget.StandaloneWindows64: return PlatformTarget.Windows;
            case UnityEditor.BuildTarget.StandaloneOSX: return PlatformTarget.macOS;
            case UnityEditor.BuildTarget.StandaloneLinux64: return PlatformTarget.Linux;
            case UnityEditor.BuildTarget.Android: return PlatformTarget.Android;
            case UnityEditor.BuildTarget.iOS: return PlatformTarget.iOS;
            case UnityEditor.BuildTarget.WebGL: return PlatformTarget.WebGL;
            default: return PlatformTarget.Windows;
        }
    }

    private void BuildMultiplePlatforms()
    {
        EditorUtility.DisplayDialog("提示", "多平台构建功能开发中...", "确定");
    }

    private void ClearBuildCache()
    {
        AddLog("清理构建缓存...", "信息");

        // 删除 Library/Bee 文件夹
        string beePath = Path.Combine(Directory.GetCurrentDirectory(), "Library", "Bee");
        if (Directory.Exists(beePath))
        {
            try
            {
                Directory.Delete(beePath, true);
                AddLog("已清理 Bee 缓存", "成功");
            }
            catch (Exception e)
            {
                AddLog($"清理 Bee 缓存失败: {e.Message}", "错误");
            }
        }

        AssetDatabase.Refresh();
        AddLog("构建缓存清理完成", "成功");
    }

    private void AddLog(string message, string type = "信息")
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string logMessage = $"[{timestamp}][{type}] {message}";
        buildLogs.Add(logMessage);
        Debug.Log($"[BuildPipeline] {logMessage}");
    }

    private void ClearLogs()
    {
        buildLogs.Clear();
    }

    private void OnDestroy()
    {
        isBuilding = false;
    }
}
