using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    private const string KeyCurrentLevel = "CurrentLevel";
    private const string KeyCompletedPrefix = "LevelCompleted_";
    private const string KeyHints = "Hints";
    private const string KeyDailyPrefix = "DailyDone_";
    private const string KeySound = "SoundOn";
    private const string KeyMusic = "MusicOn";
    private const string KeyVibration = "VibrationOn";

    public int CurrentLevel => PlayerPrefs.GetInt(KeyCurrentLevel, 1);
    public int Hints => PlayerPrefs.GetInt(KeyHints, 5);
    public bool SoundOn => PlayerPrefs.GetInt(KeySound, 1) == 1;
    public bool MusicOn => PlayerPrefs.GetInt(KeyMusic, 1) == 1;
    public bool VibrationOn => PlayerPrefs.GetInt(KeyVibration, 1) == 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsLevelCompleted(int level)
    {
        return PlayerPrefs.GetInt(KeyCompletedPrefix + level, 0) == 1;
    }

    public void CompleteLevel(int level)
    {
        PlayerPrefs.SetInt(KeyCompletedPrefix + level, 1);
        if (level >= CurrentLevel)
        {
            PlayerPrefs.SetInt(KeyCurrentLevel, level + 1);
        }
        PlayerPrefs.Save();
    }

    public void SetHints(int value)
    {
        PlayerPrefs.SetInt(KeyHints, Mathf.Max(0, value));
        PlayerPrefs.Save();
    }

    public bool IsDailyCompleted(string dateKey)
    {
        return PlayerPrefs.GetInt(KeyDailyPrefix + dateKey, 0) == 1;
    }

    public void CompleteDaily(string dateKey)
    {
        PlayerPrefs.SetInt(KeyDailyPrefix + dateKey, 1);
        PlayerPrefs.Save();
    }

    public void SetSound(bool on) { PlayerPrefs.SetInt(KeySound, on ? 1 : 0); PlayerPrefs.Save(); }
    public void SetMusic(bool on) { PlayerPrefs.SetInt(KeyMusic, on ? 1 : 0); PlayerPrefs.Save(); }
    public void SetVibration(bool on) { PlayerPrefs.SetInt(KeyVibration, on ? 1 : 0); PlayerPrefs.Save(); }


        public bool AllLevelsCompleted()
    {
        int total = LevelConfig.TotalLevels;
        if (total <= 0) return false;
        for (int i = 1; i <= total; i++)
        {
            if (!IsLevelCompleted(i)) return false;
        }
        return true;
    }

    public void SetCurrentLevel(int level)
    {
        PlayerPrefs.SetInt(KeyCurrentLevel, Mathf.Max(1, level));
        PlayerPrefs.Save();
    }
}