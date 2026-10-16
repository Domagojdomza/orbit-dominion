using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrbitDominion
{
    // Existing numeric values are explicit because SurfaceBuildKind is serialized in flight-v2.json.
    public enum SurfaceBuildKind
    {
        Foundation = 0, Wall = 1, Ramp = 2, Habitat = 3, Beacon = 4,
        Road = 5, Arcology = 6, Industry = 7, Spaceport = 8
    }

    [Serializable]
    public sealed class SurfaceBuildSave
    {
        public string id, planet;
        public SurfaceBuildKind kind;
        public Vector3 position;
        public int rotation;
    }

    [Serializable]
    public sealed class SurfaceRallySave
    {
        public string planet;
        public float bestTime;
        public int completions;
    }

    [Serializable]
    public sealed class SurfaceSandboxSave
    {
        public int nextBuild = 1;
        public List<SurfaceBuildSave> builds = new();
        public List<SurfaceRallySave> rallies = new();
    }

    public static class SurfaceSandboxRules
    {
        public const float Grid = 4;
        public const float BuildHalfWidth = 140;
        public const int MaxBuilds = 384;
        public const int RallyCheckpoints = 7;

        public static Vector2 Cost(SurfaceBuildKind kind) => kind switch
        {
            SurfaceBuildKind.Foundation => new Vector2(8, 0),
            SurfaceBuildKind.Wall => new Vector2(6, 0),
            SurfaceBuildKind.Ramp => new Vector2(10, 0),
            SurfaceBuildKind.Habitat => new Vector2(28, 12),
            SurfaceBuildKind.Beacon => new Vector2(16, 8),
            SurfaceBuildKind.Road => new Vector2(5, 0),
            SurfaceBuildKind.Arcology => new Vector2(80, 45),
            SurfaceBuildKind.Industry => new Vector2(65, 30),
            _ => new Vector2(110, 85)
        };

        public static bool IsDriveable(SurfaceBuildKind kind) => kind == SurfaceBuildKind.Foundation ||
            kind == SurfaceBuildKind.Ramp || kind == SurfaceBuildKind.Road || kind == SurfaceBuildKind.Spaceport;

        // Full X/Z footprint in the build's local axes. Quarter turns swap the axes.
        public static Vector2 Footprint(SurfaceBuildKind kind, int rotation)
        {
            Vector2 size = kind switch
            {
                SurfaceBuildKind.Wall => new Vector2(4, .7f),
                SurfaceBuildKind.Ramp => new Vector2(4, 5),
                SurfaceBuildKind.Habitat => new Vector2(6, 6),
                SurfaceBuildKind.Beacon => new Vector2(2, 2),
                SurfaceBuildKind.Arcology => new Vector2(7.2f, 7.2f),
                SurfaceBuildKind.Industry => new Vector2(8, 8),
                SurfaceBuildKind.Spaceport => new Vector2(12, 12),
                _ => new Vector2(4, 4)
            };
            return (rotation & 1) == 0 ? size : new Vector2(size.y, size.x);
        }

        public static float StructureHeight(SurfaceBuildKind kind) => kind switch
        {
            SurfaceBuildKind.Foundation => .8f,
            SurfaceBuildKind.Road => .35f,
            SurfaceBuildKind.Ramp => 3.8f,
            SurfaceBuildKind.Wall => 3.8f,
            SurfaceBuildKind.Habitat => 4.2f,
            SurfaceBuildKind.Beacon => 5.2f,
            SurfaceBuildKind.Arcology => 13.5f,
            SurfaceBuildKind.Industry => 7.5f,
            _ => 2.2f
        };

        public static bool Overlaps(SurfaceBuildKind aKind, Vector3 a, int aRotation, SurfaceBuildKind bKind, Vector3 b, int bRotation)
        {
            if (a.y >= b.y + StructureHeight(bKind) - .05f || b.y >= a.y + StructureHeight(aKind) - .05f) return false;
            Vector2 aSize = Footprint(aKind, aRotation), bSize = Footprint(bKind, bRotation);
            return Mathf.Abs(a.x - b.x) < (aSize.x + bSize.x) * .5f - .08f &&
                Mathf.Abs(a.z - b.z) < (aSize.y + bSize.y) * .5f - .08f;
        }

        public static Vector3 Snap(Vector3 raw, float ground, int layer)
        {
            return new Vector3(Mathf.Round(raw.x / Grid) * Grid, ground + Mathf.Clamp(layer, 0, 5) * Grid,
                Mathf.Round(raw.z / Grid) * Grid);
        }

        public static bool ValidPlacement(Vector3 position, int rotation)
        {
            return GameArchive.ValidVector(position) && Mathf.Abs(position.x) <= BuildHalfWidth && Mathf.Abs(position.z) <= BuildHalfWidth &&
                position.y >= -10 && position.y <= 100 && rotation >= 0 && rotation < 4 &&
                Mathf.Abs(position.x / Grid - Mathf.Round(position.x / Grid)) < .01f &&
                Mathf.Abs(position.z / Grid - Mathf.Round(position.z / Grid)) < .01f;
        }

        public static Vector2[] RallyRoute(string planet, int galaxySeed)
        {
            var random = new System.Random(SurfaceRules.StableSeed(planet, galaxySeed) ^ 0x35a91);
            var route = new Vector2[RallyCheckpoints];
            float phase = (float)random.NextDouble() * Mathf.PI * 2;
            for (int i = 0; i < route.Length; i++)
            {
                float angle = phase + i * Mathf.PI * 2 / route.Length;
                float radius = 58 + (float)random.NextDouble() * 55;
                route[i] = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
            }
            return route;
        }

        public static bool BlocksRover(IReadOnlyList<SurfaceBuildSave> builds, string planet, Vector2 point)
        {
            foreach (var build in builds)
            {
                if (build.planet != planet || IsDriveable(build.kind)) continue;
                Vector2 delta = point - new Vector2(build.position.x, build.position.z);
                float angle = -build.rotation * 90 * Mathf.Deg2Rad;
                Vector2 local = new(delta.x * Mathf.Cos(angle) - delta.y * Mathf.Sin(angle), delta.x * Mathf.Sin(angle) + delta.y * Mathf.Cos(angle));
                if (build.kind == SurfaceBuildKind.Wall && Mathf.Abs(local.x) < 2.5f && Mathf.Abs(local.y) < 1.25f) return true;
                if (build.kind == SurfaceBuildKind.Habitat && local.sqrMagnitude < 13) return true;
                if (build.kind == SurfaceBuildKind.Beacon && local.sqrMagnitude < 2.5f) return true;
                if (build.kind == SurfaceBuildKind.Arcology && Mathf.Abs(local.x) < 3.7f && Mathf.Abs(local.y) < 3.7f) return true;
                if (build.kind == SurfaceBuildKind.Industry && Mathf.Abs(local.x) < 4.2f && Mathf.Abs(local.y) < 4.2f) return true;
            }
            return false;
        }

        public static float BuiltFloor(IReadOnlyList<SurfaceBuildSave> builds, string planet, Vector2 point, float terrain)
        {
            float floor = terrain;
            foreach (var build in builds)
            {
                if (build.planet != planet || !IsDriveable(build.kind)) continue;
                Vector2 delta = point - new Vector2(build.position.x, build.position.z);
                float angle = -build.rotation * 90 * Mathf.Deg2Rad;
                Vector2 local = new(delta.x * Mathf.Cos(angle) - delta.y * Mathf.Sin(angle), delta.x * Mathf.Sin(angle) + delta.y * Mathf.Cos(angle));
                Vector2 footprint = Footprint(build.kind, 0); // point is already expressed in build-local axes
                if (Mathf.Abs(local.x) > footprint.x * .5f + .15f || Mathf.Abs(local.y) > footprint.y * .5f + .15f) continue;
                if (build.kind == SurfaceBuildKind.Foundation) floor = Mathf.Max(floor, build.position.y + .65f);
                else if (build.kind == SurfaceBuildKind.Ramp) floor = Mathf.Max(floor, build.position.y + .2f + Mathf.Clamp01((local.y + 2) / 4) * 3.2f);
                else if (build.kind == SurfaceBuildKind.Road) floor = Mathf.Max(floor, build.position.y + .22f);
                else floor = Mathf.Max(floor, build.position.y + .38f);
            }
            return floor;
        }

        public static bool ValidSave(SurfaceSandboxSave save, IList<WorldDefinition> worlds)
        {
            if (save == null) return true;
            if (save.nextBuild < 0 || save.nextBuild > 10000000 || save.builds == null || save.rallies == null || save.builds.Count > MaxBuilds || save.rallies.Count > worlds.Count) return false;
            var worldIds = new HashSet<string>(); foreach (var world in worlds) worldIds.Add(world.id);
            var ids = new HashSet<string>();
            foreach (var build in save.builds)
            {
                if (build == null || string.IsNullOrEmpty(build.id) || string.IsNullOrEmpty(build.planet) || !worldIds.Contains(build.planet) ||
                    !build.id.StartsWith(build.planet + "/build/", StringComparison.Ordinal) || !ids.Add(build.id) ||
                    !Enum.IsDefined(typeof(SurfaceBuildKind), build.kind) || !ValidPlacement(build.position, build.rotation)) return false;
            }
            var rallyPlanets = new HashSet<string>();
            foreach (var rally in save.rallies)
                if (rally == null || !worldIds.Contains(rally.planet) || !rallyPlanets.Add(rally.planet) || rally.completions < 1 || rally.completions > 1000000 ||
                    float.IsNaN(rally.bestTime) || float.IsInfinity(rally.bestTime) || rally.bestTime <= 0 || rally.bestTime > 3600) return false;
            return true;
        }
    }

    public sealed class SurfaceSandboxState
    {
        readonly List<SurfaceBuildSave> builds = new();
        readonly List<SurfaceRallySave> rallies = new();
        int nextBuild = 1;
        public IReadOnlyList<SurfaceBuildSave> Builds => builds;
        public IReadOnlyList<SurfaceRallySave> Rallies => rallies;

        public bool CanPlace(string planet, Vector3 position, int rotation)
            => CanPlace(planet, SurfaceBuildKind.Foundation, position, rotation);

        public bool CanPlace(string planet, SurfaceBuildKind kind, Vector3 position, int rotation)
        {
            if (builds.Count >= SurfaceSandboxRules.MaxBuilds || string.IsNullOrEmpty(planet) || !SurfaceSandboxRules.ValidPlacement(position, rotation)) return false;
            foreach (var build in builds) if (build.planet == planet && SurfaceSandboxRules.Overlaps(kind, position, rotation, build.kind, build.position, build.rotation)) return false;
            return true;
        }

        public SurfaceBuildSave Place(string planet, SurfaceBuildKind kind, Vector3 position, int rotation)
        {
            if (!CanPlace(planet, kind, position, rotation)) return null;
            var build = new SurfaceBuildSave { id = planet + "/build/" + nextBuild++, planet = planet, kind = kind, position = position, rotation = rotation };
            builds.Add(build); return build;
        }

        public SurfaceBuildSave Closest(string planet, Vector3 position, float range)
        {
            SurfaceBuildSave result = null; float best = range;
            foreach (var build in builds) if (build.planet == planet)
            {
                float distance = Vector3.Distance(build.position, position);
                if (distance < best) { best = distance; result = build; }
            }
            return result;
        }

        public bool Remove(string id)
        {
            int index = builds.FindIndex(build => build.id == id); if (index < 0) return false; builds.RemoveAt(index); return true;
        }

        // 2 = first finish, 1 = new best, 0 = completed without improving.
        public int CompleteRally(string planet, float time)
        {
            if (string.IsNullOrEmpty(planet) || float.IsNaN(time) || float.IsInfinity(time) || time <= 0 || time > 3600) return 0;
            var record = rallies.Find(item => item.planet == planet);
            if (record == null) { rallies.Add(new SurfaceRallySave { planet = planet, bestTime = time, completions = 1 }); return 2; }
            record.completions++;
            if (time < record.bestTime) { record.bestTime = time; return 1; }
            return 0;
        }

        public SurfaceRallySave Rally(string planet) => rallies.Find(item => item.planet == planet);

        public SurfaceSandboxSave Snapshot()
        {
            var save = new SurfaceSandboxSave { nextBuild = nextBuild };
            foreach (var build in builds) save.builds.Add(new SurfaceBuildSave { id = build.id, planet = build.planet, kind = build.kind, position = build.position, rotation = build.rotation });
            foreach (var rally in rallies) save.rallies.Add(new SurfaceRallySave { planet = rally.planet, bestTime = rally.bestTime, completions = rally.completions });
            return save;
        }

        public void Restore(SurfaceSandboxSave save)
        {
            builds.Clear(); rallies.Clear(); nextBuild = 1;
            if (save == null) return;
            nextBuild = Mathf.Max(1, save.nextBuild);
            if (save.builds != null) foreach (var build in save.builds)
                builds.Add(new SurfaceBuildSave { id = build.id, planet = build.planet, kind = build.kind, position = build.position, rotation = build.rotation });
            if (save.rallies != null) foreach (var rally in save.rallies)
                rallies.Add(new SurfaceRallySave { planet = rally.planet, bestTime = rally.bestTime, completions = rally.completions });
        }
    }
}
