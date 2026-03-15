using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class HorrorGameObjectMenu
{
    private const string ZoneMaterialPath = "Assets/Materials/ZoneMaterial.mat";
    private const int InteractableLayer = 9;

    [MenuItem("GameObject/3D Object/Horror/Zone", false, 10)]
    private static void CreateZone(MenuCommand menuCommand)
    {
        var go = CreatePrimitiveObject("Zone", PrimitiveType.Cube, menuCommand);
        go.transform.localScale = new Vector3(10f, 3f, 10f);

        var collider = go.GetComponent<BoxCollider>();
        if (collider != null)
            collider.isTrigger = true;

        ApplyZoneVisuals(go);
        go.AddComponent<Zone>();
    }

    [MenuItem("GameObject/3D Object/Horror/Riddle Answer", false, 11)]
    private static void CreateRiddleAnswer(MenuCommand menuCommand)
    {
        var go = CreatePrimitiveObject("Riddle Answer", PrimitiveType.Cube, menuCommand);
        SetLayerRecursively(go, InteractableLayer);
        go.AddComponent<RiddleAnswerHook>();
        go.AddComponent<RiddleAnswerInteractable>();
    }

    [MenuItem("GameObject/3D Object/Horror/Riddle Clue", false, 12)]
    private static void CreateRiddleClue(MenuCommand menuCommand)
    {
        var go = CreateGameObject("Riddle Clue", menuCommand);
        SetLayerRecursively(go, InteractableLayer);
        var collider = go.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.8f, 0.2f, 0.6f);

        go.AddComponent<RiddleClueHook>();

        var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.name = "WorldModel";
        model.transform.SetParent(go.transform, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localScale = new Vector3(0.8f, 0.1f, 0.6f);
        Object.DestroyImmediate(model.GetComponent<Collider>());

        var inspectable = go.AddComponent<RiddleClueInspectableItem>();
        inspectable.WorldModel = model;
        inspectable.InspectPrefab = model;
        inspectable.Description = "Riddle clue placeholder.";
        inspectable.InspectScaleMultiplier = 2f;
    }

    [MenuItem("GameObject/3D Object/Horror/Restore Point", false, 13)]
    private static void CreateRestorePoint(MenuCommand menuCommand)
    {
        var go = CreatePrimitiveObject("Restore Point", PrimitiveType.Cylinder, menuCommand);
        SetLayerRecursively(go, InteractableLayer);
        go.AddComponent<InteractionTaskHook>();
        go.AddComponent<RestoreInteractable>();
    }

    [MenuItem("GameObject/3D Object/Horror/Cleanse Trigger", false, 14)]
    private static void CreateCleanseTrigger(MenuCommand menuCommand)
    {
        var go = CreatePrimitiveObject("Cleanse Trigger", PrimitiveType.Sphere, menuCommand);
        SetLayerRecursively(go, InteractableLayer);
        go.AddComponent<CleanseTriggerHook>();
        go.AddComponent<CleanseTriggerInteractable>();
    }

    [MenuItem("GameObject/3D Object/Horror/Cleanse Origin Marker", false, 15)]
    private static void CreateCleanseOriginMarker(MenuCommand menuCommand)
    {
        var go = CreatePrimitiveObject("Cleanse Origin Marker", PrimitiveType.Cylinder, menuCommand);
        go.transform.localScale = new Vector3(0.4f, 0.05f, 0.4f);
        SetLayerRecursively(go, InteractableLayer);
        go.AddComponent<CleanseTriggerHook>();
    }

    [MenuItem("GameObject/3D Object/Horror/Hold Activation", false, 16)]
    private static void CreateHoldActivation(MenuCommand menuCommand)
    {
        var go = CreatePrimitiveObject("Hold Activation", PrimitiveType.Cube, menuCommand);
        SetLayerRecursively(go, InteractableLayer);
        go.AddComponent<InteractionTaskHook>();
        go.AddComponent<InteractionHookInteractable>();
    }

    [MenuItem("GameObject/3D Object/Horror/Hold Volume", false, 17)]
    private static void CreateHoldVolume(MenuCommand menuCommand)
    {
        var go = CreatePrimitiveObject("Hold Volume", PrimitiveType.Cube, menuCommand);
        go.name = "Hold Volume";
        go.transform.localScale = new Vector3(3f, 2f, 3f);
        var collider = go.GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = true;

        ApplyDebugVolumeVisuals(go, 0.5f);
        go.AddComponent<VolumeHoldConditionSource>();
    }

    [MenuItem("GameObject/3D Object/Horror/Held Item Condition Source", false, 18)]
    private static void CreateHeldItemConditionSource(MenuCommand menuCommand)
    {
        var go = CreateGameObject("Held Item Condition Source", menuCommand);
        go.AddComponent<HeldItemHoldConditionSource>();
    }

    [MenuItem("GameObject/3D Object/Horror/Deliver Pickup", false, 19)]
    private static void CreateDeliverPickup(MenuCommand menuCommand)
    {
        var go = CreatePrimitiveObject("Deliver Pickup", PrimitiveType.Cube, menuCommand);
        SetLayerRecursively(go, InteractableLayer);
        go.AddComponent<DeliverPickupHook>();
        go.AddComponent<DeliverPickupInteractable>();
    }

    [MenuItem("GameObject/3D Object/Horror/Deliver Deposit", false, 20)]
    private static void CreateDeliverDeposit(MenuCommand menuCommand)
    {
        var go = CreatePrimitiveObject("Deliver Deposit", PrimitiveType.Cylinder, menuCommand);
        SetLayerRecursively(go, InteractableLayer);
        go.AddComponent<DeliverDepositHook>();
        go.AddComponent<DeliverDepositInteractable>();
    }

    [MenuItem("GameObject/Horror/Run Debug View", false, 40)]
    private static void CreateRunDebugView(MenuCommand menuCommand)
    {
        var go = CreateGameObject("Run Debug View", menuCommand);
        go.AddComponent<RunDebugView>();
    }

    [MenuItem("GameObject/Horror/Managers Root", false, 41)]
    private static void CreateManagersRoot(MenuCommand menuCommand)
    {
        var root = CreateGameObject("Managers", menuCommand);
        root.AddComponent<RunManager>();
        root.AddComponent<TaskManager>();
        root.AddComponent<LocalizationManager>();
        root.AddComponent<TaskListManager>();
        root.AddComponent<RunDebugView>();
    }

    private static GameObject CreatePrimitiveObject(string name, PrimitiveType primitiveType, MenuCommand menuCommand)
    {
        var go = GameObject.CreatePrimitive(primitiveType);
        go.name = name;
        FinalizeCreation(go, menuCommand);
        return go;
    }

    private static GameObject CreateGameObject(string name, MenuCommand menuCommand)
    {
        var go = new GameObject(name);
        FinalizeCreation(go, menuCommand);
        return go;
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

        var color = Random.ColorHSV(
            0f,
            1f,
            0.55f,
            0.95f,
            0.75f,
            1f,
            alpha,
            alpha);
        color.a = alpha;

        if (instanceMaterial.HasProperty("_BaseColor"))
            instanceMaterial.SetColor("_BaseColor", color);
        else if (instanceMaterial.HasProperty("_Color"))
            instanceMaterial.color = color;

        renderer.sharedMaterial = instanceMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
    }

    private static void FinalizeCreation(GameObject go, MenuCommand menuCommand)
    {
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, $"Create {go.name}");
        Selection.activeObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null)
            return;

        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            transform.gameObject.layer = layer;
    }
}
