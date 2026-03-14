using System.Collections.Generic;
using UnityEngine;

public abstract class TaskDefinition : ScriptableObject
{
    public string TaskId;
    public int Difficulty = 1;
    public bool RequiredForNightCompletion = true;
    [IdReference(typeof(Zone), nameof(Zone.ZoneId))]
    public List<string> RequiredZoneIds = new List<string>();
    public List<TaskStageGroupDefinition> StageGroups = new List<TaskStageGroupDefinition>();
    public string TitleKeyOverride;

    public virtual string GetTitleKey()
    {
        return !string.IsNullOrWhiteSpace(TitleKeyOverride)
            ? TitleKeyOverride
            : $"task.{TaskId}.title";
    }
}
