using UnityEngine;

namespace StackJump
{
    // Sky, fog, lighting globals, the themed start ground and per-world backdrops,
    // with smooth crossfades when the climb enters a new world.
    public class Scenery : MonoBehaviour
    {
        static readonly int ShadeId = Shader.PropertyToID("_SJ_ShadeColor");
        static readonly int RimId = Shader.PropertyToID("_SJ_RimColor");
        static readonly int SkyId = Shader.PropertyToID("_SJ_SkyColor");

        Camera cam;
        Light sun;
        Material skyMat;
        Transform skyQuad;
        WorldTheme from, to;
        float blend = 1f;
        Transform backdrop, leaving;
        float enterT = 1f, leaveT = 1f;
        Transform ground;

        public WorldTheme Current { get; private set; }

        // The themed ground the tower starts on; rebuilt when a run starts in another world.
        public void SetGround(WorldTheme theme)
        {
            if (ground != null && ground.name == "Ground " + theme.Name) return;
            if (ground != null) Destroy(ground.gameObject);
            ground = Ground.Build(theme);
        }

        public void Init(Camera camera, Light light)
        {
            cam = camera;
            sun = light;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.farClipPlane = 220f;

            skyMat = new Material(Mats.Sky);
            var sky = new GameObject("Sky");
            Mats.AddRenderer(sky, MeshFactory.Quad(), skyMat);
            skyQuad = sky.transform;
            skyQuad.SetParent(cam.transform, false);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 18f;
            RenderSettings.fogEndDistance = 150f;
        }

        public void SetWorld(WorldTheme theme, bool instant)
        {
            if (!instant && to == theme) return;
            from = Current ?? theme;
            to = theme;
            blend = instant ? 1f : 0f;

            if (backdrop != null)
            {
                if (instant) Destroy(backdrop.gameObject);
                else
                {
                    if (leaving != null) Destroy(leaving.gameObject);
                    leaving = backdrop;
                    leaveT = 0f;
                }
            }
            backdrop = Backdrops.Build(theme);
            enterT = instant ? 1f : -0.35f;
            PlaceBackdrops(0f);

            Current = instant ? theme : from;
            Apply(Current);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (blend < 1f)
            {
                blend = Mathf.MoveTowards(blend, 1f, dt / 1.8f);
                Current = WorldTheme.Lerp(from, to, Mathf.SmoothStep(0f, 1f, blend));
                Apply(Current);
            }

            float d = cam.farClipPlane * 0.9f;
            float halfH = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * d * 1.08f;
            skyQuad.localPosition = new Vector3(0, 0, d);
            skyQuad.localRotation = Quaternion.identity;
            skyQuad.localScale = new Vector3(halfH * 2f * cam.aspect, halfH * 2f, 1f);
            skyMat.SetFloat("_Aspect", cam.aspect);
            skyMat.SetFloat("_Scroll", cam.transform.position.y * 0.004f);

            PlaceBackdrops(dt);
        }

        void PlaceBackdrops(float dt)
        {
            float baseY = cam.transform.position.y - 6f;
            if (backdrop != null)
            {
                enterT = Mathf.MoveTowards(enterT, 1f, dt / 1.5f);
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(enterT), 3f);
                backdrop.position = new Vector3(0, baseY - (1f - e) * 35f, 0);
            }
            if (leaving != null)
            {
                leaveT += dt / 1.1f;
                leaving.position = new Vector3(0, baseY - leaveT * leaveT * 35f, 0);
                if (leaveT >= 1f)
                {
                    Destroy(leaving.gameObject);
                    leaving = null;
                }
            }
        }

        void Apply(WorldTheme th)
        {
            skyMat.SetColor("_TopColor", th.SkyTop);
            skyMat.SetColor("_MidColor", th.SkyMid);
            skyMat.SetColor("_BottomColor", th.SkyBottom);
            skyMat.SetFloat("_MidPoint", th.MidPoint);
            skyMat.SetFloat("_Stars", th.Stars);
            skyMat.SetFloat("_Nebula", th.Nebula);
            skyMat.SetColor("_NebulaColor", th.NebulaColor);
            skyMat.SetColor("_SunColor", th.SunColor);
            skyMat.SetVector("_SunPos", th.SunPos);
            skyMat.SetFloat("_SunSize", th.SunSize);
            skyMat.SetFloat("_SunGlow", th.SunGlow);
            skyMat.SetFloat("_SunStripes", th.SunStripes);
            skyMat.SetFloat("_Clouds", th.Clouds);
            skyMat.SetColor("_CloudColor", th.CloudColor);

            // Lean the fog toward the sky's bottom color so the far ground melts into the horizon.
            RenderSettings.fogColor = Color.Lerp(th.Fog, th.SkyBottom, 0.6f);
            Shader.SetGlobalColor(ShadeId, th.Shade);
            Shader.SetGlobalColor(RimId, th.Rim);
            Shader.SetGlobalColor(SkyId, th.SkyAmbient);
            if (sun != null) sun.color = th.Light;
            cam.backgroundColor = th.SkyBottom;
        }
    }
}
