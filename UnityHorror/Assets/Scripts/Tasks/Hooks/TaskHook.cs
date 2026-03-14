using UnityEngine;

public abstract class TaskHook : MonoBehaviour
{
    public string HookId;
    public Zone Zone;

    protected virtual void OnEnable()
    {
        TaskManager.Instance?.RegisterHook(this);
    }

    protected virtual void OnDisable()
    {
        TaskManager.Instance?.UnregisterHook(this);
    }

    protected void ReportHookEvent(string eventName)
    {
        TaskManager.Instance?.ReportHookEvent(this, eventName);
    }
}
