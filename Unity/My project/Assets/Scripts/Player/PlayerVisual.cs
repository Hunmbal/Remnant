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
        if (characterPrefab == null)
            characterPrefab = Resources.Load<GameObject>("Models/main/Simple Stick");

        if (characterPrefab != null)
        {
            modelInstance = Instantiate(characterPrefab, transform);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;

            var colliders = modelInstance.GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders) Destroy(c);

            SetLayerRecursive(modelInstance, hiddenLayer);
            NormalizeModel();
            SetupAnimation();
            return;
        }

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

        Collider c = part.GetComponent<Collider>();
        if (c != null) Destroy(c);

        return part;
    }

    void BuildStickFigure()
    {
        AddBodyPart("Leg L", new Vector3(-0.11f, 0.4f, 0f), new Vector3(0.18f, 0.8f, 0.18f), limbColor, transform, 0);
        AddBodyPart("Leg R", new Vector3(0.11f, 0.4f, 0f), new Vector3(0.18f, 0.8f, 0.18f), limbColor, transform, 0);
        AddBodyPart("Torso", new Vector3(0f, 1.125f, 0f), new Vector3(0.44f, 0.65f, 0.28f), torsoColor, transform, hiddenLayer);
        AddBodyPart("Arm L", new Vector3(-0.31f, 1.09f, 0f), new Vector3(0.14f, 0.72f, 0.14f), limbColor, transform, hiddenLayer);
        AddBodyPart("Arm R", new Vector3(0.31f, 1.09f, 0f), new Vector3(0.14f, 0.72f, 0.14f), limbColor, transform, hiddenLayer);
        AddBodyPart("Head", new Vector3(0f, 1.6f, 0f), new Vector3(0.4f, 0.4f, 0.4f), torsoColor, transform, hiddenLayer);
    }

    void BuildFirstPersonArms()
    {
        if (playerCamera == null) return;

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