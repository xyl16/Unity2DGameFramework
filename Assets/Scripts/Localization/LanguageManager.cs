using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;

/// <summary>
/// 多语言管理器
/// 支持动态切换语言，支持文本、图片等资源本地化
/// </summary>
public class LanguageManager : MonoBehaviour
{
    private static LanguageManager instance;
    private static bool isApplicationQuitting = false;

    public static LanguageManager Instance
    {
        get
        {
            if (instance == null && !isApplicationQuitting)
            {
                GameObject obj = new GameObject("LanguageManager");
                instance = obj.AddComponent<LanguageManager>();
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

    // 支持的语言
    public enum Language
    {
        Chinese,    // 中文
        English,    // 英文
        Japanese,   // 日文
        Korean      // 韩文
    }

    // 当前语言
    private Language currentLanguage = Language.Chinese;

    // 语言字典
    private Dictionary<string, Dictionary<Language, string>> languageDictionary = new Dictionary<string, Dictionary<Language, string>>();

    // 语言代码映射
    private Dictionary<Language, string> languageCodes = new Dictionary<Language, string>
    {
        { Language.Chinese, "zh" },
        { Language.English, "en" },
        { Language.Japanese, "ja" },
        { Language.Korean, "ko" }
    };

    // 语言名称映射
    private Dictionary<Language, string> languageNames = new Dictionary<Language, string>
    {
        { Language.Chinese, "中文" },
        { Language.English, "English" },
        { Language.Japanese, "日本語" },
        { Language.Korean, "한국어" }
    };

    // 语言切换事件
    public event Action<Language> OnLanguageChanged;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLanguageSettings();
            LoadLanguageFiles();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 加载语言设置
    /// </summary>
    private void LoadLanguageSettings()
    {
        string languageCode = PlayerPrefs.GetString("Language", "zh");
        foreach (var kvp in languageCodes)
        {
            if (kvp.Value == languageCode)
            {
                currentLanguage = kvp.Key;
                break;
            }
        }
        Debug.Log($"[Language] 当前语言: {languageNames[currentLanguage]}");
    }

    /// <summary>
    /// 加载语言文件
    /// </summary>
    private void LoadLanguageFiles()
    {
        // 加载默认语言数据
        LoadDefaultLanguageData();

        // 尝试从Resources加载
        TextAsset[] languageFiles = Resources.LoadAll<TextAsset>("Localization");
        foreach (var file in languageFiles)
        {
            ParseLanguageFile(file.text);
        }

        Debug.Log($"[Language] 已加载 {languageDictionary.Count} 条翻译");
    }

    /// <summary>
    /// 加载默认语言数据
    /// </summary>
    private void LoadDefaultLanguageData()
    {
        // 添加一些默认翻译
        AddTranslation("Login", Language.Chinese, "登录");
        AddTranslation("Login", Language.English, "Login");
        AddTranslation("Login", Language.Japanese, "ログイン");
        AddTranslation("Login", Language.Korean, "로그인");

        AddTranslation("Username", Language.Chinese, "用户名");
        AddTranslation("Username", Language.English, "Username");
        AddTranslation("Username", Language.Japanese, "ユーザー名");
        AddTranslation("Username", Language.Korean, "사용자명");

        AddTranslation("Password", Language.Chinese, "密码");
        AddTranslation("Password", Language.English, "Password");
        AddTranslation("Password", Language.Japanese, "パスワード");
        AddTranslation("Password", Language.Korean, "비밀번호");

        AddTranslation("Confirm", Language.Chinese, "确认");
        AddTranslation("Confirm", Language.English, "Confirm");
        AddTranslation("Confirm", Language.Japanese, "確認");
        AddTranslation("Confirm", Language.Korean, "확인");

        AddTranslation("Cancel", Language.Chinese, "取消");
        AddTranslation("Cancel", Language.English, "Cancel");
        AddTranslation("Cancel", Language.Japanese, "キャンセル");
        AddTranslation("Cancel", Language.Korean, "취소");

        AddTranslation("Settings", Language.Chinese, "设置");
        AddTranslation("Settings", Language.English, "Settings");
        AddTranslation("Settings", Language.Japanese, "設定");
        AddTranslation("Settings", Language.Korean, "설정");

        AddTranslation("Language", Language.Chinese, "语言");
        AddTranslation("Language", Language.English, "Language");
        AddTranslation("Language", Language.Japanese, "言語");
        AddTranslation("Language", Language.Korean, "언어");

        AddTranslation("Exit", Language.Chinese, "退出");
        AddTranslation("Exit", Language.English, "Exit");
        AddTranslation("Exit", Language.Japanese, "終了");
        AddTranslation("Exit", Language.Korean, "종료");

        AddTranslation("Loading", Language.Chinese, "加载中...");
        AddTranslation("Loading", Language.English, "Loading...");
        AddTranslation("Loading", Language.Japanese, "読み込み中...");
        AddTranslation("Loading", Language.Korean, "로딩 중...");

        AddTranslation("Success", Language.Chinese, "成功");
        AddTranslation("Success", Language.English, "Success");
        AddTranslation("Success", Language.Japanese, "成功");
        AddTranslation("Success", Language.Korean, "성공");

        AddTranslation("Failed", Language.Chinese, "失败");
        AddTranslation("Failed", Language.English, "Failed");
        AddTranslation("Failed", Language.Japanese, "失敗");
        AddTranslation("Failed", Language.Korean, "실패");
    }

    /// <summary>
    /// 解析语言文件（JSON格式）
    /// </summary>
    private void ParseLanguageFile(string json)
    {
        // 简单的JSON解析（实际项目中建议使用JSON解析库）
        string[] lines = json.Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // 格式: "key": { "zh": "中文", "en": "English", ... }
            if (line.Contains("{"))
            {
                string key = ExtractValue(line, "\"key\"");
                // 解析各语言翻译（简化版）
            }
        }
    }

    private string ExtractValue(string line, string key)
    {
        // 简化的值提取
        int index = line.IndexOf(key);
        if (index >= 0)
        {
            int startIndex = line.IndexOf("\"", index + key.Length + 2);
            int endIndex = line.IndexOf("\"", startIndex + 1);
            if (startIndex >= 0 && endIndex > startIndex)
            {
                return line.Substring(startIndex + 1, endIndex - startIndex - 1);
            }
        }
        return "";
    }

    /// <summary>
    /// 添加翻译
    /// </summary>
    public void AddTranslation(string key, Language language, string translation)
    {
        if (!languageDictionary.ContainsKey(key))
        {
            languageDictionary[key] = new Dictionary<Language, string>();
        }
        languageDictionary[key][language] = translation;
    }

    /// <summary>
    /// 获取翻译文本
    /// </summary>
    public string GetText(string key, params object[] args)
    {
        if (languageDictionary.ContainsKey(key) && languageDictionary[key].ContainsKey(currentLanguage))
        {
            string text = languageDictionary[key][currentLanguage];
            if (args != null && args.Length > 0)
            {
                return string.Format(text, args);
            }
            return text;
        }

        // 如果找不到翻译，返回key
        Debug.LogWarning($"[Language] 未找到翻译: {key}");
        return key;
    }

    /// <summary>
    /// 获取指定语言的翻译文本
    /// </summary>
    public string GetText(string key, Language language, params object[] args)
    {
        if (languageDictionary.ContainsKey(key) && languageDictionary[key].ContainsKey(language))
        {
            string text = languageDictionary[key][language];
            if (args != null && args.Length > 0)
            {
                return string.Format(text, args);
            }
            return text;
        }
        return key;
    }

    /// <summary>
    /// 设置当前语言
    /// </summary>
    public void SetLanguage(Language language)
    {
        if (currentLanguage != language)
        {
            currentLanguage = language;
            PlayerPrefs.SetString("Language", languageCodes[language]);
            PlayerPrefs.Save();

            Debug.Log($"[Language] 语言已切换为: {languageNames[language]}");
            OnLanguageChanged?.Invoke(language);
        }
    }

    /// <summary>
    /// 通过语言代码设置语言
    /// </summary>
    public void SetLanguageByCode(string languageCode)
    {
        foreach (var kvp in languageCodes)
        {
            if (kvp.Value == languageCode)
            {
                SetLanguage(kvp.Key);
                return;
            }
        }
    }

    /// <summary>
    /// 获取当前语言
    /// </summary>
    public Language GetCurrentLanguage()
    {
        return currentLanguage;
    }

    /// <summary>
    /// 获取当前语言代码
    /// </summary>
    public string GetCurrentLanguageCode()
    {
        return languageCodes[currentLanguage];
    }

    /// <summary>
    /// 获取支持的语言列表
    /// </summary>
    public List<Language> GetSupportedLanguages()
    {
        return new List<Language>(languageNames.Keys);
    }

    /// <summary>
    /// 获取语言名称
    /// </summary>
    public string GetLanguageName(Language language)
    {
        if (languageNames.ContainsKey(language))
        {
            return languageNames[language];
        }
        return language.ToString();
    }

    /// <summary>
    /// 获取本地化图片路径
    /// </summary>
    public string GetLocalizedImagePath(string imageName)
    {
        return $"Images/{languageCodes[currentLanguage]}/{imageName}";
    }

    /// <summary>
    /// 获取本地化音频路径
    /// </summary>
    public string GetLocalizedAudioPath(string audioName)
    {
        return $"Audio/{languageCodes[currentLanguage]}/{audioName}";
    }

    /// <summary>
    /// 检查是否存在翻译
    /// </summary>
    public bool HasTranslation(string key)
    {
        return languageDictionary.ContainsKey(key) && languageDictionary[key].ContainsKey(currentLanguage);
    }

    /// <summary>
    /// 导出当前语言字典
    /// </summary>
    public void ExportLanguageDictionary()
    {
        string json = "{\n";

        foreach (var kvp in languageDictionary)
        {
            json += $"  \"{kvp.Key}\": {{\n";

            foreach (var lang in kvp.Value)
            {
                json += $"    \"{languageCodes[lang.Key]}\": \"{lang.Value}\",\n";
            }

            json = json.TrimEnd(',', '\n') + "\n  },\n";
        }

        json = json.TrimEnd(',', '\n') + "\n}";

        string filePath = Path.Combine(Application.persistentDataPath, $"LanguageDictionary_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        File.WriteAllText(filePath, json);
        Debug.Log($"[Language] 语言字典已导出: {filePath}");
    }

    /// <summary>
    /// 从文件导入语言数据
    /// </summary>
    public void ImportLanguageData(string filePath)
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            ParseLanguageFile(json);
            Debug.Log($"[Language] 已导入语言数据: {filePath}");
        }
        else
        {
            Debug.LogError($"[Language] 文件不存在: {filePath}");
        }
    }
}
