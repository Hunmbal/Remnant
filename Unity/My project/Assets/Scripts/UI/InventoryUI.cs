using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Shared Layout (matches Hotbar)")]
    public int slotSize = 72;
    public int gap = 6;
    public int hotbarSpacing = 4;
    public int centerSize = 58;
    public int hotbarYFromBottom = 10;

    [Header("Creative Panel (Practice, 4 rows x 9)")]
    public int creativeSlotSize = 64;
    public int creativeGap = 6;
    public int tabWidth = 100;
    public int tabHeight = 32;
    public int panelPadding = 20;
    public Color creativePanelBgColor = new Color(0.06f, 0.06f, 0.09f, 0.92f);

    [Header("Animation")]
    public float animDuration = 0.1f;

    [Header("Slot Styling (matches Hotbar)")]
    public float slotOutlineAlpha = 0.9f;
    public float slotFillAlpha = 0.22f;
    public Color slotOutlineColor = Color.black;
    public Color slotFillColor = Color.black;

    Texture2D creativeSlotTex;
    Texture2D slotBackTex;
    GUIStyle tabLabelStyle;
    GUIStyle clickButton;

    static Texture2D cubeTex;
    static GUIStyle stackCountStyle;

    bool isOpen;
    float animT = 0f;
    string creativeTab = "Blocks";

    Camera cam;
    Player player;
    Hotbar hotbar;

    // Slots are a single unified index space 0..28:
    //   0..8  = hotbar (owned/drawn by Hotbar)
    //   9..28 = main inventory (owned here)
    public const int HotbarSlots = 9;
    public const int InventorySlots = 20;
    public const int TotalSlots = HotbarSlots + InventorySlots; // 29

    SlotBlock[] mainSlots = new SlotBlock[InventorySlots];
    SlotBlock picked;

    public static bool IsOpenStatic = false;

    // Unified 0..28 access. Slots 0..8 map to the hotbar, 9..28 to main inventory.
    public SlotBlock GetSlot(int index)
    {
        if (index < HotbarSlots) return hotbar != null ? hotbar.GetSlot(index) : null;
        return mainSlots[index - HotbarSlots];
    }

    public void SetSlot(int index, SlotBlock item)
    {
        if (index < HotbarSlots) { if (hotbar != null) hotbar.SetSlot(index, item); return; }
        mainSlots[index - HotbarSlots] = item;
    }

    // --- Tooltip & Hover System ---
    SlotBlock hoveredItem;
    float hoverTimer = 0f;
    const float hoverDelay = 1.5f;
    GUIStyle tooltipStyle;
    Texture2D tooltipBgTex;

    void Awake()
    {
        cam = GetComponentInChildren<Camera>();
        if (cam == null) cam = Camera.main;
        player = GetComponent<Player>();
        hotbar = GetComponent<Hotbar>();

        if (hotbar == null)
        {
            hotbar = gameObject.AddComponent<Hotbar>();
        }

        BuildTextures();
    }

    void BuildTextures()
    {
        slotBackTex = BuildSlotShape(0.22f, 64, slotOutlineColor, slotFillColor, slotOutlineAlpha, slotFillAlpha);
        creativeSlotTex = BuildSlotShape(0.22f, Mathf.Max(16, creativeSlotSize), slotOutlineColor, slotFillColor, slotOutlineAlpha, slotFillAlpha);
        
        if (cubeTex == null)
            cubeTex = BuildCubeTexture(64);
    }

    static Texture2D BuildCubeTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Point;

        float cx = size * 0.5f;
        float cy = size * 0.5f;
        float u = size * 0.22f;
        float v = size * 0.24f;

        Vector2[] top = {
            new Vector2(cx, cy - u - v),
            new Vector2(cx + 2f * u, cy - v),
            new Vector2(cx, cy + u - v),
            new Vector2(cx - 2f * u, cy - v)
        };

        Vector2[] right = {
            new Vector2(cx + 2f * u, cy + v),
            new Vector2(cx + 2f * u, cy - v),
            new Vector2(cx, cy + u - v),
            new Vector2(cx, cy + u + v)
        };

        Vector2[] left = {
            new Vector2(cx - 2f * u, cy + v),
            new Vector2(cx, cy + u + v),
            new Vector2(cx, cy + u - v),
            new Vector2(cx - 2f * u, cy - v)
        };

        Color[] px = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);

                float shade = -1f;
                if (InQuad(p, top)) shade = 1.0f;
                else if (InQuad(p, right)) shade = 0.78f;
                else if (InQuad(p, left)) shade = 0.55f;

                Color col;
                if (shade > 0f)
                {
                    col = new Color(shade, shade, shade, 1f);
                }
                else if (DistToQuad(p, top, left, right) <= 1.0f)
                {
                    col = new Color(0f, 0f, 0f, 1f);
                }
                else
                {
                    col = new Color(0f, 0f, 0f, 0f);
                }

                px[y * size + x] = col;
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    static bool InQuad(Vector2 p, Vector2[] q)
    {
        bool pos = false, neg = false;
        for (int i = 0; i < 4; i++)
        {
            Vector2 a = q[i], b = q[(i + 1) % 4];
            float cr = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
            if (cr < 0f) neg = true;
            else if (cr > 0f) pos = true;
            if (pos && neg) return false;
        }
        return true;
    }

    static float DistToQuad(Vector2 p, params Vector2[][] quads)
    {
        float best = float.MaxValue;
        foreach (Vector2[] q in quads)
            for (int i = 0; i < 4; i++)
                best = Mathf.Min(best, DistToSeg(p, q[i], q[(i + 1) % 4]));
        return best;
    }

    static float DistToSeg(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float len2 = ab.sqrMagnitude;
        float t = len2 < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
        return (p - (a + ab * t)).magnitude;
    }

    public static void DrawBlockIcon(Rect rect, SlotBlock item)
    {
        if (item == null) return;
        if (cubeTex == null)
            cubeTex = BuildCubeTexture(64);

        float inset = rect.width * 0.14f;
        float s = rect.width - inset * 2f;
        Rect r = new Rect(rect.center.x - s * 0.5f, rect.center.y - s * 0.5f, s, s);

        Color c = item.GetColor();
        GUI.color = c;
        GUI.DrawTexture(r, cubeTex);
        GUI.color = Color.white;
    }

    // Draw the stack count in the bottom-right corner of a slot, but only when
    // the stack has more than one block (like Minecraft).
    public static void DrawStackCount(Rect rect, SlotBlock item)
    {
        if (item == null || item.count <= 1) return;

        if (stackCountStyle == null)
        {
            stackCountStyle = new GUIStyle(GUI.skin.label);
            stackCountStyle.fontSize = 16;
            stackCountStyle.fontStyle = FontStyle.Bold;
            stackCountStyle.alignment = TextAnchor.LowerRight;
            stackCountStyle.padding = new RectOffset(8, 6, 0, 4);
        }

        Rect labelRect = new Rect(rect.x, rect.y, rect.width, rect.height);

        // Drop-shadow for readability against any block color.
        stackCountStyle.normal.textColor = Color.black;
        GUI.Label(new Rect(labelRect.x + 1f, labelRect.y - 1f, labelRect.width, labelRect.height), item.count.ToString(), stackCountStyle);
        stackCountStyle.normal.textColor = Color.white;
        GUI.Label(labelRect, item.count.ToString(), stackCountStyle);
    }

    Texture2D BuildSlotShape(float radiusRatio, int texSize, Color outlineCol, Color fillCol, float outAlpha, float fillAlpha)
    {
        int size = Mathf.Max(16, texSize);
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float half = size * 0.5f;
        float r = half * radiusRatio;
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

                float fillCov = core * fillAlpha;
                float borderCov = (cov - core) * outAlpha;
                float alpha = fillCov + borderCov;

                Color col = Color.clear;
                if (alpha > 0f)
                {
                    float t = fillCov > 0f && borderCov > 0f ? fillCov / (fillCov + borderCov) : 0f;
                    if (fillCov <= 0f) t = 0f;
                    if (borderCov <= 0f) t = 1f;
                    col = Color.Lerp(outlineCol, fillCol, t);
                    col.a = Mathf.Clamp01(alpha);
                }

                pixels[y * size + x] = col;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    public bool IsOpen => isOpen;
    public int SlotSizeForBar => slotSize;
    public float OpenProgress => animT;
    public float InventoryHeight => 2f * slotSize + gap;

    void Update()
    {
        // While typing in chat, all game inputs are locked.
        if (Chat.IsLockingInput) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            isOpen = !isOpen;
            OnToggle();
        }
        else if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            isOpen = false;
            OnToggle();
        }

        IsOpenStatic = isOpen;

        float target = isOpen ? 1f : 0f;
        animT = Mathf.MoveTowards(animT, target, Time.unscaledDeltaTime / animDuration);
    }

    void OnToggle()
    {
        if (isOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (player != null) player.EnableLook(false);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (player != null) player.EnableLook(true);
            picked = null;
        }
    }

    void OnDestroy()
    {
        IsOpenStatic = false;
        if (isOpen)
            Cursor.lockState = CursorLockMode.Locked;
    }

    void OnGUI()
    {
        GUI.color = Color.white;
        if (animT <= 0.001f) return;

        Vector2 mousePos = Event.current.mousePosition;
        SlotBlock currentHover = null;

        // 1. Track Hover State
        if (isOpen)
        {
            currentHover = GetHoveredItem(mousePos);
            
            if (currentHover != null && SameItem(currentHover, hoveredItem))
            {
                hoverTimer += Time.unscaledDeltaTime;
            }
            else
            {
                hoveredItem = currentHover;
                hoverTimer = 0f;
            }
        }
        else
        {
            hoveredItem = null;
            hoverTimer = 0f;
        }

        // 2. Draw UI
        if (PlayerStateManager.IsPractice)
            DrawCreativePanel();

        DrawMainInventory();
        DrawHotbarTargets();

        if (picked != null)
            DrawPickedItem();

        // Drop a dragged item if the mouse is released outside any inventory slot.
        if (picked != null
            && Event.current.type == EventType.MouseUp
            && !IsOverAnySlot(mousePos))
        {
            DropPickedIntoWorld();
        }

        // 3. Draw Tooltip if hovered long enough
        if (hoverTimer >= hoverDelay && hoveredItem != null)
        {
            DrawTooltip(mousePos, hoveredItem);
        }
    }

    // True if the mouse is over any hotbar, main-inventory, or creative slot rect.
    bool IsOverAnySlot(Vector2 mp)
    {
        if (hotbar != null)
            for (int i = 0; i < 9; i++)
                if (hotbar.GetSlotRect(i).Contains(mp))
                    return true;

        float centerX = Screen.width / 2f;
        float cols = 10f;
        float totalW = cols * slotSize + (cols - 1f) * gap;
        float x = centerX - totalW / 2f;
        float hotbarBottom = Screen.height - hotbarYFromBottom;
        float hotbarTop = hotbarBottom - slotSize;
        float bottom = hotbarTop - hotbarSpacing + (1f - animT) * (slotSize + gap) * 1.5f;

        for (int row = 0; row < 2; row++)
        {
            float y = bottom - (row + 1) * (slotSize + gap);
            for (int col = 0; col < 10; col++)
            {
                Rect rect = new Rect(x + col * (slotSize + gap), y, slotSize, slotSize);
                if (rect.Contains(mp))
                    return true;
            }
        }

        if (PlayerStateManager.IsPractice)
        {
            int rows = 4, cCols = 9;
            float panelX = panelPadding + 20f;
            float panelY = panelPadding + 40f;
            float gridY = panelY + tabHeight + 15f;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cCols; c++)
                {
                    float slotX = panelX + c * (creativeSlotSize + creativeGap);
                    float slotY = gridY + r * (creativeSlotSize + creativeGap);
                    if (new Rect(slotX, slotY, creativeSlotSize, creativeSlotSize).Contains(mp))
                        return true;
                }
        }

        return false;
    }

    // Throw the dragged item out of the inventory into the world.
    void DropPickedIntoWorld()
    {
        if (picked == null) return;

        Vector3 pos = player != null ? player.DropAnchor() : transform.position;
        ArenaGenerator.DropItem(pos, picked);
        picked = null;
    }

    SlotBlock GetHoveredItem(Vector2 mousePos)
    {
        // Check Hotbar
        if (hotbar != null)
        {
            for (int i = 0; i < 9; i++)
            {
                if (hotbar.GetSlotRect(i).Contains(mousePos))
                    return hotbar.GetSlot(i);
            }
        }

        // Check Main Inventory
        float centerX = Screen.width / 2f;
        float cols = 10f;
        float totalW = cols * slotSize + (cols - 1f) * gap;
        float x = centerX - totalW / 2f;
        float hotbarBottom = Screen.height - hotbarYFromBottom;
        float hotbarTop = hotbarBottom - slotSize;
        float bottom = hotbarTop - hotbarSpacing + (1f - animT) * (slotSize + gap) * 1.5f;

        for (int row = 0; row < 2; row++)
        {
            float y = bottom - (row + 1) * (slotSize + gap);
            for (int col = 0; col < 10; col++)
            {
                int index = row * 10 + col;
                Rect rect = new Rect(x + col * (slotSize + gap), y, slotSize, slotSize);
                if (rect.Contains(mousePos))
                    return mainSlots[index];
            }
        }

        // Check Creative Panel
        if (PlayerStateManager.IsPractice)
        {
            int rows = 4, cCols = 9;
            float panelX = panelPadding + 20f;
            float panelY = panelPadding + 40f;
            float gridY = panelY + tabHeight + 15f;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cCols; c++)
                {
                    int index = r * cCols + c;
                    float slotX = panelX + c * (creativeSlotSize + creativeGap);
                    float slotY = gridY + r * (creativeSlotSize + creativeGap);
                    Rect rect = new Rect(slotX, slotY, creativeSlotSize, creativeSlotSize);
                    if (rect.Contains(mousePos))
                        return GetCreativeItem(index);
                }
            }
        }

        return null;
    }

    void DrawTooltip(Vector2 mousePos, SlotBlock item)
    {
        if (tooltipStyle == null)
        {
            tooltipStyle = new GUIStyle(GUI.skin.box);
            tooltipStyle.normal.textColor = Color.white;
            tooltipStyle.fontSize = 14;
            tooltipStyle.alignment = TextAnchor.MiddleLeft;
            tooltipStyle.padding = new RectOffset(12, 12, 8, 8);
            
            tooltipBgTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tooltipBgTex.SetPixel(0, 0, new Color(0.12f, 0.12f, 0.15f, 0.92f));
            tooltipBgTex.Apply();
            tooltipStyle.normal.background = tooltipBgTex;
        }

        string info = GetBlockInfo(item);
        Vector2 size = tooltipStyle.CalcSize(new GUIContent(info));
        
        Rect tooltipRect = new Rect(mousePos.x + 16, mousePos.y + 16, size.x, size.y);

        if (tooltipRect.xMax > Screen.width)
            tooltipRect.x = Screen.width - size.x - 16;
        if (tooltipRect.yMax > Screen.height)
            tooltipRect.y = Screen.height - size.y - 16;

        GUI.Box(tooltipRect, info, tooltipStyle);
    }

    string GetBlockInfo(SlotBlock item)
    {
        if (item == null) return "";

        // Pull the accurate break time directly from your centralized BlockDefs
        float breakTime = BlockDefs.BaseBreakTime(item.blockType);

        if (item.blockType == BlockType.Clay)
        {
            string colorName = item.clayColor.ToString();
            return $"{colorName} Clay\nBreak Time: {breakTime}s";
        }
        else
        {
            string name = item.blockType.ToString();
            return $"{name}\nBreak Time: {breakTime}s";
        }
    }

    void DrawHotbarTargets()
    {
        if (hotbar == null) return;

        for (int i = 0; i < 9; i++)
        {
            Rect rect = hotbar.GetSlotRect(i);
            if (!rect.Contains(Event.current.mousePosition)
                || Event.current.type != EventType.MouseDown)
                continue;

            SlotBlock slotItem = hotbar.GetSlot(i);

            // SHIFT + LMB or SHIFT + RMB on a hotbar slot: move it straight to the
            // last empty inventory slot.
            if (Event.current.shift)
            {
                QuickPlaceToInventory(i, slotItem);
                continue;
            }

            // RMB: split a hotbar stack in half (or pick up a single item whole).
            if (Event.current.button == 1)
            {
                if (slotItem == null || picked != null) continue;
                if (slotItem.count > 1)
                {
                    int half = slotItem.count / 2;
                    picked = new SlotBlock
                    {
                        blockType = slotItem.blockType,
                        clayColor = slotItem.clayColor,
                        customColor = slotItem.customColor,
                        count = half
                    };
                    slotItem.count -= half;
                    if (slotItem.count <= 0)
                        hotbar.SetSlot(i, null);
                }
                else
                {
                    picked = slotItem;
                    hotbar.SetSlot(i, null);
                }
                continue;
            }

            if (picked == null)
            {
                if (slotItem != null)
                {
                    picked = slotItem;
                    hotbar.SetSlot(i, null);
                }
            }
            else if (slotItem != null)
            {
                // Holding an item onto a hotbar stack: merge if same ID + room,
                // otherwise swap whole stacks.
                if (!TryMergePicked(slotItem))
                {
                    hotbar.SetSlot(i, picked);
                    picked = slotItem;
                }
            }
            else
            {
                // Holding an item onto an empty hotbar slot: place it there.
                hotbar.SetSlot(i, picked);
                picked = null;
            }
        }
    }

    void DrawPickedItem()
    {
        Vector2 center = Event.current.mousePosition;
        float s = slotSize * 0.7f;
        Rect rect = new Rect(center.x - s / 2f, center.y - s / 2f, s, s);
        GUI.color = new Color(1f, 1f, 1f, 0.85f);
        GUI.DrawTexture(rect, slotBackTex != null ? slotBackTex : Texture2D.whiteTexture);
        DrawItemIcon(rect, picked);
        DrawStackCount(rect, picked);
        GUI.color = Color.white;
    }

    void DrawMainInventory()
    {
        float slide = (1f - animT) * (slotSize + gap) * 1.5f;
        float fade = animT;

        float centerX = Screen.width / 2f;
        float cols = 10f;
        float totalW = cols * slotSize + (cols - 1f) * gap;
        float x = centerX - totalW / 2f;

        float hotbarBottom = Screen.height - hotbarYFromBottom;
        float hotbarTop = hotbarBottom - slotSize;
        float bottom = hotbarTop - hotbarSpacing + slide;

        GUI.color = new Color(1f, 1f, 1f, fade);

        for (int row = 0; row < 2; row++)
        {
            float y = bottom - (row + 1) * (slotSize + gap);
            for (int col = 0; col < 10; col++)
            {
                int index = row * 10 + col;
                Rect rect = new Rect(x + col * (slotSize + gap), y, slotSize, slotSize);

                if (slotBackTex != null)
                    GUI.DrawTexture(rect, slotBackTex);

                SlotBlock item = mainSlots[index];
                if (item != null)
                {
                    DrawItemIcon(rect, item);
                    DrawStackCount(rect, item);
                }

                if (rect.Contains(Event.current.mousePosition)
                    && Event.current.type == EventType.MouseDown)
                {
                    HandleMainSlotClick(index, item, Event.current.button, Event.current.shift);
                }
            }
        }

        GUI.color = Color.white;
    }

    // Merge the picked item into a target stack that has the SAME block ID and
    // still has room. Only the amount that fits (up to the stack limit) transfers;
    // the remainder stays under the cursor. Returns true if any amount moved.
    bool TryMergePicked(SlotBlock target)
    {
        if (picked == null || target == null) return false;
        if (picked.Id != target.Id) return false;
        if (target.count >= target.StackLimit) return false;

        int moved = Mathf.Min(target.StackLimit - target.count, picked.count);
        if (moved <= 0) return false;

        target.count += moved;
        picked.count -= moved;
        if (picked.count <= 0) picked = null;
        return true;
    }

    // Handles a mouse click on a main-inventory slot.
    //   button 0 (LMB): pick up the whole stack / item. Dropping a picked item
    //                   onto a same-ID non-full stack merges it; any overflow stays
    //                   under the cursor. Different-ID stacks swap instead.
    //   button 1 (RMB): pick up half of a block stack (whole for single items).
    //   SHIFT+LMB     : quick-place to hotbar, filling empty slots left-to-right.
    //   SHIFT+RMB     : quick-place to hotbar, filling empty slots right-to-left.
    void HandleMainSlotClick(int index, SlotBlock slotItem, int button, bool shift)
    {
        if (button == 0 && shift) { QuickPlaceToHotbar(index, slotItem, false); return; }
        if (button == 1 && shift) { QuickPlaceToHotbar(index, slotItem, true); return; }

        // RMB: split a stack in half (or pick up a single item whole).
        if (button == 1)
        {
            if (slotItem == null || picked != null) return;
            if (slotItem.count > 1)
            {
                int half = slotItem.count / 2;
                picked = new SlotBlock
                {
                    blockType = slotItem.blockType,
                    clayColor = slotItem.clayColor,
                    customColor = slotItem.customColor,
                    count = half
                };
                slotItem.count -= half;
                if (slotItem.count <= 0)
                    mainSlots[index] = null;
            }
            else
            {
                picked = slotItem;
                mainSlots[index] = null;
            }
            return;
        }

        // LMB: (default)
        if (picked == null)
        {
            // Empty cursor: pick up the whole stack.
            if (slotItem != null)
            {
                picked = slotItem;
                mainSlots[index] = null;
            }
        }
        else if (slotItem != null)
        {
            // Holding an item: try to merge into a same-ID non-full stack. If the
            // merge moves nothing (different ID, or the stack is full), swap the
            // whole stacks instead. On a partial merge the remainder stays picked.
            if (!TryMergePicked(slotItem))
            {
                mainSlots[index] = picked;
                picked = slotItem;
            }
        }
        else
        {
            // Holding an item, dropping onto an empty slot: place it there.
            mainSlots[index] = picked;
            picked = null;
        }
    }

    // Quick-place: move an inventory item into the hotbar (like Minecraft's Q /
    // double-click to hotbar). Stackable blocks merge with an existing same-type
    // stack; anything that doesn't fit stays in the inventory slot. When no same
    // stack exists (or for single items), the item goes to the first free hotbar
    // slot searching left-to-right (fillFromRight=false) or right-to-left (true).
    void QuickPlaceToHotbar(int index, SlotBlock slotItem, bool fillFromRight)
    {
        if (slotItem == null || hotbar == null || picked != null) return;

        // Stackable blocks try to merge with an existing hotbar stack first.
        if (slotItem.count > 1)
        {
            int limit = slotItem.StackLimit;
            for (int h = 0; h < 9; h++)
            {
                SlotBlock hb = hotbar.GetSlot(h);
                if (hb == null || !SameItem(hb, slotItem)) continue;
                if (hb.count >= limit) continue;

                int moved = Mathf.Min(limit - hb.count, slotItem.count);
                hb.count += moved;
                slotItem.count -= moved;

                if (slotItem.count <= 0)
                    mainSlots[index] = null;
                return; // remainder (if any) stays in the inventory slot
            }
        }

        // No same-type stack (or it's a single item): use the next free hotbar slot.
        int free = FindFreeHotbarSlot(fillFromRight);
        if (free >= 0)
        {
            hotbar.SetSlot(free, slotItem);
            mainSlots[index] = null;
        }
    }

    // Move an item straight from a hotbar slot (0..8) into the LAST empty main
    // inventory slot (inventory 9..28, highest empty index first). Empties the
    // hotbar slot.
    void QuickPlaceToInventory(int hotbarIndex, SlotBlock slotItem)
    {
        if (slotItem == null || picked != null) return;

        for (int m = mainSlots.Length - 1; m >= 0; m--)
        {
            if (mainSlots[m] == null)
            {
                mainSlots[m] = slotItem;
                hotbar.SetSlot(hotbarIndex, null);
                return;
            }
        }
    }

    int FindFreeHotbarSlot(bool fromRight)
    {
        if (fromRight)
        {
            for (int h = 8; h >= 0; h--)
                if (hotbar.GetSlot(h) == null) return h;
        }
        else
        {
            for (int h = 0; h < 9; h++)
                if (hotbar.GetSlot(h) == null) return h;
        }
        return -1;
    }

    // Add a dropped/obtained block by its ID. Loop slots 0..28 looking for a slot
    // whose block ID matches AND whose stack is not yet full; if one is found,
    // merge the drop into it. Along the way, remember the first empty slot. If no
    // slot matches (same ID + room), the block goes into that first empty slot.
    public bool AddItem(SlotBlock item)
    {
        if (item == null) return true;

        int id = item.Id;
        int emptySlot = -1; // first empty slot seen during the scan

        for (int s = 0; s < TotalSlots; s++)
        {
            SlotBlock existing = GetSlot(s);

            // Remember the id of the first empty slot (fallback placement target).
            if (existing == null)
            {
                if (emptySlot < 0) emptySlot = s;
                continue;
            }

            // Same block id AND the stack still has room -> pick up the drop here.
            if (existing.Id == id && existing.count < existing.StackLimit)
            {
                int limit = existing.StackLimit;
                int moved = Mathf.Min(limit - existing.count, item.count);
                existing.count += moved;
                item.count -= moved;
                if (item.count <= 0) return true;
                // A big drop may overflow one stack; keep scanning for more room.
            }
        }

        // No matching non-full stack (or it couldn't absorb everything): drop the
        // remainder into the first empty slot saved during the scan.
        if (emptySlot >= 0 && item.count > 0)
        {
            SetSlot(emptySlot, item);
            return true;
        }

        return false; // no match and no room anywhere
    }

    void DrawCreativePanel()
    {
        int rows = 4, cols = 9;
        float gridW = cols * creativeSlotSize + (cols - 1) * creativeGap;
        float gridH = rows * creativeSlotSize + (rows - 1) * creativeGap;

        float panelX = panelPadding + 20f;
        float panelY = panelPadding + 40f;

        float bgPadding = 15f;
        float bgWidth = gridW + bgPadding * 2f;
        float bgHeight = tabHeight + 15f + gridH + bgPadding * 2f;
        Rect bgRect = new Rect(panelX - bgPadding, panelY - bgPadding, bgWidth, bgHeight);
        
        GUI.color = new Color(creativePanelBgColor.r, creativePanelBgColor.g, creativePanelBgColor.b, creativePanelBgColor.a * animT);
        GUI.DrawTexture(bgRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        DrawTabs(panelX, panelY, gridW);

        float gridY = panelY + tabHeight + 15f;
        GUI.color = new Color(1f, 1f, 1f, animT);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int index = r * cols + c;
                float slotX = panelX + c * (creativeSlotSize + creativeGap);
                float slotY = gridY + r * (creativeSlotSize + creativeGap);
                Rect rect = new Rect(slotX, slotY, creativeSlotSize, creativeSlotSize);

                SlotBlock item = GetCreativeItem(index);
                
                if (creativeSlotTex != null)
                    GUI.DrawTexture(rect, creativeSlotTex);

                if (item != null && !SameItem(picked, item))
                    DrawItemIcon(rect, item);

                if (item != null
                    && rect.Contains(Event.current.mousePosition)
                    && Event.current.type == EventType.MouseDown)
                {
                    picked = SameItem(picked, item) ? null : item;
                }
            }
        }
        GUI.color = Color.white;
    }

    void DrawTabs(float x, float y, float gridW)
    {
        float tabGap = 6f;
        string[] tabs = { "Blocks", "Tools", "Other" };
        for (int i = 0; i < tabs.Length; i++)
        {
            Rect rect = new Rect(x + i * (tabWidth + tabGap), y, tabWidth, tabHeight);
            bool selected = creativeTab == tabs[i];
            
            Color bg = selected
                ? new Color(0.15f, 0.15f, 0.2f, 0.95f)
                : new Color(0.08f, 0.08f, 0.12f, 0.7f);
            
            DrawTabBG(rect, bg);

            if (tabLabelStyle == null)
            {
                tabLabelStyle = new GUIStyle(GUI.skin.label);
                tabLabelStyle.alignment = TextAnchor.MiddleCenter;
                tabLabelStyle.fontSize = 14;
                tabLabelStyle.fontStyle = FontStyle.Bold;
            }
            tabLabelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, animT);
            GUI.Label(rect, tabs[i], tabLabelStyle);

            if (clickButton == null)
                clickButton = new GUIStyle();
            if (GUI.Button(rect, GUIContent.none, clickButton))
                creativeTab = tabs[i];
        }
    }

    void DrawTabBG(Rect rect, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    SlotBlock GetCreativeItem(int index)
    {
        if (creativeTab == "Other")
        {
            // index 0: Builder Block marker that defines the /map save region.
            if (index == 0)
                return CreativeBlock(BlockType.BuilderBlock, BlockRegistry.GetTypeColor(BlockType.BuilderBlock));
            return null;
        }

        if (creativeTab != "Blocks") return null;

        int blockIndex = index;
        if (blockIndex < 16)
        {
            return new SlotBlock
            {
                blockType = BlockType.Clay,
                clayColor = (ClayColor)blockIndex,
                count = BlockDefs.StackLimit(BlockType.Clay)
            };
        }

        switch (blockIndex - 27)
        {
            case 0: return CreativeBlock(BlockType.Wood, BlockRegistry.GetTypeColor(BlockType.Wood));
            case 1: return CreativeBlock(BlockType.Stone, BlockRegistry.GetTypeColor(BlockType.Stone));
            case 2: return CreativeBlock(BlockType.Iron, BlockRegistry.GetTypeColor(BlockType.Iron));
            case 3: return CreativeBlock(BlockType.Diamond, BlockRegistry.GetTypeColor(BlockType.Diamond));
            default: return null;
        }
    }

    SlotBlock CreativeBlock(BlockType type, Color color)
    {
        return new SlotBlock
        {
            blockType = type,
            customColor = color,
            count = BlockDefs.StackLimit(type)   // pull a full stack from creative
        };
    }

    bool SameItem(SlotBlock a, SlotBlock b)
    {
        if (a == null || b == null) return false;
        return a.blockType == b.blockType
            && a.clayColor == b.clayColor
            && a.customColor == b.customColor;
    }

    void DrawItemIcon(Rect rect, SlotBlock item)
    {
        DrawBlockIcon(rect, item);
    }
}