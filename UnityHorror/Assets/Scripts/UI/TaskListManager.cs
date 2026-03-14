using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TaskListManager : MonoBehaviour
{
    public static TaskListManager Instance { get; private set; }

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            IsOpen = !IsOpen;
    }

    public IReadOnlyList<TaskListEntryViewData> GetCurrentEntries()
    {
        var entries = new List<TaskListEntryViewData>();
        var taskStates = TaskManager.Instance != null ? TaskManager.Instance.GetCurrentNightTasks() : System.Array.Empty<TaskRuntimeState>();

        foreach (var task in taskStates)
        {
            entries.Add(new TaskListEntryViewData
            {
                TaskInstanceId = task.TaskInstanceId,
                TitleKey = $"task.{task.TaskDefinitionId}.title",
                DetailKey = $"task.{task.TaskDefinitionId}.detail.{task.CurrentStageIndex}",
                Completed = task.Completed,
            });
        }

        return entries;
    }
}
