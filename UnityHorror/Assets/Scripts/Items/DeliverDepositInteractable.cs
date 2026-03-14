using UnityEngine;

[RequireComponent(typeof(DeliverDepositHook))]
public class DeliverDepositInteractable : Interactable
{
    private DeliverDepositHook depositHook;

    private void Awake()
    {
        depositHook = GetComponent<DeliverDepositHook>();
    }

    public override void Interact()
    {
        depositHook ??= GetComponent<DeliverDepositHook>();
        depositHook?.ReportAttempt();
    }
}
