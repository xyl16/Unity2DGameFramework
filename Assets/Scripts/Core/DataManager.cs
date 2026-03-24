using System.IO;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    private static DataManager instance;
    public static DataManager Instance { get { return instance; } }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

            savePath = Application.persistentDataPath + "/Saves/";
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private string savePath;

    public void SaveData<T>(string fileName, T data)
    {
        string json = JsonUtility.ToJson(data, true);
        string filePath = savePath + fileName + ".json";

        File.WriteAllText(filePath, json);
        Debug.Log($"Data saved to: {filePath}");
    }

    public T LoadData<T>(string fileName, T defaultValue = default(T))
    {
        string filePath = savePath + fileName + ".json";

        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                return JsonUtility.FromJson<T>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load data: {e.Message}");
                return defaultValue;
            }
        }

        return defaultValue;
    }

    public bool HasData(string fileName)
    {
        string filePath = savePath + fileName + ".json";
        return File.Exists(filePath);
    }

    public void DeleteData(string fileName)
    {
        string filePath = savePath + fileName + ".json";
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"Data deleted: {filePath}");
        }
    }

    public void DeleteAllData()
    {
        if (Directory.Exists(savePath))
        {
            string[] files = Directory.GetFiles(savePath);
            foreach (string file in files)
            {
                File.Delete(file);
            }
            Debug.Log("All saved data deleted");
        }
    }

    public string GetSavePath()
    {
        return savePath;
    }

    public void SaveRawData(string fileName, string data)
    {
        string filePath = savePath + fileName;
        File.WriteAllText(filePath, data);
        Debug.Log($"Raw data saved to: {filePath}");
    }

    public string LoadRawData(string fileName)
    {
        string filePath = savePath + fileName;
        if (File.Exists(filePath))
        {
            return File.ReadAllText(filePath);
        }
        return null;
    }
}
