using System;
using System.Collections.Generic;

[Serializable]
public class HeldItemRuntimeState
{
    public string ItemId;
    public HeldItemKind Kind = HeldItemKind.Passive;
    public List<HeldItemConditionState> Conditions = new List<HeldItemConditionState>();
}
