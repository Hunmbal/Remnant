using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Binary map save/load for arenas. A map is defined by the two Builder Blocks a
// player places: everything between them (inclusive AABB) is the save region.
//
// File layout (little-endian).
// Version 1:
//   byte[4] magic  = 'R','M','A','P'
//   byte   version = 1
//   int32  sizeX, sizeY, sizeZ   (region dimensions)
//   int32  count                 (number of saved blocks)
//   count x int32 x
//   count x int32 y
//   count x int32 z
//   count x int32 id             (BlockIDs numeric id)
// Version 2 adds a spawn-slot section after the blocks:
//   int32  spawnCount            (0 when the map defines no spawns)
//   spawnCount x byte   team     (SpawnTeam: 0 FFA, 1 Red, 2 Blue, 3 Green, 4 Yellow)
//   spawnCount x float x, y, z   (feet position)
// Block coords are relative to the region's min corner (so a loaded map starts
// at world origin). Builder Blocks themselves are never saved.
public static class MapSaver
{
    public const string Extension = ".rmap";

    static readonly byte[] Magic = { (byte)'R', (byte)'M', (byte)'A', (byte)'P' };
    const byte Version = 2;

    // Clipboard for /map copy -> /map paste (WorldEdit-style). `refOffset` is the
    // cell of the copy's lowest Builder Block relative to the region's min corner,
    // so a later paste aligns that reference with the world's anchor Builder Block.
    static List<ArenaGenerator.BlockData> clipboardBlocks;
    static Vector3Int clipboardRef;

    // Maps live alongside the code (Assets/Scripts/Arenas/maps) so they travel
    // with the project instead of the OS persistent-data folder.
    public static string ArenasFolder
    {
        get
        {
            string dir = Path.Combine(Application.dataPath, "Scripts", "Arenas", "maps");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }
    }

    static string PathFor(string name) => Path.Combine(ArenasFolder, Sanitize(name) + Extension);

