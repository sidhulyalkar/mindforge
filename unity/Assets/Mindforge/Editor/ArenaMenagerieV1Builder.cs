#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Combat;
using Mindforge.Enemies;
using Mindforge.Journey;
using Mindforge.Presentation;
using Mindforge.World;

namespace Mindforge.Editor
{
    /// <summary>
    /// Authors a dedicated ten-identity demo arena inside the existing safe world basin.
    /// All damage/selection/timing remains JourneyEnemyController authority. This builder
    /// only serializes distinct locomotion/attack data, constructs collision-backed arena
    /// geometry, and wires a fixed-tick 3/3/4 wave scheduler.
    /// </summary>
    public static class ArenaMenagerieV1Builder
    {
        public const string RootName = "Mindforge_ArenaMenagerie_V1";
        private const string ProjectilePrefabPath = "Assets/Mindforge/Generated/Prefabs/MindforgeProjectile.prefab";
        private static readonly Vector3 Center = new Vector3(5.0f, 0f, 18.0f);

        [MenuItem("Mindforge/Showcase/Apply Arena Menagerie V1", priority = 27)]
        public static void ApplyOpenScene()
        {
            GameObject ward = EditorSceneLookup.FindIncludingInactive(NullWardSceneBuilder.RootName);
            GameObject guardian = EditorSceneLookup.FindIncludingInactive("Guardian");
            if (ward == null || guardian == null)
                throw new InvalidOperationException("Arena Menagerie requires the authored Null Ward and Guardian.");

            Transform previous = ward.transform.Find(RootName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            MindforgeProjectile projectile = AssetDatabase.LoadAssetAtPath<MindforgeProjectile>(ProjectilePrefabPath);
            if (projectile == null)
                throw new InvalidOperationException("Arena Menagerie requires the generated MindforgeProjectile prefab.");

            CinematicMaterialAuthoring.EnsureAuthored();
            Material basalt = RequireMaterial("ArenaBasalt");
            Material obsidian = RequireMaterial("ObsidianArchitecture");
            Material metal = RequireMaterial("GuardianMetal");
            Material hostile = RequireMaterial("FracturedCore");
            Material violet = RequireMaterial("FracturedRing");
            Material cyan = RequireMaterial("AetherCyan");
            Material green = RequireMaterial("WispVerdant");

            CombatantVitals playerVitals = guardian.GetComponent<CombatantVitals>();
            GuardianMotor playerMotor = guardian.GetComponent<GuardianMotor>();
            GuardianSwordShieldController defense = guardian.GetComponent<GuardianSwordShieldController>();
            FluxMeter playerFlux = guardian.GetComponent<FluxMeter>();

            GameObject root = new GameObject(RootName);
            root.transform.SetParent(ward.transform, false);
            BuildCrucibleArchitecture(root.transform, basalt, obsidian, metal, cyan, violet);
            Transform trigger = Marker("Menagerie_Activation", root.transform, Center);

            JourneyEnemyController[] roster = new JourneyEnemyController[10];
            roster[0] = CreateRole("Menagerie_RiftHollow", JourneyEnemyArchetype.Hollow, 0, 42f, 26f, 0.76f, hostile, obsidian);
            roster[1] = CreateRole("Menagerie_Shardsinger", JourneyEnemyArchetype.Shardcaster, 1, 48f, 30f, 0.88f, violet, obsidian);
            roster[2] = CreateRole("Menagerie_SignalWarden", JourneyEnemyArchetype.SignalWarden, 2, 150f, 118f, 1.38f, violet, metal);
            roster[3] = CreateRole("Menagerie_NullSentry", JourneyEnemyArchetype.NullSentry, 3, 62f, 38f, 0.92f, hostile, obsidian);
            roster[4] = CreateRole("Menagerie_ChromePenitent", JourneyEnemyArchetype.ChromePenitent, 4, 92f, 76f, 1.12f, hostile, metal);
            roster[5] = CreateRole("Menagerie_RiftStalker", JourneyEnemyArchetype.Hollow, 5, 58f, 34f, 0.82f, green, obsidian);
            roster[6] = CreateRole("Menagerie_ChoirDrone", JourneyEnemyArchetype.NullSentry, 6, 72f, 42f, 0.94f, cyan, obsidian);
            roster[7] = CreateRole("Menagerie_PrismMaw", JourneyEnemyArchetype.Shardcaster, 7, 78f, 52f, 1.02f, violet, metal);
            roster[8] = CreateRole("Menagerie_VeilReaper", JourneyEnemyArchetype.ChromePenitent, 8, 110f, 86f, 1.20f, hostile, obsidian);
            roster[9] = CreateRole("Menagerie_OrbitSeraph", JourneyEnemyArchetype.NullSentry, 9, 86f, 58f, 1.06f, cyan, metal);

            ArenaMenagerieDirector waves = root.AddComponent<ArenaMenagerieDirector>();
            waves.ConfigureRuntime(guardian.transform, trigger, roster, new[] { 3, 3, 4 });

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Mindforge:Menagerie] Authored ten readable enemy identities in a dedicated Crucible: " +
                "Rift Hollow, Shardsinger, Signal Warden, Null Sentry, Chrome Penitent, Rift Stalker, Choir Drone, Prism Maw, Veil Reaper, Orbit Seraph. " +
                "The demo runs deterministic 3/3/4 waves; JourneyEnemyController remains combat authority.");

