using UnityEngine;

public class BlockBreaker : MonoBehaviour
{
    // Reach is unified with the overlay highlight and block placing via Player.reach.
    [Tooltip("Hammer level. Breaks faster: breakTime / level. Level 0 = bare hands.")]
    public int hammerLevel = 0;

    // Time waited between breaking one block and breaking the next while HOLDING
    // left mouse button down. Clicking instead of holding cancels the cooldown.
    const float breakCooldown = 0.3f;

    // The breaking-loading bar has 10 discrete levels. progress*10 maps to the
    // number of filled segments (0 = outline only, 10 = just before breaking).
    const int BreakLevels = 10;

    float progress;
    float cooldown;
    Block currentTarget;

    Camera cam;
    Player player;

    // Whether the load bar should be drawn this frame (actively holding LMB on a
    // breakable block). Computed in Update, consumed by OnGUI.
    bool showBar;
    // True when the aimed block can't be broken by the current tool (Type 2 with
    // too-low hammer). The bar renders fully red instead of filling by level.
    bool barRed;

    void Awake()
    {
        player = GetComponent<Player>();
        cam = GetComponentInChildren<Camera>();
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (GetComponent<InventoryUI>() != null && GetComponent<InventoryUI>().IsOpen) { showBar = false; barRed = false; return; }

        // Read hammer level from the shared state toggle (if present)
        PlayerStateToggle toggle = GetComponent<PlayerStateToggle>();
        if (toggle != null)
            hammerLevel = toggle.hammerLevel;

        bool holding = Input.GetMouseButton(0);

        // Clicking instead of holding cancels the cooldown, so rapid clicks
        // break blocks with no wait between them.
        if (!holding)
        {
            progress = 0f;
            cooldown = 0f;
        }
        else
        {
            cooldown -= Time.deltaTime;
        }

        Block target = player != null ? player.TargetBlock : null;
        if (target == null)
        {
            currentTarget = null;
            showBar = false;
            barRed = false;
            return;
        }

        if (target != currentTarget)
        {
            currentTarget = target;
            progress = 0f;
        }

        // Builder (practice) mode breaks blocks instantly; the loading bar and
        // hammer gating don't apply. The cooldown between breaks is kept.
        bool practice = PlayerStateManager.IsPractice;
        bool breakable = target.breakable;
        bool canBreak = practice ? breakable : CanBreak(target);

        // The load bar appears as soon as the player is aiming to break while
        // holding LMB (outline only when progress is still 0). It shows even for
        // blocks the current tool can't break (rendered fully red). Hidden in
        // practice since blocks break instantly.
        showBar = holding && breakable && !practice;
        barRed = showBar && !canBreak;

        // While holding, don't start / resume damaging until the cooldown expires.
        if (!holding || cooldown > 0f) return;

        if (!canBreak) return;

        if (practice)
        {
            target.BreakBlock();
            progress = 0f;
            currentTarget = null;
            showBar = false;
            barRed = false;
            cooldown = breakCooldown;
            return;
        }

        // breakTime = real seconds to break this single block; the cooldown only
        // adds a pause before the NEXT break while holding the button.
        float multiplier = Mathf.Max(1, hammerLevel);
        float speed = multiplier / target.breakTime;
        progress += speed * Time.deltaTime;

        if (progress >= 1f)
        {
            target.BreakBlock();
            progress = 0f;
            currentTarget = null;
            showBar = false;
            barRed = false;
            cooldown = breakCooldown;
        }
    }

    // Whether the current tool/hammer can damage the given block. Fists
    // (hammerLevel 0) and low-level hammers cannot break Type 2 blocks
    // (Iron/Diamond); Type 1 blocks (Clay/Wood/Stone) can always be broken.
    bool CanBreak(Block target)
    {
        if (target == null || !target.breakable) return false;
        return target.BreakType <= 1 || hammerLevel >= BreakType2MinLevel;
    }

    const int BreakType2MinLevel = 3;

    // Draw the 4-level breaking load bar over the face of the block being broken.
    void OnGUI()
    {
        GUI.color = Color.white;
        if (!showBar || currentTarget == null || cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(currentTarget.transform.position);
        if (sp.z <= 0f) return;

        float sx = sp.x;
        float sy = Screen.height - sp.y;

        int level = Mathf.Clamp(Mathf.FloorToInt(progress * BreakLevels), 0, BreakLevels);
        // Unbreakable blocks fill every segment, in red.
        if (barRed) level = BreakLevels;

        float barW = 100f;
        float barH = 20f;
        float inset = 1f;
        float x = sx - barW * 0.5f;
        float y = sy - barH * 0.5f - 22f;

        // Outline / frame (always visible at level 0).
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(new Rect(x - inset, y - inset, barW + inset * 2f, barH + inset * 2f), Texture2D.whiteTexture);

        // Inner background.
        GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        GUI.DrawTexture(new Rect(x, y, barW, barH), Texture2D.whiteTexture);

        // Filled segments, one per level reached.
        float segGap = 1.5f;
        float segW = (barW - segGap * (BreakLevels - 1)) / BreakLevels;
        Color segColor = barRed
            ? new Color(0.85f, 0.15f, 0.12f, 0.95f)
            : new Color(0.95f, 0.85f, 0.2f, 0.95f);
        for (int i = 0; i < level; i++)
        {
            float segX = x + i * (segW + segGap);
            GUI.color = segColor;
            GUI.DrawTexture(new Rect(segX, y, segW, barH), Texture2D.whiteTexture);
        }

        GUI.color = Color.white;
    }
}
