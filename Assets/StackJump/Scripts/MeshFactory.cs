using System.Collections.Generic;
using UnityEngine;

namespace StackJump
{
    // Procedural meshes, so the game ships with no model assets.
    public static class MeshFactory
    {
        static Mesh sphere, cylinder, quad;
        static readonly Dictionary<string, Mesh> cache = new();

        public static Mesh Sphere => sphere != null ? sphere : sphere = Primitive(PrimitiveType.Sphere);
        public static Mesh Cylinder => cylinder != null ? cylinder : cylinder = Primitive(PrimitiveType.Cylinder);

        static Mesh Primitive(PrimitiveType type)
        {
            var go = GameObject.CreatePrimitive(type);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(go);
            return mesh;
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
