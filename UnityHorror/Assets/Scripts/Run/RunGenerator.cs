using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RunGenerator
{
    public static RunPlan Generate(
        int seed,
        IReadOnlyList<TaskDefinition> taskDefinitions,
        IReadOnlyList<LoreDefinition> loreDefinitions,
        RunGenerationSettings settings,
        ProgressionState progression)
    {
        var random = new System.Random(seed);
        var plan = new RunPlan();

        int nightCount = Mathf.Max(1, settings != null ? settings.NightCount : 7);
        var remainingTasks = new List<TaskDefinition>(taskDefinitions ?? Array.Empty<TaskDefinition>());
        var availableLore = (loreDefinitions ?? Array.Empty<LoreDefinition>())
            .Where(l => l != null && !progression.DiscoveredLoreIds.Contains(l.LoreId))
            .ToList();

        var loreAssignments = AssignLoreToNights(random, availableLore, settings, nightCount);

        for (int nightIndex = 0; nightIndex < nightCount; nightIndex++)
        {
            var nightNumber = nightIndex + 1;
            var night = new NightPlan { NightNumber = nightNumber };
            night.Lore.AddRange(loreAssignments.TryGetValue(nightNumber, out var assignedLore)
                ? assignedLore
                : new List<GeneratedLorePlan>());

            int targetBudget = GetNightBudget(settings, nightIndex);
            int currentBudget = 0;

            var compatibleTasks = remainingTasks.ToList();
            Shuffle(random, compatibleTasks);

            foreach (var task in compatibleTasks)
            {
                if (task == null)
                    continue;

                if (currentBudget >= targetBudget && night.Tasks.Count > 0)
                    break;

                var instanceId = $"task_instance.n{nightNumber}.{night.Tasks.Count + 1}.{SanitizeId(task.TaskId)}";
                night.Tasks.Add(new GeneratedTaskPlan
                {
                    TaskInstanceId = instanceId,
                    TaskDefinitionId = task.TaskId,
                });

                currentBudget += Mathf.Max(1, task.Difficulty);
                plan.UsedTaskDefinitionIds.Add(task.TaskId);
            }

            foreach (var task in night.Tasks)
                remainingTasks.RemoveAll(t => t != null && t.TaskId == task.TaskDefinitionId);

            foreach (var lore in night.Lore)
                plan.AssignedLoreDefinitionIds.Add(lore.LoreId);

            plan.Nights.Add(night);
        }

        return plan;
    }

    private static Dictionary<int, List<GeneratedLorePlan>> AssignLoreToNights(
        System.Random random,
        List<LoreDefinition> loreDefinitions,
        RunGenerationSettings settings,
        int nightCount)
    {
        var result = new Dictionary<int, List<GeneratedLorePlan>>();
        if (loreDefinitions == null || loreDefinitions.Count == 0)
            return result;

        Shuffle(random, loreDefinitions);

        int maxLore = Mathf.Clamp(settings != null ? settings.MaxLorePerRun : 0, 0, loreDefinitions.Count);
        for (int i = 0; i < maxLore; i++)
        {
            int nightNumber = random.Next(1, nightCount + 1);
            if (!result.TryGetValue(nightNumber, out var list))
            {
                list = new List<GeneratedLorePlan>();
                result[nightNumber] = list;
            }

            list.Add(new GeneratedLorePlan
            {
                LoreId = loreDefinitions[i].LoreId,
            });
        }

        return result;
    }

    private static int GetNightBudget(RunGenerationSettings settings, int nightIndex)
    {
        if (settings == null || settings.NightDifficultyBudgets == null || settings.NightDifficultyBudgets.Count == 0)
            return 3 + nightIndex;

        if (nightIndex < settings.NightDifficultyBudgets.Count)
            return Mathf.Max(1, settings.NightDifficultyBudgets[nightIndex]);

        return Mathf.Max(1, settings.NightDifficultyBudgets[settings.NightDifficultyBudgets.Count - 1]);
    }

    private static void Shuffle<T>(System.Random random, IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
        }
    }

    private static string SanitizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Replace(" ", "_").ToLowerInvariant();
    }
}
