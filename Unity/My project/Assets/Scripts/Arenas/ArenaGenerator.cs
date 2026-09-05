using System.Collections.Generic;
using UnityEngine;

public class ArenaGenerator : MonoBehaviour
{
    // One saved block, positioned relative to the map's min corner.
    [System.Serializable]
    public struct BlockData
    {
        public int x, y, z; // cell coords (0-based, relative to min corner)
        public int id;      // BlockIDs numeric id

        public BlockData(int x, int y, int z, int id)
        {
            this.x = x; this.y = y; this.z = z; this.id = id;
        }
    }

    [Header("Arena Settings")]
    public int size = 100;
    public int height = 10;

    static readonly HashSet<Vector3Int> solidCells = new HashSet<Vector3Int>();

    // Query used by the Player for sneak edge-protection
    public static bool IsSolid(int x, int y, int z)
        => solidCells.Contains(new Vector3Int(x, y, z));

    // Remove a block from the solid grid (used when a block is broken/placed)
    public static void SetSolid(int x, int y, int z, bool solid)
    {
        var key = new Vector3Int(x, y, z);
        if (solid) solidCells.Add(key);
        else solidCells.Remove(key);
    }

    void Start()
    {
        // Enforce 100x100 (older serialized instances may still have size=10)
        if (size <= 10)
            size = 100;

        // Clear any leftover blocks saved in the scene from previous versions
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        // The arena is no longer generated: load the shipped chess.rmap by
        // default. The procedural arena is only a fallback if that isn't there.
        if (!TryLoadDefaultMap())
            GenerateArena();
    }

    // Try to load the default arena map (chess.rmap). Returns true on success.
    bool TryLoadDefaultMap()
    {
        if (MapSaver.TryLoad("chess", out string error, out _, out _, out _, out _))
            return true;
        Debug.LogWarning("[Arena] Default map 'chess' unavailable (" + error + "); using the generated arena.");
        return false;
    }

