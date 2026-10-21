using System.Collections.Generic;
using UnityEngine;

namespace StackJump
{
    // The world the tower starts from: a themed ground, a pedestal under the first block and props
    // scattered around it. World-fixed at the origin, so it drops out of view as the tower climbs.
    public static class Ground
    {
        public static Transform Build(WorldTheme th, float[] pedestals)
        {
            var root = new GameObject("Ground " + th.Name).transform;
            var rng = new System.Random(th.Seed * 31);

            // Wide enough that its edge sits deep in the fog, giving a soft horizon.
            var floor = Prop(root, MeshFactory.Cylinder, th.Ground, new Vector3(0, -0.06f, 0), new Vector3(400f, 0.05f, 400f), 0.05f);
            var floorPattern = th.Kind switch
            {
                WorldKind.Cosmos => Pattern.Rock,
                WorldKind.Kitchen or WorldKind.Castle => Pattern.Tiles,
                WorldKind.Neon => Pattern.None,
                _ => Pattern.Grass,
            };
            // Patterns run in object space, so undo the floor's 400x scale.
            float patternScale = th.Kind switch
            {
                WorldKind.Kitchen => 200f,
                WorldKind.Castle => 267f,
                WorldKind.Cosmos => 178f,
                _ => 400f,
            };
            Mats.SetPattern(floor, floorPattern, th.GroundAlt, patternScale);

            foreach (float x in pedestals)
            {
                var spot = new GameObject("Pedestal").transform;
                spot.SetParent(root, false);
                spot.localPosition = new Vector3(x, 0, 0);
                Pedestal(spot, th, rng);
            }
            switch (th.Kind)
            {
                case WorldKind.Cosmos: CosmosProps(root, th, rng); break;
                case WorldKind.Beanstalk: MeadowProps(root, th, rng); break;
                case WorldKind.Haunted: GraveyardProps(root, th, rng); break;
                case WorldKind.Farm: FarmProps(root, th, rng); break;
                case WorldKind.Kitchen: KitchenProps(root, th, rng); break;
                case WorldKind.Castle: CourtyardProps(root, th, rng); break;
                case WorldKind.Neon: NeonProps(root, th, rng); break;
            }
            return root;
        }

        static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        static Renderer Prop(Transform parent, Mesh mesh, Color color, Vector3 pos, Vector3 scale, float gloss = 0.1f, float emission = 0f, Vector3 euler = default, float grad = 0f)
        {
            var go = new GameObject("Prop");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler);
            var r = Mats.AddRenderer(go, mesh, Mats.Toon);
            Mats.Set(r, color, gloss, emission, grad, 0f, 0.1f);
            return r;
        }

        static Renderer Box(Transform parent, Vector3 size, float radius, Color color, Vector3 pos, float gloss = 0.1f, float emission = 0f, Vector3 euler = default)
            => Prop(parent, MeshFactory.RoundedBox(size, radius), color, pos, Vector3.one, gloss, emission, euler);

