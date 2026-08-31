#if UNITY_EDITOR
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Deterministic octave noise used only while authoring the V0.20 presentation world.
    ///
    /// The octave-offset / persistence / lacunarity structure is adapted from the MIT-licensed
    /// terrain workflow in SebLague/Procedural-Landmass-Generation. Mindforge keeps a small,
    /// editor-only implementation instead of importing a runtime terrain package. Octave offsets
    /// are derived from a stable integer hash so texture/terrain authoring allocates no RNG object
    /// per sample.
    ///
    /// Reference: https://github.com/SebLague/Procedural-Landmass-Generation
    /// License: MIT
    /// </summary>
    public static class WorldSoulNoiseV20
    {
        public static float Fbm(
            float x,
            float y,
            int seed,
            int octaves = 5,
            float scale = 18f,
            float persistence = 0.52f,
            float lacunarity = 2.05f)
        {
            octaves = Mathf.Clamp(octaves, 1, 8);
            scale = Mathf.Max(0.01f, scale);
            persistence = Mathf.Clamp01(persistence);
            lacunarity = Mathf.Max(1f, lacunarity);

            float amplitude = 1f;
            float frequency = 1f;
            float value = 0f;
            float amplitudeSum = 0f;

            for (int octave = 0; octave < octaves; octave++)
            {
                float offsetX = Mathf.Lerp(-100000f, 100000f, Hash01(seed, octave * 2));
                float offsetY = Mathf.Lerp(-100000f, 100000f, Hash01(seed ^ 0x6C8E9CF5, octave * 2 + 1));
                float sampleX = (x + offsetX) / scale * frequency;
                float sampleY = (y + offsetY) / scale * frequency;
                float sample = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;

                value += sample * amplitude;
                amplitudeSum += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return amplitudeSum > 0f ? value / amplitudeSum : 0f;
        }

        public static float Ridge(float x, float y, int seed, float scale = 13f)
        {
            float n = Fbm(x, y, seed, 4, scale, 0.56f, 2.18f);
            return 1f - Mathf.Abs(n);
        }

        public static float Detail(float x, float y, int seed)
        {
            float broad = Fbm(x, y, seed, 4, 26f, 0.50f, 2.0f);
            float fine = Fbm(x + 17.31f, y - 11.73f, seed ^ 0x5f3759df, 3, 6.5f, 0.44f, 2.3f);
            return Mathf.Clamp(broad * 0.72f + fine * 0.28f, -1f, 1f);
        }

        public static float Hash01(int seed, int index)
        {
            unchecked
            {
                uint x = (uint)seed + (uint)index * 0x9E3779B9u;
                x ^= x >> 16;
                x *= 0x7FEB352Du;
                x ^= x >> 15;
                x *= 0x846CA68Bu;
                x ^= x >> 16;
                return (x & 0x00FFFFFFu) / 16777215f;
            }
        }

        public static float SignedHash(int seed, int index) => Hash01(seed, index) * 2f - 1f;

        public static Vector2 UnitDirection(int seed, int index)
        {
            float angle = Hash01(seed, index) * Mathf.PI * 2f;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }
}
#endif
