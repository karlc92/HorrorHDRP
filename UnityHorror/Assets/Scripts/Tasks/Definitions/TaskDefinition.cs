using System.Collections.Generic;
using UnityEngine;

public abstract class TaskDefinition : ScriptableObject
{
    public string TaskId;
    public TaskArchetype Archetype;
    public int Difficulty = 1;
    public bool RequiredForNightCompletion = true;
    public List<string> RequiredZoneIds = new List<string>();
    public string TitleKeyOverride;
    public string DetailKeyPrefixOverride;

    public virtual string GetTitleKey()
    {
        return !string.IsNullOrWhiteSpace(TitleKeyOverride)
            ? TitleKeyOverride
            : $"task.{TaskId}.title";
    }

    public virtual string GetDetailKey(int stageIndex)
    {
        if (!string.IsNullOrWhiteSpace(DetailKeyPrefixOverride))
            return $"{DetailKeyPrefixOverride}.{stageIndex}";

        return $"task.{TaskId}.detail.{stageIndex}";
    }
}
