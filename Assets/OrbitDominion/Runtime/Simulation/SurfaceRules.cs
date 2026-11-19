using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrbitDominion
{
    public enum SurfaceBiome { Temperate, Ocean, Desert, Frozen, Volcanic, Rocky, Toxic }

    [Serializable]
    public sealed class SurfaceSave
    {
        public string activePlanet;
        public Vector3 explorerPosition;
        public float yaw, pitch;
        // Optional additions: archives without these fields resume in the rover.
        public bool onFoot;
        public Vector3 parkedRoverPosition;
        public float parkedRoverYaw;
        public List<string> collected = new();
        public SurfaceSandboxSave sandbox;
    }

    public static class SurfaceRules
    {
        public const int RelicsPerPlanet = 6;
        public const float HalfWidth = 150;

        public static SurfaceBiome Biome(WorldDefinition world)
        {
            if (world.id == "veil-02") return SurfaceBiome.Ocean;
            return world.archetype switch
            {
                PlanetArchetype.Desert => SurfaceBiome.Desert,
                PlanetArchetype.Frozen => SurfaceBiome.Frozen,
                PlanetArchetype.Volcanic => SurfaceBiome.Volcanic,
                PlanetArchetype.Rocky => SurfaceBiome.Rocky,
                PlanetArchetype.Toxic => SurfaceBiome.Toxic,
                _ => SurfaceBiome.Temperate
            };
        }

        public static int StableSeed(string id, int galaxySeed)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in id) hash = (hash ^ c) * 16777619;
                return (int)(hash ^ (uint)galaxySeed);
            }
        }

        public static float Height(SurfaceBiome biome, int seed, float x, float z)
        {
            if (biome == SurfaceBiome.Ocean) return -4;
            float offset = (seed & 65535) * .001f;
            float continents = Mathf.PerlinNoise(x * .007f + offset, z * .007f + offset * .7f) * 2 - 1;
            float hills = Mathf.PerlinNoise(x * .026f + offset * .3f, z * .026f + offset) * 2 - 1;
            float ridges = 1 - Mathf.Abs(Mathf.PerlinNoise(x * .014f + offset, z * .014f + 70) * 2 - 1);
            float scale = biome == SurfaceBiome.Rocky ? 17 : biome == SurfaceBiome.Desert ? 11 : biome == SurfaceBiome.Volcanic ? 14 : biome == SurfaceBiome.Frozen ? 9 : 8;
            // A broad coastal basin faces the initial landing view on mixed worlds.
            // The safe landing disk and the old relic coordinates remain unchanged.
            float coast = Mathf.SmoothStep(0, 1, Mathf.Clamp01((z - 30) / 85f));
            float height = 2.7f + continents * scale + hills * scale * .37f + ridges * scale * .24f - coast * (biome == SurfaceBiome.Rocky ? 9 : 7);
            float inletX = (x - 20) / 65f, inletZ = (z - 77) / 43f;
            height -= Mathf.Exp(-(inletX * inletX + inletZ * inletZ)) * (biome == SurfaceBiome.Rocky ? 15 : 12);
            float districtX = (x - 62) / 30f, districtZ = (z - 45) / 30f;
            height += Mathf.Exp(-(districtX * districtX + districtZ * districtZ)) * 13;
            float landing = Mathf.SmoothStep(0, 1, Mathf.Clamp01((new Vector2(x, z).magnitude - 11) / 22));
            float edge = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) - 126) / 24));
            return Mathf.Lerp(Mathf.Lerp(2.7f, height, landing), WaterLevel(biome) - 3, edge);
        }

        public static float WaterLevel(SurfaceBiome biome) => biome switch
        {
            SurfaceBiome.Ocean => 1.5f,
            SurfaceBiome.Volcanic => -1.3f,
            SurfaceBiome.Desert => -2.2f,
            SurfaceBiome.Rocky => -1.6f,
            SurfaceBiome.Frozen => -.5f,
            _ => 0f
        };

        public static Vector2[] RelicPositions(string id, int galaxySeed)
        {
            var random = new System.Random(StableSeed(id, galaxySeed) ^ 0x51943);
            var result = new Vector2[RelicsPerPlanet];
            for (int i = 0; i < result.Length; i++)
            {
                for (int tries = 0; tries < 120; tries++)
                {
                    float angle = (float)random.NextDouble() * Mathf.PI * 2;
                    float radius = 24 + (float)random.NextDouble() * 105;
                    var point = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    if (Vector2.Distance(point, new Vector2(62, 45)) < 26) continue; // Keep inhabited landing districts navigable.
                    bool separated = true;
                    for (int j = 0; j < i; j++) if (Vector2.Distance(point, result[j]) < 13) separated = false;
                    if (!separated) continue;
                    result[i] = point;
                    break;
                }
            }
            return result;
        }

        public static bool ValidSave(SurfaceSave save, IList<WorldDefinition> worlds)
        {
            if (save == null) return true; // Saves made before landing existed remain readable.
            if (save.onFoot && (string.IsNullOrEmpty(save.activePlanet) || !GameArchive.ValidVector(save.parkedRoverPosition) ||
                Mathf.Abs(save.parkedRoverPosition.x) > HalfWidth || Mathf.Abs(save.parkedRoverPosition.z) > HalfWidth ||
                Mathf.Abs(save.parkedRoverPosition.y) > 100 || !Finite(save.parkedRoverYaw))) return false;
            if (save.collected == null || save.collected.Count > worlds.Count * RelicsPerPlanet || !Finite(save.yaw) || !Finite(save.pitch) || Mathf.Abs(save.pitch) > 70 ||
                !GameArchive.ValidVector(save.explorerPosition) || Mathf.Abs(save.explorerPosition.x) > HalfWidth || Mathf.Abs(save.explorerPosition.z) > HalfWidth || Mathf.Abs(save.explorerPosition.y) > 100) return false;
            if (!string.IsNullOrEmpty(save.activePlanet))
            {
                bool known = false;
                foreach (var world in worlds) if (world.id == save.activePlanet) { known = true; break; }
                if (!known) return false;
            }
            var unique = new HashSet<string>();
            foreach (string key in save.collected)
            {
                if (string.IsNullOrEmpty(key) || !unique.Add(key)) return false;
                bool known = false;
                foreach (var world in worlds)
                    for (int i = 0; i < RelicsPerPlanet; i++) if (key == world.id + "/relic/" + i) known = true;
                if (!known) return false;
            }
            return SurfaceSandboxRules.ValidSave(save.sandbox, worlds);
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public readonly struct RoverMotion
    {
        public readonly float heading, speed;
        public RoverMotion(float heading, float speed) { this.heading = heading; this.speed = speed; }
    }

    public static class SurfaceRoverRules
    {
        public const float CruiseSpeed = 8f;
        public const float BoostSpeed = 14f;
        public const float ReverseSpeed = 5f;

        public static RoverMotion Step(RoverMotion current, float throttle, float steering, bool boost, float seconds)
        {
            if (seconds <= 0) return current;
            throttle = Mathf.Clamp(throttle, -1, 1);
            steering = Mathf.Clamp(steering, -1, 1);
            float target = throttle > 0 ? throttle * (boost ? BoostSpeed : CruiseSpeed) : throttle * ReverseSpeed;
            float speed = Mathf.MoveTowards(current.speed, target, (throttle == 0 ? 11f : 16f) * seconds);
            float turnRate = 37f + Mathf.Min(Mathf.Abs(speed) * 3.5f, 26f);
            float heading = current.heading + steering * turnRate * seconds * (speed < -.1f ? -1f : 1f);
            return new RoverMotion(heading, speed);
        }
    }
}
