using UnityEngine;
namespace OrbitDominion
{
    [CreateAssetMenu(menuName="Orbit Dominion/Discovery Settings")]
    public sealed class DiscoverySettings : ScriptableObject
    {
        [Min(1)] public float detectionRange=2200;
        [Min(1)] public float scanRange=650;
        [Min(1)] public float visitRange=180;
        [Min(.1f)] public float scanSeconds=5;
    }
}
