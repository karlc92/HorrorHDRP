using System;

public class ComposedTask : TaskBase
{
    public override bool TryHandleForcedDrop(PlayerController player)
    {
        if (RuntimeState?.Stages == null || player == null)
            return false;

        var inventory = InventoryManager.Instance;
        if (inventory == null || !inventory.IsForcedDeliveryStage(RuntimeState.TaskInstanceId, inventory.ForcedDeliveryStageId))
            return false;

        var stageState = RuntimeState.Stages.Find(s =>
            s != null && string.Equals(s.StageId, inventory.ForcedDeliveryStageId, StringComparison.OrdinalIgnoreCase));
        if (stageState == null)
            return false;

        var stageDefinition = GetStageDefinition(stageState) as DeliverStageDefinition;
        if (stageDefinition == null || !stageDefinition.AllowDrop || stageDefinition.HeldItem == null)
            return false;

        if (stageDefinition.ApplyThreatOnDrop)
            ApplyWrongAnswerThreat(stageDefinition.ThreatOnDrop);

        var managedConditionIds = GetManagedDeliverConditionIds(stageDefinition);
        stageState.DeliverItemConditionStates = inventory.CopyConditionStates(stageDefinition.HeldItem.ItemId, managedConditionIds);

        ApplyHeldItemMutations(inventory, stageDefinition.HeldItem.ItemId, stageDefinition.OnDropConditionMutations);

        if (stageDefinition.ResetItemConditionsOnDrop)
        {
            foreach (var conditionId in managedConditionIds)
                inventory.SetCondition(stageDefinition.HeldItem.ItemId, conditionId, false);
        }

        stageState.DeliverItemConditionStates = inventory.CopyConditionStates(stageDefinition.HeldItem.ItemId, managedConditionIds);

        inventory.RemoveItem(stageDefinition.HeldItem.ItemId);
        inventory.ClearForcedDeliveryCarry(RuntimeState.TaskInstanceId, stageState.StageId);

        stageState.IsDeliverCarried = false;

        if (stageDefinition.ResetToOriginOnDrop)
        {
            stageState.HasDroppedDeliverPickup = false;
            stageState.DroppedDeliverPickupPosition = default;
            TaskManager.Instance?.RemoveRuntimeDeliverPickup(stageState.StageId);
            TaskManager.Instance?.SetOriginalDeliverPickupActive(stageDefinition.PickupHookId, true);
            return true;
        }

        var dropPosition = player.transform.position;
        stageState.HasDroppedDeliverPickup = true;
        stageState.DroppedDeliverPickupPosition = dropPosition;
        TaskManager.Instance?.SetOriginalDeliverPickupActive(stageDefinition.PickupHookId, false);
        TaskManager.Instance?.SpawnDroppedDeliverPickup(stageDefinition, stageState, dropPosition);
        return true;
    }

    public override System.Collections.Generic.IReadOnlyList<string> GetCurrentDetailKeys()
    {
        if (Definition == null || RuntimeState == null || Definition.StageGroups == null)
            return Array.Empty<string>();

        if (RuntimeState.CurrentGroupIndex < 0 || RuntimeState.CurrentGroupIndex >= Definition.StageGroups.Count)
            return Array.Empty<string>();

        var group = Definition.StageGroups[RuntimeState.CurrentGroupIndex];
        if (group == null || group.Stages == null)
            return Array.Empty<string>();

        var keys = new System.Collections.Generic.List<string>();
        for (int i = 0; i < group.Stages.Count; i++)
        {
            var stageDefinition = group.Stages[i];
            if (stageDefinition == null)
                continue;

            var stageState = GetStageState(RuntimeState.CurrentGroupIndex, i);
            if (stageState == null || stageState.Completed)
                continue;

            AppendStageDetailKeys(keys, stageDefinition, stageState, RuntimeState.CurrentGroupIndex, i);
        }

        return keys;
    }

