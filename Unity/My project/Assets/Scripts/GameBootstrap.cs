using UnityEngine;

public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSetup()
    {
        // 1. Directional Light (without light everything is black)
        if (Object.FindObjectOfType<Light>() == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // 2. Arena Generator (generates the 10x10x10 arena)
        if (Object.FindObjectOfType<ArenaGenerator>() == null)
        {
            new GameObject("ArenaGenerator", typeof(ArenaGenerator));
        }

        // 3. Player (auto-adds CharacterController + Camera)
        if (Object.FindObjectOfType<Player>() == null)
        {
            new GameObject("Player", typeof(Player));
        }
    }
}