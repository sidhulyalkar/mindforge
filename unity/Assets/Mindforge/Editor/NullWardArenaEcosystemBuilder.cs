#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Combat;
using Mindforge.Journey;
using Mindforge.Presentation;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// Expands the Null Ward from two teaching fights into a three-stage enemy ecosystem.
    /// It deliberately reuses the fixed-tick JourneyEnemyController archetypes instead of
    /// introducing parallel combat authority. New units differ through attack profile,
    /// scale, elevation, encounter composition and presentation silhouette.
    /// </summary>
    public static class NullWardArenaEcosystemBuilder
    {
        public const string RootName = "Mindforge_NullWard_ArenaEcosystem_V1";
        private const string ProjectilePrefabPath = "Assets/Mindforge/Generated/Prefabs/MindforgeProjectile.prefab";
        private const string CourtZoneId = "fracture_court";

        [MenuItem("Mindforge/Legacy/Showcase/Apply Null Ward Arena Ecosystem V1", priority = 25)]
        public static void ApplyOpenScene()
        {
            GameObject ward = EditorSceneLookup.FindIncludingInactive(NullWardSceneBuilder.RootName);
            GameObject guardian = EditorSceneLookup.FindIncludingInactive("Guardian");
            if (ward == null || guardian == null)
                throw new InvalidOperationException("Arena ecosystem requires the authored Null Ward and Guardian.");

            NullWardEncounterDirector director = ward.GetComponent<NullWardEncounterDirector>();
            if (director == null)
                throw new InvalidOperationException("Null Ward encounter director is missing.");

            Transform previous = ward.transform.Find(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            MindforgeProjectile projectile = AssetDatabase.LoadAssetAtPath<MindforgeProjectile>(ProjectilePrefabPath);
            if (projectile == null)
                throw new InvalidOperationException("Arena ecosystem requires the generated MindforgeProjectile prefab.");

            CinematicMaterialAuthoring.EnsureAuthored();
            Material obsidian = RequireMaterial("ObsidianArchitecture");
            Material metal = RequireMaterial("GuardianMetal");
            Material hostile = RequireMaterial("FracturedCore");
            Material fractured = RequireMaterial("FracturedRing");

            CombatantVitals playerVitals = guardian.GetComponent<CombatantVitals>();
            GuardianMotor playerMotor = guardian.GetComponent<GuardianMotor>();
            GuardianSwordShieldController defense = guardian.GetComponent<GuardianSwordShieldController>();
            FluxMeter playerFlux = guardian.GetComponent<FluxMeter>();

            GameObject root = new GameObject(RootName);
            root.transform.SetParent(ward.transform, false);

            // Causeway: tiny rushers break the safe rhythm of simply circling the two
            // ranged Sentries. Their low durability rewards quick target switching.
            JourneyEnemyController hollowA = CreateEnemy(
                "Causeway_RiftHollow_A",
                JourneyEnemyArchetype.Hollow,
                root.transform,
                new Vector3(-2.65f, -0.30f, -47.7f),
                guardian.transform,
                playerVitals,
                playerMotor,
                defense,
                projectile,
                playerFlux,
                hostile,
                obsidian,
                30f,
                22f,
                0.66f);
            JourneyEnemyController hollowB = CreateEnemy(
                "Causeway_RiftHollow_B",
                JourneyEnemyArchetype.Hollow,
                root.transform,
                new Vector3(2.60f, -0.30f, -42.2f),
                guardian.transform,
                playerVitals,
                playerMotor,
                defense,
                projectile,
                playerFlux,
                hostile,
                obsidian,
                32f,
                24f,
                0.70f);

            // Market: an elevated caster forces the player to use Pulse, reflected fire,
            // double jump or air dash instead of solving the whole room on one plane.
            JourneyEnemyController marketCaster = CreateEnemy(
                "Market_Shardsinger",
                JourneyEnemyArchetype.Shardcaster,
                root.transform,
                new Vector3(5.35f, 1.35f, -27.2f),
                guardian.transform,
                playerVitals,
                playerMotor,
                defense,
                projectile,
                playerFlux,
                fractured,
                obsidian,
                46f,
                30f,
                0.84f);

            // Fracture Court: a large hybrid Warden anchors the floor while a narrow
            // elevated Needle owns a high lane. The pair intentionally asks the player to
            // change elevation rather than trade into both threat envelopes at once.
            Transform courtTrigger = Marker("FractureCourt_EncounterTrigger", root.transform, new Vector3(0f, 0f, -22.15f));
            JourneyEnemyController warden = CreateEnemy(
                "Court_SignalWarden",
                JourneyEnemyArchetype.SignalWarden,
                root.transform,
                new Vector3(0.75f, -0.30f, -20.25f),
                guardian.transform,
                playerVitals,
                playerMotor,
                defense,
                projectile,
                playerFlux,
                fractured,
                metal,
                138f,
                112f,
                1.38f);
            JourneyEnemyController needle = CreateEnemy(
                "Court_AetherNeedle",
                JourneyEnemyArchetype.Shardcaster,
                root.transform,
                new Vector3(-3.65f, 1.72f, -19.55f),
                guardian.transform,
                playerVitals,
                playerMotor,
                defense,
                projectile,
                playerFlux,
                hostile,
                metal,
                40f,
                24f,
                0.70f);

            NullWardEncounterZone[] current = director.Zones ?? Array.Empty<NullWardEncounterZone>();
            NullWardEncounterZone causeway = FindZone(current, "synapse_causeway");
            NullWardEncounterZone market = FindZone(current, "null_market");
            if (causeway == null || market == null)
                throw new InvalidOperationException("Arena ecosystem could not resolve the base Causeway/Market zones.");

            causeway.enemies = AppendLive(causeway.enemies, hollowA, hollowB);
            causeway.lesson = "Sentries own the long lane while Rift Hollows collapse distance · switch targets or reflect bolts through the rushers";

            market.enemies = AppendLive(market.enemies, marketCaster);
            market.lesson = "Penitent owns the floor, Shardsinger owns height, Echo taxes attention · choose which pressure source dies first";

            NullWardEncounterZone court = new NullWardEncounterZone
            {
                id = CourtZoneId,
                title = "FRACTURE COURT",
                lesson = "The Warden mixes cleave and burst while the Needle owns the high lane · change planes, isolate, collapse",
                activationPoint = courtTrigger,
                activationRadius = 4.15f,
                requiredForProtocol = true,
                enemies = new[] { warden, needle },
                echoes = Array.Empty<FracturedEchoNode>(),
            };

            NullWardEncounterZone[] expanded = ReplaceCourtZone(current, court);
            SetDirectorZones(director, expanded);

            JourneyEnemyController[] allWardEnemies = ward.GetComponentsInChildren<JourneyEnemyController>(true);
            for (int i = 0; i < allWardEnemies.Length; i++)
            {
                JourneyEnemyController enemy = allWardEnemies[i];
                if (enemy == null || enemy.GetComponent<JourneyEnemyIntentVfx>() != null) continue;
                enemy.gameObject.AddComponent<JourneyEnemyIntentVfx>();
            }

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(director);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Mindforge:ArenaEcosystem] Added Rift Hollow rushers, elevated Shardsinger, Fracture Court Signal Warden + Aether Needle, " +
                "and geometric intent telegraphs while preserving JourneyEnemyController fixed-tick authority.");
        }

        private static NullWardEncounterZone FindZone(NullWardEncounterZone[] zones, string id)
        {
            if (zones == null) return null;
            for (int i = 0; i < zones.Length; i++)
                if (zones[i] != null && string.Equals(zones[i].id, id, StringComparison.Ordinal))
                    return zones[i];
            return null;
        }

        private static JourneyEnemyController[] AppendLive(
            JourneyEnemyController[] existing,
            params JourneyEnemyController[] additions)
        {
            int live = 0;
            if (existing != null)
                for (int i = 0; i < existing.Length; i++)
                    if (existing[i] != null) live++;

            int add = 0;
            if (additions != null)
                for (int i = 0; i < additions.Length; i++)
                    if (additions[i] != null) add++;

            JourneyEnemyController[] result = new JourneyEnemyController[live + add];
            int index = 0;
            if (existing != null)
                for (int i = 0; i < existing.Length; i++)
                    if (existing[i] != null) result[index++] = existing[i];
            if (additions != null)
                for (int i = 0; i < additions.Length; i++)
                    if (additions[i] != null) result[index++] = additions[i];
            return result;
        }

        private static NullWardEncounterZone[] ReplaceCourtZone(
            NullWardEncounterZone[] current,
            NullWardEncounterZone court)
        {
            int keep = 0;
            if (current != null)
                for (int i = 0; i < current.Length; i++)
                    if (current[i] != null && !string.Equals(current[i].id, CourtZoneId, StringComparison.Ordinal))
                        keep++;

            NullWardEncounterZone[] result = new NullWardEncounterZone[keep + 1];
            int index = 0;
            if (current != null)
                for (int i = 0; i < current.Length; i++)
                    if (current[i] != null && !string.Equals(current[i].id, CourtZoneId, StringComparison.Ordinal))
                        result[index++] = current[i];
            result[index] = court;
            return result;
        }

        private static void SetDirectorZones(NullWardEncounterDirector director, NullWardEncounterZone[] zones)
        {
            FieldInfo field = typeof(NullWardEncounterDirector).GetField("zones", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new MissingFieldException(typeof(NullWardEncounterDirector).FullName, "zones");
            field.SetValue(director, zones ?? Array.Empty<NullWardEncounterZone>());
        }

        private static JourneyEnemyController CreateEnemy(
            string name,
            JourneyEnemyArchetype archetype,
            Transform parent,
            Vector3 position,
            Transform player,
            CombatantVitals playerVitals,
            GuardianMotor playerMotor,
            GuardianSwordShieldController defense,
            MindforgeProjectile projectile,
            FluxMeter playerFlux,
            Material coreMaterial,
            Material bodyMaterial,
            float health,
            float poise,
            float scale)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.radius = 0.42f * scale;
            collider.height = 1.8f * scale;
            collider.center = Vector3.up * 0.65f * scale;

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezePositionY |
                               RigidbodyConstraints.FreezeRotationX |
                               RigidbodyConstraints.FreezeRotationZ;

            PoiseSystem enemyPoise = root.AddComponent<PoiseSystem>();
            SetFloat(enemyPoise, "maxPoise", poise);
            CombatantVitals vitals = root.AddComponent<CombatantVitals>();
            SetEnum(vitals, "team", (int)CombatTeam.Enemy);
            SetFloat(vitals, "maxHealth", health);
            SetRef(vitals, "poise", enemyPoise);
            SetRef(vitals, "body", body);

            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(root.transform, false);
            Primitive("Body", PrimitiveType.Capsule, visuals.transform,
                Vector3.up * 0.65f * scale,
                new Vector3(0.72f, 0.88f, 0.72f) * scale,
                bodyMaterial,
                false);
            GameObject core = Primitive("Core", PrimitiveType.Sphere, visuals.transform,
                Vector3.up * 1.10f * scale,
                Vector3.one * 0.30f * scale,
                coreMaterial,
                false);
            GameObject ring = CreateLocalRing("TelegraphRing", visuals.transform, 0.82f * scale, coreMaterial);
            ring.transform.localPosition = Vector3.up * 0.05f;

            Color threat = ThreatColor(archetype, name);
            Light coreLight = core.AddComponent<Light>();
            coreLight.type = LightType.Point;
            coreLight.color = threat;
            coreLight.range = Mathf.Clamp(3.0f * scale, 2.0f, 5.2f);
            coreLight.intensity = archetype == JourneyEnemyArchetype.SignalWarden ? 1.85f : 1.20f;
            coreLight.shadows = LightShadows.None;

            Transform origin = Marker("ProjectileOrigin", root.transform, new Vector3(0f, 1.22f * scale, 0.48f * scale));
            JourneyEnemyController controller = root.AddComponent<JourneyEnemyController>();
            controller.ConfigureRuntime(archetype, player, playerVitals, playerMotor, defense, projectile, origin, playerFlux);
            controller.ConfigureCheckpointLifecycle(true);
            controller.Disarm();

            JourneyEnemyPresentation presentation = root.AddComponent<JourneyEnemyPresentation>();
            presentation.ConfigureRuntime(
                controller,
                visuals.transform,
                core.transform,
                ring.transform,
                core.GetComponent<Renderer>(),
                coreLight);
            return controller;
        }

        private static Color ThreatColor(JourneyEnemyArchetype archetype, string name)
        {
            if (!string.IsNullOrEmpty(name) && name.IndexOf("AetherNeedle", StringComparison.OrdinalIgnoreCase) >= 0)
                return new Color(0.98f, 0.12f, 0.62f);
            switch (archetype)
            {
                case JourneyEnemyArchetype.Hollow: return new Color(1.00f, 0.20f, 0.06f);
                case JourneyEnemyArchetype.Shardcaster: return new Color(0.90f, 0.10f, 0.72f);
                case JourneyEnemyArchetype.SignalWarden: return new Color(0.72f, 0.18f, 1.00f);
                case JourneyEnemyArchetype.NullSentry: return new Color(0.95f, 0.08f, 0.34f);
                default: return new Color(1.00f, 0.28f, 0.06f);
            }
        }

        private static GameObject Primitive(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            if (!collider)
            {
                Collider c = go.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.DestroyImmediate(c);
            }
            return go;
        }

        private static GameObject CreateLocalRing(string name, Transform parent, float radius, Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 40;
            line.widthMultiplier = 0.035f;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
            return go;
        }

        private static Transform Marker(string name, Transform parent, Vector3 localPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static Material RequireMaterial(string name)
        {
            Material material = CinematicMaterialAuthoring.Load(name);
            if (material == null)
                throw new InvalidOperationException($"Missing shared cinematic material {name}.");
            return material;
        }

        private static void SetRef(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null) throw new InvalidOperationException($"Missing serialized field {target.GetType().Name}.{property}");
            field.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(UnityEngine.Object target, string property, float value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null) throw new InvalidOperationException($"Missing serialized field {target.GetType().Name}.{property}");
            field.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(UnityEngine.Object target, string property, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null) throw new InvalidOperationException($"Missing serialized field {target.GetType().Name}.{property}");
            field.enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
