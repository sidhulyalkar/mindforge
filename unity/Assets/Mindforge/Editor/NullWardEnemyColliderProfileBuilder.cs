#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mindforge.Journey;

namespace Mindforge.Editor
{
    /// <summary>
    /// Honest collision profiles for the arena-ecosystem enemies. This pass edits only
    /// the already-authoritative root CapsuleCollider created by the ecosystem builder;
    /// presentation geometry remains collider-free. Sizes are derived from the same
    /// archetype/variant vocabulary as silhouettes so visual mass matches sword/projectile
    /// contact without introducing a second hit-detection system.
    /// </summary>
    public static class NullWardEnemyColliderProfileBuilder
    {
        public static void ApplyOpenScene()
        {
            GameObject ward = EditorSceneLookup.FindIncludingInactive(NullWardSceneBuilder.RootName);
            if (ward == null)
                throw new InvalidOperationException("Enemy collider profiles require the Null Ward scene root.");

            Transform ecosystem = ward.transform.Find(NullWardArenaEcosystemBuilder.RootName);
            if (ecosystem == null)
                throw new InvalidOperationException("Enemy collider profiles require Arena Ecosystem V1 first.");

            JourneyEnemyController[] enemies = ecosystem.GetComponentsInChildren<JourneyEnemyController>(true);
            int tuned = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                JourneyEnemyController enemy = enemies[i];
                if (enemy == null) continue;
                CapsuleCollider collider = enemy.GetComponent<CapsuleCollider>();
                Transform core = enemy.transform.Find("Visuals/Core");
                if (collider == null || core == null) continue;

                float scale = Mathf.Clamp(core.localScale.x / 0.30f, 0.50f, 1.80f);
                ResolveProfile(enemy, out float radius, out float height, out float centerY);
                collider.radius = radius * scale;
                collider.height = Mathf.Max(collider.radius * 2.05f, height * scale);
                collider.center = Vector3.up * centerY * scale;
                tuned++;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[Mindforge:EnemyCollision] Tuned {tuned} ecosystem root colliders to match visible archetype mass.");
        }

        private static void ResolveProfile(
            JourneyEnemyController enemy,
            out float radius,
            out float height,
            out float centerY)
        {
            bool needle = enemy != null &&
                          enemy.name.IndexOf("AetherNeedle", StringComparison.OrdinalIgnoreCase) >= 0;

            switch (enemy != null ? enemy.Archetype : JourneyEnemyArchetype.Hollow)
            {
                case JourneyEnemyArchetype.Hollow:
                    radius = 0.38f;
                    height = 1.18f;
                    centerY = 0.46f;
                    return;
                case JourneyEnemyArchetype.Shardcaster:
                    radius = needle ? 0.34f : 0.46f;
                    height = needle ? 1.92f : 1.62f;
                    centerY = needle ? 0.82f : 0.68f;
                    return;
                case JourneyEnemyArchetype.SignalWarden:
                    radius = 0.61f;
                    height = 2.12f;
                    centerY = 0.82f;
                    return;
                case JourneyEnemyArchetype.NullSentry:
                    radius = 0.42f;
                    height = 1.82f;
                    centerY = 0.67f;
                    return;
                case JourneyEnemyArchetype.ChromePenitent:
                    radius = 0.53f;
                    height = 1.86f;
                    centerY = 0.68f;
                    return;
                default:
                    radius = 0.42f;
                    height = 1.80f;
                    centerY = 0.65f;
                    return;
            }
        }
    }
}
#endif
