using System;

[Serializable]
public class HoldConditionRequirementDefinition
{
    [IdReference(typeof(HoldConditionSource), nameof(HoldConditionSource.SourceId))]
    public string SourceId;
    [IdReference(typeof(HeldItemDefinition), nameof(HeldItemDefinition.ItemId), IdReferenceScope.ResourcesAssets)]
    public string RequiredItemId;
    public string RequiredItemConditionId;
    public string DetailKey;
    public string SatisfiedDetailKey;
}
