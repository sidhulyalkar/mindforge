#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Mindforge.Traversal;

namespace Mindforge.Editor
{
    /// <summary>
    /// The generic Aetheria landmark helper marks ordinary architecture static. Hoverbike
    /// visuals move with the Guardian, so this deterministic follow-up explicitly clears
    /// every static flag under mount roots. No gameplay state is changed.
    /// </summary>
    public static class AetheriaDynamicMountSafetyBuilder
    {
        public static void ApplyOpenScene()
        {
            GameObject root = EditorSceneLookup.FindIncludingInactive(AetheriaWorldV1Builder.RootName);
            if (root == null)
                throw new InvalidOperationException("Dynamic mount safety requires Aetheria World V1.");

            AetherHoverbikeMount[] bikes = root.GetComponentsInChildren<AetherHoverbikeMount>(true);
            int objects = 0;
            for (int i = 0; i < bikes.Length; i++)
            {
                AetherHoverbikeMount bike = bikes[i];
                if (bike == null) continue;
                Transform[] transforms = bike.GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    Transform t = transforms[j];
                    if (t == null) continue;
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                    EditorUtility.SetDirty(t.gameObject);
                    objects++;
                }
            }

            Debug.Log($"[Mindforge:AetheriaV1] Cleared static flags on {objects} moving hoverbike presentation objects across {bikes.Length} mounts.");
        }
    }
}
#endif
