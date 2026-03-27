using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 配置表管理器 - 支持Excel/CSV配置文件解析
/// </summary>
public class ConfigTableManager : MonoBehaviour
{
    private static ConfigTableManager instance;
    public static ConfigTableManager Instance { get { return instance; } }

    private Dictionary<Type, object> configCache = new Dictionary<Type, object>();
    private Dictionary<string, Dictionary<int, Dictionary<string, string>>> rawConfigData = new Dictionary<string, Dictionary<int, Dictionary<string, string>>>();

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

    /// <summary>
    /// 从CSV文件加载配置
    /// </summary>
    public void LoadConfigFromCSV(string configName, string csvPath)
    {
        try
        {
            string csvText = File.ReadAllText(csvPath, Encoding.UTF8);
            ParseCSV(configName, csvText);
            Logger.Instance.LogInfo($"Config {configName} loaded from CSV", "ConfigTable");
        }
        catch (Exception e)
        {
            Logger.Instance.LogError($"Failed to load CSV config {configName}: {e.Message}", "ConfigTable");
        }
    }

    /// <summary>
    /// 从AssetBundle加载CSV配置
    /// </summary>
    public void LoadConfigFromAB(string configName, string assetName)
    {
        StartCoroutine(LoadConfigFromABCoroutine(configName, assetName));
    }

    private System.Collections.IEnumerator LoadConfigFromABCoroutine(string configName, string assetName)
    {
        string path = $"Config/{assetName}";
        bool loading = true;
        TextAsset loadedAsset = null;

        ResourceManager.Instance.LoadAssetAsync<TextAsset>(path, (asset) =>
        {
            loadedAsset = asset;
            loading = false;
        });

        while (loading)
        {
            yield return null;
        }

        if (loadedAsset != null)
        {
            ParseCSV(configName, loadedAsset.text);
            Logger.Instance.LogInfo($"Config {configName} loaded from AB", "ConfigTable");
        }
        else
        {
            Logger.Instance.LogError($"Failed to load config {configName} from AB", "ConfigTable");
        }
    }

    /// <summary>
    /// 解析CSV格式
    /// </summary>
    private void ParseCSV(string configName, string csvText)
    {
        string[] lines = csvText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        if (lines.Length < 3)
        {
            Logger.Instance.LogWarning($"CSV file too short: {configName}", "ConfigTable");
            return;
        }

        // 第一行：字段说明
        string[] fieldDescriptions = ParseCSVLine(lines[0]);

        // 第二行：字段名
        string[] fieldNames = ParseCSVLine(lines[1]);

        // 第三行：字段类型
        string[] fieldTypes = ParseCSVLine(lines[2]);

        // 从第四行开始是数据
        Dictionary<int, Dictionary<string, string>> data = new Dictionary<int, Dictionary<string, string>>();

        for (int i = 3; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = ParseCSVLine(lines[i]);
            if (values.Length == 0) continue;

            // 第一列通常是ID
            if (int.TryParse(values[0], out int id))
            {
                Dictionary<string, string> rowData = new Dictionary<string, string>();

                for (int j = 0; j < fieldNames.Length && j < values.Length; j++)
                {
                    rowData[fieldNames[j]] = values[j];
                }

                data[id] = rowData;
            }
        }

        rawConfigData[configName] = data;
        Logger.Instance.LogInfo($"Parsed config {configName}: {data.Count} rows", "ConfigTable");
    }

