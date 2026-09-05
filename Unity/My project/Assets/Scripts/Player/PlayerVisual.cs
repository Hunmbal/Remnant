using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [Header("Character & Animations (Assets/Resources/Models/main)")]
    public GameObject characterPrefab;
    public RuntimeAnimatorController animatorController;

    [Header("Fallback Figure (only if no assets load)")]
    public Color torsoColor = new Color(0.976f, 0.976f, 0.976f);
    public Color limbColor = new Color(0.776f, 0.776f, 0.776f);

    Camera playerCamera;
    GameObject modelInstance;
    Animator animator;

    GameObject fpsArmL;
    GameObject fpsArmR;

    string currentState = "";

#if UNITY_EDITOR
    const string ControllerPath = "Assets/Animations/PlayerController.controller";
    int controllerRetries;
    const int maxControllerRetries = 10;
#endif

    const int hiddenLayer = 2;

    void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();

        LoadCharacter();
        BuildFirstPersonArms();

        if (playerCamera != null)
            playerCamera.cullingMask = ~(1 << hiddenLayer);
    }

    void Update()
    {
#if UNITY_EDITOR
        if (animator != null &&
            animator.runtimeAnimatorController == null &&
            controllerRetries < maxControllerRetries)
        {
            TryApplyEditorController();
            controllerRetries++;
        }
#endif
    }

    void LoadCharacter()
    {
        // The character is built entirely in code from cylinders (limbs, torso)
        // and a sphere (head). No imported model is used.
        BuildStickFigure();
    }

    void NormalizeModel()
    {
        if (modelInstance == null) return;

        SkinnedMeshRenderer smr = modelInstance.GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr == null)
        {
            modelInstance.transform.localScale = Vector3.one;
            return;
        }

        Bounds b = smr.bounds;
        if (b.size.y <= 0.01f) return;

        float scale = 1.8f / b.size.y;
        if (Mathf.Abs(scale - 1f) > 0.01f)
        {
            modelInstance.transform.localScale = Vector3.one * scale;
            b = smr.bounds;
        }

        float step = b.min.y - transform.position.y;
        Vector3 p = modelInstance.transform.position;
        p.y -= step;
        modelInstance.transform.position = p;
    }

    void SetupAnimation()
    {
        animator = modelInstance.GetComponent<Animator>();
        if (animator == null)
            animator = modelInstance.GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogWarning("PlayerVisual: no Animator found on the character model.");
            return;
        }

        if (animatorController != null)
        {
            animator.runtimeAnimatorController = animatorController;
            return;
        }

#if UNITY_EDITOR
        TryApplyEditorController();
#else
        Debug.LogWarning("PlayerVisual: assign animatorController in the Inspector (build without editor).");
#endif
    }

#if UNITY_EDITOR
    void TryApplyEditorController()
    {
        var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        if (controller != null && animator != null)
            animator.runtimeAnimatorController = controller;
    }
