using System.Collections.Generic;
using UnityEngine;

namespace StackJump
{
    public enum Accessory { Tuft, Chick, Pig, Frank, Knight, Leafy, Robot }

    public class CharacterDef
    {
        public readonly string Name;
        public readonly Color Body, Feet, Iris;
        public readonly Accessory Kind;
        public readonly int Unlock; // best score needed

        public CharacterDef(string name, Color body, Color feet, Color iris, Accessory kind, int unlock)
        {
            Name = name; Body = body; Feet = feet; Iris = iris; Kind = kind; Unlock = unlock;
        }

        public bool IsUnlocked(int best) => best >= Unlock;
    }

    public static class Characters
    {
        static Color H(string hex) => Mats.Hex(hex);

        public static readonly CharacterDef[] All =
        {
            new("BLOBBY", H("F2557A"), H("C93A5E"), H("4A2A1E"), Accessory.Tuft, 0),
            new("CHIRP", H("FFD23F"), H("F29A1F"), H("2A3A6E"), Accessory.Chick, 10),
            new("OINKY", H("F7A1B5"), H("E07A93"), H("3A2A50"), Accessory.Pig, 20),
            new("FRANKIE", H("8FB26B"), H("4E6B3A"), H("5A2A2A"), Accessory.Frank, 30),
            new("SIR WOBBLE", H("FFC93C"), H("D9A21F"), H("2E4A7A"), Accessory.Knight, 45),
            new("BOK", H("EEF7DD"), H("9CCB6A"), H("2F5A2A"), Accessory.Leafy, 60),
            new("ROBO", H("B9C3D6"), H("6B7488"), H("3EF2FF"), Accessory.Robot, 80),
        };
    }

    // Transforms the Player animates.
    public class CharacterRig
    {
        public Transform Root, Squash;
        public readonly List<Transform> Eyes = new();
        public readonly List<Transform> Pupils = new();
        public readonly List<Vector3> PupilHome = new();
        public readonly List<Transform> Arms = new();
        public readonly List<GameObject> AliveOnly = new();
        public readonly List<GameObject> DeadOnly = new();

        public void SetDead(bool dead)
        {
            foreach (var g in AliveOnly) g.SetActive(!dead);
            foreach (var g in DeadOnly) g.SetActive(dead);
        }

        public void Look(float x)
        {
            for (int i = 0; i < Pupils.Count; i++)
                Pupils[i].localPosition = PupilHome[i] + new Vector3(x * 0.04f, 0, 0);
        }

        public void Blink(float open)
        {
            foreach (var e in Eyes) e.localScale = new Vector3(1, Mathf.Max(0.08f, open), 1);
        }

        // 0 = arms down, 1 = arms up.
        public void ArmsUp(float t)
        {
            for (int i = 0; i < Arms.Count; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                Arms[i].localRotation = Quaternion.Euler(0, 0, side * Mathf.Lerp(-20f, -115f, t));
            }
        }
    }

    // Builds cute round characters out of spheres, cones, tubes and rounded boxes.
    public static class CharacterFactory
    {
        static readonly Color Dark = new(0.11f, 0.08f, 0.2f);

