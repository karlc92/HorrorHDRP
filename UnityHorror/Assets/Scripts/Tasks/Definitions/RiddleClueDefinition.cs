using System;

[Serializable]
public class RiddleClueDefinition
{
    [IdReference(typeof(TaskHook), nameof(TaskHook.HookId))]
    public string HookId;
    public string ClueKey;
}
