using System.Collections.Generic;
using UnityEngine;

namespace StackJump
{
    // Distant scenery per world. The root follows the camera, so it stays framed as the tower grows.
    public static class Backdrops
    {
        public static Transform Build(WorldTheme th)
        {
            var root = new GameObject("Backdrop " + th.Name).transform;
            var rng = new System.Random(th.Seed);
            switch (th.Backdrop)
            {
                case BackdropKind.Planets: Planets(root, th, rng); break;
                case BackdropKind.Clouds: CloudSea(root, th, rng); break;
                case BackdropKind.Castle: Castle(root, th, rng); break;
                case BackdropKind.Mountains: Mountains(root, th); break;
                case BackdropKind.Hills: Hills(root, th); break;
                case BackdropKind.Grid: NeonGrid(root, th, rng); break;
            }
            return root;
        }

        static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        static void Ridge(Transform root, WorldTheme th, float z, float baseY, float amp, float freq, float seed, Color top, bool peaky)
        {
            var pts = new List<Vector2>();
            for (int i = 0; i <= 160; i++)
            {
                float x = Mathf.Lerp(-130f, 130f, i / 160f);
                float n = Mathf.PerlinNoise(x * freq + seed, seed * 0.37f) * 0.75f + Mathf.PerlinNoise(x * freq * 2.7f + seed, seed) * 0.25f;
                float h = peaky ? Mathf.Pow(1f - Mathf.Abs(n * 2f - 1f), 1.6f) : n;
                pts.Add(new Vector2(x, baseY + h * amp));
            }
            var bottom = Color.Lerp(top, th.Fog, 0.65f);
            var go = new GameObject("Ridge");
            go.transform.SetParent(root, false);
            Mats.AddRenderer(go, MeshFactory.Ridge(pts, baseY - 60f, z, top, bottom), Mats.UnlitOpaque);
        }

        static Renderer Prop(Transform root, Mesh mesh, Color color, Vector3 pos, Vector3 scale, float flat, float emission = 0f, float gloss = 0f)
        {
            var go = new GameObject("Prop");
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var r = Mats.AddRenderer(go, mesh, Mats.Toon);
            Mats.Set(r, color, gloss, emission, 0f, flat, 0.1f);
            return r;
        }

        static void Planets(Transform root, WorldTheme th, System.Random rng)
        {
            var big = Prop(root, MeshFactory.Sphere, th.Far, new Vector3(-17f, 13f, 70f), Vector3.one * 16f, 0f, 0.05f, 0.1f);
            var ring = new GameObject("Ring");
            ring.transform.SetParent(big.transform, false);
            ring.transform.localRotation = Quaternion.Euler(14f, 0, 18f);
            Mats.AddRenderer(ring, MeshFactory.Ring(0.68f, 1.05f), Mats.MakeUnlit(UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha, false, 2500, 0.4f, null, Color.Lerp(th.Far, Color.white, 0.45f)));
            Prop(root, MeshFactory.Sphere, th.Accent, new Vector3(15f, 8f, 48f), Vector3.one * 4.5f, 0f, 0.05f, 0.2f);
            Prop(root, MeshFactory.Sphere, Color.Lerp(th.Accent, th.Far, 0.6f), new Vector3(9f, 15f, 60f), Vector3.one * 1.6f, 0f);

            for (int i = 0; i < 14; i++)
            {
                var pos = new Vector3(R(rng, -22f, 22f), R(rng, -6f, 14f), R(rng, 14f, 34f));
                if (Mathf.Abs(pos.x) < 4f) pos.x += Mathf.Sign(pos.x) * 4f;
                var rock = Prop(root, MeshFactory.RoundedBox(new Vector3(1f, 0.8f, 0.9f), 0.36f), Color.Lerp(th.Near, th.Far, 0.3f), pos, Vector3.one * R(rng, 0.5f, 1.6f), 0f, 0f, 0.2f);
                rock.transform.localRotation = Random.rotationUniform;
                rock.gameObject.AddComponent<Drifter>().Setup(R(rng, -12f, 12f), R(rng, 0.2f, 0.6f), R(rng, 0f, 6f));
            }
        }

        static void CloudSea(Transform root, WorldTheme th, System.Random rng)
        {
            for (int i = 0; i < 18; i++)
            {
                var cluster = new GameObject("Cloud").transform;
                cluster.SetParent(root, false);
                cluster.localPosition = new Vector3(R(rng, -34f, 34f), R(rng, -4f, 16f), R(rng, 22f, 55f));
                if (Mathf.Abs(cluster.localPosition.x) < 7f) cluster.localPosition += new Vector3(Mathf.Sign(cluster.localPosition.x) * 7f, 0, 0);
                float size = R(rng, 1.6f, 3.6f);
                int puffs = rng.Next(4, 7);
                for (int p = 0; p < puffs; p++)
                {
                    float t = p / (float)(puffs - 1) - 0.5f;
                    float s = size * (1f - Mathf.Abs(t) * 0.9f) * R(rng, 0.8f, 1.1f);
                    Prop(cluster, MeshFactory.Sphere, Color.Lerp(th.Near, th.Far, R(rng, 0f, 0.6f)), new Vector3(t * size * 2.4f, s * 0.15f, R(rng, -0.3f, 0.3f)), new Vector3(s, s * 0.8f, s), 0f, 0.12f);
                }
                cluster.gameObject.AddComponent<Drifter>().Setup(0f, R(rng, 0.15f, 0.4f), R(rng, 0f, 6f), 1.5f);
            }
            // A sea of clouds far below.
            for (int i = 0; i < 26; i++)
            {
                float s = R(rng, 6f, 12f);
                Prop(root, MeshFactory.Sphere, Color.Lerp(th.Near, th.Far, R(rng, 0.3f, 1f)), new Vector3(R(rng, -60f, 60f), R(rng, -12f, -9f), R(rng, 25f, 80f)), new Vector3(s, s * 0.45f, s), 0f, 0.1f);
            }
        }