    Material CreateClayMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);

        mat.color = color;
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);

        return mat;
    }

    // Builder-block material: diagonal yellow/black construction stripes, baked into
    // a 16x16 texture so the markers are unmistakable next to the clay builds.
    Material CreateBuilderMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        const int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color dark = BlockRegistry.GetTypeColor(BlockType.Stone);
        Color light = BlockRegistry.GetTypeColor(BlockType.BuilderBlock);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool stripe = ((x + y) / 4) % 2 == 0;
                tex.SetPixel(x, y, stripe ? light : dark);
            }
        }
        tex.Apply();

        Material mat = new Material(shader);
        mat.color = Color.white;
        mat.mainTexture = tex;
        mat.SetColor("_BaseColor", Color.white);
        mat.SetColor("_Color", Color.white);
        mat.SetTexture("_BaseMap", tex);
        mat.SetTexture("_MainTex", tex);

        return mat;
    }

    // Skin for a dropped block: its real block color filled in, with a thin border
    // around the edge of each face so it reads as a highlighted drop. The border
    // color adapts to the block: light yellow for dark blocks, dark red for light
    // blocks, so the outline stays visible either way. Baked into the texture (no
    // per-frame GUI cost) and depth-culled by Unity's normal rendering.
    Material CreateDropSkin(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) return CreateClayMaterial(color);

        const int size = 16;
        int border = 1; // px border on each edge (thin)

        // Always the same yellow as the active hotbar highlight.
        Color edge = new Color(1f, 0.84f, 0.3f);

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool onBorder = x < border || x >= size - border || y < border || y >= size - border;
                tex.SetPixel(x, y, onBorder ? edge : color);
            }
        }
        tex.Apply();

        Material mat = new Material(shader);
        mat.color = Color.white;
        mat.mainTexture = tex;
        mat.SetColor("_BaseColor", Color.white);
        mat.SetColor("_Color", Color.white);
        mat.SetTexture("_BaseMap", tex);
        mat.SetTexture("_MainTex", tex);

        return mat;
    }


    Material CreateBarrierMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);

        Color semi = new Color(0.6f, 0.8f, 0.6f, 0.15f); // very faint green, mostly invisible
        mat.color = semi;
        mat.SetColor("_BaseColor", semi);
        mat.SetColor("_Color", semi);

        // Force transparent blending for this shader
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);

        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_ALPHABLEND_ON");

        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return mat;
    }

    void GenerateArena()
    {
        Material greenMat = CreateClayMaterial(BlockRegistry.GetClayColor(ClayColor.Green));   // #5E7C16
        Material blackMat = CreateClayMaterial(BlockRegistry.GetClayColor(ClayColor.Black));   // #1E1E1E
        Material brownMat = CreateClayMaterial(BlockRegistry.GetClayColor(ClayColor.Brown));   // #8B5A2B
        Material barrierMat = CreateBarrierMaterial();

        solidCells.Clear();

        // 1. FLOOR: Green/Black chessboard at y=0
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                bool even = (x + z) % 2 == 0;
                Material mat = even ? greenMat : blackMat;
                ClayColor cc = even ? ClayColor.Green : ClayColor.Black;
                CreateBlock(new Vector3(x, 0, z), mat, BlockMaterial.Clay, cc);
            }
        }

        // 2. CEILING: Barrier at y=height-1
        float topY = height - 1f;
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                CreateBlock(new Vector3(x, topY, z), barrierMat, BlockMaterial.Barrier);
            }
        }

        // 3. WALLS: Barrier at edges, y=1 to y=height-2
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 0; x < size; x++)
            {
                CreateBlock(new Vector3(x, y, 0), barrierMat, BlockMaterial.Barrier);
                CreateBlock(new Vector3(x, y, size - 1), barrierMat, BlockMaterial.Barrier);
            }
            for (int z = 1; z < size - 1; z++)
            {
                CreateBlock(new Vector3(0, y, z), barrierMat, BlockMaterial.Barrier);
                CreateBlock(new Vector3(size - 1, y, z), barrierMat, BlockMaterial.Barrier);
            }
        }

        // 4. CENTER TEST PLATFORM: stepped brown-clay pyramid (1→4 blocks tall).
        // Each tier rises exactly 1 block, so every hop is a clean single jump.
        GeneratePyramid(brownMat);
    }

    void GeneratePyramid(Material mat)
    {
        // Tier footprint (x/z start..end inclusive) and height in blocks
        (int x0, int x1, int z0, int z1, int h)[] tiers = new (int, int, int, int, int)[]
        {
            (45, 54, 45, 54, 1), //  10x10  tier 1 (top at y=1)
            (47, 52, 47, 52, 2), //   6x6   tier 2 (top at y=2)
            (48, 51, 48, 51, 3), //   4x4   tier 3 (top at y=3)
            (49, 50, 49, 50, 4)  //   2x2   crown  (top at y=4)
        };

        foreach (var tier in tiers)
        {
            for (int x = tier.x0; x <= tier.x1; x++)
            {
                for (int z = tier.z0; z <= tier.z1; z++)
                {
                    for (int y = 0; y < tier.h; y++)
                    {
                        CreateBlock(new Vector3(x, y, z), mat, BlockMaterial.Clay, ClayColor.Brown);
                    }
                }
            }
        }
    }

    GameObject CreateBlock(Vector3 position, Material mat, BlockMaterial blockMaterial = BlockMaterial.Clay, ClayColor? clayColorOverride = null, BlockType blockType = BlockType.Clay)
    {
        var key = new Vector3Int((int)position.x, (int)position.y, (int)position.z);
        if (!solidCells.Add(key))
            return null;

        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = "ArenaBlock";
        block.transform.position = position;
        block.transform.parent = transform;
        block.GetComponent<Renderer>().material = mat;

        Block b = block.AddComponent<Block>();
        b.material = blockMaterial;
        b.blockType = blockType;
        b.breakTime = BlockDefs.BaseBreakTime(blockType);
        if (clayColorOverride.HasValue)
            b.clayColor = clayColorOverride.Value;

        return block;
    }

    // Public placement API used by BlockPlacer (practice mode).
    public void SpawnNewBlock(Vector3 position, SlotBlock slot)
    {
        var key = new Vector3Int((int)position.x, (int)position.y, (int)position.z);
        if (solidCells.Contains(key))
            return;

        Material mat = MaterialFor(slot.blockType, slot.clayColor);
        CreateBlock(position, mat, slot.BlockMaterial, slot.clayColor, slot.blockType);
    }

    // Fresh material for any placeable block type (clay colors, non-clay tints,
    // barrier, builder marker).
    Material MaterialFor(BlockType type, ClayColor clay)
    {
        if (type == BlockType.BuilderBlock) return CreateBuilderMaterial();
        if (type == BlockType.Barrier) return CreateBarrierMaterial();
        if (type == BlockType.Clay) return CreateClayMaterial(BlockRegistry.GetClayColor(clay));
        return CreateClayMaterial(BlockRegistry.GetTypeColor(type));
    }

    // How many Builder Blocks currently exist in this world (max 2 allowed).
    public int CountBuilderBlocks()
    {
        int n = 0;
        foreach (Transform child in transform)
        {
            Block b = child.GetComponent<Block>();
            if (b != null && b.blockType == BlockType.BuilderBlock)
                n++;
        }
        return n;
    }

    // Remove the oldest-placed Builder Block (children are appended in placement
    // order, so the first builder child is the oldest). This drives the sliding
    // window: placing a new marker while 2 already exist drops the oldest first.
    // Returns the removed block, or null when none are left.
    public Block PopOldestBuilderBlock()
    {
        foreach (Transform child in transform)
        {
            Block b = child.GetComponent<Block>();
            if (b == null || b.blockType != BlockType.BuilderBlock) continue;

            Vector3Int cell = new Vector3Int(
                Mathf.RoundToInt(child.position.x),
                Mathf.RoundToInt(child.position.y),
                Mathf.RoundToInt(child.position.z));
            SetSolid(cell.x, cell.y, cell.z, false);
            Destroy(child.gameObject);
            return b;
        }
        return null;
    }

    // Tear down the world: destroys every block GO and empties the solid grid.
    public void ClearWorld()
    {
        solidCells.Clear();
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    // Rebuild the world from a saved map. The blocks arrive as cell positions
    // relative to the map's min corner, so they are placed starting at (0,0,0).
    // `spawns` holds the map's per-team spawn slots and replaces the current ones.
    public void BuildFromSave(int sx, int sy, int sz, System.Collections.Generic.List<BlockData> blocks,
        Dictionary<SpawnTeam, Vector3> spawns)
    {
        size = sx;
        height = sy;
        Spawns.Clear();
        if (spawns != null)
        {
            foreach (KeyValuePair<SpawnTeam, Vector3> kv in spawns)
                Spawns[kv.Key] = kv.Value;
        }
        ClearWorld();

        foreach (BlockData b in blocks)
        {
            var (type, clay) = BlockIDs.FromId(b.id);
            BlockMaterial mat = type == BlockType.Barrier ? BlockMaterial.Barrier
                : type == BlockType.BuilderBlock ? BlockMaterial.Builder
                : BlockMaterial.Clay;
            CreateBlock(new Vector3(b.x, b.y, b.z), MaterialFor(type, clay), mat, clay, type);
        }
    }

    // Spawn slots of the currently loaded map (FFA + 4 team spawns). Empty when
    // the map defines none.
    public Dictionary<SpawnTeam, Vector3> Spawns { get; } = new Dictionary<SpawnTeam, Vector3>();

    // Spawn position for a player's team: their team slot, else the FFA slot.
    // Returns false when the map has neither.
    public bool TryGetSpawn(PlayerTeam? team, out Vector3 pos)
    {
        pos = default;
        SpawnTeam slot = PlayerData.SpawnSlotFor(team);
        if (Spawns.TryGetValue(slot, out Vector3 s))
        {
            pos = s;
            return true;
        }
        return slot == SpawnTeam.FFA ? false : Spawns.TryGetValue(SpawnTeam.FFA, out pos);
    }

    // Move every live player to this map's spawn for their team (FFA slot when
    // they have no team or their team slot is missing). Does nothing when the
    // map defines no usable spawn.
    public void TeleportPlayersToSpawn()
    {
        foreach (Player p in Object.FindObjectsOfType<Player>())
        {
            if (TryGetSpawn(PlayerData.Team, out Vector3 spawnPos))
                p.transform.position = spawnPos;
        }
    }

    // Reset back to the default arena (reloads the shipped chess.rmap; falls
    // back to the procedural arena if the map file is gone).
    public void ResetToDefault()
    {
        size = 100;
        height = 10;
        ClearWorld();
        if (!TryLoadDefaultMap())
            GenerateArena();
    }

    // Fill every cell inside [min..max] (inclusive) with the given block. Builder
    // Block markers are kept (they define the region), and cells already full of
    // the target block are left alone. Returns the number of blocks placed.
    public int FillRegion(Vector3Int min, Vector3Int max, BlockType type, ClayColor clay)
    {
        // Index the current world by cell so each fill cell is a fast lookup.
        Dictionary<Vector3Int, Block> existing = new Dictionary<Vector3Int, Block>();
        foreach (Transform child in transform)
        {
            Block b = child.GetComponent<Block>();
            if (b == null) continue;
            Vector3Int cell = new Vector3Int(
                Mathf.RoundToInt(child.position.x),
                Mathf.RoundToInt(child.position.y),
                Mathf.RoundToInt(child.position.z));
            existing[cell] = b;
        }

        int targetId = BlockIDs.IdOf(type, clay);
        int count = 0;

        for (int x = min.x; x <= max.x; x++)
            for (int y = min.y; y <= max.y; y++)
                for (int z = min.z; z <= max.z; z++)
                {
                    Vector3Int cell = new Vector3Int(x, y, z);

                    // The two markers always survive a fill.
                    if (existing.TryGetValue(cell, out Block blk) && blk.blockType == BlockType.BuilderBlock)
                        continue;

                    // Already the target block -> nothing to change.
                    if (existing.TryGetValue(cell, out blk) && blk.GetId() == targetId)
                        continue;

                    // Replace whatever was here before.
                    if (existing.TryGetValue(cell, out blk))
                    {
                        SetSolid(x, y, z, false);
                        Destroy(blk.gameObject);
                    }

                    BlockMaterial mat = type == BlockType.Barrier ? BlockMaterial.Barrier
                        : type == BlockType.BuilderBlock ? BlockMaterial.Builder
                        : BlockMaterial.Clay;
                    CreateBlock(new Vector3(x, y, z), MaterialFor(type, clay), mat, clay, type);
                    count++;
                }

        return count;
    }

    // Stamp a saved map into the world without clearing it. Block coords are
    // relative to the map's min corner and get offset by `origin` (the anchor
    // cell). Cells already solid are replaced by the pasted block — except the
    // current world's Builder markers, which always survive. Returns the number
    // of blocks placed.
    public int PasteBlocks(Vector3Int origin, System.Collections.Generic.List<BlockData> blocks)
    {
        Dictionary<Vector3Int, Block> existing = new Dictionary<Vector3Int, Block>();
        foreach (Transform child in transform)
        {
            Block b = child.GetComponent<Block>();
            if (b == null) continue;
            Vector3Int cell = new Vector3Int(
                Mathf.RoundToInt(child.position.x),
                Mathf.RoundToInt(child.position.y),
                Mathf.RoundToInt(child.position.z));
            existing[cell] = b;
        }

        int count = 0;
        foreach (BlockData b in blocks)
        {
            Vector3Int cell = origin + new Vector3Int(b.x, b.y, b.z);

            // The paste anchor and any other markers must survive.
            if (existing.TryGetValue(cell, out Block blk) && blk.blockType == BlockType.BuilderBlock)
                continue;

            // Already the same block -> leave it.
            if (existing.TryGetValue(cell, out blk) && blk.GetId() == b.id)
                continue;

            // Replace whatever is in the way (floor, walls, ...).
            if (existing.TryGetValue(cell, out blk))
            {
                SetSolid(cell.x, cell.y, cell.z, false);
                Destroy(blk.gameObject);
            }

            var (type, clay) = BlockIDs.FromId(b.id);
            BlockMaterial mat = type == BlockType.Barrier ? BlockMaterial.Barrier
                : BlockMaterial.Clay;
            CreateBlock(new Vector3(cell.x, cell.y, cell.z), MaterialFor(type, clay), mat, clay, type);
            count++;
        }

        return count;
    }

    // Spawn a dropped item into the world. Each dropped block pops out and falls
    // (Minecraft-style). It merges into a nearby same-ID pile only once it lands.
    // Individual chunks are capped at DroppedItem.MaxDropCount each.
    public GameObject SpawnDrop(Vector3 position, SlotBlock item)
    {
        GameObject result = null;

        // Keep placing blocks until the whole drop has been put down. A single
        // (non-capped) drop becomes one falling cube; a stack larger than the cap
        // is split into several chunks that each land and merge independently.
        while (item != null && item.count > 0)
        {
            SlotBlock chunk = item;
            if (item.count > DroppedItem.MaxDropCount)
            {
                chunk = item.Clone(DroppedItem.MaxDropCount);
                item.count -= DroppedItem.MaxDropCount;
            }
            else
            {
                item = null;
            }

            GameObject drop = BuildFreshDrop(position, chunk);
            if (result == null) result = drop;
        }

        return result;
    }

    GameObject BuildFreshDrop(Vector3 position, SlotBlock item)
    {
        // The cube keeps its own block color with a baked yellow highlight skin.
        Material mat = CreateDropSkin(item.GetColor());

        GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        drop.name = "DroppedItem";
        drop.transform.position = position;
        drop.transform.localScale = Vector3.one * 0.25f;
        drop.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        drop.transform.parent = transform;
        drop.GetComponent<Renderer>().material = mat;

        DroppedItem dropped = drop.AddComponent<DroppedItem>();
        dropped.SetItem(item);
        dropped.MakeNonSolid();
        dropped.StartFalling();

        return drop;
    }

    // Convenience drop that finds the arena itself.
    public static GameObject DropItem(Vector3 position, SlotBlock item)
    {
        ArenaGenerator arena = Object.FindObjectOfType<ArenaGenerator>();
        if (arena == null) return null;
        return arena.SpawnDrop(position, item);
    }
}