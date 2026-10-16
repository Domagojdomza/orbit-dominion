using UnityEngine;

namespace OrbitDominion
{
    [CreateAssetMenu(menuName="Orbit Dominion/Empire settings")]
    public sealed class EmpireSettings : ScriptableObject
    {
        public float startingMinerals=520, startingEnergy=420;
        public float colonyMinerals=180, colonyEnergy=120, deploymentSeconds=12;
        public float interactionRange=220;
        [Range(0,100)] public int minimumHabitability=25;
        public float baseMineralsPerSecond=1, baseEnergyPerSecond=1.2f;
        public float constructionSeconds=18;
    }
}
