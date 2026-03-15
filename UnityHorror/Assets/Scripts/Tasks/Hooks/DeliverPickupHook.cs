using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("Horror/Tasks/Hooks/Deliver Pickup Hook")]
public class DeliverPickupHook : CarryTaskHook
{
    public const string PickedUpEventName = "DeliverPickedUp";
    public const string DroppedEventName = "DeliverDropped";

    public UnityEvent OnPickedUp;
    public UnityEvent OnDropped;

    public void NotifyPickedUp()
    {
        OnPickedUp?.Invoke();
        ReportHookEvent(PickedUpEventName);
    }

    public void NotifyDropped()
    {
        OnDropped?.Invoke();
        ReportHookEvent(DroppedEventName);
    }
}
