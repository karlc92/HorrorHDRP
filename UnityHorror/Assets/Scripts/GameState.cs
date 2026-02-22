using System;
using UnityEngine;

[Serializable]
public class GameState
{
    public Vector3 PlayerPos = Vector3.zero;
    public Quaternion PlayerRot = Quaternion.identity;
    public int Night = 1;
    public int Slot = 0;
    public float TotalPlayTimeSeconds = 0f;

    public MonsterBrainState MonsterBrainState = new MonsterBrainState();

    
}
