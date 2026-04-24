using System;
using UnityEngine;

[Serializable]
public class GameState
{
    public int Slot = 1;
    public InventoryState Inventory = new InventoryState();
    public Vector3 PlayerPos = Vector3.zero;
    public Quaternion PlayerRot = Quaternion.identity;
    public float TotalPlayTimeSeconds = 0f;
    public MonsterBrainState MonsterBrainState = new MonsterBrainState();
    public WorldState World = new WorldState();

    public void EnsureInitialized()
    {
        Inventory ??= new InventoryState();
        Inventory.Items ??= new System.Collections.Generic.List<HeldItemRuntimeState>();
        MonsterBrainState ??= new MonsterBrainState();
        World ??= new WorldState();
        World.EnsureInitialized();
    }
}
