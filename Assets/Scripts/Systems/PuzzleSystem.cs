using UnityEngine;

public static class PuzzleSystem
{
    public static Sprite[] SliceTexture(Texture2D source, int rows, int cols, float cornerRadius = 0f, int bleed = 0)
    {
        int cw = source.width / cols;
        int ch = source.height / rows;
        Sprite[] sprites = new Sprite[rows * cols];

        Color[] full = bleed > 0 ? source.GetPixels() : null;
        int sw = source.width;
        int sh = source.height;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int index = row * cols + col;
                int x = col * cw;
                int y = sh - (row + 1) * ch;

                Texture2D cell;

                if (bleed > 0)
                {
                    int w = cw + bleed * 2;
                    int h = ch + bleed * 2;
                    Color[] dst = new Color[w * h];

                    for (int py = 0; py < h; py++)
                    {
                        int sy = Mathf.Clamp(y + py - bleed, 0, sh - 1);
                        for (int px = 0; px < w; px++)
                        {
                            int sx = Mathf.Clamp(x + px - bleed, 0, sw - 1);
                            dst[py * w + px] = full[sy * sw + sx];
                        }
                    }

                    cell = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    cell.SetPixels(dst);
                    cell.Apply();
                }
                else
                {
                    cell = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
                    cell.SetPixels(source.GetPixels(x, y, cw, ch));
                    cell.Apply();
                }

                if (cornerRadius > 0f)
                    cell = ApplyRoundedAlpha(cell, cornerRadius);

                sprites[index] = Sprite.Create(cell, new Rect(0, 0, cell.width, cell.height), new Vector2(0.5f, 0.5f));
            }
        }

        return sprites;
    }

    public static Vector2 FitSize(float containerWidth, float containerHeight, float aspect)
    {
        float w = containerWidth;
        float h = w / aspect;

        if (h > containerHeight)
        {
            h = containerHeight;
            w = h * aspect;
        }

        return new Vector2(w, h);
    }

    public static Texture2D CenterCrop(Texture2D source, float targetAspect)
    {
        int sw = source.width, sh = source.height;
        int cw, ch;

        if (sw / (float)sh > targetAspect)
        {
            ch = sh;
            cw = Mathf.RoundToInt(sh * targetAspect);
        }
        else
        {
            cw = sw;
            ch = Mathf.RoundToInt(sw / targetAspect);
        }

        int x = (sw - cw) / 2;
        int y = (sh - ch) / 2;

        Texture2D result = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
        result.SetPixels(source.GetPixels(x, y, cw, ch));
        result.Apply();
        return result;
    }

    public static Sprite MakeThumb(Texture2D src, int w, int h, float radius)
    {
        RenderTexture rt = RenderTexture.GetTemporary(w, h);
        Graphics.Blit(src, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D small = new Texture2D(w, h, TextureFormat.RGBA32, false);
        small.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        small.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        if (radius > 0f)
            small = ApplyRoundedAlpha(small, radius);

        return Sprite.Create(small, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    private static float Dist(float x, float y, float cx, float cy)
    {
        float dx = x - cx;
        float dy = y - cy;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    private static bool InsideRounded(float x, float y, float w, float h, float r)
    {
        if (x < r && y > h - r) return Dist(x, y, r, h - r) <= r;
        if (x > w - r && y > h - r) return Dist(x, y, w - r, h - r) <= r;
        if (x > w - r && y < r) return Dist(x, y, w - r, r) <= r;
        if (x < r && y < r) return Dist(x, y, r, r) <= r;
        return true;
    }

    public static Texture2D ApplyRoundedAlpha(Texture2D tex, float radius, int super = 2)
    {
        int w = tex.width, h = tex.height;
        Color[] px = tex.GetPixels();
        Color[] outPx = new Color[px.Length];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float a = 0f;
                for (int sy = 0; sy < super; sy++)
                    for (int sx = 0; sx < super; sx++)
                    {
                        float qx = x + (sx + 0.5f) / super;
                        float qy = y + (sy + 0.5f) / super;
                        if (InsideRounded(qx, qy, w, h, radius)) a += 1f;
                    }
                a /= super * super;

                Color c = px[y * w + x];
                c.a *= a;
                outPx[y * w + x] = c;
            }
        }

        Texture2D result = new Texture2D(w, h, TextureFormat.RGBA32, false);
        result.SetPixels(outPx);
        result.Apply();
        return result;
    }
}