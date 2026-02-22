using Pathfinding;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// "Director" / brain driver for the monster.
/// Owns a serializable monster brain state (saveable as JSON) and pushes levers on MonsterController.
/// </summary>
public class MonsterManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MonsterController controller;

    [Header("Threat")]
    [Tooltip("Threat decreases by this many points per second (converted into an int 0..100).")]
    [SerializeField] private float threatDecayPerSecond = 2f;

    [Tooltip("If ThreatLevel reaches this value (or above), the monster is automatically sent Front Stage.")]
    [Range(0, 100)]
    [SerializeField] private int frontStageThreshold = 60;

    [Tooltip("If ThreatLevel reaches this value (or below), the monster is automatically sent Back Stage.")]
    [Range(0, 100)]
    [SerializeField] private int backStageThreshold = 10;

    [Header("Roaming Scaling (Threat -> Roam)")]
    [Tooltip("Roam radius when ThreatLevel is 0 (low threat = wider roaming).")]
    [SerializeField] private float roamRadiusAtThreat0 = 18f;

    [Tooltip("Roam radius when ThreatLevel is 100 (high threat = tight roaming near the hint).")]
    [SerializeField] private float roamRadiusAtThreat100 = 6f;

    [Tooltip("Min roam distance when ThreatLevel is 0.")]
    [SerializeField] private float minRoamDistanceAtThreat0 = 4f;

    [Tooltip("Min roam distance when ThreatLevel is 100.")]
    [SerializeField] private float minRoamDistanceAtThreat100 = 1.25f;

    [Header("Player Location Hint (Threat -> Accuracy)")]
    [Tooltip("Max error distance (Threat=0).")]
    [SerializeField] private float maxHintErrorDistance = 25f;

    [Tooltip("Min error distance (Threat=100).")]
    [SerializeField] private float minHintErrorDistance = 0f;

    [Tooltip("How often the monster 're-estimates' the player position (seconds).")]
    [SerializeField] private float hintUpdateInterval = 0.75f;

    [Tooltip("How quickly the hint blends toward the newest estimate.")]
    [SerializeField] private float hintSmoothing = 6f;

    [Header("Back Stage")]
    [Tooltip("How far away (approx) to park the monster when sent Back Stage.")]
    [SerializeField] private float backstageDistance = 60f;

    [Tooltip("Minimum acceptable distance from the player for a backstage point.")]
    [SerializeField] private float backstageMinDistance = 45f;

    [Tooltip("How many attempts to find a suitable backstage point.")]
    [SerializeField] private int backstagePickAttempts = 20;

    [Tooltip("If true, picks backstage points on the same navmesh area as the monster.")]
    [SerializeField] private bool backstageRequireSameNavArea = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;
    [SerializeField] private bool debugDraw = true;

    private PlayerController player;
    private float threatValue;
    private float nextHintAt;
    private Vector3 hintTarget;
    private Vector3 currentBackstageTarget;

    void Awake()
    {
        if (Game.State != null && Game.State.MonsterBrainState == null)
            Game.State.MonsterBrainState = new MonsterBrainState();

        if (!controller)
            controller = GetComponent<MonsterController>() ?? GetComponentInChildren<MonsterController>() ?? GetComponentInParent<MonsterController>();

        if (!controller)
        {
            Debug.LogError("[MonsterManager] No MonsterController found. Disabling.");
            enabled = false;
            return;
        }

        player = FindFirstObjectByType<PlayerController>();

        // Initialize internal float threat accumulator from state (so decay is smooth).
        threatValue = Mathf.Clamp(Game.State.MonsterBrainState.ThreatLevel, 0f, 100f);
        Game.State.MonsterBrainState.ThreatLevel = Mathf.Clamp(Game.State.MonsterBrainState.ThreatLevel, 0, 100);

        // If the hint hasn't been set yet, seed it to something reasonable.
        if (Game.State.MonsterBrainState.PlayerLocationHint == Vector3.zero)
        {
            Game.State.MonsterBrainState.PlayerLocationHint = player ? player.transform.position : controller.transform.position;
        }

        hintTarget = Game.State.MonsterBrainState.PlayerLocationHint;
        nextHintAt = Time.time;

        // Apply initial stage.
        // If this was loaded while the monster was already parked backstage and hidden,
        // force that state so it doesn't briefly appear walking.
        if (!Game.State.MonsterBrainState.MonsterFrontStage && Game.State.MonsterBrainState.MonsterBackstageIdle)
        {
            controller.ForceBackstageIdle(player ? player.transform.position : controller.transform.position);
            currentBackstageTarget = controller.transform.position;
        }
        else
        {
            ApplyStage(immediate: true);
        }
        PushLevers();
    }

    void Update()
    {
        if (!player)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (!player) return;
        }

        DecayThreat();
        AutoStageTransitions();
        UpdatePlayerLocationHint();
        PushLevers();

        // Persist the monster's *actual* parked backstage state for save/load.
        // (This is separate from MonsterFrontStage, which is the director's intent.)
        if (Game.State != null && Game.State.MonsterBrainState != null)
            Game.State.MonsterBrainState.MonsterBackstageIdle = controller.state == MonsterController.MonsterActionState.BackstageIdle;

        if (debugDraw && player)
        {
            Debug.DrawLine(controller.transform.position, Game.State.MonsterBrainState.PlayerLocationHint, Color.yellow);
            if (!Game.State.MonsterBrainState.MonsterFrontStage)
                Debug.DrawLine(controller.transform.position, currentBackstageTarget, Color.cyan);
        }

        if (Keyboard.current != null && Keyboard.current[Key.P].wasPressedThisFrame)
            AddThreat(10f);

        if (Keyboard.current != null && Keyboard.current[Key.L].wasPressedThisFrame)
            Game.SaveGame(Game.State.Slot);
    }

    /// <summary>
    /// Called after a save has been loaded while already in the gameplay scene.
    /// Resyncs internal caches (threatValue, hint timers) and applies the loaded stage immediately.
    /// </summary>
    public void ApplyLoadedState()
    {
        if (Game.State == null || Game.State.MonsterBrainState == null || !controller) return;

        if (!player)
            player = FindFirstObjectByType<PlayerController>();

        threatValue = Mathf.Clamp(Game.State.MonsterBrainState.ThreatLevel, 0f, 100f);
        Game.State.MonsterBrainState.ThreatLevel = Mathf.Clamp(Game.State.MonsterBrainState.ThreatLevel, 0, 100);

        hintTarget = Game.State.MonsterBrainState.PlayerLocationHint;
        nextHintAt = Time.time;

        // Restore stage.
        if (!Game.State.MonsterBrainState.MonsterFrontStage && Game.State.MonsterBrainState.MonsterBackstageIdle)
        {
            controller.ForceBackstageIdle(player ? player.transform.position : controller.transform.position);
            currentBackstageTarget = controller.transform.position;
        }
        else
        {
            ApplyStage(immediate: true);
        }

        PushLevers();
    }

    void FixedUpdate()
    {
        if (!controller) return;

        if (Game.State != null && Game.State.MonsterBrainState == null)
            Game.State.MonsterBrainState = new MonsterBrainState();

        if (Game.State == null || Game.State.MonsterBrainState == null) return;

        Game.State.MonsterBrainState.MonsterPosition = controller.transform.position;
        Game.State.MonsterBrainState.MonsterRotation = controller.transform.rotation;
    }

    // -----------------------------
    // External API (other systems)
    // -----------------------------

    public void AddThreat(float amount)
    {
        threatValue = Mathf.Clamp(threatValue + amount, 0f, 100f);
        Game.State.MonsterBrainState.ThreatLevel = Mathf.Clamp(Mathf.RoundToInt(threatValue), 0, 100);
    }

    public void SetThreat(int value)
    {
        threatValue = Mathf.Clamp(value, 0f, 100f);
        Game.State.MonsterBrainState.ThreatLevel = Mathf.Clamp(value, 0, 100);
    }

    public void SendFrontStage()
    {
        Game.State.MonsterBrainState.MonsterFrontStage = true;
        ApplyStage(immediate: false);
    }

    public void SendBackStage()
    {
        Game.State.MonsterBrainState.MonsterFrontStage = false;
        ApplyStage(immediate: false);
    }


    // -----------------------------
    // Brain logic
    // -----------------------------

    private void DecayThreat()
    {
        if (threatDecayPerSecond <= 0f) return;

        threatValue = Mathf.Clamp(threatValue - threatDecayPerSecond * Time.deltaTime, 0f, 100f);
        int asInt = Mathf.Clamp(Mathf.RoundToInt(threatValue), 0, 100);

        if (asInt != Game.State.MonsterBrainState.ThreatLevel)
            Game.State.MonsterBrainState.ThreatLevel = asInt;
    }

    private void AutoStageTransitions()
    {
        // Hysteresis (60 up, 10 down) to avoid ping-pong.
        if (!Game.State.MonsterBrainState.MonsterFrontStage && Game.State.MonsterBrainState.ThreatLevel >= frontStageThreshold)
        {
            if (debugLog) Debug.Log($"[MonsterManager] Threat {Game.State.MonsterBrainState.ThreatLevel} >= {frontStageThreshold} => Front Stage");
            SendFrontStage();
        }
        else if (Game.State.MonsterBrainState.MonsterFrontStage && Game.State.MonsterBrainState.ThreatLevel <= backStageThreshold)
        {
            if (debugLog) Debug.Log($"[MonsterManager] Threat {Game.State.MonsterBrainState.ThreatLevel} <= {backStageThreshold} => Back Stage");
            SendBackStage();
        }
    }

    private void UpdatePlayerLocationHint()
    {
        float t = Mathf.Clamp01(Game.State.MonsterBrainState.ThreatLevel / 100f);
        float error = Mathf.Lerp(maxHintErrorDistance, minHintErrorDistance, t);

        if (Time.time >= nextHintAt)
        {
            nextHintAt = Time.time + Mathf.Max(0.05f, hintUpdateInterval);

            Vector3 playerPos = player.transform.position;

            // Add planar noise (XZ) that shrinks as threat rises.
            Vector2 off2 = Random.insideUnitCircle * error;
            Vector3 noisy = playerPos + new Vector3(off2.x, 0f, off2.y);

            // Keep the hint on walkable space if possible (helps roaming).
            hintTarget = ProjectToWalkable(noisy);
        }

        if (hintSmoothing <= 0f)
        {
            Game.State.MonsterBrainState.PlayerLocationHint = hintTarget;
        }
        else
        {
            float alpha = 1f - Mathf.Exp(-hintSmoothing * Time.deltaTime);
            Game.State.MonsterBrainState.PlayerLocationHint = Vector3.Lerp(Game.State.MonsterBrainState.PlayerLocationHint, hintTarget, alpha);
        }
    }

    private void PushLevers()
    {
        // Threat -> roam parameters
        float t = Mathf.Clamp01(Game.State.MonsterBrainState.ThreatLevel / 100f);

        float radius = Mathf.Lerp(roamRadiusAtThreat0, roamRadiusAtThreat100, t);
        float minDist = Mathf.Lerp(minRoamDistanceAtThreat0, minRoamDistanceAtThreat100, t);

        // Keep minDist sane relative to radius.
        radius = Mathf.Max(0f, radius);
        minDist = Mathf.Clamp(minDist, 0f, radius);

        controller.roamRadius = radius;
        controller.minRoamDistance = minDist;

        // Drive roaming center from hint whenever we're not hunting (controller handles this).
        controller.SetRoamCenter(Game.State.MonsterBrainState.PlayerLocationHint);

        // Stage intent.
        // Note: The monster can temporarily enter Hunting even while "backstage" (e.g. if the player runs into it).
        // We therefore only send stage commands when the intent changes, not based on the controller's current state.
        controller.frontstageRevealDistance = backstageDistance;
        controller.backstageMinDistanceFromPlayer = backstageMinDistance;

        if (Game.State.MonsterBrainState.MonsterFrontStage)
        {
            if (controller.WantsBackstage || controller.IsBackstage)
                controller.SendFrontstage(player.transform.position);
        }
        else
        {
            // Only pick a backstage target once per backstage intent.
            if (!controller.WantsBackstage)
            {
                currentBackstageTarget = PickBackstageDestination(controller.transform.position, player.transform.position);
                controller.SendBackstage(currentBackstageTarget);
            }
        }
    }

    private void ApplyStage(bool immediate)
    {
        if (!player) return;

        // Keep controller distances in sync with our director parameters.
        controller.frontstageRevealDistance = backstageDistance;
        controller.backstageMinDistanceFromPlayer = backstageMinDistance;

        if (Game.State.MonsterBrainState.MonsterFrontStage)
        {
            controller.SendFrontstage(player.transform.position);
        }
        else
        {
            currentBackstageTarget = PickBackstageDestination(controller.transform.position, player.transform.position);
            controller.SendBackstage(currentBackstageTarget);
        }

        if (immediate)
            controller.ForcePickNewRoamDestination();
    }

    // -----------------------------
    // Helpers
    // -----------------------------

    private Vector3 ProjectToWalkable(Vector3 p)
    {
        if (!AstarPath.active) return p;
        var nn = AstarPath.active.GetNearest(p, NNConstraint.Walkable);
        return nn.position;
    }

    private Vector3 PickBackstageDestination(Vector3 monsterPos, Vector3 playerPos)
    {
        // Fallback if we have no A* graph.
        if (!AstarPath.active)
        {
            Vector2 dir2 = Random.insideUnitCircle;
            if (dir2.sqrMagnitude < 0.0001f) dir2 = Vector2.right;
            dir2.Normalize();
            return playerPos + new Vector3(dir2.x, 0f, dir2.y) * backstageDistance;
        }

        var start = AstarPath.active.GetNearest(monsterPos, NNConstraint.Walkable);

        uint requiredArea = start.node != null ? start.node.Area : uint.MaxValue;

        for (int i = 0; i < Mathf.Max(1, backstagePickAttempts); i++)
        {
            // Random direction with small distance jitter.
            Vector2 dir2 = Random.insideUnitCircle;
            if (dir2.sqrMagnitude < 0.0001f) dir2 = Vector2.right;
            dir2.Normalize();

            float dist = backstageDistance * Random.Range(0.85f, 1.15f);
            Vector3 candidate = playerPos + new Vector3(dir2.x, 0f, dir2.y) * dist;

            var nn = AstarPath.active.GetNearest(candidate, NNConstraint.Walkable);
            if (nn.node == null || !nn.node.Walkable) continue;

            if (backstageRequireSameNavArea && start.node != null && nn.node.Area != requiredArea)
                continue;

            float d = Vector3.Distance(new Vector3(nn.position.x, 0f, nn.position.z), new Vector3(playerPos.x, 0f, playerPos.z));
            if (d < backstageMinDistance) continue;

            return nn.position;
        }

        // Worst-case fallback: walkable projection of directly-away-from-player.
        Vector3 away = monsterPos - playerPos;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = Vector3.forward;
        away.Normalize();

        Vector3 fallback = playerPos + away * backstageDistance;
        return ProjectToWalkable(fallback);
    }
}
