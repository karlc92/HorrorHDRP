using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Implement this on scene objects that need to synchronize runtime state into Game.State
/// immediately before saving, and/or rebuild runtime caches immediately after loading.
/// </summary>
public interface IGameSaveParticipant
{
    void OnBeforeGameSaved(GameState state);
    void OnAfterGameLoaded(GameState state);
}

public static class Game
{
    public const int MaxSaveSlots = 3;
    public const string GameSceneName = "GameScene";
    public const string MenuSceneName = "MenuScene";
    public const string LoadingSceneName = "LoadingScene";
    public const string SaveFilePrefix = "save_slot_";
    public const string SaveFileName = "game_state.json";

    public static GameSettings Settings = new GameSettings();
    public static GameState State = new GameState();
    public static bool Started = false;
    public static int ActiveSlot { get; private set; } = 1;

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

        Settings.ApplyVideoSettings();
        if (!LoadGameState(ActiveSlot))
            State = CreateFreshState(ActiveSlot);
    }

    public static void Start()
    {
        Started = true;
    }

    public static void Update()
    {
        if (SceneManager.GetActiveScene().name == GameSceneName && State != null)
        {
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
        ActiveSlot = ClampSlot(slot);
        State = CreateFreshState(ActiveSlot);

        SaveGameState();
        pendingApplyLoadedState = false;
        SceneManager.LoadScene(GameSceneName);
    }

    public static void ContinueGame()
    {
        if (!HasSaveFile(ActiveSlot))
        {
            Debug.LogWarning($"[Game] ContinueGame requested with no save in slot {ActiveSlot}.");
            return;
        }

        LoadGameState(ActiveSlot);
        pendingApplyLoadedState = true;
        SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
    }

    public static void AbandonGame()
    {
        ResetCurrentGameState();
        SaveGameState();
        ReturnToMainMenu();
    }

    public static void ResetCurrentGameState()
    {
        State = CreateFreshState(ActiveSlot);
    }

    public static bool SaveGameState()
    {
        State ??= CreateFreshState(ActiveSlot);
        State.Slot = ActiveSlot;
        State.EnsureInitialized();

        NotifyBeforeSave();

        string path = GetSavePath(ActiveSlot);
        string json = JsonUtility.ToJson(State, prettyPrint: true);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);
            Console.Print("Saved game state");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Game] SaveGameState failed at '{path}'.\n{e}");
            return false;
        }
    }

    public static bool LoadGameState()
    {
        return LoadGameState(ActiveSlot);
    }

    public static bool LoadGameState(int slot)
    {
        ActiveSlot = ClampSlot(slot);
        string path = GetSavePath(ActiveSlot);
        if (!File.Exists(path))
        {
            State = CreateFreshState(ActiveSlot);
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            var loaded = JsonUtility.FromJson<GameState>(json);
            State = loaded ?? CreateFreshState(ActiveSlot);
            State.Slot = ActiveSlot;
            State.EnsureInitialized();

            Console.Print("Loaded game state");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Game] LoadGameState failed at '{path}'.\n{e}");
            State = CreateFreshState(ActiveSlot);
            return false;
        }
    }

    public static bool SaveGame(int slot)
    {
        ActiveSlot = ClampSlot(slot);
        return SaveGameState();
    }

    public static bool LoadGame(int slot)
    {
        if (!LoadGameState(slot))
            return false;

        pendingApplyLoadedState = true;
        SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
        return true;
    }

    public static bool HasSaveFile(int slot)
    {
        string path = GetSavePath(slot);
        return File.Exists(path);
    }

    public static bool HasActiveGame()
    {
        return HasSaveFile(ActiveSlot);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!pendingApplyLoadedState) return;
        if (scene.name != GameSceneName) return;

        ApplyLoadedStateToScene();
        pendingApplyLoadedState = false;
    }

    private static void ApplyLoadedStateToScene()
    {
        if (State == null)
            return;

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

        NotifyAfterLoad();
        Physics.SyncTransforms();
    }

    private static void NotifyBeforeSave()
    {
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
        foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb is IGameSaveParticipant p)
            {
                try { p.OnAfterGameLoaded(State); }
                catch (Exception e) { Debug.LogError($"[Game] OnAfterGameLoaded error on {mb.name}.\n{e}"); }
            }
        }
    }

    public static string GetSavePath()
    {
        return GetSavePath(ActiveSlot);
    }

    public static string GetSavePath(int slot)
    {
        int clampedSlot = ClampSlot(slot);
        string folder = Path.Combine(Application.persistentDataPath, $"{SaveFilePrefix}{clampedSlot}");
        return Path.Combine(folder, SaveFileName);
    }

    public static bool TryReadSaveState(int slot, out GameState state)
    {
        state = null;
        string path = GetSavePath(slot);
        if (!File.Exists(path))
            return false;

        try
        {
            string json = File.ReadAllText(path);
            state = JsonUtility.FromJson<GameState>(json);
            state ??= CreateFreshState(slot);
            state.Slot = ClampSlot(slot);
            state.EnsureInitialized();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Game] TryReadSaveState failed at '{path}'.\n{e}");
            state = null;
            return false;
        }
    }

    private static GameState CreateFreshState(int slot)
    {
        var state = new GameState
        {
            Slot = ClampSlot(slot),
            Story = new StoryState
            {
                Started = true,
                CurrentBeatIndex = 0,
                CurrentBeatId = "opening",
                CurrentObjectiveTitle = "Begin the story",
                CurrentObjectiveDetail = "Move deeper into the world and uncover what happened here.",
            },
            Inventory = new InventoryState(),
            MonsterBrainState = new MonsterBrainState(),
            Progression = new ProgressionState(),
            PlayerPos = Vector3.zero,
            PlayerRot = Quaternion.identity,
            TotalPlayTimeSeconds = 0f,
        };

        state.EnsureInitialized();
        return state;
    }

    private static int ClampSlot(int slot)
    {
        return Mathf.Clamp(slot, 1, MaxSaveSlots);
    }
}
