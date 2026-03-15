using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("Horror/Tasks/Hooks/Cleanse Trigger Hook")]
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
