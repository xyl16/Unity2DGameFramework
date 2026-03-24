using System;
using System.Collections.Generic;
using UnityEngine;

public class ConfigManager : MonoBehaviour
{
    private static ConfigManager instance;
    public static ConfigManager Instance { get { return instance; } }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

            configPath = Application.persistentDataPath + "/config.json";
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadConfig();
    }

    [Serializable]
    public class AppConfig
    {
        public string serverIP = "127.0.0.1";
        public int serverPort = 8888;
        public float musicVolume = 1.0f;
        public float sfxVolume = 1.0f;
        public bool isFullscreen = false;
        public int screenWidth = 1920;
        public int screenHeight = 1080;
        public string language = "zh-CN";
        public bool enableVSync = true;
        public int targetFrameRate = 60;
    }

    private AppConfig config;
    private string configPath;

    public void LoadConfig()
    {
        if (System.IO.File.Exists(configPath))
        {
            try
            {
                string json = System.IO.File.ReadAllText(configPath);
                config = JsonUtility.FromJson<AppConfig>(json);
                Logger.Instance.LogInfo("Config loaded successfully", "Config");
            }
            catch (Exception e)
            {
                Logger.Instance.LogError($"Failed to load config: {e.Message}", "Config");
                config = new AppConfig();
            }
        }
        else
        {
            config = new AppConfig();
            SaveConfig();
        }

        ApplyConfig();
    }

    public void SaveConfig()
    {
        try
        {
            string json = JsonUtility.ToJson(config, true);
            System.IO.File.WriteAllText(configPath, json);
            Logger.Instance.LogInfo("Config saved successfully", "Config");
        }
        catch (Exception e)
        {
            Logger.Instance.LogError($"Failed to save config: {e.Message}", "Config");
        }
    }

    private void ApplyConfig()
    {
        if (config == null) return;

        Screen.fullScreen = config.isFullscreen;
        Screen.SetResolution(config.screenWidth, config.screenHeight, config.isFullscreen);
        QualitySettings.vSyncCount = config.enableVSync ? 1 : 0;
        Application.targetFrameRate = config.targetFrameRate;

        AudioManager.Instance.SetMusicVolume(config.musicVolume);
        AudioManager.Instance.SetSFXVolume(config.sfxVolume);
    }

    public AppConfig GetConfig()
    {
        return config;
    }

    public void SetServerIP(string ip)
    {
        config.serverIP = ip;
        SaveConfig();
    }

    public void SetServerPort(int port)
    {
        config.serverPort = port;
        SaveConfig();
    }

    public void SetMusicVolume(float volume)
    {
        config.musicVolume = volume;
        AudioManager.Instance.SetMusicVolume(volume);
        SaveConfig();
    }

    public void SetSFXVolume(float volume)
    {
        config.sfxVolume = volume;
        AudioManager.Instance.SetSFXVolume(volume);
        SaveConfig();
    }

    public void SetResolution(int width, int height, bool fullscreen)
    {
        config.screenWidth = width;
        config.screenHeight = height;
        config.isFullscreen = fullscreen;
        Screen.SetResolution(width, height, fullscreen);
        SaveConfig();
    }

    public void SetLanguage(string language)
    {
        config.language = language;
        SaveConfig();
    }

    public void SetVSync(bool enable)
    {
        config.enableVSync = enable;
        QualitySettings.vSyncCount = enable ? 1 : 0;
        SaveConfig();
    }

    public void SetTargetFrameRate(int frameRate)
    {
        config.targetFrameRate = frameRate;
        Application.targetFrameRate = frameRate;
        SaveConfig();
    }

    public void ResetConfig()
    {
        config = new AppConfig();
        SaveConfig();
        ApplyConfig();
        Logger.Instance.LogInfo("Config reset to default", "Config");
    }
}
