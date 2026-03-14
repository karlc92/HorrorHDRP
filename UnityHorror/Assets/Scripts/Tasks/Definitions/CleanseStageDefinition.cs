using UnityEngine;

[CreateAssetMenu(menuName = "Horror/Tasks/Stages/Cleanse Stage")]
public class CleanseStageDefinition : TaskStageDefinition
{
    [Header("Hooks")]
    [IdReference(typeof(TaskHook), nameof(TaskHook.HookId))]
    public string TriggerHookId;
    [IdReference(typeof(TaskHook), nameof(TaskHook.HookId))]
    public string SpawnOriginHookId;

    [Header("Assets")]
    public string ObstructionResourcePath;

    [Header("Trigger Interaction")]
    public InteractionMode TriggerInteractionMode = InteractionMode.Press;
    public float TriggerHoldDurationSeconds = 1f;
    public bool ResetTriggerHoldOnCancel = true;

    [Header("Obstruction Interaction")]
    public InteractionMode ObstructionInteractionMode = InteractionMode.Press;
    public float ObstructionHoldDurationSeconds = 1f;
    public bool ResetObstructionHoldOnCancel = true;

    [Header("Spread")]
    public float SpreadDurationSeconds = 20f;
    public float SpawnIntervalSeconds = 0.75f;
    public int MaxActiveObstructions = 5;
    public int TotalSpawnCount = 8;
    public float SpawnRadius = 5f;
    public int PlacementAttemptsPerSpawn = 24;

    [Header("Persistence")]
    public bool PersistAcrossNights = false;
}
