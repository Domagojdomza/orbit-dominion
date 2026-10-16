using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace OrbitDominion.Tests
{
    public sealed class SurfaceTests
    {
        [Test]
        public void RelicsHaveStableSeparatedLocationsPerPlanet()
        {
            var first = SurfaceRules.RelicPositions("solace-01", 42);
            var repeat = SurfaceRules.RelicPositions("solace-01", 42);
            var other = SurfaceRules.RelicPositions("veil-02", 42);
            Assert.AreEqual(SurfaceRules.RelicsPerPlanet, first.Length);
            bool differs = false;
            for (int i = 0; i < first.Length; i++)
            {
                Assert.AreEqual(first[i], repeat[i]);
                Assert.Less(first[i].magnitude, 130);
                Assert.Greater(first[i].magnitude, 20);
                if (first[i] != other[i]) differs = true;
                for (int j = 0; j < i; j++) Assert.GreaterOrEqual(Vector2.Distance(first[i], first[j]), 13);
            }
            Assert.IsTrue(differs);
        }

        [Test]
        public void SurfaceSaveRejectsUnknownAndDuplicateRelicsWhileAllowingOldSaves()
        {
            var worlds = new List<WorldDefinition>
            {
                new("solace-01", "MERIDIAN", Vector3.zero, 310, Color.blue, Color.green, Color.gray, Color.cyan, .5f, .5f)
            };
            Assert.IsTrue(SurfaceRules.ValidSave(null, worlds));
            var save = new SurfaceSave { activePlanet = "solace-01", explorerPosition = new Vector3(20, 3, 10), collected = new List<string> { "solace-01/relic/2" } };
            Assert.IsTrue(SurfaceRules.ValidSave(save, worlds));
            save.collected.Add("solace-01/relic/2"); Assert.IsFalse(SurfaceRules.ValidSave(save, worlds));
            save.collected[1] = "solace-01/relic/99"; Assert.IsFalse(SurfaceRules.ValidSave(save, worlds));
            save.collected.RemoveAt(1); save.activePlanet = "missing"; Assert.IsFalse(SurfaceRules.ValidSave(save, worlds));
        }

        [Test]
        public void ExistingWorldsProvideWaterAndDistinctSurfaceBiomes()
        {
            var home = new[] { new WorldDefinition("solace-01", "MERIDIAN", Vector3.zero, 310, Color.blue, Color.green, Color.gray, Color.cyan, .5f, .5f) };
            var worlds = GalaxyCatalog.Expand(home, 17);
            var biomes = new HashSet<SurfaceBiome>();
            foreach (var world in worlds) biomes.Add(SurfaceRules.Biome(world));
            Assert.IsTrue(biomes.Contains(SurfaceBiome.Ocean));
            Assert.IsTrue(biomes.Contains(SurfaceBiome.Volcanic));
            Assert.IsTrue(biomes.Contains(SurfaceBiome.Toxic));
            Assert.AreEqual(7, biomes.Count);
        }

        [Test]
        public void MixedLandscapesKeepDryLandingAndSubmergedOuterCoast()
        {
            foreach (SurfaceBiome biome in new[] { SurfaceBiome.Temperate, SurfaceBiome.Desert, SurfaceBiome.Frozen,
                         SurfaceBiome.Volcanic, SurfaceBiome.Rocky, SurfaceBiome.Toxic })
            {
                int seed = SurfaceRules.StableSeed("solace-01", 17);
                float level = SurfaceRules.WaterLevel(biome);
                Assert.Greater(SurfaceRules.Height(biome, seed, 0, 0), level, biome.ToString());
                Assert.Less(SurfaceRules.Height(biome, seed, SurfaceRules.HalfWidth, 0), level, biome.ToString());
            }
        }

        [Test]
        public void RoverAcceleratesSteersReversesAndCoastsToRest()
        {
            var start = new RoverMotion(0, 0);
            var forward = SurfaceRoverRules.Step(start, 1, 0, false, .5f);
            Assert.Greater(forward.speed, 0);
            Assert.LessOrEqual(forward.speed, SurfaceRoverRules.CruiseSpeed);
            var turning = SurfaceRoverRules.Step(forward, 1, 1, false, .5f);
            Assert.Greater(turning.heading, forward.heading);
            var reverse = SurfaceRoverRules.Step(turning, -1, 1, false, 1);
            Assert.Less(reverse.speed, 0);
            Assert.Less(reverse.heading, turning.heading);
            Assert.AreEqual(0, SurfaceRoverRules.Step(reverse, 0, 0, false, 1).speed);
            Assert.AreEqual(turning.speed, SurfaceRoverRules.Step(turning, -1, -1, true, 0).speed);
            Assert.AreEqual(SurfaceRoverRules.BoostSpeed, SurfaceRoverRules.Step(start, 1, 0, true, 2).speed);
        }

        [Test]
        public void SandboxSnapsBuildsRejectsOccupiedCellsAndValidatesPersistence()
        {
            var state = new SurfaceSandboxState();
            Vector3 anchor = SurfaceSandboxRules.Snap(new Vector3(9.1f, 0, -6.2f), 2.7f, 1);
            Assert.AreEqual(new Vector3(8, 6.7f, -8), anchor);
            Assert.IsTrue(state.CanPlace("solace-01", anchor, 0));
            var foundation = state.Place("solace-01", SurfaceBuildKind.Foundation, anchor, 0);
            Assert.IsNotNull(foundation);
            Assert.IsFalse(state.CanPlace("solace-01", anchor, 1));
            Assert.Greater(SurfaceSandboxRules.BuiltFloor(state.Builds, "solace-01", new Vector2(8, -8), 2.7f), 7);
            Vector3 wallAnchor = SurfaceSandboxRules.Snap(new Vector3(16, 0, 0), 2.7f, 0);
            state.Place("solace-01", SurfaceBuildKind.Wall, wallAnchor, 0);
            Assert.IsTrue(SurfaceSandboxRules.BlocksRover(state.Builds, "solace-01", new Vector2(16, 0)));
            Assert.IsFalse(SurfaceSandboxRules.BlocksRover(state.Builds, "solace-01", new Vector2(22, 0)));
            Vector3 roadAnchor = SurfaceSandboxRules.Snap(new Vector3(24, 0, 0), 2.7f, 0);
            state.Place("solace-01", SurfaceBuildKind.Road, roadAnchor, 0);
            Assert.Greater(SurfaceSandboxRules.BuiltFloor(state.Builds, "solace-01", new Vector2(24, 0), 2.7f), 2.8f);
            Vector3 towerAnchor = SurfaceSandboxRules.Snap(new Vector3(32, 0, 0), 2.7f, 0);
            Assert.IsNotNull(state.Place("solace-01", SurfaceBuildKind.Arcology, towerAnchor, 0));
            Assert.IsTrue(SurfaceSandboxRules.BlocksRover(state.Builds, "solace-01", new Vector2(32, 0)));
            Assert.IsFalse(state.CanPlace("solace-01", SurfaceBuildKind.Industry, towerAnchor, 0));
            Assert.AreEqual(0, (int)SurfaceBuildKind.Foundation);
            Assert.AreEqual(4, (int)SurfaceBuildKind.Beacon);
            Assert.AreEqual(2, state.CompleteRally("solace-01", 42.5f));
            Assert.AreEqual(1, state.CompleteRally("solace-01", 40));
            var worlds = new List<WorldDefinition> { new("solace-01", "MERIDIAN", Vector3.zero, 310, Color.blue, Color.green, Color.gray, Color.cyan, .5f, .5f) };
            Assert.IsTrue(SurfaceSandboxRules.ValidSave(state.Snapshot(), worlds));
            var invalid = state.Snapshot(); invalid.builds.Add(invalid.builds[0]);
            Assert.IsFalse(SurfaceSandboxRules.ValidSave(invalid, worlds));
        }

        [Test]
        public void RallyRoutesAreStableAndSpanTheLandingRegion()
        {
            var first = SurfaceSandboxRules.RallyRoute("solace-01", 42);
            var repeat = SurfaceSandboxRules.RallyRoute("solace-01", 42);
            var other = SurfaceSandboxRules.RallyRoute("veil-01", 42);
            Assert.AreEqual(SurfaceSandboxRules.RallyCheckpoints, first.Length);
            bool differs = false;
            for (int i = 0; i < first.Length; i++)
            {
                Assert.AreEqual(first[i], repeat[i]);
                Assert.Greater(first[i].magnitude, 55);
                Assert.Less(first[i].magnitude, 115);
                differs |= first[i] != other[i];
            }
            Assert.IsTrue(differs);
        }
    }
}
