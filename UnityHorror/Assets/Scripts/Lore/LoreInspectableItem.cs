using UnityEngine;

public class LoreInspectableItem : InspectableItem
{
    public LoreDefinition LoreDefinition;

    public override void Interact()
    {
        base.Interact();

        if (LoreDefinition == null || Game.State?.Progression == null || string.IsNullOrWhiteSpace(LoreDefinition.LoreId))
            return;

        if (!Game.State.Progression.DiscoveredLoreIds.Contains(LoreDefinition.LoreId))
            Game.State.Progression.DiscoveredLoreIds.Add(LoreDefinition.LoreId);
    }
}
