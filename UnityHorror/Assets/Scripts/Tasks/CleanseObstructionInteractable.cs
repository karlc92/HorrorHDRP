using UnityEngine;
using UnityEngine.Events;

public class CleanseObstructionInteractable : Interactable
{
    [SerializeField] private UnityEvent onCleared;
    [SerializeField] private UnityEvent onSettled;

    private CleanseObstructionSpreader owner;
    private string instanceId;
    private bool settled;
    private bool cleared;

    public void Initialize(
        CleanseObstructionSpreader owner,
        string instanceId,
        InteractionMode interactionMode,
        float holdDurationSeconds,
        bool resetHoldOnCancel)
    {
        this.owner = owner;
        this.instanceId = instanceId;
        ConfigureInteraction(interactionMode, holdDurationSeconds, resetHoldOnCancel);
    }

    public override bool CanInteract()
    {
        return base.CanInteract() && !settled && !cleared;
    }

    public override void Interact()
    {
        if (cleared || settled || owner == null)
            return;

        owner.TryClearInstance(instanceId);
    }

    public void NotifyCleared()
    {
        if (cleared)
            return;

        cleared = true;
        onCleared?.Invoke();
        gameObject.SetActive(false);
    }

    public void NotifySettled()
    {
        if (settled)
            return;

        settled = true;
        onSettled?.Invoke();
    }
}
