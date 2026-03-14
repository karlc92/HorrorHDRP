using UnityEngine.Events;

public class InteractionTaskHook : TaskHook
{
    public const string InteractedEventName = "Interacted";
    public const string StepSucceededEventName = "StepSucceeded";
    public const string StepFailedEventName = "StepFailed";
    public const string TaskCompletedEventName = "TaskCompleted";

    public UnityEvent OnInteracted;
    public UnityEvent OnStepSucceeded;
    public UnityEvent OnStepFailed;
    public UnityEvent OnTaskCompleted;

    public void Interact()
    {
        OnInteracted?.Invoke();
        ReportHookEvent(InteractedEventName);
    }

    public void NotifyStepSucceeded()
    {
        OnStepSucceeded?.Invoke();
    }

    public void NotifyStepFailed()
    {
        OnStepFailed?.Invoke();
    }

    public void NotifyTaskCompleted()
    {
        OnTaskCompleted?.Invoke();
    }
}
