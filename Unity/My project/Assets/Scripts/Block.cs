using UnityEngine;

public class Block : MonoBehaviour
{
    public BlockMaterial material = BlockMaterial.Clay;
    public ClayColor clayColor = ClayColor.Green;

    // Time (seconds) to break this block with bare hands.
    // Each block type may define its own default here.
    public float breakTime = 5f;
    public bool breakable = true;

    protected Renderer blockRenderer;
    protected BoxCollider boxCollider;

    void Awake()
    {
        blockRenderer = GetComponent<Renderer>();
        boxCollider = GetComponent<BoxCollider>();

        ApplyMaterial();
    }

    void ApplyMaterial()
    {
        switch (material)
        {
            case BlockMaterial.Clay:
                // The generator assigns a correctly colored material. Only
                // override here when the renderer has no material yet.
                if (blockRenderer != null && blockRenderer.sharedMaterial == null)
                    ApplyClayColor(clayColor);
                if (boxCollider != null) boxCollider.isTrigger = false;
                break;

            case BlockMaterial.Barrier:
                ApplyBarrierMaterial();
                if (boxCollider != null) boxCollider.isTrigger = false;
                breakable = false;
                break;
        }
    }

    // Break this block: remove it from the arena grid and destroy it.
    public void BreakBlock()
    {
        Vector3Int key = new Vector3Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y),
            Mathf.RoundToInt(transform.position.z));

        ArenaGenerator.SetSolid(key.x, key.y, key.z, false);

        Destroy(gameObject);
    }

    void ApplyClayColor(ClayColor color)
    {
        if (blockRenderer != null)
            blockRenderer.material.color = BlockRegistry.GetClayColor(color);
    }

    void ApplyBarrierMaterial()
    {
        if (blockRenderer != null)
        {
            Material m = blockRenderer.material;
            m.color = Color.clear;
            m.SetFloat("_Mode", 2);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
        }
    }
}
