using System;
using System.Collections.Generic;

[Serializable]
public class StoryState
{
    public bool Started = false;
    public bool Completed = false;
    public int CurrentBeatIndex = 0;
    public string CurrentBeatId = string.Empty;
    public string CurrentObjectiveTitle = string.Empty;
    public string CurrentObjectiveDetail = string.Empty;
    public string CurrentTriggerId = string.Empty;
    public string LastCheckpointId = string.Empty;
    public List<string> CompletedBeatIds = new List<string>();
}