            JourneyEnemyController CreateRole(
                string name, JourneyEnemyArchetype archetype, int slot, float health, float poise, float scale,
                Material coreMaterial, Material bodyMaterial)
            {
                float angle = slot / 10f * Mathf.PI * 2f;
                Vector3 radial = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                float y = slot == 1 || slot == 6 || slot == 9 ? 1.02f : -0.30f;
                Vector3 position = Center + radial * 6.15f + Vector3.up * y;
                JourneyEnemyController enemy = CreateEnemy(
                    name, archetype, root.transform, position, guardian.transform, playerVitals, playerMotor,
                    defense, projectile, playerFlux, coreMaterial, bodyMaterial, health, poise, scale);
                ApplyRoleProfile(enemy, slot);
                if (enemy.GetComponent<JourneyEnemyIntentVfx>() == null)
                    enemy.gameObject.AddComponent<JourneyEnemyIntentVfx>();
                return enemy;
            }
        }

        private static void ApplyRoleProfile(JourneyEnemyController enemy, int slot)
        {
            if (enemy == null) return;
            EnemyAttackDefinition[] attacks;
            switch (slot)
            {
                case 0: // low rusher: two short melee cadences
                    Tune(enemy, 3.75f, 1.45f, 0.82f, 0.16f, 1.25f, 68);
                    attacks = new[]
                    {
                        A("hollow_snap", EnemyAttackType.Melee, 0.25f, 1.95f, 92f, 9, 82, 30, 2, 48, .34f, .42f, 7f, 5f, 1.1f, 0f, 1, 0f, false, false),
                        A("hollow_hook", EnemyAttackType.Melee, 0.40f, 2.20f, 105f, 4, 138, 55, 2, 66, .18f, .50f, 10f, 8f, 1.55f, 0f, 1, 0f, false, false),
                    };
                    break;
                case 1: // sniper: precise committed lane + narrow fan
                    Tune(enemy, 2.65f, 7.20f, 4.25f, 0.46f, 1.40f, 96);
                    attacks = new[]
                    {
                        A("shardsinger_lance", EnemyAttackType.Projectile, 2.7f, 14.5f, 112f, 8, 142, 76, 1, 62, .78f, .57f, 8f, 3f, 0f, 12.2f, 1, 0f, true, false),
                        A("shardsinger_chord", EnemyAttackType.Burst, 3.2f, 12.5f, 120f, 4, 188, 64, 1, 72, .36f, .46f, 6f, 3f, 0f, 10.2f, 4, 34f, true, false),
                    };
                    break;
                case 2: // anchor: broad heavy punish + ranged answer
                    Tune(enemy, 3.0f, 2.10f, 1.18f, 0.24f, 1.80f, 88);
                    attacks = new[]
                    {
                        A("warden_judgement", EnemyAttackType.Melee, .45f, 2.65f, 100f, 7, 134, 68, 3, 82, .28f, .44f, 16f, 17f, 2.5f, 0f, 1, 0f, false, true),
                        A("warden_triune", EnemyAttackType.Burst, 2.2f, 13.5f, 112f, 5, 176, 82, 1, 76, .64f, .58f, 8f, 5f, 0f, 11.4f, 3, 20f, true, false),
                    };
                    break;
                case 3: // ranged tracker: teaches aim lock
                    Tune(enemy, 2.85f, 7.0f, 3.55f, 0.50f, 1.40f, 82);
                    attacks = new[]
                    {
                        A("sentry_lockbolt", EnemyAttackType.Projectile, 3.0f, 15f, 108f, 7, 148, 66, 1, 58, .82f, .62f, 7.5f, 3f, 0f, 11.0f, 1, 0f, true, false),
                        A("sentry_fan", EnemyAttackType.Burst, 4.0f, 13f, 115f, 5, 196, 74, 1, 70, .44f, .48f, 5.5f, 2.5f, 0f, 9.9f, 3, 28f, true, false),
                        A("sentry_breakaway", EnemyAttackType.Retreat, 0f, 3.3f, 150f, 7, 228, 26, 1, 34, 0f, .30f, 0f, 0f, 2.9f, 0f, 1, 0f, false, false),
                    };
                    break;
                case 4: // melee rhythm test: fast / delayed / sweep
                    Tune(enemy, 3.35f, 1.85f, .92f, .32f, 1.55f, 72);
                    attacks = new[]
                    {
                        A("penitent_quick", EnemyAttackType.Melee, .3f, 2.15f, 86f, 7, 94, 38, 2, 56, .24f, .38f, 9f, 7f, 1.4f, 0f, 1, 0f, false, false),
                        A("penitent_bell", EnemyAttackType.Melee, .45f, 2.45f, 70f, 4, 176, 88, 3, 96, .42f, .48f, 15f, 16f, 2.6f, 0f, 1, 0f, false, true),
                        A("penitent_sweep", EnemyAttackType.Melee, .55f, 2.65f, 124f, 5, 152, 58, 2, 74, .14f, .35f, 11f, 10f, 1.9f, 0f, 1, 0f, false, false),
                    };
                    break;
                case 5: // insectoid pressure: unusually fast close-in, early commitment
                    Tune(enemy, 4.45f, 1.25f, .72f, .36f, 1.30f, 60);
                    attacks = new[]
                    {
                        A("stalker_pounce", EnemyAttackType.Melee, .25f, 2.75f, 76f, 8, 108, 42, 2, 52, .84f, .34f, 9f, 8f, 2.35f, 0f, 1, 0f, false, false),
                        A("stalker_falsebeat", EnemyAttackType.Melee, .35f, 2.35f, 112f, 4, 158, 70, 2, 58, .22f, .42f, 12f, 10f, 1.8f, 0f, 1, 0f, false, true),
                    };
                    break;
                case 6: // hovering choir: broad lane denial + escape pulse
                    Tune(enemy, 2.55f, 7.25f, 4.25f, .58f, 1.30f, 92);
                    attacks = new[]
                    {
                        A("choir_tone", EnemyAttackType.Projectile, 2.5f, 14.5f, 130f, 5, 146, 92, 1, 56, .86f, .68f, 6f, 3f, 0f, 7.6f, 1, 0f, true, false),
                        A("choir_crescendo", EnemyAttackType.Burst, 3.4f, 13.2f, 145f, 8, 210, 78, 1, 78, .26f, .40f, 5f, 2f, 0f, 8.8f, 5, 120f, true, false),
                        A("choir_recoil", EnemyAttackType.Retreat, 0f, 3.5f, 170f, 6, 216, 30, 1, 34, 0f, .25f, 0f, 0f, 3.2f, 0f, 1, 0f, false, false),
                    };
                    break;
                case 7: // squat prism beast: cone zoning + fast needle
                    Tune(enemy, 2.75f, 5.35f, 2.8f, .28f, 1.45f, 78);
                    attacks = new[]
                    {
                        A("prism_maw_cone", EnemyAttackType.Burst, 1.8f, 10.5f, 130f, 7, 164, 62, 1, 66, .34f, .43f, 6.5f, 3f, 0f, 9.6f, 5, 58f, true, false),
                        A("prism_maw_needle", EnemyAttackType.Projectile, 3.0f, 13.5f, 92f, 5, 132, 82, 1, 58, .68f, .50f, 9f, 4f, 0f, 14.0f, 1, 0f, true, false),
                    };
                    break;
                case 8: // tall executioner: extreme timing contrast
                    Tune(enemy, 3.55f, 2.0f, 1.02f, .22f, 1.85f, 76);
                    attacks = new[]
                    {
                        A("reaper_whisper", EnemyAttackType.Melee, .35f, 2.25f, 82f, 7, 102, 34, 2, 54, .32f, .31f, 9f, 8f, 1.5f, 0f, 1, 0f, false, false),
                        A("reaper_toll", EnemyAttackType.Melee, .5f, 2.85f, 74f, 6, 184, 98, 3, 104, .48f, .46f, 17f, 18f, 2.9f, 0f, 1, 0f, false, true),
                        A("reaper_horizon", EnemyAttackType.Melee, .6f, 2.75f, 138f, 4, 158, 64, 2, 78, .12f, .30f, 12f, 11f, 2.1f, 0f, 1, 0f, false, false),
                    };
                    break;
                default: // orbiting angel-machine: very wide fan, then surgical shot
                    Tune(enemy, 2.35f, 7.75f, 4.6f, .62f, 1.35f, 90);
                    attacks = new[]
                    {
                        A("seraph_horizon", EnemyAttackType.Burst, 3.0f, 14.2f, 160f, 8, 206, 88, 1, 80, .20f, .36f, 5.5f, 2.5f, 0f, 9.0f, 5, 180f, true, false),
                        A("seraph_verdict", EnemyAttackType.Projectile, 4.0f, 15f, 102f, 5, 154, 66, 1, 62, .72f, .52f, 9.5f, 4f, 0f, 13.6f, 1, 0f, true, false),
                    };
                    break;
            }
            SetRef(enemy, "attackDefinitions", attacks);
            InvokePrivate(enemy, "RebuildCooldownState");
        }