    /// <summary>
    /// 解析CSV行（处理逗号分隔和引号）
    /// </summary>
    private string[] ParseCSVLine(string line)
    {
        List<string> values = new List<string>();
        StringBuilder currentValue = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

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

    /// <summary>
    /// 获取配置表原始数据
    /// </summary>
    public Dictionary<int, Dictionary<string, string>> GetRawConfig(string configName)
    {
        if (rawConfigData.ContainsKey(configName))
        {
            return rawConfigData[configName];
        }
        return null;
    }

    /// <summary>
    /// 获取配置表某一行数据
    /// </summary>
    public Dictionary<string, string> GetConfigRow(string configName, int id)
    {
        if (rawConfigData.ContainsKey(configName) && rawConfigData[configName].ContainsKey(id))
        {
            return rawConfigData[configName][id];
        }
        return null;
    }

    /// <summary>
    /// 获取配置表某个字段值
    /// </summary>
    public string GetConfigValue(string configName, int id, string fieldName)
    {
        var row = GetConfigRow(configName, id);
        if (row != null && row.ContainsKey(fieldName))
        {
            return row[fieldName];
        }
        return null;
    }

    /// <summary>
    /// 获取整型配置值
    /// </summary>
    public int GetIntValue(string configName, int id, string fieldName, int defaultValue = 0)
    {
        string value = GetConfigValue(configName, id, fieldName);
        if (int.TryParse(value, out int result))
        {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// 获取浮点型配置值
    /// </summary>
    public float GetFloatValue(string configName, int id, string fieldName, float defaultValue = 0f)
    {
        string value = GetConfigValue(configName, id, fieldName);
        if (float.TryParse(value, out float result))
        {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// 获取字符串配置值
    /// </summary>
    public string GetStringValue(string configName, int id, string fieldName, string defaultValue = "")
    {
        string value = GetConfigValue(configName, id, fieldName);
        return value ?? defaultValue;
    }

    /// <summary>
    /// 获取布尔型配置值
    /// </summary>
    public bool GetBoolValue(string configName, int id, string fieldName, bool defaultValue = false)
    {
        string value = GetConfigValue(configName, id, fieldName);
        if (bool.TryParse(value, out bool result))
        {
            return result;
        }
        if (int.TryParse(value, out int intResult))
        {
            return intResult != 0;
        }
        return defaultValue;
    }

    /// <summary>
    /// 加载并解析为强类型配置
    /// </summary>
    public void LoadConfig<T>(string configName) where T : IConfigRow, new()
    {
        if (configCache.ContainsKey(typeof(T)))
        {
            return;
        }

        Dictionary<int, T> configDict = new Dictionary<int, T>();
        var rawData = GetRawConfig(configName);

        if (rawData != null)
        {
            foreach (var kvp in rawData)
            {
                T config = new T();
                config.Parse(kvp.Value);
                configDict[kvp.Key] = config;
            }
        }

        configCache[typeof(T)] = configDict;
        Logger.Instance.LogInfo($"Loaded config {typeof(T).Name}: {configDict.Count} items", "ConfigTable");
    }

    /// <summary>
    /// 获取强类型配置
    /// </summary>
    public T GetConfig<T>(int id) where T : IConfigRow, new()
    {
        Type type = typeof(T);
        if (configCache.ContainsKey(type))
        {
            var dict = (Dictionary<int, T>)configCache[type];
            if (dict.ContainsKey(id))
            {
                return dict[id];
            }
        }
        return default(T);
    }

    /// <summary>
    /// 获取所有强类型配置
    /// </summary>
    public Dictionary<int, T> GetAllConfig<T>() where T : IConfigRow, new()
    {
        Type type = typeof(T);
        if (configCache.ContainsKey(type))
        {
            return (Dictionary<int, T>)configCache[type];
        }
        return new Dictionary<int, T>();
    }

    /// <summary>
    /// 清除所有配置缓存
    /// </summary>
    public void ClearCache()
    {
        configCache.Clear();
        rawConfigData.Clear();
        Logger.Instance.LogInfo("Config cache cleared", "ConfigTable");
    }

    /// <summary>
    /// 重新加载配置
    /// </summary>
    public void ReloadConfig(string configName, string path)
    {
        if (Path.GetExtension(path).ToLower() == ".csv")
        {
            LoadConfigFromCSV(configName, path);
        }
        else
        {
            LoadConfigFromAB(configName, Path.GetFileNameWithoutExtension(path));
        }
    }
}

/// <summary>
/// 配置行接口
/// </summary>
public interface IConfigRow
{
    void Parse(Dictionary<string, string> data);
}

/// <summary>
/// 配置行基类
/// </summary>
public abstract class BaseConfigRow : IConfigRow
{
    public abstract void Parse(Dictionary<string, string> data);

    protected int GetInt(Dictionary<string, string> data, string key, int defaultValue = 0)
    {
        if (data.ContainsKey(key) && int.TryParse(data[key], out int result))
        {
            return result;
        }
        return defaultValue;
    }

    protected float GetFloat(Dictionary<string, string> data, string key, float defaultValue = 0f)
    {
        if (data.ContainsKey(key) && float.TryParse(data[key], out float result))
        {
            return result;
        }
        return defaultValue;
    }

    protected string GetString(Dictionary<string, string> data, string key, string defaultValue = "")
    {
        if (data.ContainsKey(key))
        {
            return data[key];
        }
        return defaultValue;
    }

    protected bool GetBool(Dictionary<string, string> data, string key, bool defaultValue = false)
    {
        if (data.ContainsKey(key) && bool.TryParse(data[key], out bool result))
        {
            return result;
        }
        return defaultValue;
    }
}
