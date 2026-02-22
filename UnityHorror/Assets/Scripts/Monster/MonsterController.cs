using System.Collections.Generic;
using UnityEngine;
using Animancer;
using Pathfinding;

[RequireComponent(typeof(CharacterController), typeof(Seeker))]
public class MonsterController : MonoBehaviour
{
    public enum MonsterActionState { Idle, Roaming, Hunting, Emote, Killing, BackstageTravel, BackstageIdle }
    public MonsterActionState state = MonsterActionState.Roaming;
    [SerializeField] GameObject deadBg;
    AnimancerComponent animancer;
    public AnimationClip idleClip;
    public AnimationClip runningClip;
    public AnimationClip killingClip;
    public AudioClip huntingBreatheAudio;
    public List<AudioClip> emoteAudioSound = new();
    public List<AnimationClip> emoteClips = new();

    public float speed = 3.5f;
    public float roamRadius = 12f;
    public float minRoamDistance = 3f;

    [Header("Director / Stage")]
    [Tooltip("When not Hunting, Roaming destinations will be picked around this point (typically the PlayerLocationHint).")]
    public Vector3 roamCenter;

    [Tooltip("If true, the monster will resume BackstageTravel after giving up a hunt.")]
    [SerializeField] private bool wantsBackstage = false;
    public bool WantsBackstage => wantsBackstage;


    [Header("Backstage Presentation")]
    [Tooltip("When the monster is backstage, all SkinnedMeshRenderer children are disabled so the player cannot see it.")]
    [SerializeField] private bool hideMeshesWhileBackstage = true;

    [Tooltip("If the monster is within this distance (XZ) of the player when going Backstage -> Frontstage, it teleports to a new navmesh point ~this far away before revealing.")]
    public float frontstageRevealDistance = 60f;

    [Tooltip("Teleport distance jitter as a fraction of frontstageRevealDistance (e.g. 0.15 = +/-15%).")]
    [Range(0f, 0.5f)]
    public float frontstageRevealDistanceJitter = 0.15f;

    [Tooltip("Attempts to find a valid navmesh point at reveal distance when coming frontstage.")]
    public int frontstageTeleportAttempts = 20;

    [Tooltip("If true, the re-entry teleport point must be on the same navmesh area as the player.")]
    public bool frontstageTeleportRequireSameNavAreaAsPlayer = true;

    private SkinnedMeshRenderer[] backstageSkinnedMeshes;


    [Tooltip("Disable the CharacterController while BackstageIdle so the player cannot bump into an invisible monster.")]
    [SerializeField] private bool disableCharacterControllerWhileBackstageIdle = true;

    [Tooltip("Minimum distance (XZ) from the player required before the monster will enter BackstageIdle (hide + no collision). If <= 0, uses frontstageRevealDistance.")]
    public float backstageMinDistanceFromPlayer = 0f;

    [Tooltip("If the requested backstage target is too close / invalid, the controller will try to pick a better one automatically.")]
    [SerializeField] private bool enforceBackstageDistance = true;

    [Tooltip("Attempts used when auto-picking a safer backstage destination.")]
    public int backstagePickAttempts = 20;

    float nextBackstageRepickAt;
    const float BackstageRepickCooldown = 0.25f;


    public float chaseDistance = 6f;
    public float killDistance = 1.5f;
    public float arriveDistance = 1.2f;

    public float repathInterval = 0.5f;
    public float huntRepathInterval = 0.2f;

    public float turnSpeed = 4f;
    public float emoteChancePerSecond = 0.03f;

    // Determines how long the Emote state lasts before returning to Roaming.
    // If <= 0, falls back to the selected emote clip length.
    public float emoteDuration = 1.5f;

    // When the monster gives up hunting (after reaching last seen position / losing too long), it idles for this long, then returns to Roaming.
    public float idleTimeAfterHunting = 1.0f;

    public LayerMask losMask = ~0;
    public int monsterLayer = 7;

    // Grace period after LOS breaks (ONLY used for LOS breaks, not for "player is unpathable").
    public float losGrace = 0.5f;

    // Only used when LOS is actually lost. If player is visible but unpathable, we do NOT end by timeout;
    // we run to lastSeenPos and end there.
    public float lostHuntTimeout = 4f;

    // Player considered "unpathable" if nearest walkable point is further than this (XZ) or on a different area.
    public float offMeshTolerance = 1.5f;

    public int maxPathFails = 2;

    // Stuck handling.
    public float stuckTime = 0.8f;
    public float unstuckCooldown = 0.5f;
    public int maxStuckDetours = 3;

    // How far we probe for immediate obstructions and how far we try to step away for the detour.
    public float unstuckRayDistance = 1.2f;
    public float unstuckDetourDistance = 3.0f;
    public float unstuckDetourDuration = 0.9f;
    public int roamPickAttempts = 12;

    // Prevent immediate re-hunt ping-pong after giving up.
    public float giveUpCooldown = 0.75f;

    // --- Killing "cinematic" controls ---
    [Header("Killing Sequence")]
    public float killingSlowStart = 1.0f;      // t < this => normal speed
    public float killingSlowEnd = 2.0f;        // [slowStart, slowEnd) => slow speed, then normal after
    public float killingSlowSpeed = 0.25f;
    public float killingNormalSpeed = 1.0f;
    public float killingEndTime = 3.15f;

