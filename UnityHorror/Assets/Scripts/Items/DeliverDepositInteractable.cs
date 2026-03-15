using UnityEngine;

[RequireComponent(typeof(DeliverDepositHook))]
[AddComponentMenu("Horror/Items/Deliver Deposit Interactable")]
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
