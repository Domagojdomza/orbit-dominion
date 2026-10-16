using UnityEngine;

namespace OrbitDominion
{
    // Pure rules shared by flight and tests. Distances are measured from a body's surface.
    public static class FlightRules
    {
        public static bool CanPulse(float clearance, float exclusion) => clearance > exclusion;
        public static float ApproachSpeed(float clearance, float exclusion, float normal, float pulse)
            => Mathf.Lerp(normal, pulse, Mathf.Clamp01((clearance - exclusion) / 450f));
        public static Vector3 SafePosition(Vector3 proposed, Vector3 center, float radius, Vector3 fallback)
        {
            Vector3 offset = proposed - center;
            if (offset.sqrMagnitude >= radius * radius) return proposed;
            return center + (offset.sqrMagnitude < .001f ? fallback.normalized : offset.normalized) * radius;
        }
    }
}