    public override System.Collections.Generic.IReadOnlyList<TaskListDetailViewData> GetCurrentDetails()
    {
        if (Definition == null || RuntimeState == null || Definition.StageGroups == null)
            return Array.Empty<TaskListDetailViewData>();

        if (RuntimeState.CurrentGroupIndex < 0 || RuntimeState.CurrentGroupIndex >= Definition.StageGroups.Count)
            return Array.Empty<TaskListDetailViewData>();

        var group = Definition.StageGroups[RuntimeState.CurrentGroupIndex];
        if (group == null || group.Stages == null)
            return Array.Empty<TaskListDetailViewData>();

        var details = new System.Collections.Generic.List<TaskListDetailViewData>();
        for (int i = 0; i < group.Stages.Count; i++)
        {
            var stageDefinition = group.Stages[i];
            if (stageDefinition == null)
                continue;

            var stageState = GetStageState(RuntimeState.CurrentGroupIndex, i);
            if (stageState == null || stageState.Completed)
                continue;

            if (stageDefinition is HoldStageDefinition holdStage)
            {
                AppendHoldDetails(details, holdStage);
                continue;
            }

            details.Add(new TaskListDetailViewData
            {
                Key = stageDefinition.GetDetailKey(Definition.TaskId, RuntimeState.CurrentGroupIndex, i),
                IsSatisfied = false,
            });

            if (stageState.DiscoveredDetailKeys == null)
                continue;

            foreach (var discoveredKey in stageState.DiscoveredDetailKeys)
            {
                if (string.IsNullOrWhiteSpace(discoveredKey))
                    continue;

                details.Add(new TaskListDetailViewData
                {
                    Key = discoveredKey,
                    IsSatisfied = true,
                });
            }
        }

        return details;
    }

    public override void HandleHookEvent(TaskHook hook, string eventName)
    {
        base.HandleHookEvent(hook, eventName);

        if (Definition == null || RuntimeState == null || hook == null || string.IsNullOrWhiteSpace(eventName))
            return;

        int groupIndex = RuntimeState.CurrentGroupIndex;
        if (groupIndex < 0 || Definition.StageGroups == null || groupIndex >= Definition.StageGroups.Count)
            return;

        var group = Definition.StageGroups[groupIndex];
        if (group == null || group.Stages == null)
            return;

        for (int stageIndex = 0; stageIndex < group.Stages.Count; stageIndex++)
        {
            var stageDefinition = group.Stages[stageIndex];
            if (stageDefinition == null)
                continue;

            var stageState = GetStageState(groupIndex, stageIndex);
            if (stageState == null || stageState.Completed)
                continue;

            HandleStageEvent(stageDefinition, stageState, hook, eventName);

            if (RuntimeState.Completed || RuntimeState.CurrentGroupIndex != groupIndex)
                return;
        }
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        if (Definition == null || RuntimeState == null || Definition.StageGroups == null)
            return;

        int groupIndex = RuntimeState.CurrentGroupIndex;
        if (groupIndex < 0 || groupIndex >= Definition.StageGroups.Count)
            return;

        var group = Definition.StageGroups[groupIndex];
        if (group == null || group.Stages == null)
            return;

        for (int stageIndex = 0; stageIndex < group.Stages.Count; stageIndex++)
        {
            var stageDefinition = group.Stages[stageIndex];
            if (stageDefinition == null)
                continue;

            var stageState = GetStageState(groupIndex, stageIndex);
            if (stageState == null || stageState.Completed)
                continue;

            if (stageDefinition is HoldStageDefinition holdStage)
            {
                HandleHoldStageTick(holdStage, stageState, deltaTime);

                if (RuntimeState.Completed || RuntimeState.CurrentGroupIndex != groupIndex)
                    return;
            }
        }
    }

    private void HandleStageEvent(
        TaskStageDefinition stageDefinition,
        TaskStageRuntimeState stageState,
        TaskHook hook,
        string eventName)
    {
        if (stageDefinition is RiddleStageDefinition riddleStage)
        {
            HandleRiddleStageEvent(riddleStage, stageState, hook, eventName);
            return;
        }

        if (stageDefinition is RestoreStageDefinition restoreStage)
        {
            HandleRestoreStageEvent(restoreStage, stageState, hook, eventName);
            return;
        }

        if (stageDefinition is CleanseStageDefinition cleanseStage)
        {
            HandleCleanseStageEvent(cleanseStage, stageState, hook, eventName);
            return;
        }

        if (stageDefinition is DeliverStageDefinition deliverStage)
        {
            HandleDeliverStageEvent(deliverStage, stageState, hook, eventName);
            return;
        }

        if (stageDefinition is HoldStageDefinition holdStage)
        {
            HandleHoldStageEvent(holdStage, stageState, hook, eventName);
            return;
        }
    }

