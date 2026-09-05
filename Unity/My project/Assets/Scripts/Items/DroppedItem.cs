using UnityEngine;

// A block dropped into the world. Rendered smaller than a normal block, rotated
// 45 degrees about the vertical axis, using its own block color with a baked
// yellow highlight skin. Rendering goes through Unity's normal 3D pipeline, so
// it is cheap (no per-frame GUI) and is depth-culled automatically (you never see
// drops through walls).
//
// A world-space count floats above the pile and bills to the camera; it is only
// updated when the pile's count changes, so there is no per-frame CPU cost.
//
// For optimization, identical blocks dropped within a 4-block radius merge into
// the FIRST dropped block of that kind, so many drops become one pile. The pile
// keeps a running count; picking it up grants the entire quantity at once.
public class DroppedItem : MonoBehaviour
{
    // Radius (world units) within which a new drop merges into an existing one.
    public const float MergeRadius = 4f;

    // Minimum distance a fresh drop must keep from a DIFFERENT-type drop, so two
    // different blocks never overlap. (Same-type drops merge instead, so they need
    // no spacing.)
    public const float MinSeparation = 0.5f;

    // Only raycast/show the floating count when the player is within this distance
    // of the pile; farther piles skip the per-frame raycast entirely (cheaper).
    public const float CountShowDistance = 8f;

    // Every live drop, oldest first. Used to find a merge target without a physics
    // query (cheap count-based lookup instead of scanning colliders).
    static readonly System.Collections.Generic.List<DroppedItem> All =
        new System.Collections.Generic.List<DroppedItem>();

    public SlotBlock item;

    TextMesh countText;

    // True while this drop is still falling after being released; when false the
    // item is at rest on the ground. fallVelocity is positive upward.
    bool falling;
    float fallVelocity;

    // Render interpolation while falling: FixedUpdate moves the drop on the fixed
    // 30 TPS step, and LateUpdate lerps between the last two simulated positions.
    Vector3 prevDropPos;
    Vector3 currDropPos;

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    // Maximum blocks one dropped pile can hold. Past this a new pile is made.
    public const int MaxDropCount = 100;

    // Y below which a drop is considered to have fallen into the void and is
    // destroyed permanently. The arena floor is at y=0, so anything well below it
    // (e.g. dropped through a hole) is gone for good.
    public const float VoidY = -8f;

    // "Popped out" falling animation for freshly dropped blocks (Minecraft-style).
    const float FallGravity = 22f;   // downward acceleration (units/s^2)
    const float PopHeight = 0.5f;    // release the drop this far above its landing spot
    const float PopVelocity = 2f;    // initial upward "pop" speed

    // Find the first dropped block (the earliest still present) of the SAME block
    // ID within MergeRadius of position that still has room below MaxDropCount, so
    // a new drop can merge into it. Returns null if there is no nearby pile with
    // room (or no nearby pile at all).
    public static DroppedItem FindMergeTarget(Vector3 position, SlotBlock dropped)
    {
        if (dropped == null) return null;
        int id = dropped.Id;

        float radiusSq = MergeRadius * MergeRadius;
        for (int i = 0; i < All.Count; i++)
        {
            DroppedItem d = All[i];
            if (d == null || d.item == null) continue;
            if (d.item.Id != id) continue;
            if (d.item.count >= MaxDropCount) continue; // no room left

            if ((d.transform.position - position).sqrMagnitude <= radiusSq)
                return d; // earliest same-ID pile within range with room
        }
        return null;
    }

