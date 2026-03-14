using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RunState
{
    public int Seed = 0;
    public int CurrentNightNumber = 1;
    public RunPlan Plan = new RunPlan();
    public NightRuntimeState CurrentNightState = new NightRuntimeState();
    public NightSnapshot NightStartSnapshot = new NightSnapshot();
    public InventoryState Inventory = new InventoryState();
    public bool NightStarted = false;
    public List<CleanseObstructionGroupState> CleanseObstructionGroups = new List<CleanseObstructionGroupState>();

    // Live runtime state tracked by existing systems.
    public Vector3 PlayerPos = Vector3.zero;
    public Quaternion PlayerRot = Quaternion.identity;
    public float TotalPlayTimeSeconds = 0f;
    public MonsterBrainState MonsterBrainState = new MonsterBrainState();
}
