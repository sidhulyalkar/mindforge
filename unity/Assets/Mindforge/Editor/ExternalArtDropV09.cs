#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mindforge.Editor
{
    /// <summary>
    /// Local-only ingestion seam for production art packs. Some excellent free packs permit
    /// use inside a game while restricting redistribution of the raw source assets. Mindforge
    /// therefore keeps this directory git-ignored and discovers user-obtained models at editor
    /// time rather than copying third-party binaries into the public repository.
    ///
    /// The system is deliberately conservative: external objects are presentation-only,
    /// colliders and scripts are disabled/removed, and an imported model may replace a
    /// generated fallback only when an explicit semantic role is requested.
    /// </summary>
    public static class ExternalArtDropV09
    {
        public const string LocalRoot = "Assets/Mindforge/LocalArt";

        public enum Role
        {
            Column,
            Arch,
            Door,
            Spire,
            Tree,
            Rock,
            Prop,
            Humanoid,
            Robot,
        }

        private static readonly Dictionary<Role, string[]> Keywords = new Dictionary<Role, string[]>
        {
            { Role.Column, new[] { "column", "pillar", "support", "columnround" } },
            { Role.Arch, new[] { "arch", "portal", "arcade" } },
            { Role.Door, new[] { "door", "gate", "airlock" } },
            { Role.Spire, new[] { "spire", "tower", "antenna", "beacon" } },
            { Role.Tree, new[] { "tree", "cypress", "birch", "pine", "cherry" } },
            { Role.Rock, new[] { "rock", "boulder", "cliff" } },
            { Role.Prop, new[] { "crate", "bench", "terminal", "screen", "lamp", "hologram" } },
            { Role.Humanoid, new[] { "character", "human", "base", "hero", "knight", "male", "female" } },
            { Role.Robot, new[] { "robot", "bot", "android", "drone", "mech" } },
        };

        [MenuItem("Mindforge/Art/Scan Local Production Packs", priority = 20)]
        public static void ScanAndReport()
        {
            string absolute = Path.GetFullPath(Path.Combine(Application.dataPath, "../", LocalRoot));
            if (!Directory.Exists(absolute))
            {
                Directory.CreateDirectory(absolute);
                AssetDatabase.Refresh();
                Debug.Log(
                    "[Mindforge:V09:ExternalArt] Created Assets/Mindforge/LocalArt. Drop lawfully obtained FBX/glTF/Unity assets here; " +
                    "the directory is intentionally git-ignored so restricted raw packs are never published by accident.");
                return;
            }

            Dictionary<Role, int> counts = new Dictionary<Role, int>();
            foreach (Role role in Enum.GetValues(typeof(Role))) counts[role] = FindCandidates(role).Count;
            Debug.Log(
                "[Mindforge:V09:ExternalArt] Local art scan: " +
                $"columns={counts[Role.Column]}, arches={counts[Role.Arch]}, doors={counts[Role.Door]}, spires={counts[Role.Spire]}, " +
                $"trees={counts[Role.Tree]}, rocks={counts[Role.Rock]}, props={counts[Role.Prop]}, humanoids={counts[Role.Humanoid]}, robots={counts[Role.Robot]}. " +
                "No external object is gameplay authority; all discovered assets remain local-only source art.");
        }

        public static GameObject TryInstantiateBest(
            Role role,
            Transform parent,
            string instanceName,
            Vector3 localPosition,
            Vector3 targetSize,
            Vector3 localEuler)
        {
            List<string> candidates = FindCandidates(role);
            if (candidates.Count == 0) return null;

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(candidates[0]);
            if (source == null) return null;

            GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null) instance = UnityEngine.Object.Instantiate(source);
            if (instance == null) return null;

            instance.name = instanceName;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.Euler(localEuler);
            StripGameplayAuthority(instance);
            FitToSize(instance, targetSize);
            return instance;
        }

        public static List<string> FindCandidates(Role role)
        {
            List<string> results = new List<string>();
            if (!AssetDatabase.IsValidFolder(LocalRoot)) return results;

            string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { LocalRoot });
            string[] keywords = Keywords[role];
            List<(int score, string path)> ranked = new List<(int, string)>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(path)) continue;
                string lower = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                int score = 0;
                for (int k = 0; k < keywords.Length; k++)
                    if (lower.Contains(keywords[k])) score += 20 - Mathf.Min(12, k);
                if (score <= 0) continue;
                if (lower.Contains("lod0")) score += 3;
                if (lower.Contains("collider") || lower.Contains("collision")) score -= 7;
                ranked.Add((score, path));
            }
            ranked.Sort((a, b) => b.score != a.score ? b.score.CompareTo(a.score) : string.CompareOrdinal(a.path, b.path));
            for (int i = 0; i < ranked.Count; i++) results.Add(ranked[i].path);
            return results;
        }

        private static void StripGameplayAuthority(GameObject root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) colliders[i].enabled = false;

            Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
                if (bodies[i] != null) UnityEngine.Object.DestroyImmediate(bodies[i]);

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
                if (behaviours[i] != null) behaviours[i].enabled = false;
        }

        private static void FitToSize(GameObject instance, Vector3 targetSize)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                if (renderers[i] != null) bounds.Encapsulate(renderers[i].bounds);

            Vector3 current = bounds.size;
            float sx = current.x > 0.001f ? targetSize.x / current.x : 1f;
            float sy = current.y > 0.001f ? targetSize.y / current.y : 1f;
            float sz = current.z > 0.001f ? targetSize.z / current.z : 1f;
            float uniform = Mathf.Min(sx, Mathf.Min(sy, sz));
            uniform = Mathf.Clamp(uniform, 0.02f, 100f);
            instance.transform.localScale *= uniform;
        }
    }
}
#endif