    // Maps shouldn't share filenames with random punctuation.
    static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "map";
        char[] invalid = Path.GetInvalidFileNameChars();
        char[] chars = name.Trim().ToCharArray();
        for (int i = 0; i < chars.Length; i++)
            if (System.Array.IndexOf(invalid, chars[i]) >= 0)
                chars[i] = '_';
        return new string(chars);
    }

    // Locate the two Builder Blocks and compute the inclusive region AABB between
    // them. Returns an error message (or null on success); the bounds are valid
    // when null is returned.
    static string GetBuildRegion(ArenaGenerator arena, out Vector3Int min, out Vector3Int max)
    {
        min = new Vector3Int();
        max = new Vector3Int();

        List<Vector3Int> markers = new List<Vector3Int>();
        foreach (Transform child in arena.transform)
        {
            Block b = child.GetComponent<Block>();
            if (b != null && b.blockType == BlockType.BuilderBlock)
                markers.Add(new Vector3Int(
                    Mathf.RoundToInt(child.position.x),
                    Mathf.RoundToInt(child.position.y),
                    Mathf.RoundToInt(child.position.z)));
        }

        if (markers.Count == 0)
            return "No Builder Blocks placed. Place 2 to define the map region.";
        if (markers.Count < 2)
            return "Only 1 Builder Block placed. Place a 2nd to define the map region.";

        min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        max = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
        foreach (Vector3Int m in markers)
        {
            min = Vector3Int.Min(min, m);
            max = Vector3Int.Max(max, m);
        }

        return null;
    }

    // Capture the current region between the two Builder Blocks into a binary file.
    // Returns an error message (or null on success) for the chat to display.
    public static string Save(string name)
    {
        ArenaGenerator arena = Object.FindObjectOfType<ArenaGenerator>();
        if (arena == null) return "No arena found to save.";

        string err = GetBuildRegion(arena, out Vector3Int min, out Vector3Int max);
        if (err != null) return err;

        // Collect every block inside the region (markers excluded).
        List<ArenaGenerator.BlockData> data = new List<ArenaGenerator.BlockData>();
        foreach (Transform child in arena.transform)
        {
            Block b = child.GetComponent<Block>();
            if (b == null) continue;

            Vector3Int cell = new Vector3Int(
                Mathf.RoundToInt(child.position.x),
                Mathf.RoundToInt(child.position.y),
                Mathf.RoundToInt(child.position.z));

            if (cell.x < min.x || cell.x > max.x ||
                cell.y < min.y || cell.y > max.y ||
                cell.z < min.z || cell.z > max.z)
                continue;

            if (b.blockType == BlockType.BuilderBlock) continue; // marker, not map

            data.Add(new ArenaGenerator.BlockData(cell.x - min.x, cell.y - min.y, cell.z - min.z, b.GetId()));
        }

        if (data.Count == 0)
            return "Region is empty — nothing to save.";

        int sx = max.x - min.x + 1;
        int sy = max.y - min.y + 1;
        int sz = max.z - min.z + 1;

        // Keep any spawn slots already recorded on this map name (best effort: a
        // corrupt file just loses its spawns, not the fresh save).
        string path = PathFor(name);
        Dictionary<SpawnTeam, Vector3> spawns = new Dictionary<SpawnTeam, Vector3>();
        if (File.Exists(path))
        {
            Dictionary<SpawnTeam, Vector3> existing = new Dictionary<SpawnTeam, Vector3>();
            if (TryReadBinary(path, name, out _, out _, out _, out _, out existing, out _))
                spawns = existing;
        }

        try
        {
            WriteBinary(path, sx, sy, sz, data, spawns);
        }
        catch (System.Exception e)
        {
            return "Could not save map: " + e.Message;
        }

        return $"Saved arena '{Sanitize(name)}' ({sx} x {sy} x {sz}, {data.Count} blocks).";
    }

    // Fill the region between the two Builder Blocks with a single block type.
    // `param` is either "hand" (use the player's selected hotbar block) or a
    // numeric BlockIDs id. Returns an error message (or null on success).
    public static string Fill(string param, Player player)
    {
        if (string.IsNullOrWhiteSpace(param))
            return "Usage: /map fill hand   or   /map fill <id>";

        ArenaGenerator arena = Object.FindObjectOfType<ArenaGenerator>();
        if (arena == null) return "No arena found to fill.";

        string err = GetBuildRegion(arena, out Vector3Int min, out Vector3Int max);
        if (err != null) return err;

        BlockType type;
        ClayColor clay;
        string source;

        if (param.Equals("hand", System.StringComparison.OrdinalIgnoreCase))
        {
            Hotbar hotbar = player != null ? player.GetComponent<Hotbar>() : null;
            SlotBlock hand = hotbar != null ? hotbar.SelectedSlot : null;
            if (hand == null) return "No block in hand to fill with.";
            type = hand.blockType;
            clay = hand.clayColor;
            source = type == BlockType.Clay ? "Clay " + clay : type.ToString();
        }
        else if (int.TryParse(param, out int id) && BlockIDs.IsKnown(id))
        {
            (type, clay) = BlockIDs.FromId(id);
            source = type == BlockType.Clay ? "Clay " + clay : type.ToString() + " (id " + id + ")";
        }
        else
        {
            return "Unknown block id '" + param + "'. Use 'hand' or a BlockIDs id.";
        }

        if (type == BlockType.BuilderBlock)
            return "Can't fill with Builder Blocks.";

        int placed = arena.FillRegion(min, max, type, clay);
        return $"Filled {placed} blocks with {source}.";
    }

    static void WriteBinary(string path, int sx, int sy, int sz,
        List<ArenaGenerator.BlockData> data, Dictionary<SpawnTeam, Vector3> spawns)
    {
        using (BinaryWriter w = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            w.Write(Magic);
            w.Write(Version);
            w.Write(sx);
            w.Write(sy);
            w.Write(sz);
            w.Write(data.Count);
            foreach (ArenaGenerator.BlockData b in data)
            {
                w.Write(b.x);
                w.Write(b.y);
                w.Write(b.z);
                w.Write(b.id);
            }
            w.Write(spawns.Count);
            foreach (KeyValuePair<SpawnTeam, Vector3> kv in spawns)
            {
                w.Write((byte)kv.Key);
                w.Write(kv.Value.x);
                w.Write(kv.Value.y);
                w.Write(kv.Value.z);
            }
        }
    }

    // Load a saved arena, replacing the current world. Returns an error message
    // (or null on success) for the chat to display.
    public static string Load(string name)
    {
        if (!TryLoad(name, out string err, out int sx, out int sy, out int sz, out int count))
            return err;
        return $"Loaded arena '{name}' ({sx} x {sy} x {sz}, {count} blocks).";
    }

    // Side-effect-free variant for code paths that don't display chat (Start-up
    // defaults, /map new). Returns false with a reason in `error` when the map is
    // missing or unreadable; sizes/count are zeroed in that case.
    public static bool TryLoad(string name, out string error,
        out int sx, out int sy, out int sz, out int count)
    {
        error = null;
        sx = sy = sz = count = 0;

        string path = PathFor(name);
        if (!File.Exists(path))
        {
            error = $"No map named '{name}' found.";
            return false;
        }

        if (!TryReadBinary(path, name, out sx, out sy, out sz,
            out List<ArenaGenerator.BlockData> data,
            out Dictionary<SpawnTeam, Vector3> spawns, out error))
            return false;

        ArenaGenerator arena = Object.FindObjectOfType<ArenaGenerator>();
        if (arena == null)
        {
            error = "No arena to load into.";
            return false;
        }

        arena.BuildFromSave(sx, sy, sz, data, spawns);
        count = data.Count;

        // Move players to the loaded map's spawn for their team.
        arena.TeleportPlayersToSpawn();
        return true;
    }

    // Set (or override) a team's spawn slot on an existing map file at the
    // player's current feet position. Returns an error message (or null on
    // success) for the chat to display.
    public static string SetSpawn(string name, SpawnTeam team, Vector3 position)
    {
        string path = PathFor(name);
        if (!File.Exists(path))
            return $"No map named '{name}' found. Save it first with /map save <name>.";

        if (!TryReadBinary(path, name, out int sx, out int sy, out int sz,
            out List<ArenaGenerator.BlockData> blocks,
            out Dictionary<SpawnTeam, Vector3> spawns, out string err))
            return err;

        spawns[team] = position;

        try
        {
            WriteBinary(path, sx, sy, sz, blocks, spawns);
        }
        catch (System.Exception e)
        {
            return "Could not update map: " + e.Message;
        }

        return $"Set '{name}' " + team.ToString().ToLowerInvariant() + " spawn at (" +
            position.x.ToString("0.##") + ", " + position.y.ToString("0.##") + ", " +
            position.z.ToString("0.##") + ").";
    }

    // Read + validate a .rmap file. Returns false and sets `err` on any problem.
    // `spawns` holds the map's spawn slots (empty for version-1 files).
    static bool TryReadBinary(string path, string name,
        out int sx, out int sy, out int sz,
        out List<ArenaGenerator.BlockData> data,
        out Dictionary<SpawnTeam, Vector3> spawns, out string err)
    {
        sx = sy = sz = 0;
        data = null;
        spawns = null;
        err = null;

        try
        {
            using (BinaryReader r = new BinaryReader(File.Open(path, FileMode.Open)))
            {
                if (r.BaseStream.Length < 4 + 1 + 4 * 4)
                {
                    err = $"'{name}' is corrupt (too small).";
                    return false;
                }

                byte[] magic = r.ReadBytes(4);
                if (magic[0] != Magic[0] || magic[1] != Magic[1] ||
                    magic[2] != Magic[2] || magic[3] != Magic[3])
                {
                    err = $"'{name}' is not a valid map file.";
                    return false;
                }

                byte version = r.ReadByte();
                if (version < 1 || version > Version)
                {
                    err = $"'{name}' uses an unsupported version ({version}).";
                    return false;
                }

                sx = r.ReadInt32();
                sy = r.ReadInt32();
                sz = r.ReadInt32();
                int count = r.ReadInt32();
                if (sx <= 0 || sy <= 0 || sz <= 0 || count < 0)
                {
                    err = $"'{name}' is corrupt (bad header).";
                    return false;
                }

                data = new List<ArenaGenerator.BlockData>(count);
                for (int i = 0; i < count; i++)
                {
                    int x = r.ReadInt32();
                    int y = r.ReadInt32();
                    int z = r.ReadInt32();
                    int id = r.ReadInt32();
                    data.Add(new ArenaGenerator.BlockData(x, y, z, id));
                }

                spawns = new Dictionary<SpawnTeam, Vector3>();
                if (version >= 2)
                {
                    int spawnCount = r.ReadInt32();
                    if (spawnCount < 0 || spawnCount > 5)
                    {
                        err = $"'{name}' is corrupt (bad spawn count).";
                        return false;
                    }

                    for (int i = 0; i < spawnCount; i++)
                    {
                        byte team = r.ReadByte();
                        if (team > (byte)SpawnTeam.Yellow)
                        {
                            err = $"'{name}' is corrupt (bad spawn team).";
                            return false;
                        }
                        float x = r.ReadSingle();
                        float y = r.ReadSingle();
                        float z = r.ReadSingle();
                        spawns[(SpawnTeam)team] = new Vector3(x, y, z);
                    }
                }

                return true;
            }
        }
        catch (System.Exception e)
        {
            err = "Could not read map: " + e.Message;
            return false;
        }
    }

    // Copy the region between the 2 Builder Blocks into the in-memory clipboard.
    // The reference point for a later paste is the region's lowest Builder Block.
    public static string Copy()
    {
        ArenaGenerator arena = Object.FindObjectOfType<ArenaGenerator>();
        if (arena == null) return "No arena found to copy.";

        string err = GetBuildRegion(arena, out Vector3Int min, out Vector3Int max);
        if (err != null) return err;

        if (!TryFindLowestBuilderBlock(arena, out Vector3Int refCell))
            return "No Builder Block to use as the copy reference.";

        List<ArenaGenerator.BlockData> data = new List<ArenaGenerator.BlockData>();
        foreach (Transform child in arena.transform)
        {
            Block b = child.GetComponent<Block>();
            if (b == null) continue;

            Vector3Int cell = new Vector3Int(
                Mathf.RoundToInt(child.position.x),
                Mathf.RoundToInt(child.position.y),
                Mathf.RoundToInt(child.position.z));

            if (cell.x < min.x || cell.x > max.x ||
                cell.y < min.y || cell.y > max.y ||
                cell.z < min.z || cell.z > max.z)
                continue;

            if (b.blockType == BlockType.BuilderBlock) continue; // marker, not map

            data.Add(new ArenaGenerator.BlockData(cell.x - min.x, cell.y - min.y, cell.z - min.z, b.GetId()));
        }

        if (data.Count == 0)
            return "Region is empty — nothing to copy.";

        clipboardBlocks = data;
        clipboardRef = refCell - min;

        int sx = max.x - min.x + 1;
        int sy = max.y - min.y + 1;
        int sz = max.z - min.z + 1;
        return $"Copied region ({sx} x {sy} x {sz}, {data.Count} blocks).";
    }

    // Paste the clipboard into the current world without clearing it. The copy's
    // lowest Builder Block lands on the world's lowest Builder Block, so the two
    // markers coincide. Solid cells in the way are replaced; markers survive.
    public static string Paste()
    {
        if (clipboardBlocks == null || clipboardBlocks.Count == 0)
            return "Nothing copied yet. Use /map copy first.";

        ArenaGenerator arena = Object.FindObjectOfType<ArenaGenerator>();
        if (arena == null) return "No arena to paste into.";

        if (!TryFindLowestBuilderBlock(arena, out Vector3Int anchor))
            return "No Builder Block in the world to anchor the paste. Place one where the copy's low corner should land.";

        // Put the copy's reference Builder Block onto the anchor cell.
        Vector3Int origin = anchor - clipboardRef;

        int placed = arena.PasteBlocks(origin, clipboardBlocks);
        return $"Pasted {placed} blocks anchored at ({anchor.x}, {anchor.y}, {anchor.z}).";
    }

    // The cell of the lowest Builder Block (lowest Y, then X, then Z) in the world.
    static bool TryFindLowestBuilderBlock(ArenaGenerator arena, out Vector3Int cell)
    {
        cell = new Vector3Int();
        bool found = false;

        foreach (Transform child in arena.transform)
        {
            Block b = child.GetComponent<Block>();
            if (b == null || b.blockType != BlockType.BuilderBlock) continue;

            Vector3Int c = new Vector3Int(
                Mathf.RoundToInt(child.position.x),
                Mathf.RoundToInt(child.position.y),
                Mathf.RoundToInt(child.position.z));

            if (!found || c.y < cell.y ||
                (c.y == cell.y && (c.x < cell.x || (c.x == cell.x && c.z < cell.z))))
            {
                cell = c;
                found = true;
            }
        }

        return found;
    }

    // List all saved arenas. Returns null if none exist.
    public static string[] List()
    {
        if (!Directory.Exists(ArenasFolder))
            return new string[0];
        return Directory.GetFiles(ArenasFolder, "*" + Extension);
    }
}