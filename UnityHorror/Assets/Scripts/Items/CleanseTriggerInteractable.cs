using UnityEngine;

[RequireComponent(typeof(CleanseTriggerHook))]
[AddComponentMenu("Horror/Items/Cleanse Trigger Interactable")]
public class CleanseTriggerInteractable : Interactable
{
    [SerializeField] private CleanseTriggerHook triggerHook;

    protected override void Reset()
    {
        base.Reset();
        triggerHook = GetComponent<CleanseTriggerHook>();
    }

    public override bool CanInteract()
    {
        if (!base.CanInteract())
            return false;

        triggerHook ??= GetComponent<CleanseTriggerHook>();
        return triggerHook != null && (TaskManager.Instance == null || TaskManager.Instance.IsHookCurrentlyValid(triggerHook));
    }

    public override void Interact()
    {
        if (triggerHook == null)
            triggerHook = GetComponent<CleanseTriggerHook>();

        triggerHook?.Trigger();
    }
}
