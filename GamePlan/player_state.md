# 🎮 Player States & Gamemodes

> **Module:** Player Gamemodes  
> **Purpose:** Defines the rules, restrictions, and capabilities for each game state.

---

## 1. Default Mode (Standard / Survival)
The standard gameplay experience where all core mechanics, combat, and resource management are active.

* **Block Breaking:** **Time-dependent.** The time it takes to break a block is calculated based on the **Tool Level** currently held.
* **Combat & Stats:** Health, Stamina, Armor, and XP are fully active. Players can take and deal damage.
* **Physics:** Standard gravity, collision, and fall damage are applied.
* **Resources:** Limited by inventory. Blocks and items must be gathered or crafted.

---

## 2. Practice Mode (Creative / Building)
A relaxed state designed for building, testing mechanics, and practicing movement without the pressure of survival.

* **Block Breaking:** **Instant.** Blocks break immediately (0 ticks) regardless of the tool held or block type.
* **Combat & Stats:** Health, Stamina, and Armor are disabled or locked to maximum. Fall damage is disabled.
* **Physics:** Standard gravity and collision apply, but no fall damage.
* **Resources:** Infinite. Players have access to an unlimited inventory of all blocks and items.

---

## 3. Ghost Mode (Free-Roam Observer)
A free-roaming state used for map exploration, admin oversight, or observing the battlefield from any angle without being tied to a specific player.

* **Movement:** **Free Flight / Noclip.** The player can fly freely in any direction, pass through solid blocks, and ignore all physical collisions.
* **Camera:** Detached and free-roaming. The player can look in any direction and move the camera independently of any other entity.
* **Interaction:** **Disabled.** Cannot break or place blocks, cannot attack, and cannot be attacked.
* **Visibility:** Invisible and intangible to players in Default or Practice modes.
* **UI:** Standard HUD is hidden. Only Chat and Tablist remain visible.

---

## 4. Spectator Mode (Locked Observer)
A restricted state used for players who have been eliminated in a match. They can observe the ongoing game, but are locked to the perspectives of living teammates.

* **Movement:** **Locked to Teammates.** The player cannot move their camera freely. They are physically attached to a living teammate's position.
* **Camera:** **View Cycling.** The player can cycle through the first-person or third-person views of their living teammates (e.g., using `Left/Right Mouse` or `Q/E` to switch targets).
* **Interaction:** **Disabled.** Cannot break or place blocks, cannot attack, and cannot be attacked.
* **Visibility:** Invisible and intangible to players in Default or Practice modes.
* **UI:** Standard HUD is hidden. The UI displays the name and health of the teammate currently being spectated. Chat and Tablist remain visible.