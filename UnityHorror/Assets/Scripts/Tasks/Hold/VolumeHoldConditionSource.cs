using UnityEngine;

[RequireComponent(typeof(Collider))]
[AddComponentMenu("Horror/Tasks/Hold/Volume Hold Condition Source")]
public class VolumeHoldConditionSource : HoldConditionSource
{
    [SerializeField] private bool invertResult = false;

    private int playerOverlapCount;

    private void Reset()
    {
        if (TryGetComponent<Collider>(out var collider))
            collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        playerOverlapCount++;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        playerOverlapCount = Mathf.Max(0, playerOverlapCount - 1);
    }

    public override bool Evaluate(HoldConditionRequirementDefinition requirement)
    {
        bool inside = playerOverlapCount > 0;
        return invertResult ? !inside : inside;
    }
}