        // Spots around the tower that keep the block lane (|z| small near x = 0) clear.
        static Vector3 Spot(System.Random rng, float minR, float maxR)
        {
            for (int i = 0; i < 20; i++)
            {
                float a = R(rng, 0f, Mathf.PI * 2f);
                float r = R(rng, minR, maxR);
                var p = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r + 3f);
                if (Mathf.Abs(p.z) > 1.6f && p.z > -2.5f) return p;
            }
            return new Vector3(R(rng, -8f, 8f), 0, R(rng, 4f, 12f));
        }

        static void Pedestal(Transform root, WorldTheme th, System.Random rng)
        {
            switch (th.Kind)
            {
                case WorldKind.Cosmos:
                    for (int i = 0; i < 3; i++)
                    {
                        var rock = MeshFactory.Lumpy(MeshFactory.RoundedBox(new Vector3(1.6f, 0.7f, 1.4f), 0.3f, 5), i, 0.2f);
                        var r = Prop(root, rock, Color.Lerp(th.Ground, th.Palette[0], 0.25f), new Vector3((i - 1) * 0.9f, -0.32f + (i == 1 ? 0.12f : 0f), i == 1 ? 0f : 0.4f), Vector3.one * (i == 1 ? 1.25f : 1f), 0.1f, 0f, new Vector3(0, i * 50f, 0));
                        Mats.SetPattern(r, Pattern.Rock, th.GroundAlt, 1f);
                    }
                    break;
                case WorldKind.Beanstalk:
                {
                    var mound = Prop(root, MeshFactory.Lumpy(MeshFactory.Sphere, 2, 0.12f, 3f), th.Ground, new Vector3(0, -0.35f, 0), new Vector3(3.4f, 0.9f, 3f), 0.1f);
                    Mats.SetPattern(mound, Pattern.Grass, th.GroundAlt, 1.2f);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i / 6f * 360f + 20f;
                        var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                        Prop(root, MeshFactory.Sphere, Color.Lerp(th.Palette[1], th.Trim, 0.4f), dir * 1.45f + Vector3.up * 0.05f, new Vector3(0.42f, 0.06f, 0.7f), 0.35f, 0f, new Vector3(-12f, a, 0));
                    }
                    break;
                }
                case WorldKind.Haunted:
                case WorldKind.Castle:
                {
                    var stone = th.Kind == WorldKind.Haunted ? Mats.Hex("6E7A72") : Mats.Hex("D8C7B0");
                    var b = Box(root, new Vector3(2.3f, 0.35f, 2.3f), 0.06f, stone, new Vector3(0, -0.175f, 0), 0.1f);
                    Mats.SetPattern(b, Pattern.Bricks, Color.Lerp(stone, Color.black, 0.35f), 1.4f);
                    var c = Box(root, new Vector3(2.7f, 0.18f, 2.7f), 0.05f, Color.Lerp(stone, Color.black, 0.15f), new Vector3(0, -0.4f, 0), 0.1f);
                    Mats.SetPattern(c, Pattern.Rock, Color.Lerp(stone, Color.black, 0.4f), 1.5f);
                    break;
                }
                case WorldKind.Farm:
                case WorldKind.Kitchen:
                {
                    var wood = th.Kind == WorldKind.Farm ? Mats.Hex("A86B3C") : Mats.Hex("C98E58");
                    for (int i = 0; i < 4; i++)
                    {
                        var plank = Box(root, new Vector3(2.4f, 0.12f, 0.56f), 0.04f, Color.Lerp(wood, Color.white, (i % 2) * 0.08f), new Vector3(0, -0.06f, -0.9f + i * 0.6f), 0.15f, 0f, new Vector3(0, R(rng, -2f, 2f), 0));
                        Mats.SetPattern(plank, Pattern.Planks, wood, 2f);
                    }
                    for (int s = -1; s <= 1; s += 2)
                        Box(root, new Vector3(0.35f, 0.2f, 2.4f), 0.04f, Color.Lerp(wood, Color.black, 0.25f), new Vector3(s * 0.9f, -0.22f, 0), 0.1f);
                    break;
                }
                case WorldKind.Neon:
                {
                    Box(root, new Vector3(2.4f, 0.3f, 2.4f), 0.08f, th.Ground, new Vector3(0, -0.15f, 0), 0.7f);
                    var ring = new GameObject("Ring");
                    ring.transform.SetParent(root, false);
                    ring.transform.localPosition = new Vector3(0, 0.005f, 0);
                    Mats.AddRenderer(ring, MeshFactory.Frame(2.3f, 0.06f), Mats.MakeUnlit(UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.One, false, 3000, 0f, null, th.Trim * 2f));
                    break;
                }
            }
        }

        static void CosmosProps(Transform root, WorldTheme th, System.Random rng)
        {
            for (int i = 0; i < 7; i++)
            {
                var p = Spot(rng, 3f, 12f);
                float s = R(rng, 0.8f, 2f);
                Prop(root, MeshFactory.Cylinder, th.GroundAlt, p + Vector3.up * 0.01f, new Vector3(s, 0.02f, s * 0.8f), 0f);
                Prop(root, MeshFactory.Lumpy(MeshFactory.Sphere, i, 0.15f), Color.Lerp(th.Ground, Color.white, 0.12f), p + Vector3.up * -0.02f, new Vector3(s * 1.2f, 0.12f, s), 0.05f);
            }
            for (int i = 0; i < 6; i++)
            {
                var p = Spot(rng, 2.6f, 10f);
                int n = rng.Next(2, 5);
                var c = i % 2 == 0 ? th.Trim : th.Palette[1];
                for (int k = 0; k < n; k++)
                    Prop(root, MeshFactory.Crystal(6, 0.12f, 0.4f, 0.2f), c, p, Vector3.one * R(rng, 0.6f, 1.5f), 0.9f, 1.3f, new Vector3(R(rng, -30f, 30f), R(rng, 0, 360), R(rng, -30f, 30f)));
            }
            for (int i = 0; i < 8; i++)
            {
                var p = Spot(rng, 2.5f, 13f);
                Prop(root, MeshFactory.Lumpy(MeshFactory.RoundedBox(Vector3.one, 0.35f, 4), i, 0.25f), Color.Lerp(th.Ground, th.Palette[2], 0.3f), p + Vector3.up * 0.15f, Vector3.one * R(rng, 0.3f, 0.8f), 0.1f, 0f, new Vector3(R(rng, 0, 360), R(rng, 0, 360), 0));
            }
        }

        static void MeadowProps(Transform root, WorldTheme th, System.Random rng)
        {
            for (int i = 0; i < 9; i++)
            {
                var p = Spot(rng, 2.8f, 13f);
                int puffs = rng.Next(3, 6);
                float s = R(rng, 0.5f, 1.1f);
                for (int k = 0; k < puffs; k++)
                    Prop(root, MeshFactory.Sphere, Color.Lerp(th.Palette[1], th.Trim, R(rng, 0f, 0.6f)), p + new Vector3(R(rng, -0.6f, 0.6f) * s, s * 0.35f, R(rng, -0.4f, 0.4f) * s), Vector3.one * s * R(rng, 0.7f, 1.1f), 0.25f);
            }
            var petals = new[] { Mats.Hex("FF6FA8"), Mats.Hex("FFE15A"), Mats.Hex("FFFFFF"), Mats.Hex("B48CFF") };
            for (int i = 0; i < 22; i++)
            {
                var p = Spot(rng, 2.2f, 10f);
                float h = R(rng, 0.15f, 0.35f);
                Prop(root, MeshFactory.Cylinder, th.Trim, p + Vector3.up * h * 0.5f, new Vector3(0.025f, h * 0.5f, 0.025f), 0f);
                Prop(root, MeshFactory.Sphere, petals[rng.Next(petals.Length)], p + Vector3.up * h, new Vector3(0.16f, 0.06f, 0.16f), 0.3f);
                Prop(root, MeshFactory.Sphere, Mats.Hex("FFC23A"), p + Vector3.up * (h + 0.02f), Vector3.one * 0.06f, 0.3f);
            }
        }

        static void GraveyardProps(Transform root, WorldTheme th, System.Random rng)
        {
            var stone = Mats.Hex("8E9A90");
            for (int i = 0; i < 8; i++)
            {
                var p = Spot(rng, 2.8f, 12f);
                float tilt = R(rng, -12f, 12f);
                var t = Box(root, new Vector3(0.6f, 0.8f, 0.16f), 0.18f, stone, p + Vector3.up * 0.3f, 0.05f, 0f, new Vector3(0, R(rng, -20f, 20f), tilt));
                Mats.SetPattern(t, Pattern.Rock, Color.Lerp(stone, Color.black, 0.4f), 2f);
                Box(root, new Vector3(0.7f, 0.08f, 0.4f), 0.03f, th.GroundAlt, p + Vector3.up * 0.02f, 0.05f);
            }
            for (int i = 0; i < 6; i++)
            {
                var p = Spot(rng, 2.5f, 10f);
                float s = R(rng, 0.35f, 0.6f);
                Prop(root, MeshFactory.Sphere, Mats.Hex("FF8A1F"), p + Vector3.up * s * 0.38f, new Vector3(s, s * 0.78f, s), 0.35f, 0.05f);
                Prop(root, MeshFactory.Cylinder, Mats.Hex("4E6B2A"), p + Vector3.up * s * 0.8f, new Vector3(0.05f, 0.08f, 0.05f), 0f);
                if (rng.NextDouble() < 0.6)
                    Box(root, new Vector3(s * 0.5f, s * 0.12f, 0.02f), 0.01f, Mats.Hex("FFD24A"), p + new Vector3(0, s * 0.35f, -s * 0.5f), 0f, 2f);
            }
            for (int i = 0; i < 3; i++)
            {
                var p = Spot(rng, 5f, 13f);
                DeadTree(root, p, R(rng, 2f, 3.4f), Mats.Hex("2A2622"), rng);
            }
        }

        static void DeadTree(Transform root, Vector3 p, float height, Color c, System.Random rng)
        {
            var trunk = new List<Vector3>();
            for (int i = 0; i <= 8; i++) trunk.Add(p + new Vector3(Mathf.Sin(i * 0.7f) * 0.12f, i / 8f * height, 0));
            Prop(root, MeshFactory.Tube(trunk, 0.16f, 7), c, Vector3.zero, Vector3.one, 0f);
            for (int b = 0; b < 4; b++)
            {
                var start = trunk[rng.Next(4, 8)];
                float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                var branch = new List<Vector3>();
                for (int i = 0; i <= 5; i++)
                {
                    float t = i / 5f;
                    branch.Add(start + new Vector3(side * t * R(rng, 0.6f, 0.9f), t * 0.6f + Mathf.Sin(t * 3f) * 0.1f, R(rng, -0.1f, 0.1f)));
                }
                Prop(root, MeshFactory.Tube(branch, 0.06f, 5), c, Vector3.zero, Vector3.one, 0f);
            }
        }

        static void FarmProps(Transform root, WorldTheme th, System.Random rng)
        {
            Tractor(root, new Vector3(-4.2f, 0, 2.6f), 35f);
            Tractor(root, new Vector3(4.6f, 0, 4.2f), -150f);
            for (int i = 0; i < 5; i++)
            {
                var p = Spot(rng, 3.5f, 12f);
                var bale = Box(root, new Vector3(1.2f, 0.5f, 0.75f), 0.12f, th.Palette[0], p + Vector3.up * 0.25f, 0.05f, 0f, new Vector3(0, R(rng, 0, 360), 0));
                Mats.SetPattern(bale, Pattern.Straw, Color.Lerp(th.Palette[0], Color.white, 0.5f), 1f);
            }
            var wood = Mats.Hex("8A5A34");
            for (int i = 0; i < 9; i++)
            {
                var p = new Vector3(-8f + i * 2f, 0, 9f);
                Box(root, new Vector3(0.14f, 0.9f, 0.14f), 0.03f, wood, p + Vector3.up * 0.45f, 0.05f);
                if (i < 8) Box(root, new Vector3(2f, 0.1f, 0.06f), 0.02f, wood, p + new Vector3(1f, 0.6f, 0), 0.05f);
                if (i < 8) Box(root, new Vector3(2f, 0.1f, 0.06f), 0.02f, wood, p + new Vector3(1f, 0.3f, 0), 0.05f);
            }
        }

        static void Tractor(Transform root, Vector3 p, float yaw)
        {
            var t = new GameObject("Tractor").transform;
            t.SetParent(root, false);
            t.localPosition = p;
            t.localRotation = Quaternion.Euler(0, yaw, 0);
            var red = Mats.Hex("E0353A");
            var tire = Mats.Hex("262226");
            var rim = Mats.Hex("FFD23A");
            Box(t, new Vector3(1.5f, 0.55f, 0.8f), 0.12f, red, new Vector3(0.25f, 0.7f, 0), 0.5f);
            Box(t, new Vector3(0.75f, 0.75f, 0.8f), 0.1f, red, new Vector3(-0.45f, 1.05f, 0), 0.5f);
            Box(t, new Vector3(0.6f, 0.4f, 0.82f), 0.06f, Mats.Hex("9ED8FF"), new Vector3(-0.45f, 1.18f, 0), 0.95f, 0.1f);
            Box(t, new Vector3(0.9f, 0.08f, 0.95f), 0.03f, Mats.Hex("B02A2E"), new Vector3(-0.45f, 1.46f, 0), 0.3f);
            Prop(t, MeshFactory.Cylinder, Mats.Hex("3A3A40"), new Vector3(0.6f, 1.15f, 0.2f), new Vector3(0.08f, 0.22f, 0.08f), 0.8f);
            for (int s = -1; s <= 1; s += 2)
            {
                Prop(t, MeshFactory.Cylinder, tire, new Vector3(-0.45f, 0.5f, s * 0.48f), new Vector3(1f, 0.12f, 1f), 0.1f, 0f, new Vector3(90, 0, 0));
                Prop(t, MeshFactory.Cylinder, rim, new Vector3(-0.45f, 0.5f, s * 0.55f), new Vector3(0.5f, 0.05f, 0.5f), 0.4f, 0f, new Vector3(90, 0, 0));
                Prop(t, MeshFactory.Cylinder, tire, new Vector3(0.75f, 0.3f, s * 0.42f), new Vector3(0.6f, 0.1f, 0.6f), 0.1f, 0f, new Vector3(90, 0, 0));
                Prop(t, MeshFactory.Cylinder, rim, new Vector3(0.75f, 0.3f, s * 0.47f), new Vector3(0.3f, 0.04f, 0.3f), 0.4f, 0f, new Vector3(90, 0, 0));
            }
        }

        static void KitchenProps(Transform root, WorldTheme th, System.Random rng)
        {
            var steel = Mats.Hex("C9D0DA");
            var colors = new[] { Mats.Hex("E8473A"), Mats.Hex("3A8FE8"), Mats.Hex("F2B33A"), steel };
            for (int i = 0; i < 6; i++)
            {
                var p = Spot(rng, 3f, 11f);
                float s = R(rng, 0.6f, 1.1f);
                var c = colors[rng.Next(colors.Length)];
                Prop(root, MeshFactory.Cylinder, c, p + Vector3.up * 0.3f * s, new Vector3(s, 0.3f * s, s), 0.7f);
                Prop(root, MeshFactory.Cylinder, Color.Lerp(c, Color.white, 0.25f), p + Vector3.up * 0.62f * s, new Vector3(s * 1.04f, 0.03f, s * 1.04f), 0.7f);
                Prop(root, MeshFactory.Sphere, steel, p + Vector3.up * 0.68f * s, new Vector3(0.12f, 0.08f, 0.12f) * s, 0.9f);
                for (int k = -1; k <= 1; k += 2)
                    Box(root, new Vector3(0.24f * s, 0.06f, 0.1f), 0.02f, steel, p + new Vector3(k * 0.58f * s, 0.5f * s, 0), 0.9f);
            }
            for (int i = 0; i < 5; i++)
            {
                var p = Spot(rng, 3f, 10f);
                float h = R(rng, 0.4f, 0.8f);
                Prop(root, MeshFactory.Cylinder, Mats.Hex("BFE6F0"), p + Vector3.up * h * 0.5f, new Vector3(0.32f, h * 0.5f, 0.32f), 0.95f, 0.05f);
                Prop(root, MeshFactory.Cylinder, colors[rng.Next(3)], p + Vector3.up * h, new Vector3(0.34f, 0.04f, 0.34f), 0.5f);
            }
        }

        static void CourtyardProps(Transform root, WorldTheme th, System.Random rng)
        {
            var stone = Mats.Hex("D8C7B0");
            for (int i = 0; i < 4; i++)
            {
                var p = new Vector3(i < 2 ? -3.2f : 3.2f, 0, i % 2 == 0 ? 2.5f : 7f);
                var pillar = Prop(root, MeshFactory.Cylinder, stone, p + Vector3.up * 1.1f, new Vector3(0.55f, 1.1f, 0.55f), 0.1f);
                Mats.SetPattern(pillar, Pattern.Bricks, Color.Lerp(stone, Color.black, 0.3f), 1.5f);
                Prop(root, MeshFactory.Cylinder, Mats.Hex("4A3A30"), p + Vector3.up * 2.3f, new Vector3(0.3f, 0.12f, 0.3f), 0.2f);
                Prop(root, MeshFactory.Sphere, Mats.Hex("FFB23A"), p + Vector3.up * 2.5f, new Vector3(0.22f, 0.3f, 0.22f), 0f, 3f);
                Box(root, new Vector3(0.05f, 0.9f, 0.5f), 0.02f, i % 2 == 0 ? Mats.Hex("D9443A") : Mats.Hex("3A6FD9"), p + new Vector3(0, 1.4f, -0.4f), 0.2f, 0f, new Vector3(0, 90, 0));
            }
            for (int i = 0; i < 6; i++)
            {
                var p = Spot(rng, 4f, 12f);
                float s = R(rng, 0.5f, 0.9f);
                Prop(root, MeshFactory.Sphere, Mats.Hex("6BA84A"), p + Vector3.up * s * 0.45f, Vector3.one * s, 0.2f);
            }
        }

        static void NeonProps(Transform root, WorldTheme th, System.Random rng)
        {
            var grid = new GameObject("Grid");
            grid.transform.SetParent(root, false);
            grid.transform.localPosition = new Vector3(0, 0.01f, 0);
            Mats.AddRenderer(grid, MeshFactory.Grid(40f, -10f, 40f, 2f, 0.03f),
                Mats.MakeUnlit(UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.One, false, 2500, 1f, null, th.Palette[0] * 1.3f));
            for (int i = 0; i < 8; i++)
            {
                var p = Spot(rng, 3.5f, 13f);
                float h = R(rng, 1f, 3.5f);
                Box(root, new Vector3(0.5f, h, 0.5f), 0.06f, th.GroundAlt, p + Vector3.up * h * 0.5f, 0.7f);
                for (int k = 0; k < 3; k++)
                    Box(root, new Vector3(0.52f, 0.05f, 0.52f), 0.02f, k % 2 == 0 ? th.Trim : th.Palette[0], p + Vector3.up * h * (0.3f + k * 0.25f), 0.2f, 2.2f);
            }
            for (int i = 0; i < 6; i++)
            {
                var p = Spot(rng, 2.5f, 9f);
                float s = R(rng, 0.25f, 0.45f);
                Box(root, Vector3.one * s, 0.05f, th.Palette[rng.Next(th.Palette.Length)], p + Vector3.up * (s * 0.5f + 0.4f), 0.5f, 1.2f, new Vector3(R(rng, 0, 45), R(rng, 0, 90), 0))
                    .gameObject.AddComponent<Drifter>().Setup(40f, 1.2f, R(rng, 0f, 6f), 0f);
            }
        }
    }
}
