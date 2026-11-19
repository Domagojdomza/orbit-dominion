using UnityEngine;

namespace OrbitDominion
{
    public readonly struct WalkMotion
    {
        public readonly Vector3 velocity;
        public readonly float heading;
        public WalkMotion(Vector3 velocity, float heading) { this.velocity = velocity; this.heading = heading; }
    }

    public static class SurfaceWalkRules
    {
        public const float WalkSpeed = 3.2f, RunSpeed = 6.2f, Radius = .35f;

        public static WalkMotion Step(WalkMotion current, Vector2 input, float cameraHeading, bool sprint, float seconds)
        {
            if (seconds <= 0) return current;
            input = Vector2.ClampMagnitude(input, 1);
            Vector3 desired = Quaternion.Euler(0, cameraHeading, 0) * new Vector3(input.x, 0, input.y);
            Vector3 velocity = Vector3.MoveTowards(current.velocity, desired * (sprint ? RunSpeed : WalkSpeed), 22 * seconds);
            float heading = desired.sqrMagnitude > .001f ? Mathf.MoveTowardsAngle(current.heading,
                Mathf.Atan2(desired.x, desired.z) * Mathf.Rad2Deg, 540 * seconds) : current.heading;
            return new WalkMotion(velocity, heading);
        }

        public static bool InsideBox(Vector3 point, Vector3 center, Vector3 size, float yaw, float padding = Radius)
        {
            Vector3 p = Quaternion.Euler(0, -yaw, 0) * (point - center);
            return Mathf.Abs(p.x) < size.x * .5f + padding && Mathf.Abs(p.z) < size.z * .5f + padding &&
                p.y > -size.y * .5f - padding && p.y < size.y * .5f + padding;
        }

        public static bool CanStep(float currentFloor, float nextFloor, float horizontalDistance)
            => nextFloor - currentFloor <= .72f + horizontalDistance * .75f && currentFloor - nextFloor <= 1.2f;
    }
}
