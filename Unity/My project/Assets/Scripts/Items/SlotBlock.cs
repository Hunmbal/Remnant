using UnityEngine;

// A block/item stashed in a hotbar or inventory slot, ready to be placed.
[System.Serializable]
public class SlotBlock
{
    public BlockType blockType = BlockType.Clay;
    public ClayColor clayColor = ClayColor.Green;
    public Color customColor = Color.white;
    public int count = 1;   // number of blocks in this stack (1 = single / non-stacking)

    public BlockMaterial BlockMaterial
        => blockType == BlockType.Barrier ? BlockMaterial.Barrier
         : blockType == BlockType.BuilderBlock ? BlockMaterial.Builder
         : BlockMaterial.Clay;

    // Max count of this item per inventory stack (from its block definition).
    public int StackLimit => BlockDefs.StackLimit(blockType);

    public Color GetColor()
    {
        if (blockType == BlockType.Clay)
            return BlockRegistry.GetClayColor(clayColor);
        if (blockType == BlockType.BuilderBlock)
            return BlockRegistry.GetTypeColor(BlockType.BuilderBlock);
        return customColor;
    }

    // Numeric ID for this item (BlockIDs).
    public int Id => BlockIDs.IdOf(this);

    public bool IsAir => false; // placeholder; always a real block

    // Return a separate copy of this item with the given count. Dropping/picking
    // must never alias the inventory slot's object, or the stack counts will
    // corrupt each other.
    public SlotBlock Clone(int amount)
    {
        return new SlotBlock
        {
            blockType = blockType,
            clayColor = clayColor,
            customColor = customColor,
            count = amount,
        };
    }
}
