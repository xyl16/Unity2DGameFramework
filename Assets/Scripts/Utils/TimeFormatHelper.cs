using System;
using System.Text;

/// <summary>
/// 时间格式转换工具类
/// </summary>
public static class TimeFormatHelper
{
    /// <summary>
    /// 将秒数转换为 HH:MM:SS 格式
    /// </summary>
    public static string FormatTime(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(seconds);
        return time.ToString(@"hh\:mm\:ss");
    }

    /// <summary>
    /// 将秒数转换为 MM:SS 格式
    /// </summary>
    public static string FormatMinutesSeconds(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(seconds);
        return time.ToString(@"mm\:ss");
    }

    /// <summary>
    /// 将秒数转换为可读格式
    /// </summary>
    public static string FormatReadable(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(seconds);

        if (time.TotalDays >= 1)
            return $"{time.Days}天{time.Hours}小时";
        if (time.TotalHours >= 1)
            return $"{time.Hours}小时{time.Minutes}分钟";
        if (time.TotalMinutes >= 1)
            return $"{time.Minutes}分钟{time.Seconds}秒";
        return $"{time.Seconds}秒";
    }

    /// <summary>
    /// 将时间戳转换为日期时间字符串
    /// </summary>
    public static string FormatTimestamp(long timestamp, string format = "yyyy-MM-dd HH:mm:ss")
    {
        DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);
        return dt.ToLocalTime().ToString(format);
    }

    /// <summary>
    /// 获取当前时间戳（秒）
    /// </summary>
    public static long GetCurrentTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    /// <summary>
    /// 获取当前时间戳（毫秒）
    /// </summary>
    public static long GetCurrentTimestampMs()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// 判断是否为今天
    /// </summary>
    public static bool IsToday(long timestamp)
    {
        DateTime dt = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
        return dt.Date == DateTime.Today;
    }

    /// <summary>
    /// 获取剩余时间的描述
    /// </summary>
    public static string GetRemainingTime(long endTimestamp)
    {
        long remaining = endTimestamp - GetCurrentTimestamp();
        if (remaining <= 0) return "已结束";
        return FormatReadable(remaining);
    }

    /// <summary>
    /// 格式化倒计时
    /// </summary>
    public static string FormatCountdown(float seconds)
    {
        int days = (int)(seconds / 86400);
        int hours = (int)((seconds % 86400) / 3600);
        int mins = (int)((seconds % 3600) / 60);
        int secs = (int)(seconds % 60);

        if (days > 0)
            return $"{days}天{hours:00}:{mins:00}:{secs:00}";
        return $"{hours:00}:{mins:00}:{secs:00}";
    }
}
