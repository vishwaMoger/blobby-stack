using UnityEngine;

namespace StackJump
{
    public enum BackdropKind { Planets, Clouds, Castle, Mountains, Hills, Grid }

    // Everything that defines how one world looks. Worlds change as the tower climbs.
    public class WorldTheme
    {
        public string Name;
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
        public Color Ground;
        public Color[] Palette;
        public int BlockLayers = 1;
        public BackdropKind Backdrop;
        public Color Near, Far, Accent;
        public int Seed;

        public static WorldTheme Lerp(WorldTheme a, WorldTheme b, float t)
        {
            return new WorldTheme
            {
                Name = b.Name,
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
                Ground = Color.Lerp(a.Ground, b.Ground, t),
                Palette = b.Palette,
                BlockLayers = b.BlockLayers,
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
                Name = "COSMOS", Seed = 11,
                SkyTop = H("070418"), SkyMid = H("1E0D46"), SkyBottom = H("3A1B6B"), MidPoint = 0.45f,
                Stars = 1f, Nebula = 0.85f, NebulaColor = H("7A3CFF"),
                Fog = H("2A1452"), Shade = H("4A2A8A"), Rim = H("A07BFF"), Light = H("FFF1FF"),
                Ground = H("3B2470"),
                Palette = new[] { H("B45CFF"), H("D77CFF"), H("9A4BEA"), H("C86BFF") }, BlockLayers = 2,
                Backdrop = BackdropKind.Planets, Near = H("2B1655"), Far = H("6E3FD0"), Accent = H("3FE0D0"),
            },
            new WorldTheme
            {
                Name = "BEANSTALK", Seed = 23,
                SkyTop = H("2E8FEA"), SkyMid = H("7CC4F7"), SkyBottom = H("DDF3FF"), MidPoint = 0.5f,
                SunColor = H("FFFBE0"), SunPos = new Vector2(0.82f, 0.86f), SunSize = 0.055f, SunGlow = 6f,
                Clouds = 1f, CloudColor = H("FFFFFF"),
                Fog = H("CFEAFF"), Shade = H("7FA9D6"), Rim = H("FFFFFF"), Light = H("FFFFF4"),
                Ground = H("6CC24A"),
                Palette = new[] { H("7ED957"), H("4FBF45"), H("9BE86A"), H("3FA63A") }, BlockLayers = 2,
                Backdrop = BackdropKind.Clouds, Near = H("FFFFFF"), Far = H("E3F3FF"), Accent = H("4FBF45"),
            },
            new WorldTheme
            {
                Name = "HAUNTED HILL", Seed = 37,
                SkyTop = H("06140F"), SkyMid = H("173A29"), SkyBottom = H("2C5A3E"), MidPoint = 0.5f,
                Stars = 0.35f,
                SunColor = H("FF5A1E"), SunPos = new Vector2(0.64f, 0.6f), SunSize = 0.15f, SunGlow = 3.5f,
                Fog = H("21412E"), Shade = H("2A3D3A"), Rim = H("C2FF8A"), Light = H("E8FFE0"),
                Ground = H("1E3A2A"),
                Palette = new[] { H("E6F59A"), H("CFE57A"), H("F2F7B5"), H("BBD86A") }, BlockLayers = 1,
                Backdrop = BackdropKind.Castle, Near = H("0C1D15"), Far = H("10281C"), Accent = H("FFB347"),
            },
            new WorldTheme
            {
                Name = "SUNSET FARM", Seed = 41,
                SkyTop = H("FF2FA0"), SkyMid = H("FF7AB6"), SkyBottom = H("FFD1A8"), MidPoint = 0.55f,
                Clouds = 0.35f, CloudColor = H("FFE3D6"),
                Fog = H("FFB3A6"), Shade = H("C96A8F"), Rim = H("FFF0D0"), Light = H("FFF4E6"),
                Ground = H("E9A15A"),
                Palette = new[] { H("F7D25C"), H("EFC24A"), H("FADF7A"), H("E8B23E") }, BlockLayers = 1,
                Backdrop = BackdropKind.Mountains, Near = H("7E1F5A"), Far = H("B83C7E"), Accent = H("E0464E"),
            },
            new WorldTheme
            {
                Name = "HOT KITCHEN", Seed = 53,
                SkyTop = H("7E1A0E"), SkyMid = H("C93D27"), SkyBottom = H("F08A3E"), MidPoint = 0.5f,
                Fog = H("D9603A"), Shade = H("7A2A2A"), Rim = H("FFD08A"), Light = H("FFF0DC"),
                Ground = H("7A2516"),
                Palette = new[] { H("F2462F"), H("FF7A2E"), H("FFB23A"), H("E8302A") }, BlockLayers = 1,
                Backdrop = BackdropKind.Hills, Near = H("8E2614"), Far = H("B9401F"), Accent = H("FFB23A"),
            },
            new WorldTheme
            {
                Name = "GOLDEN CASTLE", Seed = 67,
                SkyTop = H("F29A78"), SkyMid = H("F7C79A"), SkyBottom = H("FCE6B8"), MidPoint = 0.5f,
                SunColor = H("FFF6DA"), SunPos = new Vector2(0.76f, 0.9f), SunSize = 0.05f, SunGlow = 4f,
                Clouds = 0.4f, CloudColor = H("FFF2DE"),
                Fog = H("F7D0A6"), Shade = H("C98A80"), Rim = H("FFFFFF"), Light = H("FFF8EC"),
                Ground = H("E0A070"),
                Palette = new[] { H("F2A48C"), H("E8846E"), H("F5BB98"), H("DA6E62") }, BlockLayers = 1,
                Backdrop = BackdropKind.Castle, Near = H("D2876A"), Far = H("A8584F"), Accent = H("FFD27A"),
            },
            new WorldTheme
            {
                Name = "NEON CITY", Seed = 79,
                SkyTop = H("0E0428"), SkyMid = H("3A0B6E"), SkyBottom = H("B024D8"), MidPoint = 0.45f,
                Stars = 0.5f,
                SunColor = H("FF4FD8"), SunPos = new Vector2(0.5f, 0.42f), SunSize = 0.17f, SunGlow = 3f, SunStripes = 1f,
                Fog = H("6A1AA0"), Shade = H("3B1670"), Rim = H("3EF2FF"), Light = H("FFE6FF"),
                Ground = H("3A0F6E"),
                Palette = new[] { H("FF4FD8"), H("D23AF0"), H("FF7AE6"), H("B02CE0") }, BlockLayers = 1,
                Backdrop = BackdropKind.Grid, Near = H("1B0638"), Far = H("2A0B4F"), Accent = H("3EF2FF"),
            },
        };
    }
}
