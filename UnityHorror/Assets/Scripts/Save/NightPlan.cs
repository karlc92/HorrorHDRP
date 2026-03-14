using System;
using System.Collections.Generic;

[Serializable]
public class NightPlan
{
    public int NightNumber = 1;
    public List<string> ActiveZoneIds = new List<string>();
    public List<GeneratedTaskPlan> Tasks = new List<GeneratedTaskPlan>();
    public List<GeneratedLorePlan> Lore = new List<GeneratedLorePlan>();
    public List<GeneratedNightModifierPlan> Modifiers = new List<GeneratedNightModifierPlan>();
}
