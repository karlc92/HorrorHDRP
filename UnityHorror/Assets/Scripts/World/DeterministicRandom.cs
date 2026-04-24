using System;

public struct DeterministicRandom
{
    private ulong state0;
    private ulong state1;

    public DeterministicRandom(int seed)
    {
        ulong sm = (ulong)(uint)seed;
        state0 = SplitMix64(ref sm);
        state1 = SplitMix64(ref sm);
        if ((state0 | state1) == 0UL)
            state1 = 0x9E3779B97F4A7C15UL;
    }

    public uint NextUInt()
    {
        ulong x = state0;
        ulong y = state1;
        state0 = y;
        x ^= x << 23;
        state1 = x ^ y ^ (x >> 17) ^ (y >> 26);
        return (uint)(state1 + y);
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            return minInclusive;

        uint span = (uint)(maxExclusive - minInclusive);
        return minInclusive + (int)(NextUInt() % span);
    }

    public float Value01()
    {
        return (NextUInt() >> 8) * (1f / 16777216f);
    }

    public float Range(float minInclusive, float maxInclusive)
    {
        if (maxInclusive <= minInclusive)
            return minInclusive;

        return minInclusive + (maxInclusive - minInclusive) * Value01();
    }

    private static ulong SplitMix64(ref ulong x)
    {
        x += 0x9E3779B97F4A7C15UL;
        ulong z = x;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
