using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 字符串处理工具类
/// </summary>
public static class StringHelper
{
    /// <summary>
    /// 判断字符串是否为空或空白
    /// </summary>
    public static bool IsNullOrEmpty(string str)
    {
        return string.IsNullOrEmpty(str);
    }

    /// <summary>
    /// 判断字符串是否为空、空白或仅包含空白字符
    /// </summary>
    public static bool IsNullOrWhiteSpace(string str)
    {
        return string.IsNullOrWhiteSpace(str);
    }

    /// <summary>
    /// 安全截取字符串
    /// </summary>
    public static string SafeSubstring(string str, int length, string suffix = "...")
    {
        if (string.IsNullOrEmpty(str) || str.Length <= length)
            return str;

        return str.Substring(0, length) + suffix;
    }

    /// <summary>
    /// 去除字符串中的所有空白字符
    /// </summary>
    public static string RemoveAllSpaces(string str)
    {
        return Regex.Replace(str, @"\s+", "");
    }

    /// <summary>
    /// 将字符串转换为首字母大写
    /// </summary>
    public static string ToTitleCase(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;

        return char.ToUpper(str[0]) + str.Substring(1).ToLower();
    }

    /// <summary>
    /// 将字符串转换为驼峰命名（camelCase）
    /// </summary>
    public static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;

        string[] words = str.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return str;

        StringBuilder result = new StringBuilder();
        for (int i = 0; i < words.Length; i++)
        {
            if (i == 0)
            {
                result.Append(words[i].ToLower());
            }
            else
            {
                result.Append(char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower());
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// 将字符串转换为帕斯卡命名（PascalCase）
    /// </summary>
    public static string ToPascalCase(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;

        string[] words = str.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return str;

        StringBuilder result = new StringBuilder();
        foreach (string word in words)
        {
            result.Append(char.ToUpper(word[0]) + word.Substring(1).ToLower());
        }

        return result.ToString();
    }

    /// <summary>
    /// 将字符串转换为蛇形命名（snake_case）
    /// </summary>
    public static string ToSnakeCase(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;

        string result = Regex.Replace(str, @"([a-z0-9])([A-Z])", "$1_$2");
        return result.ToLower();
    }

    /// <summary>
    /// 生成随机字符串
    /// </summary>
    public static string GenerateRandomString(int length, string charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
    {
        if (length <= 0) return string.Empty;

        StringBuilder result = new StringBuilder(length);
        System.Random random = new System.Random();

        for (int i = 0; i < length; i++)
        {
            result.Append(charset[random.Next(charset.Length)]);
        }

        return result.ToString();
    }

    /// <summary>
    /// 生成唯一ID
    /// </summary>
    public static string GenerateUniqueId()
    {
        return Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// 判断是否为有效的邮箱地址
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return false;

        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, pattern);
    }

    /// <summary>
    /// 判断是否为有效的手机号（中国大陆）
    /// </summary>
    public static bool IsValidPhoneNumber(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return false;

        string pattern = @"^1[3-9]\d{9}$";
        return Regex.IsMatch(phone, pattern);
    }

    /// <summary>
    /// 隐藏字符串中间部分（如手机号、身份证号）
    /// </summary>
    public static string HideMiddle(string str, int keepStart = 3, int keepEnd = 4, string replaceStr = "****")
    {
        if (string.IsNullOrEmpty(str) || str.Length <= keepStart + keepEnd)
            return str;

        return str.Substring(0, keepStart) + replaceStr + str.Substring(str.Length - keepEnd);
    }

    /// <summary>
    /// 高亮关键词
    /// </summary>
    public static string HighlightKeyword(string text, string keyword, string color = "yellow")
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword))
            return text;

        return text.Replace(keyword, $"<color={color}>{keyword}</color>");
    }

    /// <summary>
    /// 计算字符串的显示宽度（用于UI文本长度估算）
    /// </summary>
    public static float CalculateTextWidth(string text, Font font, int fontSize)
    {
        if (font == null) return text.Length * fontSize * 0.5f;

        float width = 0;
        foreach (char c in text)
        {
            font.GetCharacterInfo(c, out CharacterInfo info, fontSize);
            width += info.advance;
        }

        return width;
    }

    /// <summary>
    /// 移除富文本标签
    /// </summary>
    public static string StripRichTextTags(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        return Regex.Replace(text, "<.*?>", string.Empty);
    }

    /// <summary>
    /// 将字典转换为查询字符串
    /// </summary>
    public static string ToQueryString(Dictionary<string, string> parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return string.Empty;

        StringBuilder result = new StringBuilder();
        bool first = true;

        foreach (var kvp in parameters)
        {
            if (!first)
                result.Append("&");

            result.Append(Uri.EscapeDataString(kvp.Key));
            result.Append("=");
            result.Append(Uri.EscapeDataString(kvp.Value));
            first = false;
        }

        return result.ToString();
    }

    /// <summary>
    /// 将查询字符串解析为字典
    /// </summary>
    public static Dictionary<string, string> ParseQueryString(string queryString)
    {
        var result = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(queryString))
            return result;

        string[] pairs = queryString.Split('&');
        foreach (string pair in pairs)
        {
            string[] keyValue = pair.Split('=');
            if (keyValue.Length == 2)
            {
                result[Uri.UnescapeDataString(keyValue[0])] = Uri.UnescapeDataString(keyValue[1]);
            }
        }

        return result;
    }
}
