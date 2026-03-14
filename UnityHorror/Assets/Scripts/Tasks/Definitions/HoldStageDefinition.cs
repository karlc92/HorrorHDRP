using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Horror/Tasks/Stages/Hold Stage")]
public class HoldStageDefinition : TaskStageDefinition
{
    [IdReference(typeof(TaskHook), nameof(TaskHook.HookId))]
    public string ActivationHookId;
    public InteractionMode ActivationInteractionMode = InteractionMode.Press;
    public float ActivationHoldDurationSeconds = 1f;
    public bool ResetActivationHoldOnCancel = true;
    public HoldConditionMode ConditionMode = HoldConditionMode.AllConditionsMet;
    public List<HoldConditionRequirementDefinition> Conditions = new List<HoldConditionRequirementDefinition>();
    public float RequiredSeconds = 30f;
    public bool AllowProgressDecay = false;
    public float DecayPerSecond = 0f;
}
