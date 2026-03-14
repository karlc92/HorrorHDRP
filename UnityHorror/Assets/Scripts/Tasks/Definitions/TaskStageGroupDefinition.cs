using System.Collections.Generic;
using UnityEngine;

public class TaskStageGroupDefinition : ScriptableObject
{
    public string GroupId;
    public bool RunInParallel = false;
    public List<TaskStageDefinition> Stages = new List<TaskStageDefinition>();
}
