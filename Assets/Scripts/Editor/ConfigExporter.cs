using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 配置表导出工具 - 将CSV配置表导出为二进制/JSON格式
/// </summary>
public class ConfigExporter : EditorWindow
{
    private enum ExportFormat
    {
        Binary,
        Json,
        Protobuf
    }

    private string sourceFolder = "Assets/Configs";
    private string outputFolder = "Assets/Resources/Configs";
    private ExportFormat exportFormat = ExportFormat.Binary;
    private bool includeVersionCheck = true;
    private bool compressOutput = false;
    private Vector2 scrollPosition;

    [MenuItem("Tools/配置表导出工具")]
    public static void ShowWindow()
    {
        GetWindow<ConfigExporter>("配置表导出工具");
    }

    private void OnGUI()
    {
        GUILayout.Label("配置表导出工具", EditorStyles.boldLabel);
        GUILayout.Label("将CSV配置表导出为二进制/JSON/Protobuf格式", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        // 路径设置
        GUILayout.Label("路径设置", EditorStyles.label);
        sourceFolder = EditorGUILayout.TextField("源文件夹 (CSV)", sourceFolder);
        outputFolder = EditorGUILayout.TextField("输出文件夹", outputFolder);

        if (GUILayout.Button("选择源文件夹"))
        {
            string path = EditorUtility.OpenFolderPanel("选择CSV源文件夹", sourceFolder, "");
            if (!string.IsNullOrEmpty(path))
            {
                sourceFolder = path;
            }
        }

        if (GUILayout.Button("选择输出文件夹"))
        {
            string path = EditorUtility.OpenFolderPanel("选择输出文件夹", outputFolder, "");
            if (!string.IsNullOrEmpty(path))
            {
                outputFolder = path;
            }
        }

        EditorGUILayout.Space();

        // 导出选项
        GUILayout.Label("导出选项", EditorStyles.label);
        exportFormat = (ExportFormat)EditorGUILayout.EnumPopup("导出格式", exportFormat);
        includeVersionCheck = EditorGUILayout.Toggle("包含版本号检查", includeVersionCheck);
        compressOutput = EditorGUILayout.Toggle("压缩输出", compressOutput);

        EditorGUILayout.Space();
        EditorGUILayout.Separator();

        // 操作按钮
        GUILayout.Label("操作", EditorStyles.label);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("导出所有配置表", GUILayout.Height(40)))
        {
            ExportAllConfigs();
        }
        if (GUILayout.Button("导出选中配置表", GUILayout.Height(40)))
        {
            ExportSelectedConfigs();
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("刷新配置列表"))
        {
            AssetDatabase.Refresh();
        }

        if (GUILayout.Button("打开输出文件夹"))
        {
            EditorUtility.RevealInFinder(outputFolder);
        }

        EditorGUILayout.Space();

        // 配置文件列表
        GUILayout.Label("配置文件列表", EditorStyles.label);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (Directory.Exists(sourceFolder))
        {
            string[] csvFiles = Directory.GetFiles(sourceFolder, "*.csv", SearchOption.AllDirectories);
            
            if (csvFiles.Length == 0)
            {
                EditorGUILayout.HelpBox("未找到CSV文件", MessageType.Info);
            }
            else
            {
                var selectedFiles = new List<string>();
                foreach (string file in csvFiles)
                {
                    string relativePath = file.Replace(Application.dataPath, "Assets");
                    bool isSelected = GUILayout.Toggle(
                        selectedFiles.Contains(relativePath),
                        Path.GetFileNameWithoutExtension(file),
                        "Toggle"
                    );

                    if (isSelected && !selectedFiles.Contains(relativePath))
                    {
                        selectedFiles.Add(relativePath);
                    }
                    else if (!isSelected && selectedFiles.Contains(relativePath))
                    {
                        selectedFiles.Remove(relativePath);
                    }

                    EditorGUILayout.LabelField(relativePath, EditorStyles.miniLabel);
                    EditorGUILayout.Space();
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox($"源文件夹不存在: {sourceFolder}", MessageType.Warning);
        }

        EditorGUILayout.EndScrollView();
    }

    private void ExportAllConfigs()
    {
        if (!Directory.Exists(sourceFolder))
        {
            EditorUtility.DisplayDialog("错误", "源文件夹不存在", "确定");
            return;
        }

        string[] csvFiles = Directory.GetFiles(sourceFolder, "*.csv", SearchOption.AllDirectories);
        int successCount = 0;
        int failCount = 0;

        Directory.CreateDirectory(outputFolder);

        foreach (string csvFile in csvFiles)
        {
            try
            {
                ExportConfigFile(csvFile);
                successCount++;
            }
            catch (Exception e)
            {
                Debug.LogError($"导出失败: {csvFile}\n{e.Message}");
                failCount++;
            }
        }

        AssetDatabase.Refresh();

        string message = $"导出完成!\n成功: {successCount}\n失败: {failCount}";
        EditorUtility.DisplayDialog("导出结果", message, "确定");
        Debug.Log(message);
    }

    private void ExportSelectedConfigs()
    {
        if (!Directory.Exists(sourceFolder))
        {
            EditorUtility.DisplayDialog("错误", "源文件夹不存在", "确定");
            return;
        }

        // 在实际使用中，这里应该有一个选中的文件列表
        // 暂时导出所有文件
        ExportAllConfigs();
    }

    private void ExportConfigFile(string csvPath)
    {
        string fileName = Path.GetFileNameWithoutExtension(csvPath);
        string outputPath = Path.Combine(outputFolder, fileName);

        // 读取CSV文件
        var configData = ReadCSV(csvPath);

        switch (exportFormat)
        {
            case ExportFormat.Binary:
                ExportToBinary(configData, outputPath);
                break;
            case ExportFormat.Json:
                ExportToJson(configData, outputPath);
                break;
            case ExportFormat.Protobuf:
                ExportToProtobuf(configData, outputPath);
                break;
        }
    }

    private ConfigData ReadCSV(string csvPath)
    {
        string[] lines = File.ReadAllLines(csvPath, Encoding.UTF8);
        var configData = new ConfigData();

        // 找到字段定义行和分隔线
        int headerLine = -1;
        int dataStartLine = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("字段说明"))
            {
                headerLine = i;
            }
            else if (headerLine >= 0 && string.IsNullOrWhiteSpace(lines[i]))
            {
                dataStartLine = i + 1;
                break;
            }
        }

        if (headerLine < 0 || dataStartLine < 0)
        {
            throw new Exception("CSV格式不正确，缺少字段定义或分隔线");
        }

        // 解析字段定义
        string[] headerFields = lines[headerLine].Split(',');
        string[] typeFields = lines[headerLine + 1].Split(',');

        // CSV格式: 字段描述,字段名,字段类型 (每三个一组)
        for (int i = 0; i < headerFields.Length; i++)
        {
            string description = headerFields[i].Trim();
            string name = i * 2 < typeFields.Length ? typeFields[i * 2].Trim() : "";
            string type = i * 2 + 1 < typeFields.Length ? typeFields[i * 2 + 1].Trim() : "string";

            configData.Fields.Add(new ConfigField
            {
                Description = description,
                Name = name,
                Type = type
            });
        }

        // 解析数据行
        for (int i = dataStartLine; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = ParseCSVLine(lines[i]);
            var rowData = new Dictionary<string, string>();

            for (int j = 0; j < configData.Fields.Count && j < values.Length; j++)
            {
                rowData[configData.Fields[j].Name] = values[j].Trim();
            }

            configData.Rows.Add(rowData);
        }

        return configData;
    }