    public AudioClip killingSfx;
    public float killingSfxAt = 1.0f;          // time since kill started
    public float killingSfxVolume = 1.0f;
    public bool killingSfxRandomPitch = true;

    public bool debug = true;
    public float debugInterval = 0.5f;

    AudioSource audioSource;
    CharacterController controller;
    Seeker seeker;
    PlayerController player;

    Vector3 destination;
    Vector3 lastSeenPos;
    Vector3 backstageDestination;

    Path path;
    int waypoint;
    float repathAt;

    float idleUntil;
    MonsterActionState idleNext;

    float emoteEndAt;

    AnimationClip lastClip;
    float lastAnimSpeed = 1f;

    float lastLosAt = -999f;
    bool wasChasing;
    float lostAt = -1f;

    float stuckFor;
    float lastUnstuckAt = -999f;
    int stuckDetours;

    float nextDebugAt;
    MonsterActionState lastState;
    bool lastLos;
    int pathFails;

    // Post-hunt / cooldown helpers.
    float gaveUpAt = -999f;
    bool forceRoamPick;

    // Temporary detour to get around local obstructions.
    bool detouring;
    Vector3 resumeDestination;
    float detourUntil;

    // Killing sequence runtime.
    float killingStartedAt = -999f;
    bool killingSfxPlayed;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        seeker = GetComponent<Seeker>();
        audioSource = GetComponent<AudioSource>();
        if (!animancer) animancer = GetComponentInChildren<AnimancerComponent>();

        player = FindFirstObjectByType<PlayerController>();

        destination = transform.position;
        lastSeenPos = destination;
        roamCenter = destination;
        backstageDestination = destination;
        lastState = state;

