using System;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Implement this on scene objects that need to synchronize runtime state into Game.State
/// immediately before saving, and/or rebuild runtime caches immediately after loading.
/// This prevents Game.SaveGame / Game.LoadGame from needing to know about every system's
/// internal fields (robust as the project grows).
/// </summary>
public interface IGameSaveParticipant
{
    void OnBeforeGameSaved(GameState state);
    void OnAfterGameLoaded(GameState state);
}

public static class Game
{
    public const string GameSceneName = "GameScene";
    public const string MenuSceneName = "MenuScene";
    public const string LoadingSceneName = "LoadingScene";
    public const string SaveFilePrefix = "save_slot_";

    public static GameSettings Settings = new GameSettings();
    public static GameState State = new GameState();
    public static bool Started = false;

    private static bool hookedSceneLoaded;
    private static bool pendingApplyLoadedState;

    public static void Awake()
    {
        if (!hookedSceneLoaded)
        {
            hookedSceneLoaded = true;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    public static void Start()
    {
        Started = true;
    }

    public static void Update()
    {
        if (SceneManager.GetActiveScene().name == GameSceneName)
        {
            // Clamp to avoid huge jumps on hitches / tab-out.
            float dt = Mathf.Min(Time.deltaTime, 0.25f);
            State.TotalPlayTimeSeconds += dt;
        }
    }

    public static void FixedUpdate()
    {

    }

    public static void ReturnToMainMenu()
    {
        SceneManager.LoadScene(MenuSceneName);
    }

    public static void StartNewGame(int slot)
    {
        pendingApplyLoadedState = false;

        State = new GameState()
        {
            Slot = slot,
            Night = 1,
            PlayerPos = Vector3.zero,
            PlayerRot = Quaternion.identity,
            MonsterBrainState = new MonsterBrainState(),
        };

        SceneManager.LoadScene(GameSceneName);
    }

    public static bool SaveGame(int slot)
    {
        if (!IsValidSlot(slot))
        {
            Debug.LogWarning($"[Game] SaveGame: Invalid slot {slot}. Only 1..3 are supported.");
            return false;
        }

        if (State == null) State = new GameState();
        if (State.MonsterBrainState == null) State.MonsterBrainState = new MonsterBrainState();

        NotifyBeforeSave();

        State.Slot = slot;

        string path = GetSavePath(slot);
        string json = JsonUtility.ToJson(State, prettyPrint: true);

        try
        {
            File.WriteAllText(path, json);
            Console.Print("Saved game for Slot " + slot);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Game] SaveGame failed for slot {slot} at '{path}'.\n{e}");
            return false;
        }
    }

    public static bool LoadGame(int slot)
    {
        if (!IsValidSlot(slot))
        {
            Debug.LogWarning($"[Game] LoadGame: Invalid slot {slot}. Only 1..3 are supported.");
            return false;
        }

        string path = GetSavePath(slot);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[Game] LoadGame: No save file found for slot {slot} at '{path}'.");
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            var loaded = JsonUtility.FromJson<GameState>(json);
            if (loaded == null)
            {
                Debug.LogError($"[Game] LoadGame: Failed to deserialize save for slot {slot} at '{path}'.");
                return false;
            }

            if (loaded.MonsterBrainState == null)
                loaded.MonsterBrainState = new MonsterBrainState();

            loaded.Slot = slot;
            State = loaded;
            Console.Print("Loaded game for Slot " + slot);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Game] LoadGame failed for slot {slot} at '{path}'.\n{e}");
            return false;
        }

        pendingApplyLoadedState = true;
        SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);

        return true;
    }

    // -----------------------------
    // Scene load hook / apply
    // -----------------------------

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!pendingApplyLoadedState) return;
        if (scene.name != GameSceneName) return;

        ApplyLoadedStateToScene();
        pendingApplyLoadedState = false;
    }

    private static void ApplyLoadedStateToScene()
    {
        Console.Print("Trying to apply loadedState: " + JsonUtility.ToJson(State));
        // Player
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (player)
        {
            player.ApplySavedPose(State.PlayerPos, State.PlayerRot);
        }
        else
        {
            Debug.LogWarning("[Game] ApplyLoadedStateToScene: No PlayerController found in scene.");
            Console.Print("[Game] ApplyLoadedStateToScene: No PlayerController found in scene.");
        }

        // Monster
        var monster = UnityEngine.Object.FindFirstObjectByType<MonsterController>();
        if (monster && State.MonsterBrainState != null)
        {
            monster.ApplyLoadedBrainState(State.MonsterBrainState);
        }
        else if (!monster)
        {
            Debug.LogWarning("[Game] ApplyLoadedStateToScene: No MonsterController found in scene.");
            Console.Print("[Game] ApplyLoadedStateToScene: No MonsterController found in scene.");
        }

        // Allow systems to rebuild non-serialized runtime caches.
        NotifyAfterLoad();

        Physics.SyncTransforms();
    }

    private static void NotifyBeforeSave()
    {
        // Sync runtime state into Game.State right before serializing.
        foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb is IGameSaveParticipant p)
            {
                try { p.OnBeforeGameSaved(State); }
                catch (Exception e) { Debug.LogError($"[Game] OnBeforeGameSaved error on {mb.name}.\n{e}"); }
            }
        }
    }

    private static void NotifyAfterLoad()
    {
        // Give scene objects a chance to rebuild non-serialized caches.
        foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb is IGameSaveParticipant p)
            {
                try { p.OnAfterGameLoaded(State); }
                catch (Exception e) { Debug.LogError($"[Game] OnAfterGameLoaded error on {mb.name}.\n{e}"); }
            }
        }
    }

    // -----------------------------
    // Save path helpers
    // -----------------------------

    private static bool IsValidSlot(int slot) => slot >= 1 && slot <= 3;

    private static string GetSavePath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, $"{SaveFilePrefix}{slot}.json");
    }

    public static bool HasSaveFile(int slot)
    {
        if (slot < 1 || slot > 3)
            return false;

        string path = Path.Combine(Application.persistentDataPath, $"{Game.SaveFilePrefix}{slot}.json");
        return File.Exists(path);
    }

}