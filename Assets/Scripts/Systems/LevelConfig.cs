using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Picture Solitaire/Level Config")]
public class LevelConfig : ScriptableObject
{
    public const int LevelsPerChapter = 25;

    [System.Serializable]
    public class Entry
    {
        public string imageAddress; 
        public int rows = 2;
        public int cols = 2;
    }

    public List<Entry> levels = new List<Entry>();
    public List<string> chapterAddresses = new List<string>();

    private static LevelConfig cached;
    public static LevelConfig Get()
    {
        if (cached == null)
            cached = Resources.Load<LevelConfig>("LevelConfig");
        return cached;
    }

    public static int TotalLevels
    {
        get
        {
            var c = Get();
            return (c != null) ? c.levels.Count : 0;
        }
    }

    public static int ChapterCount
    {
        get
        {
            int t = TotalLevels;
            if (t <= 0) return 0;
            return (t - 1) / LevelsPerChapter + 1;
        }
    }

    public static int GetChapter(int level)
    {
        return (level - 1) / LevelsPerChapter;
    }

    public static string GetChapterAddress(int chapter)
    {
        var c = Get();
        if (c == null || chapter < 0 || chapter >= c.chapterAddresses.Count) return "";
        return c.chapterAddresses[chapter];
    }

    public static Entry GetEntry(int level)
    {
        var c = Get();
        if (c == null || c.levels.Count == 0) return null;
        int i = Mathf.Clamp(level - 1, 0, c.levels.Count - 1);
        return c.levels[i];
    }
}