using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("Horror/Tasks/Hooks/Riddle Answer Hook")]
public class RiddleAnswerHook : TaskHook
{
    public const string SelectedEventName = "Selected";

    public UnityEvent OnSelected;

    public void SelectAnswer()
    {
        OnSelected?.Invoke();
        ReportHookEvent(SelectedEventName);
    }
}
