#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Mindforge.Presentation;

namespace Mindforge.Editor
{
    /// <summary>
    /// Creates/selects the optional zone-level production-art binding profile so room
    /// art can be swapped without editing world-authority scripts or scene wiring.
    /// </summary>
    public static class NullWardArtProfileAuthoring
    {
        private const string ProfilePath = "Assets/Mindforge/Resources/Cinematic/NullWardArtProfile.asset";

        [MenuItem("Mindforge/Legacy/Showcase/Open Null Ward Art Binding Profile", priority = 26)]
        public static void OpenOrCreate()
        {
            CinematicMaterialAuthoring.EnsureAuthored();
            NullWardArtProfile profile = AssetDatabase.LoadAssetAtPath<NullWardArtProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<NullWardArtProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[Mindforge:NullWardArt] Created Resources/Cinematic/NullWardArtProfile.");
            }
            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);
        }
    }
}
#endif
