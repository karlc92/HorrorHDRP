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
    public const string GameSceneName = "GameScene";
    public const string MenuSceneName = "MenuScene";
    public const string LoadingSceneName = "LoadingScene";
    public const string SaveFilePrefix = "save_slot_"; // Legacy compatibility only.
    public const string SaveFileName = "game_state.json";

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

        LoadGameState();
    }

    public static void Start()
    {
        Started = true;
    }

    public static void Update()
    {
        if (SceneManager.GetActiveScene().name == GameSceneName && State?.Run != null)
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

    public static void StartNewRun()
    {
        State ??= new GameState();
        State.Progression ??= new ProgressionState();

        var seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        if (seed == 0)
            seed = 1;

        State.Run = new RunState
        {
            Seed = seed,
            CurrentNightNumber = 1,
            PlayerPos = Vector3.zero,
            PlayerRot = Quaternion.identity,
            MonsterBrainState = new MonsterBrainState(),
            CurrentNightState = new NightRuntimeState(),
            NightStartSnapshot = new NightSnapshot(),
            Plan = new RunPlan(),
            NightStarted = false,
        };

        SaveGameState();
        pendingApplyLoadedState = false;
        SceneManager.LoadScene(GameSceneName);
    }

    public static void ContinueRun()
    {
        if (State?.Run == null)
        {
            Debug.LogWarning("[Game] ContinueRun requested with no active run.");
            return;
        }

        pendingApplyLoadedState = true;
        SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
    }

    public static void GiveUpRun()
    {
        ClearRunState();
        SaveGameState();
        ReturnToMainMenu();
    }

    public static void ClearRunState()
    {
        if (State == null)
            State = new GameState();

        State.Run = null;
    }

    public static bool SaveGameState()
    {
        if (State == null)
            State = new GameState();

        if (State.Progression == null)
            State.Progression = new ProgressionState();

        if (State.Run != null)
        {
            if (State.Run.MonsterBrainState == null)
                State.Run.MonsterBrainState = new MonsterBrainState();
            if (State.Run.CurrentNightState == null)
                State.Run.CurrentNightState = new NightRuntimeState();
            if (State.Run.Plan == null)
                State.Run.Plan = new RunPlan();
            if (State.Run.NightStartSnapshot == null)
                State.Run.NightStartSnapshot = new NightSnapshot();
        }

        NotifyBeforeSave();

        string path = GetSavePath();
        string json = JsonUtility.ToJson(State, prettyPrint: true);

        try
        {
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
        string path = GetSavePath();
        if (!File.Exists(path))
        {
            State = new GameState();
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            var loaded = JsonUtility.FromJson<GameState>(json);
            State = loaded ?? new GameState();
            State.Progression ??= new ProgressionState();
            if (State.Run != null)
            {
                State.Run.MonsterBrainState ??= new MonsterBrainState();
                State.Run.CurrentNightState ??= new NightRuntimeState();
                State.Run.Plan ??= new RunPlan();
                State.Run.NightStartSnapshot ??= new NightSnapshot();
            }

            Console.Print("Loaded game state");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Game] LoadGameState failed at '{path}'.\n{e}");
            State = new GameState();
            return false;
        }
    }

    // Legacy wrappers kept temporarily so existing menu/UI code still compiles.
    public static void StartNewGame(int _slot) => StartNewRun();

    public static bool SaveGame(int _slot) => SaveGameState();

    public static bool LoadGame(int _slot)
    {
        if (!LoadGameState() || State?.Run == null)
            return false;

        pendingApplyLoadedState = true;
        SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
        return true;
    }

    public static bool HasSaveFile(int _slot) => HasActiveRun();

    public static bool HasActiveRun()
    {
        if (State == null)
            LoadGameState();

        return State?.Run != null;
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
        if (State?.Run == null)
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
        return Path.Combine(Application.persistentDataPath, SaveFileName);
    }
}
