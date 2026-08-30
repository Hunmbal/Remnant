using System.Collections.Generic;
using UnityEngine;

public class ArenaGenerator : MonoBehaviour
{
    [Header("Arena Settings")]
    public int size = 100;
    public int height = 10;

    static readonly HashSet<Vector3Int> solidCells = new HashSet<Vector3Int>();

    // Query used by the Player for sneak edge-protection
    public static bool IsSolid(int x, int y, int z)
        => solidCells.Contains(new Vector3Int(x, y, z));

    void Start()
    {
        // Enforce 100x100 (older serialized instances may still have size=10)
        if (size <= 10)
            size = 100;

        // Clear any leftover blocks saved in the scene from previous versions
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        GenerateArena();
    }

    Material CreateClayMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);

        mat.color = color;
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);

        return mat;
    }

    Material CreateBarrierMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);

        Color semi = new Color(0.6f, 0.8f, 0.6f, 0.15f); // very faint green, mostly invisible
        mat.color = semi;
        mat.SetColor("_BaseColor", semi);
        mat.SetColor("_Color", semi);

        // Force transparent blending for this shader
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);

        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_ALPHABLEND_ON");

        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return mat;
    }

    void GenerateArena()
    {
        Material greenMat = CreateClayMaterial(BlockRegistry.GetClayColor(ClayColor.Green));   // #5E7C16
        Material blackMat = CreateClayMaterial(BlockRegistry.GetClayColor(ClayColor.Black));   // #1E1E1E
        Material brownMat = CreateClayMaterial(BlockRegistry.GetClayColor(ClayColor.Brown));   // #8B5A2B
        Material barrierMat = CreateBarrierMaterial();

        solidCells.Clear();

        // 1. FLOOR: Green/Black chessboard at y=0
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                Material mat = ((x + z) % 2 == 0) ? greenMat : blackMat;
                CreateBlock(new Vector3(x, 0, z), mat);
            }
        }

        // 2. CEILING: Barrier at y=height-1
        float topY = height - 1f;
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                CreateBlock(new Vector3(x, topY, z), barrierMat);
            }
        }

        // 3. WALLS: Barrier at edges, y=1 to y=height-2
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 0; x < size; x++)
            {
                CreateBlock(new Vector3(x, y, 0), barrierMat);
                CreateBlock(new Vector3(x, y, size - 1), barrierMat);
            }
            for (int z = 1; z < size - 1; z++)
            {
                CreateBlock(new Vector3(0, y, z), barrierMat);
                CreateBlock(new Vector3(size - 1, y, z), barrierMat);
            }
        }

        // 4. CENTER TEST PLATFORM: stepped brown-clay pyramid (1→4 blocks tall).
        // Each tier rises exactly 1 block, so every hop is a clean single jump.
        GeneratePyramid(brownMat);
    }

    void GeneratePyramid(Material mat)
    {
        // Tier footprint (x/z start..end inclusive) and height in blocks
        (int x0, int x1, int z0, int z1, int h)[] tiers = new (int, int, int, int, int)[]
        {
            (45, 54, 45, 54, 1), //  10x10  tier 1 (top at y=1)
            (47, 52, 47, 52, 2), //   6x6   tier 2 (top at y=2)
            (48, 51, 48, 51, 3), //   4x4   tier 3 (top at y=3)
            (49, 50, 49, 50, 4)  //   2x2   crown  (top at y=4)
        };

        foreach (var tier in tiers)
        {
            for (int x = tier.x0; x <= tier.x1; x++)
            {
                for (int z = tier.z0; z <= tier.z1; z++)
                {
                    for (int y = 0; y < tier.h; y++)
                    {
                        CreateBlock(new Vector3(x, y, z), mat);
                    }
                }
            }
        }
    }

    GameObject CreateBlock(Vector3 position, Material mat)
    {
        var key = new Vector3Int((int)position.x, (int)position.y, (int)position.z);
        if (!solidCells.Add(key))
            return null;

        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.transform.position = position;
        block.transform.parent = transform;
        block.GetComponent<Renderer>().material = mat;
        return block;
    }
}