    private string[] ParseCSVLine(string line)
    {
        var values = new List<string>();
        bool inQuotes = false;
        StringBuilder currentValue = new StringBuilder();

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(currentValue.ToString());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        values.Add(currentValue.ToString());
        return values.ToArray();
    }

    private void ExportToBinary(ConfigData configData, string outputPath)
    {
        using (BinaryWriter writer = new BinaryWriter(File.Create(outputPath + ".bytes")))
        {
            // 写入文件头
            writer.Write("CONFIG"); // 魔数
            writer.Write(1); // 版本号
            writer.Write(includeVersionCheck ? DateTime.Now.Ticks : 0); // 时间戳

            // 写入字段信息
            writer.Write(configData.Fields.Count);
            foreach (var field in configData.Fields)
            {
                writer.Write(field.Name);
                writer.Write(field.Type);
            }

            // 写入数据行
            writer.Write(configData.Rows.Count);
            foreach (var row in configData.Rows)
            {
                foreach (var field in configData.Fields)
                {
                    if (row.TryGetValue(field.Name, out string value))
                    {
                        WriteBinaryValue(writer, field.Type, value);
                    }
                    else
                    {
                        WriteBinaryValue(writer, field.Type, "");
                    }
                }
            }
        }

        Debug.Log($"导出二进制配置: {outputPath}.bytes");
    }

    private void WriteBinaryValue(BinaryWriter writer, string type, string value)
    {
        switch (type.ToLower())
        {
            case "int":
                writer.Write(int.TryParse(value, out int intVal) ? intVal : 0);
                break;
            case "float":
                writer.Write(float.TryParse(value, out float floatVal) ? floatVal : 0f);
                break;
            case "bool":
                writer.Write(bool.TryParse(value, out bool boolVal) && boolVal);
                break;
            case "string":
            default:
                writer.Write(value ?? "");
                break;
        }
    }

    private void ExportToJson(ConfigData configData, string outputPath)
    {
        var json = JsonUtility.ToJson(configData, true);
        File.WriteAllText(outputPath + ".json", json, Encoding.UTF8);
        Debug.Log($"导出JSON配置: {outputPath}.json");
    }

    private void ExportToProtobuf(ConfigData configData, string outputPath)
    {
        // 简化的Protobuf导出，实际项目中应使用protobuf-net
        using (MemoryStream ms = new MemoryStream())
        {
            BinaryWriter writer = new BinaryWriter(ms);
            
            // 写入字段数量
            writer.Write(configData.Fields.Count);
            
            // 写入字段定义
            foreach (var field in configData.Fields)
            {
                writer.Write(field.Name);
                writer.Write(field.Type);
            }
            
            // 写入数据行
            writer.Write(configData.Rows.Count);
            foreach (var row in configData.Rows)
            {
                foreach (var field in configData.Fields)
                {
                    row.TryGetValue(field.Name, out string value);
                    writer.Write(value ?? "");
                }
            }
            
            File.WriteAllBytes(outputPath + ".pb", ms.ToArray());
        }
        
        Debug.Log($"导出Protobuf配置: {outputPath}.pb");
    }

    private class ConfigData
    {
        public List<ConfigField> Fields = new List<ConfigField>();
        public List<Dictionary<string, string>> Rows = new List<Dictionary<string, string>>();
    }

    private class ConfigField
    {
        public string Description;
        public string Name;
        public string Type;
    }
}
