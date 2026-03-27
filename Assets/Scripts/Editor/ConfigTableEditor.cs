using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 配置表编辑器 - 用于配置文件管理
/// </summary>
public class ConfigTableEditor : EditorWindow
{
    private string configName = "";
    private string csvPath = "";
    private Vector2 scrollPosition;

    [MenuItem("Tools/配置表管理器")]
    public static void ShowWindow()
    {
        GetWindow<ConfigTableEditor>("配置表管理器");
    }

    private void OnGUI()
    {
        GUILayout.Label("配置表管理器", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 配置表信息
        GUILayout.Label("加载配置表", EditorStyles.label);
        configName = EditorGUILayout.TextField("配置名称", configName);
        csvPath = EditorGUILayout.TextField("CSV路径", csvPath);

        EditorGUILayout.Space();

        if (GUILayout.Button("从CSV文件加载"))
        {
            LoadConfigFromCSV();
        }

        if (GUILayout.Button("选择CSV文件"))
        {
            SelectCSVFile();
        }

        EditorGUILayout.Space();
        EditorGUILayout.Separator();

        // 配置表列表
        GUILayout.Label("已加载的配置表", EditorStyles.label);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        var configPath = Application.persistentDataPath + "/Config/";
        if (Directory.Exists(configPath))
        {
            string[] files = Directory.GetFiles(configPath, "*.csv");
            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(fileName);
                if (GUILayout.Button("查看", GUILayout.Width(60)))
                {
                    ViewConfigFile(file);
                }
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    DeleteConfigFile(file);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // 工具按钮
        GUILayout.Label("工具", EditorStyles.label);
        if (GUILayout.Button("创建示例配置"))
        {
            CreateExampleConfig();
        }

        if (GUILayout.Button("打开配置目录"))
        {
            OpenConfigDirectory();
        }
    }

    private void LoadConfigFromCSV()
    {
        if (string.IsNullOrEmpty(configName) || string.IsNullOrEmpty(csvPath))
        {
            EditorUtility.DisplayDialog("错误", "请输入配置名称和CSV路径", "确定");
            return;
        }

        if (!File.Exists(csvPath))
        {
            EditorUtility.DisplayDialog("错误", "CSV文件不存在", "确定");
            return;
        }

        try
        {
            string destPath = Application.persistentDataPath + "/Config/" + configName + ".csv";
            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
            File.Copy(csvPath, destPath, true);

            EditorUtility.DisplayDialog("成功", $"配置表 {configName} 加载成功", "确定");
            Debug.Log($"Config loaded: {configName} from {csvPath}");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"加载失败: {e.Message}", "确定");
        }
    }

    private void SelectCSVFile()
    {
        string path = EditorUtility.OpenFilePanel("选择CSV文件", "", "csv");
        if (!string.IsNullOrEmpty(path))
        {
            csvPath = path;
            configName = Path.GetFileNameWithoutExtension(path);
        }
    }

    private void ViewConfigFile(string filePath)
    {
        string content = File.ReadAllText(filePath);
        ConfigViewerWindow.ShowWindow(Path.GetFileNameWithoutExtension(filePath), content);
    }

    private void DeleteConfigFile(string filePath)
    {
        if (EditorUtility.DisplayDialog("确认", "确定要删除这个配置表吗？", "删除", "取消"))
        {
            File.Delete(filePath);
            Debug.Log($"Config deleted: {filePath}");
        }
    }

    private void CreateExampleConfig()
    {
        string exampleCSV = CreateExampleItemConfig();
        string path = EditorUtility.SaveFilePanel("保存示例配置", "", "ItemConfig", "csv");

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, exampleCSV, Encoding.UTF8);
            EditorUtility.DisplayDialog("成功", "示例配置已创建", "确定");
            Debug.Log($"Example config created: {path}");
        }
    }

    private void OpenConfigDirectory()
    {
        string path = Application.persistentDataPath + "/Config/";
        Directory.CreateDirectory(path);
        EditorUtility.RevealInFinder(path);
    }

    private string CreateExampleItemConfig()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("字段说明,字段名,字段类型");
        sb.AppendLine("道具ID,id,int");
        sb.AppendLine("道具名称,name,string");
        sb.AppendLine("道具类型,type,int");
        sb.AppendLine("稀有度,rarity,int");
        sb.AppendLine("基础属性,baseAttr,string");
        sb.AppendLine("描述,description,string");
        sb.AppendLine("价格,price,int");
        sb.AppendLine("堆叠上限,stackLimit,int");
        sb.AppendLine();
        sb.AppendLine("1,新手剑,1,1,\"{attack:10,defense:5}\",一把锋利的剑,100,1");
        sb.AppendLine("2,铁剑,1,2,\"{attack:20,defense:8}\",坚固的铁剑,500,1");
        sb.AppendLine("3,金剑,1,3,\"{attack:35,defense:12}\",华丽的金剑,2000,1");
        sb.AppendLine("1001,生命药水,2,1,\"{hpRestore:50}\",恢复50点生命值,50,99");
        sb.AppendLine("1002,魔法药水,2,2,\"{mpRestore:100}\",恢复100点魔法值,80,99");
        return sb.ToString();
    }
}

/// <summary>
/// 配置查看器窗口
/// </summary>
public class ConfigViewerWindow : EditorWindow
{
    private string configContent;
    private Vector2 scrollPosition;

    public static void ShowWindow(string title, string content)
    {
        ConfigViewerWindow window = GetWindow<ConfigViewerWindow>(title);
        window.configContent = content;
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.TextArea(configContent, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }
}
