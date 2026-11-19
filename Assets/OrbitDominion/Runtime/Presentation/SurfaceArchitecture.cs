using UnityEngine;

namespace OrbitDominion
{
    // One coherent set of metre-scaled pressure modules. All envelopes fit the saved build footprints.
    public static class SurfaceArchitecture
    {
        public static void Build(SurfaceBuildKind kind, Transform root, Material wall, Material metal, Material glass,
            Material concrete, Material asphalt, Material marking, Material light)
        {
            var m = new SurfaceMeshBuilder();
            switch (kind)
            {
                case SurfaceBuildKind.Foundation:
                    m.Box(new(0, .32f, 0), new(4, .64f, 4), concrete, .09f);
                    m.Box(new(0, .69f, 0), new(3.8f, .1f, 3.8f), metal, .02f);
                    for (int i = -1; i <= 1; i++) m.Box(new(i * 1.2f, .747f, 0), new(.025f, .012f, 3.65f), concrete, .002f);
                    break;
                case SurfaceBuildKind.Wall:
                    m.Box(new(0, 1.8f, 0), new(3.7f, 3.6f, .36f), wall, .1f);
                    foreach (int side in new[] { -1, 1 })
                    {
                        m.Box(new(side * 1.85f, 1.85f, 0), new(.3f, 3.7f, .62f), metal);
                        m.Box(new(0, 2.2f, side * .2f), new(2.8f, 1.1f, .05f), glass, .02f);
                        for (int i = -1; i <= 1; i++) m.Box(new(i * .9f, 2.2f, side * .24f), new(.07f, 1.25f, .06f), metal, .01f);
                    }
                    m.Box(new(0, 3.6f, 0), new(4, .2f, .65f), metal);
                    break;
                case SurfaceBuildKind.Ramp:
                    var tilt = Quaternion.Euler(-32, 0, 0);
                    m.Box(new(0, 1.55f, 0), new(3.9f, .42f, 5), metal, .04f, tilt);
                    for (int i = -5; i <= 5; i++) m.Box(new Vector3(0, 1.55f, 0) + tilt * new Vector3(0, .22f, i * .42f), new(3.45f, .025f, .035f), marking, .004f, tilt);
                    break;
                case SurfaceBuildKind.Road:
                    m.Box(new(0, .08f, 0), new(4, .16f, 4), concrete, .02f);
                    m.Box(new(0, .19f, 0), new(3.45f, .12f, 4), asphalt, .008f);
                    foreach (int side in new[] { -1, 1 })
                    {
                        m.Box(new(side * 1.86f, .23f, 0), new(.25f, .3f, 4), concrete, .035f);
                        for (int z = -1; z <= 1; z += 2)
                        {
                            m.Box(new(side * 1.68f, .265f, z * 1.38f), new(.1f, .03f, .18f), marking, .01f);
                            m.Box(new(0, .257f, z * 1.18f), new(.1f, .012f, 1.08f), marking, .002f);
                        }
                        for (int z = -1; z <= 1; z++) m.Box(new(side * 1.86f, .383f, z * 1.3f), new(.24f, .007f, .025f), metal, .001f);
                    }
                    break;
                case SurfaceBuildKind.Habitat:
                    Habitat(m, wall, metal, glass, concrete, light);
                    break;
                case SurfaceBuildKind.Beacon:
                    m.Box(new(0, .2f, 0), new(1.8f, .4f, 1.8f), concrete);
                    m.Cylinder(new(0, 2.2f, 0), .13f, 4, metal);
                    m.Box(new(.32f, 1.1f, 0), new(.52f, 1.3f, .6f), wall);
                    for (int i = 0; i < 3; i++) m.Box(new(0, 3.5f + i * .45f, 0), new(1.1f, .3f, .2f), wall);
                    m.Beam(new(0, 4.5f, 0), new(0, 5.08f, 0), .025f, metal);
                    m.Cylinder(new(0, 4.7f, 0), .09f, .12f, light);
                    break;
                case SurfaceBuildKind.Arcology:
                    m.Box(new(0, .25f, 0), new(7.2f, .5f, 7.2f), concrete, .15f);
                    m.Box(new(0, 1.25f, 0), new(6.5f, 1.6f, 6.5f), wall, .3f);
                    for (int floor = 0; floor < 7; floor++)
                    {
                        float y = 2.15f + floor * 1.2f, width = floor < 4 ? 5.5f : 4.6f;
                        m.Box(new(0, y + .45f, 0), new(width, 1.1f, width), wall, .2f);
                        m.Box(new(0, y - .05f, 0), new(width + .15f, .16f, width + .15f), metal, .04f);
                        for (int side = 0; side < 4; side++)
                        {
                            Quaternion q = Quaternion.Euler(0, side * 90, 0);
                            for (int bay = -1; bay <= 1; bay++)
                            {
                                Vector3 pos = q * new Vector3(bay * (width / 3.4f), y + .47f, width * .5f + .012f);
                                m.Box(pos, new(width / 3.4f - .2f, .66f, .045f), glass, .025f, q);
                                m.Box(pos + Vector3.up * .43f, new(width / 3.4f - .12f, .065f, .25f), metal, .012f, q);
                            }
                        }
                    }
                    m.Box(new(0, 10.6f, 0), new(4.9f, .3f, 4.9f), metal, .12f);
                    RoofServices(m, new(0, 10.8f, 0), wall, metal, glass);
                    Airlock(m, new(0, .5f, 3.07f), wall, metal, glass, light, 1.6f);
                    m.Beam(new(1.3f, 10.7f, -.8f), new(1.3f, 13.15f, -.8f), .035f, metal);
                    m.Cylinder(new(1.3f, 13.2f, -.8f), .07f, .14f, light);
                    break;
                case SurfaceBuildKind.Industry:
                    Industry(m, wall, metal, glass, concrete, light);
                    break;
                case SurfaceBuildKind.Spaceport:
                    m.Cylinder(new(0, .17f, 0), 5.95f, .34f, concrete, null, 64);
                    m.Cylinder(new(0, .38f, 0), 5.7f, .08f, asphalt, null, 64);
                    for (int i = 0; i < 32; i++)
                    {
                        Quaternion q = Quaternion.Euler(0, i * 11.25f, 0);
                        m.Box(q * new Vector3(0, .427f, 4.8f), new(.55f, .012f, .12f), marking, .002f, q);
                    }
                    m.Box(new(0, .427f, 0), new(2.1f, .012f, .24f), marking, .002f);
                    foreach (int side in new[] { -1, 1 })
                    {
                        m.Box(new(side * 1.05f, .427f, 0), new(.24f, .012f, 2.5f), marking, .002f);
                        m.Box(new(side * 5.1f, .65f, 0), new(.35f, .45f, .7f), metal);
                        m.Box(new(side * 5.1f, .9f, 0), new(.2f, .08f, .35f), light, .02f);
                    }
                    m.Box(new(-4.2f, 1.1f, -1.4f), new(1.3f, 1.4f, 2.1f), wall, .15f);
                    Vents(m, new(-4.2f, 1.2f, -.32f), metal, .85f);
                    break;
            }
            m.Finish(root, "Architectural envelope / " + kind);
        }

