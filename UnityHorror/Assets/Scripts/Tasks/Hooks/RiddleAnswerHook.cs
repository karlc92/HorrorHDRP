using UnityEngine;

[AddComponentMenu("Horror/Tasks/Hooks/Riddle Answer Hook")]
public class RiddleAnswerHook : TaskHook
{
    public const string SelectedEventName = "Selected";

    public void SelectAnswer()
    {
        ReportHookEvent(SelectedEventName);
    }
}
