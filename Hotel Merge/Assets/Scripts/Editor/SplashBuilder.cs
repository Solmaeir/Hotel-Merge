using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HotelMerge.EditorTools
{
    /// <summary>"Hotel Merge → Açılış Sahnesini Kur": creates Splash.unity and puts it first in the build's scene list.</summary>
    public static class SplashBuilder
    {
        public const string SplashPath = "Assets/Scenes/Splash.unity";
        const string GamePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Hotel Merge/Açılış Sahnesini Kur", priority = 5)]
        static void Menu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SplashPath) &&
                !EditorUtility.DisplayDialog("Hotel Merge", "Açılış sahnesi zaten var; baştan kurulsun mu?", "Kur", "Vazgeç"))
                return;
            Run();
        }

        public static void Run()
        {
            string current = EditorSceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var splash = new GameObject("Splash").AddComponent<SplashScreen>();
            splash.Build();
            EditorSceneManager.SaveScene(scene, SplashPath);

            var others = EditorBuildSettings.scenes.Where(s => s.path != SplashPath && s.path != GamePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(SplashPath, true), new EditorBuildSettingsScene(GamePath, true) }
                .Concat(others).ToArray();
            EditorSceneManager.OpenScene(string.IsNullOrEmpty(current) ? GamePath : current);
            Debug.Log("Hotel Merge: açılış sahnesi → " + SplashPath + " (build listesinde ilk sırada)");
        }
    }
}
