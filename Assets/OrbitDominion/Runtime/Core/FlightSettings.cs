using UnityEngine;

namespace OrbitDominion
{
    [CreateAssetMenu(menuName = "Orbit Dominion/Flight Settings")]
    public sealed class FlightSettings : ScriptableObject
    {
        public float normalSpeed = 100, boostSpeed = 240, pulseSpeed = 1500;
        public float acceleration = 75, braking = 180, pulseAcceleration = 500;
        public float pitchRate = 65, yawRate = 65, rollRate = 90, mouseSensitivity = .09f;
        public float pulseExclusion = 110, shipClearance = 12;
        public float baseFov = 62, boostFov = 74, pulseFov = 88;
        public int galaxySeed = 73149;
    }
}
