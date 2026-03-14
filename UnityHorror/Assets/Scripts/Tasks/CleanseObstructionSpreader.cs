using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CleanseObstructionSpreader : MonoBehaviour
{
    private readonly Dictionary<string, CleanseObstructionInteractable> liveInstances =
        new Dictionary<string, CleanseObstructionInteractable>(System.StringComparer.OrdinalIgnoreCase);

    private CleanseStageDefinition definition;
    private CleanseObstructionGroupState state;
    private bool settledNotified;

    public string GroupId => state != null ? state.GroupId : string.Empty;
    public bool IsResolved => state != null && state.Resolved;
    public bool ShouldPersistAcrossNights => state != null && state.PersistAcrossNights;
    public bool HasSettledBadly => state != null && state.Settled;

    public void Initialize(CleanseStageDefinition definition, CleanseObstructionGroupState state)
    {
        this.definition = definition;
        this.state = state;
        name = $"CleanseSpreader_{state.GroupId}";
        RebuildFromState();
    }

    public void Tick(float deltaTime)
    {
        if (definition == null || state == null || state.Resolved)
            return;

        state.Triggered = true;
        state.ElapsedSeconds += deltaTime;
        state.SpawnTimerSeconds += deltaTime;

        TrySpawnPending();

        if (AllInstancesCleared() && state.SpawnedCount >= Mathf.Max(0, definition.TotalSpawnCount))
        {
            ResolveCleanly();
            return;
        }

        if (state.ElapsedSeconds >= Mathf.Max(0.01f, definition.SpreadDurationSeconds))
            Settle();
    }

    public void TryClearInstance(string instanceId)
    {
        if (state == null || string.IsNullOrWhiteSpace(instanceId) || state.Settled || state.Resolved)
            return;

        var instanceState = state.Instances.Find(i => i != null && i.InstanceId == instanceId);
        if (instanceState == null || instanceState.Cleared)
            return;

        instanceState.Cleared = true;
        if (liveInstances.TryGetValue(instanceId, out var interactable) && interactable != null)
            interactable.NotifyCleared();

        if (AllInstancesCleared())
        {
            if (state.SpawnedCount >= Mathf.Max(0, definition.TotalSpawnCount))
                ResolveCleanly();
        }
    }

    public void DestroyRuntime()
    {
        foreach (var instance in liveInstances.Values)
        {
            if (instance != null)
                Destroy(instance.gameObject);
        }

        liveInstances.Clear();
        Destroy(gameObject);
    }

    private void RebuildFromState()
    {
        liveInstances.Clear();
        if (state == null)
            return;

        foreach (var instanceState in state.Instances)
        {
            if (instanceState == null || instanceState.Cleared)
                continue;

            SpawnRuntimeInstance(instanceState);
        }

        if (state.Settled)
            NotifySettledOnLiveInstances();
    }

    private void TrySpawnPending()
    {
        if (state.SpawnedCount >= Mathf.Max(0, definition.TotalSpawnCount))
            return;

        while (state.SpawnTimerSeconds >= Mathf.Max(0.01f, definition.SpawnIntervalSeconds))
        {
            state.SpawnTimerSeconds -= Mathf.Max(0.01f, definition.SpawnIntervalSeconds);

            if (GetActiveInstanceCount() >= Mathf.Max(1, definition.MaxActiveObstructions))
                return;

            if (!TryFindSpawnPosition(out var spawnPosition))
                continue;

            var instanceState = new CleanseObstructionInstanceState
            {
                InstanceId = $"{state.GroupId}.instance.{state.NextInstanceIndex++}",
                Position = spawnPosition,
                Cleared = false,
            };

            state.Instances.Add(instanceState);
            state.SpawnedCount++;
            SpawnRuntimeInstance(instanceState);

            if (state.SpawnedCount >= Mathf.Max(0, definition.TotalSpawnCount))
                return;
        }
    }

    private void SpawnRuntimeInstance(CleanseObstructionInstanceState instanceState)
    {
        if (instanceState == null || string.IsNullOrWhiteSpace(definition.ObstructionResourcePath))
            return;

        var obstructionPrefab = ResourceCache.Get<GameObject>(definition.ObstructionResourcePath);
        if (obstructionPrefab == null)
            return;

        var spawned = Instantiate(obstructionPrefab, instanceState.Position, Quaternion.identity, transform);
        var interactable = spawned.GetComponentInChildren<CleanseObstructionInteractable>();
        if (interactable == null)
            interactable = spawned.GetComponent<CleanseObstructionInteractable>();

        if (interactable == null)
        {
            Destroy(spawned);
            return;
        }

        interactable.Initialize(
            this,
            instanceState.InstanceId,
            definition.ObstructionInteractionMode,
            definition.ObstructionHoldDurationSeconds,
            definition.ResetObstructionHoldOnCancel);

        if (state.Settled)
            interactable.NotifySettled();

        liveInstances[instanceState.InstanceId] = interactable;
    }

    private bool TryFindSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = state.SpawnOrigin;
        int attempts = Mathf.Max(1, definition.PlacementAttemptsPerSpawn);

        var player = FindFirstObjectByType<PlayerController>();
        Vector3 playerPosition = player != null ? player.transform.position : state.SpawnOrigin;

        for (int i = 0; i < attempts; i++)
        {
            var offset = Random.insideUnitCircle * Mathf.Max(0.1f, definition.SpawnRadius);
            var candidate = state.SpawnOrigin + new Vector3(offset.x, 4f, offset.y);

            if (!Physics.Raycast(candidate, Vector3.down, out var hit, 12f, ~0, QueryTriggerInteraction.Ignore))
                continue;

            if (!NavMesh.SamplePosition(hit.point, out var sample, 2f, NavMesh.AllAreas))
                continue;

            if (!IsReachableFromPlayer(playerPosition, sample.position))
                continue;

            spawnPosition = sample.position;
            return true;
        }

        return false;
    }

    private static bool IsReachableFromPlayer(Vector3 playerPosition, Vector3 targetPosition)
    {
        if (!NavMesh.SamplePosition(playerPosition, out var playerHit, 4f, NavMesh.AllAreas))
            return true;

        if (!NavMesh.SamplePosition(targetPosition, out var targetHit, 4f, NavMesh.AllAreas))
            return false;

        var path = new NavMeshPath();
        if (!NavMesh.CalculatePath(playerHit.position, targetHit.position, NavMesh.AllAreas, path))
            return false;

        return path.status == NavMeshPathStatus.PathComplete;
    }

    private int GetActiveInstanceCount()
    {
        int count = 0;
        foreach (var instance in state.Instances)
        {
            if (instance != null && !instance.Cleared)
                count++;
        }

        return count;
    }

    private bool AllInstancesCleared()
    {
        foreach (var instance in state.Instances)
        {
            if (instance != null && !instance.Cleared)
                return false;
        }

        return true;
    }

    private void ResolveCleanly()
    {
        state.Resolved = true;
        TaskManager.Instance?.NotifyCleanseGroupResolved(state.GroupId, false);
        DestroyRuntime();
    }

    private void Settle()
    {
        if (state.Settled)
            return;

        state.Settled = true;
        state.Resolved = true;
        NotifySettledOnLiveInstances();
        TaskManager.Instance?.NotifyCleanseGroupResolved(state.GroupId, true);
    }

    private void NotifySettledOnLiveInstances()
    {
        if (settledNotified)
            return;

        settledNotified = true;
        foreach (var interactable in liveInstances.Values)
        {
            if (interactable != null)
                interactable.NotifySettled();
        }
    }
}
