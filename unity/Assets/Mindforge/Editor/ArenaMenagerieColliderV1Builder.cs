#if UNITY_EDITOR
using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mindforge.Journey;

namespace Mindforge.Editor
{
    /// <summary>
    /// Tunes only the single authoritative root CapsuleCollider on Menagerie enemies.
    /// Decorative silhouette geometry remains collider-free. Profiles intentionally fit the
    /// creature's main body mass rather than every blade/mandible/halo, keeping sword and
    /// projectile contact predictable while avoiding tall invisible capsules on low beasts.
    /// </summary>
    public static class ArenaMenagerieColliderV1Builder
    {
        public static void ApplyOpenScene()
        {
            GameObject ward = EditorSceneLookup.FindIncludingInactive(NullWardSceneBuilder.RootName);
            if (ward == null) throw new InvalidOperationException("Menagerie collision requires Null Ward.");
            Transform menagerie = ward.transform.Find(ArenaMenagerieV1Builder.RootName);
            if (menagerie == null) throw new InvalidOperationException("Menagerie collision requires Arena Menagerie V1 first.");

            JourneyEnemyController[] enemies = menagerie.GetComponentsInChildren<JourneyEnemyController>(true);
            int tuned = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                JourneyEnemyController enemy = enemies[i];
                if (enemy == null || !enemy.name.StartsWith("Menagerie_", StringComparison.Ordinal)) continue;
                CapsuleCollider collider = enemy.GetComponent<CapsuleCollider>();
                Transform core = enemy.transform.Find("Visuals/Core");
                if (collider == null || core == null) continue;

                float scale = Mathf.Clamp(core.localScale.x / 0.30f, 0.50f, 1.80f);
                ResolveProfile(enemy.name, out float radius, out float height, out float centerY);
                collider.direction = 1;
                collider.radius = radius * scale;
                collider.height = Mathf.Max(collider.radius * 2.05f, height * scale);
                collider.center = Vector3.up * centerY * scale;
                tuned++;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[Mindforge:MenagerieCollision] Tuned {tuned} single root capsules to the ten creature body plans.");
        }

        private static void ResolveProfile(string name, out float radius, out float height, out float centerY)
        {
            if (name.Contains("RiftHollow"))
            {
                radius = 0.46f; height = 1.02f; centerY = 0.43f; return;
            }
            if (name.Contains("Shardsinger"))
            {
                radius = 0.42f; height = 1.82f; centerY = 0.82f; return;
            }
            if (name.Contains("SignalWarden"))
            {
                radius = 0.62f; height = 2.22f; centerY = 0.92f; return;
            }
            if (name.Contains("NullSentry"))
            {
                radius = 0.44f; height = 1.48f; centerY = 0.70f; return;
            }
            if (name.Contains("ChromePenitent"))
            {
                radius = 0.54f; height = 2.02f; centerY = 0.82f; return;
            }
            if (name.Contains("RiftStalker"))
            {
                radius = 0.50f; height = 1.05f; centerY = 0.45f; return;
            }
            if (name.Contains("ChoirDrone"))
            {
                radius = 0.53f; height = 1.46f; centerY = 0.82f; return;
            }
            if (name.Contains("PrismMaw"))
            {
                radius = 0.56f; height = 1.20f; centerY = 0.55f; return;
            }
            if (name.Contains("VeilReaper"))
            {
                radius = 0.50f; height = 2.30f; centerY = 1.02f; return;
            }
            if (name.Contains("OrbitSeraph"))
            {
                radius = 0.64f; height = 1.42f; centerY = 0.82f; return;
            }

            radius = 0.44f;
            height = 1.80f;
            centerY = 0.68f;
        }
    }
}
#endif