        public static CharacterRig Build(CharacterDef d, Transform parent, bool silhouette)
        {
            var rig = new CharacterRig();
            rig.Root = new GameObject(d.Name).transform;
            rig.Root.SetParent(parent, false);
            rig.Squash = new GameObject("Squash").transform;
            rig.Squash.SetParent(rig.Root, false);
            var s = rig.Squash;

            Color shadow = new(0.16f, 0.13f, 0.26f);
            Color body = silhouette ? shadow : d.Body;
            Color feet = silhouette ? shadow * 0.85f : d.Feet;
            float gloss = silhouette ? 0f : 0.4f;
            bool robot = d.Kind == Accessory.Robot;

            if (robot)
                Part(s, MeshFactory.RoundedBox(new Vector3(0.8f, 0.74f, 0.72f), 0.2f), body, new Vector3(0, 0.39f, 0), Vector3.one, 0.6f, 0f, null, 0.2f);
            else if (d.Kind == Accessory.Leafy)
                Part(s, MeshFactory.Sphere, body, new Vector3(0, 0.4f, 0), new Vector3(0.76f, 0.8f, 0.74f), gloss, 0f, null, 0.25f);
            else
                Part(s, MeshFactory.Sphere, body, new Vector3(0, 0.38f, 0), new Vector3(0.84f, 0.76f, 0.8f), gloss, 0f, null, 0.25f);

            // Belly highlight for a softer, rounder look.
            if (!robot && !silhouette)
                Part(s, MeshFactory.Sphere, Color.Lerp(d.Body, Color.white, 0.28f), new Vector3(0, 0.3f, -0.12f), new Vector3(0.56f, 0.5f, 0.6f), gloss);

            for (int side = -1; side <= 1; side += 2)
                Part(s, MeshFactory.Sphere, feet, new Vector3(side * 0.18f, 0.045f, -0.08f), new Vector3(0.24f, 0.12f, 0.3f), gloss * 0.5f);

            BuildArms(rig, d, feet, gloss, robot);

            if (!robot && !silhouette)
            {
                var blush = Color.Lerp(d.Body, new Color(1f, 0.35f, 0.5f), 0.6f);
                for (int side = -1; side <= 1; side += 2)
                    Part(s, MeshFactory.Sphere, blush, new Vector3(side * 0.25f, 0.37f, -0.32f), new Vector3(0.12f, 0.06f, 0.05f), 0f);
                BuildMouth(rig, d);
            }

            BuildEyes(rig, d, robot, silhouette);
            BuildAccessory(s, d, silhouette, shadow);
            rig.ArmsUp(0f);
            rig.SetDead(false);
            return rig;
        }

        static void BuildArms(CharacterRig rig, CharacterDef d, Color color, float gloss, bool robot)
        {
            if (d.Kind == Accessory.Chick) color = Color.Lerp(d.Body, d.Feet, 0.5f);
            for (int side = -1; side <= 1; side += 2)
            {
                var pivot = new GameObject("Arm").transform;
                pivot.SetParent(rig.Squash, false);
                pivot.localPosition = new Vector3(side * 0.36f, 0.36f, -0.02f);
                rig.Arms.Add(pivot);
                if (robot)
                    Part(pivot, MeshFactory.RoundedBox(new Vector3(0.1f, 0.26f, 0.1f), 0.04f), color, new Vector3(0, -0.12f, 0), Vector3.one, 0.6f);
                else if (d.Kind == Accessory.Chick)
                    Part(pivot, MeshFactory.Sphere, color, new Vector3(0, -0.11f, 0), new Vector3(0.12f, 0.26f, 0.2f), gloss);
                else
                    Part(pivot, MeshFactory.Sphere, color, new Vector3(0, -0.1f, 0), new Vector3(0.13f, 0.22f, 0.13f), gloss);
            }
        }

        static void BuildMouth(CharacterRig rig, CharacterDef d)
        {
            if (d.Kind == Accessory.Chick || d.Kind == Accessory.Pig) return; // beak / snout instead
            var path = new List<Vector3>();
            for (int i = 0; i <= 10; i++)
            {
                float t = i / 10f * 2f - 1f;
                float x = t * 0.07f;
                path.Add(new Vector3(x, 0.315f + t * t * 0.03f, -0.43f + t * t * 0.012f));
            }
            var mouth = new GameObject("Mouth");
            mouth.transform.SetParent(rig.Squash, false);
            var r = Mats.AddRenderer(mouth, MeshFactory.Tube(path, 0.014f, 6, false), Mats.Toon);
            Mats.Set(r, Mats.Hex("4A1A2A"), 0.2f, 0f, 0f, 0f, 0f);
            rig.AliveOnly.Add(mouth);

            // An "o" mouth for the knocked-out pose.
            var o = Part(rig.Squash, MeshFactory.Sphere, Mats.Hex("4A1A2A"), new Vector3(0, 0.3f, -0.42f), new Vector3(0.09f, 0.08f, 0.04f), 0.3f);
            rig.DeadOnly.Add(o.gameObject);
        }

