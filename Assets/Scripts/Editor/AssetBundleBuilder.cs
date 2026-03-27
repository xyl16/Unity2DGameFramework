using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// AB包构建编辑器工具
/// 负责构建AB包、生成版本清单、生成哈希值
/// </summary>
public class AssetBundleBuilder : EditorWindow
{
    [MenuItem("Tools/AssetBundle工具/AssetBundle构建工具")]
    public static void ShowWindow()
    {
        GetWindow<AssetBundleBuilder>("AB包构建工具");
    }

    private string bundleOutputPath = "AssetBundles";
    private string versionOutputPath = "AssetBundles/version.json";
    private BuildAssetBundleOptions buildOptions = BuildAssetBundleOptions.None;
    private BuildTarget buildTarget = BuildTarget.StandaloneWindows;
    private string version = "1.0.0";
    private List<string> selectedBundles = new List<string>();
    private Vector2 scrollPosition;

    private void OnGUI()
    {
        GUILayout.Label("AB包构建工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 构建设置
        EditorGUILayout.LabelField("构建设置", EditorStyles.boldLabel);
        bundleOutputPath = EditorGUILayout.TextField("输出路径:", bundleOutputPath);
        versionOutputPath = EditorGUILayout.TextField("版本文件路径:", versionOutputPath);
        version = EditorGUILayout.TextField("版本号:", version);
        buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("构建目标:", buildTarget);
        buildOptions = (BuildAssetBundleOptions)EditorGUILayout.EnumFlagsField("构建选项:", buildOptions);

        EditorGUILayout.Space();

        // 构建按钮
        if (GUILayout.Button("构建AB包", GUILayout.Height(40)))
        {
            BuildAssetBundles();
        }

        if (GUILayout.Button("构建并生成版本清单", GUILayout.Height(40)))
        {
            BuildAssetBundles();
            GenerateVersionManifest();
        }

        EditorGUILayout.Space();

        // 版本管理
        EditorGUILayout.LabelField("版本管理", EditorStyles.boldLabel);

        if (GUILayout.Button("生成版本清单"))
        {
            GenerateVersionManifest();
        }

        if (GUILayout.Button("查看当前版本清单"))
        {
            ViewVersionManifest();
        }

        if (GUILayout.Button("验证AB包"))
        {
            VerifyAssetBundles();
        }

        if (GUILayout.Button("清理构建缓存"))
        {
            ClearBuildCache();
        }

        EditorGUILayout.Space();

        // AB包列表
        EditorGUILayout.LabelField("AB包列表", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DisplayBundleList();
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 构建AB包
    /// </summary>
    private void BuildAssetBundles()
    {
        try
        {
            // 确保输出目录存在
            if (!Directory.Exists(bundleOutputPath))
            {
                Directory.CreateDirectory(bundleOutputPath);
            }

            // 根据平台设置构建路径
            string platformPath = Path.Combine(bundleOutputPath, GetPlatformFolder());
            if (!Directory.Exists(platformPath))
            {
                Directory.CreateDirectory(platformPath);
            }

            // 构建AB包
            UnityEditor.BuildPipeline.BuildAssetBundles(
                platformPath,
                buildOptions,
                EditorUserBuildSettings.activeBuildTarget
            );

            Debug.Log($"AB包构建完成! 路径: {platformPath}");
            EditorUtility.DisplayDialog("构建完成", $"AB包已成功构建到:\n{platformPath}", "确定");
        }
        catch (Exception e)
        {
            Debug.LogError($"AB包构建失败: {e.Message}");
            EditorUtility.DisplayDialog("构建失败", $"构建AB包时出错:\n{e.Message}", "确定");
        }
    }

    /// <summary>
    /// 生成版本清单
    /// </summary>
    private void GenerateVersionManifest()
    {
        try
        {
            string platformPath = Path.Combine(bundleOutputPath, GetPlatformFolder());
            if (!Directory.Exists(platformPath))
            {
                EditorUtility.DisplayDialog("错误", "AB包目录不存在,请先构建AB包!", "确定");
                return;
            }

            // 获取所有AB包文件
            string[] bundleFiles = Directory.GetFiles(platformPath, "*")
                .Where(f => !f.EndsWith(".meta") && !f.EndsWith(".manifest"))
                .ToArray();

            if (bundleFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("错误", "未找到AB包文件!", "确定");
                return;
            }

            // 构建版本信息
            AssetBundleUpdater.VersionManifest manifest = new AssetBundleUpdater.VersionManifest
            {
                version = version,
                updateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                files = new List<AssetBundleUpdater.BundleFile>()
            };

            // 处理每个AB包
            foreach (string bundleFile in bundleFiles)
            {
                FileInfo fileInfo = new FileInfo(bundleFile);
                string fileName = Path.GetFileName(bundleFile);
                string hash = CalculateMD5(bundleFile);

                manifest.files.Add(new AssetBundleUpdater.BundleFile
                {
                    fileName = fileName,
                    hash = hash,
                    size = fileInfo.Length,
                    downloadUrl = fileName
                });

                Debug.Log($"AB包: {fileName}, 大小: {fileInfo.Length} bytes, Hash: {hash}");
            }

            // 保存版本文件
            string json = JsonUtility.ToJson(manifest, true);
            File.WriteAllText(versionOutputPath, json, Encoding.UTF8);

            Debug.Log($"版本清单生成完成! 路径: {versionOutputPath}");
            EditorUtility.DisplayDialog("生成完成", $"版本清单已生成:\n{versionOutputPath}\n\n包含 {manifest.files.Count} 个文件", "确定");

            // 刷新资源数据库
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            Debug.LogError($"生成版本清单失败: {e.Message}");
            EditorUtility.DisplayDialog("生成失败", $"生成版本清单时出错:\n{e.Message}", "确定");
        }
    }

    /// <summary>
    /// 查看版本清单
    /// </summary>
    private void ViewVersionManifest()
    {
        if (!File.Exists(versionOutputPath))
        {
            EditorUtility.DisplayDialog("提示", "版本清单文件不存在!", "确定");
            return;
        }

        try
        {
            string json = File.ReadAllText(versionOutputPath);
            AssetBundleUpdater.VersionManifest manifest = JsonUtility.FromJson<AssetBundleUpdater.VersionManifest>(json);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"版本: {manifest.version}");
            sb.AppendLine($"更新时间: {DateTimeOffset.FromUnixTimeSeconds(manifest.updateTime).ToLocalTime()}");
            sb.AppendLine($"文件数量: {manifest.files.Count}");
            sb.AppendLine("\n文件列表:");

            foreach (var file in manifest.files)
            {
                sb.AppendLine($"  - {file.fileName}");
                sb.AppendLine($"    大小: {FormatSize(file.size)}");
                sb.AppendLine($"    Hash: {file.hash}");
            }

            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog("版本清单", sb.ToString(), "确定");
        }
        catch (Exception e)
        {
            Debug.LogError($"读取版本清单失败: {e.Message}");
            EditorUtility.DisplayDialog("错误", $"读取版本清单时出错:\n{e.Message}", "确定");
        }
    }

    /// <summary>
    /// 验证AB包
    /// </summary>
    private void VerifyAssetBundles()
    {
        try
        {
            if (!File.Exists(versionOutputPath))
            {
                EditorUtility.DisplayDialog("错误", "版本清单文件不存在!", "确定");
                return;
            }

            string platformPath = Path.Combine(bundleOutputPath, GetPlatformFolder());
            if (!Directory.Exists(platformPath))
            {
                EditorUtility.DisplayDialog("错误", "AB包目录不存在!", "确定");
                return;
            }

            string json = File.ReadAllText(versionOutputPath);
            AssetBundleUpdater.VersionManifest manifest = JsonUtility.FromJson<AssetBundleUpdater.VersionManifest>(json);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("AB包验证结果:");
            sb.AppendLine();

            bool allValid = true;
            foreach (var file in manifest.files)
            {
                string filePath = Path.Combine(platformPath, file.fileName);
                if (!File.Exists(filePath))
                {
                    sb.AppendLine($"✗ {file.fileName} - 文件不存在");
                    allValid = false;
                    continue;
                }

                string actualHash = CalculateMD5(filePath);
                if (actualHash != file.hash)
                {
                    sb.AppendLine($"✗ {file.fileName} - Hash不匹配");
                    sb.AppendLine($"  预期: {file.hash}");
                    sb.AppendLine($"  实际: {actualHash}");
                    allValid = false;
                }
                else
                {
                    sb.AppendLine($"✓ {file.fileName} - 验证通过");
                }
            }

            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog("验证结果", sb.ToString(), "确定");

            if (allValid)
            {
                Debug.Log("所有AB包验证通过!");
            }
            else
            {
                Debug.LogWarning("部分AB包验证失败!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"验证AB包失败: {e.Message}");
            EditorUtility.DisplayDialog("验证失败", $"验证AB包时出错:\n{e.Message}", "确定");
        }
    }

    /// <summary>
    /// 清理构建缓存
    /// </summary>
    private void ClearBuildCache()
    {
        if (EditorUtility.DisplayDialog("确认", "确定要清理构建缓存吗?这将删除所有已构建的AB包和版本清单。", "确定", "取消"))
        {
            try
            {
                if (Directory.Exists(bundleOutputPath))
                {
                    Directory.Delete(bundleOutputPath, true);
                    Debug.Log($"构建缓存已清理: {bundleOutputPath}");
                }

                if (File.Exists(versionOutputPath))
                {
                    File.Delete(versionOutputPath);
                    Debug.Log($"版本清单已删除: {versionOutputPath}");
                }

                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("完成", "构建缓存已清理!", "确定");
            }
            catch (Exception e)
            {
                Debug.LogError($"清理构建缓存失败: {e.Message}");
                EditorUtility.DisplayDialog("清理失败", $"清理构建缓存时出错:\n{e.Message}", "确定");
            }
        }
    }

    /// <summary>
    /// 显示AB包列表
    /// </summary>
    private void DisplayBundleList()
    {
        string platformPath = Path.Combine(bundleOutputPath, GetPlatformFolder());
        if (!Directory.Exists(platformPath))
        {
            EditorGUILayout.HelpBox("AB包目录不存在,请先构建AB包!", MessageType.Info);
            return;
        }

        string[] bundleFiles = Directory.GetFiles(platformPath, "*")
            .Where(f => !f.EndsWith(".meta") && !f.EndsWith(".manifest"))
            .ToArray();

        if (bundleFiles.Length == 0)
        {
            EditorGUILayout.HelpBox("未找到AB包文件!", MessageType.Info);
            return;
        }

        foreach (string bundleFile in bundleFiles)
        {
            FileInfo fileInfo = new FileInfo(bundleFile);
            string fileName = Path.GetFileName(bundleFile);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(fileName, GUILayout.Width(200));
            GUILayout.Label(FormatSize(fileInfo.Length), GUILayout.Width(100));

            if (GUILayout.Button("查看详情", GUILayout.Width(80)))
            {
                ViewBundleDetails(bundleFile);
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 查看AB包详情
    /// </summary>
    private void ViewBundleDetails(string bundlePath)
    {
        try
        {
            FileInfo fileInfo = new FileInfo(bundlePath);
            string hash = CalculateMD5(bundlePath);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"文件名: {fileInfo.Name}");
            sb.AppendLine($"大小: {FormatSize(fileInfo.Length)}");
            sb.AppendLine($"路径: {fileInfo.FullName}");
            sb.AppendLine($"Hash: {hash}");
            sb.AppendLine($"创建时间: {fileInfo.CreationTime}");

            EditorUtility.DisplayDialog("AB包详情", sb.ToString(), "确定");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"读取AB包详情时出错:\n{e.Message}", "确定");
        }
    }

    /// <summary>
    /// 计算MD5哈希
    /// </summary>
    private string CalculateMD5(string filePath)
    {
        using (MD5 md5 = MD5.Create())
        {
            using (FileStream stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    private string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size = size / 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }

    /// <summary>
    /// 获取平台文件夹名称
    /// </summary>
    private string GetPlatformFolder()
    {
        switch (EditorUserBuildSettings.activeBuildTarget)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return "Windows";
            case BuildTarget.StandaloneOSX:
                return "OSX";
            case BuildTarget.StandaloneLinux64:
                return "Linux";
            case BuildTarget.iOS:
                return "iOS";
            case BuildTarget.Android:
                return "Android";
            case BuildTarget.WebGL:
                return "WebGL";
            default:
                return EditorUserBuildSettings.activeBuildTarget.ToString();
        }
    }
}

/// <summary>
/// AB包自动化构建脚本
/// </summary>
public static class AssetBundleBuildScript
{
    /// <summary>
    /// 命令行构建AB包
    /// </summary>
    public static void BuildAssetBundlesCommandLine()
    {
        string version = Environment.GetEnvironmentVariable("AB_VERSION") ?? "1.0.0";
        string outputPath = Environment.GetEnvironmentVariable("AB_OUTPUT_PATH") ?? "AssetBundles";

        Debug.Log($"开始构建AB包, 版本: {version}, 输出路径: {outputPath}");

        // 确保输出目录存在
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        // 获取平台文件夹
        string platformPath = Path.Combine(outputPath, GetPlatformFolderForBuild());
        if (!Directory.Exists(platformPath))
        {
            Directory.CreateDirectory(platformPath);
        }

        // 构建AB包
        UnityEditor.BuildPipeline.BuildAssetBundles(
            platformPath,
            BuildAssetBundleOptions.None,
            EditorUserBuildSettings.activeBuildTarget
        );

        Debug.Log($"AB包构建完成! 路径: {platformPath}");

        // 生成版本清单
        GenerateVersionManifest(version, platformPath, outputPath);
    }

    /// <summary>
    /// 生成版本清单
    /// </summary>
    private static void GenerateVersionManifest(string version, string platformPath, string outputPath)
    {
        string versionOutputPath = Path.Combine(outputPath, "version.json");

        // 获取所有AB包文件
        string[] bundleFiles = Directory.GetFiles(platformPath, "*")
            .Where(f => !f.EndsWith(".meta") && !f.EndsWith(".manifest"))
            .ToArray();

        // 构建版本信息
        AssetBundleUpdater.VersionManifest manifest = new AssetBundleUpdater.VersionManifest
        {
            version = version,
            updateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            files = new List<AssetBundleUpdater.BundleFile>()
        };

        // 处理每个AB包
        foreach (string bundleFile in bundleFiles)
        {
            FileInfo fileInfo = new FileInfo(bundleFile);
            string fileName = Path.GetFileName(bundleFile);
            string hash = CalculateMD5(bundleFile);

            manifest.files.Add(new AssetBundleUpdater.BundleFile
            {
                fileName = fileName,
                hash = hash,
                size = fileInfo.Length,
                downloadUrl = fileName
            });
        }

        // 保存版本文件
        string json = JsonUtility.ToJson(manifest, true);
        File.WriteAllText(versionOutputPath, json, System.Text.Encoding.UTF8);

        Debug.Log($"版本清单生成完成! 路径: {versionOutputPath}, 包含 {manifest.files.Count} 个文件");
    }

    /// <summary>
    /// 计算MD5哈希
    /// </summary>
    private static string CalculateMD5(string filePath)
    {
        using (MD5 md5 = MD5.Create())
        {
            using (FileStream stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }

    /// <summary>
    /// 获取平台文件夹名称
    /// </summary>
    private static string GetPlatformFolderForBuild()
    {
        switch (EditorUserBuildSettings.activeBuildTarget)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return "Windows";
            case BuildTarget.StandaloneOSX:
                return "OSX";
            case BuildTarget.StandaloneLinux64:
                return "Linux";
            case BuildTarget.iOS:
                return "iOS";
            case BuildTarget.Android:
                return "Android";
            case BuildTarget.WebGL:
                return "WebGL";
            default:
                return EditorUserBuildSettings.activeBuildTarget.ToString();
        }
    }
}
