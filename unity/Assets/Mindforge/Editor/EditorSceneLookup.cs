#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mindforge.Editor
{
    /// <summary>
    /// Scene-local editor lookup that includes inactive objects.
    ///
    /// GameObject.Find intentionally ignores inactive GameObjects. Mindforge keeps the
    /// combat arena inactive until calibration/controller-only qualification opens it,
    /// so editor authoring passes must traverse the active scene hierarchy directly.
    /// This helper never searches assets/prefabs or objects from another scene.
    /// </summary>
    internal static class EditorSceneLookup
    {
        public static GameObject FindIncludingInactive(string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return null;

            Scene scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) return null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == objectName) return root;

                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform candidate in transforms)
                {
                    if (candidate == null || candidate == root.transform) continue;
                    if (candidate.gameObject.scene != scene) continue;
                    if (candidate.name == objectName) return candidate.gameObject;
                }
            }

            return null;
        }
    }
}
#endif
