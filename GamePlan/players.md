# 🧍 Player Character Specifications

> **Module:** Player Entity & Physics  
> **Perspective:** First-Person (Local player), Third-Person (Other players)  
> **Visual Style:** 3D Stick-Figure  
> **Base Scale:** 1 Unit = 1 Block  

---

## 1. Character Model & Hitbox

### 1.1 Visual Model
* **Style:** 3D Stick-Figure.
* **Perspective Rendering:** 
  * **Local Player:** Rendered in First-Person. Only the player's arms, held item/weapon, and lower body (if looking down) are visible.
  * **Other Players:** Rendered in Third-Person. The full 3D stick-figure model is visible to everyone else in the lobby/server.
* **Animation:** Procedural or keyframed skeletal animation (walking, sprinting, jumping, crouching, attacking, blocking).

### 1.2 Physical Hitbox (Collision)
The player's physical presence in the world is a strict cuboid to ensure precise voxel interactions and grid-snapping.

| Dimension | Size (Blocks) | Description |
| :--- | :--- | :--- |
| **Width (X)** | `0.6` | Slightly smaller than a full 1x1 block to allow walking through 1-block gaps. |
| **Depth (Z)** | `0.6` | Matches width for uniform collision. |
| **Height (Y)** | `1.8` | Standard tall entity height. Allows jumping over 1-block high obstacles. |
| **Total Volume** | `0.648` blocks³ | |

* **Unity Implementation Note:** Use a `BoxCollider` (or a tightly configured `CharacterController` with minimal skin width) to enforce this exact 0.6x0.6x1.8 cuboid. 

---

## 2. Core Attributes (Stats)

All stats are managed server-authoritatively and synced to the client.

| Attribute | Max Value | Min Increment (Unit) | Data Type | Notes |
| :--- | :---: | :---: | :---: | :--- |
| **Health** | `10.0` | `0.25` | `float` | Displayed in UI as 10 squircles (each = 1.0 HP). |
| **Stamina** | `10.0` | `0.25` | `float` | Used for Flash-Step (costs 1.5). |
| **Armor** | `10.0` | `0.25` | `float` | Reduces incoming damage. |
| **XP** | `∞` (No Max) | `1` | `int` / `long` | Used for leveling/unlocks. No upper limit. |

---

## 3. Movement & Physics

Movement speeds are measured in **Blocks per Second (BPS)** (1 BPS = 1 Unity Unit/s). The movement model heavily incentivizes forward momentum.

### 3.1 Base Speeds

| Movement State | Speed (Blocks/s) | Trigger / Condition |
| :--- | :--- | :--- |
| **Forward Sprint** | `5.612` | Standard forward movement (`W`). |
| **Diagonal Sprint** | `5.612` | Forward + Strafe (`W+A` or `W+D`). **Matches Forward Sprint exactly.** |
| **Side / Backward** | `4.317` | Strafing or moving backward (`A`, `D`, or `S`). ~77% of forward speed. |
| **Sneaking** | `1.295` | Holding `Shift`. |
| **Sprint-Jumping** | `7.127` | Forward momentum + Jump (Horizontal air speed). |

### 3.2 Jump Mechanics

| Jump Attribute | Value | Notes |
| :--- | :--- | :--- |
| **Jump Height** | `1.25` blocks | Peak vertical displacement from the ground. |
| **Gravity** | *TBD* | Needs to be calculated in Unity to achieve exactly 1.25 block peak height based on jump velocity. |

---

## 4. Technical Implementation Notes (Unity)

### 4.1 Character Controller Setup
* **Recommendation:** Use Unity's **`CharacterController`** component.
* **Hitbox Setup:** 
  * `CharacterController.center`: `(0, 0.9, 0)` (Half of 1.8 height).
  * `CharacterController.radius`: `0.3` (Half of 0.6 width/depth). 

### 4.2 Movement Vector Logic (Crucial)
To achieve the specific rule where **Diagonal = Forward Speed**, but **Side/Back = Slower**, you cannot simply use `Vector3.Normalize`. You must calculate the speed based on the *forward/backward* input axis.

```csharp
// Pseudocode for Unity FixedUpdate
float forwardInput = Input.GetAxisRaw("Vertical"); // W = 1, S = -1
float strafeInput = Input.GetAxisRaw("Horizontal"); // D = 1, A = -1

float forwardSpeed = 5.612f;
float sideSpeed = 4.317f;

// Determine target speed based on input direction
float targetSpeed;
if (forwardInput > 0f) 
{
    // Moving Forward or Diagonal Forward (W, W+A, W+D)
    targetSpeed = forwardSpeed; 
} 
else 
{
    // Moving Sideways or Backward (A, D, S)
    targetSpeed = sideSpeed; 
}

// Create movement vector
Vector3 moveDir = (transform.forward * forwardInput + transform.right * strafeInput);

// Normalize ONLY if moving diagonally forward to prevent 1.414x speed boost,
// but keep the magnitude logic intact for side/back.
if (moveDir.sqrMagnitude > 1f) {
    moveDir.Normalize();
}

Vector3 velocity = moveDir * targetSpeed;

---

## 5. Camera & Field of View (FOV)

Dynamic FOV is used to visually communicate the player's movement speed and enhance the feeling of momentum.

### 5.1 FOV Specifications
| Movement State | Target FOV | Description |
| :--- | :---: | :--- |
| **Walking / Idle** | `70.0` | Default baseline FOV. Used when speed is ≤ 5.612 blocks/s (or sneaking). |
| **Sprinting** | `78.0` | Applied when the player is actively sprinting. Provides a sense of speed. |

### 5.2 Transition Mechanics
* **Interpolation:** The FOV does not snap instantly. It smoothly transitions using `Mathf.Lerp` to prevent jarring visual jumps.
* **Transition Speed:** `8.0` (Higher values = snappier, more immediate response; lower values = smoother, slower ramp-up).

### 5.3 Unity Implementation Snippet
Attach this logic to your player's camera controller. It should run in `Update()` (not `FixedUpdate`) to ensure smooth visual interpolation independent of the 30 TPS physics tick.

```csharp
using UnityEngine;

public class PlayerCameraFOV : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;

    [Header("FOV Settings")]
    public float baseFOV = 70f;           // Walking speed or less
    public float sprintFOV = 78f;         // Sprinting speed
    public float fovTransitionSpeed = 8f; // Higher = snappier

    private float currentTargetFOV;

    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        currentTargetFOV = baseFOV;
        playerCamera.fieldOfView = baseFOV;
    }

    void Update()
    {
        // TODO: Replace with your actual movement state logic
        // Example: bool isSprinting = (currentSpeed >= 5.6f) && !isSneaking;
        bool isSprinting = PlayerMovement.Instance.IsSprinting; 
        
        currentTargetFOV = isSprinting ? sprintFOV : baseFOV;

        // Smoothly interpolate the camera's FOV
        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView, 
            currentTargetFOV, 
            fovTransitionSpeed * Time.deltaTime
        );
    }
}