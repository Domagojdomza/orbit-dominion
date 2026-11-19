using UnityEngine;

namespace OrbitDominion
{
    public static class SurfaceRoverModel
    {
        public static void Build(Transform root, Transform[] wheels, GameObject[] pontoons, Material hull, Material frame,
            Material rubber, Material solar, Material glass, Material accent, Material light)
        {
            var m = new SurfaceMeshBuilder();
            m.Box(new(0, 1.17f, 0), new(2.4f, .32f, 4.05f), frame, .14f);
            m.Box(new(0, 1.5f, 0), new(2.65f, .43f, 4.2f), hull, .18f);
            m.Box(new(0, 2.14f, -.52f), new(2.05f, 1.12f, 2.35f), hull, .28f);
            m.Box(new(0, 2.31f, .65f), new(1.62f, .53f, .055f), glass, .055f, Quaternion.Euler(-18, 0, 0));
            m.Box(new(0, 2.7f, -.52f), new(2.05f, .12f, 2.3f), frame, .06f);
            m.Box(new(0, 2.78f, -.72f), new(1.6f, .065f, 1.55f), solar, .025f);
            m.Box(new(0, 1.16f, 2.18f), new(2.9f, .18f, .25f), frame, .075f);
            m.Box(new(0, 1.26f, -2.12f), new(2.7f, .24f, .25f), frame, .07f);
            m.Box(new(0, 1.96f, -1.73f), new(1.58f, .48f, .08f), frame, .04f);
            for (int i = -4; i <= 4; i++) m.Box(new(i * .15f, 1.96f, -1.78f), new(.045f, .37f, .045f), hull, .008f);
            for (int side = -1; side <= 1; side += 2)
            {
                m.Box(new(side * 1.03f, 2.26f, -.45f), new(.045f, .53f, 1.62f), glass, .02f);
                m.Box(new(side * 1.07f, 2.26f, -.6f), new(.045f, .61f, .075f), frame, .012f);
                m.Box(new(side * 1.09f, 1.94f, -.18f), new(.07f, .06f, .3f), frame, .02f);
                m.Box(new(side * 1.33f, 1.55f, .3f), new(.045f, .09f, 3), accent, .015f);
                m.Box(new(side * .97f, 1.61f, -1.77f), new(.42f, .26f, .07f), accent, .035f);
                m.Box(new(side * .96f, 1.58f, 1.97f), new(.42f, .19f, .13f), frame, .04f);
                m.Box(new(side * .96f, 1.58f, 2.045f), new(.3f, .11f, .045f), light, .025f);
                // Rocker/bogie arms have separate pivots, dampers and axle ends instead of thin spokes.
                m.Cylinder(new(side * 1.39f, 1.14f, 0), .21f, .22f, accent, Quaternion.Euler(0, 0, 90));
                m.Beam(new(side * 1.35f, 1.15f, 0), new(side * 1.58f, .74f, 1.64f), .085f, frame);
                m.Beam(new(side * 1.35f, 1.15f, 0), new(side * 1.57f, .85f, -1.1f), .085f, frame);
                m.Beam(new(side * 1.57f, .85f, -1.1f), new(side * 1.62f, .67f, -1.64f), .075f, frame);
                m.Beam(new(side * 1.57f, .85f, -1.1f), new(side * 1.62f, .67f, 0), .075f, frame);
                m.Beam(new(side * 1.18f, 1.4f, -.4f), new(side * 1.57f, .83f, -.95f), .065f, hull);
                m.Beam(new(side * 1.18f, 1.4f, -.4f), new(side * 1.43f, 1.02f, -.75f), .085f, frame);
                m.Box(new(side * 2.07f, 1.87f, -.38f), new(1.4f, .085f, 2.75f), frame, .035f);
                for (int row = 0; row < 6; row++) for (int col = 0; col < 3; col++)
                {
                    Vector3 p = new(side * (1.63f + col * .43f), 1.925f, -1.5f + row * .45f);
                    m.Box(p, new(.39f, .018f, .4f), solar, .012f);
                    m.Box(p + Vector3.up * .012f, new(.012f, .006f, .38f), hull, .001f);
                }
                m.Beam(new(side * 1.2f, 1.4f, -.7f), new(side * 2.12f, 1.81f, -.7f), .045f, frame);
                for (int axle = 0; axle < 3; axle++)
                {
                    float z = (axle - 1) * 1.64f;
                    int index = (side < 0 ? 0 : 3) + axle;
                    var wheel = new GameObject("Rover wheel / " + index).transform;
                    wheel.SetParent(root, false); wheel.localPosition = new Vector3(side * 1.63f, .68f, z);
                    wheel.localRotation = Quaternion.Euler(0, 0, 90); wheels[index] = wheel;
                    Wheel(wheel, rubber, frame, accent);
                    m.Beam(new(side * 1.25f, .68f, z), new(side * 1.65f, .68f, z), .09f, frame);
                    if (axle != 1) m.Box(new(side * 1.64f, 1.4f, z), new(.6f, .09f, 1), frame, .045f);
                }
                var pontoon = VisualFactory.Shape("Rover flotation pontoon", PrimitiveType.Capsule, root,
                    new Vector3(side * 1.42f, .34f, 0), new Vector3(.7f, 2.2f, .7f), hull);
                pontoon.transform.localRotation = Quaternion.Euler(90, 0, 0); pontoons[side < 0 ? 0 : 1] = pontoon;
                for (int bolt = 0; bolt < 5; bolt++) m.Cylinder(new(side * 1.28f, 1.73f, -1.75f + bolt * .84f), .035f, .025f, frame, null, 6);
            }
            m.Cylinder(new(0, 2.76f, .5f), .21f, .14f, frame);
            m.Beam(new(0, 2.8f, .5f), new(0, 3.55f, .85f), .065f, hull);
            m.Box(new(0, 3.59f, .88f), new(.98f, .3f, .38f), hull, .09f);
            for (int side = -1; side <= 1; side += 2)
            {
                Quaternion lens = Quaternion.Euler(90, 0, 0);
                m.Cylinder(new(side * .32f, 3.6f, 1.08f), .115f, .09f, frame, lens);
                m.Cylinder(new(side * .32f, 3.6f, 1.135f), .078f, .025f, glass, lens);
            }
            m.Beam(new(-.75f, 2.75f, -1.25f), new(-.83f, 4.04f, -1.4f), .018f, frame);
            m.Cylinder(new(-.83f, 4.04f, -1.4f), .045f, .08f, accent);
            m.Beam(new(.57f, 1.72f, 1.35f), new(.9f, 1.2f, 2.28f), .075f, frame);
            m.Beam(new(.9f, 1.2f, 2.28f), new(.63f, .93f, 2.8f), .055f, hull);
            m.Cylinder(new(.9f, 1.2f, 2.28f), .12f, .16f, accent, Quaternion.Euler(0, 0, 90));
            for (int side = -1; side <= 1; side += 2)
                m.Box(new(.63f + side * .14f, .91f, 2.84f), new(.06f, .14f, .35f), frame, .02f, Quaternion.Euler(0, side * -18, 0));
            m.Finish(root, "Rover / engineered bodywork");
        }

