using UnityEngine;

public abstract class TaskHook : MonoBehaviour
{
    public string HookId;
    public Zone Zone;

    protected void ReportHookEvent(string eventName)
    {
        // Hook-to-task routing will be fleshed out once scene bindings are wired in.
        if (TaskManager.Instance == null)
            return;
    }
}
