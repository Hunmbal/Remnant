using UnityEngine;

public class PlayerStateToggle : MonoBehaviour
{
    [Tooltip("Hammer level tied to the player (shared with the block breaker).")]
    public int hammerLevel = 0;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            PlayerStateManager.State = PlayerStateManager.IsPractice
                ? PlayerState.Default
                : PlayerState.Practice;

            Debug.Log("[GameState] " + PlayerStateManager.State);
        }
    }
}
