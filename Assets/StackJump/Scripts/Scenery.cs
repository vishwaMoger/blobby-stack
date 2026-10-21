using System.Collections.Generic;
using UnityEngine;

namespace StackJump
{
    // Sky, fog, lighting globals, the themed start ground and per-world backdrops,
    // with smooth crossfades when the climb enters a new world. Supports a second camera for
    // split screen: each camera gets its own sky quad on its own layer.
    public class Scenery : MonoBehaviour
    {
        static readonly int ShadeId = Shader.PropertyToID("_SJ_ShadeColor");
        static readonly int RimId = Shader.PropertyToID("_SJ_RimColor");
        static readonly int SkyId = Shader.PropertyToID("_SJ_SkyColor");
        const int FirstSkyLayer = 30;

        class Sky
        {
            public Camera Cam;
            public Transform Quad;
            public Material Mat;
        }

        readonly List<Sky> skies = new();
        Light sun;
        WorldTheme from, to;
        float blend = 1f;
        Transform backdrop, leaving;
        float enterT = 1f, leaveT = 1f;
        Transform ground;
        string groundKey;

        public WorldTheme Current { get; private set; }

        public void Init(Camera camera, Light light)
        {
            sun = light;
            AddCamera(camera);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 18f;
            RenderSettings.fogEndDistance = 150f;
        }

        // Each camera sees only its own sky quad.
        public void AddCamera(Camera camera)
        {
            int layer = FirstSkyLayer + skies.Count;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.farClipPlane = 220f;
            camera.cullingMask = ~((1 << FirstSkyLayer) | (1 << (FirstSkyLayer + 1))) | (1 << layer);

            var mat = new Material(Mats.Sky);
            var go = new GameObject("Sky " + skies.Count) { layer = layer };
            Mats.AddRenderer(go, MeshFactory.Quad(), mat);
            go.transform.SetParent(camera.transform, false);
            skies.Add(new Sky { Cam = camera, Quad = go.transform, Mat = mat });
            if (Current != null) Apply(Current);
        }

        // The themed ground the towers start on; rebuilt when the world or lane layout changes.
        public void SetGround(WorldTheme theme, float[] pedestals)
        {
            string key = theme.Name + string.Join(",", pedestals);
            if (ground != null && groundKey == key) return;
            if (ground != null) Destroy(ground.gameObject);
            ground = Ground.Build(theme, pedestals);
            groundKey = key;
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

            foreach (var s in skies)
            {
                if (!s.Cam.isActiveAndEnabled) continue;
                float d = s.Cam.farClipPlane * 0.9f;
                float halfH = Mathf.Tan(s.Cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * d * 1.08f;
                s.Quad.localPosition = new Vector3(0, 0, d);
                s.Quad.localRotation = Quaternion.identity;
                s.Quad.localScale = new Vector3(halfH * 2f * s.Cam.aspect, halfH * 2f, 1f);
                s.Mat.SetFloat("_Aspect", s.Cam.aspect);
                s.Mat.SetFloat("_Scroll", s.Cam.transform.position.y * 0.004f);
            }

            PlaceBackdrops(dt);
        }

        // Backdrops follow the average height of the active cameras.
        float CameraY()
        {
            float sum = 0f;
            int n = 0;
            foreach (var s in skies)
            {
                if (!s.Cam.isActiveAndEnabled) continue;
                sum += s.Cam.transform.position.y;
                n++;
            }
            return n > 0 ? sum / n : 0f;
        }

        void PlaceBackdrops(float dt)
        {
            float baseY = CameraY() - 6f;
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
            foreach (var s in skies)
            {
                var m = s.Mat;
                m.SetColor("_TopColor", th.SkyTop);
                m.SetColor("_MidColor", th.SkyMid);
                m.SetColor("_BottomColor", th.SkyBottom);
                m.SetFloat("_MidPoint", th.MidPoint);
                m.SetFloat("_Stars", th.Stars);
                m.SetFloat("_Nebula", th.Nebula);
                m.SetColor("_NebulaColor", th.NebulaColor);
                m.SetColor("_SunColor", th.SunColor);
                m.SetVector("_SunPos", th.SunPos);
                m.SetFloat("_SunSize", th.SunSize);
                m.SetFloat("_SunGlow", th.SunGlow);
                m.SetFloat("_SunStripes", th.SunStripes);
                m.SetFloat("_Clouds", th.Clouds);
                m.SetColor("_CloudColor", th.CloudColor);
                s.Cam.backgroundColor = th.SkyBottom;
            }

            // Lean the fog toward the sky's bottom color so the far ground melts into the horizon.
            RenderSettings.fogColor = Color.Lerp(th.Fog, th.SkyBottom, 0.6f);
            Shader.SetGlobalColor(ShadeId, th.Shade);
            Shader.SetGlobalColor(RimId, th.Rim);
            Shader.SetGlobalColor(SkyId, th.SkyAmbient);
            if (sun != null) sun.color = th.Light;
        }
    }
}
