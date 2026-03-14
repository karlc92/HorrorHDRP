using System;
using UnityEngine;

[Serializable]
public class GameState
{
    public ProgressionState Progression = new ProgressionState();
    public RunState Run = null;

    // Compatibility shims for existing runtime systems while the project transitions
    // to the nested Progression/Run save model.
    public Vector3 PlayerPos
    {
        get => Run != null ? Run.PlayerPos : Vector3.zero;
        set => EnsureRunState().PlayerPos = value;
    }

    public Quaternion PlayerRot
    {
        get => Run != null ? Run.PlayerRot : Quaternion.identity;
        set => EnsureRunState().PlayerRot = value;
    }

    public int Night
    {
        get => Run != null ? Run.CurrentNightNumber : 1;
        set => EnsureRunState().CurrentNightNumber = value;
    }

    public int Slot
    {
        get => 1;
        set { }
    }

    public float TotalPlayTimeSeconds
    {
        get => Run != null ? Run.TotalPlayTimeSeconds : 0f;
        set => EnsureRunState().TotalPlayTimeSeconds = value;
    }

    public MonsterBrainState MonsterBrainState
    {
        get => Run != null ? Run.MonsterBrainState : null;
        set => EnsureRunState().MonsterBrainState = value;
    }

    public RunState EnsureRunState()
    {
        if (Run == null)
            Run = new RunState();

        if (Run.MonsterBrainState == null)
            Run.MonsterBrainState = new MonsterBrainState();

        if (Run.CurrentNightState == null)
            Run.CurrentNightState = new NightRuntimeState();

        if (Run.Plan == null)
            Run.Plan = new RunPlan();

        return Run;
    }
}
