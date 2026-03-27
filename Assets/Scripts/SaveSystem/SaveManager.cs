using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

/// <summary>
/// 存档管理器
/// 支持多个存档槽、自动存档、云端存档同步
/// </summary>
public class SaveManager : MonoBehaviour
{
    private static SaveManager instance;
    private static bool isApplicationQuitting = false;

    public static SaveManager Instance
    {
        get
        {
            if (instance == null && !isApplicationQuitting)
            {
                GameObject obj = new GameObject("SaveManager");
                instance = obj.AddComponent<SaveManager>();
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

    // 配置
    public int maxSaveSlots = 5;
    public float autoSaveInterval = 300f; // 5分钟自动存档一次
    public bool enableAutoSave = true;
    public bool enableCloudSave = false;

    // 存档槽
    private List<SaveSlot> saveSlots = new List<SaveSlot>();
    private int currentSlotIndex = -1;

    // 自动存档
    private float lastAutoSaveTime;

    // 存档数据
    private Dictionary<string, object> saveData = new Dictionary<string, object>();

    // 存档事件
    public event Action<int> OnSaveCompleted;
    public event Action<int> OnLoadCompleted;
    public event Action<string> OnCloudSaveCompleted;
    public event Action<string> OnCloudLoadCompleted;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSaveSlots();
            LoadSaveSlots();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (enableAutoSave && currentSlotIndex >= 0)
        {
            if (Time.time - lastAutoSaveTime >= autoSaveInterval)
            {
                AutoSave();
            }
        }
    }

    /// <summary>
    /// 初始化存档槽
    /// </summary>
    private void InitializeSaveSlots()
    {
        saveSlots.Clear();
        for (int i = 0; i < maxSaveSlots; i++)
        {
            saveSlots.Add(new SaveSlot
            {
                slotIndex = i,
                isUsed = false,
                saveTime = DateTime.MinValue,
                playTime = 0f,
                level = 1,
                saveName = $"存档槽 {i + 1}"
            });
        }
    }

    /// <summary>
    /// 加载存档槽信息
    /// </summary>
    private void LoadSaveSlots()
    {
        for (int i = 0; i < maxSaveSlots; i++)
        {
            string filePath = GetSlotFilePath(i);
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    SaveData data = JsonUtility.FromJson<SaveData>(json);
                    saveSlots[i].isUsed = true;
                    saveSlots[i].saveName = data.saveName;
                    saveSlots[i].saveTime = DateTime.Parse(data.saveTime);
                    saveSlots[i].playTime = data.playTime;
                    saveSlots[i].level = data.level;
                    saveSlots[i].description = data.description;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Save] 加载存档槽 {i} 失败: {e.Message}");
                }
            }
        }
        Debug.Log($"[Save] 已加载 {GetUsedSlotCount()} 个存档");
    }

    /// <summary>
    /// 获取存档槽文件路径
    /// </summary>
    private string GetSlotFilePath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, $"Save_{slotIndex}.json");
    }

    /// <summary>
    /// 获取已使用的存档槽数量
    /// </summary>
    public int GetUsedSlotCount()
    {
        int count = 0;
        foreach (var slot in saveSlots)
        {
            if (slot.isUsed) count++;
        }
        return count;
    }

    /// <summary>
    /// 获取所有存档槽
    /// </summary>
    public List<SaveSlot> GetAllSaveSlots()
    {
        return new List<SaveSlot>(saveSlots);
    }

    /// <summary>
    /// 获取指定存档槽
    /// </summary>
    public SaveSlot GetSaveSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < saveSlots.Count)
        {
            return saveSlots[slotIndex];
        }
        return null;
    }

    /// <summary>
    /// 保存数据到指定槽位
    /// </summary>
    public bool SaveToSlot(int slotIndex, string saveName, string description = "")
    {
        if (slotIndex < 0 || slotIndex >= maxSaveSlots)
        {
            Debug.LogError($"[Save] 存档槽索引无效: {slotIndex}");
            return false;
        }

        try
        {
            SaveData data = new SaveData
            {
                saveName = saveName,
                description = description,
                saveTime = DateTime.Now.ToString(),
                playTime = Time.time,
                level = GetPlayerLevel(),
                data = new Dictionary<string, string>()
            };

            // 转换所有保存的数据
            foreach (var kvp in saveData)
            {
                data.data[kvp.Key] = ConvertToString(kvp.Value);
            }

            string json = JsonUtility.ToJson(data);
            File.WriteAllText(GetSlotFilePath(slotIndex), json);

            // 更新存档槽信息
            saveSlots[slotIndex].isUsed = true;
            saveSlots[slotIndex].saveName = saveName;
            saveSlots[slotIndex].saveTime = DateTime.Now;
            saveSlots[slotIndex].description = description;

            currentSlotIndex = slotIndex;
            OnSaveCompleted?.Invoke(slotIndex);

            Debug.Log($"[Save] 已保存到槽位 {slotIndex}: {saveName}");

            // 云端存档
            if (enableCloudSave)
            {
                UploadToCloud(slotIndex, json);
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] 保存失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 从指定槽位加载数据
    /// </summary>
    public bool LoadFromSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSaveSlots)
        {
            Debug.LogError($"[Save] 存档槽索引无效: {slotIndex}");
            return false;
        }

        string filePath = GetSlotFilePath(slotIndex);
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[Save] 存档槽 {slotIndex} 不存在");
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            // 恢复数据
            saveData.Clear();
            foreach (var kvp in data.data)
            {
                saveData[kvp.Key] = ConvertFromString(kvp.Value);
            }

            currentSlotIndex = slotIndex;
            OnLoadCompleted?.Invoke(slotIndex);

            Debug.Log($"[Save] 已从槽位 {slotIndex} 加载存档: {data.saveName}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] 加载失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 自动保存
    /// </summary>
    private void AutoSave()
    {
        if (currentSlotIndex >= 0)
        {
            SaveToSlot(currentSlotIndex, saveSlots[currentSlotIndex].saveName, "自动存档");
            lastAutoSaveTime = Time.time;
            Debug.Log("[Save] 自动存档完成");
        }
    }

    /// <summary>
    /// 删除存档
    /// </summary>
    public bool DeleteSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSaveSlots)
        {
            Debug.LogError($"[Save] 存档槽索引无效: {slotIndex}");
            return false;
        }

        string filePath = GetSlotFilePath(slotIndex);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            saveSlots[slotIndex].isUsed = false;
            saveSlots[slotIndex].saveTime = DateTime.MinValue;
            saveSlots[slotIndex].playTime = 0f;

            if (currentSlotIndex == slotIndex)
            {
                currentSlotIndex = -1;
            }

            Debug.Log($"[Save] 已删除存档槽 {slotIndex}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// 保存数据
    /// </summary>
    public void SetData(string key, object value)
    {
        saveData[key] = value;
    }

    /// <summary>
    /// 获取数据
    /// </summary>
    public T GetData<T>(string key, T defaultValue = default(T))
    {
        if (saveData.ContainsKey(key))
        {
            try
            {
                return (T)saveData[key];
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// 删除数据
    /// </summary>
    public void RemoveData(string key)
    {
        saveData.Remove(key);
    }

    /// <summary>
    /// 检查是否存在数据
    /// </summary>
    public bool HasData(string key)
    {
        return saveData.ContainsKey(key);
    }

    /// <summary>
    /// 清空所有数据
    /// </summary>
    public void ClearAllData()
    {
        saveData.Clear();
        Debug.Log("[Save] 已清空所有存档数据");
    }

    /// <summary>
    /// 上传到云端（示例）
    /// </summary>
    private void UploadToCloud(int slotIndex, string jsonData)
    {
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
        {
            // 发送到服务器
            // NetworkManager.Instance.SendMessage(cloudSaveMsgId, data);
            Debug.Log($"[Save] 上传存档 {slotIndex} 到云端");
            OnCloudSaveCompleted?.Invoke($"Slot_{slotIndex}");
        }
        else
        {
            Debug.LogWarning("[Save] 网络未连接，无法上传到云端");
        }
    }

    /// <summary>
    /// 从云端下载（示例）
    /// </summary>
    public void DownloadFromCloud(int slotIndex)
    {
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
        {
            // 从服务器下载
            Debug.Log($"[Save] 从云端下载存档 {slotIndex}");
            OnCloudLoadCompleted?.Invoke($"Slot_{slotIndex}");
        }
        else
        {
            Debug.LogWarning("[Save] 网络未连接，无法从云端下载");
        }
    }

    /// <summary>
    /// 导出存档
    /// </summary>
    public string ExportSave(int slotIndex)
    {
        string filePath = GetSlotFilePath(slotIndex);
        if (File.Exists(filePath))
        {
            return File.ReadAllText(filePath);
        }
        return null;
    }

    /// <summary>
    /// 导入存档
    /// </summary>
    public bool ImportSave(int slotIndex, string jsonData)
    {
        if (slotIndex < 0 || slotIndex >= maxSaveSlots)
        {
            Logger.Instance.LogError($"存档槽索引无效: {slotIndex}", "Save");
            return false;
        }

        try
        {
            File.WriteAllText(GetSlotFilePath(slotIndex), jsonData);
            LoadSaveSlots();
            Debug.Log($"[Save] 已导入存档到槽位 {slotIndex}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] 导入存档失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取玩家等级（示例）
    /// </summary>
    private int GetPlayerLevel()
    {
        return GetData<int>("PlayerLevel", 1);
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    private string ConvertToString(object obj)
    {
        if (obj == null) return "";
        return obj.ToString();
    }

    /// <summary>
    /// 从字符串转换
    /// </summary>
    private object ConvertFromString(string str)
    {
        return str;
    }

    /// <summary>
    /// 获取当前使用的存档槽
    /// </summary>
    public int GetCurrentSlotIndex()
    {
        return currentSlotIndex;
    }

    /// <summary>
    /// 获取存档槽数量
    /// </summary>
    public int GetSlotCount()
    {
        return maxSaveSlots;
    }
}

/// <summary>
/// 存档槽信息
/// </summary>
[System.Serializable]
public class SaveSlot
{
    public int slotIndex;
    public bool isUsed;
    public string saveName;
    public DateTime saveTime;
    public float playTime;
    public int level;
    public string description;
}

/// <summary>
/// 存档数据
/// </summary>
[System.Serializable]
public class SaveData
{
    public string saveName;
    public string description;
    public string saveTime;
    public float playTime;
    public int level;
    public Dictionary<string, string> data;
}