    // Return a position (same height) that keeps the new drop at least MinSeparation
    // away from any other drop (same ID included now that piles cap at MaxDropCount),
    // so piles never overlap and overflow piles sit half a block away. The provided
    // drop is excluded so it doesn't read itself as "too close" and get nudged.
    public static Vector3 FindClearPosition(Vector3 position, SlotBlock dropped, DroppedItem exclude = null)
    {
        if (dropped == null || !IsTooCloseToOther(position, dropped.Id, exclude))
            return position;

        float s = MinSeparation;
        Vector3[] offsets =
        {
            new Vector3(s, 0, 0),
            new Vector3(-s, 0, 0),
            new Vector3(0, 0, s),
            new Vector3(0, 0, -s),
            new Vector3(s, 0, s),
            new Vector3(-s, 0, s),
            new Vector3(s, 0, -s),
            new Vector3(-s, 0, -s),
            new Vector3(s * 2, 0, 0),
            new Vector3(-s * 2, 0, 0),
            new Vector3(0, 0, s * 2),
            new Vector3(0, 0, -s * 2),
        };
        foreach (var o in offsets)
        {
            Vector3 candidate = position + o;
            if (!IsTooCloseToOther(candidate, dropped.Id, exclude))
                return candidate;
        }
        return position; // give up; extremely crowded
    }

    static bool IsTooCloseToOther(Vector3 p, int id, DroppedItem exclude)
    {
        float minSq = MinSeparation * MinSeparation;
        for (int i = 0; i < All.Count; i++)
        {
            DroppedItem d = All[i];
            if (d == null || d == exclude || d.item == null) continue;
            if ((d.transform.position - p).sqrMagnitude < minSq)
                return true;
        }
        return false;
    }

    // Point the pile at the given block item and refresh the floating count. Used
    // both when spawning a fresh pile and when a new drop merges into an existing
    // pile (count simply grows).
    public void SetItem(SlotBlock newItem)
    {
        item = newItem;
        RefreshCount();
    }

    // Rebuild the world-space count text/child from the current item.count. Called
    // when a pile is created or merged, never every frame.
    public void RefreshCount()
    {
        if (item == null) return;

        if (item.count > 1)
        {
            if (countText == null)
                countText = CreateCountText();

            countText.gameObject.SetActive(true);
            countText.text = item.count.ToString();
            countText.color = CountColor;
        }
        else if (countText != null)
        {
            countText.gameObject.SetActive(false);
        }
    }

    // Text color: black for light blocks, white for dark blocks, so the count stays
    // readable against the block color.
    Color CountColor
    {
        get
        {
            Color c = item != null ? item.GetColor() : Color.white;
            float lum = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
            return lum < 0.5f ? Color.white : Color.black;
        }
    }

    // Build the billboarded TextMesh that floats above the pile.
    TextMesh CreateCountText()
    {
        GameObject go = new GameObject("Count");
        go.transform.SetParent(transform, false);

        // Float above the block: block half-height is 0.125, lift the text well
        // clear of it and make it small relative to the block so it never overlaps.
        go.transform.localPosition = Vector3.up * 0.45f;
        go.transform.localRotation = Quaternion.identity;

        TextMesh tm = go.AddComponent<TextMesh>();
        tm.font = builtinFont;
        tm.characterSize = 0.06f;   // rendered height ~ 0.29 units, small vs the block
        tm.fontSize = 48;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = CountColor;

        return tm;
    }

    static Font _builtinFont;
    static Font builtinFont =>
        _builtinFont != null ? _builtinFont
            : (_builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") != null
                ? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                : Resources.GetBuiltinResource<Font>("Arial.ttf"));

    // Remove collisions so dropped items rest on the ground and never block the
    // player. (The visual cube keeps its primitive collider as a trigger.)
    public void MakeNonSolid()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    // Raycast straight down from the cube's center; true if a solid surface is
    // within a hair of its bottom (i.e. it is resting on something). Other drops
    // are ignored (trigger colliders), so only real blocks count as ground.
    bool HasGroundBelow()
    {
        const float half = 0.125f;
        return Physics.Raycast(transform.position, Vector3.down, half + 0.02f, ~0, QueryTriggerInteraction.Ignore);
    }

