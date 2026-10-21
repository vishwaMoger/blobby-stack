using System.Collections.Generic;
using UnityEngine;

namespace StackJump
{
    public enum Accessory { Tuft, Chick, Pig, Frank, Knight, Leafy, Robot }

    public class CharacterDef
    {
        public readonly string Name;
        public readonly Color Body, Feet;
        public readonly Accessory Kind;
        public readonly int Unlock; // best score needed

        public CharacterDef(string name, Color body, Color feet, Accessory kind, int unlock)
        {
            Name = name; Body = body; Feet = feet; Kind = kind; Unlock = unlock;
        }

        public bool IsUnlocked(int best) => best >= Unlock;
    }

    public static class Characters
    {
        static Color H(string hex) => Mats.Hex(hex);

        public static readonly CharacterDef[] All =
        {
            new("BLOBBY", H("F2557A"), H("C93A5E"), Accessory.Tuft, 0),
            new("CHIRP", H("FFD23F"), H("F29A1F"), Accessory.Chick, 10),
            new("OINKY", H("F7A1B5"), H("E07A93"), Accessory.Pig, 20),
            new("FRANKIE", H("8FB26B"), H("4E6B3A"), Accessory.Frank, 30),
            new("SIR WOBBLE", H("FFC93C"), H("D9A21F"), Accessory.Knight, 45),
            new("BOK", H("EEF7DD"), H("9CCB6A"), Accessory.Leafy, 60),
            new("ROBO", H("B9C3D6"), H("6B7488"), Accessory.Robot, 80),
        };
    }

    // Transforms the Player animates.
    public class CharacterRig
    {
        public Transform Root, Squash;
        public readonly List<Transform> Eyes = new();
        public readonly List<Transform> Pupils = new();
        public readonly List<Vector3> PupilHome = new();
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
                Pupils[i].localPosition = PupilHome[i] + new Vector3(x * 0.045f, 0, 0);
        }

