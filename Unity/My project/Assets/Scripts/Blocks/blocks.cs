using UnityEngine;

// Block types used by the arena and the mining system.
public enum BlockType
{
    Clay,
    Wood,
    Stone,
    Iron,
    Diamond,
    Barrier,
    BuilderBlock
}

// Metadata for each block type (base break time, break type, stack limit).
[System.Serializable]
public struct BlockInfo
{
    public BlockType type;
    public float baseBreakTime; // bare-hands base time in seconds
    public int breakType;       // 1 or 2; determines which tools can break it
    public int stackLimit;      // max count of this block per inventory stack

    public BlockInfo(BlockType t, float time, int bt, int stack)
    {
        type = t;
        baseBreakTime = time;
        breakType = bt;
        stackLimit = stack;
    }
}

public static class BlockDefs
{
    public static readonly BlockInfo[] All = new BlockInfo[]
    {
        new BlockInfo(BlockType.Clay,    3f, 1, 100), // type-1 blocks stack to 100
        new BlockInfo(BlockType.Wood,    5f, 1, 100),
        new BlockInfo(BlockType.Stone,   7f, 1, 100),
        new BlockInfo(BlockType.Iron,    5f, 2, 10),  // type-2 blocks stack to 10
        new BlockInfo(BlockType.Diamond, 16f, 2, 10),
        new BlockInfo(BlockType.Barrier, 0f, 1, 100),  // unbreakable placeholder
        new BlockInfo(BlockType.BuilderBlock, 0f, 1, 1) // build markers (max 2 in world)
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
    public static int StackLimit(BlockType type) => Get(type).stackLimit;
}
