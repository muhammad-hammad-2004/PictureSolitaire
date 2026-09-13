using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class LevelManagerWindow : EditorWindow
{
    private const string Path = "Assets/Resources/LevelConfig.asset";
    private LevelConfig config;
    private Vector2 scroll;

    private int totalField = -1;
    private int pendingTrim = -1;

    private int dragIndex = -1;
    private bool dragLifted;
    private Vector2 dragStartMouse;
    private int hoverIndex = -1;
    private readonly List<Rect> controlRects = new List<Rect>();

    [MenuItem("Tools/Picture Solitaire/Level Manager")]
    private static void Open() { GetWindow<LevelManagerWindow>("Level Manager"); }

    private void OnEnable() { Load(); }

    private void Load()
    {
        config = AssetDatabase.LoadAssetAtPath<LevelConfig>(Path);
        if (config == null)
        {
            Directory.CreateDirectory("Assets/Resources");
            config = CreateInstance<LevelConfig>();
            AssetDatabase.CreateAsset(config, Path);
            AssetDatabase.SaveAssets();
        }
        totalField = config.levels.Count;
        pendingTrim = -1;
    }

    private void OnGUI()
    {
        if (config == null) Load();
        hoverIndex = -1;

        // ===== TOP: Total Levels (SAFE) =====
        totalField = EditorGUILayout.IntField("Total Levels", totalField);
        totalField = Mathf.Clamp(totalField, 0, 10000);

        if (totalField > config.levels.Count)
        {
            pendingTrim = -1;
            Grow(totalField);
        }
        else if (totalField < config.levels.Count)
        {
            pendingTrim = totalField;
        }

        if (pendingTrim >= 0 && pendingTrim < config.levels.Count)
        {
            EditorGUILayout.HelpBox("WARNING: Level " + (pendingTrim + 1) + " to " + config.levels.Count +
                " (with their images) will be DELETED. Confirm or Cancel.", MessageType.Warning);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Confirm Delete"))
                ApplyTrim();
            if (GUILayout.Button("Cancel"))
            {
                pendingTrim = -1;
                totalField = config.levels.Count;
            }
            EditorGUILayout.EndHorizontal();
        }

        // ===== NEW: Auto-Assign Difficulty =====
        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "Auto-assign pattern:\n" +
            "• Levels 1-5: Easy (3x3)\n" +
            "• Level 6: Medium (4x3)\n" +
            "• Levels 7-10: Easy (3x3)\n" +
            "• Level 11: Medium (3x4)\n" +
            "• Level 12: Hard (6x5)\n" +
            "• Then cycle: 3 Easy → 1 Medium → 1 Hard",
            MessageType.Info);
        if (GUILayout.Button("Auto-Assign Difficulty"))
        {
            AutoAssignDifficulty();
            Repaint();
        }
        EditorGUILayout.Space(4);
        // ===== END NEW =====

        // ===== TOP: Chapter Reveal Images =====
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Chapter Reveal Images", EditorStyles.boldLabel);

        int chapters = Mathf.CeilToInt(config.levels.Count / 25f);
        SyncChapters(chapters);

        if (chapters == 0)
            EditorGUILayout.HelpBox("Set Total Levels — chapter reveal image slots will appear here.", MessageType.Info);

        for (int i = 0; i < chapters; i++)
        {
            int start = i * 25 + 1;
            int end = Mathf.Min(config.levels.Count, (i + 1) * 25);
            config.chapterImages[i] = (Texture2D)EditorGUILayout.ObjectField(
                "Chapter " + (i + 1) + "  (Level " + start + "-" + end + ")",
                config.chapterImages[i], typeof(Texture2D), false);
        }

        EditorGUILayout.Space(6);

        // ===== LEVELS (scroll + block drag) =====
        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < config.levels.Count; i++)
            DrawLevelBox(i);
        EditorGUILayout.EndScrollView();

        Event ev = Event.current;

        if (dragIndex >= 0 && dragLifted)
        {
            Rect tag = new Rect(ev.mousePosition.x + 14, ev.mousePosition.y - 12, 76, 20);
            EditorGUI.DrawRect(tag, new Color(0.15f, 0.55f, 1f, 0.95f));
            GUI.Label(tag, "Level " + (dragIndex + 1), EditorStyles.whiteBoldLabel);
        }

        if (dragIndex >= 0)
        {
            if (ev.type == EventType.MouseDrag) Repaint();

            if (ev.type == EventType.MouseUp)
            {
                if (dragLifted && hoverIndex >= 0 && hoverIndex != dragIndex)
                {
                    var tmp = config.levels[dragIndex];
                    config.levels[dragIndex] = config.levels[hoverIndex];
                    config.levels[hoverIndex] = tmp;
                    Save();
                }
                dragIndex = -1;
                dragLifted = false;
                hoverIndex = -1;
                Repaint();
            }

            if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Escape)
            {
                dragIndex = -1;
                dragLifted = false;
            }
        }

        if (GUI.changed) Save();
    }

    private bool OverControl(Vector2 p)
    {
        for (int k = 0; k < controlRects.Count; k++)
            if (controlRects[k].Contains(p)) return true;
        return false;
    }

    private void DrawLevelBox(int i)
    {
        var e = config.levels[i];
        controlRects.Clear();

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Level " + (i + 1), EditorStyles.boldLabel);
        if (GUILayout.Button("X", GUILayout.Width(22)))
        {
            config.levels.RemoveAt(i);
            dragIndex = -1;
            dragLifted = false;
            totalField = config.levels.Count;
            Save();
            GUIUtility.ExitGUI();
        }
        controlRects.Add(GUILayoutUtility.GetLastRect());
        EditorGUILayout.EndHorizontal();

        e.image = (Texture2D)EditorGUILayout.ObjectField("Image", e.image, typeof(Texture2D), false);
        controlRects.Add(GUILayoutUtility.GetLastRect());

        e.rows = Mathf.Max(1, EditorGUILayout.IntField("Rows", e.rows));
        controlRects.Add(GUILayoutUtility.GetLastRect());

        e.cols = Mathf.Max(1, EditorGUILayout.IntField("Cols", e.cols));
        controlRects.Add(GUILayoutUtility.GetLastRect());

        EditorGUILayout.EndVertical();
        Rect boxRect = GUILayoutUtility.GetLastRect();

        Event ev = Event.current;

        EditorGUIUtility.AddCursorRect(boxRect, MouseCursor.MoveArrow);

        if (ev.type == EventType.MouseDown && boxRect.Contains(ev.mousePosition) && !OverControl(ev.mousePosition))
        {
            dragIndex = i;
            dragLifted = false;
            dragStartMouse = ev.mousePosition;
            ev.Use();
        }

        if (dragIndex == i && !dragLifted && (ev.mousePosition - dragStartMouse).magnitude > 6f)
            dragLifted = true;

        if (dragIndex == i && dragLifted)
            EditorGUI.DrawRect(boxRect, new Color(0.15f, 0.55f, 1f, 0.20f));

        if (dragLifted && dragIndex >= 0 && dragIndex != i && boxRect.Contains(ev.mousePosition))
        {
            hoverIndex = i;
            EditorGUI.DrawRect(boxRect, new Color(0.2f, 0.9f, 0.4f, 0.30f));
        }
    }

    private void Grow(int total)
    {
        while (config.levels.Count < total) config.levels.Add(new LevelConfig.Entry());
        SyncChapters(Mathf.CeilToInt(config.levels.Count / 25f));
        Save();
    }

    private void ApplyTrim()
    {
        while (config.levels.Count > pendingTrim) config.levels.RemoveAt(config.levels.Count - 1);
        SyncChapters(Mathf.CeilToInt(config.levels.Count / 25f));
        Save();
        totalField = config.levels.Count;
        pendingTrim = -1;
    }

    private void SyncChapters(int needed)
    {
        while (config.chapterImages.Count < needed) config.chapterImages.Add(null);
        while (config.chapterImages.Count > needed) config.chapterImages.RemoveAt(config.chapterImages.Count - 1);
    }

    private void Save()
    {
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
    }

    // ===== NEW: Progressive Difficulty Assignment =====
    private void AutoAssignDifficulty()
    {
        if (config == null || config.levels.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No levels to assign!", "OK");
            return;
        }

        // Pattern definition: (difficulty, count, rows, cols)
        var pattern = new System.ValueTuple<string, int, int, int>[]
        {
            ("Easy", 5, 3, 3),      // Levels 1-5
            ("Medium", 1, 4, 3),    // Level 6
            ("Easy", 4, 3, 3),      // Levels 7-10
            ("Medium", 1, 3, 4),    // Level 11
            ("Hard", 1, 6, 5),      // Level 12
            ("Easy", 3, 3, 3),      // Levels 13-15
            ("Medium", 1, 4, 3),    // Level 16
            ("Hard", 1, 6, 5),      // Level 17
            ("Easy", 3, 3, 3),      // Levels 18-20
            ("Medium", 1, 3, 4),    // Level 21
            ("Hard", 1, 6, 5),      // Level 22
        };

        int levelIndex = 0;
        foreach (var p in pattern)
        {
            for (int i = 0; i < p.Item2 && levelIndex < config.levels.Count; i++)
            {
                config.levels[levelIndex].rows = p.Item3;
                config.levels[levelIndex].cols = p.Item4;
                levelIndex++;
            }
        }

        // Remaining levels: cycle Easy → Medium → Hard
        while (levelIndex < config.levels.Count)
        {
            // Easy (3 levels)
            for (int i = 0; i < 3 && levelIndex < config.levels.Count; i++)
            {
                config.levels[levelIndex].rows = 3;
                config.levels[levelIndex].cols = 3;
                levelIndex++;
            }

            if (levelIndex >= config.levels.Count) break;

            // Medium (1 level)
            config.levels[levelIndex].rows = 4;
            config.levels[levelIndex].cols = 3;
            levelIndex++;

            if (levelIndex >= config.levels.Count) break;

            // Hard (1 level)
            config.levels[levelIndex].rows = 6;
            config.levels[levelIndex].cols = 5;
            levelIndex++;
        }

        Save();
        EditorUtility.DisplayDialog("Done",
            "Assigned progressive difficulty to all " + config.levels.Count + " levels.\n\n" +
            "Curve:\n" +
            "• Levels 1-5: Easy (3x3) — 9 pieces\n" +
            "• Level 6: Medium (4x3) — 12 pieces\n" +
            "• Levels 7-10: Easy (3x3) — 9 pieces\n" +
            "• Level 11: Medium (3x4) — 12 pieces\n" +
            "• Level 12: Hard (6x5) — 30 pieces\n" +
            "• Then cycle: 3 Easy → 1 Medium → 1 Hard\n\n" +
            "Player ko warm-up time milega!",
            "OK");
    }
}