        static void Habitat(SurfaceMeshBuilder m, Material wall, Material metal, Material glass, Material concrete, Material light)
        {
            m.Box(new(0, .25f, 0), new(5.8f, .5f, 5.8f), concrete, .12f);
            m.Box(new(0, 1.9f, -.15f), new(5.15f, 3.1f, 4.7f), wall, .5f);
            m.Box(new(0, 3.42f, -.15f), new(5.25f, .18f, 4.8f), metal, .08f);
            foreach (int side in new[] { -1, 1 })
            {
                for (int bay = -1; bay <= 1; bay++)
                {
                    m.Box(new(side * 2.58f, 2.15f, -.15f + bay * 1.15f), new(.04f, 1, .85f), glass, .015f);
                    m.Box(new(side * 2.62f, 2.82f, -.15f + bay * 1.15f), new(.18f, .09f, 1.02f), metal, .02f);
                }
                m.Box(new(side * 1.72f, 2.1f, 2.22f), new(.85f, .95f, .05f), glass, .04f);
                m.Box(new(side * 2.3f, 1.8f, 2.28f), new(.14f, 2.3f, .17f), metal, .03f);
            }
            Airlock(m, new(0, .5f, 2.6f), wall, metal, glass, light);
            RoofServices(m, new(0, 3.55f, -.5f), wall, metal, glass);
        }