    // Release a fresh drop so it pops up a little and falls (Minecraft-style).
    public void StartFalling()
    {
        transform.position += Vector3.up * PopHeight;
        fallVelocity = PopVelocity; // initial upward "pop"
        falling = true;
        prevDropPos = currDropPos = transform.position;
    }

    // Gravity ticks at the fixed 30 TPS timestep. A landed drop watches for the
    // block under it being broken; if support vanishes it falls again, landing on
    // the next surface below (re-merging) or falling into the void (removed by
    // LateUpdate).
    void FixedUpdate()
    {
        // At rest but no longer supported -> drop without the pop.
        if (!falling && !HasGroundBelow())
        {
            fallVelocity = 0f;
            falling = true;
            prevDropPos = currDropPos = transform.position;
        }

        if (!falling) return;

        prevDropPos = transform.position;

        // Animate the fall. Positive fallVelocity is upward; gravity pulls it down.
        fallVelocity -= FallGravity * Time.deltaTime;
        Vector3 p = transform.position;
        p.y += fallVelocity * Time.deltaTime;
        transform.position = p;

        currDropPos = transform.position;

        // Land when the cube's bottom is about to touch a solid block.
        const float half = 0.125f;
        if (Physics.Raycast(p, Vector3.down, out RaycastHit hit, half + 0.02f, ~0, QueryTriggerInteraction.Ignore))
        {
            Vector3 r = transform.position;
            r.y = hit.point.y + half;
            transform.position = r;
            currDropPos = transform.position;
            falling = false;
            Landed();
        }
    }

    // Called once the drop comes to rest on a surface. Merge with a nearby same-ID
    // pile that has room (so the drop joins it instead of staying separate); else
    // settle into a clear spot away from other piles.
    void Landed()
    {
        if (item != null && item.count > 0)
        {
            DroppedItem target = FindMergeTarget(transform.position, item);
            if (target != null)
            {
                int space = MaxDropCount - target.item.count;
                int moved = Mathf.Min(space, item.count);
                target.item.count += moved;
                target.RefreshCount();

                item.count -= moved;
                if (item.count <= 0)
                {
                    Destroy(gameObject);
                    return;
                }
                RefreshCount();
            }
        }

        // Didn't merge fully: keep clear of every other pile (same ID included).
        transform.position = FindClearPosition(transform.position, item, this);
    }

    // Each frame: billboard the count toward the camera and hide it if any block
    // is between the camera and the number (so you never see counts through walls).
    void LateUpdate()
    {
        // Render interpolation while falling: keep the visual between the last two
        // simulated positions so the 30 TPS sim doesn't judder on high-refresh
        // displays. Resting drops have prev==curr, so this is a no-op for them.
        if (falling)
        {
            float t = Time.fixedDeltaTime > 0f ? (Time.time - Time.fixedTime) / Time.fixedDeltaTime : 1f;
            transform.position = Vector3.Lerp(prevDropPos, currDropPos, Mathf.Clamp01(t));
        }

        // If this drop fell below the arena and into the void, remove it forever.
        if (transform.position.y < VoidY)
        {
            Destroy(gameObject);
            return;
        }

        if (countText == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        if (item == null || item.count <= 1)
        {
            countText.gameObject.SetActive(false);
            return;
        }

        // Cheap distance gate: beyond CountShowDistance, don't even raycast (hide).
        Vector3 camPos = cam.transform.position;
        Vector3 textPos = countText.transform.position;
        Vector3 dir = textPos - camPos;
        float dist = dir.magnitude;
        if (dist > CountShowDistance)
        {
            countText.gameObject.SetActive(false);
            return;
        }

        // Within range: is a solid block between the camera and the number?
        bool blocked = false;
        if (dist > 0.0001f)
            blocked = Physics.Raycast(camPos, dir / dist, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore);

        countText.gameObject.SetActive(!blocked);
        if (!blocked)
            countText.transform.rotation = cam.transform.rotation;
    }
}
