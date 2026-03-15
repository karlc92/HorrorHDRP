using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("Horror/Tasks/Hooks/Cleanse Trigger Hook")]
public class CleanseTriggerHook : TaskHook
{
    public const string TriggeredEventName = "Triggered";

    public UnityEvent OnTriggered;
    public UnityEvent OnSettled;

    public void Trigger()
    {
        OnTriggered?.Invoke();
        ReportHookEvent(TriggeredEventName);
    }

    public void NotifySettled()
    {
        OnSettled?.Invoke();
    }
}
