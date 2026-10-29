using System;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;

namespace Ragna.Editor
{
    public static class ProjectSetup
    {
        [CliCommand("ragna_careers", "Create missing starter career assets without overwriting custom balancing", MainThreadRequired = true)]
        public static string EnsureCareers()
        {
            Directory.CreateDirectory("Assets/Ragna/Resources/Careers");
            CreateCareer("knight", "기사", "최대 HP +15% · 받는 피해 12% 감소\n보호막 지속 +0.4초 · 검과 방패", 1, 1.15f, 1, .88f, .4f, 1, new Color32(158, 209, 250, 255));
            CreateCareer("berserker", "광전사", "공격력 +20% · 공격 속도 +15%\n붉은 오라와 거대한 양손검", 1.2f, 1, 1.15f, 1, 0, 2, new Color32(255, 141, 105, 255));
            foreach (string id in new[] { "knight", "berserker" })
            {
                var career = AssetDatabase.LoadAssetAtPath<CareerDefinition>("Assets/Ragna/Resources/Careers/" + id + ".asset");
                if (career.unlockLevel == 20 && career.promotionStage == 1)
                { career.unlockLevel = 60; career.promotionStage = 2; EditorUtility.SetDirty(career); }
            }
            AssetDatabase.SaveAssets(); CareerCatalog.Reload();
            return "Knight and Berserker career data ready; existing assets preserved.";
        }
        static void CreateCareer(string id, string name, string description, float power, float hp, float speed, float damageTaken, float guardBonus, int appearance, Color tint)
        {
            string path = "Assets/Ragna/Resources/Careers/" + id + ".asset";
            if (AssetDatabase.LoadAssetAtPath<CareerDefinition>(path) != null) return;
            var career = ScriptableObject.CreateInstance<CareerDefinition>();
            career.id = id; career.displayName = name; career.description = description;
            career.damageMultiplier = power; career.healthMultiplier = hp; career.attackSpeedMultiplier = speed;
            career.damageTakenMultiplier = damageTaken; career.guardDurationBonus = guardBonus; career.appearance = appearance; career.tint = tint;
            AssetDatabase.CreateAsset(career, path);
        }
        [CliCommand("ragna_fonts", "Bake all current Korean game text into the static font atlas", MainThreadRequired = true)]
        public static string BakeFont()
        {
            const string path = "Assets/Ragna/Fonts/NanumGothic SDF.asset";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Ragna/Fonts/NanumGothic-Regular.ttf");
                font = TMP_FontAsset.CreateFontAsset(source, 48, 5, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                if (font == null) throw new InvalidOperationException("TMP resources are still importing. Retry setup when import completes.");
                font.name = "NanumGothic SDF"; AssetDatabase.CreateAsset(font, path);
            }
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            string chars = string.Concat(Directory.GetFiles("Assets/Ragna", "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
            foreach (var career in Resources.LoadAll<CareerDefinition>("Careers")) chars += career.displayName + career.description;
            chars += string.Concat(Enumerable.Range(32, 95).Select(i => (char)i));
            chars = new string(chars.Where(c => !char.IsControl(c)).Distinct().ToArray());
            font.TryAddCharacters(chars, out string missing);
            if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("Unsupported decorative font characters: " + missing);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            if (!AssetDatabase.Contains(font.material)) AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var texture in font.atlasTextures) if (!AssetDatabase.Contains(texture)) AssetDatabase.AddObjectToAsset(texture, font);
            EditorUtility.SetDirty(font); AssetDatabase.SaveAssets();
            return "Korean font atlas updated from current source text.";
        }
        [MenuItem("Ragna/Refresh Korean font")]
        public static void BakeFontMenu() => BakeFont();
        [CliCommand("ragna_capture", "Capture landscape Game view including overlay UI", MainThreadRequired = true)]
        public static string Capture([CliArg("output", "PNG output path relative to project root")] string output = "../Docs/Screenshots/game.png", [CliArg("width", "Landscape width")] int width = 1920, [CliArg("height", "Landscape height")] int height = 1080)
        {
            if (width <= height || height < 1) throw new ArgumentException("Only landscape capture sizes are supported.");
            var assembly = typeof(EditorWindow).Assembly;
            var gameViewType = assembly.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(gameViewType);
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance").GetValue(null);
            var groupEnum = assembly.GetType("UnityEditor.GameViewSizeGroupType");
            string groupName = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android ? "Android" :
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS ? "iOS" : "Standalone";
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupEnum, groupName) });
            var labels = (string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group, null);
            string sizeName = "Ragna Landscape " + width + "x" + height;
            int index = Array.FindIndex(labels, label => label.StartsWith(sizeName, StringComparison.Ordinal));
            if (index < 0)
            {
                var sizeType = assembly.GetType("UnityEditor.GameViewSize");
                var enumType = assembly.GetType("UnityEditor.GameViewSizeType");
                var fixedSize = Activator.CreateInstance(sizeType, new object[] { Enum.Parse(enumType, "FixedResolution"), width, height, sizeName });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { fixedSize });
                index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null) - 1;
            }
            gameViewType.GetProperty("selectedSizeIndex").SetValue(window, index);
            window.Show(); window.Focus();
            string path = Path.GetFullPath(output); Directory.CreateDirectory(Path.GetDirectoryName(path));
            EditorApplication.delayCall += () => EditorApplication.delayCall += () => ScreenCapture.CaptureScreenshot(path);
            return "Capture queued after Game view repaint: " + path;
        }
        [CliCommand("ragna_ui_checks", "Exercise actual UI button events without changing player saves", MainThreadRequired = true)]
        public static string CheckUI()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
            string result = UnityEngine.Object.FindFirstObjectByType<RagnaGame>().ReviewUIFlow();
            File.WriteAllText("../Docs/UI_CHECKS.txt", result);
            return result;
        }
        [CliCommand("ragna_preview", "Show an isolated growth fixture without changing saved progress", MainThreadRequired = true)]
        public static string Preview([CliArg("tier", "Growth stage 0 to 3")] int tier = 0, [CliArg("job", "Job ID: Warrior 0, Adventurer 7")] int job = 0,
            [CliArg("career", "Career ID for warrior promotion previews")] string career = "knight")
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
            var game = UnityEngine.Object.FindFirstObjectByType<RagnaGame>();
            if (game == null) throw new InvalidOperationException("Ragna scene is not running.");
            game.PreviewGrowth(tier, job, career);
            return "Review fixture ready; player save untouched.";
        }
        [MenuItem("Ragna/Build or refresh game scene")]
        public static void SetupMenu() => Setup();

        [CliCommand("ragna_setup", "Create the Ragna scene, font, pixel camera and mobile settings", MainThreadRequired = true)]
        public static string Setup()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before setup.");
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline == null || !pipeline.GetType().FullName.Contains("Universal")) throw new InvalidOperationException("URP is required.");
            Directory.CreateDirectory("Assets/Ragna/Scenes"); Directory.CreateDirectory("Assets/Ragna/Settings");
            EnsureCareers();
            // Import TMP shaders/settings shipped with uGUI; no external package version guessing.
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset") == null)
            {
                var resources = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories);
                if (resources.Length == 0) throw new FileNotFoundException("TMP Essential Resources not found in the installed uGUI package.");
                AssetDatabase.ImportPackage(resources[0], false);
                AssetDatabase.Refresh();
            }
            BakeFont();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Ragna/Fonts/NanumGothic SDF.asset");
            const string materialPath = "Assets/Ragna/Settings/Sprites.mat";
            var spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (spriteMaterial == null)
            {
                var sourceMaterial = pipeline.default2DMaterial;
                if (sourceMaterial == null) throw new InvalidOperationException("URP default sprite material is missing.");
                spriteMaterial = new Material(sourceMaterial); AssetDatabase.CreateAsset(spriteMaterial, materialPath);
            }
            // Re-running setup is deliberate and limited to the project's generated scene.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData));
            cameraGo.tag = "MainCamera"; cameraGo.transform.position = new Vector3(0, 0, -10);
            var camera = cameraGo.GetComponent<Camera>(); camera.orthographic = true;
            camera.orthographicSize = 180f / 32; camera.backgroundColor = new Color32(12, 19, 31, 255);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = camera.allowMSAA = camera.allowDynamicResolution = false;
            var pixel = cameraGo.AddComponent<PixelPerfectCamera>(); pixel.assetsPPU = 16;
            pixel.refResolutionX = 320; pixel.refResolutionY = 180;
            pixel.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping; pixel.cropFrame = PixelPerfectCamera.CropFrame.None;
            var data = cameraGo.GetComponent<UniversalAdditionalCameraData>(); data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None; data.volumeLayerMask = 1;
            Debug.Log("Available quality tiers: " + string.Join(", ", QualitySettings.names));
            QualitySettings.SetQualityLevel(0, true); QualitySettings.antiAliasing = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            urp.supportsHDR = true; urp.msaaSampleCount = 1; EditorUtility.SetDirty(urp);
            var light = new GameObject("Global Light", typeof(Light2D)).GetComponent<Light2D>();
            light.lightType = Light2D.LightType.Global; light.intensity = 1;
            string profilePath = "Assets/Ragna/Settings/Moonlight.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, profilePath);
                var tone = profile.Add<Tonemapping>(true); tone.mode.value = TonemappingMode.ACES;
                var bloom = profile.Add<Bloom>(true); bloom.intensity.value = .25f; bloom.threshold.value = .7f;
                var vignette = profile.Add<Vignette>(true); vignette.intensity.value = .22f; vignette.smoothness.value = .5f;
                foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
                EditorUtility.SetDirty(profile);
            }
            var volume = new GameObject("Moonlight Volume", typeof(Volume)).GetComponent<Volume>();
            volume.isGlobal = true; volume.sharedProfile = profile;
            var game = new GameObject("Ragna", typeof(RagnaGame)).GetComponent<RagnaGame>(); game.koreanFont = font;
            game.spriteMaterial = spriteMaterial;
            PlayerSettings.companyName = "RagnaStudio"; PlayerSettings.productName = "Ragna";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false; PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.ragnastudio.ragna");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.ragnastudio.ragna");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            string scenePath = "Assets/Ragna/Scenes/Ragna.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            AssetDatabase.SaveAssets();
            return "Ragna scene saved. URP 2D, Korean font, 320x180 / 16 PPU, 1920x1080 landscape Android+iOS.";
        }

        [MenuItem("Ragna/Build Android APK")]
        public static void BuildAndroid()
        {
            BakeFont();
            Directory.CreateDirectory("Builds/Android");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = "Builds/Android/Ragna.apk", target = BuildTarget.Android, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Android build failed: " + report.summary.result);
        }
        [MenuItem("Ragna/Export iOS Xcode project")]
        public static void BuildIOS()
        {
            BakeFont();
            Directory.CreateDirectory("Builds/iOS");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = "Builds/iOS", target = BuildTarget.iOS, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("iOS export failed: " + report.summary.result);
        }
    }
}
