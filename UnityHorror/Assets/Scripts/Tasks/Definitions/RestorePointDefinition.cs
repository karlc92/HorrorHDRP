using System;

[Serializable]
public class RestorePointDefinition
{
    [IdReference(typeof(TaskHook), nameof(TaskHook.HookId))]
    public string HookId;
    public string DetailKey;
}
