using System.Collections.Generic;
using UnityEngine;

namespace StackJump
{
    // A themed block per world: moon rock, beanstalk, coffin, hay bale, kitchen appliance,
    // castle brick or circuit chip. Origin at the bottom center; the top surface is at BlockH.
    public static class BlockFactory
    {
        const float H = Tower.BlockH;
        const float S = Tower.BlockSize;

        public static GameObject Create(WorldTheme th, int index)
        {
            var root = new GameObject("Block " + index);
            var rng = new System.Random(index * 7919 + th.Seed);
            var color = th.BlockColor(index);
            switch (th.Kind)
            {
                case WorldKind.Cosmos: MoonRock(root.transform, th, color, rng); break;
                case WorldKind.Beanstalk: Beanstalk(root.transform, th, color, rng); break;
                case WorldKind.Haunted: Coffin(root.transform, th, color, rng); break;
                case WorldKind.Farm: HayBale(root.transform, th, color, rng); break;
                case WorldKind.Kitchen: Appliance(root.transform, th, color, rng, index); break;
                case WorldKind.Castle: Brick(root.transform, th, color, rng); break;
                default: Chip(root.transform, th, color, rng); break;
            }
            return root;
        }

        static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        static Renderer Part(Transform parent, Mesh mesh, Color color, Vector3 pos, Vector3 scale, Vector3 euler = default,
                             float gloss = 0.15f, float emission = 0f, float grad = 0.12f)
        {
            var go = new GameObject("Part");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler);
            var r = Mats.AddRenderer(go, mesh, Mats.Toon);
            Mats.Set(r, color, gloss, emission, grad, 0f, 0.14f);
            return r;
        }

        static Renderer Box(Transform parent, Vector3 size, float radius, Color color, Vector3 pos, Vector3 euler = default,
                            float gloss = 0.15f, float emission = 0f, float grad = 0.12f)
            => Part(parent, MeshFactory.RoundedBox(size, radius), color, pos, Vector3.one, euler, gloss, emission, grad);

        // ---------- Cosmos: stacked lumpy moon-rock slabs with glowing crystals ----------
        static void MoonRock(Transform root, WorldTheme th, Color color, System.Random rng)
        {
            for (int i = 0; i < 2; i++)
            {
                var slab = MeshFactory.Lumpy(MeshFactory.RoundedBox(new Vector3(S, 0.32f, S), 0.13f, 5), rng.Next(0, 4), 0.09f);
                float sc = R(rng, 0.94f, 1.02f);
                var r = Part(root, slab, Color.Lerp(color, Color.white, i * 0.07f), new Vector3(R(rng, -0.05f, 0.05f), 0.16f + i * 0.285f, R(rng, -0.05f, 0.05f)),
                             new Vector3(sc, 1f, sc), new Vector3(0, R(rng, -6f, 6f), 0), 0.12f);
                Mats.SetPattern(r, Pattern.Rock, Color.Lerp(color, Color.black, 0.5f), 1.1f);
            }
            int crystals = rng.Next(0, 3);
            for (int i = 0; i < crystals; i++)
            {
                float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                var pos = new Vector3(side * R(rng, 0.45f, 0.7f), R(rng, 0.25f, 0.45f), R(rng, -0.75f, -0.5f));
                var tilt = new Vector3(R(rng, -35f, -15f), R(rng, 0, 360), side * R(rng, 15f, 40f));
                var c = Color.Lerp(th.Trim, Color.white, 0.2f);
                Part(root, MeshFactory.Crystal(6, 0.07f, 0.16f, 0.1f), c, pos, Vector3.one * R(rng, 0.9f, 1.4f), tilt, 0.9f, 1.4f, 0f);
            }
        }

