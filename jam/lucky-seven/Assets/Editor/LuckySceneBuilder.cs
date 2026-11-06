using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LuckySeven.Editor
{
    public static class LuckySceneBuilder
    {
        [MenuItem("Tools/Lucky Seven/Create Playable Prototype Scene")]
        public static void BuildScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject game = new GameObject("Lucky Seven - Game");
            game.AddComponent<LuckyGlade>();
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/LuckySeven.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/LuckySeven.unity", true) };
            Selection.activeGameObject = game;
            Debug.Log("Lucky Seven scene created. Press Play for keyboard demo.");
        }
    }
}
