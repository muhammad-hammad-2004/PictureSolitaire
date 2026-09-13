using UnityEngine;
using UnityEditor;

public static class DebugSaveTools
{
    [MenuItem("Tools/Testing/Unlock All Levels")]
    public static void UnlockAll()
    {
        int total = LevelConfig.TotalLevels;
        for (int i = 1; i <= total; i++)
            PlayerPrefs.SetInt("LevelCompleted_" + i, 1);
        PlayerPrefs.SetInt("CurrentLevel", total + 1);
        PlayerPrefs.Save();
        Debug.Log("✅ All " + total + " levels completed!");
    }

    [MenuItem("Tools/Testing/Complete First 24 Levels")]
    public static void Complete24()
    {
        for (int i = 1; i <= 24; i++)
            PlayerPrefs.SetInt("LevelCompleted_" + i, 1);
        PlayerPrefs.SetInt("CurrentLevel", 25);
        PlayerPrefs.Save();
        Debug.Log("✅ Levels 1-24 completed → CurrentLevel = 25");
    }

    [MenuItem("Tools/Testing/Complete First 25 Levels")]
    public static void Complete25()
    {
        for (int i = 1; i <= 25; i++)
            PlayerPrefs.SetInt("LevelCompleted_" + i, 1);
        PlayerPrefs.SetInt("CurrentLevel", 26);
        PlayerPrefs.Save();
        Debug.Log("✅ Levels 1-25 completed → CurrentLevel = 26");
    }

    [MenuItem("Tools/Testing/Complete First 29 Levels")]
    public static void Complete29()
    {
        for (int i = 1; i <= 29; i++)
            PlayerPrefs.SetInt("LevelCompleted_" + i, 1);
        PlayerPrefs.SetInt("CurrentLevel", 30);
        PlayerPrefs.Save();
        Debug.Log("✅ Levels 1-29 completed → CurrentLevel = 30");
    }


    [MenuItem("Tools/Testing/Complete First 50 Levels")]
    public static void Complete50()
    {
        for (int i = 1; i <= 50; i++)
            PlayerPrefs.SetInt("LevelCompleted_" + i, 1);
        PlayerPrefs.SetInt("CurrentLevel", 51);
        PlayerPrefs.Save();
        Debug.Log("✅ Levels 1-50 completed → CurrentLevel = 51");
    }

    [MenuItem("Tools/Testing/Reset Save")]
    public static void ResetSave()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("🔄 Save reset — fresh start!");
    }

}

public class JumpWindow : EditorWindow
{
    private int lvl = 1;

    [MenuItem("Tools/Testing/Jump To Level...")]
    private static void Open() { GetWindow<JumpWindow>("Jump To Level"); }

    private void OnGUI()
    {
        lvl = EditorGUILayout.IntField("Current Level", lvl);
        if (GUILayout.Button("Set"))
        {
            PlayerPrefs.SetInt("CurrentLevel", Mathf.Max(1, lvl));
            PlayerPrefs.Save();
            Debug.Log("✅ CurrentLevel = " + lvl);
        }
    }
}