using System;
using System.Text;

/// <summary>
/// 数字格式转换工具类
/// </summary>
public static class NumberFormatHelper
{
    /// <summary>
    /// 将数字格式化为带逗号的字符串（如：1,234,567）
    /// </summary>
    public static string FormatWithCommas(long number)
    {
        return number.ToString("N0");
    }

    /// <summary>
    /// 将大数字转换为简短格式（如：1.2K, 1.5M, 2.3B）
    /// </summary>
    public static string FormatShortNumber(long number)
    {
        if (number < 1000) return number.ToString();

        double num = number;
        string suffix = "";

        if (number >= 1_000_000_000)
        {
            num = number / 1_000_000_000.0;
            suffix = "B";
        }
        else if (number >= 1_000_000)
        {
            num = number / 1_000_000.0;
            suffix = "M";
        }
        else if (number >= 1000)
        {
            num = number / 1000.0;
            suffix = "K";
        }

        return $"{num:F1}{suffix}";
    }

    /// <summary>
    /// 将大数字转换为中文单位格式（如：1.2万, 1.5亿）
    /// </summary>
    public static string FormatChineseNumber(long number)
    {
        if (number < 10000) return number.ToString();

        double num = number;
        string suffix = "";

        if (number >= 100_000_000)
        {
            num = number / 100_000_000.0;
            suffix = "亿";
        }
        else if (number >= 10000)
        {
            num = number / 10000.0;
            suffix = "万";
        }

        return $"{num:F1}{suffix}";
    }

    /// <summary>
    /// 格式化百分比
    /// </summary>
    public static string FormatPercent(float value, int decimals = 1)
    {
        return string.Format($"{{0:F{decimals}}}", value * 100) + "%";
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

    /// <summary>
    /// 格式化速度
    /// </summary>
    public static string FormatSpeed(float speed, string unit = "m/s")
    {
        return $"{speed:F2}{unit}";
    }

    /// <summary>
    /// 格式化距离
    /// </summary>
    public static string FormatDistance(float distance)
    {
        if (distance < 1000) return $"{distance:F0}m";
        return $"{distance / 1000:F2}km";
    }

    /// <summary>
    /// 判断数字是否在范围内
    /// </summary>
    public static bool IsInRange(float value, float min, float max)
    {
        return value >= min && value <= max;
    }

    /// <summary>
    /// 限制数字在指定范围内
    /// </summary>
    public static float Clamp(float value, float min, float max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    /// <summary>
    /// 限制整数在指定范围内
    /// </summary>
    public static int Clamp(int value, int min, int max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    /// <summary>
    /// 线性插值
    /// </summary>
    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * Clamp(t, 0, 1);
    }

    /// <summary>
    /// 数值映射（将值从一个范围映射到另一个范围）
    /// </summary>
    public static float Remap(float value, float inMin, float inMax, float outMin, float outMax)
    {
        return Lerp(outMin, outMax, (value - inMin) / (inMax - inMin));
    }
}
