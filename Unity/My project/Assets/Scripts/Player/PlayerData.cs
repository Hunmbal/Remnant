// Player team + persistent (in-session) player data. Maps save one spawn slot
// per team (including FFA) in their .rmap; the player spawns at the slot that
// matches their team, or the FFA slot when teamless or the team slot is absent.
public enum PlayerTeam
{
    Red,
    Blue,
    Green,
    Yellow
}

// Spawn slots stored in a map file. FFA is the no-team slot.
public enum SpawnTeam
{
    FFA = 0,
    Red = 1,
    Blue = 2,
    Green = 3,
    Yellow = 4
}

public static class PlayerData
{
    // The player's team (null = no team; falls back to the FFA spawn).
    public static PlayerTeam? Team;

    // Which spawn slot a team-less / teamed player should use.
    public static SpawnTeam SpawnSlotFor(PlayerTeam? team)
        => team.HasValue ? (SpawnTeam)((int)team.Value + 1) : SpawnTeam.FFA;
}