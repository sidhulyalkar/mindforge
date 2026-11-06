using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using System;
using System.IO;
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

        [MenuItem("Tools/Lucky Seven/Build WebGL Demo")]
        public static void BuildWebGL()
        {
            if (!File.Exists("Assets/Scenes/LuckySeven.unity")) BuildScene();
            Directory.CreateDirectory("Build");
            var settings = new BuildPlayerOptions {
                scenes = new[] { "Assets/Scenes/LuckySeven.unity" },
                locationPathName = "Build/WebGL",
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(settings);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("WebGL build failed: " + report.summary.result);
            Debug.Log("WebGL demo built at Build/WebGL");
        }
    }
}
