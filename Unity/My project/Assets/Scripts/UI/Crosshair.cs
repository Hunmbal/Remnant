using UnityEngine;

public class Crosshair : MonoBehaviour
{
    [Header("Crosshair")]
    [Tooltip("Texture resolution (power of two)")]
    public int textureSize = 128;
    [Tooltip("Black outline thickness (pixels, each side of the diagonal)")]
    public int outerHalf = 21;
    [Tooltip("White core thickness (pixels, each side of the diagonal)")]
    public int innerHalf = 7;
    [Tooltip("Opacity of the white core (0 = invisible, 1 = solid)")]
    [Range(0f, 1f)]
    public float whiteOpacity = 0.65f;
    [Tooltip("On-screen size in pixels")]
    public float displaySize = 32f;

    Texture2D texture;
    int lastSize;
    int lastOuter;
    int lastInner;
    float lastWhiteOpacity;

    void Awake()
    {
        Rebuild();
    }

    void Update()
    {
        // Rebuild live when inspector values change during Play mode
        if (texture == null ||
            texture.width != textureSize ||
            lastOuter != outerHalf ||
            lastInner != innerHalf ||
            !Mathf.Approximately(lastWhiteOpacity, whiteOpacity))
        {
            Rebuild();
        }
    }

    void Rebuild()
    {
        int size = Mathf.Max(8, textureSize);

        if (texture == null)
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        else
            texture.Reinitialize(size, size);

        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        int half = size / 2;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = x - half;
                int dy = y - half;

                // X shape = two diagonals: |dx-dy| and |dx+dy| bands
                bool diagA = Mathf.Abs(dx - dy) < outerHalf;
                bool diagB = Mathf.Abs(dx + dy) < outerHalf;

                Color col = Color.clear;
                if (diagA || diagB)
                {
                    col = Color.black;

                    bool coreA = Mathf.Abs(dx - dy) < innerHalf;
                    bool coreB = Mathf.Abs(dx + dy) < innerHalf;
                    if (coreA || coreB)
                        col = new Color(1f, 1f, 1f, whiteOpacity);
                }

                pixels[y * size + x] = col;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        lastSize = size;
        lastOuter = outerHalf;
        lastInner = innerHalf;
        lastWhiteOpacity = whiteOpacity;
    }

    void OnGUI()
    {
        if (texture == null) return;

        // Never inherit GUI.color from another script's OnGUI (it is static).
        GUI.color = Color.white;

        Rect rect = new Rect(
            Screen.width / 2f - displaySize / 2f,
            Screen.height / 2f - displaySize / 2f,
            displaySize,
            displaySize);

        GUI.DrawTexture(rect, texture);
    }
}