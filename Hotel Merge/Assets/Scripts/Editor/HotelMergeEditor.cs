using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HotelMerge.EditorTools
{
    /// <summary>Imports everything under Resources/Art as crisp, uncompressed UI sprites with 9-slice borders.</summary>
    public class ArtImporter : AssetPostprocessor
    {
        public override uint GetVersion() => 5;

        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Art/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.filterMode = FilterMode.Bilinear;
            ti.spritePixelsPerUnit = 100;
            ti.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(settings);

            var artPath = assetPath.Replace('\\', '/');
            float b = Art.BorderForKey(Path.ChangeExtension(artPath.Substring(artPath.IndexOf("/Resources/Art/") + "/Resources/Art/".Length), null));
            ti.spriteBorder = new Vector4(b, b, b, b);

            // Icon packs and furniture thumbnails are many and only shown small: keep them compressed.
            if (artPath.Contains("/Resources/Art/Packs/") || artPath.Contains("/Resources/Art/Furniture/") ||
                artPath.Contains("/Resources/Art/Kit/Icons/") || artPath.Contains("/Resources/Art/Kit/Portraits/"))
            {
                ti.spriteBorder = Vector4.zero;
                ti.maxTextureSize = 256;
                ti.textureCompression = TextureImporterCompression.Compressed;
                ti.crunchedCompression = false;
            }
            // Merge items are drawn at 512 px; compressed they stay light for web builds.
            if (artPath.Contains("/Resources/Art/Items/"))
            {
                ti.maxTextureSize = 512;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
                ti.crunchedCompression = false;
            }
        }
    }

    /// <summary>Keeps reference materials in Resources so URP Lit/Unlit shaders end up in player builds.</summary>
    [InitializeOnLoad]
    static class ShaderReferences
    {
        static ShaderReferences()
        {
            EditorApplication.delayCall += Ensure;
        }

        static void Ensure()
        {
            const string dir = "Assets/Resources/Materials";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/Resources", "Materials");
            Create(dir + "/LitRef.mat", "Universal Render Pipeline/Lit");
            Create(dir + "/UnlitRef.mat", "Universal Render Pipeline/Unlit");
        }

        static void Create(string path, string shaderName)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path)) return;
            var shader = Shader.Find(shaderName);
            if (!shader) return;
            AssetDatabase.CreateAsset(new Material(shader), path);
        }
    }

    static class Menu
    {
        [MenuItem("Hotel Merge/Görselleri Yeniden İçe Aktar")]
        static void ReimportArt()
        {
            AssetDatabase.ImportAsset("Assets/Resources/Art", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        }

        [MenuItem("Hotel Merge/Dikey Ekranı Ayarla (Portrait)")]
        static void PortraitOnly()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            Debug.Log("Hotel Merge: uygulama yönü dikey (Portrait) olarak ayarlandı.");
        }

        [MenuItem("Hotel Merge/Kaydı Sıfırla")]
        static void ResetSave()
        {
            GameState.DeleteSave();
            Debug.Log("Hotel Merge: kayıt silindi.");
        }
    }

    /// <summary>
    /// Adds the resolutions of common phones and tablets to the Game view size menu,
    /// so the layout can be checked against many screens (the Device Simulator also simulates notches).
    /// </summary>
    static class DeviceSizes
    {
        static readonly (string name, int w, int h)[] Devices =
        {
            ("Portrait 1080x1920", 1080, 1920),
            ("iPhone SE 750x1334", 750, 1334),
            ("iPhone 15 1179x2556", 1179, 2556),
            ("iPhone 15 Pro Max 1290x2796", 1290, 2796),
            ("Pixel 8 1080x2400", 1080, 2400),
            ("Galaxy S24 Ultra 1440x3120", 1440, 3120),
            ("Galaxy A 720x1600", 720, 1600),
            ("Xperia 21:9 1080x2520", 1080, 2520),
            ("Galaxy Z Fold (kapalı) 904x2316", 904, 2316),
            ("Galaxy Z Fold (açık) 1812x2176", 1812, 2176),
            ("iPad 10 1640x2360", 1640, 2360),
            ("iPad Pro 13 2064x2752", 2064, 2752),
            ("Galaxy Tab 1600x2560", 1600, 2560),
        };

        [MenuItem("Hotel Merge/Cihaz Çözünürlüklerini Ekle")]
        static void AddAll()
        {
            var asm = typeof(UnityEditor.Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var sizeType = asm.GetType("UnityEditor.GameViewSize");
            var sizeKind = asm.GetType("UnityEditor.GameViewSizeType");
            var instance = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            var groupType = sizesType.GetProperty("currentGroupType").GetValue(instance);
            var group = sizesType.GetMethod("GetGroup").Invoke(instance, new[] { groupType });
            var texts = ((string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group, null)).ToList();
            var ctor = sizeType.GetConstructor(new[] { sizeKind, typeof(int), typeof(int), typeof(string) });
            var fixedRes = Enum.Parse(sizeKind, "FixedResolution");
            int added = 0;
            foreach (var d in Devices)
            {
                if (texts.Any(t => t.StartsWith(d.name))) continue;
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { ctor.Invoke(new[] { fixedRes, d.w, d.h, d.name }) });
                added++;
            }
            sizesType.GetMethod("SaveToHDD").Invoke(instance, null);
            Debug.Log($"Hotel Merge: {added} cihaz çözünürlüğü Game view listesine eklendi.");
        }
    }
}

namespace HotelMerge.EditorTools
{
    /// <summary>Switches the Game view to a fixed resolution (adds it to the size list if needed). Used for layout tests.</summary>
    public static class GameViewResolution
    {
        public static void Set(int width, int height)
        {
            var asm = typeof(UnityEditor.Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var sizeType = asm.GetType("UnityEditor.GameViewSize");
            var sizeKind = asm.GetType("UnityEditor.GameViewSizeType");
            var instance = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            var groupType = sizesType.GetProperty("currentGroupType").GetValue(instance);
            var group = sizesType.GetMethod("GetGroup").Invoke(instance, new[] { groupType });
            int total = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
            int index = -1;
            for (int i = 0; i < total; i++)
            {
                var s = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                if ((int)sizeType.GetProperty("width").GetValue(s) == width && (int)sizeType.GetProperty("height").GetValue(s) == height &&
                    sizeType.GetProperty("sizeType").GetValue(s).ToString() == "FixedResolution") { index = i; break; }
            }
            if (index < 0)
            {
                var ctor = sizeType.GetConstructor(new[] { sizeKind, typeof(int), typeof(int), typeof(string) });
                var size = ctor.Invoke(new[] { System.Enum.Parse(sizeKind, "FixedResolution"), width, height, $"{width}x{height}" });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
                index = total;
            }
            var gameViewType = asm.GetType("UnityEditor.GameView");
            var window = UnityEditor.EditorWindow.GetWindow(gameViewType, false, null, false);
            gameViewType.GetMethod("SizeSelectionCallback", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                .Invoke(window, new object[] { index, null });
            window.Repaint();
        }
    }
}
