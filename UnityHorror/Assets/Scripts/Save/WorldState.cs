using System;

[Serializable]
public class WorldState
{
    public bool HasSeed;
    public int Seed;
    public int GeneratorRevision = 1;

    public void EnsureInitialized()
    {
        if (GeneratorRevision <= 0)
            GeneratorRevision = 1;
    }
}