        // ---------- Beanstalk: grooved stalk segment wrapped in a vine with leaves ----------
        static void Beanstalk(Transform root, WorldTheme th, Color color, System.Random rng)
        {
            var stalk = Part(root, MeshFactory.Cylinder, color, new Vector3(0, H * 0.5f, 0), new Vector3(1.5f, H * 0.5f, 1.5f), new Vector3(0, R(rng, 0, 360), 0), 0.25f, 0f, 0.18f);
            Mats.SetPattern(stalk, Pattern.Stalk, color, 1f);
            Part(root, MeshFactory.Cylinder, Color.Lerp(color, th.Trim, 0.45f), new Vector3(0, H - 0.03f, 0), new Vector3(1.58f, 0.03f, 1.58f), default, 0.2f);

            float start = R(rng, 0f, Mathf.PI * 2f);
            var path = new List<Vector3>();
            for (int i = 0; i <= 24; i++)
            {
                float t = i / 24f;
                float a = start + t * Mathf.PI * 1.6f;
                float r = 0.78f + Mathf.Sin(t * 9f) * 0.03f;
                path.Add(new Vector3(Mathf.Cos(a) * r, 0.08f + t * (H - 0.16f), Mathf.Sin(a) * r));
            }
            Part(root, MeshFactory.Tube(path, 0.05f, 8, false), th.Trim, Vector3.zero, Vector3.one, default, 0.3f, 0f, 0f);

            // Curly tendril at the end of the vine.
            var curl = new List<Vector3>();
            var end = path[path.Count - 1];
            var outDir = new Vector3(end.x, 0, end.z).normalized;
            var tangent = Vector3.Cross(Vector3.up, outDir);
            for (int i = 0; i <= 14; i++)
            {
                float t = i / 14f;
                float a = t * Mathf.PI * 3f;
                float r = 0.13f * (1f - t * 0.75f);
                curl.Add(end + outDir * (0.05f + t * 0.12f) + tangent * Mathf.Cos(a) * r + Vector3.up * (Mathf.Sin(a) * r + 0.02f));
            }
            Part(root, MeshFactory.Tube(curl, 0.025f, 6), th.Trim, Vector3.zero, Vector3.one, default, 0.3f, 0f, 0f);

            int leaves = rng.Next(1, 3);
            for (int i = 0; i < leaves; i++)
            {
                var p = path[rng.Next(4, path.Count - 4)];
                var dir = new Vector3(p.x, 0, p.z).normalized;
                float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                Part(root, MeshFactory.Sphere, Color.Lerp(th.Trim, color, 0.35f), p + dir * 0.16f, new Vector3(0.18f, 0.035f, 0.3f), new Vector3(R(rng, -25f, -5f), yaw, R(rng, -20f, 20f)), 0.4f, 0f, 0f);
            }
        }

        // ---------- Haunted: wooden coffin with lid and cross ----------
        static readonly Vector2[] CoffinOutline =
        {
            new(-0.8f, -0.3f), new(-0.42f, -0.52f), new(0.8f, -0.38f), new(0.8f, 0.38f), new(-0.42f, 0.52f), new(-0.8f, 0.3f),
        };

