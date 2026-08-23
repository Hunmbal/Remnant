# 📋 Tab Hold UI (Overlay)

> **Module:** User Interface (Tab State)  
> **Trigger:** Holding the `Tab` key  

---

## 1. Visibility Rules

When `Tab` is held, the UI dynamically updates to prioritize match information:

### Elements that DISAPPEAR:
* **Mid-Top:** Continuous Ability Bar
* **Right-Mid:** Potion Effects
* **Left-Bottom:** Chat & Broadcasts

### Elements that REMAIN VISIBLE:
* **Mid-Bottom:** Abilities, Health, Hotbar
* **Right-Bottom:** Armor, Stamina, XP

---

## 2. Tablist (Top-Left Corner)

* **Layout:** Vertical list positioned in the top-left corner.
* **Content per row:**
  * Player Name (Color-coded by team)
  * Ping / Latency indicator
  * Alive / Dead status
* **Sorting:** Grouped by team, then alphabetically or by score.

---

## 3. Scoreboard (Right-Mid)

* **Layout:** Replaces the Potion Effects area in the right-middle of the screen.
* **Content:**
  * Team Colors/Names
  * Current Team Scores
  * Beds Broken (per team)
  * Kills / Final Kills
* **Style:** Clean, semi-transparent background to ensure readability over the game world.

---

## 📐 Visual Layout Map (Tab Held)

```text
 [ Top-Left: Tablist ] 
 [ Player Names  ]
 [ Ping / Status ]
 
 
 
 
 
 
 
 
 
 
 
 
 
                      [ Mid-Bottom ]        [ Right-Mid: Scoreboard ]
                        Abilities (3)         [ Team Scores  ]
                        Health (10 sq + #)    [ Beds Broken  ]
                        Hotbar (4 | ♦ | 4)    [ Kills        ]
                                              [ Right-Bottom ]
                                                Armor | Stamina | XP