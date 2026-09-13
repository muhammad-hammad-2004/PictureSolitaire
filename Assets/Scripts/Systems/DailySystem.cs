using UnityEngine;

public static class DailySystem
{
    public static bool PlayingDaily;

    public static string TodayKey => System.DateTime.Now.ToString("yyyyMMdd");

    public static int TodaySeed
    {
        get { int v; int.TryParse(TodayKey, out v); return v; }
    }

    public static int DailyIndex => TodaySeed % Mathf.Max(1, LevelConfig.TotalLevels);

    public static bool IsCompleted => PlayerPrefs.GetInt("DailyDone_" + TodayKey, 0) == 1;

    public static void MarkCompleted()
    {
        PlayerPrefs.SetInt("DailyDone_" + TodayKey, 1);
        PlayerPrefs.SetInt("DailyTotal", PlayerPrefs.GetInt("DailyTotal", 0) + 1);
        PlayerPrefs.Save();
    }
}