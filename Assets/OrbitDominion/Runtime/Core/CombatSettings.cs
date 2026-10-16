using UnityEngine;
namespace OrbitDominion
{
    [CreateAssetMenu(menuName="Orbit Dominion/Combat settings")]
    public sealed class CombatSettings : ScriptableObject
    {
        public float playerHull=120,playerShield=100;
        public float shieldDelay=5,shieldRecharge=14;
        public float weaponRange=750,playerDamage=24,playerFireInterval=.22f;
        public float aimAssistDegrees=5;
        public float hostileDamage=8,hostileFireInterval=1.1f,hostileRange=620;
        public float fighterSpeed=75,engagementRange=950,warningSeconds=4;
        public float satelliteDamage=18,satelliteFireInterval=.8f;
        public float respawnSeconds=4,respawnProtection=8,satelliteRepairSeconds=30;
    }
}
