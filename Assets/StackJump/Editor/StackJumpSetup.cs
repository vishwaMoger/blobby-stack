using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace StackJump.EditorTools
{
    // One-click (or headless) project setup and WebGL builds.
    //   -executeMethod StackJump.EditorTools.StackJumpSetup.Setup
    //   -executeMethod StackJump.EditorTools.StackJumpSetup.BuildDev
    //   -executeMethod StackJump.EditorTools.StackJumpSetup.BuildRelease
    public static class StackJumpSetup
    {
        const string Root = "Assets/StackJump";
        const string ScenePath = Root + "/Scenes/Game.unity";
        const string MaterialsPath = Root + "/Resources/Materials";
        const string ProfilePath = Root + "/Settings/PostFX.asset";

        [MenuItem("Stack Jump/Setup Project")]
        public static void SetupMenu() => RunSetup();

        public static void Setup()
        {
            RunSetup();
            EditorApplication.Exit(0);
        }

        static void RunSetup()
        {
            RemoveTemplateContent();
            CreateMaterials();
            var profile = CreatePostProfile();
            ConfigureRenderPipeline();
            CreateScene(profile);
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[StackJumpSetup] Setup complete.");
        }

        static void RemoveTemplateContent()
        {
            foreach (var path in new[] { "Assets/TutorialInfo", "Assets/Readme.asset", "Assets/Scenes", "Assets/Settings/SampleSceneProfile.asset" })
                if (AssetDatabase.LoadAssetAtPath<Object>(path) != null || AssetDatabase.IsValidFolder(path))
                    AssetDatabase.DeleteAsset(path);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void CreateMaterials()
        {
            EnsureFolder(MaterialsPath);
            CreateMaterial("SJ_Toon", "StackJump/Toon");
            CreateMaterial("SJ_Unlit", "StackJump/Unlit");
            CreateMaterial("SJ_Sky", "StackJump/Sky");
        }

        static void CreateMaterial(string name, string shaderName)
        {
            var path = $"{MaterialsPath}/{name}.mat";
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"[StackJumpSetup] Shader not found: {shaderName}");
                return;
            }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) AssetDatabase.CreateAsset(new Material(shader), path);
            else mat.shader = shader;
        }

        static VolumeProfile CreatePostProfile()
        {
            EnsureFolder(Root + "/Settings");
            AssetDatabase.DeleteAsset(ProfilePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(0.9f);
            bloom.scatter.Override(0.65f);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.5f);
            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(8f);
            color.contrast.Override(6f);

            foreach (var component in profile.components)
            {
                component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static void ConfigureRenderPipeline()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null) continue;
                asset.supportsHDR = true;
                asset.msaaSampleCount = 4;
                // One soft cascade covering the tower and nearby props.
                asset.shadowDistance = 28f;
                asset.shadowCascadeCount = 1;
                asset.mainLightShadowmapResolution = 2048;
                var so = new SerializedObject(asset);
                void SetBool(string name, bool value)
                {
                    var p = so.FindProperty(name);
                    if (p != null) p.boolValue = value;
                }
                SetBool("m_MainLightShadowsSupported", true);
                SetBool("m_SoftShadowsSupported", true);
                SetBool("m_AdditionalLightShadowsSupported", false);
                SetBool("m_RequireDepthTexture", false);
                SetBool("m_RequireOpaqueTexture", false);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
        }

        static void CreateScene(VolumeProfile profile)
        {
            EnsureFolder(Root + "/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.08f, 0.3f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 220f;
            cam.fieldOfView = 42f;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            camGo.AddComponent<AudioListener>();
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            camData.renderShadows = true;
            camGo.AddComponent<CameraRig>();
            camGo.transform.position = new Vector3(0, 2.6f, -9.5f);

            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 1f;
            light.shadowBias = 0.05f;
            light.shadowNormalBias = 0.4f;
            lightGo.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            var volumeGo = new GameObject("Global Volume");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            new GameObject("Game").AddComponent<GameManager>();

            // Fog is driven at runtime; enabling it here keeps the fog shader variants in the build.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 22f;
            RenderSettings.fogEndDistance = 130f;
            RenderSettings.fogColor = new Color(0.16f, 0.08f, 0.32f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.5f);
            RenderSettings.skybox = null;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.productName = "Blobby Stack";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = false;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.WebGL.template = "PROJECT:FullWindow";
            // Every second before gameplay costs conversion; the page shows its own loader instead.
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
        }

        // ---------- Store covers ----------

        // Enters Play mode with SJ_ARGS (e.g. "shot&world=0") and SJ_CAPTURE ("outDir|prefix")
        // set in the environment; CoverShooter renders the covers and quits. Run without -quit.
        public static void CaptureCovers()
        {
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        // ---------- Builds ----------

        public static void BuildDev() => Build("Builds/WebGL-dev", WebGLCompressionFormat.Disabled, WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly, true);

        // Explicitly thrown exceptions stay catchable in release, so the try/catch guards around
        // the SDK work instead of an exception aborting the whole WebGL player.
        // Basic Launch build: CrazyGames disables ads there, so no ad buttons at all.
        public static void BuildRelease() => Build("Builds/WebGL", WebGLCompressionFormat.Brotli, WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly, false);

        // Full Launch build: midgame and rewarded ads through the SDK.
        public static void BuildReleaseFull() => Build("Builds/WebGL-full", WebGLCompressionFormat.Brotli, WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly, true);

        static void Build(string output, WebGLCompressionFormat compression, WebGLExceptionSupport exceptions, bool ads)
        {
            ConfigurePlayer();
            PlayerSettings.WebGL.compressionFormat = compression;
            PlayerSettings.WebGL.exceptionSupport = exceptions;
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.WebGL, ads ? "SJ_ADS" : "");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });
            var s = report.summary;
            Debug.Log($"[StackJumpSetup] Build {s.result}: {s.totalSize / (1024f * 1024f):F1} MB, {s.totalErrors} errors, output {output}");
            EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
