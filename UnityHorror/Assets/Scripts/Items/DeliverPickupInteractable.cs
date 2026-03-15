using UnityEngine;

[RequireComponent(typeof(DeliverPickupHook))]
[AddComponentMenu("Horror/Items/Deliver Pickup Interactable")]
public class DeliverPickupInteractable : Interactable
{
    private DeliverPickupHook pickupHook;

    private void Awake()
    {
        pickupHook = GetComponent<DeliverPickupHook>();
    }

    public override void Interact()
    {
        pickupHook ??= GetComponent<DeliverPickupHook>();
        pickupHook?.NotifyPickedUp();
    }
}
