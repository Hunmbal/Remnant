# 🏠 Main Menu UI (Home Screen)

> **Module:** User Interface - Main Menu  
> **Screen:** Initial Launch / Title Screen  
> **Resolution:** 16:9 (Scalable)  

---

## 1. Overall Layout & Background

### 1.1 Background Art
* **Style:** 2D/2.5D illustrated landscape or a blurred, low-opacity render of the in-game voxel world.

### 1.2 Game Logo
* **Position:** Top-center of the screen.
* **Style:** Large, stylized, bold typography (matching the game's brand). 
* **Effect:** Subtle drop shadow or outer glow to separate it from the background.

---

## 2. Primary Navigation (Center)

Three large, prominent buttons stacked vertically in the center of the screen. These are the main entry points to the game.

| Button Text | Action / Flow |
| :--- | :--- |
| **Single Player** | Loads the local Open Flat Creative world. Bypasses networking. |
| **Community Servers** | Opens the Server Browser to browse, join, or host Player-Hosted lobbies (8 to 24 players). |
| **Official Servers** | Opens the Official Matchmaking queue for Dedicated Server games (up to 200 players across arenas). |

---

## 3. Secondary Navigation (Bottom Center)

Three smaller, square buttons aligned horizontally at the bottom-center of the screen.

| Icon | Function | Action / Flow |
| :---: | :--- | :--- |
| ⚙️ **(Gear)** | **Settings** | Opens the Settings overlay (Audio, Video, Controls, Gameplay). |
| 🌐 **(Globe + "EN")** | **Language / Region** | Opens a dropdown or modal to change UI language and server region preference. |
| ❓ **(Question Mark)**| **Help / About** | Opens credits, tutorial, patch notes, or support links. |

---

## 4. Friends Panel (Right Side)

A full-height panel occupying the entire right edge of the screen (approx. 20-25% of screen width).

### 4.1 Header
* **Title:** "FRIENDS"
* **Counter:** Dynamic text showing online count (e.g., "3 ONLINE").
* **Action Button:** "Add Friend" icon/button in the top right of the panel.

### 4.2 Tabs / Categories
Horizontal or vertical tabs to filter the list:
* **Online** (Default view)
* **In Game** (Shows current match/lobby)
* **In Lobby** (Shows current server lobby)
* **Offline**

### 4.3 Friend List Items
Each row represents a friend and contains:
* **Avatar/Icon:** Small square profile picture or default character silhouette.
* **Player Name:** Bold white text.
* **Status Text:** Smaller, colored text indicating state (e.g., "In Official Match" in green, "Idle" in gray).
* **Action Menu:** Hovering over a friend reveals a small dropdown or side-icons for:
  * **Invite to Party** (Only if they are not `In Game`)
  * **Message** (Opens Chat panel)
  * **View Profile**

---

## 5. Chat Panel (Bottom Left)

A compact, collapsible chat interface located in the bottom-left corner, sitting above the secondary navigation.

### 5.1 Tabs
Small tabs at the top of the chat box to switch contexts:
* **Friends** (Direct messages)
* **Party** (Chat with current party members)
* **All** (Global chat, if applicable in lobby)

### 5.2 Message History
* **Layout:** Scrollable list of recent messages (keeps last 20-50 messages in memory).
* **Format:** `[Player Name]: Message text`
* **Colors:** 
  * Player Name: Light blue/cyan (matches in-game chat).
  * System Messages: Yellow/Orange (e.g., "PlayerX has come online").
  * Timestamps: Optional, small gray text.

### 5.3 Input Field
* **Design:** A single-line text input box at the bottom of the panel.
* **Placeholder Text:** "Message friends..."
* **Send Action:** Pressing `Enter` sends the message. Pressing `Esc` closes the input focus.
* **Notification:** If a new message arrives while the chat is closed, a small red badge or glow appears on the chat tab.

### 5.4 Styling
* **Background:** Dark, semi-transparent solid color.
* **Size:** Compact by default (e.g., 300x150px), can be expanded to a larger window if clicked.

---

## 6. Visual Layout Map

```text
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│                          [ GAME LOGO ]                               │
│                                                                      │
│                                                                      │
│                      ─────────────────────┐                          │
│                      │    Single Player    │                         │
│                      └─────────────────────┘                         │
│                                                                      │
│                      ┌─────────────────────┐                         │
│                      │ Community Servers   │                Friends  │
│                      └─────────────────────┘                list     │
│                                                             panel    │
│                      ┌─────────────────────┐                         │
│                      │  Official Servers   │                         │
│                      └─────────────────────┘                         │
│                                                                      │
│                  ─────┐     ┌─────┐    ┌─────┐                       │
│                  │ ️  │      │ 🌐EN│    │ ❓ │                       │
│                  └─────┘    └─────┘     ─────┘                       │
│                                                                      │
│  ┌──────────────┐                                                    │
│  │ [Chat Panel] │                                                    │
│  │ Friends | P  │                                                    │
│  │ User1: Hey!  │                                                    │
│  │ User2: Ready?│                                                    │
│  │ [Type msg..] │                                                    │
│  ──────────────┘                                                    │
│                                                                      │
│                                                                      │
├──────────────────────────────────────────────────────────────────────┤
