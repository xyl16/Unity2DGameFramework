using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 模块化AssetBundle构建工具
/// 支持公共Bundle、模块Bundle、依赖关系管理和自动排序
/// </summary>
public class AssetBundleModuleBuilder : EditorWindow
{
    [MenuItem("Tools/AssetBundle工具/模块化Bundle构建")]
    public static void ShowWindow()
    {
        GetWindow<AssetBundleModuleBuilder>("模块化Bundle构建");
    }

    public enum BundleCategory
    {
        Common,      // 公共Bundle - 优先级0-4，所有模块依赖
        UI,          // UI Bundle - 优先级5-9
        Audio,       // 音频 Bundle - 优先级10-14
        ModuleChess, // 象棋模块 - 优先级20-29
        ModuleCustom // 自定义模块 - 优先级30+
    }

    /// <summary>
    /// Bundle配置
    /// </summary>
    [System.Serializable]
    public class BundleConfig
    {
        public string name;           // Bundle显示名称
        public string bundleName;     // Bundle文件名
        public string folderPath;     // 资源文件夹路径
        public List<string> assetPatterns; // 资源类型过滤
        public BundleCategory category;
        public int priority;          // 加载优先级(数值越小越先加载)
        public List<string> dependencies; // 依赖的Bundle名称列表

        public BundleConfig(string name, string bundleName, string folderPath, BundleCategory category, int priority = 0)
        {
            this.name = name;
            this.bundleName = bundleName;
            this.folderPath = folderPath;
            this.category = category;
            this.priority = priority;
            this.assetPatterns = new List<string>();
            this.dependencies = new List<string>();
        }
    }

    private List<BundleConfig> bundles = new List<BundleConfig>();
    private string outputPath = "AssetBundles";
    private string version = "1.0.0";
    private Vector2 scrollPosition;
    private bool showSettings = true;
    private bool showBundles = true;

    private void OnEnable()
    {
        InitializeBundles();
    }

    /// <summary>
    /// 初始化Bundle配置（包含依赖关系）
    /// </summary>
    private void InitializeBundles()
    {
        bundles.Clear();

        // 公共纹理 - 优先级0，所有模块都可能依赖
        bundles.Add(new BundleConfig("公共纹理", "common_textures", "Assets/Textures/Common", BundleCategory.Common, 0)
        {
            assetPatterns = new List<string> { "t:Texture2D", "t:Sprite" }
        });

        // 公共材质 - 优先级1
        bundles.Add(new BundleConfig("公共材质", "common_materials", "Assets/Materials/Common", BundleCategory.Common, 1)
        {
            assetPatterns = new List<string> { "t:Material" },
            dependencies = new List<string> { "common_textures" }
        });

        // 公共音频 - 优先级2
        bundles.Add(new BundleConfig("公共音频", "common_audio", "Assets/Audio/Common", BundleCategory.Common, 2)
        {
            assetPatterns = new List<string> { "t:AudioClip" }
        });

        // 公共UI预制 - 优先级3
        bundles.Add(new BundleConfig("公共UI", "common_ui", "Assets/Prefabs/UI/Common", BundleCategory.UI, 3)
        {
            assetPatterns = new List<string> { "t:Prefab" },
            dependencies = new List<string> { "common_textures", "common_materials" }
        });

        // 配置表 - 优先级4
        bundles.Add(new BundleConfig("配置表", "config_tables", "Assets/Configs", BundleCategory.Common, 4)
        {
            assetPatterns = new List<string> { "t:TextAsset" }
        });

        // 象棋资源 - 优先级10
        bundles.Add(new BundleConfig("象棋资源", "chess_resources", "Assets/Chess/Resources", BundleCategory.ModuleChess, 10)
        {
            assetPatterns = new List<string> { "t:Texture2D", "t:Sprite", "t:AudioClip" },
            dependencies = new List<string> { "common_textures", "common_audio" }
        });

        // 象棋预制 - 优先级11
        bundles.Add(new BundleConfig("象棋预制", "chess_prefabs", "Assets/Chess/Prefabs", BundleCategory.ModuleChess, 11)
        {
            assetPatterns = new List<string> { "t:Prefab" },
            dependencies = new List<string> { "chess_resources", "common_ui" }
        });
    }

