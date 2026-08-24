# ⚙️ Technical Design Document (TDD)

> **Module:** Technical Architecture & Engineering  
> **Engine:** Unity (Latest LTS)  
> **Primary Language:** C# (Standard MonoBehaviours + DOTS/ECS for heavy server logic)  
> **Networking Library:** Valve's **GameNetworkingSockets (GNS)**  
> **Tick Rate:** 30 TPS (Fixed Timestep)  

---

## 1. Server Architecture & Capacities

The game utilizes a hybrid networking model, scaling from peer-to-peer player lobbies to massive official dedicated servers.

### 1.1 Player-Hosted Lobbies/Servers
* **Free Tier:** Max **8 players**. Ideal for small friend groups. Hosted via P2P Listen Server.
* **Paid Tier:** Max **24 players**. Requires a decent gaming PC and stable upload speed. Hosted via P2P Listen Server.
* **Visibility Toggle:** When creating a lobby, the host can choose its visibility:
  * **Private:** Invite-only. Does not appear in the server browser.
  * **Public:** Listed in the global server browser for anyone to join. *(Note: Making a lobby public does NOT increase the player cap; it remains strictly limited to 8 or 24 players).*
* **Topology:** Listen Server (P2P).
* **NAT Traversal:** Handled automatically by GNS (UDP Hole Punching / Relay).

### 1.2 Official Servers (Cloud Dedicated)
* **Max Capacity:** **200 players** per single server instance.
* **World Partitioning:** The 200 players are distributed across multiple **Arenas/Worlds** within the same server instance (e.g., 4 arenas of 50 players, or 2 arenas of 100).
* **Topology:** Headless Unity Dedicated Server hosted on cloud infrastructure.
* **Performance Mandate:** Requires **Unity DOTS (ECS + Burst Compiler)** for entity management and spatial hashing to maintain 30 TPS with 200 concurrent connections.

### 1.3 Single Player (Localhost)
* **Topology:** Localhost (Client and Server run on the same machine).
* **World Type:** Open Flat Creative (Superflat generation preset). Infinite or large bounded plane optimized for building.
* **Performance:** Bypasses all network serialization and GNS overhead. The 30 TPS tick rate executes instantly via local memory pointers, resulting in zero input lag and maximum frame rates.
* **Persistence:** World data saves locally to the user's file system (e.g., local JSON or SQLite database). The Central Master Server is not pinged for world state or chunk updates.
* **Anti-Cheat:** Completely disabled.

---

## 2. Social & Matchmaking Backend

To support global friends and cross-server parties, the game requires a lightweight **Central Master Server** (e.g., custom Node.js/Go backend, PlayFab, or Firebase) that runs independently of the Unity game servers.

### 2.1 Global Friends System
* **Architecture:** Managed entirely by the Central Master Server via WebSockets/REST API.
* **Features:**
  * Real-time online/offline status.
  * **Server Visibility:** Shows exactly which Public/Private server a friend is currently connected to.
  * **Direct Messaging:** Global chat independent of game server location.
* **State Tracking:** The Master Server tracks player states: `Offline`, `InLobby` (in a server but not in a match), and `InGame` (actively playing a match).

### 2.2 Party System Logic
Parties are strictly tied to the **Party Creator's** current server instance.

* **Creation:** A player can only create a party if they are currently inside a server (Public or Private). The Master Server generates a `PartyID` linked to the `ServerID`.
* **Invitation:** The creator sends an invite via the Global Friends UI.
* **Acceptance Rules (Strict):**
  * If Friend's State == `InGame`: **Invite cant be accepted** (Cannot leave an active match).
  * If Friend's State == `InLobby/AFK` (Different Server): **Acceptable**. The client is forcefully disconnected from their current server and reconnects to the Creator's `ServerID`.
* **Disbanding:** If the Party Creator leaves the server (disconnects or crashes), the Master Server automatically destroys the `PartyID` and disbands the party for all members.

---

