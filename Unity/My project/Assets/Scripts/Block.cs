using UnityEngine;

public class Block : MonoBehaviour
{
    public BlockMaterial material = BlockMaterial.Clay;
    public ClayColor clayColor = ClayColor.Green;

    protected Renderer renderer;
    protected BoxCollider collider;

    void Awake()
    {
        renderer = GetComponent<Renderer>();
        collider = GetComponent<BoxCollider>();

        ApplyMaterial();
    }

    void ApplyMaterial()
    {
        switch (material)
        {
            case BlockMaterial.Clay:
                ApplyClayColor(clayColor);
                if (collider != null) collider.isTrigger = false;
                break;

            case BlockMaterial.Barrier:
                ApplyBarrierMaterial();
                if (collider != null) collider.isTrigger = false;
                break;
        }
    }

    void ApplyClayColor(ClayColor color)
    {
        if (renderer != null)
        {
            // Use the material assigned in Inspector, just change its color
            if (renderer.sharedMaterial != null)
            {
                renderer.sharedMaterial.color = BlockRegistry.GetClayColor(color);
            }
            else
            {
                renderer.material.color = BlockRegistry.GetClayColor(color);
            }
        }
    }

    void ApplyBarrierMaterial()
    {
        if (renderer != null)
        {
            // Method 1: Just set alpha to 0 on the existing material
            // This works if the material has transparency enabled
            if (renderer.sharedMaterial != null)
            {
                renderer.sharedMaterial.color = Color.clear;
                renderer.sharedMaterial.SetFloat("_Mode", 2);
                renderer.sharedMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                renderer.sharedMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                renderer.sharedMaterial.SetInt("_ZWrite", 0);
            }
            // Method 2: Assign a transparent material from Inspector
            // Or use: renderer.material = new Material(Shader.Find("Transparent/Cutout")); 
        }
    }
}