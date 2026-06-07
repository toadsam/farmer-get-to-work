using System;

public static class GameDataUtility
{
    public static long NowTicks()
    {
        return DateTime.Now.Ticks;
    }

    public static DateTime FromTicks(long ticks)
    {
        if (ticks <= 0)
            return DateTime.MinValue;

        return new DateTime(ticks);
    }

    public static string ToDateKey(DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd");
    }

    public static string ToTimeText(long ticks)
    {
        if (ticks <= 0)
            return "--:--";

        DateTime dateTime = new DateTime(ticks);
        return dateTime.ToString("HH:mm");
    }

    public static string ToMinuteSecondText(float seconds)
    {
        if (seconds < 0f)
            seconds = 0f;

        int totalSeconds = UnityEngine.Mathf.CeilToInt(seconds);
        int minute = totalSeconds / 60;
        int second = totalSeconds % 60;

        return $"{minute:00}:{second:00}";
    }
}