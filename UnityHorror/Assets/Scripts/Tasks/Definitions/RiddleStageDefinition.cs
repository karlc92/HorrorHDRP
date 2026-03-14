using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Horror/Tasks/Stages/Riddle Stage")]
public class RiddleStageDefinition : TaskStageDefinition
{
    [IdReference(typeof(TaskHook), nameof(TaskHook.HookId))]
    public string CorrectHookId;
    [IdReference(typeof(TaskHook), nameof(TaskHook.HookId))]
    public List<string> CandidateHookIds = new List<string>();
    public bool ApplyThreatOnWrongAnswer = true;
    public int ThreatOnWrongAnswer = 10;
    public string WrongAnswerEventName;
    public List<RiddleClueDefinition> AdditionalClues = new List<RiddleClueDefinition>();
}