        private static EnemyAttackDefinition A(
            string id, EnemyAttackType type, float min, float max, float facing, int weight,
            int cooldown, int telegraph, int active, int recovery, float tracking, float lock01,
            float damage, float poise, float knockback, float speed, int count, float spread,
            bool los, bool heavy)
            => EnemyAttackDefinition.Create(id, type, min, max, facing, weight, cooldown, telegraph,
                active, recovery, tracking, lock01, damage, poise, knockback, speed, count, spread,
                los, heavy, id);

        private static void Tune(
            JourneyEnemyController enemy, float moveSpeed, float desiredDistance, float retreatDistance,
            float strafeStrength, float verticalReach, int firstAttackDelay)
        {
            SetFloat(enemy, "moveSpeed", moveSpeed);
            SetFloat(enemy, "desiredDistance", desiredDistance);
            SetFloat(enemy, "retreatDistance", retreatDistance);
            SetFloat(enemy, "strafeStrength", strafeStrength);
            SetFloat(enemy, "meleeVerticalReach", verticalReach);
            SetInt(enemy, "firstAttackDelayTicks", firstAttackDelay);
        }

        private static JourneyEnemyController CreateEnemy(
            string name, JourneyEnemyArchetype archetype, Transform parent, Vector3 position,
            Transform player, CombatantVitals playerVitals, GuardianMotor playerMotor,
            GuardianSwordShieldController defense, MindforgeProjectile projectile, FluxMeter playerFlux,
            Material coreMaterial, Material bodyMaterial, float health, float poise, float scale)
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
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            PoiseSystem enemyPoise = root.AddComponent<PoiseSystem>();
            SetFloat(enemyPoise, "maxPoise", poise);
            CombatantVitals vitals = root.AddComponent<CombatantVitals>();
            SetEnum(vitals, "team", (int)CombatTeam.Enemy);
            SetFloat(vitals, "maxHealth", health);
            SetRef(vitals, "poise", enemyPoise);
            SetRef(vitals, "body", body);

            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(root.transform, false);
            Primitive("Body", PrimitiveType.Capsule, visuals.transform, Vector3.up * 0.65f * scale,
                new Vector3(.72f, .88f, .72f) * scale, bodyMaterial, false);
            GameObject core = Primitive("Core", PrimitiveType.Sphere, visuals.transform, Vector3.up * 1.08f * scale,
                Vector3.one * .30f * scale, coreMaterial, false);
            GameObject ring = CreateLocalRing("TelegraphRing", visuals.transform, .84f * scale, coreMaterial);
            ring.transform.localPosition = Vector3.up * .05f;

