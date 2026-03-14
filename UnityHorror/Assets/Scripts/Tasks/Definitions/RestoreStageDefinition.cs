using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Horror/Tasks/Stages/Restore Stage")]
public class RestoreStageDefinition : TaskStageDefinition
{
    public bool EnforceSequence = false;
    public List<RestorePointDefinition> RequiredPoints = new List<RestorePointDefinition>();
}
