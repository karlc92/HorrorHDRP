using UnityEngine.Events;

public class CleanseTriggerHook : TaskHook
{
    public const string TriggeredEventName = "Triggered";

    public UnityEvent OnTriggered;

    public void Trigger()
    {
        OnTriggered?.Invoke();
        ReportHookEvent(TriggeredEventName);
    }
}
