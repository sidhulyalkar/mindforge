#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mindforge.LivingWorld;

namespace Mindforge.Editor
{
    /// <summary>
    /// Explicit standalone, opt-in test scene; never touches the competition scene.
    /// Generated objects are non-colliding decorative geometry and unlit materials.
    /// </summary>
    public static class LivingWorldDemoSceneBuilder
    {
        private const string ScenePath = "Assets/Mindforge/Scenes/LivingWorldPassiveDemo.unity";
        private const string MaterialPath = "Assets/Mindforge/Generated/LivingWorldDemo/NeuralAmbientUnlit.mat";

        [MenuItem("Mindforge/Living World/Create Passive Demo Scene")]
        public static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!AssetDatabase.IsValidFolder("Assets/Mindforge/Scenes"))
                AssetDatabase.CreateFolder("Assets/Mindforge", "Scenes");
            if (!AssetDatabase.IsValidFolder("Assets/Mindforge/Generated"))
                AssetDatabase.CreateFolder("Assets/Mindforge", "Generated");
            if (!AssetDatabase.IsValidFolder("Assets/Mindforge/Generated/LivingWorldDemo"))
                AssetDatabase.CreateFolder("Assets/Mindforge/Generated", "LivingWorldDemo");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null)
                {
                    Debug.LogError("[LivingWorld] No compatible unlit shader available.");
                    return;
                }
                material = new Material(shader) { name = "LivingWorld_Dedicated_Unlit" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("LivingWorld_Research_Demo_Only");
            LivingWorldPassiveReceiver receiver = root.AddComponent<LivingWorldPassiveReceiver>();
            root.AddComponent<LivingWorldDemoHud>().Configure(receiver);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.027f, 0.055f, 0.09f);
            camera.fieldOfView = 60f;
            cameraObject.transform.position = new Vector3(0f, 5.6f, -17.5f);
            cameraObject.transform.LookAt(new Vector3(0f, 1.8f, 0f));

            // A static grounded floor has no neural integration and no collider.
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "StaticReferencePlatform";
            floor.transform.position = new Vector3(0f, -0.45f, 0f);
            floor.transform.localScale = new Vector3(20f, 0.5f, 13f);
            RemoveCollider(floor);
            MeshRenderer floorRenderer = floor.GetComponent<MeshRenderer>();
            const string floorPath = "Assets/Mindforge/Generated/LivingWorldDemo/StaticFloor.mat";
            Material floorMaterial = AssetDatabase.LoadAssetAtPath<Material>(floorPath);
            if (floorMaterial == null)
            {
                floorMaterial = new Material(material) { name = "LivingWorldStaticFloor" };
                AssetDatabase.CreateAsset(floorMaterial, floorPath);
            }
            if (floorMaterial.HasProperty("_BaseColor"))
                floorMaterial.SetColor("_BaseColor", new Color(0.095f, 0.15f, 0.2f));
            else if (floorMaterial.HasProperty("_Color"))
                floorMaterial.SetColor("_Color", new Color(0.095f, 0.15f, 0.2f));
            EditorUtility.SetDirty(floorMaterial);
            floorRenderer.sharedMaterial = floorMaterial;

            for (int i = 0; i < 14; i++)
            {
                float angle = i * Mathf.PI * 2f / 14f;
                float radius = i % 2 == 0 ? 5f : 7.2f;
                GameObject shard = GameObject.CreatePrimitive(i % 3 == 0 ? PrimitiveType.Cylinder : PrimitiveType.Sphere);
                shard.name = "PassiveAuroraShard_" + i.ToString("00");
                shard.transform.position = new Vector3(
                    Mathf.Cos(angle) * radius, 0.6f + (i % 4) * .45f,
                    Mathf.Sin(angle) * radius + 0.4f);
                shard.transform.localScale = i % 3 == 0
                    ? new Vector3(.65f, 1.8f + (i % 3) * .4f, .65f)
                    : new Vector3(1.4f, 1.8f, 1.4f);
                RemoveCollider(shard);
                shard.GetComponent<Renderer>().sharedMaterial = material;
                shard.AddComponent<LivingWorldReactiveSurface>().Configure(receiver);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                Debug.LogError("[LivingWorld] Failed to save the generated demo scene.");
            else
                Debug.Log("[LivingWorld] Saved " + ScenePath +
                          ". Press Play, then run python tools/mindforge_living_world.py --demo.");
        }

        private static void RemoveCollider(GameObject item)
        {
            Collider collider = item.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
        }
    }
}
#endif
