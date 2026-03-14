using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class LoreDatabase
{
    public static List<LoreDefinition> LoadDefinitions()
    {
        return Resources.LoadAll<LoreDefinition>("Lore").Where(l => l != null).ToList();
    }
}
