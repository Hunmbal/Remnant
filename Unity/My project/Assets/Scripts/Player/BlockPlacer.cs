using UnityEngine;

public class BlockPlacer : MonoBehaviour
{
    ArenaGenerator arena;
    Player player;

    // Time waited between placing one block and placing the next while HOLDING
    // right mouse button down. Clicking instead of holding cancels the cooldown.
    const float placeCooldown = 0.3f;
    float cooldown;

    void Awake()
    {
        arena = Object.FindObjectOfType<ArenaGenerator>();
        player = GetComponent<Player>();
    }

    void Update()
    {
        if (GetComponent<InventoryUI>() != null && GetComponent<InventoryUI>().IsOpen) return;

        // Middle click: pick the block being looked at into the current slot
        // (only in builder/practice mode).
        if (Input.GetMouseButtonDown(2) && PlayerStateManager.IsPractice)
        {
            PickBlock();
        }

        bool holding = Input.GetMouseButton(1);

        // Clicking instead of holding cancels the cooldown, so rapid clicks
        // place with no wait between them.
        if (!holding)
        {
            cooldown = 0f;
        }
        else
        {
            cooldown -= Time.deltaTime;
        }

        if (!holding || cooldown > 0f) return;

        if (PlaceBlock())
            cooldown = placeCooldown;
    }

    void PickBlock()
    {
        Block target = player != null ? player.TargetBlock : null;
        if (target == null) return;

        Hotbar hotbar = GetComponent<Hotbar>();
        if (hotbar == null) return;

        BlockType type = target.material == BlockMaterial.Barrier ? BlockType.Barrier
            : target.material == BlockMaterial.Builder ? BlockType.BuilderBlock
            : BlockType.Clay;
        SlotBlock slot = new SlotBlock
        {
            blockType = type,
            clayColor = target.clayColor,
            count = BlockDefs.StackLimit(type)
        };
        hotbar.SelectedSlot = slot;
    }

    bool PlaceBlock()
    {
        Hotbar hotbar = GetComponent<Hotbar>();
        if (hotbar == null) return false;

        SlotBlock slot = hotbar.SelectedSlot;
        if (slot == null) return false;

        if (arena == null) arena = Object.FindObjectOfType<ArenaGenerator>();
        if (arena == null) return false;

        // Right-clicking a Builder Block shows the map help instead of placing.
        Block aimed = player != null ? player.TargetBlock : null;
        if (aimed != null && aimed.blockType == BlockType.BuilderBlock)
        {
            ShowMapHelp();
            cooldown = placeCooldown; // throttle while the button is held
            return false;
        }

        // Use the shared per-frame raycast result from Player.
        Vector3? placeResult = player != null ? player.PlacePosition : null;
        if (!placeResult.HasValue) return false;

        Vector3 pos = placeResult.Value;
        Vector3Int key = new Vector3Int(
            Mathf.RoundToInt(pos.x),
            Mathf.RoundToInt(pos.y),
            Mathf.RoundToInt(pos.z));

        // Don't place inside the player's hitbox (their body above the feet). A
        // block under their feet is allowed so they can build vertically by
        // jumping and placing a block to land on.
        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc != null)
        {
            float feetY = transform.position.y + bc.center.y - bc.size.y * 0.5f;
            if (pos.y + 0.5f > feetY && // block sits at/above the feet -> overlaps body
                bc.bounds.Intersects(new Bounds(pos, Vector3.one)))
                return false;
        }

        if (ArenaGenerator.IsSolid(key.x, key.y, key.z)) return false;

        arena.SpawnNewBlock(pos, slot);

        // Sliding window: never more than 2 Builder Blocks. Placing a new one with
        // 2 already present drops the oldest first — 1,2 -> 2,3 -> 3,4 -> 4,1 ...
        if (slot.blockType == BlockType.BuilderBlock && arena.CountBuilderBlocks() > 2)
            arena.PopOldestBuilderBlock();

        // Default mode: placing consumes one block. Empty the slot when the
        // stack runs out. (Practice/creative blocks are infinite, no decrement.)
        if (!PlayerStateManager.IsPractice)
        {
            slot.count--;
            if (slot.count <= 0)
                hotbar.SelectedSlot = null;
        }
        return true;
    }

    void ShowMapHelp()
    {
        Chat.Log("Builder Block: place 2 to mark the corners of the map region.");
        Chat.Log("/map save <name>  /map fill <hand|id>  — operate on the region");
        Chat.Log("/map copy  then  /map paste  — clipboard region, anchored at the lowest Builder Block");
        Chat.Log("/map load <name>  /map list  /map new");
    }
}
