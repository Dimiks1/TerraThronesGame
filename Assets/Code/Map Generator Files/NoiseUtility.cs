using UnityEngine;

public static class NoiseUtility
{
    public static float FBm(
        float x,
        float y,
        int octaves,
        float persistence,
        float lacunarity)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float sum = 0f;
        float normalization = 0f;

        octaves = Mathf.Max(1, octaves);
        persistence = Mathf.Clamp01(persistence);
        lacunarity = Mathf.Max(1f, lacunarity);

        for (int i = 0; i < octaves; i++)
        {
            sum += amplitude * Mathf.PerlinNoise(x * frequency, y * frequency);
            normalization += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return normalization > 0f ? sum / normalization : 0f;
    }

    public static float ApplyContrast(float value, float contrast)
    {
        value = Mathf.Clamp01(value);

        if (contrast <= 1f)
            return value;

        return Mathf.Pow(value, contrast);
    }

    public static int StableRotation60(Vector2Int coord)
    {
        int hash = coord.x * 73856093 ^ coord.y * 19349663;
        return Mathf.Abs(hash) % 6;
    }

    public static float Hash01(Vector2Int coord)
    {
        int hash = coord.x * 73856093 ^ coord.y * 19349663;
        return Mathf.Abs(hash % 10000) / 10000f;
    }
}
