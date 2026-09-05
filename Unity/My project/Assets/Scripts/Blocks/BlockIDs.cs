using UnityEngine;

// Numeric IDs for every block in the game. Used to identify a specific block
// (type + clay color) in one integer. IDs are:
//   Barrier = 0
//   Wood    = 10
//   Stone   = 20
//   Iron    = 30
//   Diamond = 40
//   Builder = 50
//   Clay    = 100..115  (100 = Black, 115 = White, the rest in between)
public static class BlockIDs
{
    public const int BarrierId = 0;
    public const int WoodId = 10;
    public const int StoneId = 20;
    public const int IronId = 30;
    public const int DiamondId = 40;
    public const int BuilderId = 50;
    public const int ClayBase = 100;

    // Clay color -> clay ID offset (0..15). Black is 0 (ID 100), White is 15
    // (ID 115); the colors in between progress from dark to light.
    public static readonly int[] ClayOffset = new int[]
    {
        /* White      */ 15,
        /* LightGray  */ 2,
        /* Gray       */ 1,
        /* Black      */ 0,
        /* Brown      */ 3,
        /* Red        */ 4,
        /* Orange     */ 5,
        /* Yellow     */ 6,
        /* Lime       */ 7,
        /* Green      */ 8,
        /* Cyan       */ 9,
        /* LightBlue  */ 10,
        /* Blue       */ 11,
        /* Purple     */ 12,
        /* Magenta    */ 13,
        /* Pink       */ 14,
    };

    // ID of the special/block-type colors (non-clay). Clay uses ClayBase + offset.
    static int TypeId(BlockType type)
    {
        switch (type)
        {
            case BlockType.Wood: return WoodId;
            case BlockType.Stone: return StoneId;
            case BlockType.Iron: return IronId;
            case BlockType.Diamond: return DiamondId;
            case BlockType.Barrier: return BarrierId;
            case BlockType.BuilderBlock: return BuilderId;
            default: return ClayBase;
        }
    }

    // The clay color whose offset indexes into the clay ID table (0..15).
    public static int ClayIdOffsetOf(ClayColor color) => ClayOffset[(int)color];

    // Numeric ID for a block type + clay color.
    public static int IdOf(BlockType type, ClayColor clay)
    {
        if (type == BlockType.Clay)
            return ClayBase + ClayIdOffsetOf(clay);
        return TypeId(type);
    }

    // Convenience: numeric ID carried by an inventory/stacked item.
    public static int IdOf(SlotBlock item)
    {
        if (item == null) return BarrierId;
        return IdOf(item.blockType, item.clayColor);
    }

    // Convert an ID back into a (blockType, clayColor). Returns clay as the type
    // for any clay ID. Non-clay type IDs map to their block type (clay ignored).
    public static (BlockType, ClayColor) FromId(int id)
    {
        if (id >= ClayBase && id <= ClayBase + 15)
        {
            int offset = id - ClayBase;
            return (BlockType.Clay, ClayColorFromOffset(offset));
        }

        switch (id)
        {
            case WoodId: return (BlockType.Wood, ClayColor.White);
            case StoneId: return (BlockType.Stone, ClayColor.White);
            case IronId: return (BlockType.Iron, ClayColor.White);
            case DiamondId: return (BlockType.Diamond, ClayColor.White);
            case BuilderId: return (BlockType.BuilderBlock, ClayColor.White);
            default: return (BlockType.Barrier, ClayColor.White); // Barrier or unknown
        }
    }

    // True when an ID maps to a real block (clay range or a named type).
    public static bool IsKnown(int id)
    {
        if (id >= ClayBase && id <= ClayBase + 15) return true;
        switch (id)
        {
            case BarrierId:
            case WoodId:
            case StoneId:
            case IronId:
            case DiamondId:
            case BuilderId:
                return true;
            default:
                return false;
        }
    }

    // Reverse map from a clay ID offset (0..15) back to its ClayColor.
    public static ClayColor ClayColorFromOffset(int offset)
    {
        int clamped = Mathf.Clamp(offset, 0, 15);
        for (int i = 0; i < ClayOffset.Length; i++)
            if (ClayOffset[i] == clamped)
                return (ClayColor)i;
        return ClayColor.White;
    }
}
