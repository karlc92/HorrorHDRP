using UnityEngine;

[RequireComponent(typeof(RiddleClueHook))]
[AddComponentMenu("Horror/Items/Riddle Clue Inspectable")]
public class RiddleClueInspectableItem : InspectableItem
{
    [SerializeField] private RiddleClueHook clueHook;

    protected override void Reset()
    {
        base.Reset();
        clueHook = GetComponent<RiddleClueHook>();
    }

    public override bool CanInteract()
    {
        if (!base.CanInteract())
            return false;

        clueHook ??= GetComponent<RiddleClueHook>();
        return clueHook != null && (TaskManager.Instance == null || TaskManager.Instance.IsHookCurrentlyValid(clueHook));
    }

    public override void NotifyInspectionClosed()
    {
        base.NotifyInspectionClosed();

        if (clueHook == null)
            clueHook = GetComponent<RiddleClueHook>();

        clueHook?.NotifyInspectionClosed();
    }
}
