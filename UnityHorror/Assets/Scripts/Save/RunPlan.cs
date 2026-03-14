using System;
using System.Collections.Generic;

[Serializable]
public class RunPlan
{
    public List<NightPlan> Nights = new List<NightPlan>();
    public List<string> UsedTaskDefinitionIds = new List<string>();
    public List<string> AssignedLoreDefinitionIds = new List<string>();
}
