using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Mindforge.Calibration;
using Mindforge.Combat;
using Mindforge.SoulWisp;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Deterministic presentation layer for the competition scene.
    ///
    /// Everything created here is presentation-only: decorative primitives have their
    /// colliders disabled before destruction, ambient animation freezes during every
    /// calibration or resonance epoch, and no object modifies VepAuraStimulus timing,
    /// luminance, retinal geometry, or gameplay authority.
    /// </summary>
    public sealed class MindforgeDemoEnvironmentV15 : MonoBehaviour
    {
        private const string CompetitionSceneName = "Mindforge_Competition";
        private const string RootName = "Mindforge_Demo_Environment_V15";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.name, CompetitionSceneName, StringComparison.Ordinal))
                return;
            if (FindSceneObject(RootName) != null) return;

            GameObject awakening = FindSceneObject("The_Awakening");
            GameObject arena = FindSceneObject("Fractured_Signal_Arena");
            GameObject guardian = FindSceneObject("Guardian");
            GameObject wisp = FindSceneObject("SoulWispRoot");
            GameObject boss = FindSceneObject("The_Fractured_Signal");
            Camera camera = Camera.main;
            AwakeningCalibrationDirector calibration = UnityEngine.Object.FindObjectOfType<AwakeningCalibrationDirector>(true);
            GuardianCombatInput input = UnityEngine.Object.FindObjectOfType<GuardianCombatInput>(true);
            FracturedSignalDirector bossDirector = UnityEngine.Object.FindObjectOfType<FracturedSignalDirector>(true);
            SoulWispController wispController = UnityEngine.Object.FindObjectOfType<SoulWispController>(true);

            if (awakening == null || arena == null || guardian == null || wisp == null || boss == null ||
                camera == null || calibration == null || input == null || bossDirector == null || wispController == null)
            {
                Debug.LogError("[Mindforge:DemoV15] Competition scene is missing a required authority object. Presentation bootstrap aborted; intro gate remains closed.");
                calibration?.SetIntroReady(false);
                return;
            }

            GameObject root = new GameObject(RootName);
            MindforgeDemoEnvironmentV15 environment = root.AddComponent<MindforgeDemoEnvironmentV15>();
            environment.Build(awakening, arena, guardian, wisp, boss, camera, calibration, input, bossDirector, wispController);
        }

        private void Build(
            GameObject awakening,
            GameObject arena,
            GameObject guardian,
            GameObject wisp,
            GameObject boss,
            Camera camera,
            AwakeningCalibrationDirector calibration,
            GuardianCombatInput input,
            FracturedSignalDirector bossDirector,
            SoulWispController wispController)
        {
            Material ivory = CreateLit("V15_Ivory", new Color(0.62f, 0.69f, 0.78f), 0.55f, 0.82f);
            Material pearl = CreateLit("V15_Pearl", new Color(0.25f, 0.30f, 0.38f), 0.42f, 0.74f);
            Material obsidian = CreateLit("V15_Obsidian", new Color(0.018f, 0.024f, 0.040f), 0.68f, 0.76f);
            Material gold = CreateLit("V15_Gold", new Color(0.42f, 0.29f, 0.08f), 0.90f, 0.86f);
            Material cyan = CreateEmission("V15_Cyan", new Color(0.10f, 0.62f, 1.00f), 2.6f);
            Material verdant = CreateEmission("V15_Verdant", new Color(0.10f, 1.00f, 0.52f), 2.25f);
            Material violet = CreateEmission("V15_Violet", new Color(0.53f, 0.16f, 1.00f), 2.5f);
            Material hostile = CreateEmission("V15_Hostile", new Color(1.00f, 0.11f, 0.16f), 2.8f);

            List<Transform> rotators = new List<Transform>();
            List<Light> accentLights = new List<Light>();

            BuildAwakening(awakening.transform, ivory, pearl, obsidian, gold, cyan, verdant, rotators, accentLights);
            BuildArena(arena.transform, ivory, obsidian, gold, cyan, violet, hostile, rotators, accentLights);
            BuildGuardian(guardian.transform, ivory, pearl, obsidian, cyan);
            BuildBoss(boss.transform, obsidian, violet, hostile, rotators);
            BuildWisp(wisp.transform, gold, cyan, verdant, rotators);
            ConfigureLighting(awakening.transform, arena.transform, accentLights);
            ConfigureAtmosphere(camera);

            CanvasGroup researchHud = FindSceneObject("NeuralEvidenceHud")?.GetComponent<CanvasGroup>();
            Text calibrationStatus = FindSceneObject("CalibrationStatus")?.GetComponent<Text>();

            CanvasGroup titleOverlay;
            CanvasGroup instructionPanel;
            CanvasGroup controlRibbon;
            Text titleText;
            Text subtitleText;
            Text instructionText;
            Text phaseText;
            Text controlsText;
            BuildDemoHud(out titleOverlay, out instructionPanel, out controlRibbon,
                out titleText, out subtitleText, out instructionText, out phaseText, out controlsText);

            Transform cameraRig = camera.transform.root;
            Transform introWidePose = CreatePose("IntroWidePose", new Vector3(-8.8f, 4.8f, -10.5f), new Vector3(0f, 1.2f, 1.8f));
            Transform wispPose = CreatePose("WispApproachPose", new Vector3(-2.8f, 3.35f, -7.2f), new Vector3(0f, 1.05f, 1.2f));
            Transform calibrationPose = CreatePose("CalibrationPose", new Vector3(0f, 4.8f, -9.8f), new Vector3(0f, 1.05f, 1.6f));
            Transform arenaRevealPose = CreatePose("ArenaRevealPose", new Vector3(8.6f, 7.4f, -11.8f), new Vector3(0f, 1.0f, 3.4f));
            Transform gameplayPose = CreatePose("GameplayPose", new Vector3(0f, 15f, -13f), new Vector3(0f, 0.5f, 1f));

            MindforgeDemoIntroDirector intro = gameObject.AddComponent<MindforgeDemoIntroDirector>();
            intro.Configure(camera, cameraRig, calibration, input, bossDirector,
                titleOverlay, instructionPanel, controlRibbon, researchHud,
                titleText, subtitleText, instructionText, phaseText, controlsText, calibrationStatus,
                introWidePose, wispPose, calibrationPose, arenaRevealPose, gameplayPose);

            NeuralQuietAmbientMotionV15 ambient = gameObject.AddComponent<NeuralQuietAmbientMotionV15>();
            ambient.Configure(calibration, wispController, rotators.ToArray(), accentLights.ToArray());

            Debug.Log("[Mindforge:DemoV15] Cinematic competition environment installed. Decorative motion freezes for baseline, calibration and every neural decision window.");
        }

        private static void BuildAwakening(
            Transform parent,
            Material ivory,
            Material pearl,
            Material obsidian,
            Material gold,
            Material cyan,
            Material verdant,
            List<Transform> rotators,
            List<Light> lights)
        {
            GameObject root = new GameObject("AwakeningVisualV15");
            root.transform.SetParent(parent, false);

            DecorativePrimitive("SanctumPlinth", PrimitiveType.Cylinder, root.transform,
                new Vector3(0f, -0.22f, 1.2f), new Vector3(5.4f, 0.11f, 5.4f), obsidian);
            DecorativePrimitive("SanctumInnerDisc", PrimitiveType.Cylinder, root.transform,
                new Vector3(0f, -0.08f, 1.2f), new Vector3(3.75f, 0.045f, 3.75f), pearl);
            DecorativePrimitive("WispDais", PrimitiveType.Cylinder, root.transform,
                new Vector3(0f, 0.03f, 1.4f), new Vector3(1.25f, 0.12f, 1.25f), gold);

            for (int i = 0; i < 16; i++)
            {
                float angle = i / 16f * Mathf.PI * 2f;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                GameObject inlay = DecorativePrimitive($"SanctumInlay_{i:00}", PrimitiveType.Cube, root.transform,
                    new Vector3(0f, -0.015f, 1.2f) + radial * 3.05f,
                    new Vector3(0.055f, 0.012f, 1.05f), i % 4 == 0 ? cyan : gold);
                inlay.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
            }

            for (int i = 0; i < 10; i++)
            {
                float angle = i / 10f * Mathf.PI * 2f;
                float radius = 6.2f;
                float height = i % 2 == 0 ? 6.4f : 5.4f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, height * 0.5f - 0.3f,
                    1.2f + Mathf.Sin(angle) * radius);
                GameObject rib = DecorativePrimitive($"SanctumRib_{i:00}", PrimitiveType.Cube, root.transform,
                    position, new Vector3(0.26f, height, 0.72f), i % 3 == 0 ? pearl : ivory);
                rib.transform.rotation = Quaternion.Euler(i % 2 == 0 ? -4f : 3f, -angle * Mathf.Rad2Deg + 90f, i % 2 == 0 ? 4f : -3f);
            }

            Vector3[] pylonPositions =
            {
                new Vector3(-3.8f, 1.55f, -1.2f), new Vector3(3.8f, 1.55f, -1.2f),
                new Vector3(-4.2f, 1.55f, 4.0f), new Vector3(4.2f, 1.55f, 4.0f),
            };
            for (int i = 0; i < pylonPositions.Length; i++)
            {
                DecorativePrimitive($"SignalPylonBody_{i:00}", PrimitiveType.Cylinder, root.transform,
                    pylonPositions[i], new Vector3(0.18f, 1.55f, 0.18f), pearl);
                DecorativePrimitive($"SignalPylonCore_{i:00}", PrimitiveType.Cylinder, root.transform,
                    pylonPositions[i] + Vector3.up * 0.35f, new Vector3(0.06f, 1.25f, 0.06f), i < 2 ? cyan : verdant);
            }

            Transform lowRing = CreateCircleLine("SanctumSignalRingLow", root.transform, 2.15f, 72, cyan, 0.022f);
            lowRing.localPosition = new Vector3(0f, 0.30f, 1.4f);
            lowRing.localRotation = Quaternion.Euler(90f, 0f, 0f);
            rotators.Add(lowRing);

            Transform highRing = CreateCircleLine("SanctumSignalRingHigh", root.transform, 1.15f, 64, gold, 0.018f);
            highRing.localPosition = new Vector3(0f, 2.35f, 2.75f);
            highRing.localRotation = Quaternion.Euler(0f, 0f, 0f);
            rotators.Add(highRing);

            Transform portalRing = CreateCircleLine("SanctumPortalRing", root.transform, 2.55f, 80, cyan, 0.035f);
            portalRing.localPosition = new Vector3(0f, 2.5f, 5.45f);

            Light fill = CreateAccentLight("AwakeningCyanFill", parent, new Vector3(0f, 4.6f, -1.4f), cyan.color, 1.25f, 12f);
            lights.Add(fill);
        }

        private static void BuildArena(
            Transform parent,
            Material ivory,
            Material obsidian,
            Material gold,
            Material cyan,
            Material violet,
            Material hostile,
            List<Transform> rotators,
            List<Light> lights)
        {
            GameObject root = new GameObject("ArenaVisualV15");
            root.transform.SetParent(parent, false);

            DecorativePrimitive("ArenaObsidianDais", PrimitiveType.Cylinder, root.transform,
                new Vector3(0f, -0.21f, 1f), new Vector3(8.9f, 0.12f, 8.9f), obsidian);
            DecorativePrimitive("ArenaCombatDisc", PrimitiveType.Cylinder, root.transform,
                new Vector3(0f, -0.07f, 1f), new Vector3(7.1f, 0.045f, 7.1f), CreateLit("V15_ArenaDisc", new Color(0.045f, 0.055f, 0.075f), 0.58f, 0.74f));

            for (int i = 0; i < 20; i++)
            {
                float angle = i / 20f * Mathf.PI * 2f;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                GameObject seam = DecorativePrimitive($"ArenaRune_{i:00}", PrimitiveType.Cube, root.transform,
                    new Vector3(0f, -0.008f, 1f) + radial * 6.35f,
                    new Vector3(0.035f, 0.012f, 0.82f), i % 5 == 0 ? cyan : violet);
                seam.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
            }

            System.Random random = new System.Random(15015);
            for (int i = 0; i < 14; i++)
            {
                float angle = i / 14f * Mathf.PI * 2f + 0.17f;
                float radius = i % 2 == 0 ? 10.7f : 11.7f;
                float height = Mathf.Lerp(4.4f, 8.6f, (float)random.NextDouble());
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, height * 0.5f - 0.45f,
                    1f + Mathf.Sin(angle) * radius);
                GameObject spire = DecorativePrimitive($"FractureSpire_{i:00}", PrimitiveType.Cube, root.transform,
                    position,
                    new Vector3(Mathf.Lerp(0.28f, 0.60f, (float)random.NextDouble()), height,
                        Mathf.Lerp(0.48f, 1.0f, (float)random.NextDouble())),
                    i % 4 == 0 ? ivory : obsidian);
                spire.transform.rotation = Quaternion.Euler(
                    Mathf.Lerp(-7f, 7f, (float)random.NextDouble()),
                    -angle * Mathf.Rad2Deg + 90f,
                    Mathf.Lerp(-9f, 9f, (float)random.NextDouble()));

                if (i % 3 == 0)
                {
                    GameObject seam = DecorativePrimitive($"FractureSpireSeam_{i:00}", PrimitiveType.Cube, root.transform,
                        position + Vector3.up * 0.4f, new Vector3(0.035f, height * 0.68f, 0.035f), violet);
                    seam.transform.rotation = spire.transform.rotation;
                }
            }

            for (int i = 0; i < 26; i++)
            {
                float angle = (float)(random.NextDouble() * Math.PI * 2.0);
                float radius = Mathf.Lerp(8.5f, 11.2f, (float)random.NextDouble());
                float size = Mathf.Lerp(0.18f, 0.60f, (float)random.NextDouble());
                GameObject shard = DecorativePrimitive($"ArenaShard_{i:00}", i % 3 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube,
                    root.transform,
                    new Vector3(Mathf.Cos(angle) * radius, size * 0.28f, 1f + Mathf.Sin(angle) * radius),
                    new Vector3(size * 1.3f, size * 0.62f, size), i % 7 == 0 ? gold : obsidian);
                shard.transform.rotation = Quaternion.Euler(
                    Mathf.Lerp(-25f, 25f, (float)random.NextDouble()),
                    Mathf.Lerp(0f, 360f, (float)random.NextDouble()),
                    Mathf.Lerp(-25f, 25f, (float)random.NextDouble()));
            }

            Transform outerRing = CreateCircleLine("ArenaOuterSignalRing", root.transform, 8.05f, 96, violet, 0.026f);
            outerRing.localPosition = new Vector3(0f, 0.22f, 1f);
            outerRing.localRotation = Quaternion.Euler(90f, 0f, 0f);
            rotators.Add(outerRing);

            Transform crown = CreateCircleLine("ArenaFractureCrown", root.transform, 3.3f, 84, hostile, 0.025f);
            crown.localPosition = new Vector3(0f, 4.8f, 7.8f);
            rotators.Add(crown);

            Light violetRim = CreateAccentLight("ArenaVioletRim", parent, new Vector3(7.8f, 5.2f, 5.8f), violet.color, 1.55f, 16f);
            Light cyanRim = CreateAccentLight("ArenaCyanRim", parent, new Vector3(-7.2f, 4.6f, -3.8f), cyan.color, 1.15f, 15f);
            lights.Add(violetRim);
            lights.Add(cyanRim);
        }

        private static void BuildGuardian(Transform guardian, Material ivory, Material pearl, Material obsidian, Material cyan)
        {
            Renderer placeholder = guardian.GetComponent<Renderer>();
            if (placeholder != null) placeholder.enabled = false;
            GameObject root = new GameObject("GuardianVisualV15");
            root.transform.SetParent(guardian, false);

            DecorativePrimitive("GuardianTorso", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.82f, 0f), new Vector3(0.68f, 0.86f, 0.42f), ivory);
            DecorativePrimitive("GuardianCore", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.86f, 0.225f), new Vector3(0.22f, 0.30f, 0.035f), cyan);
            DecorativePrimitive("GuardianWaist", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.35f, 0f), new Vector3(0.50f, 0.24f, 0.36f), obsidian);
            DecorativePrimitive("GuardianHead", PrimitiveType.Sphere, root.transform, new Vector3(0f, 1.47f, 0f), Vector3.one * 0.34f, pearl);
            DecorativePrimitive("GuardianVisor", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.50f, 0.18f), new Vector3(0.28f, 0.09f, 0.025f), cyan);
            DecorativePrimitive("GuardianShoulderL", PrimitiveType.Sphere, root.transform, new Vector3(-0.46f, 1.15f, 0f), new Vector3(0.28f, 0.20f, 0.34f), ivory);
            DecorativePrimitive("GuardianShoulderR", PrimitiveType.Sphere, root.transform, new Vector3(0.46f, 1.15f, 0f), new Vector3(0.28f, 0.20f, 0.34f), ivory);

            GameObject blade = DecorativePrimitive("GuardianEnergyBlade", PrimitiveType.Cylinder, root.transform,
                new Vector3(0.62f, 0.92f, 0.44f), new Vector3(0.045f, 0.72f, 0.045f), cyan);
            blade.transform.localRotation = Quaternion.Euler(58f, 0f, -9f);
            GameObject hilt = DecorativePrimitive("GuardianBladeHilt", PrimitiveType.Cylinder, root.transform,
                new Vector3(0.54f, 0.55f, 0.17f), new Vector3(0.075f, 0.20f, 0.075f), obsidian);
            hilt.transform.localRotation = blade.transform.localRotation;
        }

        private static void BuildBoss(Transform boss, Material obsidian, Material violet, Material hostile, List<Transform> rotators)
        {
            Renderer placeholder = boss.GetComponent<Renderer>();
            if (placeholder != null) placeholder.enabled = false;
            GameObject root = new GameObject("FracturedSignalVisualV15");
            root.transform.SetParent(boss, false);

            DecorativePrimitive("FracturedSignalCore", PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.12f, 0f), Vector3.one * 0.54f, hostile);
            DecorativePrimitive("FracturedSignalCage", PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.12f, 0f), Vector3.one * 0.78f, obsidian);
            DecorativePrimitive("FracturedSignalHeart", PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.12f, 0.32f), Vector3.one * 0.28f, hostile);

            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 0.86f, 0.12f + (i % 2 == 0 ? 0.22f : -0.18f), Mathf.Sin(a) * 0.86f);
                GameObject shard = DecorativePrimitive($"FracturedSignalShard_{i:00}", PrimitiveType.Cube, root.transform,
                    p, new Vector3(0.12f, 0.48f, 0.16f), i % 2 == 0 ? violet : hostile);
                shard.transform.localRotation = Quaternion.Euler(i * 17f, -a * Mathf.Rad2Deg, 22f + i * 5f);
            }

            Transform ringA = CreateCircleLine("FracturedSignalHaloA", root.transform, 1.18f, 64, violet, 0.035f);
            ringA.localPosition = new Vector3(0f, 0.12f, 0f);
            ringA.localRotation = Quaternion.Euler(63f, 0f, 0f);
            rotators.Add(ringA);
            Transform ringB = CreateCircleLine("FracturedSignalHaloB", root.transform, 0.98f, 64, hostile, 0.025f);
            ringB.localPosition = new Vector3(0f, 0.12f, 0f);
            ringB.localRotation = Quaternion.Euler(12f, 45f, 74f);
            rotators.Add(ringB);
        }

        private static void BuildWisp(Transform wisp, Material gold, Material cyan, Material verdant, List<Transform> rotators)
        {
            GameObject root = new GameObject("WispPresentationV15");
            root.transform.SetParent(wisp, false);
            Transform ringA = CreateCircleLine("WispHaloA", root.transform, 0.48f, 48, gold, 0.024f);
            ringA.localRotation = Quaternion.Euler(64f, 0f, 14f);
            Transform ringB = CreateCircleLine("WispHaloB", root.transform, 0.38f, 48, cyan, 0.018f);
            ringB.localRotation = Quaternion.Euler(16f, 52f, 78f);
            Transform ringC = CreateCircleLine("WispHaloC", root.transform, 0.30f, 40, verdant, 0.014f);
            ringC.localRotation = Quaternion.Euler(88f, 22f, 0f);
            rotators.Add(ringA);
            rotators.Add(ringB);
            rotators.Add(ringC);
        }

        private static void ConfigureLighting(Transform awakening, Transform arena, List<Light> accents)
        {
            GameObject keyObject = FindSceneObject("KeyLight");
            Light key = keyObject != null ? keyObject.GetComponent<Light>() : null;
            if (key != null)
            {
                key.color = new Color(0.82f, 0.89f, 1.0f);
                key.intensity = 1.18f;
                key.shadows = LightShadows.Soft;
                key.shadowStrength = 0.82f;
                key.transform.rotation = Quaternion.Euler(46f, -34f, 0f);
            }

            Light warm = CreateAccentLight("AwakeningWarmRim", awakening, new Vector3(0f, 5.8f, 5.4f), new Color(1f, 0.67f, 0.34f), 0.82f, 13f);
            accents.Add(warm);
            Light hostile = CreateAccentLight("ArenaHostileBacklight", arena, new Vector3(0f, 6.6f, 9.5f), new Color(1f, 0.10f, 0.14f), 1.25f, 17f);
            accents.Add(hostile);
        }

        private static void ConfigureAtmosphere(Camera camera)
        {
            camera.fieldOfView = 48f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 120f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.006f, 0.010f, 0.020f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0085f;
            RenderSettings.fogColor = new Color(0.012f, 0.018f, 0.032f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.085f, 0.12f, 0.20f);
            RenderSettings.ambientEquatorColor = new Color(0.030f, 0.045f, 0.072f);
            RenderSettings.ambientGroundColor = new Color(0.006f, 0.009f, 0.016f);
            RenderSettings.reflectionIntensity = 0.72f;
        }

        private static void BuildDemoHud(
            out CanvasGroup titleOverlay,
            out CanvasGroup instructionPanel,
            out CanvasGroup controlRibbon,
            out Text titleText,
            out Text subtitleText,
            out Text instructionText,
            out Text phaseText,
            out Text controlsText)
        {
            GameObject canvasObject = new GameObject("MindforgeDemoHUDV15", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            titleOverlay = CreatePanel("DemoTitleOverlay", canvas.transform, new Color(0.004f, 0.007f, 0.014f, 0.96f), Vector2.zero, Vector2.one);
            titleText = CreateText("DemoTitle", titleOverlay.transform, "MINDFORGE", 62, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.12f, 0.46f), new Vector2(0.88f, 0.62f), new Color(0.92f, 0.96f, 1f));
            subtitleText = CreateText("DemoSubtitle", titleOverlay.transform, "NEURAL COMBAT PROTOTYPE", 21, FontStyle.Normal, TextAnchor.UpperCenter,
                new Vector2(0.18f, 0.38f), new Vector2(0.82f, 0.48f), new Color(0.54f, 0.68f, 0.82f));
            CreateText("DemoSkip", titleOverlay.transform, "SPACE TO SKIP", 14, FontStyle.Normal, TextAnchor.LowerCenter,
                new Vector2(0.35f, 0.04f), new Vector2(0.65f, 0.10f), new Color(0.42f, 0.48f, 0.58f));

            instructionPanel = CreatePanel("DemoInstructionPanel", canvas.transform, new Color(0.008f, 0.014f, 0.028f, 0.86f),
                new Vector2(0.22f, 0.08f), new Vector2(0.78f, 0.29f));
            phaseText = CreateText("DemoPhase", instructionPanel.transform, "", 15, FontStyle.Bold, TextAnchor.UpperCenter,
                new Vector2(0.04f, 0.66f), new Vector2(0.96f, 0.94f), new Color(0.42f, 0.72f, 1f));
            instructionText = CreateText("DemoInstruction", instructionPanel.transform, "", 21, FontStyle.Normal, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.72f), Color.white);

            controlRibbon = CreatePanel("DemoControlRibbon", canvas.transform, new Color(0.006f, 0.010f, 0.022f, 0.88f),
                new Vector2(0.12f, 0.015f), new Vector2(0.88f, 0.12f));
            controlsText = CreateText("DemoControls", controlRibbon.transform, "", 16, FontStyle.Normal, TextAnchor.MiddleCenter,
                new Vector2(0.025f, 0.08f), new Vector2(0.975f, 0.92f), new Color(0.86f, 0.92f, 1f));
        }

        private Transform CreatePose(string name, Vector3 position, Vector3 lookAt)
        {
            GameObject pose = new GameObject(name);
            pose.transform.SetParent(transform, false);
            pose.transform.position = position;
            pose.transform.rotation = Quaternion.LookRotation((lookAt - position).normalized, Vector3.up);
            return pose.transform;
        }

        private static GameObject DecorativePrimitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                UnityEngine.Object.Destroy(collider);
            }
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = true;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Simple;
            }
            return go;
        }

        private static Transform CreateCircleLine(string name, Transform parent, float radius, int segments, Material material, float width)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.widthMultiplier = width;
            line.positionCount = Mathf.Max(12, segments);
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (int i = 0; i < line.positionCount; i++)
            {
                float a = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
            }
            return go.transform;
        }

        private static Light CreateAccentLight(string name, Transform parent, Vector3 localPosition, Color color, float intensity, float range)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        private static Material CreateLit(string name, Color color, float metallic, float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            Material material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color); else material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        private static Material CreateEmission(string name, Color color, float strength)
        {
            Material material = CreateLit(name, color * 0.22f, 0.38f, 0.78f);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * strength);
            }
            return material;
        }

        private static CanvasGroup CreatePanel(string name, Transform parent, Color color, Vector2 min, Vector2 max)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return go.GetComponent<CanvasGroup>();
        }

        private static Text CreateText(string name, Transform parent, string value, int size, FontStyle style, TextAnchor alignment, Vector2 min, Vector2 max, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Text text = go.GetComponent<Text>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        private static GameObject FindSceneObject(string name)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                    if (string.Equals(transforms[i].name, name, StringComparison.Ordinal))
                        return transforms[i].gameObject;
            }
            return null;
        }
    }

    /// <summary>
    /// Slow decorative motion is useful for the demo but must disappear from every EEG
    /// evidence interval. This component freezes rotators and restores constant light
    /// intensities throughout baseline, coded calibration, and player-armed resonance.
    /// </summary>
    public sealed class NeuralQuietAmbientMotionV15 : MonoBehaviour
    {
        [SerializeField] private float rotationDegreesPerSecond = 5.5f;
        [SerializeField] private float decorativePulseHz = 0.12f;
        [SerializeField] private float decorativePulseDepth = 0.07f;

        private AwakeningCalibrationDirector _calibration;
        private SoulWispController _wisp;
        private Transform[] _rotators = Array.Empty<Transform>();
        private Light[] _lights = Array.Empty<Light>();
        private float[] _baseLightIntensities = Array.Empty<float>();

        public void Configure(AwakeningCalibrationDirector calibration, SoulWispController wisp, Transform[] rotators, Light[] lights)
        {
            _calibration = calibration;
            _wisp = wisp;
            _rotators = rotators ?? Array.Empty<Transform>();
            _lights = lights ?? Array.Empty<Light>();
            _baseLightIntensities = new float[_lights.Length];
            for (int i = 0; i < _lights.Length; i++)
                _baseLightIntensities[i] = _lights[i] != null ? _lights[i].intensity : 0f;
        }

        private void Update()
        {
            bool neuralQuiet = (_calibration != null && _calibration.CalibrationInProgress) ||
                               (_wisp != null && (_wisp.CalibrationStimuliActive || _wisp.ResonanceWindowActive));

            if (neuralQuiet)
            {
                for (int i = 0; i < _lights.Length; i++)
                    if (_lights[i] != null) _lights[i].intensity = _baseLightIntensities[i];
                return;
            }

            float delta = Time.unscaledDeltaTime * rotationDegreesPerSecond;
            for (int i = 0; i < _rotators.Length; i++)
            {
                Transform rotator = _rotators[i];
                if (rotator == null) continue;
                float direction = i % 2 == 0 ? 1f : -1f;
                rotator.Rotate(0f, 0f, delta * direction, Space.Self);
            }

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * decorativePulseHz) * decorativePulseDepth;
            for (int i = 0; i < _lights.Length; i++)
                if (_lights[i] != null) _lights[i].intensity = _baseLightIntensities[i] * pulse;
        }
    }
}