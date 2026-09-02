using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [Header("Health Bar (10 squircles + numeric)")]
    public int squircleSize = 26;
    public int gap = 5;
    public int hotbarSpacing = 26;
    public int textureSize = 32;
    public Color fillColor = new Color(0.9f, 0.18f, 0.14f);
    public Color emptyColor = new Color(0.42f, 0.42f, 0.42f);
    public Color outlineColor = Color.black;

    Texture2D fillTex;
    Texture2D emptyTex;
    Texture2D halfTex;
    GUIStyle numberStyle;

    void Awake()
    {
        Rebuild();
    }

    void Rebuild()
    {
        fillTex = BuildShape(fillColor);
        emptyTex = BuildShape(emptyColor);
        halfTex = BuildSplitShape(fillColor, emptyColor);
    }

    Texture2D BuildShape(Color interior)
    {
        int size = Mathf.Max(8, textureSize);
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float half = size * 0.5f;
        float r = half * 0.25f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - Vector2.one * half;
                Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - Vector2.one * (half - r);
                float dist = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude
                           + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;

                float dd = -dist;
                float cov = Mathf.SmoothStep(-1.5f, 1.5f, dd);
                float core = Mathf.SmoothStep(-1.5f, 1.5f, dd - 3f);

                Color col = Color.clear;
                if (cov > 0f)
                {
                    Color inner = core > 0f ? interior : outlineColor;
                    float a = core + (cov - core) * 0.85f;
                    col = new Color(inner.r, inner.g, inner.b, Mathf.Clamp01(a));
                }

                pixels[y * size + x] = col;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    Texture2D BuildSplitShape(Color lowerLeft, Color upperRight)
    {
        int size = Mathf.Max(8, textureSize);
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float half = size * 0.5f;
        float r = half * 0.25f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - Vector2.one * half;
                Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - Vector2.one * (half - r);
                float dist = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude
                           + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;

                float dd = -dist;
                float cov = Mathf.SmoothStep(-1.5f, 1.5f, dd);
                float core = Mathf.SmoothStep(-1.5f, 1.5f, dd - 3f);

                bool isUpperRight = p.x + p.y >= 0f;
                Color interior = isUpperRight ? upperRight : lowerLeft;

                Color col = Color.clear;
                if (cov > 0f)
                {
                    Color inner = core > 0f ? interior : outlineColor;
                    float a = core + (cov - core) * 0.85f;
                    col = new Color(inner.r, inner.g, inner.b, Mathf.Clamp01(a));
                }

                pixels[y * size + x] = col;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    void OnGUI()
    {
        if (fillTex == null || emptyTex == null || halfTex == null) return;
        // Never inherit GUI.color from another script's OnGUI (it is static).
        GUI.color = Color.white;
        if (numberStyle == null)
        {
            numberStyle = new GUIStyle(GUI.skin.label);
            numberStyle.fontSize = Mathf.Max(10, squircleSize);
            numberStyle.alignment = TextAnchor.MiddleCenter;
            numberStyle.fontStyle = FontStyle.Bold;
            numberStyle.normal.textColor = Color.white;
        }

        Player player = GetComponent<Player>();
        float health = player != null ? player.health : 10f;
        health = Mathf.Clamp(health, 0f, 10f);

        float rounded = Mathf.Round(health * 2f) / 2f;
        int filled = Mathf.FloorToInt(rounded);
        float remainder = rounded - filled;

        float centerX = Screen.width / 2f;
        float totalW = squircleSize * 10f + gap * 9f;
        float x = centerX - totalW / 2f;

        // Slide up in sync with the inventory's own animation so it doesn't teleport.
        float shift = 0f;
        InventoryUI inv = GetComponent<InventoryUI>();
        if (inv != null)
            shift = inv.InventoryHeight * inv.OpenProgress;

        // Rest above the hotbar's tallest slot (slotSize), aligned to its top.
        int hotbarSlotSize = inv != null ? inv.SlotSizeForBar : 72;
        float bottom = Screen.height - 10f - hotbarSlotSize - hotbarSpacing - shift;
        float top = bottom - squircleSize;

        for (int i = 0; i < 10; i++)
        {
            Rect cell = new Rect(x, top, squircleSize, squircleSize);
            Texture2D tex = i < filled
                ? fillTex
                : (i == filled && remainder > 0.01f ? halfTex : emptyTex);
            GUI.DrawTexture(cell, tex);
            x += squircleSize + gap;
        }

        string value = rounded.ToString("F1");
        float labelX = centerX + totalW / 2f + 8f;
        Rect labelRect = new Rect(labelX, top, 70f, squircleSize);
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.Label(new Rect(labelRect.x + 1f, labelRect.y + 1f, labelRect.width, labelRect.height), value, numberStyle);
        GUI.color = Color.white;
        GUI.Label(labelRect, value, numberStyle);
    }
}

public class Stats : MonoBehaviour
{
    [Header("Stats (right-bottom)")]
    public int iconSize = 26;
    public int valueFontSize = 26;
    public int marginLeft = 16;
    public int marginBottom = 14;
    public int rowGap = 6;
    public int iconValueGap = 6;
    public Color armorColor = new Color(0.3f, 0.62f, 1f);
    public Color staminaColor = new Color(0.35f, 0.8f, 0.35f);
    public Color xpColor = new Color(1f, 0.85f, 0.2f);
    public int textureSize = 32;

    Texture2D shieldTex;
    Texture2D leafTex;
    Texture2D starTex;
    GUIStyle valueStyle;

    void Awake()
    {
        Rebuild();
    }

    void Rebuild()
    {
        starTex = BuildIcon(xpColor, IconKind.Star);
        shieldTex = BuildIcon(armorColor, IconKind.Shield);
        leafTex = BuildIcon(staminaColor, IconKind.Leaf);
    }

    enum IconKind { Shield, Leaf, Star }

    Texture2D BuildIcon(Color tint, IconKind kind)
    {
        int size = Mathf.Max(8, textureSize);
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float half = size * 0.5f;
        float w = half * 0.55f;
        float h = half * 0.9f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - Vector2.one * half;
                bool inside = false;

                switch (kind)
                {
                    case IconKind.Shield: inside = Shield(p, w, h); break;
                    case IconKind.Leaf:   inside = Leaf(p, w, h);   break;
                    case IconKind.Star:   inside = Star(p, w, h);   break;
                }

                pixels[y * size + x] = inside ? tint : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    static bool Shield(Vector2 p, float w, float h)
    {
        if (p.y < -h * 0.5f || p.y > h * 0.5f) return false;
        float t = Mathf.Clamp01((p.y + h * 0.5f) / h);
        float hw = Mathf.Max(1f, w * (1f - t * t));
        return Mathf.Abs(p.x) <= hw;
    }

    static bool Leaf(Vector2 p, float w, float h)
    {
        float ca = Mathf.Cos(-0.55f);
        float sa = Mathf.Sin(-0.55f);
        Vector2 rp = new Vector2(p.x * ca - p.y * sa, p.x * sa + p.y * ca);
        float dx = rp.x / w;
        float dy = rp.y / h;
        if (dx * dx + dy * dy <= 1f) return true;
        return Mathf.Abs(p.x + w * 0.4f) < 1.2f && Mathf.Abs(p.y) <= h * 0.55f;
    }

    static bool Star(Vector2 p, float w, float h)
    {
        float outer = w;
        float inner = outer * 0.45f;
        float ang = Mathf.Atan2(p.y, p.x);
        float sector = Mathf.Deg2Rad * 72f;
        float t = (ang % sector + sector) % sector;
        if (t > sector * 0.5f) t = sector - t;
        float r = outer - (outer - inner) * (t / (sector * 0.5f));
        return p.magnitude <= r;
    }

    void OnGUI()
    {
        if (shieldTex == null || leafTex == null || starTex == null) return;
        // Never inherit GUI.color from another script's OnGUI (it is static).
        GUI.color = Color.white;
        if (valueStyle == null)
        {
            valueStyle = new GUIStyle(GUI.skin.label);
            valueStyle.fontSize = valueFontSize;
            valueStyle.alignment = TextAnchor.MiddleLeft;
            valueStyle.fontStyle = FontStyle.Bold;
        }

        Player player = GetComponent<Player>();
        string armorValue = player != null ? player.armor.ToString("F1") : "10.0";
        string staminaValue = player != null ? player.stamina.ToString("F1") : "10.0";
        string xpValue = player != null ? player.xp.ToString() : "0";

        DrawRow(starTex, xpColor, xpValue, 0);
        // DrawRow(shieldTex, armorColor, armorValue, 1); // shield commented out for now
        DrawRow(leafTex, staminaColor, staminaValue, 1);
        
    }

    void DrawRow(Texture2D icon, Color tint, string value, int index)
    {
        float rowHeight = iconSize;
        float baseY = Screen.height - marginBottom - rowHeight - index * (rowHeight + rowGap);

        // Position the stats columns just to the right of the hotbar.
        float hotbarRight = HotbarRightEdge();
        float colX = hotbarRight + marginLeft;

        valueStyle.alignment = TextAnchor.MiddleLeft;

        // Icon on the left, then the value text (left-aligned) to its right.
        float iconX = colX;
        float textX = iconX + iconSize + iconValueGap;

        GUI.color = tint;
        GUI.DrawTexture(new Rect(iconX, baseY, iconSize, iconSize), icon);
        GUI.color = Color.white;

        Rect labelRect = new Rect(textX, baseY, 80f, rowHeight);
        valueStyle.normal.textColor = Color.white;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.Label(new Rect(labelRect.x + 1f, labelRect.y + 1f, labelRect.width, labelRect.height), value, valueStyle);
        GUI.color = Color.white;
        GUI.Label(labelRect, value, valueStyle);
    }

    float HotbarRightEdge()
    {
        Hotbar hotbar = GetComponent<Hotbar>();
        float slot = hotbar != null ? hotbar.slotSize : 48f;
        float center = hotbar != null ? hotbar.centerSize : 58f;
        float gap = hotbar != null ? hotbar.gap : 6f;

        float leftW = slot * 4f + gap * 3f;
        float rightW = slot * 4f + gap * 3f;
        float totalW = leftW + center + rightW + gap * 2f;

        return Screen.width / 2f + totalW / 2f;
    }
}