        static void BuildEyes(CharacterRig rig, CharacterDef d, bool robot, bool silhouette)
        {
            var s = rig.Squash;
            if (robot)
                Part(s, MeshFactory.RoundedBox(new Vector3(0.62f, 0.3f, 0.08f), 0.06f), Dark, new Vector3(0, 0.45f, -0.33f), Vector3.one, 0.95f);

            for (int side = -1; side <= 1; side += 2)
            {
                var pivot = new GameObject("Eye").transform;
                pivot.SetParent(s, false);
                pivot.localPosition = new Vector3(side * 0.14f, robot ? 0.45f : 0.5f, robot ? -0.36f : -0.3f);
                rig.Eyes.Add(pivot);
                rig.AliveOnly.Add(pivot.gameObject);

                Transform pupil;
                if (robot)
                {
                    pupil = Part(pivot, MeshFactory.Sphere, d.Iris, Vector3.zero, new Vector3(0.14f, 0.14f, 0.05f), 0f, 2.5f);
                }
                else
                {
                    var white = silhouette ? new Color(0.92f, 0.92f, 0.95f) : Mats.Hex("FBFBFF");
                    Part(pivot, MeshFactory.Sphere, white, Vector3.zero, new Vector3(0.28f, 0.33f, 0.2f), 0.7f);
                    pupil = new GameObject("Pupil").transform;
                    pupil.SetParent(pivot, false);
                    pupil.localPosition = new Vector3(0, -0.015f, -0.07f);
                    Part(pupil, MeshFactory.Sphere, silhouette ? Dark : d.Iris, Vector3.zero, new Vector3(0.17f, 0.2f, 0.09f), 0.9f);
                    Part(pupil, MeshFactory.Sphere, Mats.Hex("0E0A18"), new Vector3(0, -0.005f, -0.025f), new Vector3(0.1f, 0.12f, 0.06f), 0.95f);
                    Part(pupil, MeshFactory.Sphere, Color.white, new Vector3(0.035f, 0.045f, -0.05f), new Vector3(0.06f, 0.065f, 0.03f), 0f, 1.4f);
                    Part(pupil, MeshFactory.Sphere, Color.white, new Vector3(-0.03f, -0.04f, -0.05f), new Vector3(0.028f, 0.028f, 0.02f), 0f, 1.2f);
                }
                rig.Pupils.Add(pupil);
                rig.PupilHome.Add(pupil.localPosition);

                // X eyes for the knocked-out pose.
                var x = new GameObject("XEye").transform;
                x.SetParent(s, false);
                x.localPosition = pivot.localPosition + new Vector3(0, 0, -0.06f);
                var bar = MeshFactory.RoundedBox(new Vector3(0.2f, 0.05f, 0.05f), 0.02f, 2);
                Part(x, bar, Dark, Vector3.zero, Vector3.one, 0f, 0f, new Vector3(0, 0, 45));
                Part(x, bar, Dark, Vector3.zero, Vector3.one, 0f, 0f, new Vector3(0, 0, -45));
                rig.DeadOnly.Add(x.gameObject);
            }
        }

