using UnityEngine;

public class RiddleAnswerHook : TaskHook
{
    public const string SelectedEventName = "Selected";

    public void SelectAnswer()
    {
        ReportHookEvent(SelectedEventName);
    }
}
