using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TaskManager : MonoBehaviour, IGameSaveParticipant
{
    public static TaskManager Instance { get; private set; }

    private readonly Dictionary<string, TaskDefinition> definitionsById = new Dictionary<string, TaskDefinition>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TaskBase> tasksByInstanceId = new Dictionary<string, TaskBase>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TaskHook> hooksById = new Dictionary<string, TaskHook>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HoldConditionSource> holdConditionSourcesById = new Dictionary<string, HoldConditionSource>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CleanseObstructionSpreader> cleanseSpreadersByGroupId = new Dictionary<string, CleanseObstructionSpreader>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, GameObject> runtimeDeliverPickupsByStageId = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        BuildDefinitionCache(LoadDefinitions());
        RegisterExistingHooks();
        RegisterExistingHoldConditionSources();
        RebuildTasksFromRunState();
        RestoreDroppedDeliverPickups();
        RestoreCleanseSpreaders();
        ApplyHookInteractionConfiguration();
    }

    private void Update()
    {
        foreach (var spreader in cleanseSpreadersByGroupId.Values.ToList())
        {
            if (spreader == null)
                continue;

            spreader.Tick(Time.deltaTime);
        }

        foreach (var task in tasksByInstanceId.Values)
            task?.Tick(Time.deltaTime);

        if (Game.State?.Run?.CurrentNightState != null)
            Game.State.Run.CurrentNightState.CanEndNight = AreAllRequiredTasksComplete();
    }

    public static List<TaskDefinition> LoadDefinitions()
    {
        return Resources.LoadAll<TaskDefinition>("Tasks").Where(t => t != null).ToList();
    }

    public static NightRuntimeState CreateNightRuntimeStateForPlan(NightPlan plan)
    {
        var state = new NightRuntimeState();
        if (plan == null)
            return state;

        var definitionsById = LoadDefinitions()
            .Where(d => d != null && !string.IsNullOrWhiteSpace(d.TaskId))
            .ToDictionary(d => d.TaskId, d => d, StringComparer.OrdinalIgnoreCase);

        foreach (var task in plan.Tasks)
        {
            definitionsById.TryGetValue(task.TaskDefinitionId, out var definition);
            state.Tasks.Add(new TaskRuntimeState
            {
                TaskInstanceId = task.TaskInstanceId,
                TaskDefinitionId = task.TaskDefinitionId,
                CurrentGroupIndex = 0,
                Completed = false,
                RequiredForNightCompletion = definition == null || definition.RequiredForNightCompletion,
                Stages = BuildStageStates(definition),
            });
        }

        return state;
    }

    public bool AreAllRequiredTasksComplete()
    {
        var taskStates = Game.State?.Run?.CurrentNightState?.Tasks;
        if (taskStates == null || taskStates.Count == 0)
            return false;

        foreach (var task in taskStates)
        {
            if (task.RequiredForNightCompletion && !task.Completed)
                return false;
        }

        return true;
    }

    public void DebugCompleteAllCurrentNightTasks()
    {
        var taskStates = Game.State?.Run?.CurrentNightState?.Tasks;
        if (taskStates == null)
            return;

        foreach (var task in taskStates)
        {
            if (task == null)
                continue;

            task.Completed = true;
            foreach (var stage in task.Stages)
            {
                if (stage == null)
                    continue;

                stage.Completed = true;
            }
        }

        if (Game.State?.Run?.CurrentNightState != null)
            Game.State.Run.CurrentNightState.CanEndNight = AreAllRequiredTasksComplete();
    }

    public IEnumerable<TaskRuntimeState> GetCurrentNightTasks()
    {
        return Game.State?.Run?.CurrentNightState?.Tasks != null
            ? (IEnumerable<TaskRuntimeState>)Game.State.Run.CurrentNightState.Tasks
            : Array.Empty<TaskRuntimeState>();
    }

    public IReadOnlyList<TaskListEntryViewData> GetCurrentNightTaskEntries()
    {
        var entries = new List<TaskListEntryViewData>();
        foreach (var taskState in GetCurrentNightTasks())
        {
            if (taskState == null)
                continue;

            if (!definitionsById.TryGetValue(taskState.TaskDefinitionId, out var definition))
                continue;

            if (!tasksByInstanceId.TryGetValue(taskState.TaskInstanceId, out var runtimeTask))
                continue;

            entries.Add(new TaskListEntryViewData
            {
                TaskInstanceId = taskState.TaskInstanceId,
                TitleKey = definition.GetTitleKey(),
                DetailKeys = runtimeTask.GetCurrentDetailKeys().ToList(),
                Details = runtimeTask.GetCurrentDetails().ToList(),
                Completed = taskState.Completed,
            });
        }

        return entries;
    }

    public bool TryDropActiveDelivery(PlayerController player)
    {
        var inventory = InventoryManager.Instance;
        if (inventory == null || !inventory.HasForcedDeliveryItem || string.IsNullOrWhiteSpace(inventory.ForcedDeliveryTaskInstanceId))
            return false;

        if (!tasksByInstanceId.TryGetValue(inventory.ForcedDeliveryTaskInstanceId, out var task) || task == null)
            return false;

        return task.TryHandleForcedDrop(player);
    }

    public bool CanSprintWithForcedDelivery()
    {
        return GetActiveDeliverStage()?.AllowSprint ?? true;
    }

    public bool CanCrouchWithForcedDelivery()
    {
        return GetActiveDeliverStage()?.AllowCrouch ?? true;
    }

    public void RegisterHook(TaskHook hook)
    {
        if (hook == null || string.IsNullOrWhiteSpace(hook.HookId))
            return;

        hooksById[hook.HookId] = hook;
        RebindTasks();
    }

    public void RegisterHoldConditionSource(HoldConditionSource source)
    {
        if (source == null || string.IsNullOrWhiteSpace(source.SourceId))
            return;

        holdConditionSourcesById[source.SourceId] = source;
    }

    public void UnregisterHoldConditionSource(HoldConditionSource source)
    {
        if (source == null || string.IsNullOrWhiteSpace(source.SourceId))
            return;

        if (holdConditionSourcesById.TryGetValue(source.SourceId, out var existing) && existing == source)
            holdConditionSourcesById.Remove(source.SourceId);
    }

    public void UnregisterHook(TaskHook hook)
    {
        if (hook == null || string.IsNullOrWhiteSpace(hook.HookId))
            return;

        if (hooksById.TryGetValue(hook.HookId, out var existing) && existing == hook)
            hooksById.Remove(hook.HookId);

        RebindTasks();
    }

    public void ReportHookEvent(TaskHook hook, string eventName)
    {
        if (hook == null || string.IsNullOrWhiteSpace(eventName))
            return;

        foreach (var task in tasksByInstanceId.Values)
        {
            task?.HandleHookEvent(hook, eventName);
        }

        if (Game.State?.Run?.CurrentNightState != null)
            Game.State.Run.CurrentNightState.CanEndNight = AreAllRequiredTasksComplete();
    }

    public bool TryStartCleanseStage(
        TaskDefinition taskDefinition,
        TaskRuntimeState taskState,
        CleanseStageDefinition stageDefinition,
        TaskStageRuntimeState stageState)
    {
        if (taskDefinition == null || taskState == null || stageDefinition == null || stageState == null)
            return false;

        if (stageState.Activated && !string.IsNullOrWhiteSpace(stageState.RuntimeBindingId))
            return false;

        var originHook = GetHook<CleanseTriggerHook>(stageDefinition.SpawnOriginHookId)
            ?? GetHook<TaskHook>(stageDefinition.SpawnOriginHookId);

        if (originHook == null)
            return false;

        var run = Game.State?.Run;
        if (run == null)
            return false;

        run.CleanseObstructionGroups ??= new List<CleanseObstructionGroupState>();

        var groupState = new CleanseObstructionGroupState
        {
            GroupId = BuildCleanseGroupId(taskState, stageState),
            TaskDefinitionId = taskDefinition.TaskId,
            StageId = stageState.StageId,
            PersistAcrossNights = stageDefinition.PersistAcrossNights,
            Triggered = true,
            SpawnOrigin = originHook.transform.position,
        };

        run.CleanseObstructionGroups.RemoveAll(g => g != null && string.Equals(g.GroupId, groupState.GroupId, StringComparison.OrdinalIgnoreCase));
        run.CleanseObstructionGroups.Add(groupState);

        stageState.Activated = true;
        stageState.RuntimeBindingId = groupState.GroupId;

        CreateOrRestoreCleanseSpreader(stageDefinition, groupState);
        return true;
    }

    public void NotifyCleanseGroupResolved(string groupId, bool settledBadly)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            return;

        foreach (var task in GetCurrentNightTasks())
        {
            if (task?.Stages == null)
                continue;

            foreach (var stage in task.Stages)
            {
                if (stage == null || !string.Equals(stage.RuntimeBindingId, groupId, StringComparison.OrdinalIgnoreCase))
                    continue;

                stage.Completed = true;
                stage.Progress = 1f;
            }
        }

        RebuildTasksFromRunState();

        if (!settledBadly)
            RemoveCleanseGroup(groupId);

        if (Game.State?.Run?.CurrentNightState != null)
            Game.State.Run.CurrentNightState.CanEndNight = AreAllRequiredTasksComplete();
    }

    public void OnNightEnded()
    {
        var run = Game.State?.Run;
        if (run?.CleanseObstructionGroups == null)
            return;

        var toRemove = run.CleanseObstructionGroups
            .Where(g => g != null && !g.PersistAcrossNights)
            .Select(g => g.GroupId)
            .ToList();

        foreach (var groupId in toRemove)
            RemoveCleanseGroup(groupId);

        ClearRuntimeDeliverPickups();
    }

    public void OnBeforeGameSaved(GameState state)
    {
    }

    public void OnAfterGameLoaded(GameState state)
    {
        RebuildTasksFromRunState();
        RestoreDroppedDeliverPickups();
        RestoreCleanseSpreaders();
        ApplyHookInteractionConfiguration();
    }

    private void BuildDefinitionCache(IEnumerable<TaskDefinition> definitions)
    {
        definitionsById.Clear();
        foreach (var definition in definitions)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.TaskId))
                continue;

            definitionsById[definition.TaskId] = definition;
        }
    }

    private void RebuildTasksFromRunState()
    {
        tasksByInstanceId.Clear();
        var taskStates = Game.State?.Run?.CurrentNightState?.Tasks;
        if (taskStates == null)
            return;

        foreach (var taskState in taskStates)
        {
            if (taskState == null || string.IsNullOrWhiteSpace(taskState.TaskDefinitionId))
                continue;

            if (!definitionsById.TryGetValue(taskState.TaskDefinitionId, out var definition))
                continue;

            var runtimeTask = CreateRuntimeTask(definition);
            runtimeTask.Initialize(definition, taskState);
            runtimeTask.BindHooks(hooksById.Values);
            tasksByInstanceId[taskState.TaskInstanceId] = runtimeTask;
        }
    }

    private void RegisterExistingHooks()
    {
        hooksById.Clear();
        foreach (var hook in FindObjectsByType<TaskHook>(FindObjectsSortMode.None))
        {
            if (hook == null || string.IsNullOrWhiteSpace(hook.HookId))
                continue;

            hooksById[hook.HookId] = hook;
        }
    }

    private void RegisterExistingHoldConditionSources()
    {
        holdConditionSourcesById.Clear();
        foreach (var source in FindObjectsByType<HoldConditionSource>(FindObjectsSortMode.None))
        {
            if (source == null || string.IsNullOrWhiteSpace(source.SourceId))
                continue;

            holdConditionSourcesById[source.SourceId] = source;
        }
    }

    private void RebindTasks()
    {
        foreach (var task in tasksByInstanceId.Values)
            task?.BindHooks(hooksById.Values);

        ApplyHookInteractionConfiguration();
    }

    private static TaskBase CreateRuntimeTask(TaskDefinition definition)
    {
        return new ComposedTask();
    }

    private static List<TaskStageRuntimeState> BuildStageStates(TaskDefinition definition)
    {
        var result = new List<TaskStageRuntimeState>();
        if (definition == null || definition.StageGroups == null)
            return result;

        for (int groupIndex = 0; groupIndex < definition.StageGroups.Count; groupIndex++)
        {
            var group = definition.StageGroups[groupIndex];
            if (group == null || group.Stages == null)
                continue;

            for (int stageIndex = 0; stageIndex < group.Stages.Count; stageIndex++)
            {
                var stage = group.Stages[stageIndex];
                if (stage == null)
                    continue;

                result.Add(new TaskStageRuntimeState
                {
                    StageId = string.IsNullOrWhiteSpace(stage.StageId)
                        ? $"group_{groupIndex}_stage_{stageIndex}"
                        : stage.StageId,
                    GroupIndex = groupIndex,
                    StageIndex = stageIndex,
                    Completed = false,
                    Progress = 0f,
                });
            }
        }

        return result;
    }

    private void ApplyHookInteractionConfiguration()
    {
        foreach (var taskState in GetCurrentNightTasks())
        {
            if (taskState == null || !definitionsById.TryGetValue(taskState.TaskDefinitionId, out var definition))
                continue;

            foreach (var stageState in taskState.Stages)
            {
                if (stageState == null)
                    continue;

                var stageDefinition = GetStageDefinition(definition, stageState);
                if (stageDefinition is DeliverStageDefinition deliverStage)
                {
                    ApplyDeliverHookInteractionConfiguration(deliverStage);
                    continue;
                }

                if (stageDefinition is HoldStageDefinition holdStage)
                {
                    ApplyHoldHookInteractionConfiguration(holdStage);
                    continue;
                }

                if (stageDefinition is not CleanseStageDefinition cleanseStage)
                    continue;

                var triggerHook = GetHook<CleanseTriggerHook>(cleanseStage.TriggerHookId);
                if (triggerHook == null)
                    continue;

                var interactable = triggerHook.GetComponent<CleanseTriggerInteractable>();
                if (interactable != null)
                {
                    interactable.ConfigureInteraction(
                        cleanseStage.TriggerInteractionMode,
                        cleanseStage.TriggerHoldDurationSeconds,
                        cleanseStage.ResetTriggerHoldOnCancel);
                }
            }
        }
    }

    private void RestoreDroppedDeliverPickups()
    {
        ClearRuntimeDeliverPickups();

        foreach (var taskState in GetCurrentNightTasks())
        {
            if (taskState == null || !definitionsById.TryGetValue(taskState.TaskDefinitionId, out var definition) || taskState.Stages == null)
                continue;

            foreach (var stageState in taskState.Stages)
            {
                if (stageState == null || !stageState.HasDroppedDeliverPickup)
                    continue;

                var stageDefinition = GetStageDefinition(definition, stageState) as DeliverStageDefinition;
                if (stageDefinition == null)
                    continue;

                SetOriginalDeliverPickupActive(stageDefinition.PickupHookId, false);
                SpawnDroppedDeliverPickup(stageDefinition, stageState, stageState.DroppedDeliverPickupPosition);
            }
        }
    }

    private void ApplyHoldHookInteractionConfiguration(HoldStageDefinition holdStage)
    {
        var activationHook = GetHook<InteractionTaskHook>(holdStage.ActivationHookId);
        if (activationHook == null)
            return;

        var interactable = activationHook.GetComponent<InteractionHookInteractable>();
        interactable?.ConfigureInteraction(
            holdStage.ActivationInteractionMode,
            holdStage.ActivationHoldDurationSeconds,
            holdStage.ResetActivationHoldOnCancel);
    }

    private void ApplyDeliverHookInteractionConfiguration(DeliverStageDefinition deliverStage)
    {
        var pickupHook = GetHook<DeliverPickupHook>(deliverStage.PickupHookId);
        if (pickupHook != null)
        {
            var pickupInteractable = pickupHook.GetComponent<DeliverPickupInteractable>();
            pickupInteractable?.ConfigureInteraction(
                deliverStage.PickupInteractionMode,
                deliverStage.PickupHoldDurationSeconds,
                deliverStage.ResetPickupHoldOnCancel);
        }

        var deliveryHook = GetHook<DeliverDepositHook>(deliverStage.DeliveryHookId);
        if (deliveryHook != null)
        {
            var deliveryInteractable = deliveryHook.GetComponent<DeliverDepositInteractable>();
            deliveryInteractable?.ConfigureInteraction(
                deliverStage.DeliveryInteractionMode,
                deliverStage.DeliveryHoldDurationSeconds,
                deliverStage.ResetDeliveryHoldOnCancel);
        }
    }

    private void RestoreCleanseSpreaders()
    {
        DestroyAllCleanseSpreaders();

        var groups = Game.State?.Run?.CleanseObstructionGroups;
        if (groups == null)
            return;

        foreach (var groupState in groups)
        {
            if (groupState == null || string.IsNullOrWhiteSpace(groupState.TaskDefinitionId) || string.IsNullOrWhiteSpace(groupState.StageId))
                continue;

            var stageDefinition = GetStageDefinition(groupState.TaskDefinitionId, groupState.StageId) as CleanseStageDefinition;
            if (stageDefinition == null)
                continue;

            CreateOrRestoreCleanseSpreader(stageDefinition, groupState);
        }
    }

    private void DestroyAllCleanseSpreaders()
    {
        foreach (var spreader in cleanseSpreadersByGroupId.Values.ToList())
            spreader?.DestroyRuntime();

        cleanseSpreadersByGroupId.Clear();
    }

    private void CreateOrRestoreCleanseSpreader(CleanseStageDefinition definition, CleanseObstructionGroupState state)
    {
        if (definition == null || state == null || string.IsNullOrWhiteSpace(state.GroupId))
            return;

        if (cleanseSpreadersByGroupId.TryGetValue(state.GroupId, out var existing) && existing != null)
            existing.DestroyRuntime();

        var runtimeObject = new GameObject($"CleanseSpreader_{state.GroupId}");
        var spreader = runtimeObject.AddComponent<CleanseObstructionSpreader>();
        spreader.Initialize(definition, state);
        cleanseSpreadersByGroupId[state.GroupId] = spreader;
    }

    private void RemoveCleanseGroup(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            return;

        if (cleanseSpreadersByGroupId.TryGetValue(groupId, out var spreader) && spreader != null)
            spreader.DestroyRuntime();

        cleanseSpreadersByGroupId.Remove(groupId);
        Game.State?.Run?.CleanseObstructionGroups?.RemoveAll(g => g != null && string.Equals(g.GroupId, groupId, StringComparison.OrdinalIgnoreCase));
    }

    public bool SpawnDroppedDeliverPickup(DeliverStageDefinition stageDefinition, TaskStageRuntimeState stageState, Vector3 desiredPosition)
    {
        if (stageDefinition == null || stageState == null)
            return false;

        var sourceHook = GetHook<DeliverPickupHook>(stageDefinition.PickupHookId);
        if (sourceHook == null)
            return false;

        var prefab = stageDefinition.PickupPrefabOverride != null
            ? stageDefinition.PickupPrefabOverride
            : sourceHook.gameObject;

        if (prefab == null)
            return false;

        if (!TryFindDeliverDropPosition(desiredPosition, out var spawnPosition))
            spawnPosition = desiredPosition;

        RemoveRuntimeDeliverPickup(stageState.StageId);

        var instance = Instantiate(prefab, spawnPosition, sourceHook.transform.rotation);
        instance.name = $"{prefab.name}_Dropped_{stageState.StageId}";

        var droppedHook = instance.GetComponentInChildren<DeliverPickupHook>();
        if (droppedHook != null)
            droppedHook.HookId = sourceHook.HookId;

        var droppedInteractable = instance.GetComponentInChildren<DeliverPickupInteractable>();
        if (droppedInteractable != null)
        {
            droppedInteractable.ConfigureInteraction(
                stageDefinition.PickupInteractionMode,
                stageDefinition.PickupHoldDurationSeconds,
                stageDefinition.ResetPickupHoldOnCancel);
        }

        runtimeDeliverPickupsByStageId[stageState.StageId] = instance;
        return true;
    }

    public void RemoveRuntimeDeliverPickup(string stageId)
    {
        if (string.IsNullOrWhiteSpace(stageId))
            return;

        if (!runtimeDeliverPickupsByStageId.TryGetValue(stageId, out var runtimePickup))
            return;

        runtimeDeliverPickupsByStageId.Remove(stageId);
        if (runtimePickup != null)
            Destroy(runtimePickup);
    }

    private void ClearRuntimeDeliverPickups()
    {
        foreach (var runtimePickup in runtimeDeliverPickupsByStageId.Values)
        {
            if (runtimePickup != null)
                Destroy(runtimePickup);
        }

        runtimeDeliverPickupsByStageId.Clear();
    }

    private static bool TryFindDeliverDropPosition(Vector3 desiredPosition, out Vector3 validPosition)
    {
        validPosition = desiredPosition;
        bool hitGround = false;

        if (Physics.Raycast(desiredPosition + Vector3.up * 2f, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore))
        {
            validPosition = hit.point;
            hitGround = true;
        }

        if (UnityEngine.AI.NavMesh.SamplePosition(validPosition, out var navHit, 1.5f, UnityEngine.AI.NavMesh.AllAreas))
        {
            validPosition = navHit.position;
            return true;
        }

        return hitGround;
    }

    private T GetHook<T>(string hookId) where T : TaskHook
    {
        if (string.IsNullOrWhiteSpace(hookId))
            return null;

        if (!hooksById.TryGetValue(hookId, out var hook))
            return null;

        return hook as T;
    }

    public T GetHoldConditionSource<T>(string sourceId) where T : HoldConditionSource
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return null;

        if (!holdConditionSourcesById.TryGetValue(sourceId, out var source))
            return null;

        return source as T;
    }

    public HoldConditionSource GetHoldConditionSource(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return null;

        holdConditionSourcesById.TryGetValue(sourceId, out var source);
        return source;
    }

    private static string BuildCleanseGroupId(TaskRuntimeState taskState, TaskStageRuntimeState stageState)
    {
        return $"{taskState.TaskInstanceId}.{stageState.StageId}.cleanse";
    }

    private TaskStageDefinition GetStageDefinition(string taskDefinitionId, string stageId)
    {
        if (string.IsNullOrWhiteSpace(taskDefinitionId) || string.IsNullOrWhiteSpace(stageId))
            return null;

        if (!definitionsById.TryGetValue(taskDefinitionId, out var definition))
            return null;

        foreach (var group in definition.StageGroups)
        {
            if (group?.Stages == null)
                continue;

            foreach (var stage in group.Stages)
            {
                if (stage != null && string.Equals(stage.StageId, stageId, StringComparison.OrdinalIgnoreCase))
                    return stage;
            }
        }

        return null;
    }

    private static TaskStageDefinition GetStageDefinition(TaskDefinition definition, TaskStageRuntimeState stageState)
    {
        if (definition?.StageGroups == null || stageState == null)
            return null;

        if (stageState.GroupIndex < 0 || stageState.GroupIndex >= definition.StageGroups.Count)
            return null;

        var group = definition.StageGroups[stageState.GroupIndex];
        if (group?.Stages == null || stageState.StageIndex < 0 || stageState.StageIndex >= group.Stages.Count)
            return null;

        return group.Stages[stageState.StageIndex];
    }

    private DeliverStageDefinition GetActiveDeliverStage()
    {
        var inventory = InventoryManager.Instance;
        if (inventory == null || !inventory.HasForcedDeliveryItem || string.IsNullOrWhiteSpace(inventory.ForcedDeliveryTaskInstanceId))
            return null;

        if (!tasksByInstanceId.TryGetValue(inventory.ForcedDeliveryTaskInstanceId, out var task) || task?.Definition == null || task.RuntimeState == null)
            return null;

        var stageState = task.RuntimeState.Stages?.FirstOrDefault(s =>
            s != null && string.Equals(s.StageId, inventory.ForcedDeliveryStageId, StringComparison.OrdinalIgnoreCase));

        return GetStageDefinition(task.Definition, stageState) as DeliverStageDefinition;
    }

    public void SetOriginalDeliverPickupActive(string hookId, bool isActive)
    {
        var hook = GetHook<DeliverPickupHook>(hookId);
        if (hook != null)
            hook.gameObject.SetActive(isActive);
    }
}
