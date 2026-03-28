using UnityEngine;

[CreateAssetMenu(menuName = "Horror/Inventory/Held Item")]
public class HeldItemDefinition : ScriptableObject
{
    public string ItemId;
    public HeldItemKind Kind = HeldItemKind.Passive;
    public GameObject FirstPersonPrefab;
    public FirstPersonHandSide FirstPersonHandSide = FirstPersonHandSide.Right;
    public FirstPersonHandStance FirstPersonHandStance = FirstPersonHandStance.Open;
    public Vector3 FirstPersonLocalPosition;
    public Vector3 FirstPersonLocalEuler;
    public Vector3 FirstPersonLocalScale = Vector3.one;
}