        static void Airlock(SurfaceMeshBuilder m, Vector3 p, Material wall, Material metal, Material glass, Material light, float width = 1.35f)
        {
            m.Box(p + new Vector3(0, 1.1f, 0), new(width + .3f, 2.2f, .65f), metal, .16f);
            m.Box(p + new Vector3(0, 1.03f, .335f), new(width, 1.97f, .06f), wall, .12f);
            m.Box(p + new Vector3(0, 1.45f, .375f), new(width * .58f, .42f, .045f), glass, .07f);
            m.Box(p + new Vector3(.38f, .95f, .41f), new(.075f, .28f, .06f), metal, .015f);
            m.Box(p + new Vector3(0, 2.13f, .35f), new(.38f, .05f, .05f), light, .01f);
        }

        static void RoofServices(SurfaceMeshBuilder m, Vector3 p, Material wall, Material metal, Material glass)
        {
            m.Box(p + new Vector3(-.9f, .22f, 0), new(1.1f, .4f, 1.3f), wall, .08f);
            for (int i = -2; i <= 2; i++) m.Box(p + new Vector3(-.9f, .44f, i * .2f), new(.86f, .04f, .08f), metal, .008f);
            for (int i = 0; i < 2; i++)
            {
                m.Box(p + new Vector3(.75f, .14f, -.65f + i * 1.3f), new(1.65f, .08f, 1.14f), metal, .02f);
                m.Box(p + new Vector3(.75f, .19f, -.65f + i * 1.3f), new(1.51f, .025f, 1.01f), glass, .008f);
            }
        }

        static void Vents(SurfaceMeshBuilder m, Vector3 p, Material metal, float width)
        {
            for (int i = -3; i <= 3; i++) m.Box(p + Vector3.up * i * .1f, new(width, .035f, .08f), metal, .005f);
        }

        static void Industry(SurfaceMeshBuilder m, Material wall, Material metal, Material glass, Material concrete, Material light)
        {
            m.Box(new(0, .2f, 0), new(8, .4f, 8), concrete, .1f);
            m.Box(new(-.85f, 2.25f, 0), new(5.8f, 3.7f, 6.5f), wall, .22f);
            m.Box(new(-.85f, 4.12f, 0), new(6.05f, .22f, 6.75f), metal);
            m.Box(new(-.85f, 1.72f, 3.27f), new(2.85f, 2.6f, .08f), metal, .04f);
            for (int i = 0; i < 9; i++) m.Box(new(-.85f, .55f + i * .28f, 3.33f), new(2.7f, .035f, .04f), wall, .004f);
            for (int side = -1; side <= 1; side += 2)
            {
                m.Box(new(-.85f + side * 2.5f, 2.25f, 3.3f), new(.12f, 3.4f, .15f), metal, .02f);
                m.Box(new(-.85f + side * 2.1f, .9f, 3.6f), new(.12f, 1.1f, .12f), light, .02f);
            }
            for (int i = 0; i < 3; i++) m.Box(new(-2.55f + i * 1.7f, 3.45f, 3.3f), new(1.3f, .48f, .06f), glass, .02f);
            for (int i = 0; i < 2; i++)
            {
                Vector3 p = new(2.85f, .4f, -1.9f + i * 3.5f);
                m.Lathe(p, new[] { Vector2.zero, new Vector2(.8f, 0), new Vector2(.8f, .22f), new Vector2(.7f, .38f),
                    new Vector2(.7f, 2.6f), new Vector2(.6f, 2.9f), new Vector2(.3f, 3.08f), new Vector2(0, 3.12f) }, metal);
                for (int band = 0; band < 3; band++) m.Cylinder(p + Vector3.up * (.6f + band * .85f), .73f, .07f, wall);
                m.Beam(p + new Vector3(0, 2.5f, 0), p + new Vector3(-1.1f, 2.5f, 0), .08f, metal);
                m.Beam(p + new Vector3(-.8f, .2f, .2f), p + new Vector3(-.8f, 3.1f, .2f), .03f, metal);
                for (int rung = 0; rung < 8; rung++) m.Beam(p + new Vector3(-.8f, .45f + rung * .3f, .2f), p + new Vector3(-.8f, .45f + rung * .3f, -.2f), .025f, metal);
            }
            RoofServices(m, new(-1, 4.25f, 1.3f), wall, metal, glass);
            for (int i = 0; i < 2; i++)
            {
                m.Cylinder(new(-2.3f + i * 2.7f, 5.4f, -1.9f), .23f, 2.6f, metal);
                m.Cylinder(new(-2.3f + i * 2.7f, 6.62f, -1.9f), .32f, .16f, wall);
            }
        }
    }
}
