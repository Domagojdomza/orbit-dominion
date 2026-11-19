using UnityEngine;

namespace OrbitDominion
{
    // Procedural articulated suit; mesh/material lifetime belongs to the expedition.
    public sealed class SurfaceAstronaut : MonoBehaviour
    {
        readonly Transform[] thighs = new Transform[2], knees = new Transform[2], arms = new Transform[2], elbows = new Transform[2];
        Transform torso;
        float phase, stride;

        Transform Joint(string title, Transform parent, Vector3 at)
        {
            var joint = new GameObject(title).transform;
            joint.SetParent(parent, false); joint.localPosition = at; return joint;
        }

        public void Build(Material suit, Material seals, Material visor, Material accent, Material lamps)
        {
            torso = Joint("Astronaut / torso", transform, Vector3.zero);
            var body = new SurfaceMeshBuilder();
            body.Box(new Vector3(0, 1.3f, 0), new Vector3(.56f, .55f, .34f), suit, .12f);
            body.Box(new Vector3(0, 1.02f, 0), new Vector3(.46f, .2f, .32f), seals, .06f);
            body.Box(new Vector3(0, 1.3f, -.24f), new Vector3(.43f, .55f, .24f), seals, .07f);
            body.Box(new Vector3(0, 1.32f, -.38f), new Vector3(.35f, .42f, .07f), suit, .04f);
            body.Box(new Vector3(0, 1.38f, .195f), new Vector3(.3f, .19f, .07f), seals, .02f);
            body.Box(new Vector3(-.06f, 1.4f, .235f), new Vector3(.11f, .05f, .015f), lamps, .005f);
            body.Box(new Vector3(.11f, 1.36f, .235f), new Vector3(.035f, .035f, .015f), accent, .006f);
            body.Cylinder(new Vector3(0, 1.6f, 0), .21f, .08f, seals);
            for (int side = -1; side <= 1; side += 2)
            {
                body.Box(new Vector3(side * .2f, 1.36f, .18f), new Vector3(.065f, .35f, .06f), accent, .015f);
                body.Cylinder(new Vector3(side * .24f, 1.32f, -.28f), .08f, .42f, suit);
            }
            body.Finish(torso, "Astronaut / pressure suit and life support");
            VisualFactory.Shape("Astronaut / helmet", PrimitiveType.Sphere, torso, new Vector3(0, 1.81f, 0), new Vector3(.49f, .47f, .46f), suit);
            VisualFactory.Shape("Astronaut / panoramic visor", PrimitiveType.Sphere, torso, new Vector3(0, 1.82f, .14f), new Vector3(.43f, .28f, .26f), visor);
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1 : 1;
                thighs[i] = Joint("Astronaut / hip " + i, transform, new Vector3(side * .155f, 1.01f, 0));
                var thigh = new SurfaceMeshBuilder();
                thigh.Box(new Vector3(0, -.19f, 0), new Vector3(.24f, .43f, .26f), suit, .07f);
                thigh.Box(new Vector3(side * .13f, -.19f, 0), new Vector3(.065f, .2f, .17f), seals, .025f);
                thigh.Finish(thighs[i], "Astronaut / thigh");
                knees[i] = Joint("Astronaut / knee " + i, thighs[i], new Vector3(0, -.43f, 0));
                var shin = new SurfaceMeshBuilder();
                shin.Box(new Vector3(0, -.17f, 0), new Vector3(.21f, .35f, .23f), suit, .06f);
                shin.Box(new Vector3(0, -.02f, .13f), new Vector3(.22f, .15f, .07f), seals, .03f);
                shin.Box(new Vector3(0, -.3f, 0), new Vector3(.22f, .07f, .24f), accent, .015f);
                shin.Box(new Vector3(0, -.47f, .06f), new Vector3(.25f, .2f, .39f), seals, .04f);
                shin.Finish(knees[i], "Astronaut / shin and boot");
                arms[i] = Joint("Astronaut / shoulder " + i, torso, new Vector3(side * .35f, 1.49f, 0));
                var arm = new SurfaceMeshBuilder();
                arm.Box(new Vector3(0, -.12f, 0), new Vector3(.23f, .34f, .25f), suit, .07f);
                arm.Box(new Vector3(side * .12f, -.03f, 0), new Vector3(.04f, .1f, .15f), accent, .012f);
                arm.Finish(arms[i], "Astronaut / upper arm");
                elbows[i] = Joint("Astronaut / elbow " + i, arms[i], new Vector3(0, -.29f, 0));
                var hand = new SurfaceMeshBuilder();
                hand.Box(new Vector3(0, -.13f, 0), new Vector3(.19f, .26f, .21f), suit, .05f);
                hand.Box(new Vector3(0, -.28f, 0), new Vector3(.2f, .08f, .22f), seals, .02f);
                hand.Box(new Vector3(0, -.38f, .015f), new Vector3(.17f, .16f, .19f), seals, .05f);
                hand.Finish(elbows[i], "Astronaut / forearm and glove");
            }
        }

        public void Pose(float speed, float dt)
        {
            stride = Mathf.MoveTowards(stride, Mathf.Clamp01(speed / SurfaceWalkRules.RunSpeed), dt * 5);
            phase += speed * dt * 2.8f;
            torso.localPosition = Vector3.up * (Mathf.Abs(Mathf.Sin(phase)) * .035f * stride);
            torso.localRotation = Quaternion.Euler(stride * 6, Mathf.Sin(phase) * stride * 4, 0);
            for (int i = 0; i < 2; i++)
            {
                float swing = Mathf.Sin(phase + i * Mathf.PI);
                thighs[i].localRotation = Quaternion.Euler(swing * 40 * stride, 0, 0);
                knees[i].localRotation = Quaternion.Euler(Mathf.Max(0, -swing) * 60 * stride, 0, 0);
                arms[i].localRotation = Quaternion.Euler(-swing * 34 * stride - 5, 0, i == 0 ? 5 : -5);
                elbows[i].localRotation = Quaternion.Euler(-15 - stride * 35, 0, 0);
            }
        }
    }
}
