using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryManager : MonoBehaviour, IGameSaveParticipant
{
    public static InventoryManager Instance { get; private set; }

    private readonly Dictionary<string, HeldItemDefinition> definitionsById =
        new Dictionary<string, HeldItemDefinition>(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        BuildDefinitionCache();
        EnsureInventoryState();
    }

    public bool HasItem(string itemId)
    {
        return GetItemState(itemId) != null;
    }

    public bool HasActiveItem(string itemId)
    {
        var state = EnsureInventoryState();
        return !string.IsNullOrWhiteSpace(itemId)
            && string.Equals(state.ActiveItemId, itemId, StringComparison.OrdinalIgnoreCase);
    }

    public string GetActiveItemId()
    {
        return EnsureInventoryState().ActiveItemId;
    }

    public IReadOnlyList<HeldItemRuntimeState> GetItems()
    {
        return EnsureInventoryState().Items;
    }

    public HeldItemRuntimeState GetItemState(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        return EnsureInventoryState().Items.FirstOrDefault(i =>
            i != null && string.Equals(i.ItemId, itemId, StringComparison.OrdinalIgnoreCase));
    }

    public bool GetCondition(string itemId, string conditionId)
    {
        var item = GetItemState(itemId);
        if (item?.Conditions == null || string.IsNullOrWhiteSpace(conditionId))
            return false;

        var condition = item.Conditions.FirstOrDefault(c =>
            c != null && string.Equals(c.ConditionId, conditionId, StringComparison.OrdinalIgnoreCase));

        return condition != null && condition.Value;
    }

    public bool AddItem(string itemId, bool setActiveIfNone = false)
    {
        if (string.IsNullOrWhiteSpace(itemId) || HasItem(itemId))
            return false;

        var state = EnsureInventoryState();
        var runtimeState = new HeldItemRuntimeState
        {
            ItemId = itemId,
            Kind = GetItemKind(itemId),
        };

        state.Items.Add(runtimeState);

        if (setActiveIfNone || string.IsNullOrWhiteSpace(state.ActiveItemId))
            state.ActiveItemId = itemId;

        return true;
    }

    public bool RemoveItem(string itemId)
    {
        var state = EnsureInventoryState();
        int removed = state.Items.RemoveAll(i =>
            i != null && string.Equals(i.ItemId, itemId, StringComparison.OrdinalIgnoreCase));

        if (removed == 0)
            return false;

        if (string.Equals(state.ActiveItemId, itemId, StringComparison.OrdinalIgnoreCase))
            state.ActiveItemId = state.Items.Count > 0 ? state.Items[0].ItemId : null;

        return true;
    }

    public bool SetActiveItem(string itemId)
    {
        if (!HasItem(itemId))
            return false;

        EnsureInventoryState().ActiveItemId = itemId;
        return true;
    }

    public bool SetCondition(string itemId, string conditionId, bool value)
    {
        var item = GetItemState(itemId);
        if (item == null || string.IsNullOrWhiteSpace(conditionId))
            return false;

        var condition = item.Conditions.FirstOrDefault(c =>
            c != null && string.Equals(c.ConditionId, conditionId, StringComparison.OrdinalIgnoreCase));

        if (condition == null)
        {
            condition = new HeldItemConditionState
            {
                ConditionId = conditionId,
                Value = value,
            };
            item.Conditions.Add(condition);
            return true;
        }

        condition.Value = value;
        return true;
    }

    public void ClearInventory()
    {
        var state = EnsureInventoryState();
        state.Items.Clear();
        state.ActiveItemId = null;
    }

    public List<HeldItemConditionState> CopyConditionStates(string itemId, IEnumerable<string> conditionIds = null)
    {
        var item = GetItemState(itemId);
        if (item?.Conditions == null)
            return new List<HeldItemConditionState>();

        HashSet<string> filter = null;
        if (conditionIds != null)
        {
            filter = new HashSet<string>(
                conditionIds.Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);
        }

        var result = new List<HeldItemConditionState>();
        foreach (var condition in item.Conditions)
        {
            if (condition == null || string.IsNullOrWhiteSpace(condition.ConditionId))
                continue;

            if (filter != null && !filter.Contains(condition.ConditionId))
                continue;

            result.Add(new HeldItemConditionState
            {
                ConditionId = condition.ConditionId,
                Value = condition.Value,
            });
        }

        return result;
    }

    public void ApplyConditionStates(string itemId, IEnumerable<HeldItemConditionState> conditionStates)
    {
        if (string.IsNullOrWhiteSpace(itemId) || conditionStates == null)
            return;

        foreach (var condition in conditionStates)
        {
            if (condition == null || string.IsNullOrWhiteSpace(condition.ConditionId))
                continue;

            SetCondition(itemId, condition.ConditionId, condition.Value);
        }
    }

    public void OnBeforeGameSaved(GameState state)
    {
        EnsureInventoryState();
    }

    public void OnAfterGameLoaded(GameState state)
    {
        EnsureInventoryState();
    }

    private void BuildDefinitionCache()
    {
        definitionsById.Clear();
        foreach (var definition in Resources.LoadAll<HeldItemDefinition>("HeldItems"))
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.ItemId))
                continue;

            definitionsById[definition.ItemId] = definition;
        }
    }

    private InventoryState EnsureInventoryState()
    {
        if (Game.State == null)
            return new InventoryState();

        Game.State.EnsureInitialized();

        if (Game.State.Inventory == null)
            Game.State.Inventory = new InventoryState();

        if (Game.State.Inventory.Items == null)
            Game.State.Inventory.Items = new List<HeldItemRuntimeState>();

        return Game.State.Inventory;
    }

    private HeldItemKind GetItemKind(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return HeldItemKind.Passive;

        return definitionsById.TryGetValue(itemId, out var definition)
            ? definition.Kind
            : HeldItemKind.Passive;
    }
}
