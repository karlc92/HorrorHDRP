using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Animancer;

/// <summary>
/// Monster locomotion + behavior controller (Unity AI Navigation / NavMeshAgent).
///
/// Key behavior fixes:
/// - No "Hunt -> Idle" thrash when in range but LOS fails: explicit Investigating state.
/// - No "path through walls": NavMeshAgent owns all movement (no direct steering).
/// - Less "slippery" feel: high acceleration + high angular speed + agent-driven rotation, constant-speed chase.
/// - No "give up while still seeing player": hunting never yields to director/backstage and does not self-abort on stuck if LOS is true.
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

    [Tooltip("Optional: overrides which transform is used for LOS + chase destination. If null, uses PlayerController.raycastTarget then player.transform.")]
    public Transform playerTargetOverride;

    [Header("Animation (Animancer)")]
    public AnimationClip idleClip;
    public AnimationClip runningClip;
    public AnimationClip killingClip;

    [Header("Audio")]
    public AudioClip huntingBreatheAudio;
    public List<AudioClip> emoteAudioSound = new();
    public List<AnimationClip> emoteClips = new();

    [Header("Movement")]
    [Tooltip("Base speed when roaming.")]
    public float speed = 3.5f;

    [Tooltip("Speed multiplier during Hunting and certain Investigate modes.")]
    public float sprintMultiplier = 2.0f;

    [Tooltip("How quickly the monster rotates toward its movement/target direction (only used if useAgentRotation = false).")]
    public float turnSpeed = 4f;

    [Header("Agent Tuning")]
    [Tooltip("If true, NavMeshAgent rotates the monster toward its steering target. This feels much less slippery than custom slerp rotation.")]
    public bool useAgentRotation = true;

    [Tooltip("Higher = reaches desired speed faster (snappier, less sliding).")]
    public float agentAcceleration = 120f;

    [Tooltip("Degrees/sec. Higher = turns tighter while moving.")]
    public float agentAngularSpeed = 1080f;

    public ObstacleAvoidanceType agentAvoidance = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

    [Tooltip("If true, the agent brakes near non-hunt destinations (roam/backstage). Hunting/investigating disables braking for constant speed.")]
    public bool autoBrakingWhenNotHunting = true;

    [Tooltip("Stopping distance during hunting (keep small to avoid slowing too early).")]
    public float huntingStoppingDistance = 0.1f;

    [Tooltip("If NOT using agent rotation, we only face the player within this distance during hunting to avoid sideways sliding.")]
    public float faceTargetWithinDistance = 2.5f;

    [Header("Roaming")]
    [Tooltip("When not Hunting, Roaming destinations are picked around this point (typically the PlayerLocationHint).")]
    public Vector3 roamCenter;

    public float roamRadius = 12f;
    public float minRoamDistance = 3f;
    public int roamPickAttempts = 12;

    [Header("Perception / Combat")]
    public float chaseDistance = 6f;
    public float killDistance = 1.5f;

    [Header("Navigation")]
    [Tooltip("Consider ourselves 'arrived' when remainingDistance <= this.")]
    public float arriveDistance = 1.2f;

    [Tooltip("How often we are allowed to call SetDestination while roaming/backstage (seconds).")]
    public float destinationUpdateInterval = 0.15f;

    [Tooltip("How often we are allowed to call SetDestination while hunting/investigating (seconds). Lower = tighter tracking, more CPU.")]
    public float destinationUpdateIntervalHunt = 0.05f;

    [Header("Line of Sight")]
    public LayerMask losMask = ~0;
    public int monsterLayer = 7;

    [Tooltip("Sphere radius used for LOS checks. Helps prevent seeing through thin walls / thin geometry.")]
    public float losProbeRadius = 0.08f;

    [Tooltip("If true, trigger colliders will block LOS checks (useful if walls/doors use triggers).")]
    public bool losTreatTriggersAsOccluders = true;

    [Tooltip("Grace period after LOS breaks during a chase, to avoid flicker.")]
    public float losGrace = 0.5f;

    [Header("Investigate / Hunt Memory")]
    [Tooltip("If we enter Investigating from Roaming (in range but no LOS), we will pursue a best-guess point for up to this long.")]
    public float investigateFromRoamDuration = 2.0f;

    [Tooltip("If we lose LOS during a hunt, we will investigate last seen position for up to this long.")]
    public float lostHuntTimeout = 4f;

    [Tooltip("Player considered off-navmesh if nearest nav point is further than this (XZ).")]
    public float offMeshTolerance = 1.5f;

    [Header("Emotes / Idling")]
    public float emoteChancePerSecond = 0.03f;

    // If <= 0, falls back to selected emote clip length.
    public float emoteDuration = 1.5f;

    // When the monster gives up hunting, it idles for this long, then returns to Roaming (or resumes BackstageTravel).
    public float idleTimeAfterHunting = 1.0f;

    [Header("Cooldowns")]
    [Tooltip("Prevent immediate re-hunt ping-pong after giving up a hunt where the player was actually seen.")]
    public float giveUpCooldown = 0.75f;

    [Tooltip("Longer cooldown when we gave up without ever acquiring LOS (e.g., baiting behind a wall).")]
    public float giveUpCooldownNoLos = 2.0f;

    [Header("Stuck Handling")]
    [Tooltip("Seconds of near-zero progress before attempting an unstuck.")]
    public float stuckTime = 0.8f;

    [Tooltip("Cooldown between unstuck attempts (seconds).")]
    public float unstuckCooldown = 0.5f;

    [Tooltip("How far we probe to detect an obstacle for detour selection.")]
    public float unstuckProbeDistance = 1.2f;

    [Tooltip("How far we try to step away for a detour.")]
    public float unstuckDetourDistance = 3.0f;

    public int maxStuckDetours = 3;

    [Header("Director / Stage")]
    [Tooltip("If true, the monster will resume BackstageTravel after giving up a hunt.")]
    [SerializeField] private bool wantsBackstage = false;
    public bool WantsBackstage => wantsBackstage;

    [Header("Backstage Presentation")]
    [SerializeField] private bool hideMeshesWhileBackstage = true;

    [Tooltip("When coming frontstage while too close to the player, teleport to ~this distance before revealing.")]
    public float frontstageRevealDistance = 60f;

    [Range(0f, 0.5f)]
    public float frontstageRevealDistanceJitter = 0.15f;

    public int frontstageTeleportAttempts = 20;

    [Tooltip("If true, the re-entry teleport point must be connected to the player's navmesh (path complete).")]
    public bool frontstageTeleportRequireSameNavAreaAsPlayer = true;

    [Tooltip("Minimum distance (XZ) from the player required before the monster will enter BackstageIdle. If <= 0, uses frontstageRevealDistance.")]
    public float backstageMinDistanceFromPlayer = 0f;

    [SerializeField] private bool enforceBackstageDistance = true;
    public int backstagePickAttempts = 20;

    [Header("Killing Sequence")]
    public float killingSlowStart = 1.0f;
    public float killingSlowEnd = 2.0f;
    public float killingSlowSpeed = 0.25f;
    public float killingNormalSpeed = 1.0f;
    public float killingEndTime = 3.15f;

    public AudioClip killingSfx;
    public float killingSfxAt = 1.0f;
    public float killingSfxVolume = 1.0f;
    public bool killingSfxRandomPitch = true;

    [Header("Debug")]
    public bool debug = false;
    public float debugInterval = 0.5f;

    // --- Components / caches ---
    [SerializeField] private NavMeshAgent agent;
    private AnimancerComponent animancer;
    private AudioSource audioSource;

    private Renderer[] cachedRenderers;
    private Collider[] cachedColliders;

    // --- Destination ---
    private Vector3 destination;
    private float nextDestinationUpdateAt;

    // --- State timers ---
    private float emoteEndAt;
    private float idleEndAt;
    private MonsterActionState idleNext = MonsterActionState.Roaming;

    // --- Hunt memory ---
    private Vector3 lastSeenNavPos;
    private float lastLosAt = -999f;
    private bool hadLosThisHunt;

    // Investigate state control
    private Vector3 investigateTarget;
    private float investigateEndAt;
    private bool investigateEndsInPostHunt;

    // Give-up cooldown bookkeeping
    private float gaveUpAt = -999f;
    private bool lastGiveUpHadLos = true;

    // Backstage destination
    private Vector3 backstageDestination;
    private float nextBackstageRepickAt;
    private const float BackstageRepickCooldown = 0.25f;

    // Stuck handling
    private Vector3 lastPos;
    private float stuckFor;
    private float lastUnstuckAt;
    private int stuckDetours;

    // LOS cache (throttled)
    private readonly RaycastHit[] losHits = new RaycastHit[16];
    private float nextLosCheckAt;
    private bool cachedLos;
    private bool cachedPlayerNavOk;
    private Vector3 cachedPlayerNavPos;
    private bool cachedPlayerOffMesh;

    // Kill sequence
    private float killingStartedAt;
    private bool killingSfxPlayed;

    // Animancer cache
    private AnimationClip lastClip;
    private float lastAnimSpeed;

    private float nextDebugAt;

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

        destination = transform.position;
        lastPos = transform.position;

        lastSeenNavPos = transform.position;
        ApplyBackstagePresentationForState(state);
    }

    void Update()
    {
        if (!player)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (!player) return;
        }

        EnsureOnNavMesh();

        var pos = transform.position;
        var playerPos = player.transform.position;
        Transform rt = playerTargetOverride ? playerTargetOverride : (player.raycastTarget ? player.raycastTarget : player.transform);

        // Update cached LOS + player nav position at a throttled rate.
        UpdatePerceptionCache(pos, rt);

        Vector3 toPlayer = playerPos - pos;
        toPlayer.y = 0f;
        float dPlayer = toPlayer.magnitude;

        // Backstage idle is a hard override: invisible, no collisions, no movement.
        if (state == MonsterActionState.BackstageIdle)
        {
            ApplyBackstagePresentationForState(state);
            StopAgent();
            PlayLocomotion(false, sprinting: false);
            return;
        }

        // Ensure visible/collidable in all other states.
        ApplyBackstagePresentationForState(state);

        // Killing takes over.
        if (dPlayer <= killDistance)
        {
            if (state != MonsterActionState.Killing)
                SetState(MonsterActionState.Killing, playerPos);
        }
        else if (state == MonsterActionState.Killing)
        {
            SetState(MonsterActionState.Roaming, playerPos);
        }

        if (state == MonsterActionState.Killing)
        {
            KillingSequence(toPlayer);
            return;
        }

        // Emote state ticks.
        if (state == MonsterActionState.Emote)
        {
            StopAgent();
            if (Time.time >= emoteEndAt)
                SetState(MonsterActionState.Roaming, playerPos);

            PlayLocomotion(false, sprinting: false);
            return;
        }

        // Idle (post-hunt) ticks.
        if (state == MonsterActionState.Idle)
        {
            StopAgent();
            if (Time.time >= idleEndAt)
                SetState(idleNext, playerPos);

            PlayLocomotion(false, sprinting: false);
            return;
        }

        // Backstage travel ticks.
        if (state == MonsterActionState.BackstageTravel)
        {
            bool inRange = IsInHuntRange(dPlayer);
            if (inRange && cachedLos && CanChasePointBeReached(pos, rt.position))
                SetState(MonsterActionState.Hunting, playerPos);

            destination = ProjectToNavmesh(backstageDestination, preferConnectedTo: pos);
            MoveAgentTo(destination, speed * 0.9f);

            if (HasArrived())
            {
                float required = GetBackstageRequiredDistance();
                if (enforceBackstageDistance && required > 0.01f && SqrXZ(destination, playerPos) < required * required)
                {
                    if (Time.time >= nextBackstageRepickAt)
                    {
                        nextBackstageRepickAt = Time.time + BackstageRepickCooldown;
                        RepickBackstageDestination(pos, playerPos, required);
                    }
                }
                else
                {
                    SetState(MonsterActionState.BackstageIdle, playerPos);
                    StopAgent();
                    PlayLocomotion(false, sprinting: false);
                    return;
                }
            }

            TickStuck(pos, wantsToMove: !HasArrived());
            TickRotation(rt, agent.desiredVelocity);
            PlayLocomotion(agent.velocity.sqrMagnitude > 0.01f, sprinting: false);
            return;
        }

        // Main frontstage logic.
        bool inHuntRange = IsInHuntRange(dPlayer);

        if (state == MonsterActionState.Roaming)
        {
            // If we can see and can path, go hunt.
            if (inHuntRange && cachedLos && CanChasePointBeReached(pos, rt.position))
            {
                SetState(MonsterActionState.Hunting, playerPos);
            }
            else if (inHuntRange && !cachedLos && cachedPlayerNavOk)
            {
                // In range but LOS blocked: investigate toward a reachable point near the player.
                Vector3 target = ProjectToNavmesh(playerPos, preferConnectedTo: pos);
                EnterInvestigate(target, Time.time + Mathf.Max(0.1f, investigateFromRoamDuration), endsInPostHunt: false);
            }
            else
            {
                // Emote chance.
                if (emoteClips.Count > 0 && Random.value < emoteChancePerSecond * Time.deltaTime)
                {
                    StartEmote();
                    return;
                }

                if (HasArrived())
                    PickNewRoamDestination(pos);
            }
        }

        // Hunting tick.
        if (state == MonsterActionState.Hunting)
        {
            if (!cachedLos)
            {
                bool grace = hadLosThisHunt && (Time.time - lastLosAt) <= losGrace;

                // Keep moving to last seen during grace (prevents stop->stuck->idle).
                destination = lastSeenNavPos;
                MoveAgentTo(destination, speed * sprintMultiplier);

                if (!grace)
                    EnterInvestigate(lastSeenNavPos, Time.time + Mathf.Max(0.1f, lostHuntTimeout), endsInPostHunt: true);
            }
            else
            {
                hadLosThisHunt = true;
                lastLosAt = Time.time;

                // Chase a connected nav point near the player's current position.
                lastSeenNavPos = ProjectToNavmesh(rt.position, preferConnectedTo: pos);
                destination = lastSeenNavPos;
                MoveAgentTo(destination, speed * sprintMultiplier);
            }

            // Safety: if we arrive at last seen without LOS, go post-hunt idle.
            if (HasArrived() && !cachedLos)
            {
                EnterPostHuntIdle(playerPos);
                return;
            }

            // If navmesh path becomes invalid but LOS is true, keep pressure (reset path so we repath next tick).
            if (cachedLos && !agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                agent.ResetPath();
                nextDestinationUpdateAt = 0f;
            }

            TickStuck(pos, wantsToMove: !HasArrived());
            TickRotation(rt, agent.desiredVelocity);
            PlayLocomotion(agent.velocity.sqrMagnitude > 0.01f, sprinting: true);
            PlayHuntBreathing(loop: true);
            return;
        }

        // Investigating tick.
        if (state == MonsterActionState.Investigating)
        {
            if (cachedLos && CanChasePointBeReached(pos, rt.position))
            {
                SetState(MonsterActionState.Hunting, playerPos);
            }
            else
            {
                destination = investigateTarget;
                MoveAgentTo(destination, speed * 1.1f);

                bool timeUp = Time.time >= investigateEndAt;
                if ((HasArrived() || timeUp) && !cachedLos)
                {
                    if (investigateEndsInPostHunt)
                    {
                        EnterPostHuntIdle(playerPos);
                    }
                    else
                    {
                        MarkGaveUp(hadLos: false);
                        SetState(MonsterActionState.Roaming, playerPos);
                        ForcePickNewRoamDestination();
                    }
                    return;
                }
            }

            TickStuck(pos, wantsToMove: !HasArrived());
            TickRotation(rt, agent.desiredVelocity);
            PlayLocomotion(agent.velocity.sqrMagnitude > 0.01f, sprinting: false);
            PlayHuntBreathing(loop: false);
            return;
        }

        // Default roaming movement (keeps agent updated even if destination comes from outside).
        MoveAgentTo(destination, speed);
        TickStuck(pos, wantsToMove: !HasArrived());
        TickRotation(rt, agent.desiredVelocity);
        PlayLocomotion(agent.velocity.sqrMagnitude > 0.01f, sprinting: false);
        PlayHuntBreathing(loop: false);

        if (debug && Time.time >= nextDebugAt)
        {
            nextDebugAt = Time.time + debugInterval;
            Debug.Log($"[Monster] state={state} los={cachedLos} hadLosThisHunt={hadLosThisHunt} remDist={(agent.pathPending ? -1f : agent.remainingDistance):F2} status={agent.pathStatus}");
        }
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
        destination = transform.position;
        agent.ResetPath();
        nextDestinationUpdateAt = 0f;
    }

    public void ApplySavedPose(Vector3 position, Quaternion rotation)
    {
        TeleportTo(position);
        transform.rotation = rotation;
    }

    public void ForceBackstageIdle(Vector3 playerPos)
    {
        wantsBackstage = true;
        if (state == MonsterActionState.Killing) return;

        backstageDestination = transform.position;
        nextBackstageRepickAt = 0f;

        SetState(MonsterActionState.BackstageIdle, playerPos);
        StopAgent();
        PlayLocomotion(false, sprinting: false);
    }

    public void SendBackstage(Vector3 target)
    {
        wantsBackstage = true;
        if (state == MonsterActionState.Killing) return;

        Vector3 playerPos = player ? player.transform.position : transform.position;
        float required = GetBackstageRequiredDistance();

        if (enforceBackstageDistance && required > 0.01f)
        {
            bool ok = SqrXZ(target, playerPos) >= required * required;
            target = ProjectToNavmesh(target, preferConnectedTo: transform.position);

            if (!ok && TryPickBackstageDestination(transform.position, playerPos, required, out var picked))
                target = picked;
        }

        backstageDestination = target;
        nextBackstageRepickAt = 0f;

        SetState(MonsterActionState.BackstageTravel, playerPos);
    }

    public void SendFrontstage(Vector3 playerPos)
    {
        wantsBackstage = false;

        if (state == MonsterActionState.Idle && idleNext == MonsterActionState.BackstageTravel)
            idleNext = MonsterActionState.Roaming;

        bool wasBackstage = IsBackstageState(state);
        if (!wasBackstage) return;

        bool wasHidden = ShouldHideMeshesInState(state);

        if (wasHidden)
        {
            float d = Mathf.Max(0f, frontstageRevealDistance);
            if (d > 0.01f && SqrXZ(transform.position, playerPos) < d * d)
            {
                if (TryPickFrontstageTeleportPoint(playerPos, out var picked))
                    TeleportTo(picked);
                else
                {
                    Vector3 away = transform.position - playerPos;
                    away.y = 0f;
                    if (away.sqrMagnitude < 0.0001f) away = Vector3.forward;
                    away.Normalize();
                    TeleportTo(ProjectToNavmesh(playerPos + away * d, preferConnectedTo: playerPos));
                }
            }
        }

        SetState(MonsterActionState.Roaming, playerPos);
        ForcePickNewRoamDestination();
    }

    public bool IsBackstage => state == MonsterActionState.BackstageTravel || state == MonsterActionState.BackstageIdle;

    // -----------------------------
    // State transitions
    // -----------------------------
    private void SetState(MonsterActionState next, Vector3 playerPos)
    {
        if (state == next) return;

        state = next;

        if (debug) Debug.Log($"[Monster] Now {state}");

        switch (state)
        {
            case MonsterActionState.Roaming:
                hadLosThisHunt = false;
                PlayHuntBreathing(loop: false);
                break;

            case MonsterActionState.Hunting:
                // Only mark as "had LOS" if we really have it now.
                hadLosThisHunt = cachedLos;
                if (cachedLos) lastLosAt = Time.time;
                lastSeenNavPos = ProjectToNavmesh(playerPos, preferConnectedTo: transform.position);
                stuckDetours = 0;
                break;

            case MonsterActionState.BackstageTravel:
                stuckDetours = 0;
                break;

            case MonsterActionState.BackstageIdle:
                PlayHuntBreathing(loop: false);
                break;

            case MonsterActionState.Idle:
                PlayHuntBreathing(loop: false);
                break;

            case MonsterActionState.Killing:
                killingStartedAt = Time.time;
                killingSfxPlayed = false;

                StopAgent();

                if (player != null)
                    player.isInDeathSequence = true;
                break;
        }

        ApplyBackstagePresentationForState(state);
    }

    private void EnterInvestigate(Vector3 target, float endAt, bool endsInPostHunt)
    {
        investigateTarget = ProjectToNavmesh(target, preferConnectedTo: transform.position);
        investigateEndAt = endAt;
        investigateEndsInPostHunt = endsInPostHunt;

        SetState(MonsterActionState.Investigating, target);

        destination = investigateTarget;
        nextDestinationUpdateAt = 0f;
        MoveAgentTo(destination, speed * 1.1f);
    }

    private void EnterPostHuntIdle(Vector3 playerPos)
    {
        MarkGaveUp(hadLosThisHunt);

        idleEndAt = Time.time + Mathf.Max(0f, idleTimeAfterHunting);
        idleNext = wantsBackstage ? MonsterActionState.BackstageTravel : MonsterActionState.Roaming;

        SetState(MonsterActionState.Idle, playerPos);
        StopAgent();

        PlayLocomotion(false, sprinting: false);
    }

    private void MarkGaveUp(bool hadLos)
    {
        lastGiveUpHadLos = hadLos;
        gaveUpAt = Time.time;
        hadLosThisHunt = false;
    }

    private void StartEmote()
    {
        if (emoteClips.Count <= 0)
            return;

        SetState(MonsterActionState.Emote, player ? player.transform.position : transform.position);

        var clip = emoteClips[Random.Range(0, emoteClips.Count)];
        if (clip)
        {
            PlayClip(clip, 1f);
            float duration = emoteDuration > 0f ? emoteDuration : clip.length;
            emoteEndAt = Time.time + Mathf.Max(0f, duration);

            if (emoteAudioSound.Count > 0)
                PlayOneShot(emoteAudioSound[Random.Range(0, emoteAudioSound.Count)], 1f, randomPitch: true);
        }
        else
        {
            float duration = emoteDuration > 0f ? emoteDuration : 0f;
            emoteEndAt = Time.time + Mathf.Max(0f, duration);
        }
    }

    // -----------------------------
    // Behavior helpers
    // -----------------------------
    private bool IsInHuntRange(float dPlayer)
    {
        float cd = lastGiveUpHadLos ? giveUpCooldown : giveUpCooldownNoLos;
        return dPlayer <= chaseDistance && (Time.time - gaveUpAt) >= cd;
    }

    private bool CanChasePointBeReached(Vector3 monsterPos, Vector3 targetPos)
    {
        Vector3 chaseNav = ProjectToNavmesh(targetPos, preferConnectedTo: monsterPos);
        return IsPathComplete(monsterPos, chaseNav);
    }

    private void PickNewRoamDestination(Vector3 currentPos)
    {
        if (TryPickRoamDestination(currentPos, roamCenter, out var picked))
        {
            destination = picked;
            nextDestinationUpdateAt = 0f;
            MoveAgentTo(destination, speed);
        }
    }

    private bool TryPickRoamDestination(Vector3 currentPos, Vector3 center, out Vector3 picked)
    {
        float radius = Mathf.Max(0f, roamRadius);
        float minD = Mathf.Clamp(minRoamDistance, 0f, radius);

        Vector3 centerNav = ProjectToNavmesh(center, preferConnectedTo: currentPos);

        for (int i = 0; i < Mathf.Max(1, roamPickAttempts); i++)
        {
            Vector2 off2 = Random.insideUnitCircle * radius;
            if (off2.sqrMagnitude < 0.0001f) off2 = Vector2.right;

            Vector3 candidate = centerNav + new Vector3(off2.x, 0f, off2.y);
            candidate = ProjectToNavmesh(candidate, preferConnectedTo: currentPos);

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

    private void RepickBackstageDestination(Vector3 monsterPos, Vector3 playerPos, float requiredDistance)
    {
        if (TryPickBackstageDestination(monsterPos, playerPos, requiredDistance, out var picked))
        {
            backstageDestination = picked;
            destination = backstageDestination;
            agent.ResetPath();
            nextDestinationUpdateAt = 0f;
        }
    }

    private bool TryPickBackstageDestination(Vector3 monsterPos, Vector3 playerPos, float requiredDistance, out Vector3 picked)
    {
        float targetDistance = Mathf.Max(frontstageRevealDistance, requiredDistance, 1f);

        Vector3 monsterNav = ProjectToNavmesh(monsterPos, preferConnectedTo: monsterPos);
        Vector3 playerNav = ProjectToNavmesh(playerPos, preferConnectedTo: playerPos);

        for (int i = 0; i < Mathf.Max(1, backstagePickAttempts); i++)
        {
            Vector2 dir2 = Random.insideUnitCircle;
            if (dir2.sqrMagnitude < 0.0001f) dir2 = Vector2.right;
            dir2.Normalize();

            float dist = targetDistance * Random.Range(0.85f, 1.15f);
            Vector3 candidate = playerNav + new Vector3(dir2.x, 0f, dir2.y) * dist;
            candidate = ProjectToNavmesh(candidate, preferConnectedTo: monsterNav);

            if (Mathf.Sqrt(SqrXZ(candidate, playerPos)) < requiredDistance)
                continue;

            if (!IsPathComplete(monsterNav, candidate))
                continue;

            picked = candidate;
            return true;
        }

        Vector3 away = monsterPos - playerPos;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = Vector3.forward;
        away.Normalize();

        picked = ProjectToNavmesh(playerPos + away * targetDistance, preferConnectedTo: monsterPos);
        return true;
    }

    private bool TryPickFrontstageTeleportPoint(Vector3 playerPos, out Vector3 picked)
    {
        Vector3 playerNav = ProjectToNavmesh(playerPos, preferConnectedTo: playerPos);

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

            if (!TryGetNavPoint(candidate, 8f, out var navCandidate))
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

    private bool TryGetNavPoint(Vector3 p, float maxDistance, out Vector3 navPoint)
    {
        if (NavMesh.SamplePosition(p, out var hit, Mathf.Max(0.01f, maxDistance), NavMesh.AllAreas))
        {
            navPoint = hit.position;
            return true;
        }

        navPoint = default;
        return false;
    }

    private Vector3 ProjectToNavmesh(Vector3 p, Vector3 preferConnectedTo)
    {
        if (!TryGetNavPoint(p, 6f, out var nav))
            return p;

        if (TryGetNavPoint(preferConnectedTo, 6f, out var from) && IsPathComplete(from, nav))
            return nav;

        for (int i = 0; i < 6; i++)
        {
            Vector2 off2 = Random.insideUnitCircle * 4f;
            Vector3 candidate = p + new Vector3(off2.x, 0f, off2.y);

            if (TryGetNavPoint(candidate, 10f, out var nav2) && IsPathComplete(from, nav2))
                return nav2;
        }

        return nav;
    }

    private bool IsPathComplete(Vector3 fromWorld, Vector3 toWorld)
    {
        if (!TryGetNavPoint(fromWorld, 6f, out var from)) return false;
        if (!TryGetNavPoint(toWorld, 6f, out var to)) return false;

        var path = new NavMeshPath();
        if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path))
            return false;

        return path.status == NavMeshPathStatus.PathComplete;
    }

    // -----------------------------
    // Perception
    // -----------------------------
    private void UpdatePerceptionCache(Vector3 monsterPos, Transform target)
    {
        if (Time.time < nextLosCheckAt) return;
        nextLosCheckAt = Time.time + 0.05f;

        cachedLos = HasLineOfSight(monsterPos, target);

        Vector3 playerPos = target ? target.position : (player ? player.transform.position : monsterPos);

        cachedPlayerNavOk = TryGetNavPoint(playerPos, Mathf.Max(0.01f, offMeshTolerance), out cachedPlayerNavPos);
        cachedPlayerOffMesh = true;

        if (cachedPlayerNavOk)
        {
            float d = Mathf.Sqrt(SqrXZ(playerPos, cachedPlayerNavPos));
            cachedPlayerOffMesh = d > offMeshTolerance;
        }

        if (!cachedPlayerNavOk)
            cachedPlayerNavPos = ProjectToNavmesh(playerPos, preferConnectedTo: monsterPos);
    }

    private bool HasLineOfSight(Vector3 monsterPos, Transform target)
    {
        if (!target) return false;

        int mask = losMask & ~(1 << monsterLayer);

        float eyeHeight = Mathf.Max(0.6f, agent.height * 0.8f);
        var eye = monsterPos + Vector3.up * eyeHeight;
        var targetPos = target.position;

        var dir = targetPos - eye;
        float dist = dir.magnitude;
        if (dist <= 0.01f) return true;
        dir /= dist;

        var triggerMode = losTreatTriggersAsOccluders ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

        // Primary: raycast (fewer false negatives).
        int hitCount = Physics.RaycastNonAlloc(eye, dir, losHits, dist, mask, triggerMode);

        if (hitCount <= 0)
            return true;

        float best = float.PositiveInfinity;
        RaycastHit bestHit = default;
        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            var h = losHits[i];
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

        if (!found) return true;

        bool isPlayer =
            bestHit.transform == target ||
            (player != null && bestHit.transform.IsChildOf(player.transform));

        if (isPlayer) return true;

        // Secondary: spherecast for thin occluders (only in closer ranges to reduce corner false-occlusion).
        if (losProbeRadius > 0.001f && dist <= 12f)
        {
            int sc = Physics.SphereCastNonAlloc(eye, losProbeRadius, dir, losHits, dist, mask, triggerMode);
            if (sc <= 0) return true;

            best = float.PositiveInfinity;
            found = false;

            for (int i = 0; i < sc; i++)
            {
                var h = losHits[i];
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

            if (!found) return true;

            isPlayer =
                bestHit.transform == target ||
                (player != null && bestHit.transform.IsChildOf(player.transform));

            return isPlayer;
        }

        return false;
    }

    // -----------------------------
    // Movement / rotation / stuck
    // -----------------------------
    private void MoveAgentTo(Vector3 dest, float desiredSpeed)
    {
        if (!agent.enabled) return;

        bool huntingLike = state == MonsterActionState.Hunting || state == MonsterActionState.Investigating;

        agent.isStopped = false;
        agent.speed = Mathf.Max(0.1f, desiredSpeed);

        agent.autoBraking = !huntingLike && autoBrakingWhenNotHunting;
        agent.stoppingDistance = Mathf.Max(0.05f, state == MonsterActionState.Hunting ? huntingStoppingDistance : arriveDistance);

        float interval = huntingLike ? destinationUpdateIntervalHunt : destinationUpdateInterval;

        if (Time.time < nextDestinationUpdateAt)
            return;

        nextDestinationUpdateAt = Time.time + Mathf.Max(0.02f, interval);

        destination = dest;
        Vector3 navDest = ProjectToNavmesh(destination, preferConnectedTo: transform.position);
        agent.SetDestination(navDest);
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

        // If the path is invalid, treat as "not arrived" so stuck handling can kick in.
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            return false;

        return agent.remainingDistance <= Mathf.Max(0.05f, arriveDistance);
    }

    private void TickRotation(Transform target, Vector3 desiredVelocity)
    {
        if (useAgentRotation) return;

        Vector3 face = desiredVelocity;
        face.y = 0f;

        if (state == MonsterActionState.Hunting && cachedLos && target != null)
        {
            float d = Mathf.Sqrt(SqrXZ(transform.position, target.position));
            if (d <= Mathf.Max(0f, faceTargetWithinDistance))
            {
                face = target.position - transform.position;
                face.y = 0f;
            }
        }

        if (face.sqrMagnitude < 0.0001f)
            return;

        face.Normalize();
        var targetRot = Quaternion.LookRotation(face);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
    }

    private void TickStuck(Vector3 pos, bool wantsToMove)
    {
        if (!wantsToMove)
        {
            stuckFor = 0f;
            stuckDetours = 0;
            lastPos = pos;
            return;
        }

        float movedSqr = SqrXZ(pos, lastPos);
        lastPos = pos;

        bool moving = agent.velocity.sqrMagnitude > 0.05f || movedSqr > 0.02f * 0.02f;

        if (moving)
        {
            stuckFor = 0f;
            stuckDetours = 0;
            return;
        }

        stuckFor += Time.deltaTime;
        if (stuckFor < stuckTime) return;
        if (Time.time - lastUnstuckAt < unstuckCooldown) return;

        lastUnstuckAt = Time.time;
        stuckFor = 0f;

        if (state == MonsterActionState.Hunting)
        {
            // Never abandon a hunt while LOS is true.
            if (cachedLos)
            {
                agent.ResetPath();
                nextDestinationUpdateAt = 0f;
                return;
            }

            // If we're already investigating after LOS loss, let that timeout handle the give up.
            if (hadLosThisHunt && state != MonsterActionState.Investigating)
            {
                EnterInvestigate(lastSeenNavPos, Time.time + Mathf.Max(0.1f, lostHuntTimeout), endsInPostHunt: true);
                return;
            }

            // Otherwise: allow limited detours; if we exhaust detours with no LOS ever, give up.
            if (stuckDetours >= maxStuckDetours && !hadLosThisHunt)
            {
                EnterPostHuntIdle(player ? player.transform.position : pos);
                return;
            }
        }

        if (stuckDetours >= maxStuckDetours)
        {
            agent.ResetPath();
            stuckDetours = 0;
            return;
        }

        stuckDetours++;

        Vector3 forward = transform.forward; forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 right = transform.right; right.y = 0f;
        if (right.sqrMagnitude < 0.0001f) right = Vector3.right;
        right.Normalize();

        Vector3 escape = Vector3.zero;

        if (IsDirectionBlocked(pos, forward, unstuckProbeDistance)) escape -= forward;
        if (IsDirectionBlocked(pos, -forward, unstuckProbeDistance)) escape += forward;
        if (IsDirectionBlocked(pos, right, unstuckProbeDistance)) escape -= right;
        if (IsDirectionBlocked(pos, -right, unstuckProbeDistance)) escape += right;

        if (escape.sqrMagnitude < 0.001f)
        {
            escape = Random.insideUnitSphere;
            escape.y = 0f;
        }

        if (escape.sqrMagnitude < 0.001f)
        {
            agent.ResetPath();
            return;
        }

        escape.Normalize();
        Vector3 candidate = pos + escape * unstuckDetourDistance;

        if (TryGetNavPoint(candidate, 10f, out var navCandidate) && IsPathComplete(pos, navCandidate))
        {
            nextDestinationUpdateAt = 0f;
            MoveAgentTo(navCandidate, speed * 1.1f);
        }
        else
        {
            agent.ResetPath();
        }
    }

    private bool IsDirectionBlocked(Vector3 origin, Vector3 dir, float dist)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return false;
        dir.Normalize();

        float radius = Mathf.Max(0.05f, agent.radius * 0.95f);
        float height = Mathf.Max(radius * 2f, agent.height);

        Vector3 baseCenter = origin + Vector3.up * radius;
        Vector3 topCenter = origin + Vector3.up * (height - radius);

        int mask = losMask & ~(1 << monsterLayer);
        var triggerMode = losTreatTriggersAsOccluders ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

        return Physics.CapsuleCast(baseCenter, topCenter, radius, dir, dist, mask, triggerMode);
    }

    // -----------------------------
    // Presentation / animation / audio
    // -----------------------------
    private void CachePresentationObjects()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        cachedColliders = GetComponentsInChildren<Collider>(true);
    }

    private bool IsBackstageState(MonsterActionState s) =>
        s == MonsterActionState.BackstageTravel || s == MonsterActionState.BackstageIdle;

    private bool ShouldHideMeshesInState(MonsterActionState s) =>
        hideMeshesWhileBackstage && s == MonsterActionState.BackstageIdle;

    private void ApplyBackstagePresentationForState(MonsterActionState s)
    {
        bool hide = ShouldHideMeshesInState(s);
        bool disableCollision = s == MonsterActionState.BackstageIdle;

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

    private void PlayLocomotion(bool moving, bool sprinting)
    {
        var clip = moving ? runningClip : idleClip;

        float animSpeed = 1f;
        if (moving && sprinting && clip == runningClip) animSpeed = 1.5f;

        if (clip) PlayClip(clip, animSpeed);
    }

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
        if (!animancer) return;

        if (clip != lastClip)
        {
            var s = animancer.Play(clip, 0.15f);
            s.Speed = animSpeed;
            lastClip = clip;
            lastAnimSpeed = animSpeed;
        }
        else if (animSpeed != lastAnimSpeed)
        {
            var s = animancer.States.GetOrCreate(clip);
            s.Speed = animSpeed;
            lastAnimSpeed = animSpeed;
        }
    }

    private void PlayOneShot(AudioClip clip, float volume = 1f, bool randomPitch = true)
    {
        if (!audioSource || !clip) return;

        float oldPitch = audioSource.pitch;
        if (randomPitch) audioSource.pitch = Random.Range(0.95f, 1.05f);

        audioSource.spatialBlend = 1f;
        audioSource.PlayOneShot(clip, volume * Game.Settings.MasterVolume);

        audioSource.pitch = oldPitch;
    }

    private void KillingSequence(Vector3 toPlayerFlat)
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
            PlayOneShot(killingSfx, killingSfxVolume, killingSfxRandomPitch);
        }

        if (toPlayerFlat.sqrMagnitude > 0.001f)
        {
            toPlayerFlat.y = 0f;
            var targetRot = Quaternion.LookRotation(toPlayerFlat.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
        }

        if (t > killingEndTime)
        {
            var gameUI = FindFirstObjectByType<GameUI>();
            if (gameUI != null)
                gameUI.ShowDeadUI();
        }
    }

    private void TeleportTo(Vector3 p)
    {
        if (TryGetNavPoint(p, 10f, out var navPos))
            agent.Warp(navPos);
        else
            transform.position = p;

        agent.ResetPath();
        destination = transform.position;

        lastPos = transform.position;
        stuckFor = 0f;
        stuckDetours = 0;
        nextDestinationUpdateAt = 0f;
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
}
