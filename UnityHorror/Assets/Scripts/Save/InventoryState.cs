using System;
using System.Collections.Generic;

[Serializable]
public class InventoryState
{
    public string ActiveItemId;
    public List<HeldItemRuntimeState> Items = new List<HeldItemRuntimeState>();
}