    private void AppendStageDetailKeys(
        System.Collections.Generic.List<string> keys,
        TaskStageDefinition stageDefinition,
        TaskStageRuntimeState stageState,
        int groupIndex,
        int stageIndex)
    {
        if (stageDefinition is RestoreStageDefinition restoreStage)
        {
            AppendRestoreDetailKeys(keys, restoreStage, stageState);
            return;
        }

        keys.Add(stageDefinition.GetDetailKey(Definition.TaskId, groupIndex, stageIndex));

        if (stageState.DiscoveredDetailKeys == null)
            return;

        foreach (var discoveredKey in stageState.DiscoveredDetailKeys)
        {
            if (!string.IsNullOrWhiteSpace(discoveredKey))
                keys.Add(discoveredKey);
        }
    }

    private void HandleRiddleStageEvent(
        RiddleStageDefinition stageDefinition,
        TaskStageRuntimeState stageState,
        TaskHook hook,
        string eventName)
    {
        if (string.Equals(eventName, RiddleClueHook.InspectionClosedEventName, StringComparison.OrdinalIgnoreCase)
            && hook is RiddleClueHook clueHook)
        {
            DiscoverRiddleClue(stageDefinition, stageState, clueHook.HookId);
            return;
        }

        if (!string.Equals(eventName, RiddleAnswerHook.SelectedEventName, StringComparison.OrdinalIgnoreCase))
            return;

        if (hook is not RiddleAnswerHook answerHook)
            return;

        if (!string.Equals(answerHook.HookId, stageDefinition.CorrectHookId, StringComparison.OrdinalIgnoreCase))
        {
            if (!IsRiddleCandidate(stageDefinition, answerHook.HookId))
                return;

            if (stageDefinition.ApplyThreatOnWrongAnswer)
                ApplyWrongAnswerThreat(stageDefinition.ThreatOnWrongAnswer);

            return;
        }

        stageState.Completed = true;
        TryAdvanceGroup();
    }

