using UnityEngine;

public class BlockPlacer : MonoBehaviour
{
    Camera cam;
    ArenaGenerator arena;
    Player player;

    void Awake()
    {
        cam = GetComponentInChildren<Camera>();
        if (cam == null) cam = Camera.main;
        arena = Object.FindObjectOfType<ArenaGenerator>();
        player = GetComponent<Player>();
    }

    void Update()
    {
        if (!PlayerStateManager.IsPractice) return;
        if (GetComponent<InventoryUI>() != null && GetComponent<InventoryUI>().IsOpen) return;

        // Middle click: pick the block being looked at into the current slot.
        if (Input.GetMouseButtonDown(2))
        {
            PickBlock();
        }

        // Right click: place the current slot's block on the targeted face.
        if (Input.GetMouseButtonDown(1))
        {
            PlaceBlock();
        }
    }

    void PickBlock()
    {
        Block target = FindTargetBlock();
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

    void PlaceBlock()
    {
        Hotbar hotbar = GetComponent<Hotbar>();
        if (hotbar == null) return;

        SlotBlock slot = hotbar.SelectedSlot;
        if (slot == null) return;

        if (arena == null) arena = Object.FindObjectOfType<ArenaGenerator>();
        if (arena == null) return;

        // Find the face of the target block we are looking at.
        Vector3? placePos = FindPlacePosition();
        if (placePos.HasValue)
        {
            Vector3 pos = placePos.Value;
            Vector3Int key = new Vector3Int(
                Mathf.RoundToInt(pos.x),
                Mathf.RoundToInt(pos.y),
                Mathf.RoundToInt(pos.z));

            // Don't place where the player is standing.
            if (key.x == Mathf.RoundToInt(transform.position.x) &&
                key.y == Mathf.RoundToInt(transform.position.y - 0.5f) &&
                key.z == Mathf.RoundToInt(transform.position.z))
                return;

            if (ArenaGenerator.IsSolid(key.x, key.y, key.z)) return;

            arena.SpawnNewBlock(pos, slot);
        }
    }

    Vector3? FindPlacePosition()
    {
        if (cam == null) return null;

        Vector3 org = cam.transform.position;
        Vector3 dir = cam.transform.forward;

        float reach = player != null ? player.reach : 3f;
        RaycastHit[] hits = Physics.RaycastAll(org + dir * 0.01f, dir, reach);
        float bestDist = float.MaxValue;
        Vector3? best = null;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.transform == transform ||
                hit.collider.transform.IsChildOf(transform))
                continue;

            if (hit.distance < bestDist)
            {
                Block b = hit.collider.GetComponent<Block>();
                if (b != null)
                {
                    bestDist = hit.distance;
                    // Snap to the integer grid position one step along the face normal + hit point.
                    Vector3 place = hit.collider.transform.position + hit.normal * 1f;
                    best = place;
                }
            }
        }

        return best;
    }

    Block FindTargetBlock()
    {
        if (cam == null) return null;

        Vector3 org = cam.transform.position;
        Vector3 dir = cam.transform.forward;

        float reach = player != null ? player.reach : 3f;
        RaycastHit[] hits = Physics.RaycastAll(org + dir * 0.01f, dir, reach);
        float bestDist = float.MaxValue;
        Block best = null;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.transform == transform ||
                hit.collider.transform.IsChildOf(transform))
                continue;

            if (hit.distance < bestDist)
            {
                Block b = hit.collider.GetComponent<Block>();
                if (b != null)
                {
                    bestDist = hit.distance;
                    best = b;
                }
            }
        }

        return best;
    }
}
