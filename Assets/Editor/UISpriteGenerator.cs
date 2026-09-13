using UnityEngine;
using UnityEditor;
using System.IO;

public static class UISpriteGenerator
{
    private const int Size = 512;
    private const int Super = 4;

    [MenuItem("Tools/Generate UI Sprites")]
    public static void Generate()
    {
        string dir = "Assets/Art/UI";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        SaveSprite(MakeCircle(), dir + "/Circle_256.png", 256);
        SaveSprite(MakeRounded(128f), dir + "/Rounded_256.png", 128);
        SaveSprite(MakeRounded(48f), dir + "/Rounded24_256.png", 48);

        for (int m = 0; m < 16; m++)
        {
            bool tl = (m & 1) != 0;
            bool tr = (m & 2) != 0;
            bool br = (m & 4) != 0;
            bool bl = (m & 8) != 0;
            SaveSprite(MakeCorners(24f, tl, tr, br, bl), dir + "/RC_" + m.ToString("D2") + ".png", 0);
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("UI Sprites", "Sprites generated at 512px (crisp).", "OK");
    }

    private static float Dist(float x, float y, float cx, float cy)
    {
        float dx = x - cx;
        float dy = y - cy;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    private static bool Inside(float qx, float qy, float r, bool tl, bool tr, bool br, bool bl)
    {
        if (qx < r && qy > Size - r) return !tl || Dist(qx, qy, r, Size - r) <= r;
        if (qx > Size - r && qy > Size - r) return !tr || Dist(qx, qy, Size - r, Size - r) <= r;
        if (qx > Size - r && qy < r) return !br || Dist(qx, qy, Size - r, r) <= r;
        if (qx < r && qy < r) return !bl || Dist(qx, qy, r, r) <= r;
        return true;
    }

    private static Color[] MakeCorners(float r, bool tl, bool tr, bool br, bool bl)
    {
        Color[] px = new Color[Size * Size];
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float a = 0f;
                for (int sy = 0; sy < Super; sy++)
                    for (int sx = 0; sx < Super; sx++)
                    {
                        float qx = x + (sx + 0.5f) / Super;
                        float qy = y + (sy + 0.5f) / Super;
                        if (Inside(qx, qy, r, tl, tr, br, bl)) a += 1f;
                    }
                a /= Super * Super;
                px[y * Size + x] = new Color(1f, 1f, 1f, a);
            }
        return px;
    }

    private static Color[] MakeCircle()
    {
        Color[] px = new Color[Size * Size];
        float c = Size * 0.5f;
        float rad = c - 2f;
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float a = 0f;
                for (int sy = 0; sy < Super; sy++)
                    for (int sx = 0; sx < Super; sx++)
                    {
                        float qx = x + (sx + 0.5f) / Super;
                        float qy = y + (sy + 0.5f) / Super;
                        if (Dist(qx, qy, c, c) <= rad) a += 1f;
                    }
                a /= Super * Super;
                px[y * Size + x] = new Color(1f, 1f, 1f, a);
            }
        return px;
    }

    private static Color[] MakeRounded(float r)
    {
        return MakeCorners(r, true, true, true, true);
    }

    private static void SaveSprite(Color[] pixels, string path, int border)
    {
        Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.filterMode = FilterMode.Bilinear;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.maxTextureSize = 512;
        imp.spriteBorder = new Vector4(border, border, border, border);
        imp.SaveAndReimport();
    }
}