        public void Blink(float open)
        {
            foreach (var e in Eyes) e.localScale = new Vector3(1, Mathf.Max(0.08f, open), 1);
        }
    }

    // Builds cute round characters out of spheres, cones and rounded boxes.
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
            float gloss = silhouette ? 0f : 0.35f;
            bool robot = d.Kind == Accessory.Robot;

            if (robot)
                Part(s, MeshFactory.RoundedBox(new Vector3(0.8f, 0.74f, 0.72f), 0.2f), body, new Vector3(0, 0.39f, 0), Vector3.one, 0.55f);
            else if (d.Kind == Accessory.Leafy)
                Part(s, MeshFactory.Sphere, body, new Vector3(0, 0.4f, 0), new Vector3(0.76f, 0.8f, 0.74f), gloss);
            else
                Part(s, MeshFactory.Sphere, body, new Vector3(0, 0.38f, 0), new Vector3(0.84f, 0.76f, 0.8f), gloss);

            for (int side = -1; side <= 1; side += 2)
                Part(s, MeshFactory.Sphere, feet, new Vector3(side * 0.18f, 0.045f, -0.08f), new Vector3(0.22f, 0.11f, 0.28f), gloss * 0.5f);

            if (!robot && !silhouette)
            {
                var blush = Color.Lerp(d.Body, new Color(1f, 0.35f, 0.5f), 0.55f);
                for (int side = -1; side <= 1; side += 2)
                    Part(s, MeshFactory.Sphere, blush, new Vector3(side * 0.27f, 0.35f, -0.29f), new Vector3(0.11f, 0.055f, 0.05f), 0f);
            }

            BuildEyes(rig, robot, silhouette);
            BuildAccessory(s, d, silhouette, shadow);
            rig.SetDead(false);
            return rig;
        }

        static void BuildEyes(CharacterRig rig, bool robot, bool silhouette)
        {
            var s = rig.Squash;
            if (robot)
                Part(s, MeshFactory.RoundedBox(new Vector3(0.62f, 0.3f, 0.08f), 0.06f), Dark, new Vector3(0, 0.45f, -0.33f), Vector3.one, 0.9f);

            for (int side = -1; side <= 1; side += 2)
            {
                var pivot = new GameObject("Eye").transform;
                pivot.SetParent(s, false);
                pivot.localPosition = new Vector3(side * 0.15f, robot ? 0.45f : 0.48f, robot ? -0.36f : -0.31f);
                rig.Eyes.Add(pivot);
                rig.AliveOnly.Add(pivot.gameObject);

                Transform pupil;
                if (robot)
                {
                    pupil = Part(pivot, MeshFactory.Sphere, Mats.Hex("3EF2FF"), Vector3.zero, new Vector3(0.14f, 0.14f, 0.05f), 0f, 2.5f);
                }
                else
                {
                    var white = silhouette ? new Color(1f, 1f, 1f) * 0.9f : Mats.Hex("F7F8FF");
                    Part(pivot, MeshFactory.Sphere, white, Vector3.zero, new Vector3(0.27f, 0.31f, 0.2f), 0.6f);
                    pupil = Part(pivot, MeshFactory.Sphere, Mats.Hex("1B1433"), new Vector3(0, -0.01f, -0.075f), new Vector3(0.15f, 0.17f, 0.1f), 0.9f);
                    Part(pupil, MeshFactory.Sphere, Color.white, new Vector3(0.22f, 0.25f, -0.45f), new Vector3(0.36f, 0.33f, 0.3f), 0f, 1.2f);
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
                    break;

                case Accessory.Chick:
                    Part(s, MeshFactory.Cone(0.08f, 0.17f), C(Mats.Hex("F28A1F")), new Vector3(0, 0.36f, -0.36f), Vector3.one, g, 0, new Vector3(-90, 0, 0));
                    for (int side = -1; side <= 1; side += 2)
                        Part(s, MeshFactory.Sphere, C(d.Feet), new Vector3(side * 0.42f, 0.34f, 0), new Vector3(0.14f, 0.26f, 0.22f), g, 0, new Vector3(0, 0, side * 28));
                    Part(s, MeshFactory.Cone(0.06f, 0.16f), C(d.Feet), new Vector3(0, 0.72f, 0), Vector3.one, g, 0, new Vector3(0, 0, 10));
                    break;

                case Accessory.Pig:
                    Part(s, MeshFactory.Cylinder, C(Mats.Hex("E07A93")), new Vector3(0, 0.31f, -0.37f), new Vector3(0.22f, 0.04f, 0.16f), g, 0, new Vector3(90, 0, 0));
                    if (!silhouette)
                        for (int side = -1; side <= 1; side += 2)
                            Part(s, MeshFactory.Sphere, Mats.Hex("8A3A52"), new Vector3(side * 0.045f, 0.31f, -0.415f), new Vector3(0.04f, 0.055f, 0.03f), 0f);
                    for (int side = -1; side <= 1; side += 2)
                        Part(s, MeshFactory.Cone(0.09f, 0.17f), C(d.Feet), new Vector3(side * 0.22f, 0.66f, -0.02f), Vector3.one, g, 0, new Vector3(0, 0, -side * 28));
                    break;

                case Accessory.Frank:
                    Part(s, MeshFactory.RoundedBox(new Vector3(0.62f, 0.16f, 0.56f), 0.05f), C(Mats.Hex("2B3A22")), new Vector3(0, 0.69f, 0), Vector3.one, g * 0.5f);
                    for (int side = -1; side <= 1; side += 2)
                        Part(s, MeshFactory.Cylinder, C(Mats.Hex("9AA0A8")), new Vector3(side * 0.43f, 0.36f, 0), new Vector3(0.08f, 0.07f, 0.08f), 0.8f, 0, new Vector3(0, 0, 90));
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
                        var leaf = Part(s, MeshFactory.Sphere, leafColor, new Vector3(0, 0.78f, 0), new Vector3(0.22f, 0.44f, 0.07f), g, 0, new Vector3(0, a, 0));
                        leaf.localPosition += leaf.forward * 0.1f;
                        leaf.localRotation = Quaternion.Euler(0, a, 0) * Quaternion.Euler(-22, 0, 0);
                    }
                    break;

                case Accessory.Robot:
                    Part(s, MeshFactory.Cylinder, C(d.Feet), new Vector3(0, 0.86f, 0), new Vector3(0.035f, 0.1f, 0.035f), 0.6f);
                    Part(s, MeshFactory.Sphere, C(Mats.Hex("FF4F6A")), new Vector3(0, 0.99f, 0), Vector3.one * 0.11f, 0.4f, silhouette ? 0f : 2f);
                    for (int side = -1; side <= 1; side += 2)
                        Part(s, MeshFactory.Cylinder, C(d.Feet), new Vector3(side * 0.42f, 0.4f, 0), new Vector3(0.12f, 0.04f, 0.12f), 0.6f, 0, new Vector3(0, 0, 90));
                    break;
            }
        }

        static Transform Part(Transform parent, Mesh mesh, Color color, Vector3 pos, Vector3 scale, float gloss, float emission = 0f, Vector3? euler = null)
        {
            var go = new GameObject("Part");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            var r = Mats.AddRenderer(go, mesh, Mats.Toon);
            Mats.Set(r, color, gloss, emission, 0f, 0f, 0.05f);
            return go.transform;
        }
    }
}
