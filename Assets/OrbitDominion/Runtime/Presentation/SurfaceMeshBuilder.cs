using System.Collections.Generic;
using UnityEngine;

namespace OrbitDominion
{
    // Static detail is combined per material, while wheels retain separate articulated roots.
    // Positions are local metres; these meshes never participate in simulation/collision.
    public sealed class SurfaceMeshBuilder
    {
        sealed class Part
        {
            public readonly List<Vector3> vertices = new(), normals = new();
            public readonly List<Vector2> uv = new();
            public readonly List<int> triangles = new();
        }
        readonly Dictionary<Material, Part> parts = new();

        void Triangle(Material material, Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc)
        {
            if (Vector3.Cross(b - a, c - a).sqrMagnitude < 1e-12f) return;
            if (!parts.TryGetValue(material, out var part)) parts.Add(material, part = new Part());
            int start = part.vertices.Count;
            Vector3 face = (na + nb + nc).normalized;
            int plane = Mathf.Abs(face.y) > .65f ? 0 : Mathf.Abs(face.x) > Mathf.Abs(face.z) ? 1 : 2;
            Vertex(part, a, plane); Vertex(part, b, plane); Vertex(part, c, plane);
            part.normals.Add(na); part.normals.Add(nb); part.normals.Add(nc);
            part.triangles.Add(start); part.triangles.Add(start + 1); part.triangles.Add(start + 2);
        }

        static void Vertex(Part part, Vector3 p, int plane)
        {
            part.vertices.Add(p);
            // Metre-sized planar UVs keep panel/stone detail consistent across differently sized parts.
            part.uv.Add((plane == 0 ? new Vector2(p.x, p.z) : plane == 1 ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y)) * .35f);
        }

        void Flat(Material material, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            Triangle(material, a, b, c, n, n, n);
        }

