# 🧱 Terrain & Block System

> **Module:** Core World & Building Mechanics  
> **Status:** Draft / Implementation  
> **Base Scale:** 1 Unit = 1x1x1 Cube  

## 1. Grid & Placement Rules
All terrain and building elements snap to a strict 3D grid. 
* **Base Unit:** The fundamental unit of measurement is the **1x1x1 Cube**.
* **Bounding Boxes:** All shapes must fit perfectly within a 1x1x1 bounding box to ensure seamless grid snapping and collision detection.

### Placement Categories
Blocks are divided into two logical categories for the placement engine:

* **Category A: Uniform (Context-Independent)**
  * These blocks look and behave exactly the same regardless of where the player is standing or what surface they click. 
  * *Example:* Cube.
* **Category B: Directional (Surface-Dependent)**
  * These blocks change their orientation based on the **surface normal** (the specific face of the block the player is clicking) and/or the **player's facing direction**.
  * *Examples:* Slab, Ramp, Cylinder.

---

## 2. Geometric Shapes (Primitives)

| # | Shape Name | Category | Dimensions | Placement Logic & Description |
| :--- | :--- | :---: | :--- | :--- |
| **1** | **Cube** | **A** | `1 x 1 x 1` | The baseline block. Symmetrical. Placed identically regardless of player position or clicked surface. |
| **2** | **Cuboid (Slab)** | **B** | `1 x 0.5 x 1` (H)<br>`1 x 1 x 0.5` (V) | **Surface-Snapping:**<br>• Click **Top/Bottom** face ➔ Places **Horizontal** slab (floor/ceiling).<br>• Click **Side** face ➔ Places **Vertical** slab (wall).<br>*No manual rotation keys required.* |
| **3** | **Right Triangular Prism (Ramp)** | **B** | `1 x 1 x 1` *(Bounding Box)* | **Player-Facing Snapping:**<br>Used for ramps and roofs. The **hypotenuse (sloped face) will always face the player** at the moment of placement (exactly like Minecraft stairs). |
| **4** | **Cylinder** | **B** | `1 x 1 x 1` *(Bounding Box)* | **Surface-Normal Snapping:**<br>Circular cross-section (Diameter: 1). Its central axis aligns with the surface normal. <br>• Click Top/Bottom ➔ Stands vertically.<br>• Click Side ➔ Sticks out horizontally. |

---

## 3. Block Types (Materials)

Materials dictate the physical and visual properties of the shapes.

### 3.1. Clay
* **Visual:** Matte, slightly textured, opaque.
* **Physical:** Solid, standard collision, affected by gravity (if enabled in game rules).
* **Variants:** Comes in **16 different colors** (see Color Palette below).

### 3.2. Barrier
* **Visual:** 100% transparent (or highly translucent with a subtle grid/wireframe overlay so players can see it in bright environments).
* **Physical:** Blocks player/enemy movement. *(Dev Note: Decide if it blocks projectiles during prototyping).*
* **Color:** Uncolored / Always transparent.

---

## 4. Color Palette (16 Basic Colors)

Standardized hex codes for the 16 Clay colors to ensure consistency across UI, 3D models, and lighting.

| Color Name | Hex Code | Visual Reference |
| :--- | :--- | :--- |
| **White** | `#F9F9F9` | ⬜ |
| **Light Gray** | `#C6C6C6` | 🌫️ |
| **Gray** | `#7E7E7E` | 🌑 |
| **Black** | `#1E1E1E` | ⬛ |
| **Brown** | `#8B5A2B` | 🟫 |
| **Red** | `#B02E26` | 🟥 |
| **Orange** | `#F9801D` | 🟧 |
| **Yellow** | `#FED83D` | 🟨 |
| **Lime** | `#71CC3A` | 🟩 |
| **Green** | `#5E7C16` | 🌲 |
| **Cyan** | `#169C9C` | 🩵 |
| **Light Blue** | `#3AB3DA` | 💧 |
| **Blue** | `#3C44AA` | 🟦 |
| **Purple** | `#8932B8` | 🟪 |
| **Magenta** | `#C74EBD` | 💖 |
| **Pink** | `#F38BAA` | 🌸 |

---

## 5. Interaction & Controls (Builder Mode)

How the player interacts with the terrain system in-hand.

* **Place Block:** `Right Click` / `RT`
* **Break/Remove Block:** `Left Click` / `LT`
* **Cycle Block Shape (Cube -> Slab -> Ramp -> Cylinder):** `Scroll Wheel` / `DPad` / `Number Keys`
* **Cycle Block Material (Clay Colors -> Barrier):** `Q` / `E` (or dedicated radial menu)

*(Note: Because Category B blocks auto-orient based on the surface/player facing, there are **no manual rotation keys** needed for placing blocks.)*

---

## 6. Open Questions / Dev Notes
- [ ] **Slab Stacking:** If a player places a Horizontal Slab on top of another Horizontal Slab, does it automatically merge into a full Cube? (Standard voxel behavior).
- [ ] **Ramp Stacking:** If a player places a Ramp on top of another Ramp, does it create a full block, or just a taller ramp?
- [ ] **Cylinder Collision:** Does the Cylinder use a perfect cylindrical mesh collider, or a simplified box collider for performance?
- [ ] **Barrier Collision:** Does the Barrier block projectiles (like arrows/bullets) or just player movement?
- [ ] **Ghost Preview:** The "Ghost Block" (preview before placing) must accurately reflect Category B logic. If the player looks at a wall while holding a Slab, the ghost block must visually snap to the wall vertically in real-time.