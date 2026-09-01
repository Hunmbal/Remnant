using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;

public static class PlayerAnimatorBuilder
{
    const string ControllerPath = "Assets/Animations/PlayerController.controller";
    const string ModelFolder = "Assets/Resources/Models/main";

    static int buildAttempts;

    [InitializeOnLoadMethod]
    static void AutoBuild()
    {
        buildAttempts = 0;
        EditorApplication.delayCall += TryBuildIfMissing;
    }

    [MenuItem("Tools/Rebuild Player Animation Controller")]
    public static void Rebuild()
    {
        buildAttempts = 0;
        TryBuildIfMissing();
    }

    static void TryBuildIfMissing()
    {
        if (BuildController())
            return;

        buildAttempts++;
        if (buildAttempts < 10)
            EditorApplication.delayCall += TryBuildIfMissing;
    }

    public static bool BuildController()
    {
        string resolvedPath = Path.GetFullPath(ControllerPath);
        if (File.Exists(resolvedPath)) return true;

        AnimationClips clips = LoadClips();
        if (clips.walk == null || clips.run == null || clips.sneak == null)
            return false;

        if (!Directory.Exists("Assets/Animations"))
        {
            Directory.CreateDirectory("Assets/Animations");
            AssetDatabase.Refresh();
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Sneaking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Airborne", AnimatorControllerParameterType.Bool);

        var stateMachine = controller.layers[0].stateMachine;

        var walk = stateMachine.AddState("Walk", new Vector3(240, 60, 0));
        walk.motion = clips.walk;

        var run = stateMachine.AddState("Run", new Vector3(480, 60, 0));
        run.motion = clips.run;

        var sneak = stateMachine.AddState("Sneaking", new Vector3(240, 180, 0));
        sneak.motion = clips.sneak;

        var air = stateMachine.AddState("Airborne", new Vector3(480, 180, 0));
        air.motion = clips.run;

        var toRun = walk.AddTransition(run);
        toRun.hasExitTime = false;
        toRun.duration = 0.15f;
        toRun.AddCondition(AnimatorConditionMode.Greater, 4.6f, "Speed");

        var toWalk = run.AddTransition(walk);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.15f;
        toWalk.AddCondition(AnimatorConditionMode.Less, 4.6f, "Speed");

        var toSneak = stateMachine.AddAnyStateTransition(sneak);
        toSneak.hasExitTime = false;
        toSneak.duration = 0.2f;
        toSneak.AddCondition(AnimatorConditionMode.If, 0f, "Sneaking");

        var toAir = stateMachine.AddAnyStateTransition(air);
        toAir.hasExitTime = false;
        toAir.duration = 0.2f;
        toAir.AddCondition(AnimatorConditionMode.If, 0f, "Airborne");

        var outSneakWalk = sneak.AddTransition(walk);
        outSneakWalk.hasExitTime = false;
        outSneakWalk.duration = 0.15f;
        outSneakWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, "Sneaking");
        outSneakWalk.AddCondition(AnimatorConditionMode.Less, 4.6f, "Speed");

        var outSneakRun = sneak.AddTransition(run);
        outSneakRun.hasExitTime = false;
        outSneakRun.duration = 0.15f;
        outSneakRun.AddCondition(AnimatorConditionMode.IfNot, 0f, "Sneaking");
        outSneakRun.AddCondition(AnimatorConditionMode.Greater, 4.6f, "Speed");

        var outAirWalk = air.AddTransition(walk);
        outAirWalk.hasExitTime = false;
        outAirWalk.duration = 0.15f;
        outAirWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, "Airborne");
        outAirWalk.AddCondition(AnimatorConditionMode.Less, 4.6f, "Speed");

        var outAirRun = air.AddTransition(run);
        outAirRun.hasExitTime = false;
        outAirRun.duration = 0.15f;
        outAirRun.AddCondition(AnimatorConditionMode.IfNot, 0f, "Airborne");
        outAirRun.AddCondition(AnimatorConditionMode.Greater, 4.6f, "Speed");

        AssetDatabase.SaveAssets();
        Debug.Log("PlayerAnimatorBuilder: created " + ControllerPath);
        return true;
    }

    struct AnimationClips
    {
        public AnimationClip walk;
        public AnimationClip run;
        public AnimationClip sneak;
    }

    static AnimationClips LoadClips()
    {
        List<AnimationClip> all = new List<AnimationClip>();
        foreach (string fbxPath in Directory.GetFiles(ModelFolder, "*.fbx"))
        {
            foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(fbxPath.Replace('\\', '/')))
            {
                AnimationClip clip = obj as AnimationClip;
                if (clip != null)
                    all.Add(clip);
            }
        }
        return new AnimationClips
        {
            sneak = FindClip(all, "Crouched", "Crouch"),
            walk = FindClip(all, "Walking"),
            run = FindClip(all, "Run")
        };
    }

    static AnimationClip FindClip(List<AnimationClip> clips, params string[] patterns)
    {
        foreach (AnimationClip clip in clips)
        {
            if (clip == null) continue;
            foreach (string pattern in patterns)
            {
                if (clip.name.IndexOf(pattern, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return clip;
            }
        }
        return null;
    }
}