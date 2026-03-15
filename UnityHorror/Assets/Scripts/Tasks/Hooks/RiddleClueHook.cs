using UnityEngine;

[AddComponentMenu("Horror/Tasks/Hooks/Riddle Clue Hook")]
public class RiddleClueHook : TaskHook
{
    public const string InspectionClosedEventName = "InspectionClosed";

    public void NotifyInspectionClosed()
    {
        ReportHookEvent(InspectionClosedEventName);
    }
}
