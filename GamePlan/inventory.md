#  Inventory System

> **Module:** User Interface & Item Management  
> **Trigger:** Press `R` to toggle Open/Close  
> **State Dependency:** Layout changes based on Player State (Default vs. Practice)

---

## 1. Core Inventory Layout (All States)

When the player presses `R`, the main inventory opens. 

### 1.1 Grid Specifications
* **Dimensions:** 2 Rows × 9 Columns (18 slots total).
* **Slot Shape:** Standard rounded squares (squircles), matching the hotbar aesthetic.
* **Diamond Slot:** **None.** Unlike the hotbar, the main inventory does not have a center diamond slot. All 18 slots are uniform.

### 1.2 Animation & Positioning
* **Position:** Anchored to the bottom-center of the screen, sitting directly above the Hotbar.
* **Open Animation:** The 2 rows smoothly **slide up** out from behind the Hotbar and fade in.
* **Close Animation:** The 2 rows smoothly **slide down** back behind the Hotbar and fade out.
* **Transition Speed:** ~0.2 seconds (snappy and responsive).

---

## 2. Mouse & Camera Control

Opening the inventory fundamentally changes how the player interacts with the game world.

* **Camera Lock:** The camera freezes in place. The player cannot look around while the inventory is open.
* **Mouse Detachment:** The mouse cursor is released from the center of the screen (`CursorLockMode.None`). 
* **Cursor Visibility:** The standard OS/game UI cursor becomes visible and can be moved freely across the screen to drag and drop items.
* **Re-attachment:** Upon closing the inventory (pressing `R` or `Esc`), the cursor instantly re-locks to the center of the screen and disappears.

---

## 3. Practice Mode Extension (Creative Inventory)

When the player is in **Practice State**, an **additional, separate inventory panel** opens alongside the main 2x9 inventory.

### 3.1 Creative Panel Layout
* **Dimensions:** 4 Rows × 9 Columns (36 slots total).
* **Position:** Anchored to the left or right side of the screen (leaving the center/bottom clear for the main 2x9 inventory and Hotbar).
* **Content:** Contains an infinite supply of all available game items for the player to pull from.

### 3.2 Category Tabs
At the top of the Creative Panel, there are 3 clickable tab headings to filter the items:
1. **Blocks** (Default Selected)
2. **Tools**
3. **Other Items**

### 3.3 Tab Contents
* **Blocks (Default):** Lists all available building blocks. 
  * Includes all color variations of Clay.
  * Includes Wood, Stone, Iron, Diamond, and any other structural blocks defined in the game.
* **Tools:** Lists all Swords (Levels 1-6), Hammers (Levels 1-5), and Shields (Levels 1-6).
* **Other Items:** Lists consumables (Bread, Super Bread), potions, and miscellaneous utility items.

---

## 4. Visual Layout Map

### Default / Spectator State (Press R)
```text
┌─────────────────────────────────────────────────────────┐
│ │
│ │
│ │
│ │
│ │
│ │
│ │
│ │
│ │
│ │
│ ┌─────────────────────────┐ │
│ │ [Main Inventory: 2x9] │ │
│ │ [Slot] [Slot] ... [Slot]│ │
│ │ [Slot] [Slot] ... [Slot]│ │
│ ─────────────────────────┘ │
│ │
│ ┌─────────────────────────┐ │
│ │ [Hotbar: 4 | ♦ | 4] │ │
│ └─────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```


### Practice State (Press R)
```text
┌─────────────────────────────────────────────────────────┐
│                                                         │
│  ┌──────────────────────┐                               │
│  │ [Blocks] [Tools] [Other]                             │
│  │ [Item] [Item] [Item] [Item] [Item] [Item] [Item] [Item] [Item] │
│  │ [Item] [Item] [Item] [Item] [Item] [Item] [Item] [Item] [Item] │
│  │ [Item] [Item] [Item] [Item] [Item] [Item] [Item] [Item] [Item] │
│  │ [Item] [Item] [Item] [Item] [Item] [Item] [Item] [Item] [Item] │
│  └──────────────────────┘                               │
│                                                         │
│                 ┌─────────────────────────┐             │
│                 │ [Main Inventory: 2x9]   │             │
│                 │ [Slot] [Slot] ... [Slot]│             │
│                 │ [Slot] [Slot] ... [Slot]│             │
│                 └─────────────────────────┘             │
│                                                         │
│                 ┌─────────────────────────             │
│                 │ [Hotbar: 4 | ♦ | 4]     │             │
│                 └─────────────────────────┘             │
└─────────────────────────────────────────────────────────┘
```

