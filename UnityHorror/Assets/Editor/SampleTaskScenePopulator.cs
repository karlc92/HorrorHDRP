using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class SampleTaskScenePopulator
{
    private const string SampleRootName = "SampleTasks_Auto";
    private const string EnvironmentRootName = "Environment";
    private const string PlayerStartName = "PlayerStartPoint";
    private const string ZoneMaterialPath = "Assets/Materials/ZoneMaterial.mat";

    private const string ChurchBenchPrefab = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Church/sm_ChurchBench_01_01.prefab";
    private const string ChurchPulpitPrefab = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Church/sm_ChurchPulpit_01_01.prefab";
    private const string BenchPrefab = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Decorative/sm_Bench_01_02.prefab";
    private const string CandlePrefab = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Decorative/sm_Candle_01_01.prefab";
    private const string CandleAltPrefab = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Decorative/sm_Candle_01_04.prefab";
    private const string ChandelierPrefab = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Decorative/sm_Chandelier_01_01.prefab";
    private const string CoffinPrefab = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Decorative/sm_Coffin_01_01.prefab";
    private const string LanternPrefab = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Decorative/sm_Lantern_01_02.prefab";
    private const string TorchPrefab = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Decorative/sm_Torch_01_02.prefab";

    private const string GravePrefabA = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Graves/sm_Grave_02_01.prefab";
    private const string GravePrefabB = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Graves/sm_Grave_03_03.prefab";
    private const string GravePrefabC = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Graves/sm_GraveStone_01_01.prefab";
    private const string GraveCrossPrefab = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Graves/sm_ColumnCross_01_01.prefab";
    private const string GraveStatuePrefab = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Sculptures/sm_GraveSculpture_05_01.prefab";

    private const string PathPrefab = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Grounds/sm_Path_01_05.prefab";
    private const string GroundPrefab = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Grounds/sm_Ground_04_08.prefab";
    private const string ChapelPrefab = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Small Architecture/sm_Chapel_01_01.prefab";
    private const string MausoleumPrefab = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Small Architecture/sm_Mausoleum_01_02.prefab";
    private const string FencePrefab = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Small Architecture/sm_OldFence_02_01.prefab";
    private const string RootsPrefabA = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Vegetation/sm_Roots_01_01.prefab";
    private const string RootsPrefabB = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Vegetation/sm_Roots_02_01.prefab";
    private const string IvyPrefab = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Vegetation/sm_Ivy_01_04.prefab";
    private const string DeadTreePrefab = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Vegetation/sm_TreeDead_01_02.prefab";
    private const string BushPrefab = "Assets/ExtraAssets/ScansFactory/Cemetery/HDRP/Prefabs/Vegetation/sm_BushGroup_01_02.prefab";

    [MenuItem("Tools/Horror/Populate Sample Tasks In Active Scene")]
    public static void PopulateActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("No active loaded scene to populate.");
            return;
        }

        var sampleRoot = FindRootObject(SampleRootName);
        if (sampleRoot != null)
            Undo.DestroyObjectImmediate(sampleRoot);

        var environment = FindRootObject(EnvironmentRootName);
        var root = CreateGameObject(SampleRootName, environment != null ? environment.transform : null, Vector3.zero);

        Vector3 origin = GetLayoutOrigin();
        Vector3 graveyardCenter = origin + new Vector3(10f, 0f, 18f);
        Vector3 chapelCenter = origin + new Vector3(42f, 0f, 18f);

        var graveyardZone = CreateZone(root.transform, "Zone_Graveyard", "graveyard", graveyardCenter, new Vector3(34f, 4f, 42f));
        var chapelZone = CreateZone(root.transform, "Zone_Chapel", "chapel", chapelCenter, new Vector3(30f, 4f, 34f));

        var graveyardRoot = CreateThemedAreaRoot(root.transform, "GraveyardSamples", graveyardCenter);
        var chapelRoot = CreateThemedAreaRoot(root.transform, "ChapelSamples", chapelCenter);

        CreateGraveyardBackdrop(graveyardRoot.transform, graveyardCenter);
        CreateChapelBackdrop(chapelRoot.transform, chapelCenter);

        CreateSingleStageSamples(graveyardRoot.transform, chapelRoot.transform, graveyardZone, chapelZone, graveyardCenter, chapelCenter);
        CreateMultiStageSamples(graveyardRoot.transform, chapelRoot.transform, graveyardZone, chapelZone, graveyardCenter, chapelCenter);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeObject = root;
        Debug.Log("Sample task objects populated into active scene with cemetery-themed visuals.");
    }

    private static void CreateSingleStageSamples(
        Transform graveyardRoot,
        Transform chapelRoot,
        Zone graveyardZone,
        Zone chapelZone,
        Vector3 graveyardCenter,
        Vector3 chapelCenter)
    {
        var riddleParent = CreateTaskCluster(graveyardRoot, "FalseMarker_Riddle", graveyardCenter + new Vector3(-8f, 0f, -10f));
        CreateRiddleAnswer(riddleParent, "grave.false_marker.correct", graveyardZone, graveyardCenter + new Vector3(-10f, 0f, -10f), GravePrefabA, Vector3.one, new Vector3(0f, 25f, 0f));
        CreateRiddleAnswer(riddleParent, "grave.false_marker.candidate_north", graveyardZone, graveyardCenter + new Vector3(-7f, 0f, -9f), GravePrefabB, Vector3.one, new Vector3(0f, -15f, 0f));
        CreateRiddleAnswer(riddleParent, "grave.false_marker.candidate_ivy", graveyardZone, graveyardCenter + new Vector3(-4f, 0f, -11f), GravePrefabC, new Vector3(0.95f, 0.95f, 0.95f), new Vector3(0f, 10f, 0f));
        CreateRiddleClue(riddleParent, "grave.false_marker.epitaph", graveyardZone, graveyardCenter + new Vector3(-11f, 0f, -6f), GraveCrossPrefab, "Weathered epitaph fragment.");
        CreateRiddleClue(riddleParent, "grave.false_marker.bench_note", graveyardZone, graveyardCenter + new Vector3(-6f, 0f, -5f), BenchPrefab, "Damp bench note.");

        var restoreParent = CreateTaskCluster(graveyardRoot, "MemorialCandles_Restore", graveyardCenter + new Vector3(0f, 0f, -8f));
        CreateRestorePoint(restoreParent, "restore.memorial_candles.north", graveyardZone, graveyardCenter + new Vector3(-2f, 0f, -7f), CandlePrefab, new Vector3(3f, 3f, 3f));
        CreateRestorePoint(restoreParent, "restore.memorial_candles.center", graveyardZone, graveyardCenter + new Vector3(0f, 0f, -5.5f), CandleAltPrefab, new Vector3(3f, 3f, 3f));
        CreateRestorePoint(restoreParent, "restore.memorial_candles.south", graveyardZone, graveyardCenter + new Vector3(2f, 0f, -7f), CandlePrefab, new Vector3(3f, 3f, 3f));
        CreateDecorativeVisual(restoreParent, "MemorialStone", graveyardCenter + new Vector3(0f, 0f, -8f), GraveStatuePrefab, Vector3.one, Vector3.zero);

        var cleanseParent = CreateTaskCluster(chapelRoot, "RootedBench_Cleanse", chapelCenter + new Vector3(-6f, 0f, -10f));
        CreateCleanseTrigger(cleanseParent, "cleanse.rooted_bench.trigger", chapelZone, chapelCenter + new Vector3(-6f, 0f, -10f), BenchPrefab, new Vector3(1.1f, 1.1f, 1.1f), Vector3.zero);
        CreateCleanseOrigin(cleanseParent, "cleanse.rooted_bench.origin", chapelZone, chapelCenter + new Vector3(-3.5f, 0f, -10f), RootsPrefabA, new Vector3(1.3f, 1.3f, 1.3f), new Vector3(0f, 35f, 0f));
        CreateDecorativeVisual(cleanseParent, "BenchRoots", chapelCenter + new Vector3(-5f, 0f, -11.5f), RootsPrefabB, new Vector3(1.2f, 1.2f, 1.2f), new Vector3(0f, 15f, 0f));

        var holdParent = CreateTaskCluster(chapelRoot, "PulpitWatch_Hold", chapelCenter + new Vector3(6f, 0f, -9f));
        CreateHoldActivation(holdParent, "hold.pulpit_watch.start", chapelZone, chapelCenter + new Vector3(6f, 0f, -10f), ChurchPulpitPrefab, Vector3.one, new Vector3(0f, 180f, 0f));
        CreateHoldVolume(holdParent, "hold.pulpit_watch.volume", chapelCenter + new Vector3(6f, 0f, -5.5f), new Vector3(5f, 2.5f, 5f));
        CreateDecorativeVisual(holdParent, "WatchChandelier", chapelCenter + new Vector3(6f, 3f, -5.5f), ChandelierPrefab, Vector3.one, Vector3.zero);

        var deliverParent = CreateTaskCluster(chapelRoot, "SanctifiedCandle_Deliver", chapelCenter + new Vector3(-1f, 0f, 3f));
        CreateDeliverPickup(deliverParent, "deliver.sanctified_candle.pickup", chapelZone, chapelCenter + new Vector3(-3f, 0f, 2f), CandlePrefab, new Vector3(3f, 3f, 3f), Vector3.zero);
        CreateDeliverDeposit(deliverParent, "deliver.sanctified_candle.altar", chapelZone, chapelCenter + new Vector3(2f, 0f, 2f), ChurchPulpitPrefab, new Vector3(0.9f, 0.9f, 0.9f), new Vector3(0f, 180f, 0f));
    }

    private static void CreateMultiStageSamples(
        Transform graveyardRoot,
        Transform chapelRoot,
        Zone graveyardZone,
        Zone chapelZone,
        Vector3 graveyardCenter,
        Vector3 chapelCenter)
    {
        var penitentParent = CreateTaskCluster(graveyardRoot, "PenitentGrave", graveyardCenter + new Vector3(-9f, 0f, 8f));
        CreateRiddleAnswer(penitentParent, "grave.penitent.correct", graveyardZone, graveyardCenter + new Vector3(-11f, 0f, 8f), GraveCrossPrefab, Vector3.one, Vector3.zero);
        CreateRiddleAnswer(penitentParent, "grave.penitent.moss", graveyardZone, graveyardCenter + new Vector3(-8f, 0f, 9f), GravePrefabB, Vector3.one, new Vector3(0f, 12f, 0f));
        CreateRiddleAnswer(penitentParent, "grave.penitent.broken", graveyardZone, graveyardCenter + new Vector3(-5f, 0f, 8f), GravePrefabC, Vector3.one, new Vector3(0f, -20f, 0f));
        CreateRiddleClue(penitentParent, "grave.penitent.epitaph", graveyardZone, graveyardCenter + new Vector3(-12f, 0f, 12f), GraveStatuePrefab, "Penitent epitaph tablet.");
        CreateRiddleClue(penitentParent, "grave.penitent.bench", graveyardZone, graveyardCenter + new Vector3(-7f, 0f, 12f), BenchPrefab, "Bench-side funeral note.");
        CreateDeliverPickup(penitentParent, "deliver.bone_casket.ossuary", graveyardZone, graveyardCenter + new Vector3(-12f, 0f, 15f), CoffinPrefab, new Vector3(0.9f, 0.9f, 0.9f), new Vector3(0f, 90f, 0f));
        CreateDeliverDeposit(penitentParent, "deliver.bone_casket.penitent_grave", graveyardZone, graveyardCenter + new Vector3(-5.5f, 0f, 15f), GravePrefabA, Vector3.one, new Vector3(0f, 25f, 0f));
        CreateHoldActivation(penitentParent, "hold.penitent_grave.start", graveyardZone, graveyardCenter + new Vector3(-11f, 0f, 19f), GraveStatuePrefab, new Vector3(0.8f, 0.8f, 0.8f), Vector3.zero);
        CreateHoldVolume(penitentParent, "hold.penitent_grave.volume", graveyardCenter + new Vector3(-6f, 0f, 19f), new Vector3(5f, 2.5f, 5f));

        var chapelParent = CreateTaskCluster(chapelRoot, "ChapelService", chapelCenter + new Vector3(5f, 0f, 7f));
        CreateDeliverPickup(chapelParent, "deliver.prayer_book.bench", chapelZone, chapelCenter + new Vector3(2f, 0f, 6f), ChurchBenchPrefab, Vector3.one, new Vector3(0f, 90f, 0f));
        CreateDeliverDeposit(chapelParent, "deliver.prayer_book.pulpit", chapelZone, chapelCenter + new Vector3(8f, 0f, 6f), ChurchPulpitPrefab, Vector3.one, new Vector3(0f, 180f, 0f));
        CreateRestorePoint(chapelParent, "restore.chapel_service.candle_left", chapelZone, chapelCenter + new Vector3(3f, 0f, 10f), CandlePrefab, new Vector3(3f, 3f, 3f));
        CreateRestorePoint(chapelParent, "restore.chapel_service.candle_right", chapelZone, chapelCenter + new Vector3(6f, 0f, 10f), CandleAltPrefab, new Vector3(3f, 3f, 3f));
        CreateRestorePoint(chapelParent, "restore.chapel_service.chandelier", chapelZone, chapelCenter + new Vector3(9f, 0f, 10f), ChandelierPrefab, Vector3.one);
        CreateHoldActivation(chapelParent, "hold.chapel_service.start", chapelZone, chapelCenter + new Vector3(3f, 0f, 14f), ChurchBenchPrefab, Vector3.one, new Vector3(0f, 180f, 0f));
        CreateHoldVolume(chapelParent, "hold.chapel_service.pulpit", chapelCenter + new Vector3(8f, 0f, 14f), new Vector3(5f, 2.5f, 5f));

        var reliquaryParent = CreateTaskCluster(chapelRoot, "RootedReliquary", chapelCenter + new Vector3(-6f, 0f, 16f));
        CreateDeliverPickup(reliquaryParent, "deliver.consecrated_oil.store", chapelZone, chapelCenter + new Vector3(-8f, 0f, 15f), LanternPrefab, new Vector3(2f, 2f, 2f), Vector3.zero);
        CreateDeliverDeposit(reliquaryParent, "deliver.consecrated_oil.rooted_reliquary", chapelZone, chapelCenter + new Vector3(-3f, 0f, 15f), GraveStatuePrefab, new Vector3(0.9f, 0.9f, 0.9f), new Vector3(0f, 180f, 0f));
        CreateCleanseTrigger(reliquaryParent, "cleanse.rooted_reliquary.trigger", chapelZone, chapelCenter + new Vector3(-8f, 0f, 19f), RootsPrefabA, new Vector3(1.2f, 1.2f, 1.2f), new Vector3(0f, 20f, 0f));
        CreateCleanseOrigin(reliquaryParent, "cleanse.rooted_reliquary.origin", chapelZone, chapelCenter + new Vector3(-4.5f, 0f, 19f), RootsPrefabB, new Vector3(1.3f, 1.3f, 1.3f), new Vector3(0f, -15f, 0f));
        CreateRestorePoint(reliquaryParent, "restore.rooted_reliquary.candle_west", chapelZone, chapelCenter + new Vector3(-8f, 0f, 23f), CandlePrefab, new Vector3(3f, 3f, 3f));
        CreateRestorePoint(reliquaryParent, "restore.rooted_reliquary.candle_east", chapelZone, chapelCenter + new Vector3(-5.5f, 0f, 23f), CandleAltPrefab, new Vector3(3f, 3f, 3f));
        CreateRestorePoint(reliquaryParent, "restore.rooted_reliquary.shrine", chapelZone, chapelCenter + new Vector3(-3f, 0f, 23f), ChurchPulpitPrefab, new Vector3(0.8f, 0.8f, 0.8f), new Vector3(0f, 180f, 0f));

        var wakeParent = CreateTaskCluster(graveyardRoot, "LanternWake", graveyardCenter + new Vector3(3f, 0f, 13f));
        CreateRestorePoint(wakeParent, "restore.lantern_wake.bench_left", graveyardZone, graveyardCenter + new Vector3(0f, 0f, 12f), BenchPrefab, Vector3.one, new Vector3(0f, 90f, 0f));
        CreateRestorePoint(wakeParent, "restore.lantern_wake.bench_right", graveyardZone, graveyardCenter + new Vector3(3.5f, 0f, 12f), BenchPrefab, Vector3.one, new Vector3(0f, -90f, 0f));
        CreateDeliverPickup(wakeParent, "deliver.funeral_lantern.rack", graveyardZone, graveyardCenter + new Vector3(0.5f, 0f, 16f), LanternPrefab, new Vector3(2f, 2f, 2f), Vector3.zero);
        CreateDeliverDeposit(wakeParent, "deliver.funeral_lantern.wake_grave", graveyardZone, graveyardCenter + new Vector3(5f, 0f, 16f), GravePrefabA, Vector3.one, new Vector3(0f, 10f, 0f));
        CreateHoldActivation(wakeParent, "hold.lantern_wake.start", graveyardZone, graveyardCenter + new Vector3(0.5f, 0f, 20f), TorchPrefab, new Vector3(1.8f, 1.8f, 1.8f), Vector3.zero);
        CreateHoldVolume(wakeParent, "hold.lantern_wake.volume", graveyardCenter + new Vector3(5f, 0f, 20f), new Vector3(5f, 2.5f, 5f));
        CreateHeldItemConditionSource(wakeParent, "hold.lantern_wake.active_lantern", graveyardCenter + new Vector3(7.5f, 0f, 20f));
    }

    private static GameObject CreateThemedAreaRoot(Transform parent, string name, Vector3 worldPosition)
    {
        return CreateGameObject(name, parent, SampleGround(worldPosition));
    }

    private static Transform CreateTaskCluster(Transform parent, string name, Vector3 worldPosition)
    {
        var cluster = CreateGameObject(name, parent, SampleGround(worldPosition));
        return cluster.transform;
    }

    private static Zone CreateZone(Transform parent, string name, string zoneId, Vector3 center, Vector3 size)
    {
        var worldPosition = SampleGround(center);
        worldPosition.y += size.y * 0.5f;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = worldPosition;
        go.transform.localScale = size;

        if (go.TryGetComponent<BoxCollider>(out var collider))
            collider.isTrigger = true;

        ApplyZoneVisuals(go);

        var zone = Undo.AddComponent<Zone>(go);
        zone.ZoneId = zoneId;
        return zone;
    }

    private static void CreateGraveyardBackdrop(Transform parent, Vector3 center)
    {
        CreateDecorativeVisual(parent, "GraveyardGround", center + new Vector3(0f, 0f, 0f), GroundPrefab, new Vector3(2f, 1f, 2f), Vector3.zero);
        CreateDecorativeVisual(parent, "GraveyardPathA", center + new Vector3(-5f, 0f, -2f), PathPrefab, new Vector3(1.2f, 1f, 1.2f), new Vector3(0f, 90f, 0f));
        CreateDecorativeVisual(parent, "GraveyardPathB", center + new Vector3(4f, 0f, 10f), PathPrefab, new Vector3(1.2f, 1f, 1.2f), Vector3.zero);
        CreateDecorativeVisual(parent, "MausoleumBackdrop", center + new Vector3(10f, 0f, 14f), MausoleumPrefab, Vector3.one, new Vector3(0f, 180f, 0f));
        CreateDecorativeVisual(parent, "DeadTreeA", center + new Vector3(-14f, 0f, -2f), DeadTreePrefab, Vector3.one, new Vector3(0f, 30f, 0f));
        CreateDecorativeVisual(parent, "DeadTreeB", center + new Vector3(13f, 0f, 6f), DeadTreePrefab, new Vector3(0.85f, 0.85f, 0.85f), new Vector3(0f, -20f, 0f));
        CreateDecorativeVisual(parent, "FenceA", center + new Vector3(-15f, 0f, 14f), FencePrefab, Vector3.one, new Vector3(0f, 90f, 0f));
        CreateDecorativeVisual(parent, "FenceB", center + new Vector3(15f, 0f, -8f), FencePrefab, Vector3.one, new Vector3(0f, -90f, 0f));
        CreateDecorativeVisual(parent, "BushCluster", center + new Vector3(-12f, 0f, 10f), BushPrefab, Vector3.one, Vector3.zero);
        CreateDecorativeVisual(parent, "IvyCluster", center + new Vector3(11f, 0f, 18f), IvyPrefab, Vector3.one, new Vector3(0f, 45f, 0f));
    }

    private static void CreateChapelBackdrop(Transform parent, Vector3 center)
    {
        CreateDecorativeVisual(parent, "ChapelGround", center + new Vector3(0f, 0f, 0f), GroundPrefab, new Vector3(2f, 1f, 2f), Vector3.zero);
        CreateDecorativeVisual(parent, "ChapelBuilding", center + new Vector3(0f, 0f, 10f), ChapelPrefab, Vector3.one, new Vector3(0f, 180f, 0f));
        CreateDecorativeVisual(parent, "ChurchBenchA", center + new Vector3(-7f, 0f, 1f), ChurchBenchPrefab, Vector3.one, new Vector3(0f, 90f, 0f));
        CreateDecorativeVisual(parent, "ChurchBenchB", center + new Vector3(7f, 0f, 1f), ChurchBenchPrefab, Vector3.one, new Vector3(0f, -90f, 0f));
        CreateDecorativeVisual(parent, "PulpitBackdrop", center + new Vector3(0f, 0f, -2f), ChurchPulpitPrefab, Vector3.one, new Vector3(0f, 180f, 0f));
        CreateDecorativeVisual(parent, "ChandelierBackdrop", center + new Vector3(0f, 4.5f, 0f), ChandelierPrefab, Vector3.one, Vector3.zero);
        CreateDecorativeVisual(parent, "RootsA", center + new Vector3(-10f, 0f, 14f), RootsPrefabA, new Vector3(1.3f, 1.3f, 1.3f), new Vector3(0f, 25f, 0f));
        CreateDecorativeVisual(parent, "RootsB", center + new Vector3(10f, 0f, 16f), RootsPrefabB, new Vector3(1.3f, 1.3f, 1.3f), new Vector3(0f, -35f, 0f));
    }

    private static GameObject CreateRiddleAnswer(Transform parent, string hookId, Zone zone, Vector3 position, string visualPrefabPath, Vector3 visualScale, Vector3 visualRotation)
    {
        var root = CreateInteractionRoot("RiddleAnswer_" + Sanitize(hookId), parent, position, new Vector3(1.6f, 2f, 1.6f));
        var hook = Undo.AddComponent<RiddleAnswerHook>(root);
        hook.HookId = hookId;
        hook.Zone = zone;
        Undo.AddComponent<RiddleAnswerInteractable>(root);
        AttachThemedVisual(root.transform, visualPrefabPath, visualScale, visualRotation, PrimitiveType.Cube, new Vector3(1f, 1.4f, 0.8f));
        return root;
    }

    private static GameObject CreateRiddleClue(Transform parent, string hookId, Zone zone, Vector3 position, string visualPrefabPath, string description)
    {
        var root = CreateInteractionRoot("RiddleClue_" + Sanitize(hookId), parent, position, new Vector3(1.2f, 0.9f, 1.2f));
        var hook = Undo.AddComponent<RiddleClueHook>(root);
        hook.HookId = hookId;
        hook.Zone = zone;

        var visual = AttachThemedVisual(root.transform, visualPrefabPath, Vector3.one, Vector3.zero, PrimitiveType.Cube, new Vector3(0.8f, 0.1f, 0.6f));

        var inspectable = Undo.AddComponent<RiddleClueInspectableItem>(root);
        inspectable.WorldModel = visual;
        inspectable.InspectPrefab = visual;
        inspectable.Description = description;
        inspectable.InspectScaleMultiplier = 1.75f;
        return root;
    }

    private static GameObject CreateRestorePoint(
        Transform parent,
        string hookId,
        Zone zone,
        Vector3 position,
        string visualPrefabPath,
        Vector3 visualScale,
        Vector3? visualRotation = null)
    {
        var root = CreateInteractionRoot("RestorePoint_" + Sanitize(hookId), parent, position, new Vector3(1.2f, 1.2f, 1.2f));
        var hook = Undo.AddComponent<InteractionTaskHook>(root);
        hook.HookId = hookId;
        hook.Zone = zone;
        Undo.AddComponent<RestoreInteractable>(root);
        AttachThemedVisual(root.transform, visualPrefabPath, visualScale, visualRotation ?? Vector3.zero, PrimitiveType.Cylinder, new Vector3(0.6f, 0.8f, 0.6f));
        return root;
    }

    private static GameObject CreateCleanseTrigger(
        Transform parent,
        string hookId,
        Zone zone,
        Vector3 position,
        string visualPrefabPath,
        Vector3 visualScale,
        Vector3 visualRotation)
    {
        var root = CreateInteractionRoot("CleanseTrigger_" + Sanitize(hookId), parent, position, new Vector3(1.4f, 1.4f, 1.4f));
        var hook = Undo.AddComponent<CleanseTriggerHook>(root);
        hook.HookId = hookId;
        hook.Zone = zone;
        Undo.AddComponent<CleanseTriggerInteractable>(root);
        AttachThemedVisual(root.transform, visualPrefabPath, visualScale, visualRotation, PrimitiveType.Sphere, new Vector3(1f, 1f, 1f));
        return root;
    }

    private static GameObject CreateCleanseOrigin(
        Transform parent,
        string hookId,
        Zone zone,
        Vector3 position,
        string visualPrefabPath,
        Vector3 visualScale,
        Vector3 visualRotation)
    {
        var root = CreateInteractionRoot("CleanseOrigin_" + Sanitize(hookId), parent, position, new Vector3(1.6f, 0.8f, 1.6f));
        var hook = Undo.AddComponent<CleanseTriggerHook>(root);
        hook.HookId = hookId;
        hook.Zone = zone;
        AttachThemedVisual(root.transform, visualPrefabPath, visualScale, visualRotation, PrimitiveType.Cylinder, new Vector3(0.8f, 0.1f, 0.8f));
        return root;
    }

    private static GameObject CreateHoldActivation(
        Transform parent,
        string hookId,
        Zone zone,
        Vector3 position,
        string visualPrefabPath,
        Vector3 visualScale,
        Vector3 visualRotation)
    {
        var root = CreateInteractionRoot("HoldActivation_" + Sanitize(hookId), parent, position, new Vector3(1.5f, 1.8f, 1.5f));
        var hook = Undo.AddComponent<InteractionTaskHook>(root);
        hook.HookId = hookId;
        hook.Zone = zone;
        Undo.AddComponent<InteractionHookInteractable>(root);
        AttachThemedVisual(root.transform, visualPrefabPath, visualScale, visualRotation, PrimitiveType.Cube, new Vector3(1f, 1f, 1f));
        return root;
    }

    private static GameObject CreateHoldVolume(Transform parent, string sourceId, Vector3 center, Vector3 size)
    {
        var worldCenter = SampleGround(center);
        worldCenter.y += size.y * 0.5f;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(go, $"Create HoldVolume_{sourceId}");
        go.name = "HoldVolume_" + Sanitize(sourceId);
        go.transform.SetParent(parent, false);
        go.transform.position = worldCenter;
        go.transform.localScale = size;

        if (go.TryGetComponent<BoxCollider>(out var collider))
            collider.isTrigger = true;

        ApplyDebugVolumeVisuals(go, 0.5f);

        var source = Undo.AddComponent<VolumeHoldConditionSource>(go);
        source.SourceId = sourceId;
        return go;
    }

    private static GameObject CreateHeldItemConditionSource(Transform parent, string sourceId, Vector3 position)
    {
        var go = CreateGameObject("HeldItemCondition_" + Sanitize(sourceId), parent, SampleGround(position));
        var source = Undo.AddComponent<HeldItemHoldConditionSource>(go);
        source.SourceId = sourceId;
        return go;
    }

    private static GameObject CreateDeliverPickup(
        Transform parent,
        string hookId,
        Zone zone,
        Vector3 position,
        string visualPrefabPath,
        Vector3 visualScale,
        Vector3 visualRotation)
    {
        var root = CreateInteractionRoot("DeliverPickup_" + Sanitize(hookId), parent, position, new Vector3(1.4f, 1.4f, 1.4f));
        var hook = Undo.AddComponent<DeliverPickupHook>(root);
        hook.HookId = hookId;
        hook.Zone = zone;
        Undo.AddComponent<DeliverPickupInteractable>(root);
        AttachThemedVisual(root.transform, visualPrefabPath, visualScale, visualRotation, PrimitiveType.Cube, new Vector3(1f, 1f, 1f));
        return root;
    }

    private static GameObject CreateDeliverDeposit(
        Transform parent,
        string hookId,
        Zone zone,
        Vector3 position,
        string visualPrefabPath,
        Vector3 visualScale,
        Vector3 visualRotation)
    {
        var root = CreateInteractionRoot("DeliverDeposit_" + Sanitize(hookId), parent, position, new Vector3(1.8f, 1.8f, 1.8f));
        var hook = Undo.AddComponent<DeliverDepositHook>(root);
        hook.HookId = hookId;
        hook.Zone = zone;
        Undo.AddComponent<DeliverDepositInteractable>(root);
        AttachThemedVisual(root.transform, visualPrefabPath, visualScale, visualRotation, PrimitiveType.Cylinder, new Vector3(1f, 1f, 1f));
        return root;
    }

    private static GameObject CreateInteractionRoot(string name, Transform parent, Vector3 worldPosition, Vector3 colliderSize)
    {
        var go = CreateGameObject(name, parent, SampleGround(worldPosition));
        var collider = Undo.AddComponent<BoxCollider>(go);
        collider.size = colliderSize;
        collider.center = new Vector3(0f, colliderSize.y * 0.5f, 0f);
        return go;
    }

    private static void CreateDecorativeVisual(
        Transform parent,
        string name,
        Vector3 worldPosition,
        string visualPrefabPath,
        Vector3 visualScale,
        Vector3 visualRotation)
    {
        var root = CreateGameObject(name, parent, SampleGround(worldPosition));
        AttachThemedVisual(root.transform, visualPrefabPath, visualScale, visualRotation, PrimitiveType.Cube, Vector3.one);
    }

    private static GameObject AttachThemedVisual(
        Transform parent,
        string prefabPath,
        Vector3 localScale,
        Vector3 localEulerAngles,
        PrimitiveType fallbackPrimitive,
        Vector3 fallbackScale)
    {
        GameObject visual = null;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            visual = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (visual != null)
            {
                Undo.RegisterCreatedObjectUndo(visual, $"Create {prefab.name}");
                visual.transform.SetParent(parent, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.Euler(localEulerAngles);
                visual.transform.localScale = localScale;
                DisableCollidersRecursively(visual);
                return visual;
            }
        }

        visual = GameObject.CreatePrimitive(fallbackPrimitive);
        Undo.RegisterCreatedObjectUndo(visual, $"Create {parent.name}Visual");
        visual.name = "WorldModel";
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(localEulerAngles);
        visual.transform.localScale = fallbackScale;
        if (visual.TryGetComponent<Collider>(out var collider))
            Object.DestroyImmediate(collider);
        return visual;
    }

    private static void DisableCollidersRecursively(GameObject root)
    {
        if (root == null)
            return;

        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
    }

    private static GameObject CreateGameObject(string name, Transform parent, Vector3 worldPosition)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent, false);
        go.transform.position = worldPosition;
        return go;
    }

    private static GameObject FindRootObject(string name)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root != null && root.name == name)
                return root;
        }

        return null;
    }

    private static Vector3 GetLayoutOrigin()
    {
        var playerStart = GameObject.Find(PlayerStartName);
        if (playerStart != null)
        {
            var basePos = playerStart.transform.position;
            return new Vector3(basePos.x, 0f, basePos.z);
        }

        return Vector3.zero;
    }

    private static Vector3 SampleGround(Vector3 desiredPosition)
    {
        var origin = new Vector3(desiredPosition.x, 200f, desiredPosition.z);
        if (Physics.Raycast(origin, Vector3.down, out var hit, 500f, ~0, QueryTriggerInteraction.Ignore))
            return hit.point;

        return new Vector3(desiredPosition.x, Mathf.Max(0f, desiredPosition.y), desiredPosition.z);
    }

    private static void ApplyZoneVisuals(GameObject go)
    {
        ApplyDebugVolumeVisuals(go, 0.35f);
    }

    private static void ApplyDebugVolumeVisuals(GameObject go, float alpha)
    {
        if (go == null || !go.TryGetComponent<MeshRenderer>(out var renderer))
            return;

        var baseMaterial = AssetDatabase.LoadAssetAtPath<Material>(ZoneMaterialPath);
        if (baseMaterial == null)
            return;

        var instanceMaterial = new Material(baseMaterial);
        instanceMaterial.name = $"{baseMaterial.name}_{go.name}";

        var color = Random.ColorHSV(0f, 1f, 0.55f, 0.95f, 0.75f, 1f, alpha, alpha);
        color.a = alpha;

        if (instanceMaterial.HasProperty("_BaseColor"))
            instanceMaterial.SetColor("_BaseColor", color);
        else if (instanceMaterial.HasProperty("_Color"))
            instanceMaterial.color = color;

        renderer.sharedMaterial = instanceMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
    }

    private static string Sanitize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Unnamed" : value.Replace('.', '_');
    }
}
