using System;
using UnityEngine;

/// <summary>
/// Serializable state for the monster "brain". Keep this as pure data so it can be saved/loaded as JSON.
/// </summary>
[Serializable]
public class MonsterBrainState
{
    // NOTE: This object is intended to be the monster's PRIMARY source-of-truth.
    // Both MonsterManager (director) and MonsterController (executor) should read/write ONLY this
    // state (or derived values) rather than duplicating runtime state in their own fields.
    // This keeps save/load robust as the monster evolves.

    // Used to detect whether this state has been initialized for the current run.
    // (Avoids relying on Vector3.zero sentinels which are valid world positions.)
    public bool Initialized = false;

    /// <summary>
    /// Persisted high-level mode for the monster state machine.
    /// IMPORTANT: Keep value order aligned with MonsterController.MonsterActionState
    /// (Idle, Roaming, Investigating, Hunting, Emote, Killing, BackstageTravel, BackstageIdle).
    /// </summary>
    public enum MonsterBrainMode
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

    public MonsterBrainMode Mode = MonsterBrainMode.Roaming;

    // Dictates whether the monster is "front stage" (active/hunting) or "back stage" (parked away).
    public bool MonsterFrontStage = false;

    // 0..100. Higher => more threatening (roams tighter around player, hint becomes more accurate).
    [Range(0, 100)]
    public int ThreatLevel = 0;

    // Where the monster believes the player is (approximation). Used as the roaming "anchor" when not hunting.
    public Vector3 PlayerLocationHint = Vector3.zero;

    // Saved pose. Updated continuously by MonsterController.
    public Vector3 MonsterPosition = Vector3.zero;
    public Quaternion MonsterRotation = Quaternion.identity;

    // Internal navigation targets that define *where* the monster is headed.
    // These are updated by MonsterController (and BackstageDestination is authored by MonsterManager).
    public Vector3 RoamDestination = Vector3.zero;
    public Vector3 BackstageDestination = Vector3.zero;
    public Vector3 InvestigateTarget = Vector3.zero;
    public Vector3 LastKnownPlayerPos = Vector3.zero;

    // Remaining timers for transient modes. Stored as remaining seconds so load can restore seamlessly.
    public float InvestigateTimeRemaining = 0f;
    public float EmoteTimeRemaining = 0f;

    // Return modes to preserve intent across transient modes.
    public MonsterBrainMode HuntingReturnMode = MonsterBrainMode.Roaming;
    public MonsterBrainMode InvestigateReturnMode = MonsterBrainMode.Roaming;

    // If true, the monster was parked backstage and fully hidden (meshes off / CC disabled) at the time of saving.
    // This allows a load to restore the monster without briefly showing it traveling.
    public bool MonsterBackstageIdle = false;
}