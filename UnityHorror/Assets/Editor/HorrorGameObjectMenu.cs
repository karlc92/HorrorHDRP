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

    [MenuItem("GameObject/Horror/Managers Root", false, 41)]
    private static void CreateManagersRoot(MenuCommand menuCommand)
    {
        var root = CreateGameObject("Managers", menuCommand);
        root.AddComponent<LocalizationManager>();
        root.AddComponent<InventoryManager>();
        root.AddComponent<MonsterManager>();
        root.AddComponent<OutlineManager>();
        root.AddComponent<InspectionManager>();
        root.AddComponent<ProceduralWorldGenerator>();
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
