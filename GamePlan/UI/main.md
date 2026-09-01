# 🖥️ UI Layout & HUD Design

> **Module:** User Interface  
> **Perspective:** First-Person  

---

## 1. Mid-Bottom (Core Combat & Inventory)

### Abilities
* **Layout:** 3 horizontal slots centered above the health bar.
* **Function:** Quick-cast active skills or tools.

### Health System
* **Visual:** 10 rounded squares (squircles).
  * **Filled:** Red (Current health).
  * **Empty:** Gray (Missing health).
  * **Half:** red Right triangle other half gray right triangle (Missing health).
* **Numeric Display:** Shows exact health value rounded to the nearest 0.5 (e.g., `18.5`).

### Hotbar
* **Layout:** 9 slots total, arranged symmetrically.
  * **Left Group:** 4 standard squircle slots.
  * **Center:** 1 diamond-shaped slot (Primary/Selected item).
  * **Right Group:** 4 standard squircle slots.

---

## 2. Mid-Top (Ultimate / Continuous Abilities)

### Continuous Ability Bar
* **Layout:** Horizontal progress bar centered at the very top of the screen.
* **Scale:** 0 to 100.
* **Function:** Tracks charge or duration for continuous abilities (e.g., sprints, ultimate skills).

---

## 3. Left-Bottom (Communication)

### Chat & Broadcasts
* **Layout:** Text overlay panel.
* **Function:** Displays system broadcasts (enemy spots, game events) and player chat messages.
* **Style:** Semi-transparent background with readable text formatting.

---

## 4. Right-Mid (Status Effects)

### Potion Effects
* **Layout:** Vertical stack of active buffs/debuffs.
* **Visual:** Icon + Name + Duration/Level indicator for each active effect.

---

## 5. Right-Bottom (Player Stats)

### Resource Counters
* **Layout:** Vertical or horizontal grouping of core player stats.
* **Components:**
  * **Armor:** Defense rating / durability. (blue sheild icon)
  * **Stamina:** Energy for sprinting/dodging. (green leaf icon)
  * **XP:** Experience points / level progress. (yellow Star icon)

---

## 📐 Visual Layout Map

```text
       [ Mid-Top: Continuous Ability Bar (0-100) ]
       
       
       
       
       
       
       
       
       
       
       
[ Left-Bottom ]       [ Mid-Bottom ]        [ Right-Mid ]
  Chat &                Abilities (3)         Potion Effects
  Broadcasts            Health (10 sq + #)
                        Hotbar (4 | ♦ | 4)
                                              [ Right-Bottom ]
                                                Armor | Stamina | XP