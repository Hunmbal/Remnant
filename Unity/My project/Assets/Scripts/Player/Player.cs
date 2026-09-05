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

    [Header("Flight (Builder Mode)")]
    public float flySpeed = 10.9f;    // Blocks/second in fly mode (Minecraft sprint-fly ~ x2 walk)
    public float flyAccel = 60f;      // Acceleration while flying (higher = snappier)

    [Header("Jump & Gravity")]
    public float jumpHeight = 1.25f; // Peak vertical displacement (blocks)
    public float gravity = -32f;     // Higher = snappier jump + faster fall

    [Header("Interaction Reach")]
    public float reach = 3f;         // Block highlight / interaction range (blocks)

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

    [Header("Sneak Edge Clamp")]
    [Tooltip("Radius of the cylindrical foot-support footprint (diameter 0.4 => radius 0.2). " +
             "The cylinder may hang entirely over the air; a direction is only blocked once it " +
             "would no longer touch the block. Center can reach one radius past the block edge.")]
    public float sneakFootRadius = 0.2f;

    public bool IsSneaking { get; private set; }
    public bool IsSprinting { get; private set; }
    public float CurrentSpeed { get; private set; }

    Rigidbody body;
    BoxCollider box;
    bool grounded;          // true when a grounded raycast reported support this tick
    bool isFlying;          // builder-mode flight (P toggles in Practice state)
    float boxWidth = 0.6f;  // real collider: box width/depth (0.6 = old radius 0.3 * 2)
    float footCylRadius = 0.2f; // calculation-only cylindrical footprint (diameter 0.4)
    float xRotation;
    float yRotation;
    float verticalVelocity;
    Vector3 airborneVelocity;
    bool isAirborne;
    bool thirdPerson;
    float currentMoveSpeed;
    BlockHighlight highlight;

    // Render interpolation: the collider stays at the authoritative fixed-TPS
    // position (simulated in FixedUpdate), while visualRoot is lerped between the
    // last two simulated positions each rendered frame so movement looks smooth
    // even on a high-refresh monitor.
    Transform visualRoot;
    Vector3 prevSimPos;
    Vector3 currSimPos;

    // Look control can be disabled while the inventory is open.
    bool lookEnabled = true;

    // Shared targeting result, computed once per frame in Update after Look().
    // The highlight, block breaker, and block placer all read this so the overlay,
    // break reach, and place reach are always the exact same raycast.
    Block targetBlock;
    // Closest aimed block (or null)
    Vector3? placePosition; // Cell to place into when targeting a block's face

    public Block TargetBlock => targetBlock;
    public Vector3? PlacePosition => placePosition;

    // Drop position: 1.5 blocks away from the player in the look direction. The
    // crosshair only determines direction, never which block to aim at. The item
    // rests on whatever surface is directly below that point.
    public Vector3 DropAnchor()
    {
        // Horizontal look direction (the player's yaw rotation matches the camera).
        Vector3 dir = transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            dir.Normalize();

        Vector3 p = transform.position + dir * 1.5f;

        // Rest on the surface below the drop point (a 0.25 cube sits 0.125 up).
        if (Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Ignore))
            p.y = hit.point.y + 0.125f;
        else
            p.y = 0.625f; // arena floor top is at y=0.5

        return p;
    }

    // Auto-pick up any dropped item the player's hitbox passes through.
    void CollectDroppedItems()
    {
        // The box collider is feet-up (center (0, h*0.5, 0), height h), so the
        // body is around transform.y + center. Overlap around the body center
        // with enough radius to reach floor-level drops.
        Collider[] cols = Physics.OverlapSphere(transform.position, 1.1f, ~0, QueryTriggerInteraction.Collide);
        if (cols.Length == 0) return;

        InventoryUI inv = GetComponent<InventoryUI>();
        if (inv == null) return;

        foreach (var col in cols)
        {
            DroppedItem di = col != null ? col.GetComponentInParent<DroppedItem>() : null;
            if (di == null) continue;
            if (inv.AddItem(di.item))
                Destroy(di.gameObject);
        }
    }

    public void EnableLook(bool enabled)
    {
        lookEnabled = enabled;
    }

    void Awake()
    {
        // Real collider is a BOX (0.6 wide x height x 0.6 deep), not the old
        // capsule. Rigidbody is kinematic: we drive all movement ourselves via
        // ray-cast collision in Move() and gravity, so nothing fights the box.
        var oldController = GetComponent<CharacterController>();
        if (oldController != null) Destroy(oldController);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        body = rb;

        Collider any = GetComponent<Collider>();
        if (any != null) Destroy(any);

        box = gameObject.AddComponent<BoxCollider>();
        ResizeBox(1.8f); // standing height
    }

    // Size the box collider to (boxWidth x height x boxWidth), bottom at local
    // Y 0 (the feet). Keeping the same dimensions the old hitbox wireframe used.
    void ResizeBox(float height)
    {
        if (box == null) return;
        box.size = new Vector3(boxWidth, height, boxWidth);
        box.center = new Vector3(0f, height * 0.5f, 0f);
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

        // Toggleable game state (Default / Practice) with the X key
        if (GetComponent<PlayerStateToggle>() == null)
            gameObject.AddComponent<PlayerStateToggle>();

        // Block breaking (hold LMB; instant in Practice)
        if (GetComponent<BlockBreaker>() == null)
            gameObject.AddComponent<BlockBreaker>();

        // Block placing / picking (Practice mode: RMB place, MMB pick)
        if (GetComponent<BlockPlacer>() == null)
            gameObject.AddComponent<BlockPlacer>();

        // Center-screen crosshair
        if (GetComponent<Crosshair>() == null)
            gameObject.AddComponent<Crosshair>();

        // Bottom-center hotbar (4 squircle | diamond | 4 squircle)
        if (GetComponent<Hotbar>() == null)
            gameObject.AddComponent<Hotbar>();

        // Health bar above the hotbar (10 squircles + numeric)
        if (GetComponent<HealthBar>() == null)
            gameObject.AddComponent<HealthBar>();

        // Right-bottom stats (armor | stamina | xp)
        if (GetComponent<Stats>() == null)
            gameObject.AddComponent<Stats>();

        // Block outline highlight (independent root object, not a child).
        // Destroy any stale highlight so only one clean frame exists.
        if (highlight == null)
        {
            BlockHighlight old = Object.FindObjectOfType<BlockHighlight>();
            if (old != null) Destroy(old.gameObject);
            highlight = new GameObject("BlockHighlight").AddComponent<BlockHighlight>();
        }

        // Inventory (R toggles; creative panel in Practice)
        if (GetComponent<InventoryUI>() == null)
            gameObject.AddComponent<InventoryUI>();

        // Chat (system commands; "/" or "T" to open)
        if (GetComponent<Chat>() == null)
            gameObject.AddComponent<Chat>();

        // Visual root: the renderable camera + character body live under this
        // child so they can be interpolated between sim ticks (the collider stays
        // fixed at the authoritative position on this root GameObject).
        visualRoot = new GameObject("VisualRoot").transform;
        visualRoot.SetParent(transform, false);
        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform c = transform.GetChild(i);
            if (c == visualRoot) continue;
            c.SetParent(visualRoot, true); // keep world transform
        }

        // The BOX on this root is the ONLY collider on the player: no body part,
        // arm, or camera ever collides. Strip anything that snuck in (e.g. a
        // CreatePrimitive child re-adding its collider) so all collisions stay
        // exclusively on the authoritative hitbox.
        foreach (Collider c in GetComponentsInChildren<Collider>(true))
        {
            if (c == box) continue;
            Destroy(c);
        }

        // Spawn at this map's spawn for the player's team (FFA slot when teamless or
        // their team slot is missing). Falls back to the old centered placement
        // only when the current map defines no spawns at all.
        ArenaGenerator arena = Object.FindObjectOfType<ArenaGenerator>();
        if (arena != null && arena.TryGetSpawn(PlayerData.Team, out Vector3 spawnPos))
        {
            transform.position = spawnPos;
        }
        else if (arena != null)
        {
            int c = arena.size / 2;
            transform.position = new Vector3(c - 0.5f, 1f, c + 6f);
        }
        else
        {
            transform.position = new Vector3(4.5f, 1f, 4.5f);
        }
    }


    void Update()
    {
        // Practice mode locks stats to maximum and prevents fall damage.
        if (PlayerStateManager.IsPractice)
        {
            health = 10f;
            stamina = 10f;
            armor = 10f;
        }

        // P toggles flight in Builder (Practice) mode. Losing builder mode while
        // flying drops you out of flight and back into normal gravity.
        if (PlayerStateManager.IsPractice && Input.GetKeyDown(KeyCode.P) && !Chat.IsLockingInput)
        {
            ToggleFly();
        }
        else if (!PlayerStateManager.IsPractice && isFlying)
        {
            isFlying = false;
            verticalVelocity = grounded ? 0f : -1f;
            Chat.Log("Fly: OFF (requires Builder mode)");
        }

        // Rendering/FOV/aim: frame-rate driven. Movement, gravity, and jumps tick
        // at the fixed 40 TPS rate in FixedUpdate instead.
        Look();
        UpdateTarget();
        GetComponent<PlayerVisual>()?.SetAnimationState(IsSneaking, IsSprinting, CurrentSpeed, grounded, verticalVelocity);
        UpdateFOV();
    }

    // Game simulation runs on a fixed 40 TPS timestep (ProjectSettings Fixed
    // Timestep = 0.025). Ray-cast collision (MoveBox), auto-jump, and item
    // pickup all tick here consistently regardless of the render framerate.
    void FixedUpdate()
    {
        prevSimPos = transform.position;
        CollectDroppedItems();
        Move();
        currSimPos = transform.position;
        Jump();
    }

    void LateUpdate()
    {
        // Render interpolation: place the visual root between the previous and
        // current simulated positions based on how far we are into the current
        // fixed step. The collider (this GameObject) stays at the authoritative
        // simulated position, so physics is unaffected.
        if (visualRoot != null)
        {
            float t = Time.fixedDeltaTime > 0f ? (Time.time - Time.fixedTime) / Time.fixedDeltaTime : 1f;
            visualRoot.position = Vector3.Lerp(prevSimPos, currSimPos, Mathf.Clamp01(t));
        }

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

        // Block overlay highlight uses the shared per-frame target. The system
        // relies on block centers sitting exactly on integer grid coords.
        if (highlight != null)
        {
            if (targetBlock != null)
                highlight.Show(targetBlock.transform.position);
            else
                highlight.Hide();
        }

        // In freelook, holding H shows the player's collision hitbox.
        PlayerVisual pv = GetComponent<PlayerVisual>();
        if (pv != null)
        {
            bool showHitbox = thirdPerson && Input.GetKey(KeyCode.H);
            pv.SetHitboxVisible(showHitbox);
            if (showHitbox)
                pv.SetHitboxSize(BoxHeight(), boxWidth * 0.5f);
        }
    }

    // Single raycast per frame powering the highlight, block breaking, and block
    // placing. Called after Look() so the camera is already oriented this frame.
    void UpdateTarget()
    {
        targetBlock = null;
        placePosition = null;
        if (playerCamera == null) return;

        Vector3 org = playerCamera.transform.position;
        Vector3 dir = playerCamera.transform.forward;

        RaycastHit[] hits = Physics.RaycastAll(org + dir * 0.01f, dir, reach);
        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            // Skip the player's own colliders (root BoxCollider + body)
            if (hit.collider.transform == transform ||
                hit.collider.transform.IsChildOf(transform))
                continue;

            Block b = hit.collider.GetComponent<Block>();
            if (b == null) continue;

            if (hit.distance < bestDist)
            {
                bestDist = hit.distance;
                targetBlock = b;
                // Cell to place a new block into: one step along the face normal.
                placePosition = b.transform.position + hit.normal;
            }
        }
    }

    void Look()
    {
        if (!lookEnabled) return;

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
        // While typing in chat, all game inputs are locked.
        bool typing = Chat.IsLockingInput;

        float forwardInput = typing ? 0f : Input.GetAxisRaw("Vertical");
        float strafeInput = typing ? 0f : Input.GetAxisRaw("Horizontal");

        // Shift no longer crouches while flying (it becomes the descend key).
        IsSneaking = !typing && Input.GetKey(KeyCode.LeftShift) && !isFlying;

        // Crouch height
        float targetHeight = IsSneaking ? 1.5f : 1.8f;
        if (!Mathf.Approximately(BoxHeight(), targetHeight))
            ResizeBox(targetHeight);

        Vector3 moveDir = transform.forward * forwardInput + transform.right * strafeInput;
        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        // Full flight control: bypasses gravity, jumping, and sneak edge-hold.
        if (isFlying)
        {
            MoveFly(moveDir);
            return;
        }

        bool touchingBox = BoxTouchesWall(moveDir);

        // Target speed
        float targetSpeed;
        if (IsSneaking)
            targetSpeed = sneakSpeed;
        else if (forwardInput > 0f)
            targetSpeed = touchingBox ? walkSpeed : forwardSpeed;
        else
            targetSpeed = sideSpeed;

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

        // --- Sneak edge protection (only while not jumping/falling) ---
        int by = Mathf.FloorToInt(transform.position.y - 0.5f); // ground cell below the feet
        if (IsSneaking && !isAirborne)
        {
            velocity = ApplySneakEdgeClamp(velocity);
        }

        // Grounding: the 0.4 cylinder may hang over the air, in which case the
        // collider's own ground probe is past the face and would let us fall.
        // While the cylindrical footprint still overlaps a solid block we keep the
        // feet pinned to that block's top surface (by + 0.5).
        bool supported = FootprintHasSupport(transform.position.x, transform.position.z, by);
        float landingTop = float.NegativeInfinity; // forced landing surface for this tick
        if (IsSneaking && !isAirborne && supported)
        {
            verticalVelocity = -1f;
            grounded = true;
            landingTop = by + 0.5f; // hold the edge: land on the block under the feet
        }
        else
        {
            grounded = verticalVelocity <= 0f && DetectGround();
            if (grounded && verticalVelocity < 0f)
                verticalVelocity = -1f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        if (verticalVelocity < -40f) verticalVelocity = -40f;
        velocity.y = verticalVelocity;

        // Move the box strictly via grid collision (axis separated) so walking
        // into a wall slides instead of stopping hard, and gravity stops at floors.
        MoveBox(velocity * Time.deltaTime, landingTop);

        // Update state
        if ((grounded
             || (IsSneaking && FootprintHasSupport(transform.position.x, transform.position.z, by)))
            && verticalVelocity <= 0f)
            isAirborne = false;

        CurrentSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        IsSprinting = (forwardInput > 0f) && !IsSneaking && !touchingBox;
    }

    // Builder-mode flight: WASD flies forward/strafe at fly speed, Space climbs,
    // Shift descends. The box still collides against the grid (MoveBox), so you
    // slide along builds instead of passing through them.
    void MoveFly(Vector3 moveDir)
    {
        bool typing = Chat.IsLockingInput;
        grounded = false;
        isAirborne = false;

        float targetSpeed = moveDir.sqrMagnitude > 0.001f ? flySpeed : 0f;
        currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, targetSpeed, flyAccel * Time.deltaTime);
        Vector3 velocity = moveDir * currentMoveSpeed;

        float verticalInput = typing ? 0f
            : (Input.GetKey(KeyCode.Space) ? 1f
            : (Input.GetKey(KeyCode.LeftShift) ? -1f : 0f));
        verticalVelocity = Mathf.MoveTowards(verticalVelocity, verticalInput * flySpeed, flyAccel * Time.deltaTime);
        velocity.y = verticalVelocity;

        MoveBox(velocity * Time.deltaTime, float.NegativeInfinity);

        CurrentSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        IsSprinting = false;
    }

    void ToggleFly()
    {
        isFlying = !isFlying;
        if (isFlying)
        {
            // Hovering keeps the current height; motion is fully velocity-driven.
            verticalVelocity = 0f;
            isAirborne = false;
            grounded = false;
            currentMoveSpeed = 0f;
        }
        else
        {
            // Exit flight: resume gravity (fall if mid-air, rest if grounded).
            verticalVelocity = grounded ? 0f : -1f;
        }
        Chat.Log(isFlying ? "Fly: ON" : "Fly: OFF");
    }

    float BoxHeight() => box != null ? box.size.y : 1.8f;

    // True if the box (depth 0.6, feet at Y0) would overlap any solid cell when
    // its feet are at (feetX, feetZ) while the body spans [minY, maxY] vertically.
    // Grid-based (no Physics casts): mirrors the sneak footprint logic further
    // down, but full-body. halfFoot is 0.3 minus a 1cm tolerance so the box
    // slides flush along walls without grinding on faces.
    const float halfFoot = 0.29f;

    bool BoxOverlapsGrid(float feetX, float feetZ, float minY, float maxY)
    {
        for (int cy = Mathf.FloorToInt(minY) - 1; cy <= Mathf.FloorToInt(maxY) + 1; cy++)
        {
            if (!(cy - 0.5f < maxY && cy + 0.5f > minY)) continue;
            for (int cx = Mathf.FloorToInt(feetX - halfFoot) - 1; cx <= Mathf.FloorToInt(feetX + halfFoot) + 1; cx++)
            {
                if (!(cx - 0.5f < feetX + halfFoot && cx + 0.5f > feetX - halfFoot)) continue;
                for (int cz = Mathf.FloorToInt(feetZ - halfFoot) - 1; cz <= Mathf.FloorToInt(feetZ + halfFoot) + 1; cz++)
                {
                    if (cz - 0.5f < feetZ + halfFoot && cz + 0.5f > feetZ - halfFoot &&
                        ArenaGenerator.IsSolid(cx, cy, cz))
                        return true;
                }
            }
        }
        return false;
    }

    // True if pressing into / along 'dir' at torso height would hit a block
    // (used to pick walk-vs-sprint speed like side collisions).
    bool BoxTouchesWall(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return false;
        float fx = transform.position.x + dir.x * 0.3f;
        float fz = transform.position.z + dir.z * 0.3f;
        float bottom = transform.position.y + 0.4f; // torso band (skip feet/head)
        return BoxOverlapsGrid(fx, fz, bottom, bottom + 1.2f);
    }

    // Grid-based ground probe. The box stands only when its feet are actually
    // planted on a block top (no early-fire, so falls stay full-speed to the
    // landing tick). Support comes from the full bottom-face footprint, exactly
    // like the sweep collision: the hitbox is a real solid box.
    bool DetectGround()
    {
        float top = HighestTopUnderFeet(transform.position.x, transform.position.z, transform.position.y);
        return top > float.NegativeInfinity && Mathf.Abs(top - transform.position.y) < 0.05f;
    }

    // Move the box by 'delta', colliding per axis against the solid grid so we
    // slide along walls and stop/flatten on floors/ceilings. Falling lands on the
    // highest block top under the FULL bottom-face footprint (the hitbox is a
    // solid cube: it can never descend through a block it is still touching), or
    // on 'landingTop' when the sneaking edge-hold pins the feet.
    void MoveBox(Vector3 delta, float landingTop)
    {
        if (box == null) return;
        Vector3 pos = transform.position;

        // X axis (slide if blocked)
        if (delta.x != 0f)
        {
            float nx = pos.x + delta.x;
            if (!BoxOverlapsGrid(nx, pos.z, pos.y, pos.y + BoxHeight()))
                pos.x = nx;
        }

        // Z axis (slide if blocked)
        if (delta.z != 0f)
        {
            float nz = pos.z + delta.z;
            if (!BoxOverlapsGrid(pos.x, nz, pos.y, pos.y + BoxHeight()))
                pos.z = nz;
        }

        // Y axis (gravity / jump). The descent is swept in fine steps: the box
        // can never move down into a position where its body overlaps a solid
        // cell, so falling past the side of a build can't clip through its face.
        if (delta.y < 0f)
        {
            float top = landingTop > float.NegativeInfinity
                ? landingTop
                : HighestTopUnderFeet(pos.x, pos.z, pos.y);
            float targetY = pos.y + delta.y;
            if (top > float.NegativeInfinity &&
                targetY <= top + 0.001f &&
                !BoxOverlapsGrid(pos.x, pos.z, top, top + BoxHeight()))
            {
                // Feet reached the top surface this tick and the body fits
                // flush on it: land. (The fit check keeps an overhanging side
                // cell from producing a floating embed below it.)
                grounded = true;
                verticalVelocity = 0f;
                pos.y = top;
            }
            else
            {
                pos.y = DescendSwept(pos.x, pos.z, targetY);
                if (top > float.NegativeInfinity &&
                    pos.y <= top + 0.001f &&
                    !BoxOverlapsGrid(pos.x, pos.z, pos.y, pos.y + BoxHeight()))
                {
                    grounded = true;
                    verticalVelocity = 0f;
                    pos.y = top;
                }
            }
        }
        else if (delta.y > 0f)
        {
            // Ceiling (full head footprint).
            float ny = pos.y + delta.y;
            if (!BoxOverlapsGrid(pos.x, pos.z, ny, ny + BoxHeight()))
                pos.y = ny;
            else
                verticalVelocity = Mathf.Min(verticalVelocity, 0f);
        }

        transform.position = pos;
    }

    // Move the feet straight down from the current height toward 'targetY' in
    // fine steps, stopping at the last height where the whole body stays clear
    // of the solid grid. This is the down-side of the axis-separated slide: it
    // guarantees gravity never tunnels the box through a side face, wall, or
    // overhang regardless of the local build geometry. Reaching 'targetY'
    // unhindered means plain free-fall speed (the caller then re-checks landing).
    float DescendSwept(float fx, float fz, float targetY)
    {
        float y = transform.position.y;
        if (targetY >= y) return y;

        // Fast path: nothing blocks at the target -> plain free-fall, exact.
        if (!BoxOverlapsGrid(fx, fz, targetY, targetY + BoxHeight()))
            return targetY;

        // The target sits inside a solid (a side/overhang cell beside the box).
        // Walk back up in fine steps to the highest height where the whole body
        // is free again, so gravity stops flush against the obstruction instead
        // of tunneling through it.
        const float step = 0.05f;
        float g = targetY;
        while (g < y && BoxOverlapsGrid(fx, fz, g, g + BoxHeight()))
            g += step;
        return Mathf.Min(g, y);
    }

    // Highest block top the box's bottom-face footprint (0.6 wide, halfFoot)
    // could rest on at (feetX, feetY, feetZ), considering only surfaces at or
    // below the current feet. One column scan per footprint cell; the box can
    // only settle where its whole underside sits on solid tops. NegativeInfinity
    // when nothing supports the underside at that height.
    float HighestTopUnderFeet(float feetX, float feetZ, float maxTop)
    {
        float best = float.NegativeInfinity;
        // Same enumeration as BoxOverlapsGrid (margin cells included + continuous
        // overlap filter) so ground probes and blocking agree on which cells are
        // under the footprint at the edge. Without the margin, a partially
        // clipped cell (box overhanging a block edge) was invisible to the probe
        // while still blocking the descent, freezing the box in mid-air.
        int cx0 = Mathf.FloorToInt(feetX - halfFoot) - 1;
        int cx1 = Mathf.FloorToInt(feetX + halfFoot) + 1;
        int cz0 = Mathf.FloorToInt(feetZ - halfFoot) - 1;
        int cz1 = Mathf.FloorToInt(feetZ + halfFoot) + 1;
        for (int cx = cx0; cx <= cx1; cx++)
        {
            if (!(cx - 0.5f < feetX + halfFoot && cx + 0.5f > feetX - halfFoot)) continue;
            for (int cz = cz0; cz <= cz1; cz++)
            {
                if (!(cz - 0.5f < feetZ + halfFoot && cz + 0.5f > feetZ - halfFoot)) continue;
                for (int cy = Mathf.FloorToInt(maxTop - 0.5f); cy >= 0; cy--)
                {
                    if (ArenaGenerator.IsSolid(cx, cy, cz))
                    {
                        if (cy + 0.5f > best) best = cy + 0.5f;
                        break;
                    }
                }
            }
        }
        return best;
    }



