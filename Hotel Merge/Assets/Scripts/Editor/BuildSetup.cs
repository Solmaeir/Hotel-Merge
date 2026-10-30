using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace HotelMerge.EditorTools
{
    /// <summary>
    /// "Hotel Merge → Build Ayarlarını Uygula": portrait Player settings for Android and WebGL, the app icon
    /// (Assets/HotelMerge/Icon), Unity's splash and the scene list (Splash first). Safe to run again.
    /// </summary>
    public static class BuildSetup
    {
        const string IconDir = "Assets/HotelMerge/Icon";
        static readonly Color SplashPink = new Color(1f, 0.56f, 0.72f);

        [MenuItem("Hotel Merge/Build Ayarlarını Uygula", priority = 6)]
        static void Menu()
        {
            Apply();
            EditorUtility.DisplayDialog("Hotel Merge", "Android ve WebGL için Player ayarları, ikon ve sahne listesi uygulandı.", "Tamam");
        }

        public static string Apply()
        {
            var icon = LoadIcon("AppIcon");
            var background = LoadIcon("AppIcon_Background");
            var foreground = LoadIcon("AppIcon_Foreground");

            // Common
            PlayerSettings.productName = "Hotel Merge";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.runInBackground = false; // the game pauses in the background (GameRoot)
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            // Unity's own splash: our Splash scene is the real opening screen, so the engine splash is turned off.
            // (If the license forces it on, it at least uses the game's colours and logo.)
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = SplashPink;
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Art/UI/logo.png");
            if (logo) PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(2f, logo) };

            // Android
            var android = NamedBuildTarget.Android;
            if (PlayerSettings.GetApplicationIdentifier(android).Contains("UnityTechnologies"))
                PlayerSettings.SetApplicationIdentifier(android, "com.DefaultCompany.HotelMerge");
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64; // required by Google Play
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.startInFullscreen = true;
            PlayerSettings.Android.renderOutsideSafeArea = true; // draw under the notch; ScreenFit keeps UI inside the safe area
            SetAndroidIcons(AndroidPlatformIconKind.Adaptive, background, foreground);

            // WebGL: a phone-shaped canvas; Brotli with a fallback so it runs on any web server.
            PlayerSettings.defaultWebScreenWidth = 540;
            PlayerSettings.defaultWebScreenHeight = 960;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.WebGL.template = "PROJECT:HotelMerge"; // Assets/WebGLTemplates/HotelMerge: loader page in the Splash scene's look
            // Less generated C++ (shared generics, more unused engine code stripped): the IL2CPP + emscripten steps
            // need much less memory and time, which keeps the build from running the machine out of RAM.
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Medium);

            ApplyRenderQuality();

            // Scenes: Splash first, then the game.
            var scenes = new[] { SplashBuilder.SplashPath, "Assets/Scenes/SampleScene.unity" };
            EditorBuildSettings.scenes = scenes.Select(p => new EditorBuildSettingsScene(p, true))
                .Concat(EditorBuildSettings.scenes.Where(s => !scenes.Contains(s.path))).ToArray();

            AssetDatabase.SaveAssets();
            string result = $"icon={(icon ? icon.name : "YOK")} splash.show={PlayerSettings.SplashScreen.show} androidId={PlayerSettings.GetApplicationIdentifier(android)} " +
                            $"webTemplate={PlayerSettings.WebGL.template} scenes={string.Join(",", EditorBuildSettings.scenes.Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path)))}";
            Debug.Log("Hotel Merge: build ayarları uygulandı. " + result);
            return result;
        }

        /// <summary>
        /// Crisper 3D: MSAA instead of the camera's SMAA, full render scale on phones, sharper and soft shadows
        /// (a shorter shadow distance packs the shadow map's pixels onto the hotel).
        /// </summary>
        static void ApplyRenderQuality()
        {
            foreach (var path in new[] { "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/Mobile_RPAsset.asset" })
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (!asset) continue;
                bool mobile = path.Contains("Mobile");
                var so = new SerializedObject(asset);
                so.FindProperty("m_MSAA").intValue = mobile ? 2 : 4;
                so.FindProperty("m_RenderScale").floatValue = 1f;
                so.FindProperty("m_MainLightShadowmapResolution").intValue = 2048;
                so.FindProperty("m_ShadowDistance").floatValue = 35f;
                so.FindProperty("m_SoftShadowsSupported").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static Texture2D LoadIcon(string name)
        {
            string path = $"{IconDir}/{name}.png";
            if (AssetImporter.GetAtPath(path) is TextureImporter ti &&
                (ti.textureCompression != TextureImporterCompression.Uncompressed || ti.mipmapEnabled || ti.npotScale != TextureImporterNPOTScale.None))
            {
                ti.textureType = TextureImporterType.Default;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.mipmapEnabled = false;
                ti.npotScale = TextureImporterNPOTScale.None;
                ti.alphaIsTransparency = true;
                ti.maxTextureSize = 1024;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void SetAndroidIcons(PlatformIconKind kind, params Texture2D[] layers)
        {
            var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
            foreach (var i in icons)
                for (int l = 0; l < i.maxLayerCount && l < layers.Length; l++)
                    i.SetTexture(layers[l], l);
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
        }
    }
}
