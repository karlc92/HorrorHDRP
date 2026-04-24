using UnityEngine;

public static class DeterministicNoise
{
    public static float Noise2D(int seed, float x, float y)
    {
        int x0 = FastFloor(x);
        int y0 = FastFloor(y);
        int x1 = x0 + 1;
        int y1 = y0 + 1;

        float tx = x - x0;
        float ty = y - y0;

        float u = Fade(tx);
        float v = Fade(ty);

        float n00 = Gradient(seed, x0, y0, tx, ty);
        float n10 = Gradient(seed, x1, y0, tx - 1f, ty);
        float n01 = Gradient(seed, x0, y1, tx, ty - 1f);
        float n11 = Gradient(seed, x1, y1, tx - 1f, ty - 1f);

        float nx0 = Mathf.Lerp(n00, n10, u);
        float nx1 = Mathf.Lerp(n01, n11, u);
        return Mathf.Lerp(nx0, nx1, v);
    }

    public static float Fbm(int seed, float x, float y, int octaves, float lacunarity, float gain)
    {
        float frequency = 1f;
        float amplitude = 1f;
        float sum = 0f;
        float totalAmplitude = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float n = Noise2D(seed + i * 1013, x * frequency, y * frequency);
            sum += n * amplitude;
            totalAmplitude += amplitude;
            frequency *= lacunarity;
            amplitude *= gain;
        }

        if (totalAmplitude <= 0f)
            return 0f;

        return sum / totalAmplitude;
    }

    public static float RidgedFbm(int seed, float x, float y, int octaves, float lacunarity, float gain)
    {
        float frequency = 1f;
        float amplitude = 0.5f;
        float sum = 0f;
        float prev = 1f;

        for (int i = 0; i < octaves; i++)
        {
            float n = Mathf.Abs(Noise2D(seed + i * 1999, x * frequency, y * frequency));
            n = 1f - n;
            n *= n;
            n *= prev;
            prev = Mathf.Clamp01(n * 1.8f);
            sum += n * amplitude;

            frequency *= lacunarity;
            amplitude *= gain;
        }

        return Mathf.Clamp01(sum);
    }

    public static float Hash01(int seed, int x, int y)
    {
        uint h = Hash(seed, x, y);
        return (h & 0x00FFFFFFu) * (1f / 16777216f);
    }

    private static float Gradient(int seed, int x, int y, float dx, float dy)
    {
        uint h = Hash(seed, x, y) & 7u;
        return h switch
        {
            0 => dx + dy,
            1 => -dx + dy,
            2 => dx - dy,
            3 => -dx - dy,
            4 => dx,
            5 => -dx,
            6 => dy,
            _ => -dy
        };
    }

    private static uint Hash(int seed, int x, int y)
    {
        unchecked
        {
            uint h = (uint)seed;
            h ^= (uint)x * 374761393u;
            h = (h << 13) | (h >> 19);
            h = h * 1274126177u;
            h ^= (uint)y * 668265263u;
            h ^= h >> 15;
            h *= 2246822519u;
            h ^= h >> 13;
            h *= 3266489917u;
            h ^= h >> 16;
            return h;
        }
    }

    private static float Fade(float t)
    {
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    private static int FastFloor(float value)
    {
        int truncated = (int)value;
        if (value >= 0f || Mathf.Approximately(value, truncated))
            return truncated;

        return truncated - 1;
    }
}
