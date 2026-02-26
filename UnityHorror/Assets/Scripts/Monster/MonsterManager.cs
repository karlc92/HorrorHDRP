using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

/// <summary>
/// Monster "director".
/// Owns the saveable MonsterBrainState and pushes high-level intent into MonsterController.
///
/// Refactor goals:
/// - Descriptive update passes (ThreatUpdate, StageUpdate, HintUpdate, ControllerSync).
/// - Keep monster front-stage while engaged (Investigating/Hunting/Killing).
/// - Keep roaming radius/min distance threat-driven.
/// </summary>
public class MonsterManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MonsterController controller;

    [Header("Threat")]
    [Tooltip("Threat decreases by this many points per second (0..100 scale).")]
    [SerializeField] private float threatDecayPerSecond = 2f;

    [Tooltip("If ThreatLevel reaches this value (or above), the monster is automatically sent Front Stage.")]
    [Range(0, 100)]
    [SerializeField] private int frontStageThreshold = 60;

    [Tooltip("If ThreatLevel reaches this value (or below), the monster is automatically sent Back Stage.")]
    [Range(0, 100)]
    [SerializeField] private int backStageThreshold = 10;

    [Header("Roaming Scaling (Threat -> Roam)")]
    [SerializeField] private float roamRadiusAtThreat0 = 18f;
    [SerializeField] private float roamRadiusAtThreat100 = 6f;
    [SerializeField] private float minRoamDistanceAtThreat0 = 4f;
    [SerializeField] private float minRoamDistanceAtThreat100 = 1.25f;

    [Header("Player Location Hint (Threat -> Accuracy)")]
    [SerializeField] private float maxHintErrorDistance = 25f;
    [SerializeField] private float minHintErrorDistance = 0f;
    [SerializeField] private float hintUpdateInterval = 0.75f;
    [SerializeField] private float hintSmoothing = 6f;

    [Header("Back Stage")]
    [SerializeField] private float backstageDistance = 60f;
    [SerializeField] private float backstageMinDistance = 45f;
    [SerializeField] private int backstagePickAttempts = 20;

    [Tooltip("If true, requires backstage points to be connected (path complete) from the monster's current position.")]
    [SerializeField] private bool backstageRequireConnected = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;
    [SerializeField] private bool debugDraw = true;

    private PlayerController player;

    // Smooth accumulator, persisted as int 0..100.
    private float threatValue;

    private float nextHintAt;
    private Vector3 hintTarget;
    private Vector3 currentBackstageTarget;

    private void Awake()
    {
        EnsureBrainState();

        if (!controller)
            controller = GetComponent<MonsterController>() ?? GetComponentInChildren<MonsterController>() ?? GetComponentInParent<MonsterController>();

        if (!controller)
        {
            Debug.LogError("[MonsterManager] No MonsterController found. Disabling.");
            enabled = false;
            return;
        }

        player = FindFirstObjectByType<PlayerController>();

        threatValue = Mathf.Clamp(Game.State.MonsterBrainState.ThreatLevel, 0f, 100f);
        Game.State.MonsterBrainState.ThreatLevel = Mathf.Clamp(Game.State.MonsterBrainState.ThreatLevel, 0, 100);

        // NOTE: Vector3.zero sentinel kept for compatibility.
        if (Game.State.MonsterBrainState.PlayerLocationHint == Vector3.zero)
            Game.State.MonsterBrainState.PlayerLocationHint = player ? player.transform.position : controller.transform.position;

        hintTarget = Game.State.MonsterBrainState.PlayerLocationHint;
        nextHintAt = Time.time;

        if (!Game.State.MonsterBrainState.MonsterFrontStage && Game.State.MonsterBrainState.MonsterBackstageIdle)
        {
            controller.ForceBackstageIdle(player ? player.transform.position : controller.transform.position);
            currentBackstageTarget = controller.transform.position;
        }
        else
        {
            ApplyStage(immediate: true);
        }

        ControllerSync();
    }

    private void Update()
    {
        if (!player)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (!player) return;
        }

        bool engaged = IsMonsterEngaged();

        ThreatUpdate(engaged);
        StageUpdate(engaged);
        HintUpdate();
        ControllerSync();

        Game.State.MonsterBrainState.MonsterBackstageIdle = controller.state == MonsterController.MonsterActionState.BackstageIdle;

        if (debugDraw)
            DebugDraw();

        if (Keyboard.current != null && Keyboard.current[Key.P].wasPressedThisFrame)
            AddThreat(10f);
    }

    private void FixedUpdate()
    {
        if (!controller) return;
        EnsureBrainState();

        Game.State.MonsterBrainState.MonsterPosition = controller.transform.position;
        Game.State.MonsterBrainState.MonsterRotation = controller.transform.rotation;
    }

    public void ApplyLoadedState()
    {
        if (Game.State == null || Game.State.MonsterBrainState == null || !controller) return;

        if (!player)
            player = FindFirstObjectByType<PlayerController>();

        threatValue = Mathf.Clamp(Game.State.MonsterBrainState.ThreatLevel, 0f, 100f);
        Game.State.MonsterBrainState.ThreatLevel = Mathf.Clamp(Game.State.MonsterBrainState.ThreatLevel, 0, 100);

        hintTarget = Game.State.MonsterBrainState.PlayerLocationHint;
        nextHintAt = Time.time;

        if (!Game.State.MonsterBrainState.MonsterFrontStage && Game.State.MonsterBrainState.MonsterBackstageIdle)
        {
            controller.ForceBackstageIdle(player ? player.transform.position : controller.transform.position);
            currentBackstageTarget = controller.transform.position;
        }
        else
        {
            ApplyStage(immediate: true);
        }

        ControllerSync();
    }

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
    // Update passes
    // -----------------------------
    private bool IsMonsterEngaged()
    {
        return controller.state == MonsterController.MonsterActionState.Investigating
            || controller.state == MonsterController.MonsterActionState.Hunting
            || controller.state == MonsterController.MonsterActionState.Killing;
    }

    private void ThreatUpdate(bool engaged)
    {
        if (engaged)
        {
            threatValue = Mathf.Max(threatValue, frontStageThreshold);
            Game.State.MonsterBrainState.ThreatLevel = Mathf.Max(Game.State.MonsterBrainState.ThreatLevel, frontStageThreshold);
            return;
        }

        if (threatDecayPerSecond <= 0f) return;

        threatValue = Mathf.Clamp(threatValue - threatDecayPerSecond * Time.deltaTime, 0f, 100f);
        int asInt = Mathf.Clamp(Mathf.RoundToInt(threatValue), 0, 100);

        if (asInt != Game.State.MonsterBrainState.ThreatLevel)
            Game.State.MonsterBrainState.ThreatLevel = asInt;
    }

    private void StageUpdate(bool engaged)
    {
        if (engaged)
        {
            if (!Game.State.MonsterBrainState.MonsterFrontStage)
            {
                Game.State.MonsterBrainState.MonsterFrontStage = true;
                if (debugLog) Debug.Log("[MonsterManager] Engaged => forcing Front Stage");
            }
            return;
        }

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

    private void HintUpdate()
    {
        if (!player) return;

        float threat01 = Mathf.Clamp01(Game.State.MonsterBrainState.ThreatLevel / 100f);
        float error = Mathf.Lerp(maxHintErrorDistance, minHintErrorDistance, threat01);

        if (Time.time >= nextHintAt)
        {
            nextHintAt = Time.time + Mathf.Max(0.05f, hintUpdateInterval);

            Vector3 playerPos = player.transform.position;
            Vector2 off2 = Random.insideUnitCircle * error;
            Vector3 noisy = playerPos + new Vector3(off2.x, 0f, off2.y);

            hintTarget = ProjectToNavmeshNearPlayer(noisy, playerPos);
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

    private void ControllerSync()
    {
        if (!controller || !player) return;

        float t = Mathf.Clamp01(Game.State.MonsterBrainState.ThreatLevel / 100f);

        float radius = Mathf.Lerp(roamRadiusAtThreat0, roamRadiusAtThreat100, t);
        float minDist = Mathf.Lerp(minRoamDistanceAtThreat0, minRoamDistanceAtThreat100, t);

        radius = Mathf.Max(0f, radius);
        minDist = Mathf.Clamp(minDist, 0f, radius);

        controller.roamRadius = radius;
        controller.minRoamDistance = minDist;

        controller.SetRoamCenter(Game.State.MonsterBrainState.PlayerLocationHint);

        controller.frontstageRevealDistance = backstageDistance;
        controller.backstageMinDistanceFromPlayer = backstageMinDistance;

        if (Game.State.MonsterBrainState.MonsterFrontStage)
        {
            if (controller.WantsBackstage || controller.IsBackstage)
                controller.SendFrontstage(player.transform.position);
        }
        else
        {
            if (!controller.WantsBackstage)
            {
                currentBackstageTarget = PickBackstageDestination(controller.transform.position, player.transform.position);
                controller.SendBackstage(currentBackstageTarget);
            }
        }
    }

    private void ApplyStage(bool immediate)
    {
        if (!player || !controller) return;

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

    private void DebugDraw()
    {
        if (!player || !controller) return;

        Debug.DrawLine(controller.transform.position, Game.State.MonsterBrainState.PlayerLocationHint, Color.yellow);

        if (!Game.State.MonsterBrainState.MonsterFrontStage)
            Debug.DrawLine(controller.transform.position, currentBackstageTarget, Color.cyan);
    }

    private static void EnsureBrainState()
    {
        if (Game.State != null && Game.State.MonsterBrainState == null)
            Game.State.MonsterBrainState = new MonsterBrainState();
    }

    // -----------------------------
    // Nav helpers (hint + backstage picking)
    // -----------------------------
    private static bool TryGetNavPoint(Vector3 p, float maxDistance, out Vector3 navPoint)
    {
        if (NavMesh.SamplePosition(p, out var hit, Mathf.Max(0.01f, maxDistance), NavMesh.AllAreas))
        {
            navPoint = hit.position;
            return true;
        }

        navPoint = default;
        return false;
    }

    private static bool IsPathComplete(Vector3 fromWorld, Vector3 toWorld)
    {
        if (!TryGetNavPoint(fromWorld, 6f, out var from)) return false;
        if (!TryGetNavPoint(toWorld, 6f, out var to)) return false;

        var path = new NavMeshPath();
        if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path))
            return false;

        return path.status == NavMeshPathStatus.PathComplete;
    }

    private Vector3 ProjectToNavmeshNearPlayer(Vector3 p, Vector3 playerPos)
    {
        if (TryGetNavPoint(playerPos, 6f, out var playerNav))
        {
            if (TryGetNavPoint(p, 12f, out var nav) && IsPathComplete(playerNav, nav))
                return nav;

            for (int i = 0; i < 6; i++)
            {
                Vector2 off2 = Random.insideUnitCircle * 6f;
                Vector3 cand = p + new Vector3(off2.x, 0f, off2.y);
                if (TryGetNavPoint(cand, 12f, out var nav2) && IsPathComplete(playerNav, nav2))
                    return nav2;
            }

            return playerNav;
        }

        if (TryGetNavPoint(p, 12f, out var any))
            return any;

        return p;
    }

    private Vector3 PickBackstageDestination(Vector3 monsterPos, Vector3 playerPos)
    {
        if (!TryGetNavPoint(monsterPos, 20f, out var monsterNav) || !TryGetNavPoint(playerPos, 20f, out var playerNav))
        {
            Vector2 dir2 = Random.insideUnitCircle;
            if (dir2.sqrMagnitude < 0.0001f) dir2 = Vector2.right;
            dir2.Normalize();
            return playerPos + new Vector3(dir2.x, 0f, dir2.y) * backstageDistance;
        }

        for (int i = 0; i < Mathf.Max(1, backstagePickAttempts); i++)
        {
            Vector2 dir2 = Random.insideUnitCircle;
            if (dir2.sqrMagnitude < 0.0001f) dir2 = Vector2.right;
            dir2.Normalize();

            float dist = backstageDistance * Random.Range(0.85f, 1.15f);
            Vector3 candidate = playerNav + new Vector3(dir2.x, 0f, dir2.y) * dist;

            if (!TryGetNavPoint(candidate, 12f, out var navCandidate))
                continue;

            float flatDist = Vector3.Distance(
                new Vector3(navCandidate.x, 0f, navCandidate.z),
                new Vector3(playerPos.x, 0f, playerPos.z));

            if (flatDist < backstageMinDistance)
                continue;

            if (backstageRequireConnected && !IsPathComplete(monsterNav, navCandidate))
                continue;

            return navCandidate;
        }

        Vector3 away = monsterNav - playerNav;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = Vector3.forward;
        away.Normalize();

        Vector3 fallback = playerNav + away * backstageDistance;

        if (TryGetNavPoint(fallback, 20f, out var navFallback))
            return navFallback;

        return fallback;
    }
}