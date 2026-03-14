using System;
using System.Collections.Generic;

[Serializable]
public class TaskRuntimeState
{
    public string TaskInstanceId;
    public string TaskDefinitionId;
    public int CurrentGroupIndex;
    public bool Completed;
    public bool RequiredForNightCompletion = true;
    public List<TaskStageRuntimeState> Stages = new List<TaskStageRuntimeState>();
}
