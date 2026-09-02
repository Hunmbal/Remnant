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

        // Middle click: pick the block being looked at into the current slot.
        if (Input.GetMouseButtonDown(2))
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

        SlotBlock slot = new SlotBlock
        {
            blockType = target.material == BlockMaterial.Barrier ? BlockType.Barrier : BlockType.Clay,
            clayColor = target.clayColor
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

        // Use the shared per-frame raycast result from Player.
        Vector3? placeResult = player != null ? player.PlacePosition : null;
        if (!placeResult.HasValue) return false;

        Vector3 pos = placeResult.Value;
        Vector3Int key = new Vector3Int(
            Mathf.RoundToInt(pos.x),
            Mathf.RoundToInt(pos.y),
            Mathf.RoundToInt(pos.z));

        // Don't place where the player is standing.
        if (key.x == Mathf.RoundToInt(transform.position.x) &&
            key.y == Mathf.RoundToInt(transform.position.y - 0.5f) &&
            key.z == Mathf.RoundToInt(transform.position.z))
            return false;

        if (ArenaGenerator.IsSolid(key.x, key.y, key.z)) return false;

        arena.SpawnNewBlock(pos, slot);
        return true;
    }
}
