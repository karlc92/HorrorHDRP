using UnityEngine;

[RequireComponent(typeof(DeliverPickupHook))]
[AddComponentMenu("Horror/Items/Deliver Pickup Interactable")]
public class DeliverPickupInteractable : Interactable
{
    private DeliverPickupHook pickupHook;

    protected override void Awake()
    {
        base.Awake();
        pickupHook = GetComponent<DeliverPickupHook>();
    }

    public override bool CanInteract()
    {
        if (!base.CanInteract())
            return false;

        pickupHook ??= GetComponent<DeliverPickupHook>();
        return pickupHook != null && (TaskManager.Instance == null || TaskManager.Instance.IsHookCurrentlyValid(pickupHook));
    }

    public override void Interact()
    {
        pickupHook ??= GetComponent<DeliverPickupHook>();
        pickupHook?.NotifyPickedUp();
    }
}