#endif

    public void SetAnimationState(bool sneaking, bool sprinting, float speed, bool grounded, float verticalVelocity)
    {
        if (animator == null) return;

        animator.SetFloat("Speed", speed);
        animator.SetBool("Sneaking", sneaking);
        animator.SetBool("Airborne", !grounded);

        string stateName = sneaking ? "sneaking" : (!grounded ? "airborne" : (sprinting ? "running" : (speed < 1.8f ? "idle" : "walking")));
        if (stateName != currentState)
        {
            currentState = stateName;
            Debug.Log("[Animation] " + stateName);
        }
    }

    void SetLayerRecursive(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    public void SetThirdPerson(bool thirdPerson)
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera != null)
            playerCamera.cullingMask = thirdPerson ? -1 : ~(1 << hiddenLayer);

        if (modelInstance != null)
            SetLayerRecursive(modelInstance, thirdPerson ? 0 : hiddenLayer);

        if (fpsArmL != null) fpsArmL.SetActive(!thirdPerson);
        if (fpsArmR != null) fpsArmR.SetActive(!thirdPerson);
    }

    // Toggle a wireframe box that shows the player's collision hitbox (freelook + H).
    // Built lazily the first time it is shown; sized from the box collider.
    Transform hitboxRoot;
    float hitboxHeight = -1f; // cached so we only rebuild on sneak/normal change

    public void SetHitboxVisible(bool visible)
    {
        if (hitboxRoot == null && visible)
            BuildHitbox();

        if (hitboxRoot != null)
            hitboxRoot.gameObject.SetActive(visible);
    }

    // Rebuild the wireframe to match the box collider's current size. The
    // box is centered at (0, height*0.5, 0) and spans local Y 0..height.
    public void SetHitboxSize(float height, float radius)
    {
        if (hitboxRoot == null) return;
        if (Mathf.Approximately(height, hitboxHeight)) return;

        hitboxHeight = height;
        float width = radius * 2f;
        hitboxRoot.localScale = new Vector3(width, height, width);
        // Center the box vertically so its bottom sits at local Y 0 (the feet).
        hitboxRoot.localPosition = new Vector3(0f, height * 0.5f, 0f);
    }

    void BuildHitbox()
    {
        hitboxRoot = new GameObject("Hitbox").transform;
        hitboxRoot.SetParent(transform, false);

        Material lineMat = MakeMat(new Color(0.2f, 1f, 0.35f, 1f));

        // Corners of a unit cube centered on the origin; scaled later to the box.
        Vector3[] c =
        {
            new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),  new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),  new Vector3(-0.5f, 0.5f, 0.5f)
        };
        int[] pairs =
        {
            0,1, 1,2, 2,3, 3,0, // bottom
            4,5, 5,6, 6,7, 7,4, // top
            0,4, 1,5, 2,6, 3,7  // verticals
        };
        for (int i = 0; i < pairs.Length; i += 2)
        {
            GameObject edge = new GameObject("Edge");
            edge.transform.SetParent(hitboxRoot, false);
            LineRenderer lr = edge.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, c[pairs[i]]);
            lr.SetPosition(1, c[pairs[i + 1]]);
            lr.startWidth = 0.04f;
            lr.endWidth = 0.04f;
            lr.alignment = LineAlignment.View;
            lr.sharedMaterial = lineMat;
            lr.useWorldSpace = false;
        }
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

    GameObject AddBodyPart(string name, PrimitiveType type, Vector3 localPos, Vector3 scale, Color color, Transform parent, int layer)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.layer = layer;
        part.transform.parent = parent;
        part.transform.localPosition = localPos;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().material = MakeMat(color);

        Collider c = part.GetComponent<Collider>();
        if (c != null) Destroy(c);

        return part;
    }

    // The character is a simple robot: cylinder limbs + torso and a sphere head.
    // Unity's cylinder primitive is height 2, radius 0.5 (axis Y), so scale.y is
    // half the desired height and scale.x/z are the desired diameter. The sphere
    // primitive is radius 0.5, so scale is the desired diameter.
    void BuildStickFigure()
    {
        AddBodyPart("Leg L", PrimitiveType.Cylinder, new Vector3(-0.11f, 0.4f, 0f), new Vector3(0.18f, 0.4f, 0.18f), limbColor, transform, hiddenLayer);
        AddBodyPart("Leg R", PrimitiveType.Cylinder, new Vector3(0.11f, 0.4f, 0f), new Vector3(0.18f, 0.4f, 0.18f), limbColor, transform, hiddenLayer);
        AddBodyPart("Torso", PrimitiveType.Cylinder, new Vector3(0f, 1.125f, 0f), new Vector3(0.42f, 0.325f, 0.42f), torsoColor, transform, hiddenLayer);
        AddBodyPart("Arm L", PrimitiveType.Cylinder, new Vector3(-0.22f, 1.09f, 0f), new Vector3(0.13f, 0.36f, 0.13f), limbColor, transform, hiddenLayer);
        AddBodyPart("Arm R", PrimitiveType.Cylinder, new Vector3(0.22f, 1.09f, 0f), new Vector3(0.13f, 0.36f, 0.13f), limbColor, transform, hiddenLayer);
        AddBodyPart("Head", PrimitiveType.Sphere, new Vector3(0f, 1.6f, 0f), new Vector3(0.4f, 0.4f, 0.4f), torsoColor, transform, hiddenLayer);
    }

    void BuildFirstPersonArms()
    {
        if (playerCamera == null) return;

        // Forearms: cylinders (height 2 primitive -> scale.y is half height).
        GameObject armL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        armL.name = "FPS Arm L";
        armL.transform.parent = playerCamera.transform;
        armL.transform.localPosition = new Vector3(-0.22f, -0.14f, 0.26f);
        armL.transform.localScale = new Vector3(0.11f, 0.25f, 0.11f);
        armL.transform.localRotation = Quaternion.Euler(0f, 0f, 22f);
        armL.GetComponent<Renderer>().material = MakeMat(limbColor);
        Collider c1 = armL.GetComponent<Collider>();
        if (c1 != null) Destroy(c1);
        fpsArmL = armL;

        GameObject armR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        armR.name = "FPS Arm R";
        armR.transform.parent = playerCamera.transform;
        armR.transform.localPosition = new Vector3(0.22f, -0.14f, 0.26f);
        armR.transform.localScale = new Vector3(0.11f, 0.25f, 0.11f);
        armR.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
        armR.GetComponent<Renderer>().material = MakeMat(limbColor);
        Collider c2 = armR.GetComponent<Collider>();
        if (c2 != null) Destroy(c2);
        fpsArmR = armR;
    }
}