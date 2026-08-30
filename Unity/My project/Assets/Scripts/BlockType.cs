using UnityEngine;

public enum BlockMaterial
{
    Clay,
    Barrier
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

    static Color HexToColor(string hex)
    {
        Color c = Color.white;
        ColorUtility.TryParseHtmlString(hex, out c);
        return c;
    }
}