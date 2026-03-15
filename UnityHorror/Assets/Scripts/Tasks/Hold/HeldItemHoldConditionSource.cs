[UnityEngine.AddComponentMenu("Horror/Tasks/Hold/Held Item Hold Condition Source")]
public class HeldItemHoldConditionSource : HoldConditionSource
{
    public override bool Evaluate(HoldConditionRequirementDefinition requirement)
    {
        if (requirement == null || string.IsNullOrWhiteSpace(requirement.RequiredItemId))
            return false;

        var inventory = InventoryManager.Instance;
        if (inventory == null || !inventory.HasActiveItem(requirement.RequiredItemId))
            return false;

        if (string.IsNullOrWhiteSpace(requirement.RequiredItemConditionId))
            return true;

        return inventory.GetCondition(requirement.RequiredItemId, requirement.RequiredItemConditionId);
    }
}