        ApplyBackstagePresentationForState(state);
    }

    static float SqrXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    Vector3 ProjectToWalkable(Vector3 p)
    {
        if (!AstarPath.active) return p;
        var nn = AstarPath.active.GetNearest(p, NNConstraint.Walkable);
        return nn.position;
    }

    bool IsBackstageState(MonsterActionState s) => s == MonsterActionState.BackstageTravel || s == MonsterActionState.BackstageIdle;

    // Only hide visuals once the monster is parked and idle backstage (so you can still see it leaving).
    bool ShouldHideMeshesInState(MonsterActionState s) => s == MonsterActionState.BackstageIdle;


    float GetBackstageRequiredDistance()
    {
        float d = backstageMinDistanceFromPlayer > 0.01f ? backstageMinDistanceFromPlayer : frontstageRevealDistance;
        return Mathf.Max(0f, d);
    }

    void ApplyBackstagePresentationForState(MonsterActionState s)
    {
        bool hide = ShouldHideMeshesInState(s);

        // Visuals (only hidden in BackstageIdle).
        if (hideMeshesWhileBackstage)
            SetBackstageMeshesEnabled(!hide);

        // Collision: disable CharacterController only when hidden.
        if (disableCharacterControllerWhileBackstageIdle)
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            if (controller != null && controller.enabled == hide)
                controller.enabled = !hide;
        }
    }

    bool TryPickBackstageDestination(Vector3 monsterPos, Vector3 playerPos, float requiredDist, out Vector3 picked)
    {
        picked = monsterPos;

        requiredDist = Mathf.Max(0f, requiredDist);
        if (requiredDist <= 0.01f) return false;

        // Fallback if we have no A* graph.
        if (!AstarPath.active)
        {
            Vector3 away = monsterPos - playerPos;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = Vector3.forward;
            away.Normalize();

            picked = playerPos + away * requiredDist;
            return true;
        }

        var start = AstarPath.active.GetNearest(monsterPos, NNConstraint.Walkable);
        if (start.node == null || !start.node.Walkable) return false;

        uint startArea = start.node.Area;

        float jitter = Mathf.Clamp(frontstageRevealDistanceJitter, 0f, 0.5f);
        int attempts = Mathf.Max(1, backstagePickAttempts);

        for (int i = 0; i < attempts; i++)
        {
            Vector2 dir2 = Random.insideUnitCircle;
            if (dir2.sqrMagnitude < 0.0001f) dir2 = Vector2.right;
            dir2.Normalize();

            float d = requiredDist * Random.Range(1f - jitter, 1f + jitter);
            Vector3 candidate = playerPos + new Vector3(dir2.x, 0f, dir2.y) * d;

            var nn = AstarPath.active.GetNearest(candidate, NNConstraint.Walkable);
            if (nn.node == null || !nn.node.Walkable) continue;

            if (nn.node.Area != startArea) continue;

            if (SqrXZ(nn.position, playerPos) < requiredDist * requiredDist)
                continue;

            // Must be reachable from the monster.
            if (!IsTargetPathableFrom(monsterPos, nn.position))
                continue;

            picked = nn.position;
            return true;
        }

        return false;
    }

    bool RepickBackstageDestination(Vector3 monsterPos, Vector3 playerPos)
    {
        float required = GetBackstageRequiredDistance();
        if (required <= 0.01f) return false;

        if (!TryPickBackstageDestination(monsterPos, playerPos, required, out var picked))
            return false;

        backstageDestination = picked;
        destination = picked;

        if (seeker == null) seeker = GetComponent<Seeker>();
        if (seeker != null) seeker.CancelCurrentPathRequest();

        path = null;
        waypoint = 0;
        repathAt = 0f;
        pathFails = 0;

        return true;
    }

    void CacheBackstageMeshes()
    {
        if (!hideMeshesWhileBackstage) return;
        if (backstageSkinnedMeshes == null || backstageSkinnedMeshes.Length == 0)
            backstageSkinnedMeshes = GetComponentsInChildren<SkinnedMeshRenderer>(true);
    }

    void SetBackstageMeshesEnabled(bool enabled)
    {
        if (!hideMeshesWhileBackstage) return;

        CacheBackstageMeshes();
        if (backstageSkinnedMeshes == null) return;

        for (int i = 0; i < backstageSkinnedMeshes.Length; i++)
        {
            var r = backstageSkinnedMeshes[i];
            if (r) r.enabled = enabled;
        }
    }

    void TeleportTo(Vector3 p)
    {
        // CharacterController doesn't like being moved while enabled.
        if (controller == null) controller = GetComponent<CharacterController>();

        bool ccWasEnabled = controller != null && controller.enabled;
        if (controller != null) controller.enabled = false;

        transform.position = p;

        if (controller != null)
        {
            controller.enabled = ccWasEnabled;
            if (controller.enabled) controller.Move(Vector3.zero); // flush internal state
        }

        destination = p;
        lastSeenPos = p;

        if (seeker == null) seeker = GetComponent<Seeker>();
        if (seeker != null) seeker.CancelCurrentPathRequest();

        path = null;
        waypoint = 0;
        repathAt = 0f;
    }

    bool TryPickFrontstageTeleportPoint(Vector3 playerPos, out Vector3 picked)
    {
        picked = transform.position;

        float dist = Mathf.Max(0f, frontstageRevealDistance);
        if (dist <= 0.01f) return false;

        // Fallback if we have no A* graph.
        if (!AstarPath.active)
        {
            Vector2 dir2 = Random.insideUnitCircle;
            if (dir2.sqrMagnitude < 0.0001f) dir2 = Vector2.right;
            dir2.Normalize();

            picked = playerPos + new Vector3(dir2.x, 0f, dir2.y) * dist;
            return true;
        }

        var playerNN = AstarPath.active.GetNearest(playerPos, NNConstraint.Walkable);
        if (playerNN.node == null || !playerNN.node.Walkable) return false;

        uint requiredArea = playerNN.node.Area;

        float minSqr = dist * dist;
        float jitter = Mathf.Clamp(frontstageRevealDistanceJitter, 0f, 0.5f);

        for (int i = 0; i < Mathf.Max(1, frontstageTeleportAttempts); i++)
        {
            Vector2 dir2 = Random.insideUnitCircle;
            if (dir2.sqrMagnitude < 0.0001f) dir2 = Vector2.right;
            dir2.Normalize();

            float d = dist * Random.Range(1f - jitter, 1f + jitter);
            Vector3 candidate = playerPos + new Vector3(dir2.x, 0f, dir2.y) * d;

            var nn = AstarPath.active.GetNearest(candidate, NNConstraint.Walkable);
            if (nn.node == null || !nn.node.Walkable) continue;

            if (frontstageTeleportRequireSameNavAreaAsPlayer && nn.node.Area != requiredArea)
                continue;

            if (SqrXZ(nn.position, playerPos) < minSqr)
                continue;

            // Ensure the monster can actually reach the player from there.
            if (!IsTargetPathableFrom(nn.position, playerPos))
                continue;

            picked = nn.position;
            return true;
        }

        // Worst-case fallback: directly away from player.
        Vector3 away = transform.position - playerPos;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = Vector3.forward;
        away.Normalize();

        Vector3 fallback = ProjectToWalkable(playerPos + away * dist);

        var fallbackNN = AstarPath.active.GetNearest(fallback, NNConstraint.Walkable);
        if (fallbackNN.node != null && fallbackNN.node.Walkable)
        {
            if (!frontstageTeleportRequireSameNavAreaAsPlayer || fallbackNN.node.Area == requiredArea)
            {
                picked = fallbackNN.position;
                return IsTargetPathableFrom(picked, playerPos);
            }
        }

        return false;
    }


    bool IsTargetPathableFrom(Vector3 from, Vector3 target)
    {
        if (!AstarPath.active) return true;

        var start = AstarPath.active.GetNearest(from, NNConstraint.Walkable);
        var end = AstarPath.active.GetNearest(target, NNConstraint.Walkable);

        if (start.node == null || end.node == null) return false;
        if (!start.node.Walkable || !end.node.Walkable) return false;

        // Disconnected navmesh islands.
        if (start.node.Area != end.node.Area) return false;

        // Target significantly off the navmesh.
        if (SqrXZ(target, end.position) > offMeshTolerance * offMeshTolerance) return false;

        return true;
    }

    bool TryPickRoamDestination(Vector3 startPos, Vector3 center, out Vector3 picked)
    {
        picked = startPos;

        float radius = Mathf.Max(0f, roamRadius);
        float minDist = Mathf.Clamp(minRoamDistance, 0f, radius);

        if (!AstarPath.active)
        {
            var r = Random.insideUnitSphere * radius;
            r.y = 0f;
            if (r.sqrMagnitude < minDist * minDist)
                r = r.sqrMagnitude > 0.0001f ? r.normalized * minDist : Vector3.forward * minDist;

            picked = center + r;
            return true;
        }

        var start = AstarPath.active.GetNearest(startPos, NNConstraint.Walkable);
        if (start.node == null || !start.node.Walkable) return false;

        for (int i = 0; i < Mathf.Max(1, roamPickAttempts); i++)
        {
            var r = Random.insideUnitSphere * radius;
            r.y = 0f;

            if (r.sqrMagnitude < minDist * minDist)
                r = r.sqrMagnitude > 0.0001f ? r.normalized * minDist : Vector3.forward * minDist;

            var candidate = center + r;
            var end = AstarPath.active.GetNearest(candidate, NNConstraint.Walkable);

            if (end.node == null || !end.node.Walkable) continue;
            if (end.node.Area != start.node.Area) continue;

            picked = end.position;
            return true;
        }

        picked = start.position;
        return true;
    }

    void PickNewRoamDestination(Vector3 startPos, Vector3 center)
    {
        if (TryPickRoamDestination(startPos, center, out var picked))
        {
            destination = picked;
            if (seeker == null) seeker = GetComponent<Seeker>();
        if (seeker != null) seeker.CancelCurrentPathRequest();
            path = null;
            waypoint = 0;
            repathAt = 0f;
        }
        else
        {
            destination = startPos;
            if (seeker == null) seeker = GetComponent<Seeker>();
        if (seeker != null) seeker.CancelCurrentPathRequest();
            path = null;
            waypoint = 0;
            repathAt = 0f;
        }
    }

    void EnterIdle(float duration, MonsterActionState nextState, Vector3 playerPos)
    {
        SetState(MonsterActionState.Idle, playerPos);
        idleUntil = Time.time + Mathf.Max(0f, duration);
        idleNext = nextState;
    }

    void EnterPostHuntIdle(Vector3 playerPos)
    {
        gaveUpAt = Time.time;

        // If the director currently wants the monster backstage, a failed hunt should resume BackstageTravel
        // (after the same post-hunt idle). Otherwise, return to normal roaming.
        if (wantsBackstage)
        {
            EnterIdle(idleTimeAfterHunting, MonsterActionState.BackstageTravel, playerPos);
        }
        else
        {
            forceRoamPick = true;
            EnterIdle(idleTimeAfterHunting, MonsterActionState.Roaming, playerPos);
        }
    }

    void ClearDetour()
    {
        detouring = false;
        detourUntil = 0f;
    }

    void SetState(MonsterActionState s, Vector3 playerPos)
    {
        if (s == state) return;

        if (debug) Debug.Log($"[Monster] State {state} -> {s}");

        // Leaving Hunting: stop breathe loop if active.
        if (state == MonsterActionState.Hunting && s != MonsterActionState.Hunting &&
            audioSource && audioSource.clip == huntingBreatheAudio)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }

        // Leaving Killing: release death sequence flag.
        if (state == MonsterActionState.Killing && s != MonsterActionState.Killing && player != null)
        {
            player.isInDeathSequence = false;
        }

        if (seeker == null) seeker = GetComponent<Seeker>();
        if (seeker != null) seeker.CancelCurrentPathRequest();
        path = null;
        waypoint = 0;
        repathAt = 0f;
        pathFails = 0;

        stuckFor = 0f;
        stuckDetours = 0;

        ClearDetour();

        state = s;
        ApplyBackstagePresentationForState(state);


        if (state == MonsterActionState.Hunting)
        {
            lastSeenPos = playerPos;
            lastLosAt = Time.time;
            wasChasing = true;
            lostAt = -1f;
        }
        else
        {
            wasChasing = false;
            lostAt = -1f;
        }

        if (state == MonsterActionState.Killing)
        {
            killingStartedAt = Time.time;
            killingSfxPlayed = false;

            if (player != null)
                player.isInDeathSequence = true;
        }
    }

    bool IsDirectionBlocked(Vector3 origin, Vector3 dir, float dist)
    {
        origin += Vector3.up * Mathf.Min(0.8f, controller.height * 0.35f);
        return Physics.Raycast(origin, dir, dist, ~0, QueryTriggerInteraction.Ignore);
    }

    bool TryStartDetour(Vector3 currentPos)
    {
        var f = transform.forward; f.y = 0f; f = f.sqrMagnitude > 0.001f ? f.normalized : Vector3.forward;
        var r = transform.right; r.y = 0f; r = r.sqrMagnitude > 0.001f ? r.normalized : Vector3.right;

        bool blockF = IsDirectionBlocked(currentPos, f, unstuckRayDistance);
        bool blockB = IsDirectionBlocked(currentPos, -f, unstuckRayDistance);
        bool blockR = IsDirectionBlocked(currentPos, r, unstuckRayDistance);
        bool blockL = IsDirectionBlocked(currentPos, -r, unstuckRayDistance);

        Vector3 escape = Vector3.zero;
        if (blockF) escape += -f;
        if (blockB) escape += f;
        if (blockR) escape += -r;
        if (blockL) escape += r;

        if (escape.sqrMagnitude < 0.001f)
        {
            escape = Random.insideUnitSphere;
            escape.y = 0f;
        }

        escape.y = 0f;
        if (escape.sqrMagnitude < 0.001f) return false;
        escape.Normalize();

        Vector3 from = currentPos;
        for (int i = 0; i < 6; i++)
        {
            float ang = Random.Range(-45f, 45f);
            var dir = Quaternion.Euler(0f, ang, 0f) * escape;

            var candidate = from + dir * unstuckDetourDistance;
            var walkable = ProjectToWalkable(candidate);

            if (!AstarPath.active)
            {
                resumeDestination = destination;
                detouring = true;
                detourUntil = Time.time + Mathf.Max(0.1f, unstuckDetourDuration);

                destination = walkable;
                if (seeker == null) seeker = GetComponent<Seeker>();
        if (seeker != null) seeker.CancelCurrentPathRequest();
                path = null;
                waypoint = 0;
                repathAt = 0f;
                return true;
            }

            if (!IsTargetPathableFrom(from, walkable)) continue;

            resumeDestination = destination;
            detouring = true;
            detourUntil = Time.time + Mathf.Max(0.1f, unstuckDetourDuration);

            destination = walkable;
            if (seeker == null) seeker = GetComponent<Seeker>();
        if (seeker != null) seeker.CancelCurrentPathRequest();
            path = null;
            waypoint = 0;
            repathAt = 0f;

            return true;
        }

        return false;
    }

    void KillingSequence(Vector3 pos, Vector3 playerPos, Vector3 toPlayerFlat)
    {
        // Freeze movement during the cinematic.
        controller.SimpleMove(Vector3.zero);

        // Timeline-based animation speed.
        float t = Time.time - killingStartedAt;

        float animSpeed = killingNormalSpeed;
        if (t >= killingSlowStart && t < killingSlowEnd)
            animSpeed = killingSlowSpeed;

        if (killingClip)
            PlayClip(killingClip, animSpeed);

        // Optional timed SFX.
        if (!killingSfxPlayed && killingSfx && t >= killingSfxAt)
        {
            killingSfxPlayed = true;
            PlayOneShot(killingSfx, killingSfxVolume, killingSfxRandomPitch, 0f);
        }

        // Always face the player during kill (lerped).
        if (toPlayerFlat.sqrMagnitude > 0.001f)
            TurnTowards(toPlayerFlat);

        if (t > killingEndTime)
        {
            if (deadBg != null)
            {
                deadBg.SetActive(true);
            }
        }
    }

    void Update()
    {
        if (!player)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (!player) return;
        }

        var pos = transform.position;
        var playerPos = player.transform.position;

        if (detouring)
        {
            if (Time.time >= detourUntil || SqrXZ(pos, destination) <= arriveDistance * arriveDistance)
            {
                destination = resumeDestination;
                ClearDetour();
                if (seeker == null) seeker = GetComponent<Seeker>();
        if (seeker != null) seeker.CancelCurrentPathRequest();
                path = null;
                waypoint = 0;
                repathAt = 0f;
            }
        }

        var toPlayer = playerPos - pos;
        toPlayer.y = 0f;
        var dPlayer = toPlayer.magnitude;

        Transform rt = player.raycastTarget ? player.raycastTarget : player.transform;


        // Backstage behaviour: travel to a backstage destination, then idle there.
        if (state == MonsterActionState.BackstageIdle)
        {
            // Robust: enforce hidden + no-collision every frame while parked backstage.
            ApplyBackstagePresentationForState(state);

            if (controller != null && controller.enabled)
                controller.SimpleMove(Vector3.zero);

            if (idleClip) PlayClip(idleClip, 1f);
            return;
        }
        if (state == MonsterActionState.BackstageTravel)
        {
            // Ensure we stay visible/collidable while leaving.
            ApplyBackstagePresentationForState(state);

            // IMPORTANT: Even while traveling backstage, the monster remains dangerous.
            // If the player gets within hunt range, enter Hunting using the exact same code path as normal.
            bool inRange = dPlayer <= chaseDistance && (Time.time - gaveUpAt) >= giveUpCooldown;
            bool huntable = inRange && IsTargetPathableFrom(pos, rt.position);
            if (huntable)
            {
                SetState(MonsterActionState.Hunting, playerPos);
            }

            // If we didn't switch to Hunting, continue traveling backstage.
            if (state == MonsterActionState.BackstageTravel)
            {
                destination = ProjectToWalkable(backstageDestination);

                // If we've effectively arrived, only park backstage if we're far enough from the player.
                if (SqrXZ(pos, destination) <= arriveDistance * arriveDistance)
                {
                    float required = GetBackstageRequiredDistance();

                    if (enforceBackstageDistance && required > 0.01f && SqrXZ(destination, playerPos) < required * required)
                    {
                        // Too close - pick a better backstage point and keep traveling.
                        if (Time.time >= nextBackstageRepickAt)
                        {
                            nextBackstageRepickAt = Time.time + BackstageRepickCooldown;
                            RepickBackstageDestination(pos, playerPos);
                        }
                    }
                    else
                    {
                        SetState(MonsterActionState.BackstageIdle, playerPos);

                        // Apply immediately (so visuals/collision flip right away on arrival).
                        ApplyBackstagePresentationForState(state);

                        if (controller != null && controller.enabled)
                            controller.SimpleMove(Vector3.zero);

                        if (idleClip) PlayClip(idleClip, 1f);
                        return;
                    }
                }
            }
        }

        if (state != lastState)
        {
            lastState = state;
            if (debug) Debug.Log($"[Monster] Now {state}");
        }

        if (dPlayer <= killDistance) SetState(MonsterActionState.Killing, playerPos);
        else if (state == MonsterActionState.Killing) SetState(MonsterActionState.Roaming, playerPos);

        if (state == MonsterActionState.Killing)
        {
            KillingSequence(pos, playerPos, toPlayer);
            return;
        }

        if (state == MonsterActionState.Idle)
        {
            controller.SimpleMove(Vector3.zero);
            if (idleClip) PlayClip(idleClip, 1f);
            if (Time.time >= idleUntil) SetState(idleNext, playerPos);
            return;
        }

        if (state == MonsterActionState.Emote)
        {
            controller.SimpleMove(Vector3.zero);
            if (Time.time >= emoteEndAt) SetState(MonsterActionState.Roaming, playerPos);
            return;
        }

        // ROAMING logic
        if (state == MonsterActionState.Roaming)
        {
            bool inRange = dPlayer <= chaseDistance && (Time.time - gaveUpAt) >= giveUpCooldown;
            bool huntable = inRange && IsTargetPathableFrom(pos, rt.position);

            if (huntable)
            {
                SetState(MonsterActionState.Hunting, playerPos);
            }
            else
            {
                if (forceRoamPick)
                {
                    forceRoamPick = false;
                    PickNewRoamDestination(pos, roamCenter);
                }
                else if (emoteClips.Count > 0 && Random.value < emoteChancePerSecond * Time.deltaTime)
                {
                    SetState(MonsterActionState.Emote, playerPos);

                    var clip = emoteClips[Random.Range(0, emoteClips.Count)];
                    if (clip)
                    {
                        PlayClip(clip, 1f);
                        float duration = emoteDuration > 0f ? emoteDuration : clip.length;
                        emoteEndAt = Time.time + Mathf.Max(0f, duration);

                        if (emoteAudioSound.Count > 0)
                            PlayAudio(emoteAudioSound[Random.Range(0, emoteAudioSound.Count)], 1f);
                    }
                    else
                    {
                        float duration = emoteDuration > 0f ? emoteDuration : 0f;
                        emoteEndAt = Time.time + Mathf.Max(0f, duration);
                    }

                    return;
                }
                else
                {
                    bool needNew =
                        path == null ||
                        waypoint >= (path?.vectorPath?.Count ?? 0) ||
                        (destination - pos).sqrMagnitude <= arriveDistance * arriveDistance;

                    if (needNew)
                        PickNewRoamDestination(pos, roamCenter);
                }
            }
        }

        bool los = false;
        bool chasing = false;
        bool losPathable = false;

        // HUNTING logic
        if (state == MonsterActionState.Hunting)
        {
            int mask = losMask & ~(1 << monsterLayer);

            var eye = pos + Vector3.up * (controller.height * 0.8f);
            var targetPos = rt.position;
            var dir = targetPos - eye;

            if (dir.sqrMagnitude > 0.001f)
            {
                var start = eye + dir.normalized * (controller.radius + 0.05f);
                var dir2 = targetPos - start;
                var dist = dir2.magnitude;

                if (dist > 0.01f)
                {
                    if (Physics.Raycast(start, dir2 / dist, out var hit, dist, mask, QueryTriggerInteraction.Ignore))
                    {
                        los = (hit.transform == rt) || hit.transform.IsChildOf(player.transform);
                        if (debug && Time.time >= nextDebugAt)
                            Debug.Log($"[Monster] LOS ray hit {hit.collider.name} (layer {hit.collider.gameObject.layer}) => los={los}");
                    }
                    else
                    {
                        los = true;
                    }
                }
            }

            // Only refresh lastSeenPos (and lastLosAt) when player pos is pathable.
            if (los)
            {
                if (IsTargetPathableFrom(pos, targetPos))
                {
                    losPathable = true;
                    lastLosAt = Time.time;
                    lastSeenPos = targetPos;
                    lostAt = -1f;
                }
                else
                {
                    losPathable = false;
                }
            }

            bool graceChase = (!los) && (Time.time - lastLosAt <= losGrace);
            chasing = losPathable || graceChase;

            if (!los && !chasing && lostAt < 0f) lostAt = Time.time;
            if (los) lostAt = -1f;

            if (debug && los != lastLos) Debug.Log($"[Monster] LOS {(los ? "OK" : "FAIL")} losPathable={losPathable} chasing={chasing}");
            lastLos = los;

            if (wasChasing && !chasing)
            {
                path = null;
                waypoint = 0;
                repathAt = 0f;
                if (debug) Debug.Log("[Monster] Stop perfect chase -> lastSeenPos");
            }
            wasChasing = chasing;

            Vector3 desired = chasing ? targetPos : lastSeenPos;
            destination = ProjectToWalkable(desired);

            if (lostAt >= 0f && Time.time - lostAt >= lostHuntTimeout)
            {
                if (debug) Debug.Log("[Monster] Lost too long -> Post-hunt idle");
                EnterPostHuntIdle(playerPos);
                return;
            }
        }

        bool searchingToLastKnown =
            state == MonsterActionState.Hunting &&
            !chasing &&
            SqrXZ(pos, destination) > arriveDistance * arriveDistance;

        bool sprinting =
            state == MonsterActionState.Hunting &&
            (chasing || searchingToLastKnown);

        // Repath
        if (seeker.IsDone() && Time.time >= repathAt)
        {
            float interval = (state == MonsterActionState.Hunting && sprinting) ? huntRepathInterval : repathInterval;
            repathAt = Time.time + interval;

            if (AstarPath.active)
            {
                var start = AstarPath.active.GetNearest(pos, NNConstraint.Walkable).position;
                var end = AstarPath.active.GetNearest(destination, NNConstraint.Walkable).position;

                bool doRepath =
                    state == MonsterActionState.Roaming ||
                    state == MonsterActionState.BackstageTravel ||
                    (state == MonsterActionState.Hunting && (chasing || path == null || detouring));

                if (doRepath)
                {
                    var playerPosNow = playerPos;
                    if (debug) Debug.Log($"[Monster] Repath {state} chasing={chasing} sprinting={sprinting} detour={detouring} -> {end}");

                    seeker.StartPath(start, end, p =>
                    {
                        if (!p.error && p.vectorPath != null && p.vectorPath.Count > 0)
                        {
                            path = p;
                            waypoint = 0;
                            pathFails = 0;
                        }
                        else
                        {
                            pathFails++;
                            if (debug) Debug.Log($"[Monster] Path FAIL ({pathFails}) {p.errorLog}");

                            path = null;
                            waypoint = 0;

                            if (state == MonsterActionState.Roaming)
                            {
                                forceRoamPick = true;
                            }

                            if (state == MonsterActionState.BackstageTravel)
                            {
                                // If we can't path to the chosen backstage point, pick a new one (otherwise it can appear to "idle" in view).
                                float required = GetBackstageRequiredDistance();
                                if (enforceBackstageDistance && required > 0.01f && Time.time >= nextBackstageRepickAt)
                                {
                                    nextBackstageRepickAt = Time.time + BackstageRepickCooldown;
                                    RepickBackstageDestination(transform.position, playerPosNow);
                                }
                            }

                            if (state == MonsterActionState.Hunting && pathFails >= maxPathFails)
                            {
                                if (debug) Debug.Log("[Monster] Too many path fails -> Post-hunt idle");
                                EnterPostHuntIdle(playerPosNow);
                            }
                        }
                    });
                }
            }
        }

        // Movement vector
        Vector3 move = Vector3.zero;

        if (state == MonsterActionState.Hunting && losPathable)
        {
            var direct = rt.position - pos;
            direct.y = 0f;
            move = direct.sqrMagnitude > 0.001f ? direct.normalized : Vector3.zero;
        }
        else if (path != null && waypoint < path.vectorPath.Count)
        {
            while (waypoint < path.vectorPath.Count - 1 &&
                   (path.vectorPath[waypoint] - pos).sqrMagnitude < 2.25f)
                waypoint++;

            var wp = path.vectorPath[waypoint] - pos;
            wp.y = 0f;
            if (wp.sqrMagnitude <= arriveDistance * arriveDistance) waypoint++;

            move = wp.sqrMagnitude > 0.001f ? wp.normalized : Vector3.zero;
        }

        float speedMult = sprinting ? 2f : 1f;
        controller.SimpleMove(move * (speed * speedMult));

        bool moving = controller.velocity.sqrMagnitude > 0.01f;

        if (moving) stuckDetours = 0;

        if (state == MonsterActionState.Hunting && sprinting && moving)
            PlayAudio(huntingBreatheAudio, 1f, true);
        else if (audioSource && audioSource.clip == huntingBreatheAudio && audioSource.isPlaying)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }

        var clip2 = moving ? runningClip : idleClip;
        float animSpeed2 = (state == MonsterActionState.Hunting && sprinting && moving && clip2 == runningClip) ? 1.5f : 1f;
        if (clip2) PlayClip(clip2, animSpeed2);

        if (state == MonsterActionState.Hunting && losPathable && (rt.position - pos).sqrMagnitude > 0.001f)
        {
            var face = rt.position - pos;
            face.y = 0f;
            TurnTowards(face);
        }
        else if (move.sqrMagnitude > 0.001f)
        {
            TurnTowards(move);
        }

        bool shouldMove =
            (state == MonsterActionState.Roaming || state == MonsterActionState.Hunting || state == MonsterActionState.BackstageTravel) &&
            SqrXZ(pos, destination) > arriveDistance * arriveDistance;

        if (shouldMove && controller.velocity.sqrMagnitude < 0.01f) stuckFor += Time.deltaTime;
        else stuckFor = 0f;

        if (stuckFor >= stuckTime && Time.time - lastUnstuckAt >= unstuckCooldown)
        {
            lastUnstuckAt = Time.time;
            stuckFor = 0f;

            if (stuckDetours >= maxStuckDetours && state == MonsterActionState.Hunting)
            {
                if (debug) Debug.Log("[Monster] Too many stuck detours during hunt -> Post-hunt idle");
                EnterPostHuntIdle(playerPos);
                return;
            }

            bool startedDetour = TryStartDetour(pos);
            if (startedDetour)
            {
                stuckDetours++;
                if (debug) Debug.Log($"[Monster] UNSTUCK detour #{stuckDetours}");
            }
            else
            {
                if (seeker == null) seeker = GetComponent<Seeker>();
        if (seeker != null) seeker.CancelCurrentPathRequest();
                path = null;
                waypoint = 0;
                repathAt = 0f;

                if (debug) Debug.Log("[Monster] UNSTUCK fallback repath");
            }
        }

        if (state == MonsterActionState.Hunting && !chasing &&
            SqrXZ(pos, destination) <= arriveDistance * arriveDistance &&
            (path == null || waypoint >= path.vectorPath.Count))
        {
            EnterPostHuntIdle(playerPos);
            return;
        }

        if (debug && Time.time >= nextDebugAt)
        {
            nextDebugAt = Time.time + debugInterval;
            Debug.Log($"[Monster] state={state} los={los} losPathable={losPathable} chasing={chasing} sprinting={sprinting} detour={detouring} speedMult={speedMult:F1} destDist={Mathf.Sqrt(SqrXZ(pos, destination)):F1} path={(path != null ? path.vectorPath.Count.ToString() : "null")} wp={waypoint} vel={controller.velocity.magnitude:F2}");
        }
    }

    // --- Director API (MonsterManager) ---
    public void SetRoamCenter(Vector3 center, bool forceNewDestination = false)
    {
        roamCenter = center;
        if (forceNewDestination) forceRoamPick = true;
    }

    public void ForcePickNewRoamDestination()
    {
        forceRoamPick = true;
    }

    public void ApplySavedPose(Vector3 position, Quaternion rotation)
    {
        // Use our internal teleport helper so we reset pathing, destinations, etc.
        TeleportTo(position);
        transform.rotation = rotation;
    }

    /// <summary>
    /// Forces the monster into BackstageIdle immediately (hidden + no collision), without requiring travel.
    /// Used by save/load so a monster saved while parked backstage doesn't briefly appear walking on load.
    /// </summary>
    public void ForceBackstageIdle(Vector3 playerPos)
    {
        wantsBackstage = true;

        // Don't stomp the death cinematic.
        if (state == MonsterActionState.Killing) return;

        backstageDestination = transform.position;
        nextBackstageRepickAt = 0f;

        SetState(MonsterActionState.BackstageIdle, playerPos);

        destination = transform.position;

        if (idleClip) PlayClip(idleClip, 1f);
    }

    public void SendBackstage(Vector3 target)
    {
        wantsBackstage = true;

        // Don't stomp the death cinematic.
        if (state == MonsterActionState.Killing) return;

        Vector3 playerPos = player ? player.transform.position : transform.position;
        float required = GetBackstageRequiredDistance();

        if (enforceBackstageDistance && required > 0.01f)
        {
            bool ok = true;

            // Must be far enough from the player.
            if (SqrXZ(target, playerPos) < required * required)
                ok = false;

            // Must land on walkable.
            if (AstarPath.active)
            {
                var nn = AstarPath.active.GetNearest(target, NNConstraint.Walkable);
                if (nn.node == null || !nn.node.Walkable)
                    ok = false;
            }

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

        // If we're currently in a post-hunt idle that was going to resume backstage travel, redirect it to roaming.
        if (state == MonsterActionState.Idle && idleNext == MonsterActionState.BackstageTravel)
            idleNext = MonsterActionState.Roaming;

        bool wasBackstage = IsBackstageState(state);
        if (!wasBackstage) return;

        // Only do the "hidden re-entry" teleport if we were parked backstage and invisible.
        // If we're still in BackstageTravel, we want to stay visible so the player can see/hear it leaving.
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
                    // Worst-case: still move it away so it doesn't "pop" right next to the player.
                    Vector3 away = transform.position - playerPos;
                    away.y = 0f;
                    if (away.sqrMagnitude < 0.0001f) away = Vector3.forward;
                    away.Normalize();

                    TeleportTo(ProjectToWalkable(playerPos + away * d));
                }
            }
        }

        SetState(MonsterActionState.Roaming, playerPos);
        forceRoamPick = true;
    }

    public bool IsBackstage => state == MonsterActionState.BackstageTravel || state == MonsterActionState.BackstageIdle;


    void PlayClip(AnimationClip clip, float animSpeed)
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

    void PlayAudio(AudioClip clip, float volume = 1f, bool loop = false, float spatialBlend = 1f)
    {
        if (!audioSource || !clip) return;
        if (audioSource.clip == clip && loop && audioSource.loop && audioSource.isPlaying) return;

        audioSource.Stop();
        audioSource.volume = volume * Game.Settings.MasterVolume;
        audioSource.loop = loop;
        if (audioSource.clip != clip) audioSource.clip = clip;
        audioSource.spatialBlend = spatialBlend;
        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.Play();
    }

    void PlayOneShot(AudioClip clip, float volume = 1f, bool randomPitch = true, float spatialBlend = 1f)
    {
        if (!audioSource || !clip) return;

        float oldPitch = audioSource.pitch;
        if (randomPitch) audioSource.pitch = Random.Range(0.95f, 1.05f);

        audioSource.spatialBlend = spatialBlend;

        audioSource.PlayOneShot(clip, volume * Game.Settings.MasterVolume);

        audioSource.pitch = oldPitch;
    }

    void TurnTowards(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        var target = Quaternion.LookRotation(dir);
        var a = transform.eulerAngles;
        var b = target.eulerAngles;

        transform.rotation = Quaternion.Euler(
            0f,
            Mathf.LerpAngle(a.y, b.y, turnSpeed * Time.deltaTime),
            0f
        );
    }
}
