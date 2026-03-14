using System;
using System.Collections.Generic;

[Serializable]
public class NightRuntimeState
{
    public List<TaskRuntimeState> Tasks = new List<TaskRuntimeState>();
    public List<string> SpawnedLoreIds = new List<string>();
    public bool CanEndNight = false;
    public bool NightStarted = false;
}
