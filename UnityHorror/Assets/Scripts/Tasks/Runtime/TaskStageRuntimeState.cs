using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TaskStageRuntimeState
{
    public string StageId;
    public int GroupIndex;
    public int StageIndex;
    public bool Completed;
    public bool Activated;
    public float Progress;
    public List<string> DiscoveredDetailKeys = new List<string>();
    public List<string> CompletedHookIds = new List<string>();
    public string RuntimeBindingId;
    public bool IsDeliverCarried;
    public bool HasDroppedDeliverPickup;
    public Vector3 DroppedDeliverPickupPosition;
    public List<HeldItemConditionState> DeliverItemConditionStates = new List<HeldItemConditionState>();
}
