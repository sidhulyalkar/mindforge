#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Mindforge.Presentation;

namespace Mindforge.Editor
{
    public static class CinematicArtProfileAuthoring
    {
        public const string ProfilePath = "Assets/Mindforge/Resources/Cinematic/MindforgeArtProfile.asset";

        [MenuItem("Mindforge/Legacy/Showcase/Open Production Art Binding Profile", priority = 23)]
        public static void OpenOrCreate()
        {
            CinematicMaterialAuthoring.EnsureAuthored();
            CinematicArtProfile profile = AssetDatabase.LoadAssetAtPath<CinematicArtProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<CinematicArtProfile>();
                profile.name = "MindforgeArtProfile";
                AssetDatabase.CreateAsset(profile, ProfilePath);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);
            Debug.Log("[Mindforge:Cinematic] Production art profile selected. Assign Guardian, Fractured Signal and/or arena set-dress prefabs; gameplay authority stays on the existing scene objects.");
        }
    }
}
#endif
