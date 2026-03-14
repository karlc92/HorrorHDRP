using System;
using UnityEngine;

[Serializable]
public class NightSnapshot
{
    public Vector3 PlayerPos = Vector3.zero;
    public Quaternion PlayerRot = Quaternion.identity;
    public MonsterBrainState MonsterBrainState = new MonsterBrainState();
    public NightRuntimeState NightState = new NightRuntimeState();
}
