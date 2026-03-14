public abstract class TaskStageBase
{
    public virtual void Enter()
    {
    }

    public virtual void Exit()
    {
    }

    public virtual void HandleHookEvent(TaskHook hook, string eventName)
    {
    }

    public virtual bool IsComplete()
    {
        return false;
    }
}
