#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mindforge.Combat;
using Mindforge.Enemies;
using Mindforge.Journey;
using Mindforge.Presentation;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// Aetheria combat-identity pass applied after Menagerie gameplay/collision/silhouette
    /// authoring. It reuses the existing ten-role roster, makes the Stalker pounce body
    /// movement mechanically honest, and installs presentation-only story identities.
    /// No alternate enemy or boss scheduler is introduced.
    /// </summary>
    public static class AetheriaHordeBossV1Builder
    {
        public const string Revision = "AETHERIA_HORDE_BOSS_V1";
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("Mindforge/Showcase/Apply Aetheria Horde + Malatract V1", priority = 29)]
        public static void ApplyOpenScene()
        {
            GameObject ward = EditorSceneLookup.FindIncludingInactive(NullWardSceneBuilder.RootName);
            if (ward == null) throw new InvalidOperationException("Aetheria Horde V1 requires Null Ward.");

            Transform menagerie = ward.transform.Find(ArenaMenagerieV1Builder.RootName);
            if (menagerie == null) throw new InvalidOperationException("Aetheria Horde V1 requires Arena Menagerie V1 first.");

            JourneyEnemyController[] enemies = menagerie.GetComponentsInChildren<JourneyEnemyController>(true);
            JourneyEnemyController goblin = Find(enemies, "Menagerie_RiftHollow");
            JourneyEnemyController golem = Find(enemies, "Menagerie_SignalWarden");
            JourneyEnemyController gargoyle = Find(enemies, "Menagerie_NullSentry");
            JourneyEnemyController stalker = Find(enemies, "Menagerie_RiftStalker");

            if (goblin == null || golem == null || gargoyle == null || stalker == null)
                throw new InvalidOperationException("Aetheria Horde V1 could not resolve the four required Menagerie roles.");

            ConfigureStoryIdentity(goblin, "Menagerie_ScrapGoblin", AetheriaHordeIdentity.ScrapGoblin);
            ConfigureStoryIdentity(golem, "Menagerie_BassGolem", AetheriaHordeIdentity.BassGolem);
            ConfigureStoryIdentity(gargoyle, "Menagerie_AeroGargoyle", AetheriaHordeIdentity.AeroGargoyle);

            ConfigureStalkerPounce(stalker);
            ConfigureAeroGargoyleDive(gargoyle);
            InstallLordMalatract();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Mindforge:AetheriaHordeV1] Rethemed three existing Menagerie roles as Scrap Goblin, Bass Golem and Aero Gargoyle; " +
                "Stalker pounce now has collision-bounded committed advance; Aero Gargoyle gains a close dive; those post-Menagerie combat " +
                "mutations are re-captured into the serialized role profiles so wave activation cannot erase them. Lord Malatract presentation " +
                "is layered over the existing Fractured Signal boss authority. The roster remains exactly ten identities.");
        }

        private static JourneyEnemyController Find(JourneyEnemyController[] enemies, string name)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                JourneyEnemyController enemy = enemies[i];
                if (enemy != null && string.Equals(enemy.name, name, StringComparison.Ordinal)) return enemy;
            }
            return null;
        }

        private static void ConfigureStoryIdentity(
            JourneyEnemyController enemy,
            string displayName,
            AetheriaHordeIdentity identity)
        {
            if (enemy == null) return;
            enemy.name = displayName;
            AetheriaHordeCharacterPresentationV1 presentation = enemy.GetComponent<AetheriaHordeCharacterPresentationV1>();
            if (presentation == null) presentation = enemy.gameObject.AddComponent<AetheriaHordeCharacterPresentationV1>();
            presentation.Configure(identity);
            EditorUtility.SetDirty(enemy.gameObject);
            EditorUtility.SetDirty(presentation);
        }

        private static void ConfigureStalkerPounce(JourneyEnemyController stalker)
        {
            EnemyAttackDefinition[] attacks = GetAttacks(stalker);
            EnemyAttackDefinition pounce = FindAttack(attacks, "stalker_pounce");
            if (pounce == null) throw new InvalidOperationException("Rift Stalker is missing stalker_pounce.");
            SetAttackAdvance(pounce, 1.62f);
            RebuildCooldownState(stalker);
            RefreshRoleProfile(stalker);
        }

        private static void ConfigureAeroGargoyleDive(JourneyEnemyController gargoyle)
        {
            EnemyAttackDefinition[] source = GetAttacks(gargoyle);
            if (FindAttack(source, "gargoyle_dive") == null)
            {
                EnemyAttackDefinition dive = EnemyAttackDefinition.Create(
                    "gargoyle_dive",
                    EnemyAttackType.Melee,
                    0.55f,
                    4.10f,
                    82f,
                    5,
                    178,
                    60,
                    2,
                    76,
                    0.88f,
                    0.48f,
                    12f,
                    10f,
                    2.2f,
                    0f,
                    1,
                    0f,
                    false,
                    true,
                    "gargoyle_dive",
                    2.05f);

                EnemyAttackDefinition[] expanded = new EnemyAttackDefinition[source.Length + 1];
                Array.Copy(source, expanded, source.Length);
                expanded[expanded.Length - 1] = dive;
                SetField(gargoyle, "attackDefinitions", expanded);
                SetField(gargoyle, "meleeVerticalReach", 2.0f);
            }

            RebuildCooldownState(gargoyle);
            RefreshRoleProfile(gargoyle);
        }

        /// <summary>
        /// ArenaMenagerieDirector intentionally restores a serialized role profile every
        /// time a wave activates because JourneyEnemyController reapplies base archetype
        /// defaults in OnEnable. Aetheria runs after the Menagerie authoring pass, so any
        /// combat mutation made here must be captured again or runtime activation would
        /// restore the earlier profile and silently erase the upgrade.
        /// </summary>
        private static void RefreshRoleProfile(JourneyEnemyController enemy)
        {
            if (enemy == null) return;
            ArenaMenagerieRoleProfile profile = enemy.GetComponent<ArenaMenagerieRoleProfile>();
            if (profile == null) profile = enemy.gameObject.AddComponent<ArenaMenagerieRoleProfile>();
            profile.CaptureFromCurrent(enemy);
            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(enemy);
        }

        private static EnemyAttackDefinition[] GetAttacks(JourneyEnemyController enemy)
        {
            FieldInfo field = typeof(JourneyEnemyController).GetField("attackDefinitions", Fields);
            if (field == null) throw new MissingFieldException(typeof(JourneyEnemyController).FullName, "attackDefinitions");
            return field.GetValue(enemy) as EnemyAttackDefinition[] ?? Array.Empty<EnemyAttackDefinition>();
        }

        private static EnemyAttackDefinition FindAttack(EnemyAttackDefinition[] attacks, string id)
        {
            if (attacks == null) return null;
            for (int i = 0; i < attacks.Length; i++)
            {
                EnemyAttackDefinition attack = attacks[i];
                if (attack != null && string.Equals(attack.Id, id, StringComparison.Ordinal)) return attack;
            }
            return null;
        }

        private static void SetAttackAdvance(EnemyAttackDefinition attack, float value)
        {
            FieldInfo field = typeof(EnemyAttackDefinition).GetField("advanceDistance", Fields);
            if (field == null) throw new MissingFieldException(typeof(EnemyAttackDefinition).FullName, "advanceDistance");
            field.SetValue(attack, Mathf.Clamp(value, 0f, 3.5f));
        }

        private static void SetField<T>(JourneyEnemyController enemy, string fieldName, T value)
        {
            FieldInfo field = typeof(JourneyEnemyController).GetField(fieldName, Fields);
            if (field == null) throw new MissingFieldException(typeof(JourneyEnemyController).FullName, fieldName);
            field.SetValue(enemy, value);
            EditorUtility.SetDirty(enemy);
        }

        private static void RebuildCooldownState(JourneyEnemyController enemy)
        {
            MethodInfo method = typeof(JourneyEnemyController).GetMethod("RebuildCooldownState", Fields);
            if (method == null) throw new MissingMethodException(typeof(JourneyEnemyController).FullName, "RebuildCooldownState");
            method.Invoke(enemy, null);
        }

        private static void InstallLordMalatract()
        {
            FracturedSignalDirector boss = UnityEngine.Object.FindObjectOfType<FracturedSignalDirector>(true);
            if (boss == null) throw new InvalidOperationException("Aetheria Horde V1 requires the Fractured Signal boss authority.");

            // Preserve the legacy boss GameObject name for scene lookup/checkpoint/qualification
            // compatibility. Lord Malatract is a presentation and narrative identity only.
            LordMalatractPresentationV1 presentation = boss.GetComponent<LordMalatractPresentationV1>();
            if (presentation == null) presentation = boss.gameObject.AddComponent<LordMalatractPresentationV1>();
            EditorUtility.SetDirty(boss.gameObject);
            EditorUtility.SetDirty(presentation);
        }
    }
}
#endif