        public void Box(Vector3 center, Vector3 size, Material material, float bevel = .06f, Quaternion? rotation = null)
        {
            Quaternion q = rotation ?? Quaternion.identity;
            Vector3 half = size * .5f;
            float r = Mathf.Clamp(bevel, .001f, Mathf.Min(half.x, Mathf.Min(half.y, half.z)) * .8f);
            Vector3 inner = half - Vector3.one * r;
            for (int face = 0; face < 6; face++)
            {
                Vector3 normal = face switch { 0 => Vector3.right, 1 => Vector3.left, 2 => Vector3.up,
                    3 => Vector3.down, 4 => Vector3.forward, _ => Vector3.back };
                Vector3 u = Mathf.Abs(normal.y) > .5f ? Vector3.right : Vector3.up;
                Vector3 v = Vector3.Cross(normal, u);
                float hu = Vector3.Dot(half, Abs(u)), hv = Vector3.Dot(half, Abs(v));
                float hn = Vector3.Dot(half, Abs(normal));
                float[] us = { -hu, -hu + r, hu - r, hu }, vs = { -hv, -hv + r, hv - r, hv };
                var p = new Vector3[16]; var n = new Vector3[16];
                for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++)
                {
                    Vector3 raw = normal * hn + u * us[x] + v * vs[y];
                    Vector3 core = new(Mathf.Clamp(raw.x, -inner.x, inner.x), Mathf.Clamp(raw.y, -inner.y, inner.y), Mathf.Clamp(raw.z, -inner.z, inner.z));
                    Vector3 outward = (raw - core).normalized;
                    int i = y * 4 + x; p[i] = center + q * (core + outward * r); n[i] = q * outward;
                }
                for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
                {
                    int a = y * 4 + x, b = a + 1, c = a + 5, d = a + 4;
                    Triangle(material, p[a], p[b], p[c], n[a], n[b], n[c]);
                    Triangle(material, p[a], p[c], p[d], n[a], n[c], n[d]);
                }
            }
        }

        static Vector3 Abs(Vector3 v) => new(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        // Each profile point is (radius, height), traversed from bottom towards top.
        public void Lathe(Vector3 center, Vector2[] profile, Material material, int sides = 24, Quaternion? rotation = null)
        {
            Quaternion q = rotation ?? Quaternion.identity;
            for (int row = 0; row < profile.Length - 1; row++)
            {
                Vector2 low = profile[row], high = profile[row + 1], slope = high - low;
                for (int side = 0; side < sides; side++)
                {
                    float a = side * Mathf.PI * 2 / sides, b = (side + 1) * Mathf.PI * 2 / sides;
                    Vector3 ra = new(Mathf.Cos(a), 0, Mathf.Sin(a)), rb = new(Mathf.Cos(b), 0, Mathf.Sin(b));
                    Vector3 p0 = center + q * (ra * low.x + Vector3.up * low.y), p1 = center + q * (ra * high.x + Vector3.up * high.y);
                    Vector3 p2 = center + q * (rb * high.x + Vector3.up * high.y), p3 = center + q * (rb * low.x + Vector3.up * low.y);
                    Vector3 na = q * (ra * slope.y - Vector3.up * slope.x).normalized, nb = q * (rb * slope.y - Vector3.up * slope.x).normalized;
                    Triangle(material, p0, p1, p2, na, na, nb); Triangle(material, p0, p2, p3, na, nb, nb);
                }
            }
        }

        public void Cylinder(Vector3 center, float radius, float height, Material material, Quaternion? rotation = null, int sides = 20)
            => Lathe(center, new[] { new Vector2(0, -height * .5f), new Vector2(radius, -height * .5f),
                new Vector2(radius, height * .5f), new Vector2(0, height * .5f) }, material, sides, rotation);

        public void Beam(Vector3 a, Vector3 b, float radius, Material material, float endRadius = -1)
        {
            float length = Vector3.Distance(a, b);
            Lathe(a, new[] { Vector2.zero, new Vector2(radius, 0), new Vector2(endRadius < 0 ? radius : endRadius, length),
                new Vector2(0, length) }, material, 10, Quaternion.FromToRotation(Vector3.up, b - a));
        }

        public void Rock(Vector3 origin, Vector3 size, int seed, Material material, bool crystal = false)
        {
            var random = new System.Random(seed);
            int sides = crystal ? 6 : 11, rings = crystal ? 4 : 7;
            var points = new Vector3[rings, sides];
            float phase = (float)random.NextDouble() * 6.28f;
            for (int row = 0; row < rings; row++) for (int col = 0; col < sides; col++)
            {
                float t = row / (float)(rings - 1), angle = phase + col * Mathf.PI * 2 / sides;
                float taper = crystal ? (row == rings - 1 ? .025f : 1 - t * .35f) : Mathf.Sin((.15f + t * .85f) * Mathf.PI) * (.8f + .2f * Mathf.Sin(t * 16));
                float irregularity = crystal ? 1 : .73f + (float)random.NextDouble() * .4f;
                Vector3 p = new(Mathf.Cos(angle) * taper * irregularity * .5f + t * .13f,
                    t - .08f, Mathf.Sin(angle) * taper * irregularity * .5f + Mathf.Sin(t * 3) * .1f);
                points[row, col] = origin + Vector3.Scale(p, size);
            }
            for (int row = 0; row < rings - 1; row++) for (int col = 0; col < sides; col++)
            {
                int next = (col + 1) % sides;
                Flat(material, points[row, col], points[row + 1, col], points[row + 1, next]);
                Flat(material, points[row, col], points[row + 1, next], points[row, next]);
            }
            Vector3 bottom = Vector3.zero, top = Vector3.zero;
            for (int col = 0; col < sides; col++) { bottom += points[0, col] / sides; top += points[rings - 1, col] / sides; }
            for (int col = 0; col < sides; col++)
            {
                int next = (col + 1) % sides;
                Flat(material, bottom, points[0, col], points[0, next]);
                Flat(material, top, points[rings - 1, next], points[rings - 1, col]);
            }
        }

        public void Grass(Vector3 origin, int seed, Material material)
        {
            var random = new System.Random(seed);
            for (int i = 0; i < 11; i++)
            {
                float angle = (float)random.NextDouble() * 6.28f;
                Vector3 radial = new(Mathf.Cos(angle), 0, Mathf.Sin(angle)), right = Vector3.Cross(radial, Vector3.up) * .055f;
                Vector3 basePoint = origin + radial * ((float)random.NextDouble() * .38f);
                float height = .25f + (float)random.NextDouble() * .48f;
                Vector3 mid = basePoint + Vector3.up * height * .65f + radial * .09f, tip = basePoint + Vector3.up * height + radial * .26f;
                foreach (int sign in new[] { -1, 1 })
                {
                    Flat(material, basePoint - right * sign, basePoint + right * sign, mid + right * sign * .55f);
                    Flat(material, basePoint - right * sign, mid + right * sign * .55f, mid - right * sign * .55f);
                    Flat(material, mid - right * sign * .55f, mid + right * sign * .55f, tip);
                }
            }
        }

        public void Finish(Transform parent, string name)
        {
            foreach (var entry in parts)
            {
                Part p = entry.Value;
                var mesh = new Mesh { name = name, indexFormat = p.vertices.Count > 65535 ?
                    UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
                mesh.SetVertices(p.vertices); mesh.SetNormals(p.normals); mesh.SetUVs(0, p.uv); mesh.SetTriangles(p.triangles, 0);
                mesh.RecalculateBounds(); mesh.RecalculateTangents();
                var item = new GameObject(name); item.transform.SetParent(parent, false);
                item.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = item.AddComponent<MeshRenderer>(); renderer.sharedMaterial = entry.Key;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                item.AddComponent<OwnedMesh>().mesh = mesh;
            }
            parts.Clear();
        }
    }
}
