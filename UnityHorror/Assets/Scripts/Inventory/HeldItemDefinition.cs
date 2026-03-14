using UnityEngine;

[CreateAssetMenu(menuName = "Horror/Inventory/Held Item")]
public class HeldItemDefinition : ScriptableObject
{
    public string ItemId;
    public HeldItemKind Kind = HeldItemKind.Passive;
}
