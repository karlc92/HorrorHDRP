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
        return TaskManager.Instance != null
            ? TaskManager.Instance.GetCurrentNightTaskEntries()
            : new List<TaskListEntryViewData>();
    }
}
