using UnityEngine;

public class RiddleClueHook : TaskHook
{
    public const string InspectionClosedEventName = "InspectionClosed";

    public void NotifyInspectionClosed()
    {
        ReportHookEvent(InspectionClosedEventName);
    }
}
