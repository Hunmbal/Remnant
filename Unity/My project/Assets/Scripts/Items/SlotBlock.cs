using UnityEngine;

// A block/item stashed in a hotbar or inventory slot, ready to be placed.
[System.Serializable]
public class SlotBlock
{
    public BlockType blockType = BlockType.Clay;
    public ClayColor clayColor = ClayColor.Green;
    public Color customColor = Color.white;

    public BlockMaterial BlockMaterial => (blockType == BlockType.Barrier) ? BlockMaterial.Barrier : BlockMaterial.Clay;

    public Color GetColor()
    {
        if (blockType == BlockType.Clay)
            return BlockRegistry.GetClayColor(clayColor);
        return customColor;
    }

    public bool IsAir => false; // placeholder; always a real block
}
