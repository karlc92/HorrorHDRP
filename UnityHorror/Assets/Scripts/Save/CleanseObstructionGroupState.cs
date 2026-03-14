using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CleanseObstructionGroupState
{
    public string GroupId;
    public string TaskDefinitionId;
    public string StageId;
    public bool PersistAcrossNights = false;
    public bool Triggered = false;
    public bool Settled = false;
    public bool Resolved = false;
    public Vector3 SpawnOrigin = Vector3.zero;
    public float ElapsedSeconds = 0f;
    public float SpawnTimerSeconds = 0f;
    public int SpawnedCount = 0;
    public int NextInstanceIndex = 0;
    public List<CleanseObstructionInstanceState> Instances = new List<CleanseObstructionInstanceState>();
}