        static void Castle(Transform root, WorldTheme th, System.Random rng)
        {
            var c = th.Far;
            float flat = 1f;
            var castle = new GameObject("Castle").transform;
            castle.SetParent(root, false);
            castle.localPosition = new Vector3(-9f, 0f, 58f);

            Prop(castle, MeshFactory.RoundedBox(new Vector3(18f, 6f, 2f), 0.05f), c, new Vector3(0, 3f, 0), Vector3.one, flat);
            for (float x = -8.5f; x <= 8.5f; x += 1.7f)
                Prop(castle, MeshFactory.RoundedBox(new Vector3(0.9f, 0.9f, 2f), 0.05f), c, new Vector3(x, 6.4f, 0), Vector3.one, flat);
            var towers = new[] { (-9f, 1.6f, 11f), (9f, 1.6f, 11f), (0f, 2.2f, 15f), (-4.5f, 1.2f, 8.5f), (4.5f, 1.2f, 9.5f) };
            foreach (var (x, r, h) in towers)
            {
                float z = x == 0f ? 1.5f : 0.6f;
                Prop(castle, MeshFactory.Cylinder, c, new Vector3(x, h * 0.5f, z), new Vector3(r * 2f, h * 0.5f, r * 2f), flat);
                Prop(castle, MeshFactory.Cone(1f, 1f), c, new Vector3(x, h, z), new Vector3(r * 1.25f, r * 2.6f, r * 1.25f), flat);
                for (int w = 0; w < 2; w++)
                    Prop(castle, MeshFactory.RoundedBox(new Vector3(0.45f, 0.8f, 0.2f), 0.1f), th.Accent, new Vector3(x + (w - 0.5f) * r * 0.6f, h * (0.55f + w * 0.2f), z - r - 0.05f), Vector3.one, 1f, 1.2f);
            }

            Ridge(root, th, 46f, -3f, 4f, 0.03f, th.Seed, th.Near, false);
            Ridge(root, th, 75f, 0f, 7f, 0.02f, th.Seed + 5, Color.Lerp(th.Near, th.Fog, 0.5f), false);
        }

        static void Mountains(Transform root, WorldTheme th)
        {
            Ridge(root, th, 88f, 2f, 16f, 0.035f, th.Seed, Color.Lerp(th.Far, th.Fog, 0.35f), true);
            Ridge(root, th, 66f, -1f, 12f, 0.045f, th.Seed + 3, Color.Lerp(th.Far, th.Near, 0.5f), true);
            Ridge(root, th, 44f, -4f, 6f, 0.03f, th.Seed + 7, th.Near, false);
        }

        static void Hills(Transform root, WorldTheme th)
        {
            Ridge(root, th, 80f, 1f, 9f, 0.025f, th.Seed, Color.Lerp(th.Far, th.Fog, 0.3f), false);
            Ridge(root, th, 58f, -2f, 7f, 0.04f, th.Seed + 3, Color.Lerp(th.Far, th.Near, 0.5f), false);
            Ridge(root, th, 40f, -4f, 4f, 0.05f, th.Seed + 7, th.Near, false);
        }

        static void NeonGrid(Transform root, WorldTheme th, System.Random rng)
        {
            var grid = new GameObject("Grid");
            grid.transform.SetParent(root, false);
            grid.transform.localPosition = new Vector3(0, -3f, 0);
            Mats.AddRenderer(grid, MeshFactory.Grid(90f, 6f, 130f, 4f, 0.06f),
                Mats.MakeUnlit(UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.One, false, 2500, 1f, null, th.Palette[0] * 1.6f));

            for (int i = 0; i < 34; i++)
            {
                float x = R(rng, -80f, 80f);
                if (Mathf.Abs(x) < 6f) continue;
                float h = R(rng, 5f, 22f), w = R(rng, 3f, 7f);
                var pos = new Vector3(x, -3f + h * 0.5f, R(rng, 70f, 95f));
                Prop(root, MeshFactory.RoundedBox(new Vector3(w, h, w), 0.05f), Color.Lerp(th.Near, th.Far, R(rng, 0f, 1f)), pos, Vector3.one, 1f);
                if (rng.NextDouble() < 0.6)
                    Prop(root, MeshFactory.RoundedBox(new Vector3(w * 0.8f, 0.12f, 0.1f), 0.03f), rng.NextDouble() < 0.5 ? th.Accent : th.Palette[0], pos + new Vector3(0, h * R(rng, -0.2f, 0.4f), -w * 0.5f - 0.1f), Vector3.one, 1f, 1.5f);
            }
        }
    }

    // Gentle floating motion for backdrop props.
    public class Drifter : MonoBehaviour
    {
        float spin, speed, phase, sway;
        Vector3 home;

        public void Setup(float spinDegrees, float bobSpeed, float bobPhase, float swayAmount = 0.4f)
        {
            spin = spinDegrees; speed = bobSpeed; phase = bobPhase; sway = swayAmount;
            home = transform.localPosition;
        }

        void Update()
        {
            float t = Time.time * speed + phase;
            transform.localPosition = home + new Vector3(Mathf.Sin(t * 0.7f) * sway, Mathf.Sin(t) * 0.35f, 0);
            if (spin != 0f) transform.Rotate(Vector3.up, spin * Time.deltaTime, Space.World);
        }
    }
}
