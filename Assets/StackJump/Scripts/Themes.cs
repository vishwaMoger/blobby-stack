using UnityEngine;

namespace StackJump
{
    public enum BackdropKind { Planets, Clouds, Castle, Mountains, Kitchen, Grid }
    public enum WorldKind { Cosmos, Beanstalk, Haunted, Farm, Kitchen, Castle, Neon }

    // Everything that defines how one world looks. Worlds change as the tower climbs.
    public class WorldTheme
    {
        public string Name;
        public WorldKind Kind;
        public Color Trim = Color.black;   // secondary block color: twine, mortar, glow, chrome...
        public Color SkyAmbient = Color.white;
        public Color SkyTop, SkyMid, SkyBottom;
        public float MidPoint = 0.5f;
        public float Stars, Nebula;
        public Color NebulaColor = Color.magenta;
        public Color SunColor = Color.white;
        public Vector2 SunPos = new(0.5f, 0.8f);
        public float SunSize, SunGlow = 4f, SunStripes;
        public float Clouds;
        public Color CloudColor = Color.white;
        public Color Fog, Shade, Rim, Light = Color.white;
        public Color Ground, GroundAlt;
        public Color[] Palette;
        public BackdropKind Backdrop;
        public Color Near, Far, Accent;
        public int Seed;

        public static WorldTheme Lerp(WorldTheme a, WorldTheme b, float t)
        {
            return new WorldTheme
            {
                Name = b.Name,
                Kind = b.Kind,
                Trim = b.Trim,
                SkyAmbient = Color.Lerp(a.SkyAmbient, b.SkyAmbient, t),
                SkyTop = Color.Lerp(a.SkyTop, b.SkyTop, t),
                SkyMid = Color.Lerp(a.SkyMid, b.SkyMid, t),
                SkyBottom = Color.Lerp(a.SkyBottom, b.SkyBottom, t),
                MidPoint = Mathf.Lerp(a.MidPoint, b.MidPoint, t),
                Stars = Mathf.Lerp(a.Stars, b.Stars, t),
                Nebula = Mathf.Lerp(a.Nebula, b.Nebula, t),
                NebulaColor = Color.Lerp(a.NebulaColor, b.NebulaColor, t),
                SunColor = Color.Lerp(a.SunColor, b.SunColor, t),
                SunPos = Vector2.Lerp(a.SunPos, b.SunPos, t),
                SunSize = Mathf.Lerp(a.SunSize, b.SunSize, t),
                SunGlow = Mathf.Lerp(a.SunGlow, b.SunGlow, t),
                SunStripes = Mathf.Lerp(a.SunStripes, b.SunStripes, t),
                Clouds = Mathf.Lerp(a.Clouds, b.Clouds, t),
                CloudColor = Color.Lerp(a.CloudColor, b.CloudColor, t),
                Fog = Color.Lerp(a.Fog, b.Fog, t),
                Shade = Color.Lerp(a.Shade, b.Shade, t),
                Rim = Color.Lerp(a.Rim, b.Rim, t),
                Light = Color.Lerp(a.Light, b.Light, t),
                Ground = b.Ground,
                GroundAlt = b.GroundAlt,
                Palette = b.Palette,
                Backdrop = b.Backdrop,
                Near = b.Near, Far = b.Far, Accent = b.Accent, Seed = b.Seed,
            };
        }

        public Color BlockColor(int index)
        {
            float t = index / 5f;
            int k = Mathf.FloorToInt(t);
            return Color.Lerp(Palette[k % Palette.Length], Palette[(k + 1) % Palette.Length], t - k);
        }
    }

    public static class Themes
    {
        static Color H(string hex) => Mats.Hex(hex);

