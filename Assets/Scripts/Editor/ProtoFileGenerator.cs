using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Proto文件生成器 - 根据配置表自动生成Proto定义文件
/// </summary>
public class ProtoFileGenerator : EditorWindow
{
    private string protoOutputFolder = "Assets/Scripts/Network/Protos";
    private string configSourceFolder = "Assets/Configs";
    private string csOutputFolder = "Assets/Scripts/Network/ProtoMessages";
    private string protoPackage = "GameProtos";
    private string csNamespace = "GameNetwork";
    private bool generateCSCode = true;
    private bool generateProtoComments = true;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Proto文件生成器")]
    public static void ShowWindow()
    {
        GetWindow<ProtoFileGenerator>("Proto文件生成器");
    }

    private void OnGUI()
    {
        GUILayout.Label("Proto文件生成器", EditorStyles.boldLabel);
        GUILayout.Label("根据配置表自动生成Proto定义文件和C#代码", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        // 路径设置
        GUILayout.Label("路径设置", EditorStyles.label);
        protoOutputFolder = EditorGUILayout.TextField("Proto文件输出文件夹", protoOutputFolder);
        configSourceFolder = EditorGUILayout.TextField("配置表源文件夹", configSourceFolder);
        csOutputFolder = EditorGUILayout.TextField("C#代码输出文件夹", csOutputFolder);

        if (GUILayout.Button("选择Proto输出文件夹"))
        {
            string path = EditorUtility.OpenFolderPanel("选择Proto输出文件夹", protoOutputFolder, "");
            if (!string.IsNullOrEmpty(path))
            {
                protoOutputFolder = path;
            }
        }

        if (GUILayout.Button("选择配置表源文件夹"))
        {
            string path = EditorUtility.OpenFolderPanel("选择配置表源文件夹", configSourceFolder, "");
            if (!string.IsNullOrEmpty(path))
            {
                configSourceFolder = path;
            }
        }

        if (GUILayout.Button("选择C#输出文件夹"))
        {
            string path = EditorUtility.OpenFolderPanel("选择C#输出文件夹", csOutputFolder, "");
            if (!string.IsNullOrEmpty(path))
            {
                csOutputFolder = path;
            }
        }

        EditorGUILayout.Space();

        // 生成选项
        GUILayout.Label("生成选项", EditorStyles.label);
        protoPackage = EditorGUILayout.TextField("Proto包名", protoPackage);
        csNamespace = EditorGUILayout.TextField("C#命名空间", csNamespace);
        generateCSCode = EditorGUILayout.Toggle("生成C#代码", generateCSCode);
        generateProtoComments = EditorGUILayout.Toggle("生成Proto注释", generateProtoComments);

        EditorGUILayout.Space();
        EditorGUILayout.Separator();

        // 操作按钮
        GUILayout.Label("操作", EditorStyles.label);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("生成所有Proto文件", GUILayout.Height(40)))
        {
            GenerateAllProtoFiles();
        }
        if (GUILayout.Button("生成消息Proto文件", GUILayout.Height(40)))
        {
            GenerateMessageProtoFile();
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("生成示例消息"))
        {
            GenerateExampleMessages();
        }

        if (GUILayout.Button("打开Proto文件夹"))
        {
            Directory.CreateDirectory(protoOutputFolder);
            EditorUtility.RevealInFinder(protoOutputFolder);
        }

        if (GUILayout.Button("打开C#文件夹"))
        {
            Directory.CreateDirectory(csOutputFolder);
            EditorUtility.RevealInFinder(csOutputFolder);
        }

        EditorGUILayout.Space();

        // 预览区域
        GUILayout.Label("生成的Proto文件预览", EditorStyles.label);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        if (Directory.Exists(protoOutputFolder))
        {
            string[] protoFiles = Directory.GetFiles(protoOutputFolder, "*.proto");
            
            if (protoFiles.Length == 0)
            {
                EditorGUILayout.HelpBox("暂无生成的Proto文件", MessageType.Info);
            }
            else
            {
                foreach (string protoFile in protoFiles)
                {
                    string fileName = Path.GetFileName(protoFile);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(fileName, EditorStyles.boldLabel);
                    
                    if (GUILayout.Button("查看", GUILayout.Width(50)))
                    {
                        ViewProtoFile(protoFile);
                    }
                    
                    if (GUILayout.Button("删除", GUILayout.Width(50)))
                    {
                        File.Delete(protoFile);
                        AssetDatabase.Refresh();
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.LabelField($"路径: {protoFile}", EditorStyles.miniLabel);
                    EditorGUILayout.Space();
                }
            }
        }
        
        EditorGUILayout.EndScrollView();
    }

    private void GenerateAllProtoFiles()
    {
        Directory.CreateDirectory(protoOutputFolder);
        
        if (generateCSCode)
        {
            Directory.CreateDirectory(csOutputFolder);
        }

        int protoCount = 0;

        // 从配置表生成Proto
        if (Directory.Exists(configSourceFolder))
        {
            string[] csvFiles = Directory.GetFiles(configSourceFolder, "*.csv");
            
            foreach (string csvFile in csvFiles)
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(csvFile);
                    string protoContent = GenerateProtoFromConfig(csvFile, fileName);
                    string protoPath = Path.Combine(protoOutputFolder, fileName + ".proto");
                    
                    File.WriteAllText(protoPath, protoContent, Encoding.UTF8);
                    
                    if (generateCSCode)
                    {
                        string csContent = GenerateCSFromConfig(csvFile, fileName);
                        string csPath = Path.Combine(csOutputFolder, fileName + "Proto.cs");
                        File.WriteAllText(csPath, csContent, Encoding.UTF8);
                    }
                    
                    protoCount++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"生成Proto失败: {csvFile}\n{e.Message}");
                }
            }
        }

        AssetDatabase.Refresh();

        string message = $"生成完成!\nProto文件: {protoCount}";
        EditorUtility.DisplayDialog("生成结果", message, "确定");
        Debug.Log(message);
    }

    private void GenerateMessageProtoFile()
    {
        Directory.CreateDirectory(protoOutputFolder);
        
        if (generateCSCode)
        {
            Directory.CreateDirectory(csOutputFolder);
        }

        string protoContent = GenerateGameMessageProto();
        string protoPath = Path.Combine(protoOutputFolder, "GameMessages.proto");
        File.WriteAllText(protoPath, protoContent, Encoding.UTF8);

        if (generateCSCode)
        {
            string csContent = GenerateGameMessageCS();
            string csPath = Path.Combine(csOutputFolder, "GameMessages.cs");
            File.WriteAllText(csPath, csContent, Encoding.UTF8);
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("成功", "消息Proto文件已生成", "确定");
        Debug.Log($"Generated: {protoPath}");
    }

    private void GenerateExampleMessages()
    {
        Directory.CreateDirectory(protoOutputFolder);
        
        if (generateCSCode)
        {
            Directory.CreateDirectory(csOutputFolder);
        }

        // 示例配置Proto
        string itemProto = @"
syntax = ""proto3"";
package GameProtos;

// 道具配置消息
message ItemConfig {
    int32 id = 1;           // 道具ID
    string name = 2;        // 道具名称
    int32 type = 3;         // 道具类型
    int32 rarity = 4;       // 稀有度
    string base_attr = 5;   // 基础属性JSON
    string description = 6; // 描述
    int32 price = 7;        // 价格
    int32 stack_limit = 8;  // 堆叠上限
}

// 道具列表配置
message ItemConfigs {
    repeated ItemConfig items = 1;
}
".TrimStart();

        File.WriteAllText(Path.Combine(protoOutputFolder, "ItemConfig.proto"), itemProto, Encoding.UTF8);

        // 示例C#代码
        string itemCS = @"
using System;
using System.Collections.Generic;

namespace GameNetwork
{
    [Serializable]
    public class ItemConfig
    {
        public int id;
        public string name;
        public int type;
        public int rarity;
        public string baseAttr;
        public string description;
        public int price;
        public int stackLimit;
    }

    [Serializable]
    public class ItemConfigs
    {
        public List<ItemConfig> items = new List<ItemConfig>();
    }
}
".TrimStart();

        File.WriteAllText(Path.Combine(csOutputFolder, "ItemConfig.cs"), itemCS, Encoding.UTF8);

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("成功", "示例消息已生成", "确定");
    }

    private string GenerateProtoFromConfig(string csvPath, string fileName)
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine(@"syntax = ""proto3"";");
        sb.AppendLine($"package {protoPackage};");
        
        if (generateProtoComments)
        {
            sb.AppendLine($"// Auto-generated from: {Path.GetFileName(csvPath)}");
            sb.AppendLine($"// Generated at: {DateTime.Now}");
            sb.AppendLine();
        }

        // 读取CSV字段定义
        string[] lines = File.ReadAllLines(csvPath, Encoding.UTF8);
        var fields = ParseConfigFields(lines);

        string messageName = fileName;
        sb.AppendLine($"message {messageName} {{");

        for (int i = 0; i < fields.Count; i++)
        {
            string protoType = GetProtoType(fields[i].Type);
            sb.AppendLine($"    {protoType} {fields[i].Name} = {i + 1}; // {fields[i].Description}");
        }

        sb.AppendLine("}");
        
        // 生成列表消息
        sb.AppendLine();
        sb.AppendLine($"message {messageName}List {{");
        sb.AppendLine($"    repeated {messageName} items = 1;");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GenerateCSFromConfig(string csvPath, string fileName)
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine($"namespace {csNamespace}");
        sb.AppendLine("{");
        sb.AppendLine($"    [Serializable]");
        sb.AppendLine($"    public class {fileName}");
        sb.AppendLine("    {");

        // 读取CSV字段定义
        string[] lines = File.ReadAllLines(csvPath, Encoding.UTF8);
        var fields = ParseConfigFields(lines);

        foreach (var field in fields)
        {
            string csType = GetCSType(field.Type);
            sb.AppendLine($"        public {csType} {CapitalizeFirstLetter(field.Name)}; // {field.Description}");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    [Serializable]");
        sb.AppendLine($"    public class {fileName}List");
        sb.AppendLine("    {");
        sb.AppendLine($"        public List<{fileName}> items = new List<{fileName}>();");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GenerateGameMessageProto()
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine(@"syntax = ""proto3"";");
        sb.AppendLine($"package {protoPackage};");
        
        if (generateProtoComments)
        {
            sb.AppendLine("// Game Network Messages");
            sb.AppendLine($"// Generated at: {DateTime.Now}");
            sb.AppendLine();
        }

        sb.AppendLine("// 登录请求");
        sb.AppendLine("message LoginRequest {");
        sb.AppendLine("    string username = 1;");
        sb.AppendLine("    string password = 2;");
        sb.AppendLine("    string device_id = 3;");
        sb.AppendLine("}");
        
        sb.AppendLine();
        
        sb.AppendLine("// 登录响应");
        sb.AppendLine("message LoginResponse {");
        sb.AppendLine("    int32 result = 1;");
        sb.AppendLine("    string message = 2;");
        sb.AppendLine("    string token = 3;");
        sb.AppendLine("    int64 player_id = 4;");
        sb.AppendLine("}");
        
        sb.AppendLine();
        
        sb.AppendLine("// 游戏进入请求");
        sb.AppendLine("message EnterGameRequest {");
        sb.AppendLine("    string token = 1;");
        sb.AppendLine("}");
        
        sb.AppendLine();
        
        sb.AppendLine("// 游戏进入响应");
        sb.AppendLine("message EnterGameResponse {");
        sb.AppendLine("    int32 result = 1;");
        sb.AppendLine("    string message = 2;");
        sb.AppendLine("    PlayerData player_data = 3;");
        sb.AppendLine("}");
        
        sb.AppendLine();
        
        sb.AppendLine("// 玩家数据");
        sb.AppendLine("message PlayerData {");
        sb.AppendLine("    int64 player_id = 1;");
        sb.AppendLine("    string nickname = 2;");
        sb.AppendLine("    int32 level = 3;");
        sb.AppendLine("    int64 exp = 4;");
        sb.AppendLine("    int32 gold = 5;");
        sb.AppendLine("    repeated ItemData items = 6;");
        sb.AppendLine("}");
        
        sb.AppendLine();
        
        sb.AppendLine("// 道具数据");
        sb.AppendLine("message ItemData {");
        sb.AppendLine("    int32 item_id = 1;");
        sb.AppendLine("    int32 count = 2;");
        sb.AppendLine("}");
        
        sb.AppendLine();
        
        sb.AppendLine("// 心跳包");
        sb.AppendLine("message Heartbeat {");
        sb.AppendLine("    int64 timestamp = 1;");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GenerateGameMessageCS()
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine($"namespace {csNamespace}");
        sb.AppendLine("{");
        sb.AppendLine();
        sb.AppendLine("    [Serializable]");
        sb.AppendLine("    public class LoginRequest");
        sb.AppendLine("    {");
        sb.AppendLine("        public string username;");
        sb.AppendLine("        public string password;");
        sb.AppendLine("        public string deviceId;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [Serializable]");
        sb.AppendLine("    public class LoginResponse");
        sb.AppendLine("    {");
        sb.AppendLine("        public int result;");
        sb.AppendLine("        public string message;");
        sb.AppendLine("        public string token;");
        sb.AppendLine("        public long playerId;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [Serializable]");
        sb.AppendLine("    public class EnterGameRequest");
        sb.AppendLine("    {");
        sb.AppendLine("        public string token;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [Serializable]");
        sb.AppendLine("    public class EnterGameResponse");
        sb.AppendLine("    {");
        sb.AppendLine("        public int result;");
        sb.AppendLine("        public string message;");
        sb.AppendLine("        public PlayerData playerData;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [Serializable]");
        sb.AppendLine("    public class PlayerData");
        sb.AppendLine("    {");
        sb.AppendLine("        public long playerId;");
        sb.AppendLine("        public string nickname;");
        sb.AppendLine("        public int level;");
        sb.AppendLine("        public long exp;");
        sb.AppendLine("        public int gold;");
        sb.AppendLine("        public List<ItemData> items = new List<ItemData>();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [Serializable]");
        sb.AppendLine("    public class ItemData");
        sb.AppendLine("    {");
        sb.AppendLine("        public int itemId;");
        sb.AppendLine("        public int count;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [Serializable]");
        sb.AppendLine("    public class Heartbeat");
        sb.AppendLine("    {");
        sb.AppendLine("        public long timestamp;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private List<ConfigField> ParseConfigFields(string[] lines)
    {
        var fields = new List<ConfigField>();
        int headerLine = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("字段说明"))
            {
                headerLine = i;
                break;
            }
        }

        if (headerLine < 0) return fields;

        string[] headerFields = lines[headerLine].Split(',');
        string[] typeFields = lines[headerLine + 1].Split(',');

        for (int i = 0; i < headerFields.Length; i++)
        {
            if (i * 2 + 1 < typeFields.Length)
            {
                fields.Add(new ConfigField
                {
                    Description = headerFields[i].Trim(),
                    Name = typeFields[i * 2].Trim(),
                    Type = typeFields[i * 2 + 1].Trim()
                });
            }
        }

        return fields;
    }

    private string GetProtoType(string configType)
    {
        switch (configType.ToLower())
        {
            case "int":
                return "int32";
            case "long":
                return "int64";
            case "float":
                return "float";
            case "double":
                return "double";
            case "bool":
                return "bool";
            case "string":
            default:
                return "string";
        }
    }

    private string GetCSType(string configType)
    {
        switch (configType.ToLower())
        {
            case "int":
                return "int";
            case "long":
                return "long";
            case "float":
                return "float";
            case "double":
                return "double";
            case "bool":
                return "bool";
            case "string":
            default:
                return "string";
        }
    }

    private string CapitalizeFirstLetter(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpper(input[0]) + input.Substring(1);
    }

    private void ViewProtoFile(string protoPath)
    {
        string content = File.ReadAllText(protoPath, Encoding.UTF8);
        ProtoViewerWindow.ShowWindow(Path.GetFileName(protoPath), content);
    }

    private class ConfigField
    {
        public string Description;
        public string Name;
        public string Type;
    }
}

/// <summary>
/// Proto文件查看器窗口
/// </summary>
public class ProtoViewerWindow : EditorWindow
{
    private string protoContent;
    private Vector2 scrollPosition;

    public static void ShowWindow(string title, string content)
    {
        ProtoViewerWindow window = GetWindow<ProtoViewerWindow>(title);
        window.protoContent = content;
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.TextArea(protoContent, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }
}