        static void Coffin(Transform root, WorldTheme th, Color color, System.Random rng)
        {
            var holder = new GameObject("Coffin").transform;
            holder.SetParent(root, false);
            holder.localRotation = Quaternion.Euler(0, rng.NextDouble() < 0.5 ? 0f : 180f, 0);
            var body = Part(holder, MeshFactory.Extrude(CoffinOutline, 0.5f, 0.03f), color, Vector3.zero, Vector3.one, default, 0.1f, 0f, 0.1f);
            Mats.SetPattern(body, Pattern.Planks, color, 1f);
            var lidOutline = new Vector2[CoffinOutline.Length];
            for (int i = 0; i < lidOutline.Length; i++) lidOutline[i] = CoffinOutline[i] * 0.94f;
            var lid = Part(holder, MeshFactory.Extrude(lidOutline, 0.1f, 0.035f), Color.Lerp(color, Color.white, 0.12f), new Vector3(0, 0.5f, 0), Vector3.one, default, 0.15f, 0f, 0f);
            Mats.SetPattern(lid, Pattern.Planks, color, 1.3f);
            var cross = Color.Lerp(th.Trim, Color.black, 0.2f);
            Box(holder, new Vector3(0.42f, 0.03f, 0.09f), 0.012f, cross, new Vector3(-0.1f, H + 0.012f, 0f), default, 0.3f);
            Box(holder, new Vector3(0.09f, 0.03f, 0.3f), 0.012f, cross, new Vector3(-0.2f, H + 0.012f, 0f), default, 0.3f);
            // Cross on both long sides so it reads from the camera whichever way the coffin faces.
            for (int s = -1; s <= 1; s += 2)
            {
                float yaw = s * -6.6f;
                Box(holder, new Vector3(0.07f, 0.3f, 0.03f), 0.012f, cross, new Vector3(0.25f, 0.27f, s * 0.452f), new Vector3(0, yaw, 0), 0.3f);
                Box(holder, new Vector3(0.2f, 0.07f, 0.03f), 0.012f, cross, new Vector3(0.25f, 0.32f, s * 0.455f), new Vector3(0, yaw, 0), 0.3f);
            }
            // Handles on the long sides.
            for (int s = -1; s <= 1; s += 2)
            for (int k = 0; k < 2; k++)
                Box(holder, new Vector3(0.16f, 0.05f, 0.04f), 0.02f, H2("C9A45A"), new Vector3(-0.1f + k * 0.55f, 0.25f, s * (0.47f - k * 0.04f)), default, 0.8f);
        }

        static Color H2(string hex) => Mats.Hex(hex);

        // ---------- Farm: hay bale with twine ----------
        static void HayBale(Transform root, WorldTheme th, Color color, System.Random rng)
        {
            var bale = Box(root, new Vector3(S, H, S * 0.92f), 0.13f, color, new Vector3(0, H * 0.5f, 0), new Vector3(0, R(rng, -3f, 3f), 0), 0.05f, 0f, 0.1f);
            Mats.SetPattern(bale, Pattern.Straw, Color.Lerp(color, Color.white, 0.5f), 1f);
            for (int s = -1; s <= 1; s += 2)
                Box(root, new Vector3(0.05f, H + 0.02f, S * 0.92f + 0.02f), 0.02f, th.Trim, new Vector3(s * 0.42f, H * 0.5f, 0), default, 0.1f, 0f, 0f);
            int sticks = rng.Next(3, 7);
            for (int i = 0; i < sticks; i++)
            {
                var pos = new Vector3(R(rng, -0.7f, 0.7f), H, R(rng, -0.65f, 0.65f));
                Part(root, MeshFactory.Cylinder, Color.Lerp(color, Color.white, 0.3f), pos, new Vector3(0.018f, R(rng, 0.08f, 0.14f), 0.018f), new Vector3(R(rng, 50f, 80f), R(rng, 0, 360), 0), 0.1f, 0f, 0f);
            }
        }

