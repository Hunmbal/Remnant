using UnityEngine;

public enum PlayerState
{
    Default,
    Practice
}

public static class PlayerStateManager
{
    public static PlayerState State = PlayerState.Practice;

    public static bool IsPractice => State == PlayerState.Practice;
}
