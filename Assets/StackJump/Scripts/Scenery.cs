using UnityEngine;

namespace StackJump
{
    // Sky, fog, lighting globals, the floating start island and per-world backdrops,
    // with smooth crossfades when the climb enters a new world.
    public class Scenery : MonoBehaviour
    {
        static readonly int ShadeId = Shader.PropertyToID("_SJ_ShadeColor");
        static readonly int RimId = Shader.PropertyToID("_SJ_RimColor");

        Camera cam;
        Light sun;
        Material skyMat;
        Transform skyQuad;
        WorldTheme from, to;
        float blend = 1f;
        Transform backdrop, leaving;
        float enterT = 1f, leaveT = 1f;
        Renderer islandTop, islandRock, platform;

        public WorldTheme Current { get; private set; }

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

            var island = new GameObject("Island").transform;
            islandTop = Mats.AddRenderer(new GameObject("Top"), MeshFactory.Cylinder, Mats.Toon);
            islandTop.transform.SetParent(island, false);
            islandTop.transform.localPosition = new Vector3(0, -0.35f, 0);
            islandTop.transform.localScale = new Vector3(5.2f, 0.25f, 5.2f);
            islandRock = Mats.AddRenderer(new GameObject("Rock"), MeshFactory.Cone(1f, 1f, 10), Mats.Toon);
            islandRock.transform.SetParent(island, false);
            islandRock.transform.localPosition = new Vector3(0, -0.58f, 0);
            islandRock.transform.localRotation = Quaternion.Euler(180f, 0, 0);
            islandRock.transform.localScale = new Vector3(2.7f, 3.6f, 2.7f);
            platform = Mats.AddRenderer(new GameObject("Platform"), MeshFactory.RoundedBox(new Vector3(2.5f, 0.5f, 2.5f), 0.14f), Mats.Toon);
            platform.transform.SetParent(island, false);
            platform.transform.localPosition = new Vector3(0, -0.25f, 0);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 22f;
            RenderSettings.fogEndDistance = 130f;
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

            RenderSettings.fogColor = th.Fog;
            Shader.SetGlobalColor(ShadeId, th.Shade);
            Shader.SetGlobalColor(RimId, th.Rim);
            if (sun != null) sun.color = th.Light;
            cam.backgroundColor = th.SkyBottom;

            Mats.Set(islandTop, th.Ground, 0.1f, 0f, 0f, 0f, 0.2f);
            Mats.Set(islandRock, Color.Lerp(th.Ground, th.Shade, 0.45f), 0f, 0f, 0f, 0f, 0f);
            Mats.Set(platform, Color.Lerp(th.Ground, Color.white, 0.12f), 0.1f, 0f, 0.1f, 0f, 0.15f);
        }
    }
}
