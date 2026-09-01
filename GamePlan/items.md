# 🛠️ Items Specifications

> **Module:** Items, Combat & Mining Mechanics  
> **Purpose:** Defines the stats, levels, and interaction rules for all tools and blocks.

---

## 1. Core Tool Attributes

Every tool in the game is defined by a combination of the following attributes:
* **Damage:** Base damage dealt to other players (for weapons).
* **Break Time Multiplier:** How much faster the tool breaks a block compared to the base time (e.g., `x2` means it takes half the time).
* **Break Type:** Determines *which* blocks the tool is allowed to break (Type 1 or Type 2).
* **Reduction:** A shield-specific attribute that dictates how much incoming damage is divided/reduced.

---

## 2. Block Definitions

All blocks have a **Base Break Time** (in seconds or ticks, to be finalized in `mechanics.md`) and a **Break Type**.

| Block Name | Base Break Time | Break Type | Notes |
| :--- | :---: | :---: | :--- |
| **Clay** | `3` | `1` | Basic building material. | 
| **Wood** | `5` | `1` | Standard structural material. |
| **Stone** | `7` | `1` | Durable structural material. |
| **Iron** | `5` | `2` | Requires advanced tools to break. |
| **Diamond** | `16` | `2` | Highly durable, requires top-tier tools. |

Notes: 
- Clay blocks also have a color attribute unlike other blocks
- All blocks have an attribute "bool isArenaBlock". Arena blocks cant be broken, only blocks placed by player can be broken
- Barrier block cannot be placed by player (can only be used in practice state)
---

## 3. Tool Specifications

### 3.1 Swords (Levels 1–6)
Swords are primarily for combat but also provide a baseline mining speed.

| Level | Damage | Break Time Multiplier | Notes |
| :---: | :---: | :---: | :--- |
| **1** | `4` | `x1` (Normal speed) | Baseline weapon. |
| **2** | `5` | `x1` | |
| **3** | `6` | `x1` | |
| **4** | `7` | `x2` (Twice as fast) | |
| **5** | `8` | `x2` | |
| **6** | `10` | `x3` (Three times as fast)| Top-tier weapon. |

### 3.2 Hammers (Levels 1–5)
Hammers are specialized mining tools that deal moderate melee damage. Their mining effectiveness changes drastically based on the **Block Type**.

| Level | Damage | Effect on **Type 1** Blocks<br>*(Clay, Wood, Stone)* | Effect on **Type 2** Blocks<br>*(Iron, Diamond)* |
| :---: | :---: | :--- | :--- |
| **1** | `1` | `x2` faster | ❌ **Cannot Break** |
| **2** | `2` | `x3` faster | ❌ **Cannot Break** |
| **3** | `4` | `x4` faster | `x1` (Normal speed) |
| **4** | `5` | `x4` faster | `x2` faster |
| **5** | `6` | `x4` faster | `x4` faster |

**⚠️ Hammer Rules (Hard Constraints):**
1. **Type 1 Blocks:** Can be broken by *any* level of hammer (Levels 1–5).
2. **Type 2 Blocks:** Can **ONLY** be broken by Hammers of **Level 3 or higher**. Levels 1 and 2 will yield `0` damage to the block.

### 3.3 Shields (Levels 1–6)
Shields are held in the off-hand (or via Right-Click block) to mitigate incoming damage. 

| Level | Damage Reduction Multiplier | Effect (Example: vs 10 Damage Hit) |
| :---: | :---: | :--- |
| **1** | `x2` | Takes `5` damage |
| **2** | `x3` | Takes `3.33` damage |
| **3** | `x4` | Takes `2.5` damage |
| **4** | `x6` | Takes `1.66` damage |
| **5** | `x8` | Takes `1.25` damage |
| **6** | `x10` | Takes `1.0` damage |

Note: To apply sheild, player must place it in slot 5 only

### 3.4 Unarmed (Fists)
When a player has no tool equipped in their main hand, they use their fists.

* **Damage:** `0.5` (Deals minimal damage, takes 20 hits to kill a max-health player).
* **Block Breaking:** 
  * **Type 1 Blocks:** Extremely slow (e.g., `x1` multiplier).
  * **Type 2 Blocks:** ❌ **Cannot Break**.

---