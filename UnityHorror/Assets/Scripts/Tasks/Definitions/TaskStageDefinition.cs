using UnityEngine;

public abstract class TaskStageDefinition : ScriptableObject
{
    public string StageId;
    public TaskStageArchetype Archetype;
    public string DetailKeyOverride;

    public virtual string GetDetailKey(string taskId, int groupIndex, int stageIndex)
    {
        if (!string.IsNullOrWhiteSpace(DetailKeyOverride))
            return DetailKeyOverride;

        return $"task.{taskId}.group.{groupIndex}.stage.{stageIndex}";
    }
}
