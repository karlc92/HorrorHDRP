using UnityEngine;

[RequireComponent(typeof(DeliverDepositHook))]
[AddComponentMenu("Horror/Items/Deliver Deposit Interactable")]
public class DeliverDepositInteractable : Interactable
{
    private DeliverDepositHook depositHook;

    protected override void Awake()
    {
        base.Awake();
        depositHook = GetComponent<DeliverDepositHook>();
    }

    public override bool CanInteract()
    {
        if (!base.CanInteract())
            return false;

        depositHook ??= GetComponent<DeliverDepositHook>();
        return depositHook != null && (TaskManager.Instance == null || TaskManager.Instance.IsHookCurrentlyValid(depositHook));
    }

    public override void Interact()
    {
        depositHook ??= GetComponent<DeliverDepositHook>();
        depositHook?.ReportAttempt();
    }
}
