using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [Header("Stick Figure (per GamePlan)")]
    public Color torsoColor = new Color(0.976f, 0.976f, 0.976f); // White #F9F9F9
    public Color limbColor = new Color(0.776f, 0.776f, 0.776f);  // Light Gray #C6C6C6

    Camera playerCamera;
    GameObject fpsArmL;
    GameObject fpsArmR;

    const int hiddenLayer = 2; // "Ignore Raycast": hidden from the local camera

    void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();

        BuildStickFigure();
        BuildFirstPersonArms();

        // The local camera must not render head/torso/arms (FPS view)
        if (playerCamera != null)
            playerCamera.cullingMask = ~(1 << hiddenLayer);
    }

    // First-person: hide the body + show FPS arms.
    // Third-person: show the full body + hide the FPS arms.
    public void SetThirdPerson(bool thirdPerson)
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera != null)
            playerCamera.cullingMask = thirdPerson ? -1 : ~(1 << hiddenLayer);

        if (fpsArmL != null) fpsArmL.SetActive(!thirdPerson);
        if (fpsArmR != null) fpsArmR.SetActive(!thirdPerson);
    }

    Material MakeMat(Color c)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Unlit/Color");

        Material m = new Material(s);
        m.color = c;
        m.SetColor("_BaseColor", c);
        m.SetColor("_Color", c);
        return m;
    }

    GameObject AddBodyPart(string name, Vector3 localPos, Vector3 scale, Color color, Transform parent, int layer)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.layer = layer;
        part.transform.parent = parent;
        part.transform.localPosition = localPos;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().material = MakeMat(color);

        // No physics — the CharacterController handles all player collision
        Collider c = part.GetComponent<Collider>();
        if (c != null) Destroy(c);

        return part;
    }

    void BuildStickFigure()
    {
        // Full 1.8-tall blocky stick figure (visible in third person / to other players)
        // Legs stay visible so looking down shows the lower body
        AddBodyPart("Leg L", new Vector3(-0.11f, 0.4f, 0f), new Vector3(0.18f, 0.8f, 0.18f), limbColor, transform, 0);
        AddBodyPart("Leg R", new Vector3( 0.11f, 0.4f, 0f), new Vector3(0.18f, 0.8f, 0.18f), limbColor, transform, 0);

        // Torso, arms, head are hidden from the local camera (FPS view)
        AddBodyPart("Torso", new Vector3(0f, 1.125f, 0f), new Vector3(0.44f, 0.65f, 0.28f), torsoColor, transform, hiddenLayer);
        AddBodyPart("Arm L", new Vector3(-0.31f, 1.09f, 0f), new Vector3(0.14f, 0.72f, 0.14f), limbColor, transform, hiddenLayer);
        AddBodyPart("Arm R", new Vector3( 0.31f, 1.09f, 0f), new Vector3(0.14f, 0.72f, 0.14f), limbColor, transform, hiddenLayer);
        AddBodyPart("Head", new Vector3(0f, 1.6f, 0f), new Vector3(0.4f, 0.4f, 0.4f), torsoColor, transform, hiddenLayer);
    }

    void BuildFirstPersonArms()
    {
        if (playerCamera == null) return;

        // Two simple arms rendered from the camera's viewpoint (bottom corners)
        GameObject armL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        armL.name = "FPS Arm L";
        armL.transform.parent = playerCamera.transform;
        armL.transform.localPosition = new Vector3(-0.34f, -0.14f, 0.26f);
        armL.transform.localScale = new Vector3(0.11f, 0.5f, 0.11f);
        armL.transform.localRotation = Quaternion.Euler(0f, 0f, 22f);
        armL.GetComponent<Renderer>().material = MakeMat(limbColor);
        Collider c1 = armL.GetComponent<Collider>();
        if (c1 != null) Destroy(c1);
        fpsArmL = armL;

        GameObject armR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        armR.name = "FPS Arm R";
        armR.transform.parent = playerCamera.transform;
        armR.transform.localPosition = new Vector3(0.34f, -0.14f, 0.26f);
        armR.transform.localScale = new Vector3(0.11f, 0.5f, 0.11f);
        armR.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
        armR.GetComponent<Renderer>().material = MakeMat(limbColor);
        Collider c2 = armR.GetComponent<Collider>();
        if (c2 != null) Destroy(c2);
        fpsArmR = armR;
    }
}