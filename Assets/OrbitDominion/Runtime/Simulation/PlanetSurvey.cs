using System;

namespace OrbitDominion
{
    public enum DiscoveryState { Unknown, Detected, Scanned, Visited }
    [Serializable]
    public sealed class PlanetSurvey
    {
        public string type, atmosphere, life, civilization, owner, trait;
        public int temperature, habitability, minerals, energy, defense, development;
        public PlanetSurvey(string type, string atmosphere, int temperature, int habitability, int minerals,
            int energy, string life, string civilization, string owner, int defense, int development, string trait)
        {
            this.type=type;this.atmosphere=atmosphere;this.temperature=temperature;this.habitability=habitability;
            this.minerals=minerals;this.energy=energy;this.life=life;this.civilization=civilization;
            this.owner=owner;this.defense=defense;this.development=development;this.trait=trait;
        }
    }
}
