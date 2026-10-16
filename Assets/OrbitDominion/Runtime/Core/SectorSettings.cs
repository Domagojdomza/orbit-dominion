using UnityEngine;
namespace OrbitDominion
{
    [CreateAssetMenu(menuName="Orbit Dominion/Sector settings")]
    public sealed class SectorSettings : ScriptableObject
    {
        public float captureSeconds=12,captureRange=220;
        public float shieldCost=160,shieldDuration=45,shieldCooldown=180,recentAttackLock=15;
        public float aiDecisionSeconds=75,fleetSpeed=600,fleetMinerals=150,fleetEnergy=90,enemyCaptureSeconds=35;
        public int fleetLimit=4,victoryPlanets=8;
        public float victorySeconds=60,anomalyRange=180,anomalySeconds=4;
    }
}
