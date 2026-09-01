using UnityEngine;

public class BlockBreaker : MonoBehaviour
{
    // Reach is unified with the overlay highlight and block placing via Player.reach.
    [Tooltip("Hammer level. Breaks faster: breakTime / level. Level 0 = bare hands.")]
    public int hammerLevel = 0;
    [Tooltip("Practice mode instant-breaks regardless of tool/block.")]
    public bool practiceMode = false;

    float progress;
    Block currentTarget;

    Player player;

    void Awake()
    {
        player = GetComponent<Player>();
    }

    void Update()
    {
        if (GetComponent<InventoryUI>() != null && GetComponent<InventoryUI>().IsOpen) return;

        practiceMode = PlayerStateManager.IsPractice;

        // Read hammer level from the shared state toggle (if present)
        PlayerStateToggle toggle = GetComponent<PlayerStateToggle>();
        if (toggle != null)
            hammerLevel = toggle.hammerLevel;

        bool holding = Input.GetMouseButton(0);
        bool practiceClick = practiceMode && Input.GetMouseButtonDown(0);
        if (holding && !practiceMode || practiceMode && !practiceClick)
            progress = 0f;

        Block target = player != null ? player.TargetBlock : null;
        if (target == null)
        {
            currentTarget = null;
            return;
        }

        if (target != currentTarget)
        {
            currentTarget = target;
            progress = 0f;
        }

        if (practiceMode)
        {
            if (practiceClick)
            {
                target.BreakBlock();
                progress = 0f;
                currentTarget = null;
            }
            return;
        }

        if (!holding)
            return;

        if (target.breakable)
        {
            float multiplier = Mathf.Max(1, hammerLevel);
            float speed = multiplier / target.breakTime;
            progress += speed * Time.deltaTime;

            if (progress >= 1f)
            {
                target.BreakBlock();
                progress = 0f;
                currentTarget = null;
            }
        }
    }

    // Block FindTargetBlock()
    // {
    //     if (Player. == null) return null;

    //     Vector3 org = cam.transform.position;
    //     Vector3 dir = cam.transform.forward;

    //     float reach = player != null ? player.reach : 3f;
    //     RaycastHit[] hits = Physics.RaycastAll(org + dir * 0.01f, dir, reach);
    //     float bestDist = float.MaxValue;
    //     Block best = null;

    //     foreach (RaycastHit hit in hits)
    //     {
    //         if (hit.collider == null) continue;
    //         if (hit.collider.transform == transform ||
    //             hit.collider.transform.IsChildOf(transform))
    //             continue;

    //         if (hit.distance < bestDist)
    //         {
    //             Block b = hit.collider.GetComponent<Block>();
    //             if (b != null)
    //             {
    //                 bestDist = hit.distance;
    //                 best = b;
    //             }
    //         }
    //     }

    //     return best;
    // }
}
