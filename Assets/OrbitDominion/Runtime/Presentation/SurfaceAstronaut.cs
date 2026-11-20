using UnityEngine;

namespace OrbitDominion
{
    public enum SurfaceSpecies { Human, Ithacan, Varkesh }

    // Metre-scale articulated anatomy; planted leg poses and two-handed rifle grips.
    public sealed partial class SurfaceAstronaut : MonoBehaviour
    {
        readonly Transform[] thighs = new Transform[2], knees = new Transform[2], boots = new Transform[2], arms = new Transform[2], elbows = new Transform[2];
        Transform torso, weapon, muzzle;
        float phase, stride, aimBlend, recoil, breathing, reloadBlend;
        public Vector3 Muzzle => muzzle != null ? muzzle.position : transform.position + transform.forward + Vector3.up * 1.4f;
        public bool Armed => weapon != null;
        Transform Joint(string title, Transform parent, Vector3 at)
        {
            var joint = new GameObject(title).transform; joint.SetParent(parent, false); joint.localPosition = at; return joint;
        }
        public void Build(Material suit, Material seals, Material visor, Material accent, Material lamps,
            bool armed = true, SurfaceSpecies species = SurfaceSpecies.Human)
        {
            if (species == SurfaceSpecies.Human)
            {
                BuildExpeditionSuit(suit, seals, visor, accent, lamps);
                if (armed) BuildRifle(seals, suit, lamps);
                Pose(0, 0);
                return;
            }
            torso = Joint("Explorer / torso", transform, Vector3.zero);
            var body = new SurfaceMeshBuilder();
            body.Ellipsoid(new Vector3(0, 1.29f, 0), new Vector3(.49f, .62f, .32f), suit);
            body.Ellipsoid(new Vector3(0, 1.02f, 0), new Vector3(.39f, .27f, .28f), seals);
            body.Box(new Vector3(0, 1.37f, .145f), new Vector3(.34f, .26f, .07f), suit, .06f);
            body.Box(new Vector3(0, 1.25f, -.22f), new Vector3(.34f, .49f, .22f), seals, .07f);
            body.Box(new Vector3(0, 1.28f, -.34f), new Vector3(.28f, .35f, .045f), suit, .025f);
            body.Box(new Vector3(-.07f, 1.43f, .19f), new Vector3(.105f, .045f, .025f), lamps, .006f);
            body.Cylinder(new Vector3(0, 1.59f, 0), .15f, .07f, seals);
            for (int side = -1; side <= 1; side += 2)
            {
                body.Box(new Vector3(side * .18f, 1.36f, .145f), new Vector3(.045f, .32f, .04f), accent, .012f);
                body.Cylinder(new Vector3(side * .19f, 1.27f, -.24f), .055f, .36f, suit);
                body.Beam(new Vector3(side * .19f, 1.12f, .12f), new Vector3(side * .23f, 1.34f, -.09f), .024f, seals);
                body.Box(new Vector3(side * .12f, 1.08f, .15f), new Vector3(.1f, .12f, .065f), seals, .016f);
            }
            if (species == SurfaceSpecies.Human)
            {
                body.Ellipsoid(new Vector3(0, 1.78f, 0), new Vector3(.36f, .39f, .38f), suit);
                body.Ellipsoid(new Vector3(0, 1.79f, .13f), new Vector3(.32f, .24f, .19f), visor);
                body.Box(new Vector3(0, 1.61f, .15f), new Vector3(.24f, .065f, .1f), seals, .025f);
                body.Box(new Vector3(.17f, 1.82f, .03f), new Vector3(.06f, .08f, .11f), lamps, .018f);
            }
            else
            {
                body.Ellipsoid(new Vector3(0, 1.82f, 0), new Vector3(species == SurfaceSpecies.Ithacan ? .34f : .4f, .49f, .31f), accent);
                body.Ellipsoid(new Vector3(0, 1.65f, .08f), new Vector3(.2f, .19f, .25f), accent);
                for (int side = -1; side <= 1; side += 2)
                {
                    body.Ellipsoid(new Vector3(side * .09f, 1.85f, .137f), new Vector3(.115f, .14f, .06f), visor);
                    body.Beam(new Vector3(side * .14f, 1.95f, -.04f), new Vector3(side * .25f, 2.13f, -.14f),
                        species == SurfaceSpecies.Ithacan ? .035f : .075f, species == SurfaceSpecies.Ithacan ? accent : seals, .006f);
                }
            }
            body.Finish(torso, "Explorer / anatomy and life support");
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1 : 1;
                thighs[i] = Joint("Explorer / hip " + i, transform, new Vector3(side * .115f, 1.01f, 0));
                var thigh = new SurfaceMeshBuilder();
                thigh.Ellipsoid(new Vector3(0, -.22f, 0), new Vector3(.235f, .49f, .255f), suit);
                thigh.Box(new Vector3(side * .105f, -.22f, .03f), new Vector3(.065f, .2f, .16f), seals, .024f);
                thigh.Finish(thighs[i], "Explorer / upper leg");
                knees[i] = Joint("Explorer / knee " + i, thighs[i], new Vector3(0, -.44f, 0));
                var shin = new SurfaceMeshBuilder();
                shin.Ellipsoid(new Vector3(0, -.18f, -.015f), new Vector3(.175f, .42f, .20f), suit);
                shin.Box(new Vector3(0, -.015f, .1f), new Vector3(.17f, .15f, .08f), seals, .04f);
                shin.Cylinder(new Vector3(0, -.32f, 0), .092f, .055f, accent);
                shin.Finish(knees[i], "Explorer / lower leg");
                boots[i] = Joint("Explorer / ankle " + i, knees[i], new Vector3(0, -.43f, 0));
                var boot = new SurfaceMeshBuilder();
                boot.Box(new Vector3(0, -.055f, .055f), new Vector3(.205f, .16f, .33f), seals, .045f);
                boot.Box(new Vector3(0, -.13f, .055f), new Vector3(.21f, .045f, .34f), seals, .012f);
                for (int t = 0; t < 5; t++) boot.Box(new Vector3(0, -.153f, -.06f + t * .055f), new Vector3(.19f, .015f, .024f), seals, .003f);
                boot.Finish(boots[i], "Explorer / sole and boot");
                arms[i] = Joint("Explorer / shoulder " + i, torso, new Vector3(side * .28f, 1.48f, 0));
                var arm = new SurfaceMeshBuilder();
                arm.Ellipsoid(new Vector3(0, -.14f, 0), new Vector3(.21f, .37f, .225f), suit);
                arm.Box(new Vector3(side * .105f, -.05f, 0), new Vector3(.04f, .1f, .13f), accent, .015f);
                arm.Finish(arms[i], "Explorer / upper arm");
                elbows[i] = Joint("Explorer / elbow " + i, arms[i], new Vector3(0, -.30f, 0));
                var hand = new SurfaceMeshBuilder();
                hand.Ellipsoid(new Vector3(0, -.115f, 0), new Vector3(.16f, .28f, .18f), suit);
                hand.Cylinder(new Vector3(0, -.23f, 0), .085f, .05f, seals);
                hand.Ellipsoid(new Vector3(0, -.30f, .015f), new Vector3(.14f, .17f, .145f), seals);
                for (int f = 0; f < 4; f++) hand.Box(new Vector3(-.047f + f * .031f, -.355f, .05f), new Vector3(.024f, .075f, .055f), seals, .009f);
                hand.Finish(elbows[i], "Explorer / forearm and glove");
            }
            if (armed) BuildRifle(seals, suit, lamps);
        }
        void BuildRifle(Material metal, Material body, Material lamp)
        {
            body = Resources.Load<Material>("AstronautHardware") ?? body;
            weapon = Joint("Wayfarer / ARC-18 rifle", torso, new Vector3(.16f, 1.22f, .18f));
            var gun = new SurfaceMeshBuilder();
            gun.Box(new Vector3(0, 0, .12f), new Vector3(.11f, .14f, .43f), metal, .022f);
            gun.Box(new Vector3(0, -.02f, -.14f), new Vector3(.095f, .12f, .2f), body, .02f);
            gun.Box(new Vector3(0, -.095f, .04f), new Vector3(.065f, .18f, .085f), metal, .016f, Quaternion.Euler(-16, 0, 0));
            gun.Box(new Vector3(0, -.1f, .2f), new Vector3(.075f, .18f, .13f), body, .02f);
            gun.Beam(new Vector3(0, .015f, .33f), new Vector3(0, .015f, .66f), .026f, metal);
            gun.Cylinder(new Vector3(0, .015f, .66f), .038f, .065f, body, Quaternion.Euler(90, 0, 0));
            gun.Box(new Vector3(0, .12f, .13f), new Vector3(.07f, .07f, .15f), body, .014f);
            gun.Box(new Vector3(.056f, .015f, .18f), new Vector3(.01f, .035f, .1f), lamp, .003f);
            for (int i = 0; i < 5; i++) gun.Box(new Vector3(0, .08f, .18f + i * .035f), new Vector3(.12f, .018f, .015f), body, .004f);
            gun.Finish(weapon, "ARC-18 / receiver, stock, grip and barrel");
            muzzle = Joint("ARC-18 / muzzle", weapon, new Vector3(0, .015f, .7f));
        }
        public void Kick() => recoil = 1;
        public void Pose(float speed, float dt) => Pose(new Vector3(0, 0, speed), dt, false, 0, false, 0, 0);
        public void Pose(Vector3 localVelocity, float dt, bool aiming, float aimPitch, bool reloading, float leftFloor, float rightFloor)
        {
            float speed = localVelocity.magnitude;
            stride = Mathf.MoveTowards(stride, Mathf.Clamp01(speed / 5.4f), dt * 4);
            aimBlend = Mathf.MoveTowards(aimBlend, aiming ? 1 : 0, dt * 7);
            reloadBlend = Mathf.MoveTowards(reloadBlend, reloading ? 1 : 0, dt * 4);
            recoil = Mathf.MoveTowards(recoil, 0, dt * 9); breathing += dt; phase += speed * dt * 3.5f;
            torso.localPosition = Vector3.up * (Mathf.Sin(phase * 2) * .018f * stride + Mathf.Sin(breathing * 1.9f) * .004f);
            torso.localRotation = Quaternion.Euler(stride * 7 * (1 - aimBlend), Mathf.Sin(phase) * stride * 3 * (1 - aimBlend), -localVelocity.x * .5f);
            Vector3 stepDirection = speed > .01f ? localVelocity / speed : Vector3.forward;
            for (int i = 0; i < 2; i++)
            {
                float cycle = Mathf.Repeat(phase / (Mathf.PI * 2) + i * .5f, 1);
                float swing = cycle < .5f ? Mathf.Lerp(-1, 1, cycle * 2) : Mathf.Lerp(1, -1, (cycle - .5f) * 2);
                float lift = cycle < .5f ? Mathf.Sin(cycle * Mathf.PI * 2) * .17f * stride : 0;
                float terrain = Mathf.Clamp(i == 0 ? leftFloor : rightFloor, -.18f, .24f);
                Vector3 foot = thighs[i].localPosition + Vector3.down * (.845f - terrain - lift) + stepDirection * (swing * .27f * stride);
                SolveLimb(thighs[i], knees[i], transform.TransformPoint(foot), .44f, .43f, transform.forward);
                boots[i].rotation = transform.rotation * Quaternion.Euler(cycle > .78f ? (cycle - .78f) * 85 * stride : 0, 0, 0);
                if (!Armed) { arms[i].localRotation = Quaternion.Euler(-swing * 27 * stride - 6, 0, i == 0 ? 6 : -6); elbows[i].localRotation = Quaternion.Euler(-15 - 20 * stride, 0, 0); }
            }
            if (!Armed) return;
            weapon.localPosition = Vector3.Lerp(new Vector3(.16f, 1.18f, .14f), new Vector3(.18f, 1.39f, .12f), aimBlend) - Vector3.forward * recoil * .055f;
            weapon.localRotation = Quaternion.Euler(Mathf.Lerp(24, aimPitch, aimBlend) - recoil * 4, -32 * reloadBlend, Mathf.Lerp(-5 * (1 - aimBlend), -34, reloadBlend));
            PoseRifleHands();
        }
        // Presentation follows the same camera-selected point used by the muzzle trace.
        public void AlignRifle(Vector3 worldTarget)
        {
            if(!Armed || reloadBlend>.05f)return;
            Vector3 direction=worldTarget-weapon.position;
            if(direction.sqrMagnitude<.01f)return;
            weapon.rotation=Quaternion.LookRotation(direction,transform.up)*Quaternion.Euler(-recoil*2,0,0);
            PoseRifleHands();
        }
        void PoseRifleHands()
        {
            Vector3 rightGrip = suitHands[1] != null ? new Vector3(.06f,-.105f,.05f) : new Vector3(0,-.115f,.05f);
            Vector3 leftGrip = suitHands[0] != null ? new Vector3(-.045f,-.055f,Mathf.Lerp(.29f,.16f,reloadBlend)) : new Vector3(-.015f,-.055f,Mathf.Lerp(.35f,.16f,reloadBlend));
            SolveLimb(arms[1], elbows[1], weapon.TransformPoint(rightGrip), .30f, .31f, transform.right - transform.forward);
            SolveLimb(arms[0], elbows[0], weapon.TransformPoint(leftGrip), .30f, .31f, -transform.right - transform.forward);
            for (int i=0;i<2;i++) if(suitHands[i]!=null) suitHands[i].rotation=weapon.rotation;
        }
        static void SolveLimb(Transform upper, Transform lower, Vector3 target, float a, float b, Vector3 bend)
        {
            Vector3 offset = target - upper.position; float distance = Mathf.Clamp(offset.magnitude, .05f, a + b - .005f);
            Vector3 direction = offset.normalized, pole = Vector3.ProjectOnPlane(bend, direction).normalized;
            float along = (a * a + distance * distance - b * b) / (2 * distance);
            Vector3 joint = upper.position + direction * along + pole * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            Quaternion basis = upper.parent.rotation;
            upper.rotation = Quaternion.FromToRotation(basis * Vector3.down, (joint - upper.position).normalized) * basis;
            lower.rotation = Quaternion.FromToRotation(basis * Vector3.down, (target - lower.position).normalized) * basis;
        }
    }
}
