using UnityEngine;

[RequireComponent(typeof(CleanseTriggerHook))]
public class CleanseTriggerInteractable : Interactable
{
    [SerializeField] private CleanseTriggerHook triggerHook;

    private void Reset()
    {
        triggerHook = GetComponent<CleanseTriggerHook>();
    }

    public override void Interact()
    {
        if (triggerHook == null)
            triggerHook = GetComponent<CleanseTriggerHook>();

        triggerHook?.Trigger();
    }
}