## 3. Networking & Data Routing

### 3.1 GameNetworkingSockets (GNS) Implementation
* **Private Lobbies:** Uses GNS P2P. The host's client acts as the server. GNS handles NAT traversal seamlessly.
* **Public Servers:** Uses GNS Dedicated. The server holds a public IP. 
* **Channels:**
  * **Unreliable:** Player movement, camera rotation, particle effects.
  * **Reliable:** Block placement/breaking, combat hits, inventory, chat, party state.

### 3.2 Interest Management (Crucial for 200 Players)
Sending 200 player states to 200 clients is impossible. We use **Spatial Interest Management (Area of Interest)**.
* **Grid System:** The server divides the world into a spatial hash grid.
* **Arena Isolation:** Players in Arena A receive **zero** network updates for players in Arena B.
* **Proximity Culling:** Within an arena, a player only receives updates for entities within their render/combat radius (e.g., 50 meters). 

---

## 4. Unity Performance & Optimization

### 4.1 The DOTS Mandate (For 200-Player Public Servers)
Standard Unity `MonoBehaviours` will bottleneck the CPU at 200 players.
* **Movement & Physics:** Must be written in **Unity ECS** using the **Burst Compiler**.
* **Voxel Meshing:** Chunk mesh generation must be offloaded to the **Unity Job System** to run asynchronously on background threads.

### 4.2 Memory & Object Pooling
* **Rule:** Zero `Instantiate()` or `Destroy()` calls during gameplay for projectiles, particles, or dropped items.
* **Solution:** Pre-allocate massive object pools (e.g., 10,000 arrows, 50,000 block update packets) to prevent Garbage Collection (GC) spikes, which would drop the server below 30 TPS.

---

## 5. Server Transition & Teleportation Flow

When a player accepts a party invite from a different server, the following technical flow occurs:

1. **Invite Accept:** Client sends `AcceptInvite(PartyID)` to Master Server.
2. **Validation:** Master Server checks target player state. If `InGame`, reject. If valid, proceed.
3. **Token Generation:** Master Server generates a secure `ConnectionToken` for the Creator's `ServerID` and sends it to the accepting client.
4. **Disconnect:** Accepting client gracefully disconnects from their current Unity server.
5. **Reconnect:** Accepting client uses GNS to connect to the Creator's `ServerID` using the `ConnectionToken`.
6. **Spawn:** Creator's server receives the connection, verifies the token with the Master Server, and spawns the player in the server's central Lobby area, automatically assigning them to the Party.

---

## 6. Security & Anti-Cheat (Basic)

* **Server Authority:** The Unity server (Host or Dedicated) is the single source of truth. Clients never dictate health, inventory, or block placements.
* **Validation:** 
  * Server validates block placement range.
  * Server validates movement speed and Flash-Step stamina costs.
  * Server validates Party actions (e.g., ensuring a client cannot force-add someone to a party without the creator's permission via the Master Server).

---

## 7. Implementation Roadmap

### Phase 1: Core Engine & Local Voxel
* Implement chunk generation, greedy meshing, and block placing/breaking in Unity.
* Establish the 30 TPS fixed timestep game loop.

### Phase 2: Master Server & Social
* Build the lightweight Central Master Server (Node.js/Go).
* Implement Global Friends list, status tracking, and Direct Chat.

### Phase 3: Private Networking (P2P)
* Integrate GameNetworkingSockets.
* Build the 8-player / 32-player Listen Server architecture.
* Implement the Party Creation/Disband logic tied to the host.

### Phase 4: Public Server Scale-Up
* Migrate heavy logic to Unity DOTS/ECS.
* Build the Headless Dedicated Server.
* Implement Spatial Interest Management (Arena partitioning) for 200 players.
* Implement the Server Hopping/Teleportation flow for party invites.

### Phase 5: Combat & Polish
* Implement Flash-Step, sword blocking, and health/stamina systems.
* Add client-side prediction, server reconciliation, and lag compensation.