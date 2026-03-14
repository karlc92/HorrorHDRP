using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SampleTaskContentGenerator
{
    private const string TasksRoot = "Assets/Resources/Tasks/Samples";
    private const string SingleRoot = TasksRoot + "/SingleStage";
    private const string MultiRoot = TasksRoot + "/MultiStage";
    private const string HeldItemsRoot = "Assets/Resources/HeldItems/Samples";
    private const string PrefabsRoot = "Assets/Resources/Prefabs/Tasks";
    private const string LocalizationRoot = "Assets/Resources/Localization";
    private const string EnglishLocalizationPath = LocalizationRoot + "/English.json";
    private const string SampleObstructionPrefabPath = PrefabsRoot + "/SampleCleanseObstruction.prefab";

    [MenuItem("Tools/Horror/Generate Sample Task Content")]
    public static void GenerateAll()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Tasks");
        EnsureFolder(TasksRoot);
        EnsureFolder(SingleRoot);
        EnsureFolder(MultiRoot);
        EnsureFolder("Assets/Resources/HeldItems");
        EnsureFolder(HeldItemsRoot);
        EnsureFolder("Assets/Resources/Prefabs");
        EnsureFolder(PrefabsRoot);
        EnsureFolder(LocalizationRoot);

        AssetDatabase.StartAssetEditing();
        try
        {
            var heldItems = CreateHeldItems();
            CreateSampleObstructionPrefab();

            CreateSingleStageTasks(heldItems);
            CreateMultiStageTasks(heldItems);

            UpdateLocalization();
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log("Sample task content generated.");
    }

    public static void GenerateAllBatch()
    {
        GenerateAll();
        EditorApplication.Exit(0);
    }

    public static void BatchPing()
    {
        File.WriteAllText("Assets/Editor/batch_ping.txt", "ping");
        AssetDatabase.Refresh();
        EditorApplication.Exit(0);
    }

    private static Dictionary<string, HeldItemDefinition> CreateHeldItems()
    {
        var items = new Dictionary<string, HeldItemDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["sample.sanctified_candle"] = CreateHeldItem("HeldItem_SanctifiedCandle", "sample.sanctified_candle", HeldItemKind.Usable),
            ["sample.bone_casket"] = CreateHeldItem("HeldItem_BoneCasket", "sample.bone_casket", HeldItemKind.Passive),
            ["sample.prayer_book"] = CreateHeldItem("HeldItem_PrayerBook", "sample.prayer_book", HeldItemKind.Usable),
            ["sample.consecrated_oil"] = CreateHeldItem("HeldItem_ConsecratedOil", "sample.consecrated_oil", HeldItemKind.Passive),
            ["sample.funeral_lantern"] = CreateHeldItem("HeldItem_FuneralLantern", "sample.funeral_lantern", HeldItemKind.Usable),
        };

        return items;
    }

    private static HeldItemDefinition CreateHeldItem(string assetName, string itemId, HeldItemKind kind)
    {
        string path = $"{HeldItemsRoot}/{assetName}.asset";
        var item = LoadOrCreateAsset<HeldItemDefinition>(path);
        item.ItemId = itemId;
        item.Kind = kind;
        EditorUtility.SetDirty(item);
        return item;
    }

    private static void CreateSampleObstructionPrefab()
    {
        var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "SampleCleanseObstruction";
        root.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

        var renderer = root.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("HDRP/Lit"));
            material.color = new Color(0.23f, 0.34f, 0.18f);
            renderer.sharedMaterial = material;
        }

        root.AddComponent<CleanseObstructionInteractable>();
        PrefabUtility.SaveAsPrefabAsset(root, SampleObstructionPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateSingleStageTasks(Dictionary<string, HeldItemDefinition> heldItems)
    {
        CreateTask(
            $"{SingleRoot}/Task_Riddle_FalseMarker.asset",
            "sample.false_marker",
            1,
            new[] { "graveyard" },
            groups =>
            {
                AddSequentialGroup(groups, "identify_false_marker", stage =>
                {
                    var riddle = stage as RiddleStageDefinition;
                    riddle.StageId = "identify_false_marker";
                    riddle.Archetype = TaskStageArchetype.Riddle;
                    riddle.DetailKeyOverride = "task.sample.false_marker.group.0.stage.0";
                    riddle.CorrectHookId = "grave.false_marker.correct";
                    riddle.CandidateHookIds = new List<string>
                    {
                        "grave.false_marker.correct",
                        "grave.false_marker.candidate_north",
                        "grave.false_marker.candidate_ivy",
                    };
                    riddle.ThreatOnWrongAnswer = 8;
                    riddle.AdditionalClues = new List<RiddleClueDefinition>
                    {
                        new() { HookId = "grave.false_marker.epitaph", ClueKey = "task.sample.false_marker.clue.epitaph" },
                        new() { HookId = "grave.false_marker.bench_note", ClueKey = "task.sample.false_marker.clue.note" },
                    };
                });
            });

        CreateTask(
            $"{SingleRoot}/Task_Restore_MemorialCandles.asset",
            "sample.memorial_candles",
            1,
            new[] { "graveyard" },
            groups =>
            {
                AddSequentialGroup(groups, "restore_memorial_candles", stage =>
                {
                    var restore = stage as RestoreStageDefinition;
                    restore.StageId = "restore_memorial_candles";
                    restore.Archetype = TaskStageArchetype.Restore;
                    restore.DetailKeyOverride = "task.sample.memorial_candles.group.0.stage.0";
                    restore.EnforceSequence = false;
                    restore.RequiredPoints = new List<RestorePointDefinition>
                    {
                        new() { HookId = "restore.memorial_candles.north", DetailKey = "task.sample.memorial_candles.point.north" },
                        new() { HookId = "restore.memorial_candles.center", DetailKey = "task.sample.memorial_candles.point.center" },
                        new() { HookId = "restore.memorial_candles.south", DetailKey = "task.sample.memorial_candles.point.south" },
                    };
                });
            });

        CreateTask(
            $"{SingleRoot}/Task_Cleanse_RootedBench.asset",
            "sample.rooted_bench",
            2,
            new[] { "chapel" },
            groups =>
            {
                AddSequentialGroup(groups, "cleanse_rooted_bench", stage =>
                {
                    var cleanse = stage as CleanseStageDefinition;
                    cleanse.StageId = "cleanse_rooted_bench";
                    cleanse.Archetype = TaskStageArchetype.Cleanse;
                    cleanse.DetailKeyOverride = "task.sample.rooted_bench.group.0.stage.0";
                    cleanse.TriggerHookId = "cleanse.rooted_bench.trigger";
                    cleanse.SpawnOriginHookId = "cleanse.rooted_bench.origin";
                    cleanse.ObstructionResourcePath = "Prefabs/Tasks/SampleCleanseObstruction";
                    cleanse.TriggerInteractionMode = InteractionMode.Press;
                    cleanse.ObstructionInteractionMode = InteractionMode.Hold;
                    cleanse.ObstructionHoldDurationSeconds = 1.2f;
                    cleanse.SpreadDurationSeconds = 18f;
                    cleanse.SpawnIntervalSeconds = 0.8f;
                    cleanse.MaxActiveObstructions = 4;
                    cleanse.TotalSpawnCount = 6;
                    cleanse.SpawnRadius = 4f;
                });
            });

        CreateTask(
            $"{SingleRoot}/Task_Hold_PulpitWatch.asset",
            "sample.pulpit_watch",
            1,
            new[] { "chapel" },
            groups =>
            {
                AddSequentialGroup(groups, "hold_pulpit_watch", stage =>
                {
                    var hold = stage as HoldStageDefinition;
                    hold.StageId = "hold_pulpit_watch";
                    hold.Archetype = TaskStageArchetype.Hold;
                    hold.DetailKeyOverride = "task.sample.pulpit_watch.group.0.stage.0";
                    hold.ActivationHookId = "hold.pulpit_watch.start";
                    hold.RequiredSeconds = 25f;
                    hold.Conditions = new List<HoldConditionRequirementDefinition>
                    {
                        new()
                        {
                            SourceId = "hold.pulpit_watch.volume",
                            DetailKey = "task.sample.pulpit_watch.condition.volume",
                            SatisfiedDetailKey = "task.sample.pulpit_watch.condition.volume_done",
                        }
                    };
                });
            });

        CreateTask(
            $"{SingleRoot}/Task_Deliver_SanctifiedCandle.asset",
            "sample.sanctified_candle_delivery",
            1,
            new[] { "chapel" },
            groups =>
            {
                AddSequentialGroup(groups, "deliver_sanctified_candle", stage =>
                {
                    var deliver = stage as DeliverStageDefinition;
                    deliver.StageId = "deliver_sanctified_candle";
                    deliver.Archetype = TaskStageArchetype.Deliver;
                    deliver.DetailKeyOverride = "task.sample.sanctified_candle_delivery.group.0.stage.0";
                    deliver.HeldItem = heldItems["sample.sanctified_candle"];
                    deliver.PickupHookId = "deliver.sanctified_candle.pickup";
                    deliver.DeliveryHookId = "deliver.sanctified_candle.altar";
                    deliver.RequiredItemConditionId = "lit";
                    deliver.AllowSprint = false;
                    deliver.AllowCrouch = true;
                    deliver.OnPickupConditionMutations = new List<HeldItemConditionMutationDefinition>
                    {
                        new() { ConditionId = "lit", Value = true },
                    };
                    deliver.OnDropConditionMutations = new List<HeldItemConditionMutationDefinition>
                    {
                        new() { ConditionId = "lit", Value = false },
                    };
                });
            });
    }

    private static void CreateMultiStageTasks(Dictionary<string, HeldItemDefinition> heldItems)
    {
        CreateTask(
            $"{MultiRoot}/Task_PenitentGrave.asset",
            "sample.penitent_grave",
            3,
            new[] { "graveyard", "chapel" },
            groups =>
            {
                AddSequentialGroup(groups, "identify_grave", stage =>
                {
                    var riddle = stage as RiddleStageDefinition;
                    riddle.StageId = "identify_grave";
                    riddle.Archetype = TaskStageArchetype.Riddle;
                    riddle.DetailKeyOverride = "task.sample.penitent_grave.group.0.stage.0";
                    riddle.CorrectHookId = "grave.penitent.correct";
                    riddle.CandidateHookIds = new List<string>
                    {
                        "grave.penitent.correct",
                        "grave.penitent.moss",
                        "grave.penitent.broken",
                    };
                    riddle.ThreatOnWrongAnswer = 10;
                    riddle.AdditionalClues = new List<RiddleClueDefinition>
                    {
                        new() { HookId = "grave.penitent.epitaph", ClueKey = "task.sample.penitent_grave.clue.epitaph" },
                        new() { HookId = "grave.penitent.bench", ClueKey = "task.sample.penitent_grave.clue.bench" },
                    };
                });

                AddSequentialGroup(groups, "return_remains", stage =>
                {
                    var deliver = stage as DeliverStageDefinition;
                    deliver.StageId = "return_remains";
                    deliver.Archetype = TaskStageArchetype.Deliver;
                    deliver.DetailKeyOverride = "task.sample.penitent_grave.group.1.stage.0";
                    deliver.HeldItem = heldItems["sample.bone_casket"];
                    deliver.PickupHookId = "deliver.bone_casket.ossuary";
                    deliver.DeliveryHookId = "deliver.bone_casket.penitent_grave";
                    deliver.AllowSprint = false;
                    deliver.AllowCrouch = true;
                });

                AddSequentialGroup(groups, "keep_vigil", stage =>
                {
                    var hold = stage as HoldStageDefinition;
                    hold.StageId = "keep_vigil";
                    hold.Archetype = TaskStageArchetype.Hold;
                    hold.DetailKeyOverride = "task.sample.penitent_grave.group.2.stage.0";
                    hold.ActivationHookId = "hold.penitent_grave.start";
                    hold.RequiredSeconds = 20f;
                    hold.Conditions = new List<HoldConditionRequirementDefinition>
                    {
                        new()
                        {
                            SourceId = "hold.penitent_grave.volume",
                            DetailKey = "task.sample.penitent_grave.condition.volume",
                            SatisfiedDetailKey = "task.sample.penitent_grave.condition.volume_done",
                        }
                    };
                });
            });

        CreateTask(
            $"{MultiRoot}/Task_ChapelService.asset",
            "sample.chapel_service",
            4,
            new[] { "chapel" },
            groups =>
            {
                AddParallelGroup(groups, "prepare_service", stageDefs =>
                {
                    var deliver = CreateStage<DeliverStageDefinition>("deliver_prayer_book");
                    deliver.StageId = "deliver_prayer_book";
                    deliver.Archetype = TaskStageArchetype.Deliver;
                    deliver.DetailKeyOverride = "task.sample.chapel_service.group.0.stage.0";
                    deliver.HeldItem = heldItems["sample.prayer_book"];
                    deliver.PickupHookId = "deliver.prayer_book.bench";
                    deliver.DeliveryHookId = "deliver.prayer_book.pulpit";
                    deliver.AllowSprint = true;
                    deliver.AllowCrouch = true;

                    var restore = CreateStage<RestoreStageDefinition>("restore_candles");
                    restore.StageId = "restore_candles";
                    restore.Archetype = TaskStageArchetype.Restore;
                    restore.DetailKeyOverride = "task.sample.chapel_service.group.0.stage.1";
                    restore.EnforceSequence = false;
                    restore.RequiredPoints = new List<RestorePointDefinition>
                    {
                        new() { HookId = "restore.chapel_service.candle_left", DetailKey = "task.sample.chapel_service.point.left" },
                        new() { HookId = "restore.chapel_service.candle_right", DetailKey = "task.sample.chapel_service.point.right" },
                        new() { HookId = "restore.chapel_service.chandelier", DetailKey = "task.sample.chapel_service.point.chandelier" },
                    };

                    stageDefs.Add(deliver);
                    stageDefs.Add(restore);
                });

                AddSequentialGroup(groups, "read_service", stage =>
                {
                    var hold = stage as HoldStageDefinition;
                    hold.StageId = "read_service";
                    hold.Archetype = TaskStageArchetype.Hold;
                    hold.DetailKeyOverride = "task.sample.chapel_service.group.1.stage.0";
                    hold.ActivationHookId = "hold.chapel_service.start";
                    hold.RequiredSeconds = 30f;
                    hold.Conditions = new List<HoldConditionRequirementDefinition>
                    {
                        new()
                        {
                            SourceId = "hold.chapel_service.pulpit",
                            DetailKey = "task.sample.chapel_service.condition.pulpit",
                            SatisfiedDetailKey = "task.sample.chapel_service.condition.pulpit_done",
                        }
                    };
                });
            });

        CreateTask(
            $"{MultiRoot}/Task_RootedReliquary.asset",
            "sample.rooted_reliquary",
            4,
            new[] { "chapel", "graveyard" },
            groups =>
            {
                AddSequentialGroup(groups, "deliver_oil", stage =>
                {
                    var deliver = stage as DeliverStageDefinition;
                    deliver.StageId = "deliver_oil";
                    deliver.Archetype = TaskStageArchetype.Deliver;
                    deliver.DetailKeyOverride = "task.sample.rooted_reliquary.group.0.stage.0";
                    deliver.HeldItem = heldItems["sample.consecrated_oil"];
                    deliver.PickupHookId = "deliver.consecrated_oil.store";
                    deliver.DeliveryHookId = "deliver.consecrated_oil.rooted_reliquary";
                });

                AddSequentialGroup(groups, "cleanse_roots", stage =>
                {
                    var cleanse = stage as CleanseStageDefinition;
                    cleanse.StageId = "cleanse_roots";
                    cleanse.Archetype = TaskStageArchetype.Cleanse;
                    cleanse.DetailKeyOverride = "task.sample.rooted_reliquary.group.1.stage.0";
                    cleanse.TriggerHookId = "cleanse.rooted_reliquary.trigger";
                    cleanse.SpawnOriginHookId = "cleanse.rooted_reliquary.origin";
                    cleanse.ObstructionResourcePath = "Prefabs/Tasks/SampleCleanseObstruction";
                    cleanse.ObstructionInteractionMode = InteractionMode.Hold;
                    cleanse.ObstructionHoldDurationSeconds = 1.4f;
                    cleanse.SpreadDurationSeconds = 24f;
                    cleanse.SpawnIntervalSeconds = 0.7f;
                    cleanse.MaxActiveObstructions = 5;
                    cleanse.TotalSpawnCount = 8;
                    cleanse.SpawnRadius = 4.5f;
                });

                AddSequentialGroup(groups, "restore_reliquary_candles", stage =>
                {
                    var restore = stage as RestoreStageDefinition;
                    restore.StageId = "restore_reliquary_candles";
                    restore.Archetype = TaskStageArchetype.Restore;
                    restore.DetailKeyOverride = "task.sample.rooted_reliquary.group.2.stage.0";
                    restore.EnforceSequence = true;
                    restore.RequiredPoints = new List<RestorePointDefinition>
                    {
                        new() { HookId = "restore.rooted_reliquary.candle_west", DetailKey = "task.sample.rooted_reliquary.point.west" },
                        new() { HookId = "restore.rooted_reliquary.candle_east", DetailKey = "task.sample.rooted_reliquary.point.east" },
                        new() { HookId = "restore.rooted_reliquary.shrine", DetailKey = "task.sample.rooted_reliquary.point.shrine" },
                    };
                });
            });

        CreateTask(
            $"{MultiRoot}/Task_LanternWake.asset",
            "sample.lantern_wake",
            5,
            new[] { "graveyard", "chapel" },
            groups =>
            {
                AddSequentialGroup(groups, "restore_wake_candles", stage =>
                {
                    var restore = stage as RestoreStageDefinition;
                    restore.StageId = "restore_wake_candles";
                    restore.Archetype = TaskStageArchetype.Restore;
                    restore.DetailKeyOverride = "task.sample.lantern_wake.group.0.stage.0";
                    restore.EnforceSequence = false;
                    restore.RequiredPoints = new List<RestorePointDefinition>
                    {
                        new() { HookId = "restore.lantern_wake.bench_left", DetailKey = "task.sample.lantern_wake.point.bench_left" },
                        new() { HookId = "restore.lantern_wake.bench_right", DetailKey = "task.sample.lantern_wake.point.bench_right" },
                    };
                });

                AddSequentialGroup(groups, "deliver_lantern", stage =>
                {
                    var deliver = stage as DeliverStageDefinition;
                    deliver.StageId = "deliver_lantern";
                    deliver.Archetype = TaskStageArchetype.Deliver;
                    deliver.DetailKeyOverride = "task.sample.lantern_wake.group.1.stage.0";
                    deliver.HeldItem = heldItems["sample.funeral_lantern"];
                    deliver.PickupHookId = "deliver.funeral_lantern.rack";
                    deliver.DeliveryHookId = "deliver.funeral_lantern.wake_grave";
                    deliver.RequiredItemConditionId = "lit";
                    deliver.AllowSprint = false;
                    deliver.OnPickupConditionMutations = new List<HeldItemConditionMutationDefinition>
                    {
                        new() { ConditionId = "lit", Value = true },
                    };
                    deliver.OnDropConditionMutations = new List<HeldItemConditionMutationDefinition>
                    {
                        new() { ConditionId = "lit", Value = false },
                    };
                    deliver.ApplyThreatOnDrop = true;
                    deliver.ThreatOnDrop = 8;
                });

                AddSequentialGroup(groups, "hold_final_prayer", stage =>
                {
                    var hold = stage as HoldStageDefinition;
                    hold.StageId = "hold_final_prayer";
                    hold.Archetype = TaskStageArchetype.Hold;
                    hold.DetailKeyOverride = "task.sample.lantern_wake.group.2.stage.0";
                    hold.ActivationHookId = "hold.lantern_wake.start";
                    hold.RequiredSeconds = 35f;
                    hold.AllowProgressDecay = true;
                    hold.DecayPerSecond = 6f;
                    hold.Conditions = new List<HoldConditionRequirementDefinition>
                    {
                        new()
                        {
                            SourceId = "hold.lantern_wake.volume",
                            DetailKey = "task.sample.lantern_wake.condition.volume",
                            SatisfiedDetailKey = "task.sample.lantern_wake.condition.volume_done",
                        },
                        new()
                        {
                            SourceId = "hold.lantern_wake.active_lantern",
                            RequiredItemId = "sample.funeral_lantern",
                            RequiredItemConditionId = "lit",
                            DetailKey = "task.sample.lantern_wake.condition.lantern",
                            SatisfiedDetailKey = "task.sample.lantern_wake.condition.lantern_done",
                        }
                    };
                });
            });
    }

    private static void UpdateLocalization()
    {
        var data = new LocalizationLanguageData
        {
            Language = "English",
            Entries = new List<LocalizationEntry>(),
        };

        if (File.Exists(EnglishLocalizationPath))
        {
            var existingJson = File.ReadAllText(EnglishLocalizationPath);
            if (!string.IsNullOrWhiteSpace(existingJson))
            {
                var existing = JsonUtility.FromJson<LocalizationLanguageData>(existingJson);
                if (existing != null && existing.Entries != null)
                {
                    data.Language = string.IsNullOrWhiteSpace(existing.Language) ? "English" : existing.Language;
                    data.Entries = existing.Entries;
                }
            }
        }

        var map = data.Entries
            .Where(e => e != null && !string.IsNullOrWhiteSpace(e.Key))
            .GroupBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        void Put(string key, string value) => map[key] = value;

        Put("task.sample.false_marker.title", "Identify the false chapel marker");
        Put("task.sample.false_marker.group.0.stage.0", "Study the grave markers and select the one that bears the chapel's false dedication.");
        Put("task.sample.false_marker.clue.epitaph", "The true marker mentions no saint by name, only a penitent caretaker.");
        Put("task.sample.false_marker.clue.note", "A bench note says the false stone was carved after the flood to calm the mourners.");
        Put("task.sample.memorial_candles.title", "Relight the memorial candles");
        Put("task.sample.memorial_candles.group.0.stage.0", "Restore the memorial candles around the rain-blackened grave.");
        Put("task.sample.memorial_candles.point.north", "Relight the north memorial candle");
        Put("task.sample.memorial_candles.point.center", "Relight the central memorial candle");
        Put("task.sample.memorial_candles.point.south", "Relight the south memorial candle");
        Put("task.sample.rooted_bench.title", "Burn back the rooted bench growth");
        Put("task.sample.rooted_bench.group.0.stage.0", "Trigger the rooted growth near the church bench and burn it back before it settles.");
        Put("task.sample.pulpit_watch.title", "Keep watch at the pulpit");
        Put("task.sample.pulpit_watch.group.0.stage.0", "Begin the watch at the pulpit and remain there until the prayer is complete.");
        Put("task.sample.pulpit_watch.condition.volume", "Remain within the pulpit prayer space");
        Put("task.sample.pulpit_watch.condition.volume_done", "Pulpit prayer space held");
        Put("task.sample.sanctified_candle_delivery.title", "Carry the sanctified candle to the altar");
        Put("task.sample.sanctified_candle_delivery.group.0.stage.0", "Carry the sanctified candle to the altar while it remains lit.");
        Put("task.sample.penitent_grave.title", "Set the penitent to rest");
        Put("task.sample.penitent_grave.group.0.stage.0", "Identify the penitent's grave from the scattered clues.");
        Put("task.sample.penitent_grave.clue.epitaph", "The penitent asked to be buried facing the chapel, not the gate.");
        Put("task.sample.penitent_grave.clue.bench", "A soaked note says his marker was repaired with plain stone after the bell tower cracked.");
        Put("task.sample.penitent_grave.group.1.stage.0", "Return the bone casket to the penitent's grave.");
        Put("task.sample.penitent_grave.group.2.stage.0", "Keep vigil over the grave until the prayer settles.");
        Put("task.sample.penitent_grave.condition.volume", "Remain within the grave's prayer circle");
        Put("task.sample.penitent_grave.condition.volume_done", "Prayer circle held");
        Put("task.sample.chapel_service.title", "Restore the chapel service");
        Put("task.sample.chapel_service.group.0.stage.0", "Carry the prayer book from the bench to the pulpit.");
        Put("task.sample.chapel_service.group.0.stage.1", "Restore the chapel lights before the service begins.");
        Put("task.sample.chapel_service.point.left", "Relight the left side candle");
        Put("task.sample.chapel_service.point.right", "Relight the right side candle");
        Put("task.sample.chapel_service.point.chandelier", "Restore the chandelier light");
        Put("task.sample.chapel_service.group.1.stage.0", "Read from the pulpit until the service is complete.");
        Put("task.sample.chapel_service.condition.pulpit", "Remain at the pulpit");
        Put("task.sample.chapel_service.condition.pulpit_done", "Pulpit held");
        Put("task.sample.rooted_reliquary.title", "Free the rooted reliquary");
        Put("task.sample.rooted_reliquary.group.0.stage.0", "Bring consecrated oil to the rooted reliquary.");
        Put("task.sample.rooted_reliquary.group.1.stage.0", "Trigger the reliquary growth and cleanse it before the roots settle.");
        Put("task.sample.rooted_reliquary.group.2.stage.0", "Restore the candles around the reliquary in sequence.");
        Put("task.sample.rooted_reliquary.point.west", "Relight the west reliquary candle");
        Put("task.sample.rooted_reliquary.point.east", "Relight the east reliquary candle");
        Put("task.sample.rooted_reliquary.point.shrine", "Restore the shrine lantern");
        Put("task.sample.lantern_wake.title", "Prepare the lantern wake");
        Put("task.sample.lantern_wake.group.0.stage.0", "Restore the wake candles beside the church benches.");
        Put("task.sample.lantern_wake.point.bench_left", "Relight the left bench candle");
        Put("task.sample.lantern_wake.point.bench_right", "Relight the right bench candle");
        Put("task.sample.lantern_wake.group.1.stage.0", "Carry the funeral lantern to the wake grave without letting it go dark.");
        Put("task.sample.lantern_wake.group.2.stage.0", "Complete the final prayer beside the wake grave.");
        Put("task.sample.lantern_wake.condition.volume", "Remain at the wake grave");
        Put("task.sample.lantern_wake.condition.volume_done", "Wake grave held");
        Put("task.sample.lantern_wake.condition.lantern", "Keep the funeral lantern active");
        Put("task.sample.lantern_wake.condition.lantern_done", "Funeral lantern active");

        data.Entries = map.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kvp => new LocalizationEntry { Key = kvp.Key, Value = kvp.Value })
            .ToList();

        File.WriteAllText(EnglishLocalizationPath, JsonUtility.ToJson(data, true));
    }

    private static void CreateTask(string assetPath, string taskId, int difficulty, IEnumerable<string> requiredZones, Action<List<TaskStageGroupDefinition>> configureGroups)
    {
        var task = LoadOrCreateAsset<ComposedTaskDefinition>(assetPath);
        ClearSubAssets(assetPath);

        task.TaskId = taskId;
        task.Difficulty = difficulty;
        task.RequiredForNightCompletion = true;
        task.RequiredZoneIds = requiredZones?.ToList() ?? new List<string>();
        task.StageGroups = new List<TaskStageGroupDefinition>();

        var groups = new List<TaskStageGroupDefinition>();
        configureGroups(groups);

        foreach (var group in groups)
        {
            AddSubAsset(task, group);
            task.StageGroups.Add(group);

            foreach (var stage in group.Stages.Where(s => s != null))
                AddSubAsset(task, stage);
        }

        EditorUtility.SetDirty(task);
        AssetDatabase.ImportAsset(assetPath);
    }

    private static void AddSequentialGroup(List<TaskStageGroupDefinition> groups, string groupId, Action<TaskStageDefinition> configureStage)
    {
        var group = CreateGroup(groupId, false);
        var stage = CreateStageForGroup(groupId);
        configureStage(stage);
        group.Stages.Add(stage);
        groups.Add(group);
    }

    private static void AddParallelGroup(List<TaskStageGroupDefinition> groups, string groupId, Action<List<TaskStageDefinition>> configureStages)
    {
        var group = CreateGroup(groupId, true);
        configureStages(group.Stages);
        groups.Add(group);
    }

    private static TaskStageGroupDefinition CreateGroup(string groupId, bool runInParallel)
    {
        var group = ScriptableObject.CreateInstance<TaskStageGroupDefinition>();
        group.name = groupId;
        group.GroupId = groupId;
        group.RunInParallel = runInParallel;
        group.Stages = new List<TaskStageDefinition>();
        return group;
    }

    private static TaskStageDefinition CreateStageForGroup(string groupId)
    {
        return groupId switch
        {
            "identify_false_marker" => CreateStage<RiddleStageDefinition>("Riddle"),
            "restore_memorial_candles" => CreateStage<RestoreStageDefinition>("Restore"),
            "cleanse_rooted_bench" => CreateStage<CleanseStageDefinition>("Cleanse"),
            "hold_pulpit_watch" => CreateStage<HoldStageDefinition>("Hold"),
            "deliver_sanctified_candle" => CreateStage<DeliverStageDefinition>("Deliver"),
            "identify_grave" => CreateStage<RiddleStageDefinition>("Riddle"),
            "return_remains" => CreateStage<DeliverStageDefinition>("Deliver"),
            "keep_vigil" => CreateStage<HoldStageDefinition>("Hold"),
            "deliver_oil" => CreateStage<DeliverStageDefinition>("Deliver"),
            "cleanse_roots" => CreateStage<CleanseStageDefinition>("Cleanse"),
            "restore_reliquary_candles" => CreateStage<RestoreStageDefinition>("Restore"),
            "restore_wake_candles" => CreateStage<RestoreStageDefinition>("Restore"),
            "deliver_lantern" => CreateStage<DeliverStageDefinition>("Deliver"),
            "hold_final_prayer" => CreateStage<HoldStageDefinition>("Hold"),
            "read_service" => CreateStage<HoldStageDefinition>("Hold"),
            _ => CreateStage<RestoreStageDefinition>("Stage"),
        };
    }

    private static T CreateStage<T>(string name) where T : TaskStageDefinition
    {
        var stage = ScriptableObject.CreateInstance<T>();
        stage.name = name;
        return stage;
    }

    private static void ClearSubAssets(string assetPath)
    {
        var subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .Where(a => a != null && !AssetDatabase.IsMainAsset(a))
            .ToArray();

        foreach (var subAsset in subAssets)
            UnityEngine.Object.DestroyImmediate(subAsset, true);
    }

    private static void AddSubAsset(UnityEngine.Object mainAsset, UnityEngine.Object subAsset)
    {
        if (mainAsset == null || subAsset == null)
            return;

        AssetDatabase.AddObjectToAsset(subAsset, mainAsset);
        EditorUtility.SetDirty(subAsset);
    }

    private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<T>();
        asset.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folderName = Path.GetFileName(path);
        if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        if (!string.IsNullOrWhiteSpace(parent))
            AssetDatabase.CreateFolder(parent, folderName);
    }
}