        static void BuildAccessory(Transform s, CharacterDef d, bool silhouette, Color shadow)
        {
            Color C(Color c) => silhouette ? shadow : c;
            float g = silhouette ? 0f : 0.35f;

            switch (d.Kind)
            {
                case Accessory.Tuft:
                    Part(s, MeshFactory.Cone(0.09f, 0.26f), C(d.Feet), new Vector3(0, 0.7f, 0), Vector3.one, g, 0, new Vector3(0, 0, 14));
                    Part(s, MeshFactory.Cone(0.07f, 0.2f), C(d.Feet), new Vector3(0.07f, 0.7f, 0.02f), Vector3.one, g, 0, new Vector3(0, 0, -28));
                    Part(s, MeshFactory.Cone(0.06f, 0.16f), C(d.Feet), new Vector3(-0.07f, 0.69f, 0.03f), Vector3.one, g, 0, new Vector3(0, 0, 32));
                    break;

                case Accessory.Chick:
                    Part(s, MeshFactory.Cone(0.08f, 0.17f), C(Mats.Hex("F28A1F")), new Vector3(0, 0.36f, -0.36f), Vector3.one, g, 0, new Vector3(-90, 0, 0));
                    Part(s, MeshFactory.Cone(0.06f, 0.16f), C(d.Feet), new Vector3(0, 0.72f, 0), Vector3.one, g, 0, new Vector3(0, 0, 10));
                    Part(s, MeshFactory.Cone(0.05f, 0.13f), C(d.Feet), new Vector3(0.05f, 0.71f, 0.02f), Vector3.one, g, 0, new Vector3(0, 0, -25));
                    break;

                case Accessory.Pig:
                    Part(s, MeshFactory.Cylinder, C(Mats.Hex("E07A93")), new Vector3(0, 0.32f, -0.37f), new Vector3(0.22f, 0.04f, 0.16f), g, 0, new Vector3(90, 0, 0));
                    if (!silhouette)
                        for (int side = -1; side <= 1; side += 2)
                            Part(s, MeshFactory.Sphere, Mats.Hex("8A3A52"), new Vector3(side * 0.045f, 0.32f, -0.415f), new Vector3(0.04f, 0.055f, 0.03f), 0f);
                    for (int side = -1; side <= 1; side += 2)
                        Part(s, MeshFactory.Cone(0.09f, 0.17f), C(d.Feet), new Vector3(side * 0.22f, 0.66f, -0.02f), Vector3.one, g, 0, new Vector3(0, 0, -side * 28));
                    break;

                case Accessory.Frank:
                    Part(s, MeshFactory.RoundedBox(new Vector3(0.64f, 0.16f, 0.58f), 0.05f), C(Mats.Hex("2B3A22")), new Vector3(0, 0.69f, 0), Vector3.one, g * 0.5f);
                    for (int side = -1; side <= 1; side += 2)
                        Part(s, MeshFactory.Cylinder, C(Mats.Hex("9AA0A8")), new Vector3(side * 0.43f, 0.42f, 0), new Vector3(0.08f, 0.07f, 0.08f), 0.8f, 0, new Vector3(0, 0, 90));
                    if (!silhouette)
                        for (int k = 0; k < 3; k++)
                            Part(s, MeshFactory.RoundedBox(new Vector3(0.015f, 0.07f, 0.015f), 0.006f, 1), Mats.Hex("2B3A22"), new Vector3(0.12f + k * 0.04f, 0.62f, -0.31f), Vector3.one, 0f);
                    break;

                case Accessory.Knight:
                    var steel = C(Mats.Hex("A9B0BE"));
                    Part(s, MeshFactory.Cylinder, steel, new Vector3(0, 0.66f, 0), new Vector3(0.82f, 0.07f, 0.78f), 0.85f);
                    Part(s, MeshFactory.Sphere, steel, new Vector3(0, 0.72f, 0), new Vector3(0.72f, 0.36f, 0.68f), 0.85f);
                    var plume = C(Mats.Hex("E8343C"));
                    Part(s, MeshFactory.Sphere, plume, new Vector3(0, 0.92f, 0.02f), Vector3.one * 0.14f, g);
                    Part(s, MeshFactory.Sphere, plume, new Vector3(0, 0.99f, 0.11f), Vector3.one * 0.13f, g);
                    Part(s, MeshFactory.Sphere, plume, new Vector3(0, 1.0f, 0.21f), Vector3.one * 0.11f, g);
                    break;

                case Accessory.Leafy:
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i / 5f * 360f + 18f;
                        var leafColor = C(i % 2 == 0 ? Mats.Hex("7CC24E") : Mats.Hex("5FAE3A"));
                        var leaf = Part(s, MeshFactory.Sphere, leafColor, new Vector3(0, 0.8f, 0), new Vector3(0.22f, 0.44f, 0.07f), g);
                        leaf.localRotation = Quaternion.Euler(0, a, 0) * Quaternion.Euler(-22, 0, 0);
                        leaf.localPosition += leaf.localRotation * new Vector3(0, 0.05f, 0.08f);
                    }
                    break;

                case Accessory.Robot:
                    Part(s, MeshFactory.Cylinder, C(d.Feet), new Vector3(0, 0.86f, 0), new Vector3(0.035f, 0.1f, 0.035f), 0.6f);
                    Part(s, MeshFactory.Sphere, C(Mats.Hex("FF4F6A")), new Vector3(0, 0.99f, 0), Vector3.one * 0.11f, 0.4f, silhouette ? 0f : 2f);
                    break;
            }
        }

        static Transform Part(Transform parent, Mesh mesh, Color color, Vector3 pos, Vector3 scale, float gloss, float emission = 0f, Vector3? euler = null, float grad = 0f)
        {
            var go = new GameObject("Part");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            var r = Mats.AddRenderer(go, mesh, Mats.Toon);
            Mats.Set(r, color, gloss, emission, grad, 0f, 0.05f);
            return go.transform;
        }
    }
}
