using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace HotelMerge.EditorTools
{
    /// <summary>
    /// "Hotel Merge → Match-3 Test Sahnesini Kur": creates Match3Test.unity, a stand-alone fruit match-3 board for
    /// trying out the next business's game. It is not added to the build's scene list.
    /// </summary>
    public static class Match3Builder
    {
        public const string ScenePath = "Assets/Scenes/Match3Test.unity";

        [MenuItem("Hotel Merge/Match-3 Test Sahnesini Kur", priority = 7)]
        static void Menu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) &&
                !EditorUtility.DisplayDialog("Hotel Merge", "Match-3 test sahnesi zaten var; baştan kurulsun mu?", "Kur", "Vazgeç"))
                return;
            Run();
        }

        public static void Run()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Match3").AddComponent<Match3Screen>().Build();
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Hotel Merge: match-3 test sahnesi → " + ScenePath);
        }
    }
}
