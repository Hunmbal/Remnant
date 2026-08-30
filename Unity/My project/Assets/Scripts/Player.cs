using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Stats (Core Attributes)")]
    public float health = 10f;     // Max 10.0, increments 0.25
    public float stamina = 10f;    // Max 10.0, increments 0.25
    public float armor = 10f;      // Max 10.0, increments 0.25
    public long xp = 0;            // No max

    [Header("Movement Speeds (Blocks/Second)")]
    public float forwardSpeed = 5.612f;   // W, W+A, W+D (sprinting / open ground)
    public float walkSpeed = 4.317f;      // Forward while touching a block
    public float sideSpeed = 4.317f;      // A, D, S (77% of forward)
    public float sneakSpeed = 1.295f;     // Shift
    public float sprintJumpSpeed = 7.127f; // Forward + Jump (horizontal air speed)
    public float sprintTransitionSpeed = 8f; // Acceleration toward sprint speed (Higher = snappier)

    [Header("Jump & Gravity")]
    public float jumpHeight = 1.25f; // Peak vertical displacement (blocks)
    public float gravity = -32f;     // Higher = snappier jump + faster fall

    [Header("Camera & FOV")]
    public Camera playerCamera;
    public float sensitivityX = 10f;
    public float sensitivityY = 10f;
    public float baseFOV = 70f;            // Walking / idle
    public float sprintFOV = 78f;          // Sprinting
    public float fovTransitionSpeed = 8f;  // Higher = snappier

    [Header("Freelook (Third-Person Camera)")]
    public float freelookHeight = 1.9f;  // Camera height above the player
    public float freelookDistance = 4f;  // Camera distance behind the player

    public bool IsSneaking { get; private set; }
    public bool IsSprinting { get; private set; }
    public float CurrentSpeed { get; private set; }

    CharacterController controller;
    float xRotation;
    float yRotation;
    float verticalVelocity;
    Vector3 airborneVelocity;
    bool isAirborne;
    bool thirdPerson;
    float currentMoveSpeed;

    void Awake()
    {
        // CharacterController per spec: center (0,0.9,0), radius 0.3, height 1.8
        // (0.6 x 1.8 x 0.6 hitbox via radius 0.3 = 0.6 width/depth)
        controller = GetComponent<CharacterController>();
        if (controller == null)
            controller = gameObject.AddComponent<CharacterController>();

        controller.height = 1.8f;
        controller.radius = 0.3f;
        controller.center = new Vector3(0f, 0.9f, 0f);

        // Remove conflicting physics components
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);
        Collider any = GetComponent<Collider>();
        if (any != null) Destroy(any);
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            playerCamera = camObj.AddComponent<Camera>();
        }

        // First-person: camera as child at eye level
        if (playerCamera.transform.parent != transform)
        {
            playerCamera.transform.SetParent(transform, false);
            playerCamera.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        }

        playerCamera.fieldOfView = baseFOV;

        // Build the visible stick-figure body + FPS arms
        if (GetComponent<PlayerVisual>() == null)
            gameObject.AddComponent<PlayerVisual>();

        // Center-screen crosshair
        if (GetComponent<Crosshair>() == null)
            gameObject.AddComponent<Crosshair>();

        // Spawn south of the center platform, standing on the chess floor
        ArenaGenerator arena = Object.FindObjectOfType<ArenaGenerator>();
        if (arena != null)
        {
            int c = arena.size / 2;
            transform.position = new Vector3(c - 0.5f, 1f, c + 6f);
        }
        else
        {
            transform.position = new Vector3(4.5f, 1f, 4.5f);
        }
    }


    bool IsSolidAt(Vector3 point)
    {
        // Check if there's a solid block at the given world position
        int bx = Mathf.FloorToInt(point.x);
        int by = Mathf.FloorToInt(point.y);
        int bz = Mathf.FloorToInt(point.z);
        return ArenaGenerator.IsSolid(bx, by, bz);
    }

    void Update()
    {
        Look();
        Move();
        Jump();
        UpdateFOV();
    }

    void LateUpdate()
    {
        if (playerCamera == null) return;

        // Hold V for third-person (freelook-style)
        bool freelookHeld = Input.GetKey(KeyCode.V);

        if (freelookHeld != thirdPerson)
        {
            thirdPerson = freelookHeld;
            PlayerVisual visual = GetComponent<PlayerVisual>();
            if (visual != null) visual.SetThirdPerson(thirdPerson);
        }

        if (thirdPerson)
        {
            // Camera behind the player, pushed in if a block is in the way
            Vector3 eye = transform.position + Vector3.up * freelookHeight;
            Vector3 back = -transform.forward;
            float dist = freelookDistance;

            if (Physics.Raycast(eye + back * 0.5f, back, out RaycastHit hit, dist, ~(1 << 2)))
            {
                dist = Mathf.Max(0.5f, hit.distance - 0.25f);
            }

            playerCamera.transform.localPosition = new Vector3(0f, freelookHeight, -dist);
        }
        else
        {
            // Lower eye level while sneaking
            float eyeHeight = IsSneaking ? 1.33f : 1.6f;
            playerCamera.transform.localPosition = new Vector3(0f, eyeHeight, 0f);
        }
    }

    void Look()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * sensitivityX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensitivityY;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

        if (playerCamera != null)
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void Move()
    {
        float forwardInput = Input.GetAxisRaw("Vertical");
        float strafeInput = Input.GetAxisRaw("Horizontal");

        IsSneaking = Input.GetKey(KeyCode.LeftShift);

        // Crouch height
        float targetHeight = IsSneaking ? 1.5f : 1.8f;
        if (!Mathf.Approximately(controller.height, targetHeight))
        {
            controller.height = targetHeight;
            controller.center = new Vector3(0f, targetHeight * 0.5f, 0f);
        }

        bool touchingBox = (controller.collisionFlags & CollisionFlags.Sides) != 0;

        // Target speed
        float targetSpeed;
        if (IsSneaking)
            targetSpeed = sneakSpeed;
        else if (forwardInput > 0f)
            targetSpeed = touchingBox ? walkSpeed : forwardSpeed;
        else
            targetSpeed = sideSpeed;

        Vector3 moveDir = transform.forward * forwardInput + transform.right * strafeInput;
        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        // Air control
        Vector3 velocity;
        if (isAirborne)
        {
            velocity = airborneVelocity;
        }
        else
        {
            currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, targetSpeed, sprintTransitionSpeed * Time.deltaTime);
            velocity = moveDir * currentMoveSpeed;
        }

        // --- Sneak edge protection (only when grounded) ---
        if (IsSneaking && controller.isGrounded && !isAirborne)
        {
            velocity = ApplySneakEdgeClamp(velocity);
        }

        // Gravity
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -1f;

        verticalVelocity += gravity * Time.deltaTime;
        if (verticalVelocity < -40f) verticalVelocity = -40f;
        velocity.y = verticalVelocity;

        // Move
        controller.Move(velocity * Time.deltaTime);

        // Update state
        if (controller.isGrounded && verticalVelocity <= 0f)
            isAirborne = false;

        CurrentSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        IsSprinting = (forwardInput > 0f) && !IsSneaking && !touchingBox;
    }


    Vector3 ApplySneakEdgeClamp(Vector3 velocity)
    {
        const float edgeEps = 0.01f;
        float dt = Time.deltaTime;
        if (dt <= 0f) return velocity;

        int by = Mathf.FloorToInt(transform.position.y - 0.5f);
        int CellIndex(float v) => Mathf.FloorToInt(v + 0.5f);

        int cx = CellIndex(transform.position.x);
        int cz = CellIndex(transform.position.z);

        if (velocity.x > 0.001f && !ArenaGenerator.IsSolid(cx + 1, by, cz))
        {
            float edge = cx + 0.5f - edgeEps;
            float allowed = Mathf.Max(0f, edge - transform.position.x);
            velocity.x = Mathf.Min(velocity.x, allowed / dt);
        }
        else if (velocity.x < -0.001f && !ArenaGenerator.IsSolid(cx - 1, by, cz))
        {
            float edge = cx - 0.5f + edgeEps;
            float allowed = Mathf.Max(0f, transform.position.x - edge);
            velocity.x = Mathf.Max(velocity.x, -allowed / dt);
        }

        if (velocity.z > 0.001f && !ArenaGenerator.IsSolid(cx, by, cz + 1))
        {
            float edge = cz + 0.5f - edgeEps;
            float allowed = Mathf.Max(0f, edge - transform.position.z);
            velocity.z = Mathf.Min(velocity.z, allowed / dt);
        }
        else if (velocity.z < -0.001f && !ArenaGenerator.IsSolid(cx, by, cz - 1))
        {
            float edge = cz - 0.5f + edgeEps;
            float allowed = Mathf.Max(0f, transform.position.z - edge);
            velocity.z = Mathf.Max(velocity.z, -allowed / dt);
        }

        return velocity;
    }

    
    bool HasGroundSupport(Vector3 point)
    {
        // Cast a short ray downward from 'point' to see if it hits a solid block.
        // The block layer is assumed to be at integer y positions, but we'll just
        // check if there's any collider below.
        float checkDistance = 0.2f; // enough to reach the block surface
        if (Physics.Raycast(point, Vector3.down, out RaycastHit hit, checkDistance))
        {
            // You may also want to verify that the hit object is a block.
            // If you have a specific layer for blocks, use a layer mask.
            return true;
        }
        return false;
    }

    void ClampToSupportedEdge(float forwardInput, float strafeInput)
    {
        Vector3 pos = transform.position;
        int bx = Mathf.FloorToInt(pos.x);
        int bz = Mathf.FloorToInt(pos.z);
        // The block level the player is standing ON (its cell index), i.e. the
        // level whose top surface is at the feet. Neighbors are checked on this
        // same level, so walking onto a same-height surface is allowed.
        int supportY = Mathf.FloorToInt(pos.y);

        if (strafeInput < 0f && !ArenaGenerator.IsSolid(bx - 1, supportY, bz))
            pos.x = bx;              // don't cross the west edge
        else if (strafeInput > 0f && !ArenaGenerator.IsSolid(bx + 1, supportY, bz))
            pos.x = bx + 1f;         // don't cross the east edge

        if (forwardInput < 0f && !ArenaGenerator.IsSolid(bx, supportY, bz - 1))
            pos.z = bz;              // don't cross the south edge
        else if (forwardInput > 0f && !ArenaGenerator.IsSolid(bx, supportY, bz + 1))
            pos.z = bz + 1f;         // don't cross the north edge

        transform.position = pos;
    }

    void Jump()
    {
        // While Space is held, auto-jump every time you land (Minecraft-style)
        if (Input.GetButton("Jump") && controller.isGrounded)
        {
            // Lock horizontal direction + speed at the moment of takeoff
            float forwardInput = Input.GetAxisRaw("Vertical");
            float strafeInput = Input.GetAxisRaw("Horizontal");

            // Sprint-jump speed when leaping forward, side/back speed otherwise
            float takeoffSpeed = (forwardInput > 0f) ? sprintJumpSpeed : sideSpeed;

            Vector3 moveDir = transform.forward * forwardInput + transform.right * strafeInput;
            if (moveDir.sqrMagnitude > 1f)
            {
                moveDir.Normalize();
            }

            airborneVelocity = moveDir * takeoffSpeed;
            isAirborne = true;

            // Jump velocity achieving exactly jumpHeight peak with gravity
            verticalVelocity = Mathf.Sqrt(2f * jumpHeight * -gravity);
        }
    }

    void UpdateFOV()
    {
        if (playerCamera == null) return;

        float targetFOV = IsSprinting ? sprintFOV : baseFOV;

        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView,
            targetFOV,
            fovTransitionSpeed * Time.deltaTime);
    }
}