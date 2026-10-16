using System;
using UnityEngine;

namespace OrbitDominion
{
    [Serializable]
    public sealed class WorldDefinition
    {
        public string id, title;
        public Vector3 center;
        public float radius;
        public Color ocean, terrain, highlands, atmosphere;
        public float land, clouds;
        public bool developed, rings;
        public int system;
        public PlanetArchetype archetype;
        public bool moon;
        public PlanetSurvey survey;
        public WorldDefinition(string id, string title, Vector3 center, float radius, Color ocean,
            Color terrain, Color highlands, Color atmosphere, float land, float clouds, bool developed = false, bool rings = false)
        {
            this.id = id; this.title = title; this.center = center; this.radius = radius;
            this.ocean = ocean; this.terrain = terrain; this.highlands = highlands;
            this.atmosphere = atmosphere; this.land = land; this.clouds = clouds;
            this.developed = developed; this.rings = rings;
        }
    }
}
