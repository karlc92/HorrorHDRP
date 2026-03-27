using System;
using UnityEngine;

[Serializable]
public class GameState
{
    public int Slot = 1;
    public StoryState Story = new StoryState();
    public InventoryState Inventory = new InventoryState();
    public Vector3 PlayerPos = Vector3.zero;
    public Quaternion PlayerRot = Quaternion.identity;
    public float TotalPlayTimeSeconds = 0f;
    public MonsterBrainState MonsterBrainState = new MonsterBrainState();
    public ProgressionState Progression = new ProgressionState();

    public void EnsureInitialized()
    {
        Story ??= new StoryState();
        Inventory ??= new InventoryState();
        Inventory.Items ??= new System.Collections.Generic.List<HeldItemRuntimeState>();
        MonsterBrainState ??= new MonsterBrainState();
        Progression ??= new ProgressionState();
        Progression.DiscoveredLoreIds ??= new System.Collections.Generic.List<string>();
        Progression.UnlockFlags ??= new System.Collections.Generic.List<string>();
    }
}
