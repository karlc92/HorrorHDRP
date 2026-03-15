using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("Horror/Tasks/Hooks/Riddle Clue Hook")]
public class RiddleClueHook : TaskHook
{
    public const string InspectionClosedEventName = "InspectionClosed";

    public UnityEvent OnInspectionClosed;

    public void NotifyInspectionClosed()
    {
        OnInspectionClosed?.Invoke();
        ReportHookEvent(InspectionClosedEventName);
    }
}
