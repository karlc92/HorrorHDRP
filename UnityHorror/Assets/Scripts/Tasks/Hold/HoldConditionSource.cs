using UnityEngine;

public abstract class HoldConditionSource : MonoBehaviour
{
    public string SourceId;

    protected virtual void OnEnable()
    {
        TaskManager.Instance?.RegisterHoldConditionSource(this);
    }

    protected virtual void OnDisable()
    {
        if (TaskManager.Instance != null)
            TaskManager.Instance.UnregisterHoldConditionSource(this);
    }

    public abstract bool Evaluate(HoldConditionRequirementDefinition requirement);
}
