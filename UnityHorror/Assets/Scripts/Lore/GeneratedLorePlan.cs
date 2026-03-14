using System;

[Serializable]
public class GeneratedLorePlan
{
    [IdReference(typeof(LoreDefinition), nameof(LoreDefinition.LoreId), IdReferenceScope.ResourcesAssets)]
    public string LoreId;
}