        public static readonly WorldTheme[] All =
        {
            new WorldTheme
            {
                Name = "COSMOS", Kind = WorldKind.Cosmos, Seed = 11,
                SkyTop = H("070418"), SkyMid = H("1E0D46"), SkyBottom = H("3A1B6B"), MidPoint = 0.45f,
                Stars = 1f, Nebula = 0.85f, NebulaColor = H("7A3CFF"),
                Fog = H("2A1452"), Shade = H("3A2178"), Rim = H("A07BFF"), Light = H("FFF1FF"), SkyAmbient = H("8A6BE0"),
                Ground = H("4A3384"), GroundAlt = H("33235E"),
                Palette = new[] { H("B45CFF"), H("D77CFF"), H("9A4BEA"), H("C86BFF") }, Trim = H("3FF0E0"),
                Backdrop = BackdropKind.Planets, Near = H("2B1655"), Far = H("6E3FD0"), Accent = H("3FE0D0"),
            },
            new WorldTheme
            {
                Name = "BEANSTALK", Kind = WorldKind.Beanstalk, Seed = 23,
                SkyTop = H("2E8FEA"), SkyMid = H("7CC4F7"), SkyBottom = H("DDF3FF"), MidPoint = 0.5f,
                SunColor = H("FFFBE0"), SunPos = new Vector2(0.82f, 0.86f), SunSize = 0.055f, SunGlow = 6f,
                Clouds = 1f, CloudColor = H("FFFFFF"),
                Fog = H("CFEAFF"), Shade = H("5E8FC2"), Rim = H("FFFFFF"), Light = H("FFFFF0"), SkyAmbient = H("CDEBFF"),
                Ground = H("6CC24A"), GroundAlt = H("4E9E35"),
                Palette = new[] { H("7ED957"), H("5CC448"), H("9BE86A"), H("4FB33F") }, Trim = H("3E8F2E"),
                Backdrop = BackdropKind.Clouds, Near = H("FFFFFF"), Far = H("E3F3FF"), Accent = H("4FBF45"),
            },
            new WorldTheme
            {
                Name = "HAUNTED HILL", Kind = WorldKind.Haunted, Seed = 37,
                SkyTop = H("06140F"), SkyMid = H("173A29"), SkyBottom = H("2C5A3E"), MidPoint = 0.5f,
                Stars = 0.35f,
                SunColor = H("FF5A1E"), SunPos = new Vector2(0.64f, 0.6f), SunSize = 0.15f, SunGlow = 3.5f,
                Fog = H("21412E"), Shade = H("23363A"), Rim = H("C2FF8A"), Light = H("F0FFE6"), SkyAmbient = H("6E9C7A"),
                Ground = H("2E4A33"), GroundAlt = H("1F3424"),
                Palette = new[] { H("E6F59A"), H("CFE57A"), H("F2F7B5"), H("BBD86A") }, Trim = H("6B5A3A"),
                Backdrop = BackdropKind.Castle, Near = H("0C1D15"), Far = H("10281C"), Accent = H("FFB347"),
            },
            new WorldTheme
            {
                Name = "SUNSET FARM", Kind = WorldKind.Farm, Seed = 41,
                SkyTop = H("FF2FA0"), SkyMid = H("FF7AB6"), SkyBottom = H("FFD1A8"), MidPoint = 0.55f,
                Clouds = 0.35f, CloudColor = H("FFE3D6"),
                Fog = H("FFB3A6"), Shade = H("B85A86"), Rim = H("FFF0D0"), Light = H("FFF4E6"), SkyAmbient = H("FFB0C8"),
                Ground = H("E8B65A"), GroundAlt = H("9CBF4E"),
                Palette = new[] { H("F7D25C"), H("EFC24A"), H("FADF7A"), H("E8B23E") }, Trim = H("8A4B2A"),
                Backdrop = BackdropKind.Mountains, Near = H("7E1F5A"), Far = H("B83C7E"), Accent = H("E0464E"),
            },
            new WorldTheme
            {
                Name = "HOT KITCHEN", Kind = WorldKind.Kitchen, Seed = 53,
                SkyTop = H("5A1A12"), SkyMid = H("9E3420"), SkyBottom = H("E0743A"), MidPoint = 0.5f,
                Fog = H("B8532E"), Shade = H("6A2622"), Rim = H("FFD08A"), Light = H("FFF0DC"), SkyAmbient = H("FFB37A"),
                Ground = H("E8DCC8"), GroundAlt = H("B9A88E"),
                Palette = new[] { H("F2462F"), H("FF7A2E"), H("FFB23A"), H("E8302A") }, Trim = H("D9DEE6"),
                Backdrop = BackdropKind.Kitchen, Near = H("7A2A1C"), Far = H("A3402A"), Accent = H("FFB23A"),
            },
            new WorldTheme
            {
                Name = "GOLDEN CASTLE", Kind = WorldKind.Castle, Seed = 67,
                SkyTop = H("F29A78"), SkyMid = H("F7C79A"), SkyBottom = H("FCE6B8"), MidPoint = 0.5f,
                SunColor = H("FFF6DA"), SunPos = new Vector2(0.76f, 0.9f), SunSize = 0.05f, SunGlow = 4f,
                Clouds = 0.4f, CloudColor = H("FFF2DE"),
                Fog = H("F7D0A6"), Shade = H("B97A78"), Rim = H("FFFFFF"), Light = H("FFF8EC"), SkyAmbient = H("FFD9B8"),
                Ground = H("C9B9A6"), GroundAlt = H("8F7F70"),
                Palette = new[] { H("F2A48C"), H("E8846E"), H("F5BB98"), H("DA6E62") }, Trim = H("F3E3CF"),
                Backdrop = BackdropKind.Castle, Near = H("D2876A"), Far = H("A8584F"), Accent = H("FFD27A"),
            },
            new WorldTheme
            {
                Name = "NEON CITY", Kind = WorldKind.Neon, Seed = 79,
                SkyTop = H("0E0428"), SkyMid = H("3A0B6E"), SkyBottom = H("B024D8"), MidPoint = 0.45f,
                Stars = 0.5f,
                SunColor = H("FF4FD8"), SunPos = new Vector2(0.5f, 0.42f), SunSize = 0.17f, SunGlow = 3f, SunStripes = 1f,
                Fog = H("6A1AA0"), Shade = H("2E1060"), Rim = H("3EF2FF"), Light = H("FFE6FF"), SkyAmbient = H("C070FF"),
                Ground = H("1C0838"), GroundAlt = H("2A0B4F"),
                Palette = new[] { H("FF4FD8"), H("D23AF0"), H("FF7AE6"), H("B02CE0") }, Trim = H("3EF2FF"),
                Backdrop = BackdropKind.Grid, Near = H("1B0638"), Far = H("2A0B4F"), Accent = H("3EF2FF"),
            },
        };
    }
}
