using UnityEngine;

[CreateAssetMenu(menuName = "Horror/Lore Definition")]
public class LoreDefinition : ScriptableObject
{
    public string LoreId;
    public string PrimaryZoneId;
    public string TitleKeyOverride;
    public string BodyKeyOverride;

    public string GetTitleKey() => string.IsNullOrWhiteSpace(TitleKeyOverride) ? $"lore.{LoreId}.title" : TitleKeyOverride;
    public string GetBodyKey() => string.IsNullOrWhiteSpace(BodyKeyOverride) ? $"lore.{LoreId}.body" : BodyKeyOverride;
}