            Light coreLight = core.AddComponent<Light>();
            coreLight.type = LightType.Point;
            coreLight.color = ThreatColor(name);
            coreLight.range = Mathf.Clamp(3.2f * scale, 2.2f, 5.4f);
            coreLight.intensity = name.Contains("Warden") || name.Contains("Reaper") ? 1.75f : 1.15f;
            coreLight.shadows = LightShadows.None;

            Transform origin = Marker("ProjectileOrigin", root.transform, new Vector3(0f, 1.22f * scale, .50f * scale));
            JourneyEnemyController controller = root.AddComponent<JourneyEnemyController>();
            controller.ConfigureRuntime(archetype, player, playerVitals, playerMotor, defense, projectile, origin, playerFlux);
            controller.ConfigureCheckpointLifecycle(true);
            controller.Disarm();

            JourneyEnemyPresentation presentation = root.AddComponent<JourneyEnemyPresentation>();
            presentation.ConfigureRuntime(controller, visuals.transform, core.transform, ring.transform, core.GetComponent<Renderer>(), coreLight);
            return controller;
        }

        private static void BuildCrucibleArchitecture(
            Transform parent, Material basalt, Material obsidian, Material metal, Material cyan, Material violet)
        {
            Transform zone = Marker("District_MenagerieCrucible", parent, Vector3.zero);
            Primitive("Crucible_Center", PrimitiveType.Cylinder, zone, Center + Vector3.up * .18f,
                new Vector3(5.9f, .18f, 5.9f), basalt, true);
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                Vector3 radial = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                Vector3 p = Center + radial * 7.1f + Vector3.up * (.25f + (i % 2) * .25f);
                Primitive($"Crucible_Platform_{i:00}", PrimitiveType.Cube, zone, p,
                    new Vector3(2.7f, .42f, 2.2f), i % 2 == 0 ? obsidian : metal, true,
                    new Vector3(0f, a * Mathf.Rad2Deg, 0f));
                CreateSignalPillar($"Crucible_Beacon_{i:00}", zone, p + radial * 1.2f + Vector3.up * 1.65f,
                    i % 2 == 0 ? cyan : violet, metal);
            }

            for (int i = 0; i < 4; i++)
            {
                float a = (i * 90f + 45f) * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                Vector3 p = Center + radial * 9.0f + Vector3.up * 3.2f;
                Primitive($"Crucible_ArchPylon_{i}", PrimitiveType.Cube, zone, p,
                    new Vector3(1.1f, 6.4f, 1.1f), obsidian, true,
                    new Vector3(i % 2 == 0 ? 4f : -4f, -a * Mathf.Rad2Deg, 0f));
            }

            CreateLocalRing("Crucible_OuterSignalRing", zone, Center + Vector3.up * .48f, 8.25f, violet, .055f);
            CreateLocalRing("Crucible_InnerSignalRing", zone, Center + Vector3.up * .46f, 4.45f, cyan, .035f);
        }

        private static void CreateSignalPillar(string name, Transform parent, Vector3 position, Material signal, Material metal)
        {
            Primitive(name + "_Stem", PrimitiveType.Cube, parent, position,
                new Vector3(.18f, 2.8f, .18f), metal, false);
            Primitive(name + "_Rune", PrimitiveType.Cube, parent, position + new Vector3(0f, .2f, .12f),
                new Vector3(.055f, 1.65f, .035f), signal, false);
        }

        private static Color ThreatColor(string name)
        {
            if (name.Contains("Stalker")) return new Color(.28f, 1f, .48f);
            if (name.Contains("Choir")) return new Color(.18f, .82f, 1f);
            if (name.Contains("Prism")) return new Color(.82f, .24f, 1f);
            if (name.Contains("Reaper")) return new Color(1f, .10f, .22f);
            if (name.Contains("Seraph")) return new Color(.55f, .92f, 1f);
            return new Color(1f, .18f, .34f);
        }

        private static GameObject Primitive(
            string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale,
            Material material, bool collider, Vector3? euler = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = collider ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = collider;
            }
            Collider c = go.GetComponent<Collider>();
            if (!collider && c != null) UnityEngine.Object.DestroyImmediate(c);
            return go;
        }

        private static GameObject CreateLocalRing(string name, Transform parent, float radius, Material material)
            => CreateLocalRing(name, parent, Vector3.zero, radius, material, .045f);

        private static GameObject CreateLocalRing(string name, Transform parent, Vector3 position, float radius, Material material, float width)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 49;
            line.widthMultiplier = width;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (int i = 0; i < 49; i++)
            {
                float a = i / 48f * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, .03f, Mathf.Sin(a) * radius));
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
            if (material == null) throw new InvalidOperationException($"Missing authored material '{name}'.");
            return material;
        }

        private static void SetFloat(object target, string field, float value) => SetField(target, field, value);
        private static void SetInt(object target, string field, int value) => SetField(target, field, value);
        private static void SetRef(object target, string field, object value) => SetField(target, field, value);
        private static void SetEnum(object target, string field, int value)
        {
            FieldInfo f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null) throw new MissingFieldException(target.GetType().FullName, field);
            f.SetValue(target, Enum.ToObject(f.FieldType, value));
        }
        private static void SetField(object target, string field, object value)
        {
            FieldInfo f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null) throw new MissingFieldException(target.GetType().FullName, field);
            f.SetValue(target, value);
        }
        private static void InvokePrivate(object target, string method)
        {
            MethodInfo m = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            if (m == null) throw new MissingMethodException(target.GetType().FullName, method);
            m.Invoke(target, null);
        }
    }
}
#endif
