using System;

[Serializable]
public class GeneratedNightModifierPlan
{
    [IdReference(typeof(NightModifierDefinition), nameof(NightModifierDefinition.ModifierId), IdReferenceScope.ResourcesAssets)]
    public string ModifierId;
}
