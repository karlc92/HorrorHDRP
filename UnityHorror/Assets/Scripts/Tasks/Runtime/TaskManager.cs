using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    private readonly Dictionary<string, TaskDefinition> definitionsById = new Dictionary<string, TaskDefinition>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TaskBase> tasksByInstanceId = new Dictionary<string, TaskBase>(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        BuildDefinitionCache(LoadDefinitions());
        RebuildTasksFromRunState();
    }

    public static List<TaskDefinition> LoadDefinitions()
    {
        return Resources.LoadAll<TaskDefinition>("Tasks").Where(t => t != null).ToList();
    }

    public static NightRuntimeState CreateNightRuntimeStateForPlan(NightPlan plan)
    {
        var state = new NightRuntimeState();
        if (plan == null)
            return state;

        foreach (var task in plan.Tasks)
        {
            state.Tasks.Add(new TaskRuntimeState
            {
                TaskInstanceId = task.TaskInstanceId,
                TaskDefinitionId = task.TaskDefinitionId,
                CurrentStageIndex = 0,
                Completed = false,
                RequiredForNightCompletion = true,
            });
        }

        return state;
    }

    public bool AreAllRequiredTasksComplete()
    {
        var taskStates = Game.State?.Run?.CurrentNightState?.Tasks;
        if (taskStates == null || taskStates.Count == 0)
            return false;

        foreach (var task in taskStates)
        {
            if (task.RequiredForNightCompletion && !task.Completed)
                return false;
        }

        return true;
    }

    public void DebugCompleteAllCurrentNightTasks()
    {
        var taskStates = Game.State?.Run?.CurrentNightState?.Tasks;
        if (taskStates == null)
            return;

        foreach (var task in taskStates)
        {
            if (task == null)
                continue;

            task.Completed = true;
        }

        if (Game.State?.Run?.CurrentNightState != null)
            Game.State.Run.CurrentNightState.CanEndNight = AreAllRequiredTasksComplete();
    }

    public IEnumerable<TaskRuntimeState> GetCurrentNightTasks()
    {
        return Game.State?.Run?.CurrentNightState?.Tasks != null
            ? (IEnumerable<TaskRuntimeState>)Game.State.Run.CurrentNightState.Tasks
            : Array.Empty<TaskRuntimeState>();
    }

    private void BuildDefinitionCache(IEnumerable<TaskDefinition> definitions)
    {
        definitionsById.Clear();
        foreach (var definition in definitions)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.TaskId))
                continue;

            definitionsById[definition.TaskId] = definition;
        }
    }

    private void RebuildTasksFromRunState()
    {
        tasksByInstanceId.Clear();
        var taskStates = Game.State?.Run?.CurrentNightState?.Tasks;
        if (taskStates == null)
            return;

        foreach (var taskState in taskStates)
        {
            if (taskState == null || string.IsNullOrWhiteSpace(taskState.TaskDefinitionId))
                continue;

            if (!definitionsById.TryGetValue(taskState.TaskDefinitionId, out var definition))
                continue;

            var runtimeTask = CreateRuntimeTask(definition);
            runtimeTask.Initialize(definition, taskState);
            tasksByInstanceId[taskState.TaskInstanceId] = runtimeTask;
        }
    }

    private static TaskBase CreateRuntimeTask(TaskDefinition definition)
    {
        return definition.Archetype switch
        {
            TaskArchetype.Interpret => new InterpretTask(),
            TaskArchetype.Restore => new RestoreTask(),
            TaskArchetype.Contain => new ContainTask(),
            TaskArchetype.Distract => new DistractTask(),
            TaskArchetype.Endure => new EndureTask(),
            TaskArchetype.Escort => new EscortTask(),
            TaskArchetype.Choose => new ChooseTask(),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
}
