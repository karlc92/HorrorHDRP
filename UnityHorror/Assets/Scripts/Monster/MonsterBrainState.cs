using System;
using UnityEngine;

/// <summary>
/// Serializable state for the monster "brain". Keep this as pure data so it can be saved/loaded as JSON.
/// </summary>
[Serializable]
public class MonsterBrainState
{
    // Dictates whether the monster is "front stage" (active/hunting) or "back stage" (parked away).
    public bool MonsterFrontStage = false;

    // 0..100. Higher => more threatening (roams tighter around player, hint becomes more accurate).
    [Range(0, 100)]
    public int ThreatLevel = 0;

    // Where the monster believes the player is (approximation). Used as the roaming "anchor" when not hunting.
    public Vector3 PlayerLocationHint = Vector3.zero;
}