        static void Wheel(Transform root, Material rubber, Material metal, Material accent)
        {
            var m = new SurfaceMeshBuilder();
            m.Lathe(Vector3.zero, new[] { new Vector2(0, -.23f), new Vector2(.48f, -.23f), new Vector2(.6f, -.19f),
                new Vector2(.65f, -.12f), new Vector2(.65f, .12f), new Vector2(.6f, .19f), new Vector2(.48f, .23f), new Vector2(0, .23f) }, rubber, 32);
            for (int side = -1; side <= 1; side += 2)
            {
                m.Cylinder(new(0, side * .235f, 0), .4f, .035f, metal, null, 32);
                m.Cylinder(new(0, side * .265f, 0), .16f, .07f, accent);
                for (int bolt = 0; bolt < 8; bolt++)
                {
                    float angle = bolt * Mathf.PI / 4;
                    Vector3 radial = new(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    m.Box(radial * .265f + Vector3.up * (side * .26f), new(.23f, .025f, .055f), accent, .015f, Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0));
                    m.Cylinder(radial * .12f + Vector3.up * (side * .31f), .025f, .02f, metal, null, 6);
                }
            }
            for (int tread = 0; tread < 24; tread++)
            {
                float angle = tread * Mathf.PI / 12;
                m.Box(new(Mathf.Cos(angle) * .652f, 0, Mathf.Sin(angle) * .652f), new(.055f, .37f, .12f), rubber,
                    .015f, Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 12));
            }
            m.Finish(root, "Rover / tread rim and fasteners");
        }
    }
}
