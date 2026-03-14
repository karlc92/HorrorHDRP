using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class TaskBase
{
    public TaskDefinition Definition { get; private set; }
    public TaskRuntimeState RuntimeState { get; private set; }
    protected readonly List<TaskHook> BoundHooks = new List<TaskHook>();
    protected readonly List<TaskStageBase> Stages = new List<TaskStageBase>();

    public virtual void Initialize(TaskDefinition definition, TaskRuntimeState runtimeState)
    {
        Definition = definition;
        RuntimeState = runtimeState;
    }

    public virtual void BindHooks(IEnumerable<TaskHook> hooks)
    {
        BoundHooks.Clear();
        if (hooks == null)
            return;

        BoundHooks.AddRange(hooks);
    }

    public virtual void HandleHookEvent(TaskHook hook, string eventName)
    {
    }

    public virtual void Tick(float deltaTime)
    {
    }

    public virtual bool TryHandleForcedDrop(PlayerController player)
    {
        return false;
    }

    public virtual string GetDisplayTitleKey() => Definition != null ? Definition.GetTitleKey() : string.Empty;

    public virtual IReadOnlyList<string> GetCurrentDetailKeys()
    {
        if (Definition == null || RuntimeState == null || Definition.StageGroups == null)
            return System.Array.Empty<string>();

        if (RuntimeState.CurrentGroupIndex < 0 || RuntimeState.CurrentGroupIndex >= Definition.StageGroups.Count)
            return System.Array.Empty<string>();

        var group = Definition.StageGroups[RuntimeState.CurrentGroupIndex];
        if (group == null || group.Stages == null)
            return System.Array.Empty<string>();

        var keys = new List<string>();
        for (int i = 0; i < group.Stages.Count; i++)
        {
            var stageDefinition = group.Stages[i];
            if (stageDefinition == null)
                continue;

            var stageState = GetStageState(RuntimeState.CurrentGroupIndex, i);
            if (stageState != null && stageState.Completed)
                continue;

            keys.Add(stageDefinition.GetDetailKey(Definition.TaskId, RuntimeState.CurrentGroupIndex, i));

            if (stageState?.DiscoveredDetailKeys == null)
                continue;

            foreach (var discoveredKey in stageState.DiscoveredDetailKeys)
            {
                if (!string.IsNullOrWhiteSpace(discoveredKey))
                    keys.Add(discoveredKey);
            }
        }

        return keys;
    }

    public virtual IReadOnlyList<TaskListDetailViewData> GetCurrentDetails()
    {
        var keys = GetCurrentDetailKeys();
        var details = new List<TaskListDetailViewData>(keys.Count);
        foreach (var key in keys)
        {
            details.Add(new TaskListDetailViewData
            {
                Key = key,
                IsSatisfied = false,
            });
        }

        return details;
    }

    public virtual IReadOnlyList<TaskStageRuntimeState> GetActiveStageStates()
    {
        if (RuntimeState == null)
            return System.Array.Empty<TaskStageRuntimeState>();

        return RuntimeState.Stages
            .Where(s => s != null && s.GroupIndex == RuntimeState.CurrentGroupIndex && !s.Completed)
            .ToList();
    }

    public virtual bool TryCompleteStage(string stageId)
    {
        if (RuntimeState == null || string.IsNullOrWhiteSpace(stageId))
            return false;

        var stageState = RuntimeState.Stages.FirstOrDefault(s =>
            s != null &&
            s.GroupIndex == RuntimeState.CurrentGroupIndex &&
            string.Equals(s.StageId, stageId, System.StringComparison.OrdinalIgnoreCase));

        if (stageState == null)
            return false;

        stageState.Completed = true;
        TryAdvanceGroup();
        return true;
    }

    protected virtual void TryAdvanceGroup()
    {
        if (Definition == null || RuntimeState == null || Definition.StageGroups == null)
            return;

        bool groupComplete = RuntimeState.Stages
            .Where(s => s != null && s.GroupIndex == RuntimeState.CurrentGroupIndex)
            .All(s => s.Completed);

        if (!groupComplete)
            return;

        RuntimeState.CurrentGroupIndex++;
        if (RuntimeState.CurrentGroupIndex >= Definition.StageGroups.Count)
        {
            RuntimeState.Completed = true;
        }
    }

    protected TaskStageRuntimeState GetStageState(int groupIndex, int stageIndex)
    {
        if (RuntimeState == null)
            return null;

        return RuntimeState.Stages.FirstOrDefault(s => s != null && s.GroupIndex == groupIndex && s.StageIndex == stageIndex);
    }

    protected T GetBoundHook<T>(string hookId) where T : TaskHook
    {
        if (string.IsNullOrWhiteSpace(hookId))
            return null;

        return BoundHooks
            .OfType<T>()
            .FirstOrDefault(h => h != null && string.Equals(h.HookId, hookId, System.StringComparison.OrdinalIgnoreCase));
    }

    protected void ApplyWrongAnswerThreat(int amount)
    {
        if (amount <= 0)
            return;

        var monsterManager = Object.FindFirstObjectByType<MonsterManager>();
        monsterManager?.AddThreat(amount);
    }
}