    private void OnGUI()
    {
        GUILayout.Label("模块化AssetBundle构建工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 设置区域
        showSettings = EditorGUILayout.Foldout(showSettings, "构建设置", true);
        if (showSettings)
        {
            EditorGUI.indentLevel++;
            outputPath = EditorGUILayout.TextField("输出路径:", outputPath);
            version = EditorGUILayout.TextField("版本号:", version);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        // Bundle配置区域
        showBundles = EditorGUILayout.Foldout(showBundles, "Bundle配置", true);
        if (showBundles)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawBundleConfigs();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();
        }

        // 操作按钮
        DrawActionButtons();

        EditorGUILayout.Space();

        // 当前Bundle列表和依赖关系
        DrawCurrentBundles();
    }

    /// <summary>
    /// 绘制Bundle配置
    /// </summary>
    private void DrawBundleConfigs()
    {
        for (int i = 0; i < bundles.Count; i++)
        {
            BundleConfig bundle = bundles[i];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            // Bundle启用状态
            bool isEnabled = EditorGUILayout.ToggleLeft(bundle.name, true, GUILayout.Width(120));

            // 分类和优先级
            string categoryLabel = bundle.category == BundleCategory.Common ? "[公共]" : "[模块]";
            GUILayout.Label(categoryLabel, GUILayout.Width(50));
            bundle.priority = EditorGUILayout.IntField($"优先级:", bundle.priority, GUILayout.Width(80));

            // Bundle名称
            bundle.bundleName = EditorGUILayout.TextField("Bundle:", bundle.bundleName);

            // 文件夹选择
            if (GUILayout.Button("选择文件夹", GUILayout.Width(80)))
            {
                string folder = EditorUtility.OpenFolderPanel($"选择{bundle.name}文件夹", "Assets", "");
                if (!string.IsNullOrEmpty(folder) && folder.Contains(Application.dataPath))
                {
                    bundle.folderPath = "Assets" + folder.Substring(Application.dataPath.Length);
                }
            }

            // 删除Bundle
            if (GUILayout.Button("删除", GUILayout.Width(50)))
            {
                bundles.RemoveAt(i);
                i--;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                continue;
            }

            EditorGUILayout.EndHorizontal();

            // 显示文件夹路径
            EditorGUILayout.LabelField($"文件夹: {bundle.folderPath}", EditorStyles.miniLabel);

            // 依赖关系配置
            if (bundle.dependencies.Count > 0)
            {
                string depStr = string.Join(", ", bundle.dependencies);
                EditorGUILayout.LabelField($"依赖: {depStr}", EditorStyles.miniLabel);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("配置依赖", GUILayout.Width(80)))
            {
                ConfigureDependencies(bundle);
            }
            if (GUILayout.Button("清空依赖", GUILayout.Width(80)))
            {
                bundle.dependencies.Clear();
            }
            EditorGUILayout.EndHorizontal();

            // 资源类型过滤
            EditorGUILayout.LabelField("资源类型:", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            for (int j = 0; j < bundle.assetPatterns.Count; j++)
            {
                string pattern = bundle.assetPatterns[j];
                pattern = EditorGUILayout.TextField(pattern, GUILayout.Width(100));

                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    bundle.assetPatterns.RemoveAt(j);
                    j--;
                    continue;
                }
                bundle.assetPatterns[j] = pattern;
            }
            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                bundle.assetPatterns.Add("t:Texture2D");
            }
            EditorGUILayout.EndHorizontal();

            // 设置当前Bundle的标签
            if (GUILayout.Button("设置该Bundle资源标签"))
            {
                SetBundleLabels(bundle);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        // 添加新Bundle
        if (GUILayout.Button("添加新Bundle"))
        {
            bundles.Add(new BundleConfig("新Bundle", "new_bundle", "Assets", BundleCategory.ModuleCustom));
        }
    }

    /// <summary>
    /// 绘制操作按钮
    /// </summary>
    private void DrawActionButtons()
    {
        EditorGUILayout.LabelField("构建操作", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("设置所有Bundle标签", GUILayout.Height(40)))
        {
            SetAllBundleLabels();
        }

        if (GUILayout.Button("清除所有标签", GUILayout.Height(40)))
        {
            ClearAllLabels();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("构建所有Bundle", GUILayout.Height(40), GUILayout.Width(200)))
        {
            BuildAllBundles();
        }

        if (GUILayout.Button("构建公共Bundle", GUILayout.Height(40), GUILayout.Width(200)))
        {
            BuildCommonBundles();
        }

        if (GUILayout.Button("构建模块Bundle", GUILayout.Height(40), GUILayout.Width(200)))
        {
            BuildModuleBundles();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (GUILayout.Button("生成版本清单", GUILayout.Height(40)))
        {
            GenerateVersionManifest();
        }

        if (GUILayout.Button("复制到StreamingAssets", GUILayout.Height(40)))
        {
            CopyToStreamingAssets();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("查看依赖顺序", GUILayout.Height(30)))
        {
            ShowLoadOrder();
        }

        if (GUILayout.Button("验证依赖关系", GUILayout.Height(30)))
        {
            ValidateDependencies();
        }
    }

    /// <summary>
    /// 绘制当前Bundle列表
    /// </summary>
    private void DrawCurrentBundles()
    {
        EditorGUILayout.LabelField("当前Bundle列表", EditorStyles.boldLabel);

        var bundleNames = AssetDatabase.GetAllAssetBundleNames();
        if (bundleNames.Length == 0)
        {
            EditorGUILayout.HelpBox("没有设置任何Bundle标签", MessageType.Info);
            return;
        }

        EditorGUILayout.HelpBox($"共 {bundleNames.Length} 个Bundle", MessageType.Info);

        foreach (string bundleName in bundleNames)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(bundleName, GUILayout.Width(200));

            var assets = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
            GUILayout.Label($"({assets.Length} 资源)", GUILayout.Width(80));

            if (GUILayout.Button("查看", GUILayout.Width(50)))
            {
                ShowBundleAssets(bundleName);
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 配置依赖关系
    /// </summary>
    private void ConfigureDependencies(BundleConfig bundle)
    {
        var availableBundles = bundles.Where(b => b.bundleName != bundle.bundleName).Select(b => b.bundleName).ToList();
        string[] options = availableBundles.ToArray();

        BundleConfigDependencyWindow.ShowWindow(bundle, options, (selectedDeps) =>
        {
            bundle.dependencies = selectedDeps;
            AssetDatabase.SaveAssets();
        });
    }

    /// <summary>
    /// 设置Bundle标签
    /// </summary>
    private void SetBundleLabels(BundleConfig bundle)
    {
        if (!AssetDatabase.IsValidFolder(bundle.folderPath))
        {
            EditorUtility.DisplayDialog("错误", $"文件夹不存在: {bundle.folderPath}", "确定");
            return;
        }

        int count = 0;
        string[] guids = AssetDatabase.FindAssets("", new[] { bundle.folderPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            if (AssetDatabase.IsValidFolder(path))
                continue;

            var importer = AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                importer.assetBundleName = bundle.bundleName;
                importer.assetBundleVariant = "";
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("完成", $"已设置 {count} 个资源到Bundle: {bundle.bundleName}", "确定");
        Debug.Log($"Bundle [{bundle.name}] 已设置 {count} 个资源");
    }

    /// <summary>
    /// 设置所有Bundle标签
    /// </summary>
    private void SetAllBundleLabels()
    {
        if (!EditorUtility.DisplayDialog("确认", "这将覆盖现有的Bundle标签设置，继续吗?", "确定", "取消"))
        {
            return;
        }

        int total = 0;
        foreach (var bundle in bundles)
        {
            SetBundleLabels(bundle);
            total++;
        }

        EditorUtility.DisplayDialog("完成", $"已设置 {total} 个Bundle的标签", "确定");
    }

    /// <summary>
    /// 清除所有Bundle标签
    /// </summary>
    private void ClearAllLabels()
    {
        if (!EditorUtility.DisplayDialog("确认", "这将清除所有Bundle标签，继续吗?", "确定", "取消"))
        {
            return;
        }

        var bundleNames = AssetDatabase.GetAllAssetBundleNames();
        int count = 0;

        foreach (string bundleName in bundleNames)
        {
            var assets = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
            foreach (string path in assets)
            {
                var importer = AssetImporter.GetAtPath(path);
                if (importer != null)
                {
                    importer.assetBundleName = "";
                    importer.assetBundleVariant = "";
                    count++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("完成", $"已清除 {count} 个资源的Bundle标签", "确定");
    }

    /// <summary>
    /// 构建所有Bundle
    /// </summary>
    private void BuildAllBundles()
    {
        BuildBundlesInternal(false);
    }

    /// <summary>
    /// 构建公共Bundle
    /// </summary>
    private void BuildCommonBundles()
    {
        BuildBundlesInternal(true);
    }

    /// <summary>
    /// 构建模块Bundle
    /// </summary>
    private void BuildModuleBundles()
    {
        // 先构建公共Bundle
        BuildBundlesInternal(true);

        // 再构建模块Bundle
        BuildBundlesInternal(false, true);
    }

    /// <summary>
    /// 内部构建方法
    /// </summary>
    private void BuildBundlesInternal(bool commonOnly, bool modulesOnly = false)
    {
        try
        {
            // 确保输出目录存在
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // 根据平台设置构建路径
            string platformPath = Path.Combine(outputPath, GetPlatformFolder());
            if (!Directory.Exists(platformPath))
            {
                Directory.CreateDirectory(platformPath);
            }

            // 设置构建选项
            BuildAssetBundleOptions options = BuildAssetBundleOptions.ChunkBasedCompression;

            // 构建AB包
            UnityEditor.BuildPipeline.BuildAssetBundles(platformPath, options, EditorUserBuildSettings.activeBuildTarget);

            Debug.Log($"AB包构建完成! 路径: {platformPath}");

            // 自动生成版本清单
            GenerateVersionManifestInternal(platformPath);

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
            string platformPath = Path.Combine(outputPath, GetPlatformFolder());
            GenerateVersionManifestInternal(platformPath);
            EditorUtility.DisplayDialog("生成完成", "版本清单已生成", "确定");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"生成版本清单失败: {e.Message}", "确定");
        }
    }

    /// <summary>
    /// 内部生成版本清单方法
    /// </summary>
    private void GenerateVersionManifestInternal(string platformPath)
    {
        if (!Directory.Exists(platformPath))
        {
            throw new Exception("AB包目录不存在,请先构建AB包!");
        }

        string versionOutputPath = Path.Combine(outputPath, "version.json");

        // 获取所有AB包文件
        string[] bundleFiles = Directory.GetFiles(platformPath, "*")
            .Where(f => !f.EndsWith(".meta") && !f.EndsWith(".manifest"))
            .ToArray();

        if (bundleFiles.Length == 0)
        {
            throw new Exception("未找到AB包文件!");
        }

        // 构建版本信息
        var manifest = new
        {
            version = version,
            updateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            bundles = new List<object>()
        };

        // 获取所有Bundle配置
        Dictionary<string, BundleConfig> bundleDict = bundles.ToDictionary(b => b.bundleName);

        // 处理每个AB包
        foreach (string bundleFile in bundleFiles)
        {
            FileInfo fileInfo = new FileInfo(bundleFile);
            string fileName = Path.GetFileName(bundleFile);
            string hash = CalculateMD5(bundleFile);

            var bundleInfo = new
            {
                fileName = fileName,
                hash = hash,
                size = fileInfo.Length,
                priority = bundleDict.ContainsKey(fileName) ? bundleDict[fileName].priority : 999,
                dependencies = bundleDict.ContainsKey(fileName) ? bundleDict[fileName].dependencies : new List<string>()
            };

            manifest.bundles.Add(bundleInfo);
        }

        // 保存版本文件
        string json = JsonUtility.ToJson(JsonUtility.FromJson<object>(Json.SerializeObject(manifest)), true);
        File.WriteAllText(versionOutputPath, json, Encoding.UTF8);

        Debug.Log($"版本清单生成完成! 路径: {versionOutputPath}");
    }

    /// <summary>
    /// 复制到StreamingAssets
    /// </summary>
    private void CopyToStreamingAssets()
    {
        try
        {
            string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, "AssetBundles");
            string versionPath = Path.Combine(outputPath, "version.json");

            // 创建目标目录
            if (Directory.Exists(streamingAssetsPath))
            {
                Directory.Delete(streamingAssetsPath, true);
            }
            Directory.CreateDirectory(streamingAssetsPath);

            string platformPath = Path.Combine(outputPath, GetPlatformFolder());
            if (!Directory.Exists(platformPath))
            {
                EditorUtility.DisplayDialog("错误", "AB包目录不存在，请先构建AB包!", "确定");
                return;
            }

            // 复制AB包
            CopyDirectory(platformPath, streamingAssetsPath);

            // 复制版本文件
            if (File.Exists(versionPath))
            {
                File.Copy(versionPath, Path.Combine(Application.streamingAssetsPath, "version.json"), true);
            }

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("复制完成", $"已复制到StreamingAssets:\n{streamingAssetsPath}", "确定");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"复制失败: {e.Message}", "确定");
        }
    }

    /// <summary>
    /// 显示加载顺序
    /// </summary>
    private void ShowLoadOrder()
    {
        var loadOrder = GetLoadOrder();
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Bundle加载顺序（按优先级）:\n");

        foreach (var bundleName in loadOrder)
        {
            var bundle = bundles.Find(b => b.bundleName == bundleName);
            if (bundle != null)
            {
                sb.AppendLine($"{bundle.priority}. {bundle.bundleName} ({bundle.name})");
                if (bundle.dependencies.Count > 0)
                {
                    sb.AppendLine($"   依赖: {string.Join(", ", bundle.dependencies)}");
                }
            }
        }

        EditorUtility.DisplayDialog("加载顺序", sb.ToString(), "确定");
    }

    /// <summary>
    /// 获取加载顺序（拓扑排序）
    /// </summary>
    private List<string> GetLoadOrder()
    {
        var result = new List<string>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        var bundleDict = bundles.ToDictionary(b => b.bundleName);

        foreach (var bundle in bundles.OrderBy(b => b.priority))
        {
            if (!visited.Contains(bundle.bundleName))
            {
                VisitBundle(bundle.bundleName, bundleDict, visited, visiting, result);
            }
        }

        return result;
    }

    /// <summary>
    /// 深度优先遍历Bundle（检测循环依赖）
    /// </summary>
    private void VisitBundle(string bundleName, Dictionary<string, BundleConfig> bundleDict,
        HashSet<string> visited, HashSet<string> visiting, List<string> result)
    {
        if (visiting.Contains(bundleName))
        {
            throw new Exception($"检测到循环依赖: {bundleName}");
        }

        if (visited.Contains(bundleName))
        {
            return;
        }

        visiting.Add(bundleName);

        if (bundleDict.ContainsKey(bundleName))
        {
            foreach (var dep in bundleDict[bundleName].dependencies)
            {
                VisitBundle(dep, bundleDict, visited, visiting, result);
            }
        }

        visiting.Remove(bundleName);
        visited.Add(bundleName);
        result.Add(bundleName);
    }

    /// <summary>
    /// 验证依赖关系
    /// </summary>
    private void ValidateDependencies()
    {
        var bundleNames = bundles.Select(b => b.bundleName).ToList();
        var errors = new List<string>();

        foreach (var bundle in bundles)
        {
            foreach (var dep in bundle.dependencies)
            {
                if (!bundleNames.Contains(dep))
                {
                    errors.Add($"Bundle '{bundle.bundleName}' 依赖的Bundle '{dep}' 不存在");
                }
            }
        }

        // 检测循环依赖
        try
        {
            GetLoadOrder();
        }
        catch (Exception e)
        {
            errors.Add(e.Message);
        }

        if (errors.Count > 0)
        {
            string errorMsg = string.Join("\n", errors);
            EditorUtility.DisplayDialog("依赖错误", errorMsg, "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("验证通过", "所有依赖关系正确", "确定");
        }
    }

    /// <summary>
    /// 显示Bundle资源
    /// </summary>
    private void ShowBundleAssets(string bundleName)
    {
        var assets = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
        if (assets.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", $"Bundle {bundleName} 中没有资源", "确定");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Bundle: {bundleName}");
        sb.AppendLine($"资源数量: {assets.Length}");
        sb.AppendLine("\n资源列表:");

        foreach (string path in assets)
        {
            string fileName = Path.GetFileName(path);
            sb.AppendLine($"  - {fileName}");
        }

        EditorUtility.DisplayDialog("Bundle资源", sb.ToString(), "确定");
    }

    /// <summary>
    /// 复制目录
    /// </summary>
    private void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            string destDir = Path.Combine(targetDir, Path.GetFileName(directory));
            CopyDirectory(directory, destDir);
        }
    }

    /// <summary>
    /// 计算MD5
    /// </summary>
    private string CalculateMD5(string filePath)
    {
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            using (var stream = File.OpenRead(filePath))
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
    /// 获取平台文件夹
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
/// Bundle依赖配置窗口
/// </summary>
public class BundleConfigDependencyWindow : EditorWindow
{
    private AssetBundleModuleBuilder.BundleConfig bundle;
    private string[] availableBundles;
    private List<bool> selection;
    private System.Action<List<string>> onConfirm;

    public static void ShowWindow(AssetBundleModuleBuilder.BundleConfig bundle, string[] availableBundles, System.Action<List<string>> onConfirm)
    {
        var window = GetWindow<BundleConfigDependencyWindow>("配置依赖");
        window.bundle = bundle;
        window.availableBundles = availableBundles;
        window.onConfirm = onConfirm;
        window.selection = new List<bool>();
        foreach (string b in availableBundles)
        {
            window.selection.Add(bundle.dependencies.Contains(b));
        }
    }

    private void OnGUI()
    {
        GUILayout.Label($"配置 {bundle.name} 的依赖", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        for (int i = 0; i < availableBundles.Length; i++)
        {
            selection[i] = EditorGUILayout.ToggleLeft(availableBundles[i], selection[i]);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("确定", GUILayout.Height(30)))
        {
            List<string> selectedDeps = new List<string>();
            for (int i = 0; i < availableBundles.Length; i++)
            {
                if (selection[i])
                {
                    selectedDeps.Add(availableBundles[i]);
                }
            }
            onConfirm?.Invoke(selectedDeps);
            Close();
        }
    }
}

/// <summary>
/// JSON序列化辅助类
/// </summary>
public static class Json
{
    public static string SerializeObject(object obj)
    {
        return JsonUtility.ToJson(obj, true);
    }

    public static string SerializeObject<T>(T obj)
    {
        return JsonUtility.ToJson(obj, true);
    }
}
