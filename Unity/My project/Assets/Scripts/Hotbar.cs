using UnityEngine;

public class Hotbar : MonoBehaviour
{
    [Header("Hotbar (9 slots, diamond center)")]
    public int slotSize = 72;
    public int centerSize = 58;
    public int gap = 6;
    public int yFromBottom = 10;
    public int textureSize = 64;
    public float outlineAlpha = 0.9f;
    public float fillAlpha = 0.22f;
    public Color outlineColor = Color.white;
    public Color fillColor = Color.black;
    public Color selectedColor = new Color(1f, 0.84f, 0.3f);

    Texture2D slotTex;
    Texture2D diamondTex;
    Texture2D slotTexSelected;
    Texture2D diamondTexSelected;
    int selected = 4;

    SlotBlock[] slots = new SlotBlock[9];

    public int SelectedIndex => selected;

    public SlotBlock SelectedSlot
    {
        get => slots[selected];
        set => slots[selected] = value;
    }

    public SlotBlock GetSlot(int index) => slots[index];
    public void SetSlot(int index, SlotBlock b) => slots[index] = b;

    // Expose slot sizing + textures so the inventory can match the hotbar exactly.
    public Texture2D SlotTexture => slotTex;
    public Texture2D DiamondTexture => diamondTex;
    public Texture2D SlotTextureSelected => slotTexSelected;
    public Texture2D DiamondTextureSelected => diamondTexSelected;
    public int CovSlotSize => slotSize;
    public int CenterSize => centerSize;

    // Screen-space rect for a hotbar slot (same layout math as OnGUI).
    // Used so the inventory can accept clicks/drops on hotbar slots.
    public Rect GetSlotRect(int index)
    {
        float centerX = Screen.width / 2f;
        float bottom = Screen.height - yFromBottom;

        float leftW = slotSize * 4f + gap * 3f;
        float rightW = slotSize * 4f + gap * 3f;
        float totalW = leftW + centerSize + rightW + gap * 2f;

        float x = centerX - totalW / 2f;
        Rect rect = new Rect();
        for (int i = 0; i <= index; i++)
        {
            bool isCenter = i == 4;
            float size = isCenter ? centerSize : slotSize;
            float top = bottom - size;
            if (i == index)
                rect = new Rect(x, top, size, size);
            x += size + gap;
        }
        return rect;
    }

    void Awake()
    {
        Rebuild();
    }

    void Update()
    {
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                selected = i;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
            selected = (selected + 8) % 9;
        else if (scroll < 0f)
            selected = (selected + 1) % 9;
    }

    void Rebuild()
    {
        Color savedOutline = outlineColor;

        outlineColor = Color.black;
        slotTex = BuildShape(false);
        diamondTex = BuildShape(true);

        outlineColor = selectedColor;
        slotTexSelected = BuildShape(false);
        diamondTexSelected = BuildShape(true);

        outlineColor = savedOutline;
    }

    Texture2D BuildShape(bool diamond)
    {
        int size = Mathf.Max(16, textureSize);
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float half = size * 0.5f;
        float r = half * 0.22f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - Vector2.one * half;

                float dist;
                if (diamond)
                {
                    dist = Mathf.Abs(p.x) + Mathf.Abs(p.y) - half;
                }
                else
                {
                    Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - Vector2.one * (half - r);
                    dist = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude
                         + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
                }

                float dd = -dist;
                float cov = Mathf.SmoothStep(-1.5f, 1.5f, dd);
                float core = Mathf.SmoothStep(-1.5f, 1.5f, dd - 3f);

                float fillCov = core * fillAlpha;
                float borderCov = (cov - core) * outlineAlpha;
                float alpha = fillCov + borderCov;

                Color col = Color.clear;
                if (alpha > 0f)
                {
                    float t = fillCov > 0f && borderCov > 0f ? fillCov / (fillCov + borderCov) : 0f;
                    if (fillCov <= 0f) t = 0f;
                    if (borderCov <= 0f) t = 1f;
                    col = Color.Lerp(outlineColor, fillColor, t);
                    col.a = Mathf.Clamp01(alpha);
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
        if (slotTex == null || diamondTex == null || slotTexSelected == null || diamondTexSelected == null) return;

        // Never inherit GUI.color from another script's OnGUI (it is static).
        GUI.color = Color.white;

        float centerX = Screen.width / 2f;
        float bottom = Screen.height - yFromBottom;

        float leftW = slotSize * 4f + gap * 3f;
        float rightW = slotSize * 4f + gap * 3f;
        float totalW = leftW + centerSize + rightW + gap * 2f;

        float x = centerX - totalW / 2f;

        for (int i = 0; i < 9; i++)
        {
            bool isCenter = i == 4;
            float size = isCenter ? centerSize : slotSize;
            float top = bottom - size;
            bool highlighted = i == selected;

            Texture2D tex = isCenter
                ? (highlighted ? diamondTexSelected : diamondTex)
                : (highlighted ? slotTexSelected : slotTex);
            Rect rect = new Rect(x, top, size, size);
            GUI.DrawTexture(rect, tex);

            // Show the stored item inside the slot (matches the main grid icons).
            SlotBlock slot = slots[i];
            if (slot != null)
                InventoryUI.DrawBlockIcon(rect, slot);

            x += size + gap;
        }
    }
}