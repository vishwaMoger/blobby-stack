using UnityEngine;
using UnityEngine.Rendering;

namespace StackJump
{
    // Materials live in Resources so their shaders are always included in the WebGL build.
    public static class Mats
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int GlossId = Shader.PropertyToID("_Gloss");
        static readonly int EmissionId = Shader.PropertyToID("_Emission");
        static readonly int GradId = Shader.PropertyToID("_Grad");
        static readonly int FlatId = Shader.PropertyToID("_Flat");
        static readonly int TopBoostId = Shader.PropertyToID("_TopBoost");

        static Material toon, unlit, sky, unlitOpaque, unlitAlpha, unlitAdd;
        static MaterialPropertyBlock block;

        public static Material Toon => toon != null ? toon : toon = Resources.Load<Material>("Materials/SJ_Toon");
        public static Material Unlit => unlit != null ? unlit : unlit = Resources.Load<Material>("Materials/SJ_Unlit");
        public static Material Sky => sky != null ? sky : sky = Resources.Load<Material>("Materials/SJ_Sky");

        // Shared variants of the unlit shader.
        public static Material UnlitOpaque => unlitOpaque != null ? unlitOpaque : unlitOpaque = MakeUnlit(BlendMode.One, BlendMode.Zero, true, 2000, 0f);
        public static Material UnlitAlpha => unlitAlpha != null ? unlitAlpha : unlitAlpha = MakeUnlit(BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha, false, 3000, 0f);
        public static Material UnlitAdditive => unlitAdd != null ? unlitAdd : unlitAdd = MakeUnlit(BlendMode.SrcAlpha, BlendMode.One, false, 3000, 0f);

        public static Material MakeUnlit(BlendMode src, BlendMode dst, bool zWrite, int queue, float fog, Texture texture = null, Color? color = null)
        {
            var m = new Material(Unlit);
            m.SetFloat("_SrcBlend", (float)src);
            m.SetFloat("_DstBlend", (float)dst);
            m.SetFloat("_ZWrite", zWrite ? 1f : 0f);
            m.SetFloat("_FogAmount", fog);
            if (texture != null) m.SetTexture("_MainTex", texture);
            m.SetColor(BaseColorId, color ?? Color.white);
            m.renderQueue = queue;
            return m;
        }

        // Per-renderer look for the toon material.
        public static void Set(Renderer r, Color color, float gloss = 0.25f, float emission = 0f, float grad = 0f, float flat = 0f, float topBoost = 0.12f)
        {
            block ??= new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetFloat(GlossId, gloss);
            block.SetFloat(EmissionId, emission);
            block.SetFloat(GradId, grad);
            block.SetFloat(FlatId, flat);
            block.SetFloat(TopBoostId, topBoost);
            r.SetPropertyBlock(block);
        }

        public static void SetColorOnly(Renderer r, Color color)
        {
            block ??= new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            r.SetPropertyBlock(block);
        }

        public static Renderer AddRenderer(GameObject go, Mesh mesh, Material material)
        {
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return r;
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }

    // Small procedural textures, generated once.
    public static class Tex
    {
        static Texture2D star, soft;

        public static Texture2D Star => star != null ? star : star = Make(64, (x, y, d) =>
        {
            float core = Mathf.Exp(-d * d * 14f);
            float rays = Mathf.Exp(-Mathf.Abs(x) * 16f) * Mathf.Exp(-Mathf.Abs(y) * 2.6f)
                       + Mathf.Exp(-Mathf.Abs(y) * 16f) * Mathf.Exp(-Mathf.Abs(x) * 2.6f);
            return Mathf.Clamp01(core + rays * 0.9f) * Mathf.Clamp01(1f - d);
        });

        public static Texture2D Soft => soft != null ? soft : soft = Make(64, (x, y, d) =>
            Mathf.Pow(Mathf.Clamp01(1f - d), 1.6f));

        static Texture2D Make(int size, System.Func<float, float, float, float> alpha)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int j = 0; j < size; j++)
            for (int i = 0; i < size; i++)
            {
                float x = (i + 0.5f) / size * 2f - 1f, y = (j + 0.5f) / size * 2f - 1f;
                float a = alpha(x, y, Mathf.Sqrt(x * x + y * y));
                px[j * size + i] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
            }
            t.SetPixels32(px);
            t.Apply(false, true);
            return t;
        }
    }
}
