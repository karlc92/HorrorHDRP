using UnityEngine;

[RequireComponent(typeof(InteractionTaskHook))]
[AddComponentMenu("Horror/Items/Interaction Hook Interactable")]
public class InteractionHookInteractable : Interactable
{
    [SerializeField] private InteractionTaskHook interactionHook;

    private void Reset()
    {
        interactionHook = GetComponent<InteractionTaskHook>();
    }

    public override void Interact()
    {
        if (interactionHook == null)
            interactionHook = GetComponent<InteractionTaskHook>();

        interactionHook?.Interact();
    }
}
