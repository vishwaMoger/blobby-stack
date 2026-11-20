using System.IO;
using System.Linq;
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
    //   -executeMethod StackJump.EditorTools.StackJumpSetup.BuildPoki
    //   -executeMethod StackJump.EditorTools.StackJumpSetup.BuildGameDistribution
    //   -executeMethod StackJump.EditorTools.StackJumpSetup.BuildItch
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
            PlayerSettings.SplashScreen.backgroundColor = new Color32(0x1E, 0x0D, 0x46, 0xFF);
            ConfigureSizeOptimizations();
        }

        // Smaller downloads load faster (conversion) and Poki targets 5 MB initial / 8 MB total.
        static void ConfigureSizeOptimizations()
        {
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.WebGL, Il2CppCompilerConfiguration.Master);

            // Unity 6 WebGL "Code Optimization: Disk Size with LTO", set via reflection so this script
            // still compiles if the WebGL extension's API moves.
            var settings = System.Type.GetType("UnityEditor.WebGL.UserBuildSettings, UnityEditor.WebGL.Extensions");
            var prop = settings?.GetProperty("codeOptimization", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (prop != null && prop.PropertyType.IsEnum && System.Enum.IsDefined(prop.PropertyType, "DiskSizeLTO"))
                prop.SetValue(null, System.Enum.Parse(prop.PropertyType, "DiskSizeLTO"));
            else
                Debug.LogWarning("[StackJumpSetup] WebGL code optimization setting not found; skipping LTO.");

            // Drop shader variants the game never uses (debug views, unused post-processing).
            if (GraphicsSettings.TryGetRenderPipelineSettings<ShaderStrippingSetting>(out var strip))
                strip.stripRuntimeDebugShaders = true;
            if (GraphicsSettings.TryGetRenderPipelineSettings<URPShaderStrippingSetting>(out var urpStrip))
            {
                urpStrip.stripUnusedPostProcessingVariants = true;
                urpStrip.stripUnusedVariants = true;
                urpStrip.stripScreenCoordOverrideVariants = true;
            }
        }

        // ---------- Store covers ----------

        // Enters Play mode with SJ_ARGS (e.g. "shot&world=0") and SJ_CAPTURE ("outDir|prefix")
        // set in the environment; CoverShooter renders the covers and quits. Run without -quit.
        public static void CaptureCovers()
        {
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        // Same Play-mode entry for the trailer: SJ_ARGS="trailer" and SJ_TRAILER="outDir|coverDir".
        public static void CaptureTrailer() => CaptureCovers();

        // ---------- Builds ----------

        public static void BuildDev() => Build("Builds/WebGL-dev", WebGLCompressionFormat.Disabled, WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly, true);

        // Explicitly thrown exceptions stay catchable in release, so the try/catch guards around
        // the SDK work instead of an exception aborting the whole WebGL player.
        // Basic Launch build: CrazyGames disables ads there, so no ad buttons at all.
        public static void BuildRelease() => Build("Builds/WebGL", WebGLCompressionFormat.Brotli, WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly, false);

        // Full Launch build: midgame and rewarded ads through the SDK.
        public static void BuildReleaseFull() => Build("Builds/WebGL-full", WebGLCompressionFormat.Brotli, WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly, true);

        // Poki build: Poki SDK + Poki's WebGL template.
        public static void BuildPoki() => BuildForPortal(Portal.Poki, "Builds/Poki");

        // GameDistribution build: GD SDK loaded by the gamedistribution template. The game ID from the
        // GameDistribution control panel comes from SJ_GD_GAME_ID or Submission/gamedistribution/game-id.txt.
        public static void BuildGameDistribution() => BuildForPortal(Portal.GameDistribution, "Builds/GameDistribution");

        // itch.io build: no portal SDK and no ads; saves stay in the browser's local storage.
        public static void BuildItch() => BuildForPortal(Portal.Itch, "Builds/Itch");

        enum Portal { CrazyGames, Poki, GameDistribution, Itch }

        // Other portals: the CrazyGames SDK (whose site-lock crashes the game outside CrazyGames) is
        // compiled out, its jslib and Resources left out of the build.
        // Brotli with decompression fallback works whether or not the host sends encoding headers.
        static void BuildForPortal(Portal portal, string output)
        {
            const string crazyResources = "Assets/CrazySDK/Resources";
            const string parkedResources = "Assets/CrazySDK/ResourcesExcluded";
            bool parked = AssetDatabase.IsValidFolder(crazyResources)
                && string.IsNullOrEmpty(AssetDatabase.MoveAsset(crazyResources, parkedResources));
            try
            {
                Build(output, WebGLCompressionFormat.Brotli, WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly, false, portal, exit: false);
                if (lastBuildSucceeded && portal == Portal.GameDistribution) InjectGameDistributionId(output);
            }
            finally
            {
                // Leave the project in its default (CrazyGames) configuration.
                SetPortalPlugins(Portal.CrazyGames);
                if (parked) AssetDatabase.MoveAsset(parkedResources, crazyResources);
                SetDefines(false, Portal.CrazyGames);
                PlayerSettings.WebGL.template = "PROJECT:FullWindow";
                PlayerSettings.WebGL.decompressionFallback = false;
                AssetDatabase.SaveAssets();
            }
            EditorApplication.Exit(lastBuildSucceeded ? 0 : 1);
        }

        const string GameIdPlaceholder = "__GD_GAME_ID__";

        static void InjectGameDistributionId(string output)
        {
            var id = System.Environment.GetEnvironmentVariable("SJ_GD_GAME_ID");
            const string idFile = "Submission/gamedistribution/game-id.txt";
            if (string.IsNullOrWhiteSpace(id) && File.Exists(idFile)) id = File.ReadAllText(idFile);
            id = id?.Trim();
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[StackJumpSetup] No GameDistribution game ID; replace {GameIdPlaceholder} in {output}/index.html before uploading.");
                return;
            }
            var index = Path.Combine(output, "index.html");
            File.WriteAllText(index, File.ReadAllText(index).Replace(GameIdPlaceholder, id));
            Debug.Log("[StackJumpSetup] GameDistribution game ID set.");
        }

        const string CrazyJslib = "Assets/Plugins/crazySDK.jslib";
        const string PokiJslib = "Assets/PokiSDK/Plugins/PokiSDKBridge.jslib";
        const string GdJslib = Root + "/Plugins/GameDistribution.jslib";
        static bool lastBuildSucceeded;

        // Each build ships only its own portal's JavaScript bridge.
        static void SetPortalPlugins(Portal portal)
        {
            SetPlugin(CrazyJslib, portal == Portal.CrazyGames);
            SetPlugin(PokiJslib, portal == Portal.Poki);
            SetPlugin(GdJslib, portal == Portal.GameDistribution);
            // (Portal.Itch enables none of them.)
        }

        static void SetPlugin(string path, bool enabled)
        {
            if (AssetImporter.GetAtPath(path) is not PluginImporter importer) return;
            importer.SetCompatibleWithAnyPlatform(false);
            importer.SetCompatibleWithEditor(false);
            importer.SetCompatibleWithPlatform(BuildTarget.WebGL, enabled);
            importer.SaveAndReimport();
        }

        // Sets our build symbols while keeping any others (e.g. ones the CrazyGames tools add).
        static void SetDefines(bool ads, Portal portal)
        {
            var ours = new[] { "SJ_ADS", "SJ_POKI", "SJ_GD", "SJ_ITCH" };
            var current = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.WebGL)
                .Split(';').Where(d => d.Length > 0 && !ours.Contains(d)).ToList();
            if (ads) current.Add("SJ_ADS");
            if (portal == Portal.Poki) current.Add("SJ_POKI");
            if (portal == Portal.GameDistribution) current.Add("SJ_GD");
            if (portal == Portal.Itch) current.Add("SJ_ITCH");
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.WebGL, string.Join(";", current));
        }

        static void Build(string output, WebGLCompressionFormat compression, WebGLExceptionSupport exceptions, bool ads, Portal portal = Portal.CrazyGames, bool exit = true)
        {
            ConfigurePlayer();
            PlayerSettings.WebGL.compressionFormat = compression;
            PlayerSettings.WebGL.exceptionSupport = exceptions;
            if (portal != Portal.CrazyGames)
            {
                PlayerSettings.WebGL.template = portal == Portal.Poki ? "PROJECT:poki-template"
                    : portal == Portal.GameDistribution ? "PROJECT:gamedistribution" : "PROJECT:FullWindow";
                PlayerSettings.WebGL.decompressionFallback = true;
            }
            SetPortalPlugins(portal);
            SetDefines(ads, portal);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });
            var s = report.summary;
            lastBuildSucceeded = s.result == BuildResult.Succeeded;
            Debug.Log($"[StackJumpSetup] Build {s.result}: {s.totalSize / (1024f * 1024f):F1} MB, {s.totalErrors} errors, output {output}");
            if (exit) EditorApplication.Exit(lastBuildSucceeded ? 0 : 1);
        }
    }
}
