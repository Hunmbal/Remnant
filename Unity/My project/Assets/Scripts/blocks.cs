using UnityEngine;

// Block types used by the arena and the mining system.
public enum BlockType
{
    Clay,
    Wood,
    Stone,
    Iron,
    Diamond,
    Barrier
}

// Metadata for each block type (base break time in seconds, break type 1 or 2).
[System.Serializable]
public struct BlockInfo
{
    public BlockType type;
    public float baseBreakTime; // bare-hands base time in seconds
    public int breakType;       // 1 or 2; determines which tools can break it

    public BlockInfo(BlockType t, float time, int bt)
    {
        type = t;
        baseBreakTime = time;
        breakType = bt;
    }
}

public static class BlockDefs
{
    public static readonly BlockInfo[] All = new BlockInfo[]
    {
        new BlockInfo(BlockType.Clay,    3f, 1),
        new BlockInfo(BlockType.Wood,    5f, 1),
        new BlockInfo(BlockType.Stone,   7f, 1),
        new BlockInfo(BlockType.Iron,    5f, 2),
        new BlockInfo(BlockType.Diamond, 16f, 2),
        new BlockInfo(BlockType.Barrier, 0f, 1) // unbreakable placeholder
    };

    public static BlockInfo Get(BlockType type)
    {
        foreach (var info in All)
            if (info.type == type)
                return info;
        return All[0];
    }

    public static float BaseBreakTime(BlockType type) => Get(type).baseBreakTime;
    public static int BreakType(BlockType type) => Get(type).breakType;
}