Vector3 ApplySneakEdgeClamp(Vector3 velocity)
    {
        // Simple rule: the cylindrical foot hitbox (diameter 0.4) may hang entirely
        // over the air, but a movement direction is only allowed while the cylinder
        // still touches the block (its rim on the block edge, or on the block).
        // Predicted on each axis: if, after moving that axis this frame, the
        // footprint still overlaps a block, the move is allowed; once the whole
        // cylinder would leave the block, that axis is blocked. Solid ground ahead
        // means the footprint always overlaps, so nothing is blocked; at a ledge it
        // lets the center travel up to one radius (r) past the block face then stops
        // with the trailing rim just touching the block edge.
        float r = sneakFootRadius;
        float dt = Time.deltaTime;
        if (dt <= 0f) return velocity;

        float px = transform.position.x;
        float pz = transform.position.z;
        int by = Mathf.FloorToInt(transform.position.y - 0.5f);

        float allowed;

        // Predict each axis against its own footprint-overlap so corner cases and
        // sliding along edges stay symmetric (all four sides behave the same).
        if (velocity.x > 0.001f)
        {
            float pred = px + velocity.x * dt;
            if (FootprintHasSupport(pred, pz, by))
            {
                // Still touching a block after moving: allowed. Cap the center so
                // the trailing rim stays on the block when the way ahead is open.
                int cx = Mathf.FloorToInt(pred + 0.5f);
                float edge = cx + 0.5f + r;
                allowed = Mathf.Max(0f, edge - px);
                velocity.x = Mathf.Min(velocity.x, allowed / dt);
            }
            else
            {
                velocity.x = 0f; // whole cylinder would leave the block: blocked
            }
        }
        else if (velocity.x < -0.001f)
        {
            float pred = px + velocity.x * dt;
            if (FootprintHasSupport(pred, pz, by))
            {
                int cx = Mathf.FloorToInt(pred + 0.5f);
                float edge = cx - 0.5f - r;
                allowed = Mathf.Max(0f, px - edge);
                velocity.x = Mathf.Max(velocity.x, -allowed / dt);
            }
            else
            {
                velocity.x = 0f;
            }
        }

        if (velocity.z > 0.001f)
        {
            float pred = pz + velocity.z * dt;
            if (FootprintHasSupport(px, pred, by))
            {
                int cz = Mathf.FloorToInt(pred + 0.5f);
                float edge = cz + 0.5f + r;
                allowed = Mathf.Max(0f, edge - pz);
                velocity.z = Mathf.Min(velocity.z, allowed / dt);
            }
            else
            {
                velocity.z = 0f;
            }
        }
        else if (velocity.z < -0.001f)
        {
            float pred = pz + velocity.z * dt;
            if (FootprintHasSupport(px, pred, by))
            {
                int cz = Mathf.FloorToInt(pred + 0.5f);
                float edge = cz - 0.5f - r;
                allowed = Mathf.Max(0f, pz - edge);
                velocity.z = Mathf.Max(velocity.z, -allowed / dt);
            }
            else
            {
                velocity.z = 0f;
            }
        }

        return velocity;
    }

    // True if the cylindrical footprint at (px, pz) overlaps ANY solid cell at
    // the given ground level. Any overlap means a footprint rim still touches a
    // block — the rest of the cylinder may hang entirely over the air. The range
    // is nudged inward by a tiny epsilon so a rim sitting exactly on a block edge
    // counts as touching (avoids a hair-thin gap making the player fall).
    bool FootprintHasSupport(float px, float pz, int by)
    {
        float r = sneakFootRadius;
        int xa = Mathf.FloorToInt(px - r - 0.01f + 0.5f);
        int xb = Mathf.FloorToInt(px + r + 0.01f + 0.5f);
        int za = Mathf.FloorToInt(pz - r - 0.01f + 0.5f);
        int zb = Mathf.FloorToInt(pz + r + 0.01f + 0.5f);
        for (int X = xa; X <= xb; X++)
        {
            for (int Z = za; Z <= zb; Z++)
            {
                if (ArenaGenerator.IsSolid(X, by, Z)) return true;
            }
        }
        return false;
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
        // While typing in chat, all game inputs are locked.
        if (Chat.IsLockingInput) return;

        // While Space is held, auto-jump every time you land (Minecraft-style)
        if (Input.GetButton("Jump") && grounded && !isAirborne)
        {
            // Lock horizontal direction + speed at the moment of takeoff
            float forwardInput = Input.GetAxisRaw("Vertical");
            float strafeInput = Input.GetAxisRaw("Horizontal");

            Vector3 moveDir = transform.forward * forwardInput + transform.right * strafeInput;
            if (moveDir.sqrMagnitude > 1f)
            {
                moveDir.Normalize();
            }

            // Match the grounded speed rules: touching a block keeps the jump at
            // walking speed, sneaking caps it even lower; only open forward ground
            // gets the sprint-jump speed. Otherwise use side speed.
            bool touchingBox = moveDir.sqrMagnitude > 0.001f && BoxTouchesWall(moveDir);
            float takeoffSpeed;
            if (IsSneaking)
                takeoffSpeed = sneakSpeed;
            else if (forwardInput > 0f)
                takeoffSpeed = touchingBox ? walkSpeed : sprintJumpSpeed;
            else
                takeoffSpeed = sideSpeed;

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