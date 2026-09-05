using UnityEngine;

public enum BlockMaterial
{
    Clay,
    Barrier,
    Builder
}

public enum ClayColor
{
    White = 0,
    LightGray = 1,
    Gray = 2,
    Black = 3,
    Brown = 4,
    Red = 5,
    Orange = 6,
    Yellow = 7,
    Lime = 8,
    Green = 9,
    Cyan = 10,
    LightBlue = 11,
    Blue = 12,
    Purple = 13,
    Magenta = 14,
    Pink = 15
}

public static class BlockRegistry
{
    public static readonly Color[] ClayColors = new Color[]
    {
        HexToColor("#F9F9F9"), // White
        HexToColor("#C6C6C6"), // Light Gray
        HexToColor("#7E7E7E"), // Gray
        HexToColor("#1E1E1E"), // Black
        HexToColor("#8B5A2B"), // Brown
        HexToColor("#B02E26"), // Red
        HexToColor("#F9801D"), // Orange
        HexToColor("#FED83D"), // Yellow
        HexToColor("#71CC3A"), // Lime
        HexToColor("#5E7C16"), // Green
        HexToColor("#169C9C"), // Cyan
        HexToColor("#3AB3DA"), // Light Blue
        HexToColor("#3C44AA"), // Blue
        HexToColor("#8932B8"), // Purple
        HexToColor("#C74EBD"), // Magenta
        HexToColor("#F38BAA")  // Pink
    };

    public static Color GetClayColor(ClayColor color) => ClayColors[(int)color];

    public static Color GetClayColor(int index) => ClayColors[Mathf.Clamp(index, 0, 15)];

    // Standard tint for non-clay block types (used by the creative panel icons,
    // dropped items, and when loading a saved map).
    public static Color GetTypeColor(BlockType type)
    {
        switch (type)
        {
            case BlockType.Wood: return new Color(0.62f, 0.43f, 0.22f);
            case BlockType.Stone: return new Color(0.55f, 0.55f, 0.55f);
            case BlockType.Iron: return new Color(0.8f, 0.82f, 0.85f);
            case BlockType.Diamond: return new Color(0.4f, 0.9f, 0.85f);
            case BlockType.BuilderBlock: return new Color(0.95f, 0.9f, 0.3f); // marker stripe yellow
            default: return Color.white;
        }
    }

    static Color HexToColor(string hex)
    {
        Color c = Color.white;
        ColorUtility.TryParseHtmlString(hex, out c);
        return c;
    }
}