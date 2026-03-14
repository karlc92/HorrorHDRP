using System;

[Serializable]
public class TaskRuntimeState
{
    public string TaskInstanceId;
    public string TaskDefinitionId;
    public int CurrentStageIndex;
    public bool Completed;
    public bool RequiredForNightCompletion = true;
}