    private static bool IsRiddleCandidate(RiddleStageDefinition stageDefinition, string hookId)
    {
        if (stageDefinition == null || string.IsNullOrWhiteSpace(hookId))
            return false;

        if (string.Equals(stageDefinition.CorrectHookId, hookId, StringComparison.OrdinalIgnoreCase))
            return true;

        if (stageDefinition.CandidateHookIds == null)
            return false;

        foreach (var candidateHookId in stageDefinition.CandidateHookIds)
        {
            if (string.Equals(candidateHookId, hookId, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static void DiscoverRiddleClue(
        RiddleStageDefinition stageDefinition,
        TaskStageRuntimeState stageState,
        string hookId)
    {
        if (stageDefinition?.AdditionalClues == null || stageState?.DiscoveredDetailKeys == null || string.IsNullOrWhiteSpace(hookId))
            return;

        foreach (var clue in stageDefinition.AdditionalClues)
        {
            if (clue == null || !string.Equals(clue.HookId, hookId, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrWhiteSpace(clue.ClueKey) && !stageState.DiscoveredDetailKeys.Contains(clue.ClueKey))
                stageState.DiscoveredDetailKeys.Add(clue.ClueKey);

            return;
        }
    }

    private void HandleRestoreStageEvent(
        RestoreStageDefinition stageDefinition,
        TaskStageRuntimeState stageState,
        TaskHook hook,
        string eventName)
    {
        if (!string.Equals(eventName, InteractionTaskHook.InteractedEventName, StringComparison.OrdinalIgnoreCase))
            return;

        if (hook is not InteractionTaskHook interactionHook)
            return;

        int requiredIndex = GetRestoreRequiredIndex(stageDefinition, interactionHook.HookId);
        if (requiredIndex < 0)
            return;

        if (stageState.CompletedHookIds.Contains(interactionHook.HookId))
            return;

        int nextExpectedIndex = stageState.CompletedHookIds.Count;
        if (stageDefinition.EnforceSequence && requiredIndex != nextExpectedIndex)
        {
            interactionHook.NotifyStepFailed();
            return;
        }

        stageState.CompletedHookIds.Add(interactionHook.HookId);
        stageState.Progress = stageDefinition.RequiredPoints != null && stageDefinition.RequiredPoints.Count > 0
            ? (float)stageState.CompletedHookIds.Count / stageDefinition.RequiredPoints.Count
            : 1f;
        interactionHook.NotifyStepSucceeded();

        bool allPointsComplete = stageDefinition.RequiredPoints != null
            && stageDefinition.RequiredPoints.Count > 0
            && stageState.CompletedHookIds.Count >= stageDefinition.RequiredPoints.Count;

        if (!allPointsComplete)
            return;

        stageState.Completed = true;
        NotifyRestoreTaskCompleted(stageDefinition);

        TryAdvanceGroup();
    }

    private static int GetRestoreRequiredIndex(RestoreStageDefinition stageDefinition, string hookId)
    {
        if (stageDefinition?.RequiredPoints == null || string.IsNullOrWhiteSpace(hookId))
            return -1;

        for (int i = 0; i < stageDefinition.RequiredPoints.Count; i++)
        {
            var point = stageDefinition.RequiredPoints[i];
            if (point != null && string.Equals(point.HookId, hookId, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static void AppendRestoreDetailKeys(
        System.Collections.Generic.List<string> keys,
        RestoreStageDefinition stageDefinition,
        TaskStageRuntimeState stageState)
    {
        if (stageDefinition?.RequiredPoints == null)
            return;

        foreach (var point in stageDefinition.RequiredPoints)
        {
            if (point == null || string.IsNullOrWhiteSpace(point.DetailKey))
                continue;

            if (stageState.CompletedHookIds != null && stageState.CompletedHookIds.Contains(point.HookId))
                continue;

            keys.Add(point.DetailKey);
        }
    }

    private void NotifyRestoreTaskCompleted(RestoreStageDefinition stageDefinition)
    {
        if (stageDefinition?.RequiredPoints == null)
            return;

        foreach (var point in stageDefinition.RequiredPoints)
        {
            if (point == null || string.IsNullOrWhiteSpace(point.HookId))
                continue;

            var hook = GetBoundHook<InteractionTaskHook>(point.HookId);
            hook?.NotifyTaskCompleted();
        }
    }

    private void HandleCleanseStageEvent(
        CleanseStageDefinition stageDefinition,
        TaskStageRuntimeState stageState,
        TaskHook hook,
        string eventName)
    {
        if (!string.Equals(eventName, CleanseTriggerHook.TriggeredEventName, StringComparison.OrdinalIgnoreCase))
            return;

        if (hook is not CleanseTriggerHook triggerHook)
            return;

        if (!string.Equals(triggerHook.HookId, stageDefinition.TriggerHookId, StringComparison.OrdinalIgnoreCase))
            return;

        if (Definition == null || RuntimeState == null)
            return;

        if (TaskManager.Instance != null && TaskManager.Instance.TryStartCleanseStage(Definition, RuntimeState, stageDefinition, stageState))
        {
            stageState.Activated = true;
            stageState.Progress = 0.01f;
        }
    }

    private void HandleHoldStageTick(
        HoldStageDefinition stageDefinition,
        TaskStageRuntimeState stageState,
        float deltaTime)
    {
        if (!stageState.Activated)
            return;

        bool conditionsMet = EvaluateHoldConditions(stageDefinition);
        float requiredSeconds = Math.Max(0.01f, stageDefinition.RequiredSeconds);

        if (conditionsMet)
        {
            stageState.Progress += deltaTime / requiredSeconds;
        }
        else if (stageDefinition.AllowProgressDecay && stageDefinition.DecayPerSecond > 0f)
        {
            stageState.Progress -= stageDefinition.DecayPerSecond * deltaTime / requiredSeconds;
        }

        stageState.Progress = Math.Clamp(stageState.Progress, 0f, 1f);

        if (stageState.Progress < 1f)
            return;

        stageState.Completed = true;
        NotifyHoldTaskCompleted(stageDefinition);
        TryAdvanceGroup();
    }

    private static bool EvaluateHoldConditions(HoldStageDefinition stageDefinition)
    {
        if (stageDefinition?.Conditions == null || stageDefinition.Conditions.Count == 0 || TaskManager.Instance == null)
            return false;

        bool anyMatched = false;
        bool allMatched = true;

        foreach (var condition in stageDefinition.Conditions)
        {
            if (condition == null || string.IsNullOrWhiteSpace(condition.SourceId))
            {
                allMatched = false;
                continue;
            }

            var source = TaskManager.Instance.GetHoldConditionSource(condition.SourceId);
            bool matched = source != null && source.Evaluate(condition);
            anyMatched |= matched;
            allMatched &= matched;
        }

        return stageDefinition.ConditionMode == HoldConditionMode.AnyConditionMet
            ? anyMatched
            : allMatched;
    }

    private void HandleHoldStageEvent(
        HoldStageDefinition stageDefinition,
        TaskStageRuntimeState stageState,
        TaskHook hook,
        string eventName)
    {
        if (stageState.Activated)
            return;

        if (!string.Equals(eventName, InteractionTaskHook.InteractedEventName, StringComparison.OrdinalIgnoreCase))
            return;

        if (hook is not InteractionTaskHook interactionHook)
            return;

        if (!string.Equals(interactionHook.HookId, stageDefinition.ActivationHookId, StringComparison.OrdinalIgnoreCase))
            return;

        stageState.Activated = true;
        stageState.Progress = Math.Max(stageState.Progress, 0.01f);
    }

    private void HandleDeliverStageEvent(
        DeliverStageDefinition stageDefinition,
        TaskStageRuntimeState stageState,
        TaskHook hook,
        string eventName)
    {
        if (stageDefinition?.HeldItem == null)
            return;

        if (hook is DeliverPickupHook pickupHook
            && string.Equals(eventName, DeliverPickupHook.PickedUpEventName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(pickupHook.HookId, stageDefinition.PickupHookId, StringComparison.OrdinalIgnoreCase))
        {
            HandleDeliverPickup(stageDefinition, stageState, pickupHook);
            return;
        }

        if (hook is not DeliverDepositHook deliveryHook
            || !string.Equals(eventName, DeliverDepositHook.AttemptedEventName, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(deliveryHook.HookId, stageDefinition.DeliveryHookId, StringComparison.OrdinalIgnoreCase))
            return;

        HandleDeliverAttempt(stageDefinition, stageState, deliveryHook);
    }

    private void HandleDeliverPickup(
        DeliverStageDefinition stageDefinition,
        TaskStageRuntimeState stageState,
        DeliverPickupHook pickupHook)
    {
        var inventory = InventoryManager.Instance;
        if (inventory == null)
            return;

        inventory.AddItem(stageDefinition.HeldItem.ItemId, setActiveIfNone: true);
        if (stageState.DeliverItemConditionStates != null && stageState.DeliverItemConditionStates.Count > 0)
            inventory.ApplyConditionStates(stageDefinition.HeldItem.ItemId, stageState.DeliverItemConditionStates);
        inventory.SetActiveItem(stageDefinition.HeldItem.ItemId);
        inventory.BeginForcedDeliveryCarry(stageDefinition.HeldItem.ItemId, RuntimeState.TaskInstanceId, stageState.StageId);
        ApplyHeldItemMutations(inventory, stageDefinition.HeldItem.ItemId, stageDefinition.OnPickupConditionMutations);
        stageState.Activated = true;
        stageState.IsDeliverCarried = true;
        stageState.HasDroppedDeliverPickup = false;
        stageState.Progress = 0.5f;
        TaskManager.Instance?.RemoveRuntimeDeliverPickup(stageState.StageId);

        if (pickupHook != null)
            pickupHook.gameObject.SetActive(false);
    }

    private void HandleDeliverAttempt(
        DeliverStageDefinition stageDefinition,
        TaskStageRuntimeState stageState,
        DeliverDepositHook deliveryHook)
    {
        var inventory = InventoryManager.Instance;
        if (inventory == null)
            return;

        bool valid = inventory.HasActiveItem(stageDefinition.HeldItem.ItemId);
        if (valid && !string.IsNullOrWhiteSpace(stageDefinition.RequiredItemConditionId))
        {
            valid = inventory.GetCondition(stageDefinition.HeldItem.ItemId, stageDefinition.RequiredItemConditionId);
        }

        if (!valid)
        {
            if (stageDefinition.ApplyThreatOnInvalidDelivery)
                ApplyWrongAnswerThreat(stageDefinition.ThreatOnInvalidDelivery);

            deliveryHook.NotifyInvalidDelivery();
            return;
        }

        ApplyHeldItemMutations(inventory, stageDefinition.HeldItem.ItemId, stageDefinition.OnDeliverConditionMutations);
        inventory.RemoveItem(stageDefinition.HeldItem.ItemId);
        inventory.ClearForcedDeliveryCarry(RuntimeState.TaskInstanceId, stageState.StageId);
        stageState.IsDeliverCarried = false;
        stageState.HasDroppedDeliverPickup = false;
        stageState.DeliverItemConditionStates.Clear();
        stageState.Completed = true;
        stageState.Progress = 1f;
        deliveryHook.NotifyDelivered();
        TryAdvanceGroup();
    }

    private static void ApplyHeldItemMutations(
        InventoryManager inventory,
        string itemId,
        System.Collections.Generic.IEnumerable<HeldItemConditionMutationDefinition> mutations)
    {
        if (inventory == null || string.IsNullOrWhiteSpace(itemId) || mutations == null)
            return;

        foreach (var mutation in mutations)
        {
            if (mutation == null || string.IsNullOrWhiteSpace(mutation.ConditionId))
                continue;

            inventory.SetCondition(itemId, mutation.ConditionId, mutation.Value);
        }
    }

    private void NotifyHoldTaskCompleted(HoldStageDefinition stageDefinition)
    {
        if (stageDefinition == null || string.IsNullOrWhiteSpace(stageDefinition.ActivationHookId))
            return;

        var hook = GetBoundHook<InteractionTaskHook>(stageDefinition.ActivationHookId);
        hook?.NotifyTaskCompleted();
    }

    private static void AppendHoldDetails(
        System.Collections.Generic.List<TaskListDetailViewData> details,
        HoldStageDefinition holdStage)
    {
        if (details == null || holdStage?.Conditions == null)
            return;

        foreach (var condition in holdStage.Conditions)
        {
            if (condition == null || string.IsNullOrWhiteSpace(condition.DetailKey))
                continue;

            bool satisfied = EvaluateHoldCondition(condition);
            details.Add(new TaskListDetailViewData
            {
                Key = satisfied && !string.IsNullOrWhiteSpace(condition.SatisfiedDetailKey)
                    ? condition.SatisfiedDetailKey
                    : condition.DetailKey,
                IsSatisfied = satisfied,
            });
        }
    }

    private static bool EvaluateHoldCondition(HoldConditionRequirementDefinition condition)
    {
        if (condition == null || string.IsNullOrWhiteSpace(condition.SourceId) || TaskManager.Instance == null)
            return false;

        var source = TaskManager.Instance.GetHoldConditionSource(condition.SourceId);
        return source != null && source.Evaluate(condition);
    }

    private TaskStageDefinition GetStageDefinition(TaskStageRuntimeState stageState)
    {
        if (Definition?.StageGroups == null || stageState == null)
            return null;

        if (stageState.GroupIndex < 0 || stageState.GroupIndex >= Definition.StageGroups.Count)
            return null;

        var group = Definition.StageGroups[stageState.GroupIndex];
        if (group?.Stages == null || stageState.StageIndex < 0 || stageState.StageIndex >= group.Stages.Count)
            return null;

        return group.Stages[stageState.StageIndex];
    }

    private static System.Collections.Generic.List<string> GetManagedDeliverConditionIds(DeliverStageDefinition stageDefinition)
    {
        var ids = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectMutationIds(stageDefinition?.OnPickupConditionMutations, ids);
        CollectMutationIds(stageDefinition?.OnDropConditionMutations, ids);
        CollectMutationIds(stageDefinition?.OnDeliverConditionMutations, ids);

        if (!string.IsNullOrWhiteSpace(stageDefinition?.RequiredItemConditionId))
            ids.Add(stageDefinition.RequiredItemConditionId);

        return new System.Collections.Generic.List<string>(ids);
    }

    private static void CollectMutationIds(
        System.Collections.Generic.IEnumerable<HeldItemConditionMutationDefinition> mutations,
        System.Collections.Generic.HashSet<string> ids)
    {
        if (mutations == null || ids == null)
            return;

        foreach (var mutation in mutations)
        {
            if (mutation == null || string.IsNullOrWhiteSpace(mutation.ConditionId))
                continue;

            ids.Add(mutation.ConditionId);
        }
    }
}
