using UnityEngine;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [SerializeField] private RunGenerationSettings generationSettings;
    [SerializeField] private Transform playerStartPoint;

    private Zone[] zones = System.Array.Empty<Zone>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        zones = FindObjectsByType<Zone>(FindObjectsSortMode.None);
    }

    private void Start()
    {
        if (Game.State?.Run == null)
            return;

        if (Game.State.Run.Plan == null || Game.State.Run.Plan.Nights.Count == 0)
            InitializeNewRunPlan();
        else
            ApplyCurrentNightPlan();
    }

    public void InitializeNewRunPlan()
    {
        var run = Game.State.EnsureRunState();
        var taskDefinitions = TaskManager.LoadDefinitions();
        var loreDefinitions = LoreDatabase.LoadDefinitions();
        run.Plan = RunGenerator.Generate(run.Seed, taskDefinitions, loreDefinitions, generationSettings, Game.State.Progression);
        run.CurrentNightNumber = Mathf.Clamp(run.CurrentNightNumber, 1, Mathf.Max(1, run.Plan.Nights.Count));
        run.CurrentNightState = TaskManager.CreateNightRuntimeStateForPlan(GetCurrentNightPlan());
        run.NightStarted = false;
        CaptureNightStartSnapshot();
        ApplyCurrentNightPlan();
        Game.SaveGameState();
    }

    public NightPlan GetCurrentNightPlan()
    {
        if (Game.State?.Run?.Plan?.Nights == null || Game.State.Run.Plan.Nights.Count == 0)
            return null;

        int index = Mathf.Clamp(Game.State.Run.CurrentNightNumber - 1, 0, Game.State.Run.Plan.Nights.Count - 1);
        return Game.State.Run.Plan.Nights[index];
    }

    public void ApplyCurrentNightPlan()
    {
        var plan = GetCurrentNightPlan();
        if (plan == null)
            return;

        ApplyZoneStates(plan);
    }

    public void OnNightStarted()
    {
        if (Game.State?.Run == null)
            return;

        Game.State.Run.NightStarted = true;
        if (Game.State.Run.CurrentNightState != null)
            Game.State.Run.CurrentNightState.NightStarted = true;

        Game.SaveGameState();
    }

    public bool CanEndNight()
    {
        bool canEnd = TaskManager.Instance != null && TaskManager.Instance.AreAllRequiredTasksComplete();
        if (Game.State?.Run?.CurrentNightState != null)
            Game.State.Run.CurrentNightState.CanEndNight = canEnd;

        return canEnd;
    }

    public bool TryEndNight()
    {
        if (!CanEndNight() || Game.State?.Run == null)
            return false;

        Game.State.Run.CurrentNightNumber++;
        if (Game.State.Run.Plan != null && Game.State.Run.CurrentNightNumber > Game.State.Run.Plan.Nights.Count)
        {
            Game.ClearRunState();
            Game.SaveGameState();
            Game.ReturnToMainMenu();
            return true;
        }

        Game.State.Run.CurrentNightState = TaskManager.CreateNightRuntimeStateForPlan(GetCurrentNightPlan());
        Game.State.Run.NightStarted = false;
        CaptureNightStartSnapshot();
        ApplyCurrentNightPlan();
        Game.SaveGameState();
        return true;
    }

    public void RetryCurrentNight()
    {
        var run = Game.State?.Run;
        if (run == null || run.NightStartSnapshot == null)
            return;

        run.PlayerPos = run.NightStartSnapshot.PlayerPos;
        run.PlayerRot = run.NightStartSnapshot.PlayerRot;
        run.MonsterBrainState = JsonUtility.FromJson<MonsterBrainState>(
            JsonUtility.ToJson(run.NightStartSnapshot.MonsterBrainState ?? new MonsterBrainState()));
        run.CurrentNightState = JsonUtility.FromJson<NightRuntimeState>(
            JsonUtility.ToJson(run.NightStartSnapshot.NightState ?? new NightRuntimeState()));
        run.NightStarted = run.CurrentNightState != null && run.CurrentNightState.NightStarted;
        ApplyCurrentNightPlan();
        Game.SaveGameState();
        Game.ContinueRun();
    }

    public void DebugStartNight()
    {
        OnNightStarted();
    }

    public void DebugCompleteCurrentNightTasks()
    {
        if (TaskManager.Instance == null)
            return;

        TaskManager.Instance.DebugCompleteAllCurrentNightTasks();
        CanEndNight();
        Game.SaveGameState();
    }

    public void DebugRefreshNightState()
    {
        if (Game.State?.Run?.CurrentNightState == null)
            return;

        Game.State.Run.CurrentNightState.CanEndNight = CanEndNight();
    }

    public void CaptureNightStartSnapshot()
    {
        var run = Game.State?.Run;
        if (run == null)
            return;

        run.NightStartSnapshot = new NightSnapshot
        {
            PlayerPos = playerStartPoint != null ? playerStartPoint.position : run.PlayerPos,
            PlayerRot = playerStartPoint != null ? playerStartPoint.rotation : run.PlayerRot,
            MonsterBrainState = JsonUtility.FromJson<MonsterBrainState>(JsonUtility.ToJson(run.MonsterBrainState ?? new MonsterBrainState())),
            NightState = JsonUtility.FromJson<NightRuntimeState>(JsonUtility.ToJson(run.CurrentNightState ?? new NightRuntimeState())),
        };
    }

    private void ApplyZoneStates(NightPlan plan)
    {
        if (zones == null)
            return;

        foreach (var zone in zones)
        {
            if (zone == null)
                continue;

            bool active = plan.ActiveZoneIds == null || plan.ActiveZoneIds.Count == 0 || plan.ActiveZoneIds.Contains(zone.ZoneId);
            zone.ApplyActiveState(active);
        }
    }
}
