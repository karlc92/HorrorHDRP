using System;
using UnityEngine;

[Serializable]
public class CleanseObstructionInstanceState
{
    public string InstanceId;
    public Vector3 Position = Vector3.zero;
    public bool Cleared = false;
}
