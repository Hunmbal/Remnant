using UnityEngine;

public class PlayerStateToggle : MonoBehaviour
{
    [Tooltip("Hammer level tied to the player (shared with the block breaker).")]
    public int hammerLevel = 0;

    void Update()
    {
        // While typing in chat, all game inputs are locked.
        if (Chat.IsLockingInput) return;

        if (Input.GetKeyDown(KeyCode.P))
        {
            PlayerStateManager.State = PlayerStateManager.IsPractice
                ? PlayerState.Default
                : PlayerState.Practice;

            Debug.Log("[GameState] " + PlayerStateManager.State);
        }
    }
}
