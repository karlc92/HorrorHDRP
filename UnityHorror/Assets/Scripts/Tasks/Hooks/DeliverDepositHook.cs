using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("Horror/Tasks/Hooks/Deliver Deposit Hook")]
public class DeliverDepositHook : CarryTaskHook
{
    public const string AttemptedEventName = "DeliverAttempted";
    public const string DeliveredEventName = "DeliverCompleted";
    public const string InvalidDeliveryEventName = "DeliverRejected";

    public UnityEvent OnDelivered;
    public UnityEvent OnInvalidDelivery;

    public void ReportAttempt()
    {
        ReportHookEvent(AttemptedEventName);
    }

    public void NotifyDelivered()
    {
        OnDelivered?.Invoke();
        ReportHookEvent(DeliveredEventName);
    }

    public void NotifyInvalidDelivery()
    {
        OnInvalidDelivery?.Invoke();
        ReportHookEvent(InvalidDeliveryEventName);
    }
}