        // ---------- Kitchen: ovens, microwaves and drawers ----------
        static void Appliance(Transform root, WorldTheme th, Color color, System.Random rng, int index)
        {
            var chrome = th.Trim;
            var glass = H2("1B1420");
            Box(root, new Vector3(S, H, S * 0.88f), 0.07f, color, new Vector3(0, H * 0.5f, 0), default, 0.55f, 0f, 0.08f);
            float front = -S * 0.44f;
            switch (index % 3)
            {
                case 0: // oven
                    Box(root, new Vector3(1.3f, 0.4f, 0.05f), 0.04f, Color.Lerp(color, Color.black, 0.12f), new Vector3(0, 0.24f, front), default, 0.5f);
                    Box(root, new Vector3(0.9f, 0.22f, 0.03f), 0.04f, glass, new Vector3(0, 0.22f, front - 0.03f), default, 0.95f, 0.15f);
                    Part(root, MeshFactory.Cylinder, chrome, new Vector3(0, 0.4f, front - 0.08f), new Vector3(0.035f, 0.5f, 0.035f), new Vector3(0, 0, 90), 0.95f);
                    for (int k = -1; k <= 1; k++)
                        Part(root, MeshFactory.Cylinder, chrome, new Vector3(k * 0.4f, 0.52f, front - 0.02f), new Vector3(0.1f, 0.025f, 0.1f), new Vector3(90, 0, 0), 0.9f);
                    break;
                case 1: // microwave
                    Box(root, new Vector3(0.95f, 0.38f, 0.03f), 0.04f, glass, new Vector3(-0.22f, 0.3f, front - 0.01f), default, 0.95f, 0.1f);
                    Box(root, new Vector3(0.3f, 0.09f, 0.03f), 0.02f, H2("39FF9A"), new Vector3(0.52f, 0.43f, front - 0.01f), default, 0.3f, 1.5f);
                    for (int y = 0; y < 2; y++)
                    for (int x = 0; x < 3; x++)
                        Box(root, new Vector3(0.07f, 0.05f, 0.03f), 0.015f, chrome, new Vector3(0.43f + x * 0.09f, 0.3f - y * 0.09f, front - 0.01f), default, 0.8f);
                    break;
                default: // drawers
                    for (int k = 0; k < 2; k++)
                    {
                        Box(root, new Vector3(1.45f, 0.22f, 0.04f), 0.03f, Color.Lerp(color, Color.white, 0.08f), new Vector3(0, 0.16f + k * 0.27f, front), default, 0.5f);
                        Part(root, MeshFactory.Sphere, chrome, new Vector3(0, 0.16f + k * 0.27f, front - 0.05f), new Vector3(0.09f, 0.09f, 0.06f), default, 0.95f);
                    }
                    break;
            }
        }

        // ---------- Castle: brick block with a stone cap ----------
        static void Brick(Transform root, WorldTheme th, Color color, System.Random rng)
        {
            var body = Box(root, new Vector3(S, H - 0.08f, S), 0.04f, color, new Vector3(0, (H - 0.08f) * 0.5f, 0), new Vector3(0, R(rng, -2f, 2f), 0), 0.08f, 0f, 0.1f);
            Mats.SetPattern(body, Pattern.Bricks, th.Trim, 1f);
            var cap = Box(root, new Vector3(S + 0.08f, 0.09f, S + 0.08f), 0.03f, Color.Lerp(th.Trim, color, 0.25f), new Vector3(0, H - 0.045f, 0), default, 0.1f, 0f, 0f);
            Mats.SetPattern(cap, Pattern.Rock, Color.Lerp(th.Trim, Color.black, 0.3f), 2f);
        }

        // ---------- Neon: circuit chip with glowing traces and pins ----------
        static void Chip(Transform root, WorldTheme th, Color color, System.Random rng)
        {
            var body = Box(root, new Vector3(S * 0.94f, H * 0.82f, S * 0.94f), 0.06f, Color.Lerp(color, Color.black, 0.45f), new Vector3(0, H * 0.41f + 0.05f, 0), default, 0.6f, 0f, 0f);
            Mats.SetPattern(body, Pattern.Circuit, rng.NextDouble() < 0.5 ? th.Trim : color, 1f, 2.2f);
            Box(root, new Vector3(S * 0.5f, 0.03f, S * 0.5f), 0.02f, Color.Lerp(color, Color.white, 0.2f), new Vector3(0, H - 0.01f, 0), default, 0.7f, 0.6f);
            var pin = H2("C8CCD8");
            for (int s = -1; s <= 1; s += 2)
            for (int k = 0; k < 6; k++)
            {
                float x = -0.6f + k * 0.24f;
                Box(root, new Vector3(0.08f, 0.14f, 0.1f), 0.02f, pin, new Vector3(x, 0.08f, s * S * 0.49f), default, 0.9f);
            }
        }
    }
}
