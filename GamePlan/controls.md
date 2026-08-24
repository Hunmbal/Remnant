# ⌨️ Control Scheme & Input Mapping

> **Module:** Player Input & Controls  
> **Perspective:** First-Person  
> **Target Platform:** PC (Keyboard & Mouse)  

---

## 1. Movement & Navigation

| Action | Key / Input | Mechanics & Rules |
| :--- | :---: | :--- |
| **Move Forward** | `W` | Standard movement. |
| **Move Left** | `A` | Standard movement. |
| **Move Backward** | `S` | Standard movement. |
| **Move Right** | `D` | Standard movement. |
| **Jump** | `Space` | Standard vertical jump. |
| **Crouch / Walk** | `Shift` | Reduces movement speed and player hitbox height. |
| **Flash-Step** | `Ctrl` | **Special Movement:** Performs a quick, large step (2x normal step distance) in the current movement direction.<br>• **Cost:** Instantly reduces Stamina by 1.5.<br>• **Restriction 1:** Cannot attack until the step is fully landed.<br>• **Restriction 2:** Cannot be used under **ANY** debuff.<br>• **Note:** This is a quick repositioning tool, not a super-speed dash. |

---

## 2. Combat & Interaction

| Action | Key / Input | Mechanics & Rules |
| :--- | :---: | :--- |
| **Hit / Attack** | `Left Mouse Button` | Swings equipped weapon or uses held item. |
| **Place / Block** | `Right Mouse Button` | **Context-Sensitive:**<br>• If holding a **Block**: Places the block in the world.<br>• If holding a **Sword**: Raises guard, blocking incoming hits and reducing damage taken by 50%. |

---

## 3. Inventory & Item Management

| Action | Key / Input | Mechanics & Rules |
| :--- | :---: | :--- |
| **Hotbar Slot 1** | `Q` | Selects the first hotbar slot. |
| **Hotbar Slots 2–9** | `2` to `9` | Directly selects the corresponding hotbar slot. |
| **Scroll Wheel** | `Scroll` | Cycles through hotbar slots sequentially.<br>• **Slot 5 Exception:** Reserved exclusively for **Diamond**. It scrolls normally but can only hold diamond items. |
| **Drop Item** | `1` | Drops the currently held item from the hand/hotbar onto the ground. |
| **Open Inventory** | `R` | Opens the full player inventory screen. |

---

## 4. Abilities

| Action | Key / Input | Mechanics & Rules |
| :--- | :---: | :--- |
| **Ability 1** | `E` | Activates primary ability. |
| **Ability 2** | `C` | Activates secondary ability. |
| **Ability 3** | `F` | Activates tertiary ability. *(Updated from Q to avoid conflict with Hotbar Slot 1)* |

---

## 5. System & UI

| Action | Key / Input | Mechanics & Rules |
| :--- | :---: | :--- |
| **Tablist & Scoreboard** | `Tab` (Hold) | Displays player list (Top-Left) and match scoreboard (Right-Mid). Hides Chat, Potion Effects, and Continuous Ability Bar while held. |
| **Settings / Pause** | `Esc` | Opens the game settings and pause menu. |

---

## ⚠️ Dev Notes & Implementation Details

1. **Flash-Step Direction Logic:** The Flash-Step should calculate its direction based on the *active WASD input vector*, ensuring the player steps exactly where they are trying to go, rather than just straight forward relative to the camera. The design specifies Flash-Step costs 1.5 Stamina.
2. **Sword Blocking Stamina:** Reduces in coming damage by half
3. **Slot 5 Diamond Restriction:** The inventory system will need a validation check: `if (slot == 5 && item.type != "Diamond") { preventPlacement(); }`.