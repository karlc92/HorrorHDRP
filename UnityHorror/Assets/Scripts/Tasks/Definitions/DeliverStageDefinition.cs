using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Horror/Tasks/Stages/Deliver Stage")]
public class DeliverStageDefinition : TaskStageDefinition
{
    public HeldItemDefinition HeldItem;
    [IdReference(typeof(TaskHook), nameof(TaskHook.HookId))]
    public string PickupHookId;
    [IdReference(typeof(TaskHook), nameof(TaskHook.HookId))]
    public string DeliveryHookId;
    public string RequiredItemConditionId;
    public InteractionMode PickupInteractionMode = InteractionMode.Press;
    public float PickupHoldDurationSeconds = 1f;
    public bool ResetPickupHoldOnCancel = true;
    public InteractionMode DeliveryInteractionMode = InteractionMode.Press;
    public float DeliveryHoldDurationSeconds = 1f;
    public bool ResetDeliveryHoldOnCancel = true;
    public bool AllowSprint = false;
    public bool AllowCrouch = true;
    public bool AllowDrop = true;
    public bool ResetToOriginOnDrop = false;
    public bool ResetItemConditionsOnDrop = false;
    public GameObject PickupPrefabOverride;
    public List<HeldItemConditionMutationDefinition> OnPickupConditionMutations = new List<HeldItemConditionMutationDefinition>();
    public List<HeldItemConditionMutationDefinition> OnDropConditionMutations = new List<HeldItemConditionMutationDefinition>();
    public List<HeldItemConditionMutationDefinition> OnDeliverConditionMutations = new List<HeldItemConditionMutationDefinition>();
    public bool ApplyThreatOnDrop = false;
    public int ThreatOnDrop = 0;
    public bool ApplyThreatOnInvalidDelivery = false;
    public int ThreatOnInvalidDelivery = 0;
}
