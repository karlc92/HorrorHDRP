using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Animancer;

/// <summary>
/// State-driven monster controller (Unity AI Navigation / NavMeshAgent).
///
/// Refactor goals:
/// - Single, explicit state machine.
/// - No stuck detection / detour logic.
/// - Per-frame updates grouped into descriptive passes:
///   PerceptionUpdate -> StateTransitionUpdate -> MovementUpdate -> AnimationUpdate -> AudioUpdate -> PresentationUpdate.
/// - LOS is based on TWO raycasts:
///   (1) to playerTargetOverride (or player.raycastTarget if override is null)
///   (2) to player.transform.position
///   Player is considered visible only if BOTH raycasts hit the player.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class MonsterController : MonoBehaviour
{
    public enum MonsterActionState
    {
        Idle,
        Roaming,
        Investigating,
        Hunting,
        Emote,
        Killing,
        BackstageTravel,
        BackstageIdle
    }

    [Header("Runtime State")]
    public MonsterActionState state = MonsterActionState.Roaming;

    [Header("References")]
    public PlayerController player;

    [Tooltip("Optional: LOS raycast (1) target override. If null, uses PlayerController.raycastTarget. Raycast (2) always uses player.transform.position.")]
    public Transform playerTargetOverride;

    [Header("Animation (Animancer)")]
    public AnimationClip idleClip;
    public AnimationClip runningClip;
    public AnimationClip killingClip;
    public List<AnimationClip> emoteClips = new();

    [Header("Audio")]
    public AudioClip huntingBreatheAudio;
    public List<AudioClip> emoteAudioSound = new();
    public AudioClip killingSfx;
    public float killingSfxAt = 1.0f;
    public float killingSfxVolume = 1.0f;
    public bool killingSfxRandomPitch = true;

    [Header("Movement")]
    [Tooltip("Base speed for Roaming/Investigating/BackstageTravel.")]
    public float speed = 3.5f;

    [Tooltip("Multiplier applied during Hunting.")]
    public float sprintMultiplier = 2.0f;

    [Tooltip("If true, NavMeshAgent rotates the monster toward its steering target.")]
    public bool useAgentRotation = true;

    [Tooltip("How quickly we rotate if not using agent rotation.")]
    public float turnSpeed = 6f;

    [Header("Agent Tuning")]
    public float agentAcceleration = 120f;
    public float agentAngularSpeed = 1080f;
    public ObstacleAvoidanceType agentAvoidance = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
    public bool autoBrakingWhenNotHunting = true;
    public float arriveDistance = 1.2f;
    public float huntingStoppingDistance = 0.1f;

    [Header("Roaming")]
    [Tooltip("Roaming destinations are picked around this point (typically MonsterManager's PlayerLocationHint).")]
    public Vector3 roamCenter;
    public float roamRadius = 12f;
    public float minRoamDistance = 3f;
    public int roamPickAttempts = 12;

    [Header("Perception")]
    public float chaseDistance = 6f;
    public float killDistance = 1.5f;
    public LayerMask losMask = ~0;
    public int monsterLayer = 7;

    [Tooltip("If true, trigger colliders block LOS raycasts.")]
    public bool losTreatTriggersAsOccluders = true;

    [Tooltip("How often LOS is evaluated (seconds).")]
    public float perceptionInterval = 0.05f;

    [Tooltip("Grace time after LOS breaks (seconds) to reduce flicker.")]
    public float losGrace = 0.2f;

    [Header("Investigating")]
    [Tooltip("How long we will keep moving to the last known position after LOS breaks.")]
    public float investigateTimeout = 4f;

    [Header("Emotes")]
    [Tooltip("Chance per second (only while Roaming).")]
    public float emoteChancePerSecond = 0.03f;

    [Tooltip("If <= 0, uses chosen emote clip length.")]
    public float emoteDuration = 1.5f;

    [Header("Director / Stage")]
    [SerializeField] private bool wantsBackstage = false;
    public bool WantsBackstage => wantsBackstage;

    [Header("Backstage Presentation")]
    [SerializeField] private bool hideMeshesWhileBackstage = true;

    [Tooltip("When coming frontstage while too close to the player, teleport to ~this distance before revealing.")]
    public float frontstageRevealDistance = 60f;

    [Range(0f, 0.5f)]
    public float frontstageRevealDistanceJitter = 0.15f;

    public int frontstageTeleportAttempts = 20;
    public bool frontstageTeleportRequireSameNavAreaAsPlayer = true;

    [Tooltip("Minimum distance (XZ) from the player required before the monster will enter BackstageIdle. If <= 0, uses frontstageRevealDistance.")]
    public float backstageMinDistanceFromPlayer = 0f;

    [Header("Killing Sequence")]
    public float killingSlowStart = 1.0f;
    public float killingSlowEnd = 2.0f;
    public float killingSlowSpeed = 0.25f;
    public float killingNormalSpeed = 1.0f;
    public float killingEndTime = 3.15f;

    [Header("Debug")]
    public bool debug = false;

    // --- Components / caches ---
    [SerializeField] private NavMeshAgent agent;
    private AnimancerComponent animancer;
    private AudioSource audioSource;

    private Renderer[] cachedRenderers;
    private Collider[] cachedColliders;

    // --- Perception runtime ---
    private float nextPerceptionAt;
    private bool hasLos;
    private float lastLosAt;
    private Vector3 lastKnownPlayerPos;

    // --- State runtime ---
    private Vector3 roamDestination;
    private Vector3 investigateTarget;
    private float investigateEndsAt;
    private float emoteEndsAt;
    private Vector3 backstageDestination;

    // When Hunting starts from BackstageTravel, losing LOS returns to BackstageTravel (not Investigating).
    private MonsterActionState huntingReturnState = MonsterActionState.Roaming;

    // Destination throttling.
    private float nextDestinationUpdateAt;

    [Header("Navigation")]
    [Tooltip("How often we are allowed to call SetDestination while roaming/backstage (seconds).")]
    public float destinationUpdateInterval = 0.15f;

    [Tooltip("How often we are allowed to call SetDestination while hunting (seconds).")]
    public float destinationUpdateIntervalHunt = 0.05f;

    // LOS raycast scratch.
    private readonly RaycastHit[] rayHits = new RaycastHit[16];

    // Killing runtime.
    private float killingStartedAt;
    private bool killingSfxPlayed;

    // Animancer cache.
    private AnimationClip lastClip;
    private float lastAnimSpeed;

    private struct Perception
    {
        public Vector3 PlayerPos;
        public float DistanceToPlayer;
        public bool InChaseRange;
        public bool InKillRange;
        public bool HasLOS;
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animancer = GetComponentInChildren<AnimancerComponent>();
        audioSource = GetComponent<AudioSource>();

        agent.updateRotation = useAgentRotation;
        agent.autoRepath = true;
        agent.acceleration = Mathf.Max(1f, agentAcceleration);
        agent.angularSpeed = Mathf.Max(60f, agentAngularSpeed);
        agent.obstacleAvoidanceType = agentAvoidance;

        CachePresentationObjects();
        EnsureOnNavMesh();

        roamDestination = transform.position;
        backstageDestination = transform.position;
        lastKnownPlayerPos = transform.position;

        // Prevent initial grace from incorrectly giving LOS at time ~0.
        lastLosAt = -999f;

        PresentationUpdate();
    }

    void Update()
    {
        if (!player)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (!player) return;
        }

        EnsureOnNavMesh();

        var perception = PerceptionUpdate();

        // BackstageIdle is hard override: hidden, no collision, no movement.
        if (state == MonsterActionState.BackstageIdle)
        {
            StopAgent();
            AnimationUpdate(perception);
            AudioUpdate(perception);
            PresentationUpdate();
            return;
        }

        // Enter killing if in range (and not already killing).
        if (state != MonsterActionState.Killing && perception.InKillRange)
        {
            EnterKilling();
        }

        if (state == MonsterActionState.Killing)
        {
            KillingUpdate(perception);
            PresentationUpdate();
            return;
        }

        StateTransitionUpdate(perception);
        MovementUpdate(perception);
        AnimationUpdate(perception);
        AudioUpdate(perception);
        PresentationUpdate();
    }

    // -----------------------------
    // Director API (MonsterManager)
    // -----------------------------
    public void SetRoamCenter(Vector3 center, bool forceNewDestination = false)
    {
        roamCenter = center;
        if (forceNewDestination) ForcePickNewRoamDestination();
    }

    public void ForcePickNewRoamDestination()
    {
        roamDestination = transform.position;
        nextDestinationUpdateAt = 0f;
        agent.ResetPath();
    }

    public void ApplySavedPose(Vector3 position, Quaternion rotation)
    {
        TeleportTo(position);
        transform.rotation = rotation;
    }

    public void ForceBackstageIdle(Vector3 _playerPos)
    {
        wantsBackstage = true;
        if (state == MonsterActionState.Killing) return;

        backstageDestination = transform.position;
        SetState(MonsterActionState.BackstageIdle);
        StopAgent();
        PresentationUpdate();
    }

    public void SendBackstage(Vector3 target)
    {
        wantsBackstage = true;
        if (state == MonsterActionState.Killing) return;

        backstageDestination = ProjectToNavmesh(target);
        SetState(MonsterActionState.BackstageTravel);
    }

    public void SendFrontstage(Vector3 playerPos)
    {
        wantsBackstage = false;

        bool wasBackstage = IsBackstage;
        if (!wasBackstage) return;

        // If we're hidden and too close when going frontstage, relocate before revealing.
        if (ShouldHideMeshesInState(state))
        {
            float d = Mathf.Max(0f, frontstageRevealDistance);
            if (d > 0.01f && SqrXZ(transform.position, playerPos) < d * d)
            {
                if (TryPickFrontstageTeleportPoint(playerPos, out var picked))
                    TeleportTo(picked);
                else
                    TeleportTo(ProjectToNavmesh(playerPos + FlatDirAway(transform.position, playerPos) * d));
            }
        }

        SetState(MonsterActionState.Roaming);
        ForcePickNewRoamDestination();
    }

    public bool IsBackstage => state == MonsterActionState.BackstageTravel || state == MonsterActionState.BackstageIdle;

    // -----------------------------
    // Perception
    // -----------------------------
    private Perception PerceptionUpdate()
    {
        Vector3 monsterPos = transform.position;
        Vector3 playerPos = player.transform.position;

        Vector3 toPlayer = playerPos - monsterPos;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        bool inChase = dist <= chaseDistance;
        bool inKill = dist <= killDistance;

        if (Time.time >= nextPerceptionAt)
        {
            nextPerceptionAt = Time.time + Mathf.Max(0.01f, perceptionInterval);
            bool losNow = HasLineOfSight(monsterPos);
            Debug.Log("Has LOS? " + losNow);

            if (losNow)
            {
                hasLos = true;
                lastLosAt = Time.time;
                lastKnownPlayerPos = ProjectToNavmesh(player.transform.position);
            }
            else
            {
                // Grace to prevent flicker.
                hasLos = (Time.time - lastLosAt) <= Mathf.Max(0f, losGrace);
            }
        }

        // If we are currently seeing them, keep lastKnownPlayerPos fresh even between perception ticks.
        if (hasLos)
            lastKnownPlayerPos = ProjectToNavmesh(player.transform.position);

        return new Perception
        {
            PlayerPos = player.transform.position,
            DistanceToPlayer = dist,
            InChaseRange = inChase,
            InKillRange = inKill,
            HasLOS = hasLos
        };
    }

    /// <summary>
    /// LOS is TRUE only if raycast hit the player:
    /// Ray to playerTargetOverride (or player.raycastTarget when override is null)
    /// </summary>
    private bool HasLineOfSight(Vector3 monsterPos)
    {
        if (!player) return false;

        Transform primaryTarget = playerTargetOverride != null ? playerTargetOverride : (player.raycastTarget != null ? player.raycastTarget : null);
        Vector3 p1 = primaryTarget != null ? primaryTarget.position : player.transform.position;
        Vector3 p2 = player.transform.position;

        Vector3 eye = monsterPos + Vector3.up * Mathf.Max(0.6f, agent.height * 0.8f);

        return RaycastHitsPlayer(eye, p1);// && RaycastHitsPlayer(eye, p2);
    }

    private bool RaycastHitsPlayer(Vector3 origin, Vector3 targetPoint)
    {
        Vector3 dir = targetPoint - origin;
        float dist = dir.magnitude;
        if (dist <= 0.01f) return true;
        dir /= dist;

        int mask = losMask & ~(1 << monsterLayer);
        var triggerMode = losTreatTriggersAsOccluders ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

        int hitCount = Physics.RaycastNonAlloc(origin, dir, rayHits, dist, mask, triggerMode);
        if (hitCount <= 0)
            return false; // If we didn't hit anything, we also didn't "hit the player".

        float best = float.PositiveInfinity;
        RaycastHit bestHit = default;
        bool found = false;

        // NonAlloc hits are not guaranteed sorted.
        for (int i = 0; i < hitCount; i++)
        {
            var h = rayHits[i];
            if (h.collider == null) continue;
            if (h.transform == transform || h.transform.IsChildOf(transform))
                continue;

            if (h.distance < best)
            {
                best = h.distance;
                bestHit = h;
                found = true;
            }
        }

        if (!found) return false;

        return bestHit.transform == player.transform || bestHit.transform.IsChildOf(player.transform);
    }

    // -----------------------------
    // State machine
    // -----------------------------
    private void StateTransitionUpdate(Perception p)
    {
        switch (state)
        {
            case MonsterActionState.Idle:
                if (p.InChaseRange && p.HasLOS)
                    EnterHunting(returnTo: MonsterActionState.Idle);
                break;

            case MonsterActionState.Roaming:
                if (p.InChaseRange && p.HasLOS)
                {
                    EnterHunting(returnTo: MonsterActionState.Roaming);
                    break;
                }

                if (p.InChaseRange && !p.HasLOS)
                {
                    // No LOS but within chase range: investigate current hint center (treated as last known position).
                    EnterInvestigating(target: roamCenter, timeout: investigateTimeout);
                    break;
                }

                // Emote chance only during Roaming.
                if (emoteClips.Count > 0 && Random.value < emoteChancePerSecond * Time.deltaTime)
                    EnterEmote();
                break;

            case MonsterActionState.Emote:
                if (p.InChaseRange && p.HasLOS)
                {
                    EnterHunting(returnTo: MonsterActionState.Roaming);
                    break;
                }

                if (Time.time >= emoteEndsAt)
                    SetState(MonsterActionState.Roaming);
                break;

            case MonsterActionState.Investigating:
                if (p.InChaseRange && p.HasLOS)
                {
                    EnterHunting(returnTo: MonsterActionState.Roaming);
                    break;
                }

                if (Time.time >= investigateEndsAt || HasArrived())
                    SetState(MonsterActionState.Roaming);
                break;

            case MonsterActionState.Hunting:
                if (p.HasLOS)
                {
                    lastKnownPlayerPos = ProjectToNavmesh(p.PlayerPos);
                }
                else
                {
                    if (huntingReturnState == MonsterActionState.BackstageTravel)
                        SetState(MonsterActionState.BackstageTravel);
                    else
                        EnterInvestigating(target: lastKnownPlayerPos, timeout: investigateTimeout);
                }
                break;

            case MonsterActionState.BackstageTravel:
                if (p.InChaseRange && p.HasLOS)
                {
                    EnterHunting(returnTo: MonsterActionState.BackstageTravel);
                    break;
                }

                if (HasArrived())
                {
                    float required = GetBackstageRequiredDistance();
                    if (required <= 0.01f || SqrXZ(transform.position, p.PlayerPos) >= required * required)
                        SetState(MonsterActionState.BackstageIdle);
                    else
                        backstageDestination = ProjectToNavmesh(p.PlayerPos + FlatDirAway(transform.position, p.PlayerPos) * required);
                }
                break;
        }
    }

    private void EnterHunting(MonsterActionState returnTo)
    {
        huntingReturnState = returnTo;
        SetState(MonsterActionState.Hunting);
    }

    private void EnterInvestigating(Vector3 target, float timeout)
    {
        investigateTarget = ProjectToNavmesh(target);
        investigateEndsAt = Time.time + Mathf.Max(0.1f, timeout);
        SetState(MonsterActionState.Investigating);
    }

    private void EnterEmote()
    {
        if (emoteClips.Count <= 0)
            return;

        SetState(MonsterActionState.Emote);

        var clip = emoteClips[Random.Range(0, emoteClips.Count)];
        if (clip)
        {
            PlayClip(clip, 1f);
            float d = emoteDuration > 0f ? emoteDuration : clip.length;
            emoteEndsAt = Time.time + Mathf.Max(0f, d);

            if (emoteAudioSound.Count > 0)
                PlayOneShot(emoteAudioSound[Random.Range(0, emoteAudioSound.Count)], 1f, randomPitch: true);
        }
        else
        {
            emoteEndsAt = Time.time + Mathf.Max(0f, emoteDuration);
        }
    }

    private void EnterKilling()
    {
        SetState(MonsterActionState.Killing);
        killingStartedAt = Time.time;
        killingSfxPlayed = false;
        StopAgent();

        if (player != null)
            player.isInDeathSequence = true;
    }

    private void SetState(MonsterActionState next)
    {
        if (state == next) return;

        state = next;
        if (debug) Debug.Log($"[Monster] State -> {state}");

        // Reset throttles when we change states (avoids "why isn't it moving" after transitions).
        nextDestinationUpdateAt = 0f;

        if (state == MonsterActionState.Roaming)
            agent.ResetPath();

        PresentationUpdate();
    }

    // -----------------------------
    // Movement
    // -----------------------------
    private void MovementUpdate(Perception _p)
    {
        switch (state)
        {
            case MonsterActionState.Idle:
            case MonsterActionState.Emote:
            case MonsterActionState.Killing:
            case MonsterActionState.BackstageIdle:
                StopAgent();
                return;

            case MonsterActionState.Roaming:
                if (HasArrived() || !agent.hasPath)
                {
                    if (TryPickRoamDestination(transform.position, roamCenter, out var picked))
                        roamDestination = picked;
                }
                MoveAgentTo(roamDestination, speed);
                break;

            case MonsterActionState.Investigating:
                MoveAgentTo(investigateTarget, speed * 1.1f);
                break;

            case MonsterActionState.Hunting:
                MoveAgentTo(lastKnownPlayerPos, speed * sprintMultiplier);
                break;

            case MonsterActionState.BackstageTravel:
                MoveAgentTo(backstageDestination, speed * 0.9f);
                break;
        }

        if (!useAgentRotation)
            ManualRotationUpdate();
    }

    private void ManualRotationUpdate()
    {
        Vector3 face = agent.desiredVelocity;
        face.y = 0f;
        if (face.sqrMagnitude < 0.0001f)
            return;

        face.Normalize();
        var targetRot = Quaternion.LookRotation(face);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
    }

    private void MoveAgentTo(Vector3 dest, float desiredSpeed)
    {
        if (!agent.enabled) return;

        bool huntingLike = state == MonsterActionState.Hunting;

        agent.updateRotation = useAgentRotation;
        agent.isStopped = false;
        agent.speed = Mathf.Max(0.1f, desiredSpeed);
        agent.autoBraking = !huntingLike && autoBrakingWhenNotHunting;
        agent.stoppingDistance = Mathf.Max(0.05f, huntingLike ? huntingStoppingDistance : arriveDistance);

        float interval = huntingLike ? destinationUpdateIntervalHunt : destinationUpdateInterval;
        if (Time.time < nextDestinationUpdateAt)
            return;
        nextDestinationUpdateAt = Time.time + Mathf.Max(0.02f, interval);

        agent.SetDestination(ProjectToNavmesh(dest));
    }

    private void StopAgent()
    {
        if (!agent.enabled) return;
        agent.isStopped = true;
        agent.ResetPath();
    }

    private bool HasArrived()
    {
        if (!agent.enabled) return true;
        if (agent.pathPending) return false;
        if (!agent.hasPath) return true;
        return agent.remainingDistance <= Mathf.Max(0.05f, arriveDistance);
    }

    // -----------------------------
    // Animation / audio / presentation
    // -----------------------------
    private void AnimationUpdate(Perception _p)
    {
        if (state == MonsterActionState.Killing)
            return;

        if (state == MonsterActionState.Emote)
            return;

        bool moving = agent.enabled && agent.velocity.sqrMagnitude > 0.05f;
        bool sprinting = state == MonsterActionState.Hunting;

        var clip = moving ? runningClip : idleClip;
        float animSpeed = 1f;
        if (moving && sprinting && clip == runningClip) animSpeed = 1.5f;

        if (clip) PlayClip(clip, animSpeed);
    }

    private void AudioUpdate(Perception _p)
    {
        bool shouldBreathe = state == MonsterActionState.Hunting;
        PlayHuntBreathing(loop: shouldBreathe);
    }

    private void PresentationUpdate()
    {
        bool hide = ShouldHideMeshesInState(state);
        bool disableCollision = state == MonsterActionState.BackstageIdle;

        if (cachedRenderers != null)
        {
            foreach (var r in cachedRenderers)
            {
                if (!r) continue;
                if (r is ParticleSystemRenderer) continue;
                r.enabled = !hide;
            }
        }

        if (cachedColliders != null)
        {
            foreach (var c in cachedColliders)
            {
                if (!c) continue;
                if (c.isTrigger) continue;
                c.enabled = !disableCollision;
            }
        }
    }

    private void KillingUpdate(Perception p)
    {
        StopAgent();

        float t = Time.time - killingStartedAt;

        float animSpeed = killingNormalSpeed;
        if (t >= killingSlowStart && t < killingSlowEnd)
            animSpeed = killingSlowSpeed;

        if (killingClip)
            PlayClip(killingClip, animSpeed);

        if (!killingSfxPlayed && killingSfx && t >= killingSfxAt)
        {
            killingSfxPlayed = true;
            PlayOneShot(killingSfx, killingSfxVolume, killingSfxRandomPitch, 0f);
        }

        Vector3 toPlayer = p.PlayerPos - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.001f)
        {
            var targetRot = Quaternion.LookRotation(toPlayer.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
        }

        if (t > killingEndTime)
        {
            var gameUI = FindFirstObjectByType<GameUI>();
            if (gameUI != null)
                gameUI.ShowDeadUI();
        }
    }

    private void CachePresentationObjects()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        cachedColliders = GetComponentsInChildren<Collider>(true);
    }

    private bool ShouldHideMeshesInState(MonsterActionState s) =>
        hideMeshesWhileBackstage && s == MonsterActionState.BackstageIdle;

    // -----------------------------
    // NavMesh helpers
    // -----------------------------
    private void EnsureOnNavMesh()
    {
        if (agent.isOnNavMesh) return;
        if (TryGetNavPoint(transform.position, 10f, out var navPos))
        {
            agent.Warp(navPos);
            agent.ResetPath();
        }
    }

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

    private Vector3 ProjectToNavmesh(Vector3 p)
    {
        if (TryGetNavPoint(p, 8f, out var nav))
            return nav;
        return p;
    }

    private bool TryPickRoamDestination(Vector3 currentPos, Vector3 center, out Vector3 picked)
    {
        float radius = Mathf.Max(0f, roamRadius);
        float minD = Mathf.Clamp(minRoamDistance, 0f, radius);

        Vector3 centerNav = ProjectToNavmesh(center);

        for (int i = 0; i < Mathf.Max(1, roamPickAttempts); i++)
        {
            Vector2 off2 = Random.insideUnitCircle * radius;
            Vector3 candidate = centerNav + new Vector3(off2.x, 0f, off2.y);
            candidate = ProjectToNavmesh(candidate);

            float d = Mathf.Sqrt(SqrXZ(candidate, currentPos));
            if (d < minD) continue;

            if (!IsPathComplete(currentPos, candidate))
                continue;

            picked = candidate;
            return true;
        }

        picked = centerNav;
        return true;
    }

    private bool TryPickFrontstageTeleportPoint(Vector3 playerPos, out Vector3 picked)
    {
        Vector3 playerNav = ProjectToNavmesh(playerPos);

        Vector3 preferredDir = Vector3.forward;
        if (player != null)
        {
            preferredDir = -player.transform.forward;
            preferredDir.y = 0f;
        }
        if (preferredDir.sqrMagnitude < 0.0001f) preferredDir = Vector3.forward;
        preferredDir.Normalize();

        float baseDist = Mathf.Max(0f, frontstageRevealDistance);
        float jitter = Mathf.Clamp01(frontstageRevealDistanceJitter);
        float minDist = baseDist * (1f - jitter);
        float maxDist = baseDist * (1f + jitter);

        for (int i = 0; i < Mathf.Max(1, frontstageTeleportAttempts); i++)
        {
            float ang = Random.Range(-70f, 70f);
            Vector3 dir = Quaternion.Euler(0f, ang, 0f) * preferredDir;
            float dist = Random.Range(minDist, maxDist);
            Vector3 candidate = playerNav + dir * dist;

            if (!TryGetNavPoint(candidate, 12f, out var navCandidate))
                continue;

            if (frontstageTeleportRequireSameNavAreaAsPlayer)
            {
                if (!IsPathComplete(playerNav, navCandidate))
                    continue;
            }

            if (SqrXZ(navCandidate, playerPos) < (killDistance * killDistance))
                continue;

            picked = navCandidate;
            return true;
        }

        picked = default;
        return false;
    }

    private float GetBackstageRequiredDistance()
    {
        return backstageMinDistanceFromPlayer > 0.01f ? backstageMinDistanceFromPlayer : frontstageRevealDistance;
    }

    private void TeleportTo(Vector3 p)
    {
        if (TryGetNavPoint(p, 12f, out var navPos))
            agent.Warp(navPos);
        else
            transform.position = p;

        agent.ResetPath();
        roamDestination = transform.position;
        nextDestinationUpdateAt = 0f;
    }

    // -----------------------------
    // Animancer helpers
    // -----------------------------
    private void PlayHuntBreathing(bool loop)
    {
        if (!audioSource || !huntingBreatheAudio) return;

        if (!loop)
        {
            if (audioSource.clip == huntingBreatheAudio && audioSource.isPlaying)
            {
                audioSource.Stop();
                audioSource.loop = false;
            }
            return;
        }

        if (audioSource.clip == huntingBreatheAudio && audioSource.isPlaying && audioSource.loop)
            return;

        audioSource.Stop();
        audioSource.volume = 1f * Game.Settings.MasterVolume;
        audioSource.loop = true;
        audioSource.clip = huntingBreatheAudio;
        audioSource.spatialBlend = 1f;
        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.Play();
    }

    private void PlayClip(AnimationClip clip, float animSpeed)
    {
        if (!animancer || !clip) return;

        if (clip != lastClip)
        {
            var s = animancer.Play(clip, 0.15f);
            s.Speed = animSpeed;
            lastClip = clip;
            lastAnimSpeed = animSpeed;
        }
        else if (!Mathf.Approximately(animSpeed, lastAnimSpeed))
        {
            var s = animancer.States.GetOrCreate(clip);
            s.Speed = animSpeed;
            lastAnimSpeed = animSpeed;
        }
    }

    private void PlayOneShot(AudioClip clip, float volume = 1f, bool randomPitch = true, float spatialBlend = 1f)
    {
        if (!audioSource || !clip) return;

        float oldPitch = audioSource.pitch;
        if (randomPitch) audioSource.pitch = Random.Range(0.95f, 1.05f);

        audioSource.spatialBlend = spatialBlend;
        audioSource.PlayOneShot(clip, volume * Game.Settings.MasterVolume);

        audioSource.pitch = oldPitch;
    }

    // -----------------------------
    // Utils
    // -----------------------------
    private static float SqrXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    private static Vector3 FlatDirAway(Vector3 from, Vector3 target)
    {
        Vector3 v = from - target;
        v.y = 0f;
        if (v.sqrMagnitude < 0.0001f)
            v = Vector3.forward;
        return v.normalized;
    }
}