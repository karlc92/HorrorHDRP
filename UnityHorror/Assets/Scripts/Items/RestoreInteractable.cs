using UnityEngine;

[RequireComponent(typeof(InteractionTaskHook))]
[AddComponentMenu("Horror/Items/Restore Interactable")]
public class RestoreInteractable : Interactable
{
    [SerializeField] private InteractionTaskHook interactionHook;

    protected override void Reset()
    {
        base.Reset();
        interactionHook = GetComponent<InteractionTaskHook>();
    }

    public override bool CanInteract()
    {
        if (!base.CanInteract())
            return false;

        interactionHook ??= GetComponent<InteractionTaskHook>();
        return interactionHook != null && (TaskManager.Instance == null || TaskManager.Instance.IsHookCurrentlyValid(interactionHook));
    }

    public override void Interact()
    {
        if (interactionHook == null)
            interactionHook = GetComponent<InteractionTaskHook>();

        interactionHook?.Interact();
    }
}
