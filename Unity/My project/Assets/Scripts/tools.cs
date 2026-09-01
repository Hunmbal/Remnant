using UnityEngine;

// The type of tool a player can hold.
public enum ToolType
{
    Unarmed,
    Sword,
    Hammer,
    Shield
}

// Stats for a tool at a given level.
[System.Serializable]
public struct ToolInfo
{
    public ToolType type;
    public int level;
    public float damage;            // base damage dealt to players
    public float breakMultiplier;   // x faster at breaking type-1 blocks
    public int type2Break;          // 0 = cannot, 1 = x1, 2 = x2, 4 = x4, etc.
    public float blockReduction;    // shield: divides incoming damage (xN)

    public ToolInfo(ToolType t, int lvl, float dmg, float brkMult, int type2BreakVal = 0, float reduction = 1f)
    {
        type = t;
        level = lvl;
        damage = dmg;
        breakMultiplier = brkMult;
        type2Break = type2BreakVal;
        blockReduction = reduction;
    }
}

public static class ToolDefs
{
    // Swords (Levels 1-6)
    public static readonly ToolInfo[] Swords = new ToolInfo[]
    {
        new ToolInfo(ToolType.Sword, 1, 4f, 1f),
        new ToolInfo(ToolType.Sword, 2, 5f, 1f),
        new ToolInfo(ToolType.Sword, 3, 6f, 1f),
        new ToolInfo(ToolType.Sword, 4, 7f, 2f),
        new ToolInfo(ToolType.Sword, 5, 8f, 2f),
        new ToolInfo(ToolType.Sword, 6, 10f, 3f),
    };

    // Hammers (Levels 1-5)
    public static readonly ToolInfo[] Hammers = new ToolInfo[]
    {
        new ToolInfo(ToolType.Hammer, 1, 1f, 2f, 0),                       // cannot break type 2
        new ToolInfo(ToolType.Hammer, 2, 2f, 3f, 0),                       // cannot break type 2
        new ToolInfo(ToolType.Hammer, 3, 4f, 4f, 1),                       // type2 x1
        new ToolInfo(ToolType.Hammer, 4, 5f, 4f, 2),                       // type2 x2
        new ToolInfo(ToolType.Hammer, 5, 6f, 4f, 4),                       // type2 x4
    };

    // Shields (Levels 1-6); reduction divides incoming damage.
    public static readonly ToolInfo[] Shields = new ToolInfo[]
    {
        new ToolInfo(ToolType.Shield, 1, 0f, 1f, 0, 2f),
        new ToolInfo(ToolType.Shield, 2, 0f, 1f, 0, 3f),
        new ToolInfo(ToolType.Shield, 3, 0f, 1f, 0, 4f),
        new ToolInfo(ToolType.Shield, 4, 0f, 1f, 0, 6f),
        new ToolInfo(ToolType.Shield, 5, 0f, 1f, 0, 8f),
        new ToolInfo(ToolType.Shield, 6, 0f, 1f, 0, 10f),
    };

    // Unarmed (fists) - no tool equipped.
    public static readonly ToolInfo Unarmed = new ToolInfo(ToolType.Unarmed, 0, 0.5f, 1f, 0);

    public static ToolInfo GetSword(int level) => GetLeveled(Swords, level);
    public static ToolInfo GetHammer(int level) => GetLeveled(Hammers, Mathf.Clamp(level, 1, 5));
    public static ToolInfo GetShield(int level) => GetLeveled(Shields, level);

    static ToolInfo GetLeveled(ToolInfo[] arr, int level)
    {
        foreach (var t in arr)
            if (t.level == level)
                return t;
        return arr[arr.Length - 1];
    }

    // Whether a given tool can break a block of the given break type.
    public static bool CanBreak(ToolInfo tool, int blockBreakType)
    {
        if (tool.type == ToolType.Unarmed)
            return blockBreakType == 1;

        if (tool.type == ToolType.Hammer)
            return blockBreakType == 1 || tool.type2Break > 0;

        // Sword can break both types at its base multiplier.
        return true;
    }

    // Effective break multiplier (times faster) for a tool against a block type.
    public static float BreakMultiplierAgainst(ToolInfo tool, int blockBreakType)
    {
        if (tool.type == ToolType.Unarmed)
            return 1f;

        if (tool.type == ToolType.Hammer)
        {
            if (blockBreakType == 1)
                return tool.breakMultiplier;
            // Type 2: use the tool's type2Break speed.
            return tool.type2Break > 0 ? tool.type2Break : 0f;
        }

        // Sword applies its general multiplier to both types.
        return tool.breakMultiplier;
    }
}
