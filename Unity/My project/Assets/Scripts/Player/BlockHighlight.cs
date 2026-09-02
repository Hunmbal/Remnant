using UnityEngine;

public class BlockHighlight : MonoBehaviour
{
    void Awake()
    {
        // Guarantee a clean frame: identity rotation, unit scale, center origin.
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Unlit/Color");

        Material edgeMat = new Material(s);
        edgeMat.color = Color.black;
        edgeMat.SetColor("_BaseColor", Color.black);
        edgeMat.SetColor("_Color", Color.black);

        // 4 X-axis edges: at y=±0.5, z=±0.5, bars run along X (length 1)
        for (int i = 0; i < 4; i++)
        {
            float y = (i & 1) == 0 ? 0.5f : -0.5f;
            float z = (i & 2) == 0 ? 0.5f : -0.5f;
            BuildEdge(new Vector3(0f, y, z), new Vector3(1f, 0.03f, 0.03f));
        }
        // 4 Y-axis edges: at x=±0.5, z=±0.5, bars run along Y (length 1)
        for (int i = 0; i < 4; i++)
        {
            float x = (i & 1) == 0 ? 0.5f : -0.5f;
            float z = (i & 2) == 0 ? 0.5f : -0.5f;
            BuildEdge(new Vector3(x, 0f, z), new Vector3(0.03f, 1f, 0.03f));
        }
        // 4 Z-axis edges: at x=±0.5, y=±0.5, bars run along Z (length 1)
        for (int i = 0; i < 4; i++)
        {
            float x = (i & 1) == 0 ? 0.5f : -0.5f;
            float y = (i & 2) == 0 ? 0.5f : -0.5f;
            BuildEdge(new Vector3(x, y, 0f), new Vector3(0.03f, 0.03f, 1f));
        }

        gameObject.SetActive(false);
    }

    void BuildEdge(Vector3 localPos, Vector3 scale)
    {
        GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        edge.name = "Edge";
        edge.transform.SetParent(transform, false);
        edge.transform.localPosition = localPos;
        edge.transform.localScale = scale;

        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Unlit/Color");
        Material mat = new Material(s);
        mat.color = Color.black;
        mat.SetColor("_BaseColor", Color.black);
        mat.SetColor("_Color", Color.black);
        edge.GetComponent<Renderer>().material = mat;

        Collider c = edge.GetComponent<Collider>();
        if (c != null) Destroy(c);
    }

    public void Show(Vector3 worldPos)
    {
        // Only position matters — transform stays scale 1 / identity rotation.
        transform.localScale = Vector3.one;
        transform.position = worldPos;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}