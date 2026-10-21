using System.Collections.Generic;
using UnityEngine;

namespace StackJump
{
    // Procedural meshes, so the game ships with no model assets.
    public static class MeshFactory
    {
        static Mesh sphere, cylinder, quad;
        static readonly Dictionary<string, Mesh> cache = new();

        // Cached or built-in meshes are shared by many renderers and must outlive any one of them.
        public static bool IsShared(Mesh m) =>
            m == null || m == sphere || m == cylinder || m == quad || cache.ContainsValue(m);

        // Same dimensions as Unity's built-in primitives (sphere radius 0.5; cylinder radius 0.5,
        // height 2), but generated here: CreatePrimitive adds colliders, and the physics classes
        // are stripped from the WebGL build.
        public static Mesh Sphere => sphere != null ? sphere : sphere = UvSphere(28, 18);
        public static Mesh Cylinder => cylinder != null ? cylinder : cylinder = CylinderMesh(24);

        static Mesh UvSphere(int lon, int lat)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            for (int j = 0; j <= lat; j++)
            {
                float v = j / (float)lat * Mathf.PI;
                for (int i = 0; i <= lon; i++)
                {
                    float u = i / (float)lon * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Sin(v) * Mathf.Cos(u), Mathf.Cos(v), Mathf.Sin(v) * Mathf.Sin(u));
                    verts.Add(n * 0.5f);
                    normals.Add(n);
                }
            }
            for (int j = 0; j < lat; j++)
            for (int i = 0; i < lon; i++)
            {
                int a = j * (lon + 1) + i, b = a + 1, c = a + lon + 1, d = c + 1;
                tris.AddRange(new[] { a, b, c, b, d, c });
            }
            return Build("Sphere", verts, normals, tris);
        }

        static Mesh CylinderMesh(int sides)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i <= sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                var n = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                verts.Add(n * 0.5f + Vector3.down);
                verts.Add(n * 0.5f + Vector3.up);
                normals.Add(n);
                normals.Add(n);
                if (i < sides)
                {
                    int b = i * 2;
                    tris.AddRange(new[] { b, b + 1, b + 2, b + 1, b + 3, b + 2 });
                }
            }
            for (int cap = -1; cap <= 1; cap += 2)
            {
                int center = verts.Count;
                verts.Add(new Vector3(0, cap, 0));
                normals.Add(new Vector3(0, cap, 0));
                for (int i = 0; i <= sides; i++)
                {
                    float a = i / (float)sides * Mathf.PI * 2f;
                    verts.Add(new Vector3(Mathf.Cos(a) * 0.5f, cap, Mathf.Sin(a) * 0.5f));
                    normals.Add(new Vector3(0, cap, 0));
                    if (i < sides) tris.AddRange(new[] { center, center + 1 + i, center + 2 + i });
                }
            }
            return Build("Cylinder", verts, normals, tris);
        }

        // Unit quad in the XY plane, uv 0..1, centered.
        public static Mesh Quad()
        {
            if (quad != null) return quad;
            quad = new Mesh { name = "Quad" };
            quad.SetVertices(new[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(-0.5f, 0.5f), new Vector3(0.5f, 0.5f) });
            quad.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) });
            quad.SetColors(new[] { Color.white, Color.white, Color.white, Color.white });
            quad.SetTriangles(new[] { 0, 2, 1, 1, 2, 3 }, 0);
            quad.RecalculateBounds();
            return quad;
        }

        // Flat quad lying in the XZ plane (for shadows), centered.
        public static Mesh FloorQuad()
        {
            if (cache.TryGetValue("floor", out var m)) return m;
            m = new Mesh { name = "FloorQuad" };
            m.SetVertices(new[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f) });
            m.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) });
            m.SetColors(new[] { Color.white, Color.white, Color.white, Color.white });
            m.SetTriangles(new[] { 0, 2, 1, 1, 2, 3 }, 0);
            m.RecalculateBounds();
            return cache["floor"] = m;
        }

        // Box with rounded edges and corners; normals are smooth across the bevels.
        public static Mesh RoundedBox(Vector3 size, float radius, int seg = 4)
        {
            string key = $"rb{size.x:F3},{size.y:F3},{size.z:F3},{radius:F3},{seg}";
            if (cache.TryGetValue(key, out var cached)) return cached;

            Vector3 h = size * 0.5f;
            radius = Mathf.Min(radius, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.99f);
            float[][] coords = { AxisCoords(h.x, radius, seg), AxisCoords(h.y, radius, seg), AxisCoords(h.z, radius, seg) };
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();

            for (int axis = 0; axis < 3; axis++)
            for (int sign = -1; sign <= 1; sign += 2)
            {
                int ua = (axis + 1) % 3, va = (axis + 2) % 3;
                float[] us = coords[ua], vs = coords[va];
                int start = verts.Count;
                var faceNormal = Vector3.zero;
                faceNormal[axis] = sign;

                for (int j = 0; j < vs.Length; j++)
                for (int i = 0; i < us.Length; i++)
                {
                    var p = Vector3.zero;
                    p[axis] = sign * h[axis];
                    p[ua] = us[i];
                    p[va] = vs[j];
                    var inner = new Vector3(
                        Mathf.Clamp(p.x, -h.x + radius, h.x - radius),
                        Mathf.Clamp(p.y, -h.y + radius, h.y - radius),
                        Mathf.Clamp(p.z, -h.z + radius, h.z - radius));
                    var d = p - inner;
                    var n = d.sqrMagnitude > 1e-10f ? d.normalized : faceNormal;
                    verts.Add(inner + n * radius);
                    normals.Add(n);
                }

                int w = us.Length;
                for (int j = 0; j < vs.Length - 1; j++)
                for (int i = 0; i < w - 1; i++)
                {
                    int a = start + j * w + i, b = a + 1, c = a + w, e = c + 1;
                    tris.AddRange(new[] { a, b, c, b, e, c });
                }
            }

            var mesh = Build("RoundedBox", verts, normals, tris);
            return cache[key] = mesh;
        }

        static float[] AxisCoords(float half, float r, int seg)
        {
            var list = new List<float>();
            for (int i = 0; i <= seg; i++) list.Add(-(half - r) - r * Mathf.Cos(i / (float)seg * Mathf.PI * 0.5f));
            for (int i = 0; i <= seg; i++) list.Add((half - r) + r * Mathf.Sin(i / (float)seg * Mathf.PI * 0.5f));
            return list.ToArray();
        }

        // Cone along +Y, base at y = 0.
        public static Mesh Cone(float radius, float height, int seg = 16)
        {
            string key = $"cone{radius}{height}{seg}";
            if (cache.TryGetValue(key, out var cached)) return cached;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            float slope = radius / height;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i / (float)seg * Mathf.PI * 2f, a1 = (i + 1) / (float)seg * Mathf.PI * 2f, am = (a0 + a1) * 0.5f;
                var p0 = new Vector3(Mathf.Cos(a0) * radius, 0, Mathf.Sin(a0) * radius);
                var p1 = new Vector3(Mathf.Cos(a1) * radius, 0, Mathf.Sin(a1) * radius);
                var n0 = new Vector3(Mathf.Cos(a0), slope, Mathf.Sin(a0)).normalized;
                var n1 = new Vector3(Mathf.Cos(a1), slope, Mathf.Sin(a1)).normalized;
                var nm = new Vector3(Mathf.Cos(am), slope, Mathf.Sin(am)).normalized;
                int s = verts.Count;
                verts.AddRange(new[] { p0, p1, new Vector3(0, height, 0) });
                normals.AddRange(new[] { n0, n1, nm });
                tris.AddRange(new[] { s, s + 1, s + 2 });
                s = verts.Count;
                verts.AddRange(new[] { p0, p1, Vector3.zero });
                normals.AddRange(new[] { Vector3.down, Vector3.down, Vector3.down });
                tris.AddRange(new[] { s, s + 1, s + 2 });
            }
            return cache[key] = Build("Cone", verts, normals, tris);
        }

        // Flat ring in the XZ plane (planet rings).
        public static Mesh Ring(float inner, float outer, int seg = 64)
        {
            var verts = new List<Vector3>();
            var cols = new List<Color>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                verts.Add(dir * inner);
                verts.Add(dir * outer);
                cols.Add(new Color(1, 1, 1, 0.15f));
                cols.Add(new Color(1, 1, 1, 0.75f));
                uvs.Add(new Vector2(0.5f, 0.5f));
                uvs.Add(new Vector2(0.5f, 0.5f));
                if (i < seg)
                {
                    int b = i * 2;
                    tris.AddRange(new[] { b, b + 1, b + 2, b + 1, b + 3, b + 2 });
                }
            }
            var m = new Mesh { name = "Ring" };
            m.SetVertices(verts);
            m.SetColors(cols);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        // Square outline in the XZ plane (landing slot indicator).
        public static Mesh Frame(float size, float thickness)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            float o = size * 0.5f, i = o - thickness;
            void Rect(float x0, float z0, float x1, float z1)
            {
                int s = verts.Count;
                verts.AddRange(new[] { new Vector3(x0, 0, z0), new Vector3(x1, 0, z0), new Vector3(x0, 0, z1), new Vector3(x1, 0, z1) });
                tris.AddRange(new[] { s, s + 2, s + 1, s + 1, s + 2, s + 3 });
            }
            Rect(-o, -o, o, -i);
            Rect(-o, i, o, o);
            Rect(-o, -i, -i, i);
            Rect(i, -i, o, i);
            var m = new Mesh { name = "Frame" };
            m.SetVertices(verts);
            var cols = new Color[verts.Count];
            var uvs = new Vector2[verts.Count];
            for (int k = 0; k < cols.Length; k++) { cols[k] = Color.white; uvs[k] = new Vector2(0.5f, 0.5f); }
            m.SetColors(cols);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        // Vertex colors bypass the gamma-to-linear conversion material colors get, so convert by hand.
        static Color VertexColor(Color c) => QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c;

        // Vertical silhouette strip: ridge line on top, fading to bottomColor below.
        public static Mesh Ridge(IList<Vector2> ridge, float bottomY, float z, Color top, Color bottom)
        {
            top = VertexColor(top);
            bottom = VertexColor(bottom);
            var verts = new List<Vector3>();
            var cols = new List<Color>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int i = 0; i < ridge.Count; i++)
            {
                verts.Add(new Vector3(ridge[i].x, ridge[i].y, z));
                verts.Add(new Vector3(ridge[i].x, bottomY, z));
                cols.Add(top);
                cols.Add(bottom);
                uvs.Add(new Vector2(0.5f, 0.5f));
                uvs.Add(new Vector2(0.5f, 0.5f));
                if (i < ridge.Count - 1)
                {
                    int b = i * 2;
                    tris.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 });
                }
            }
            var m = new Mesh { name = "Ridge" };
            m.SetVertices(verts);
            m.SetColors(cols);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        // Grid of thin lines on the XZ plane (neon floor).
        public static Mesh Grid(float halfWidth, float zNear, float zFar, float spacing, float thickness)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            void Rect(float x0, float z0, float x1, float z1)
            {
                int s = verts.Count;
                verts.AddRange(new[] { new Vector3(x0, 0, z0), new Vector3(x1, 0, z0), new Vector3(x0, 0, z1), new Vector3(x1, 0, z1) });
                tris.AddRange(new[] { s, s + 2, s + 1, s + 1, s + 2, s + 3 });
            }
            for (float x = -halfWidth; x <= halfWidth; x += spacing) Rect(x - thickness, zNear, x + thickness, zFar);
            for (float z = zNear; z <= zFar; z += spacing) Rect(-halfWidth, z - thickness, halfWidth, z + thickness);
            var m = new Mesh { name = "Grid", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(verts);
            var cols = new Color[verts.Count];
            var uvs = new Vector2[verts.Count];
            for (int k = 0; k < cols.Length; k++) { cols[k] = Color.white; uvs[k] = new Vector2(0.5f, 0.5f); }
            m.SetColors(cols);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        // Copy of a mesh with its vertices pushed in/out by noise (rocks, mounds).
        public static Mesh Lumpy(Mesh src, int seed, float amount, float frequency = 2.2f)
        {
            string key = $"lumpy{src.GetInstanceID()},{seed},{amount:F3},{frequency:F2}";
            if (cache.TryGetValue(key, out var cached)) return cached;
            var verts = src.vertices;
            var normals = src.normals;
            float o = seed * 13.17f;
            for (int i = 0; i < verts.Length; i++)
            {
                var v = verts[i];
                float n = Mathf.PerlinNoise(v.x * frequency + o, v.z * frequency + v.y * 1.7f + o * 0.5f) - 0.5f;
                verts[i] = v + normals[i] * n * amount;
            }
            var m = new Mesh { name = "Lumpy" };
            m.vertices = verts;
            m.normals = normals;
            m.triangles = src.triangles;
            m.RecalculateBounds();
            return cache[key] = m;
        }

        // Pointed crystal: a prism with a pyramid tip, base at y = 0. Flat shaded.
        public static Mesh Crystal(int sides, float radius, float height, float tip)
        {
            string key = $"crystal{sides},{radius:F3},{height:F3},{tip:F3}";
            if (cache.TryGetValue(key, out var cached)) return cached;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            var top = new Vector3(0, height + tip, 0);
            for (int i = 0; i < sides; i++)
            {
                float a0 = i / (float)sides * Mathf.PI * 2f, a1 = (i + 1) / (float)sides * Mathf.PI * 2f;
                var b0 = new Vector3(Mathf.Cos(a0) * radius, 0, Mathf.Sin(a0) * radius);
                var b1 = new Vector3(Mathf.Cos(a1) * radius, 0, Mathf.Sin(a1) * radius);
                var t0 = b0 + Vector3.up * height;
                var t1 = b1 + Vector3.up * height;
                AddFlatQuad(verts, normals, tris, b0, b1, t1, t0);
                AddFlatTri(verts, normals, tris, t0, t1, top);
            }
            return cache[key] = Build("Crystal", verts, normals, tris);
        }

        // Prism from a convex XZ outline, base at y = 0, with a small bevel on the top edge. Flat shaded sides.
        public static Mesh Extrude(Vector2[] outline, float height, float bevel)
        {
            string key = "extrude" + string.Join(";", outline) + $"{height:F3},{bevel:F3}";
            if (cache.TryGetValue(key, out var cached)) return cached;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            var center = Vector2.zero;
            foreach (var p in outline) center += p;
            center /= outline.Length;
            Vector3 Inset(Vector2 p, float y, float amount)
            {
                var d = (p - center).normalized * amount;
                return new Vector3(p.x - d.x, y, p.y - d.y);
            }
            int n = outline.Length;
            for (int i = 0; i < n; i++)
            {
                var p0 = outline[i];
                var p1 = outline[(i + 1) % n];
                AddFlatQuad(verts, normals, tris,
                    new Vector3(p0.x, 0, p0.y), new Vector3(p1.x, 0, p1.y),
                    new Vector3(p1.x, height - bevel, p1.y), new Vector3(p0.x, height - bevel, p0.y));
                AddFlatQuad(verts, normals, tris,
                    new Vector3(p0.x, height - bevel, p0.y), new Vector3(p1.x, height - bevel, p1.y),
                    Inset(p1, height, bevel), Inset(p0, height, bevel));
                AddFlatTri(verts, normals, tris, Inset(p0, height, bevel), Inset(p1, height, bevel), new Vector3(center.x, height, center.y));
                AddFlatTri(verts, normals, tris, new Vector3(p1.x, 0, p1.y), new Vector3(p0.x, 0, p0.y), new Vector3(center.x, 0, center.y));
            }
            return cache[key] = Build("Extrude", verts, normals, tris);
        }

        // Smooth tube along a polyline (vines, tendrils, pipes).
        public static Mesh Tube(IList<Vector3> path, float radius, int sides = 8, bool taper = true)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            var up = Vector3.up;
            for (int i = 0; i < path.Count; i++)
            {
                var fwd = (path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
                if (Mathf.Abs(Vector3.Dot(fwd, up)) > 0.95f) up = Vector3.right;
                var side = Vector3.Cross(fwd, up).normalized;
                var nUp = Vector3.Cross(side, fwd).normalized;
                float r = taper ? radius * Mathf.Lerp(1f, 0.35f, i / (float)(path.Count - 1)) : radius;
                for (int s = 0; s <= sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f;
                    var n = side * Mathf.Cos(a) + nUp * Mathf.Sin(a);
                    verts.Add(path[i] + n * r);
                    normals.Add(n);
                }
                if (i < path.Count - 1)
                {
                    int b = i * (sides + 1);
                    for (int s = 0; s < sides; s++)
                        tris.AddRange(new[] { b + s, b + s + sides + 1, b + s + 1, b + s + 1, b + s + sides + 1, b + s + sides + 2 });
                }
            }
            return Build("Tube", verts, normals, tris);
        }

        static void AddFlatQuad(List<Vector3> v, List<Vector3> n, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            AddFlatTri(v, n, t, a, b, c);
            AddFlatTri(v, n, t, a, c, d);
        }

        // Flat triangle whose normal points away from the mesh's vertical axis (convex shapes).
        static void AddFlatTri(List<Vector3> v, List<Vector3> n, List<int> t, Vector3 a, Vector3 b, Vector3 c)
        {
            var normal = Vector3.Cross(b - a, c - a).normalized;
            var mid = (a + b + c) / 3f;
            var outward = new Vector3(mid.x, 0, mid.z);
            if (Mathf.Abs(normal.y) > 0.9f) outward = new Vector3(0, normal.y, 0);
            if (Vector3.Dot(normal, outward) < 0f) normal = -normal;
            int s = v.Count;
            v.AddRange(new[] { a, b, c });
            n.AddRange(new[] { normal, normal, normal });
            t.AddRange(new[] { s, s + 1, s + 2 });
        }

        // Builds a lit mesh and makes every triangle face along its vertex normals.
        static Mesh Build(string name, List<Vector3> verts, List<Vector3> normals, List<int> tris)
        {
            for (int t = 0; t < tris.Count; t += 3)
            {
                int a = tris[t], b = tris[t + 1], c = tris[t + 2];
                var faceN = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
                if (Vector3.Dot(faceN, normals[a] + normals[b] + normals[c]) < 0f)
                {
                    tris[t + 1] = c;
                    tris[t + 2] = b;
                }
            }
            var m = new Mesh { name = name };
            m.SetVertices(verts);
            m.SetNormals(normals);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}
