using UnityEngine;

[RequireComponent(typeof(RiddleClueHook))]
[AddComponentMenu("Horror/Items/Riddle Clue Inspectable")]
public class RiddleClueInspectableItem : InspectableItem
{
    [SerializeField] private RiddleClueHook clueHook;

    private void Reset()
    {
        clueHook = GetComponent<RiddleClueHook>();
    }

    public override void NotifyInspectionClosed()
    {
        base.NotifyInspectionClosed();

        if (clueHook == null)
            clueHook = GetComponent<RiddleClueHook>();

        clueHook?.NotifyInspectionClosed();
    }
}
