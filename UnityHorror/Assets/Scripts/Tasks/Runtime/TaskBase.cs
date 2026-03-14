using System.Collections.Generic;

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

    public virtual string GetDisplayTitleKey() => Definition != null ? Definition.GetTitleKey() : string.Empty;

    public virtual string GetCurrentDetailKey()
    {
        return Definition != null ? Definition.GetDetailKey(RuntimeState != null ? RuntimeState.CurrentStageIndex : 0) : string.Empty;
    }
}
