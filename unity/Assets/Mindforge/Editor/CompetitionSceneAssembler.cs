#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Mindforge.Calibration;
using Mindforge.Combat;
using Mindforge.Neural;
using Mindforge.Presentation;
using Mindforge.Qualification;
using Mindforge.SoulWisp;
using Mindforge.Telemetry;

namespace Mindforge.Editor
{
    /// <summary>
    /// Rebuilds the complete Gate-1 competition scene from a clean checkout.
    /// Placeholder geometry is deliberate: assembly/authority/timing must work before
    /// production art is allowed to obscure broken serialized references.
    /// </summary>
    public static class CompetitionSceneAssembler
    {
        public const string ScenePath = CompetitionGateValidator.ScenePath;
        private const string Generated = "Assets/Mindforge/Generated";
        private const string Prefabs = Generated + "/Prefabs";
        private const string Materials = Generated + "/Materials";

        [MenuItem("Mindforge/Competition/Build Competition Scene")]
        public static void BuildCompetitionScene()
        {
            CompetitionProjectConfigurator.Configure();
            EnsureFolders();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CombatTuning tuning = EnsureAsset<CombatTuning>(Generated + "/MindforgeCombatTuning.asset");
            CombatVisualPalette palette = EnsureAsset<CombatVisualPalette>(Generated + "/MindforgeVisualPalette.asset");
            Material groundMat = EnsureMaterial(Materials + "/Ground.mat", new Color(0.035f, 0.045f, 0.075f));
            Material guardianMat = EnsureMaterial(Materials + "/Guardian.mat", new Color(0.78f, 0.82f, 0.96f));
            Material bossMat = EnsureMaterial(Materials + "/Boss.mat", new Color(0.38f, 0.04f, 0.12f));
            Material sightCoreMat = EnsureEmissionMaterial(Materials + "/SightCore.mat", palette.sightTarget);
            Material guardCoreMat = EnsureEmissionMaterial(Materials + "/GuardCore.mat", palette.guardTarget);
            Material sightShellMat = EnsureEmissionMaterial(Materials + "/SightShell.mat", palette.sightTarget * 0.45f);
            Material guardShellMat = EnsureEmissionMaterial(Materials + "/GuardShell.mat", palette.guardTarget * 0.45f);
            Material wispMat = EnsureEmissionMaterial(Materials + "/WispCore.mat", palette.concord);
            Material hostileMat = EnsureEmissionMaterial(Materials + "/Hostile.mat", palette.hostilePrimary);

            MindforgeProjectile projectilePrefab = CreateProjectilePrefab(palette, hostileMat);
            FracturedEchoNode echoPrefab = CreateEchoPrefab(projectilePrefab, bossMat);

            GameObject runtime = new GameObject("MindforgeRuntime");
            HitStopController hitStop = runtime.AddComponent<HitStopController>();
            CombatBootstrap bootstrap = runtime.AddComponent<CombatBootstrap>();
            DisplayTimingMonitor timing = runtime.AddComponent<DisplayTimingMonitor>();
            runtime.AddComponent<DemoFaultHarness>();

            GameObject awakening = BuildAwakening(groundMat, wispMat);
            GameObject arena = BuildArena(groundMat, bossMat);

            Light key;
            Camera camera;
            Transform impactPivot;
            CombatPresentationDirector presentation = BuildCamera(out camera, out impactPivot, out key);

            GameObject guardian = CreatePrimitive("Guardian", PrimitiveType.Capsule, null,
                new Vector3(0f, 0.5f, -4.5f), new Vector3(0.85f, 1f, 0.85f), guardianMat, true);
            Rigidbody guardianBody = guardian.AddComponent<Rigidbody>();
            guardianBody.useGravity = false;
            guardianBody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            PoiseSystem guardianPoise = guardian.AddComponent<PoiseSystem>();
            CombatantVitals guardianVitals = guardian.AddComponent<CombatantVitals>();
            SetEnum(guardianVitals, "team", (int)CombatTeam.Guardian); SetFloat(guardianVitals, "maxHealth", 120f);
            SetRef(guardianVitals, "poise", guardianPoise); SetRef(guardianVitals, "body", guardianBody);
            AuraBuffController buffs = guardian.AddComponent<AuraBuffController>();
            FluxMeter flux = guardian.AddComponent<FluxMeter>(); SetRef(flux, "tuning", tuning);
            GuardianMotor motor = guardian.AddComponent<GuardianMotor>(); SetRef(motor, "tuning", tuning); SetRef(motor, "cameraReference", camera.transform);
            GuardianCombatController combat = guardian.AddComponent<GuardianCombatController>();
            GuardianCombatInput input = guardian.AddComponent<GuardianCombatInput>();
            GravityBloomAbility bloom = guardian.AddComponent<GravityBloomAbility>();
            GameObject muzzle = Child("Muzzle", guardian.transform, new Vector3(0f, 0.45f, 0.65f));
            GameObject capture = Child("BloomCapture", guardian.transform, new Vector3(0f, 0.8f, 0f));
            GameObject nearMiss = Child("NearMissSensor", guardian.transform, Vector3.zero);
            SphereCollider nearMissCollider = nearMiss.AddComponent<SphereCollider>(); nearMissCollider.radius = 1.45f; nearMissCollider.isTrigger = true;
            ProjectileNearMissSensor sensor = nearMiss.AddComponent<ProjectileNearMissSensor>();
            SetRef(sensor, "motor", motor); SetRef(sensor, "flux", flux); SetRef(sensor, "tuning", tuning);

            GameObject boss = CreatePrimitive("The_Fractured_Signal", PrimitiveType.Cylinder, arena.transform,
                new Vector3(0f, 0.9f, 5.5f), new Vector3(2.2f, 1.8f, 2.2f), bossMat, true);
            Rigidbody bossBody = boss.AddComponent<Rigidbody>(); bossBody.useGravity = false; bossBody.isKinematic = true;
            PoiseSystem bossPoise = boss.AddComponent<PoiseSystem>();
            CombatantVitals bossVitals = boss.AddComponent<CombatantVitals>();
            SetEnum(bossVitals, "team", (int)CombatTeam.Enemy); SetFloat(bossVitals, "maxHealth", 540f);
            SetRef(bossVitals, "poise", bossPoise); SetRef(bossVitals, "body", bossBody);
            GameObject bossOrigin = Child("ProjectileOrigin", boss.transform, new Vector3(0f, 0.7f, 0f));
            FracturedSignalTelegraph telegraph = boss.AddComponent<FracturedSignalTelegraph>();
            ConfigureTelegraph(telegraph, boss.transform, palette, hostileMat);
            FracturedSignalDirector bossDirector = boss.AddComponent<FracturedSignalDirector>();
            SignalBreakReward signalBreak = boss.AddComponent<SignalBreakReward>();

            GameObject wisp = new GameObject("SoulWispRoot");
            SoulWispController wispController = wisp.AddComponent<SoulWispController>();
            GameObject core = CreatePrimitive("WispCore", PrimitiveType.Sphere, wisp.transform, Vector3.zero, Vector3.one * 0.32f, wispMat, false);
            GameObject sightRoot = Child("SightAuraRoot", wisp.transform, Vector3.zero);
            GameObject guardRoot = Child("GuardAuraRoot", wisp.transform, Vector3.zero);
            VepAuraStimulus sightStimulus = CreateAuraCore("SightVepCore", sightRoot.transform, sightCoreMat, 10f, palette.sightTarget);
            VepAuraStimulus guardStimulus = CreateAuraCore("GuardVepCore", guardRoot.transform, guardCoreMat, 12f, palette.guardTarget);
            Renderer sightShell = CreateShell("SightFeedbackShell", sightRoot.transform, sightShellMat);
            Renderer guardShell = CreateShell("GuardFeedbackShell", guardRoot.transform, guardShellMat);
            SetRef(wispController, "player", guardian.transform); SetRef(wispController, "wispCore", core.transform);
            SetRef(wispController, "sightAura", sightRoot.transform); SetRef(wispController, "guardAura", guardRoot.transform);
            SetRef(wispController, "sightStimulus", sightStimulus); SetRef(wispController, "guardStimulus", guardStimulus); SetRef(wispController, "palette", palette);

            GameObject neural = new GameObject("MindforgeNeuralRuntime");
            UdpNeuralReceiver receiver = neural.AddComponent<UdpNeuralReceiver>();
            DualAuraCombatDirector auraDirector = neural.AddComponent<DualAuraCombatDirector>();
            SetRef(auraDirector, "neuralReceiver", receiver); SetRef(auraDirector, "buffs", buffs);
            NeuralAuraFeedback auraFeedback = neural.AddComponent<NeuralAuraFeedback>();
            SetRef(auraFeedback, "receiver", receiver); SetRef(auraFeedback, "palette", palette);
            ConfigureFeedbackShell(auraFeedback, "sight", sightShell.transform, sightShell);
            ConfigureFeedbackShell(auraFeedback, "guard", guardShell.transform, guardShell);
            NeuralHapticFeedback haptics = neural.AddComponent<NeuralHapticFeedback>(); SetRef(haptics, "receiver", receiver); SetRef(haptics, "buffs", buffs);
            CalibrationMarkerSender marker = neural.AddComponent<CalibrationMarkerSender>();

            Canvas canvas = CreateCanvas();
            Text calibrationStatus = CreateText("CalibrationStatus", canvas.transform, "WAITING FOR NEURAL CALIBRATION SERVICE", 26, TextAnchor.MiddleCenter,
                new Vector2(0.18f, 0.72f), new Vector2(0.82f, 0.92f));
            CanvasGroup warningGroup = CreatePanel("NeuralWarning", canvas.transform, new Color(0.04f, 0.04f, 0.06f, 0.92f),
                new Vector2(0.27f, 0.44f), new Vector2(0.73f, 0.58f), out _);
            Text warningText = CreateText("WarningText", warningGroup.transform, "", 30, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            CanvasGroup veil = CreatePanel("NeuralDegradationVeil", canvas.transform, new Color(0.32f, 0.32f, 0.36f, 0.80f), Vector2.zero, Vector2.one, out _);
            veil.alpha = 0f; veil.transform.SetAsFirstSibling();
            BuildEvidenceHud(canvas.transform, receiver, palette);
            Image diodeImage = CreateImage("PhotodiodePatch", canvas.transform, Color.black, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-8f, 8f), new Vector2(78f, 78f));
            PhotodiodePatch diode = diodeImage.gameObject.AddComponent<PhotodiodePatch>();
            SetRef(diode, "sightStimulus", sightStimulus); SetRef(diode, "guardStimulus", guardStimulus); SetRef(diode, "patch", diodeImage);
            DisplayQualificationController displayQualification = runtime.AddComponent<DisplayQualificationController>();
            SetRef(displayQualification, "timingMonitor", timing); SetRef(displayQualification, "photodiodePatch", diode);

            NeuralLinkContingency contingency = neural.AddComponent<NeuralLinkContingency>();
            SetRef(contingency, "receiver", receiver); SetRef(contingency, "bossDirector", bossDirector); SetRef(contingency, "guardianInput", input);
            SetRef(contingency, "gravityBloom", bloom); SetRef(contingency, "warningGroup", warningGroup); SetRef(contingency, "warningText", warningText); SetRef(contingency, "desaturationVeil", veil);

            AwakeningCalibrationDirector calibration = neural.AddComponent<AwakeningCalibrationDirector>();
            SetRef(calibration, "receiver", receiver); SetRef(calibration, "markerSender", marker); SetRef(calibration, "linkContingency", contingency);
            SetRef(calibration, "guardianInput", input); SetRef(calibration, "soulWisp", wispController); SetRef(calibration, "displayTiming", timing); SetRef(calibration, "combatTarget", boss.transform);
            SetRef(calibration, "wispCoreRoot", core); SetRef(calibration, "sightAuraRoot", sightRoot); SetRef(calibration, "guardAuraRoot", guardRoot);
            SetRef(calibration, "awakeningRoomRoot", awakening); SetRef(calibration, "arenaRoot", arena); SetRef(calibration, "statusText", calibrationStatus);

            SetRef(combat, "tuning", tuning); SetRef(combat, "motor", motor); SetRef(combat, "auras", buffs); SetRef(combat, "flux", flux);
            SetRef(combat, "vitals", guardianVitals); SetRef(combat, "hitStop", hitStop); SetRef(combat, "presentation", presentation);
            SetRef(combat, "projectilePrefab", projectilePrefab); SetRef(combat, "muzzle", muzzle.transform); SetRef(combat, "primaryTarget", boss.transform);
            SetMask(combat, "damageMask", ~0); SetMask(combat, "projectileMask", ~0);
            SetRef(input, "motor", motor); SetRef(input, "combat", combat); SetRef(input, "bloom", bloom); SetRef(input, "aimTarget", boss.transform);
            SetRef(bloom, "tuning", tuning); SetRef(bloom, "flux", flux); SetRef(bloom, "auras", buffs); SetRef(bloom, "captureAnchor", capture.transform);
            SetRef(bloom, "primaryTarget", boss.transform); SetMask(bloom, "projectileMask", ~0); SetRef(bloom, "hitStop", hitStop); SetRef(bloom, "presentation", presentation);

            SetRef(bossDirector, "vitals", bossVitals); SetRef(bossDirector, "projectilePrefab", projectilePrefab); SetRef(bossDirector, "projectileOrigin", bossOrigin.transform);
            SetRef(bossDirector, "player", guardian.transform); SetRef(bossDirector, "playerFlux", flux); SetRef(bossDirector, "soulWisp", wispController);
            SetRef(bossDirector, "telegraph", telegraph); SetRef(bossDirector, "echoPrefab", echoPrefab); SetRef(bossDirector, "echoParent", arena.transform);
            SetRef(signalBreak, "poise", bossPoise); SetRef(signalBreak, "flux", flux); SetRef(signalBreak, "tuning", tuning); SetRef(signalBreak, "hitStop", hitStop); SetRef(signalBreak, "presentation", presentation);

            SetRefs(bootstrap, "continuousBodies", guardianBody, bossBody);
            MindforgeSessionLogger logger = runtime.AddComponent<MindforgeSessionLogger>();
            SetRef(logger, "receiver", receiver); SetRef(logger, "calibration", calibration); SetRef(logger, "linkContingency", contingency);
            SetRef(logger, "bossDirector", bossDirector); SetRef(logger, "bossVitals", bossVitals); SetRef(logger, "playerVitals", guardianVitals); SetRef(logger, "flux", flux);

            arena.SetActive(false);
            Selection.activeGameObject = guardian;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[Mindforge] Competition scene assembled: {ScenePath}");
        }

        public static void BuildAndValidate()
        {
            BuildCompetitionScene();
            if (!CompetitionGateValidator.ValidateAndWrite(true))
                throw new UnityEditor.Build.BuildFailedException("Mindforge Gate 1 validation failed. See experiments/reports/unity-gate1-latest.json");
        }

        [MenuItem("Mindforge/Competition/Build Windows Demo")]
        public static void BuildWindowsDemo()
        {
            BuildAndValidate();
            Directory.CreateDirectory("Builds/Mindforge");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = "Builds/Mindforge/Mindforge.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development,
            };
            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new UnityEditor.Build.BuildFailedException($"Windows build failed: {report.summary.result}");
        }

        private static GameObject BuildAwakening(Material ground, Material accent)
        {
            GameObject root = new GameObject("The_Awakening");
            CreatePrimitive("AwakeningFloor", PrimitiveType.Cube, root.transform, new Vector3(0f, -0.55f, 0f), new Vector3(8f, 0.5f, 8f), ground, true);
            CreatePrimitive("ListeningMonolith", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.75f, 2.8f), new Vector3(0.8f, 1.5f, 0.8f), accent, false);
            return root;
        }

        private static GameObject BuildArena(Material ground, Material accent)
        {
            GameObject root = new GameObject("Fractured_Signal_Arena");
            CreatePrimitive("ArenaFloor", PrimitiveType.Cube, root.transform, new Vector3(0f, -0.55f, 1f), new Vector3(24f, 0.5f, 24f), ground, true);
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                CreatePrimitive($"ArenaPillar_{i:00}", PrimitiveType.Cylinder, root.transform,
                    new Vector3(Mathf.Cos(a) * 10f, 1.2f, 1f + Mathf.Sin(a) * 10f), new Vector3(0.45f, 2.4f, 0.45f), accent, false);
            }
            return root;
        }

        private static CombatPresentationDirector BuildCamera(out Camera camera, out Transform impactPivot, out Light key)
        {
            GameObject follow = new GameObject("FollowRig");
            GameObject pivot = Child("ImpactPivot", follow.transform, Vector3.zero); impactPivot = pivot.transform;
            GameObject cameraGo = Child("GameplayCamera", pivot.transform, Vector3.zero); cameraGo.tag = "MainCamera";
            camera = cameraGo.AddComponent<Camera>(); cameraGo.AddComponent<AudioListener>();
            follow.transform.position = new Vector3(0f, 15f, -13f);
            follow.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.5f, 1f) - follow.transform.position, Vector3.up);
            camera.fieldOfView = 48f; camera.nearClipPlane = 0.1f; camera.farClipPlane = 100f;
            key = new GameObject("KeyLight").AddComponent<Light>(); key.type = LightType.Directional; key.color = new Color(0.72f, 0.80f, 1f); key.intensity = 1.25f; key.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            CombatPresentationDirector presentation = follow.AddComponent<CombatPresentationDirector>();
            SetRef(presentation, "impactPivot", pivot.transform); SetRef(presentation, "gameplayCamera", camera); SetRefs(presentation, "ambientLights", key);
            return presentation;
        }

        private static MindforgeProjectile CreateProjectilePrefab(CombatVisualPalette palette, Material material)
        {
            GameObject go = CreatePrimitive("MindforgeProjectile", PrimitiveType.Cube, null, Vector3.zero, new Vector3(0.16f, 0.16f, 0.42f), material, true);
            go.GetComponent<Collider>().isTrigger = true;
            Rigidbody body = go.AddComponent<Rigidbody>(); body.useGravity = false; body.mass = 0.1f;
            TrailRenderer trail = go.AddComponent<TrailRenderer>(); trail.time = 0.18f; trail.startWidth = 0.09f; trail.endWidth = 0f;
            MindforgeProjectile projectile = go.AddComponent<MindforgeProjectile>();
            SetRef(projectile, "palette", palette); SetRef(projectile, "visualRenderer", go.GetComponent<Renderer>()); SetRef(projectile, "trailRenderer", trail);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, Prefabs + "/MindforgeProjectile.prefab"); UnityEngine.Object.DestroyImmediate(go);
            return prefab.GetComponent<MindforgeProjectile>();
        }

        private static FracturedEchoNode CreateEchoPrefab(MindforgeProjectile projectile, Material material)
        {
            GameObject go = CreatePrimitive("FracturedEcho", PrimitiveType.Cube, null, Vector3.zero, new Vector3(0.62f, 0.82f, 0.62f), material, true);
            go.transform.rotation = Quaternion.Euler(28f, 45f, 18f);
            CombatantVitals vitals = go.AddComponent<CombatantVitals>(); SetEnum(vitals, "team", (int)CombatTeam.Enemy); SetFloat(vitals, "maxHealth", 42f);
            FracturedEchoNode echo = go.AddComponent<FracturedEchoNode>(); SetRef(echo, "vitals", vitals); SetRef(echo, "projectilePrefab", projectile);
            GameObject origin = Child("ProjectileOrigin", go.transform, new Vector3(0f, 0.55f, 0f)); SetRef(echo, "projectileOrigin", origin.transform);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, Prefabs + "/FracturedEcho.prefab"); UnityEngine.Object.DestroyImmediate(go);
            return prefab.GetComponent<FracturedEchoNode>();
        }

        private static VepAuraStimulus CreateAuraCore(string name, Transform parent, Material material, float hz, Color color)
        {
            GameObject go = CreatePrimitive(name, PrimitiveType.Sphere, parent, Vector3.zero, Vector3.one * 0.30f, material, false);
            VepAuraStimulus stimulus = go.AddComponent<VepAuraStimulus>(); SetRef(stimulus, "targetRenderer", go.GetComponent<Renderer>()); SetFloat(stimulus, "frequencyHz", hz); SetColor(stimulus, "baseColor", color); return stimulus;
        }

        private static Renderer CreateShell(string name, Transform parent, Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.widthMultiplier = 0.045f;
            line.positionCount = 48;
            const float radius = 0.43f;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i / (float)line.positionCount * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
            return line;
        }

        private static void ConfigureFeedbackShell(NeuralAuraFeedback feedback, string field, Transform root, Renderer renderer)
        {
            SerializedObject so = new SerializedObject(feedback); SerializedProperty shell = so.FindProperty(field);
            if (shell == null) throw new InvalidOperationException($"Missing NeuralAuraFeedback.{field}");
            shell.FindPropertyRelative("root").objectReferenceValue = root; shell.FindPropertyRelative("shellRenderer").objectReferenceValue = renderer; so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureTelegraph(FracturedSignalTelegraph telegraph, Transform parent, CombatVisualPalette palette, Material material)
        {
            LineRenderer[] rays = new LineRenderer[8];
            for (int i = 0; i < rays.Length; i++) { GameObject go = Child($"TelegraphRay_{i:00}", parent, Vector3.zero); LineRenderer line = go.AddComponent<LineRenderer>(); line.material = material; line.widthMultiplier = 0.055f; line.useWorldSpace = true; rays[i] = line; }
            GameObject ringGo = Child("TelegraphRing", parent, Vector3.zero); LineRenderer ring = ringGo.AddComponent<LineRenderer>(); ring.material = material; ring.widthMultiplier = 0.055f; ring.useWorldSpace = true;
            SetRef(telegraph, "palette", palette); SetRefs(telegraph, "rays", rays); SetRef(telegraph, "radialRing", ring);
        }

        private static Canvas CreateCanvas()
        {
            GameObject go = new GameObject("CompetitionHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); return canvas;
        }

        private static void BuildEvidenceHud(Transform parent, UdpNeuralReceiver receiver, CombatVisualPalette palette)
        {
            CanvasGroup panel = CreatePanel("NeuralEvidenceHud", parent, new Color(0.015f, 0.02f, 0.04f, 0.86f), new Vector2(0.015f, 0.67f), new Vector2(0.31f, 0.97f), out _);
            Image sight = CreateBar("SightEvidence", panel.transform, palette.sightTarget, 0.72f); Image guard = CreateBar("GuardEvidence", panel.transform, palette.guardTarget, 0.57f); Image quality = CreateBar("QualityEvidence", panel.transform, Color.white, 0.42f);
            Text state = CreateText("EvidenceState", panel.transform, "NEURAL OFFLINE", 22, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.79f), new Vector2(0.94f, 0.96f));
            Text scores = CreateText("EvidenceScores", panel.transform, "Sight 0 Guard 0 Δ 0 Q 0", 16, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.20f), new Vector2(0.94f, 0.34f));
            Text mode = CreateText("EvidenceMode", panel.transform, "UNKNOWN", 15, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.08f), new Vector2(0.48f, 0.18f));
            Text transport = CreateText("EvidenceTransport", panel.transform, "Q 0 old 0 overflow 0", 15, TextAnchor.MiddleRight, new Vector2(0.48f, 0.08f), new Vector2(0.94f, 0.18f));
            NeuralEvidenceHud hud = panel.gameObject.AddComponent<NeuralEvidenceHud>(); SetRef(hud, "receiver", receiver); SetRef(hud, "sightFill", sight); SetRef(hud, "guardFill", guard); SetRef(hud, "qualityFill", quality); SetRef(hud, "stateText", state); SetRef(hud, "scoreText", scores); SetRef(hud, "modeText", mode); SetRef(hud, "transportText", transport);
        }

        private static Image CreateBar(string name, Transform parent, Color color, float y)
        {
            Image bg = CreateImage(name + "Background", parent, new Color(0.12f, 0.13f, 0.17f, 1f), new Vector2(0.06f, y), new Vector2(0.94f, y + 0.08f), Vector2.zero, Vector2.zero);
            Image fill = CreateImage(name, bg.transform, color, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 0f; return fill;
        }

        private static CanvasGroup CreatePanel(string name, Transform parent, Color color, Vector2 min, Vector2 max, out Image image)
        { image = CreateImage(name, parent, color, min, max, Vector2.zero, Vector2.zero); return image.gameObject.AddComponent<CanvasGroup>(); }

        private static Image CreateImage(string name, Transform parent, Color color, Vector2 min, Vector2 max, Vector2 anchored, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(parent, false); RectTransform rt = (RectTransform)go.transform; rt.anchorMin = min; rt.anchorMax = max; rt.anchoredPosition = anchored; rt.sizeDelta = size; Image image = go.GetComponent<Image>(); image.color = color; return image;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment, Vector2 min, Vector2 max)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); go.transform.SetParent(parent, false); RectTransform rt = (RectTransform)go.transform; rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            Text text = go.GetComponent<Text>(); text.text = value; text.fontSize = size; text.alignment = alignment; text.color = Color.white; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); return text;
        }

        private static GameObject Child(string name, Transform parent, Vector3 localPosition)
        { GameObject go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = localPosition; return go; }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, bool keepCollider)
        {
            GameObject go = GameObject.CreatePrimitive(type); go.name = name; if (parent != null) go.transform.SetParent(parent, false); go.transform.position = position; go.transform.localScale = scale;
            Renderer renderer = go.GetComponent<Renderer>(); if (renderer != null && material != null) renderer.sharedMaterial = material;
            if (!keepCollider) { Collider collider = go.GetComponent<Collider>(); if (collider != null) UnityEngine.Object.DestroyImmediate(collider); } return go;
        }

        private static void EnsureFolders() { Directory.CreateDirectory(Generated); Directory.CreateDirectory(Prefabs); Directory.CreateDirectory(Materials); Directory.CreateDirectory("Assets/Mindforge/Scenes"); }
        private static T EnsureAsset<T>(string path) where T : ScriptableObject { T asset = AssetDatabase.LoadAssetAtPath<T>(path); if (asset != null) return asset; asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset; }
        private static Material EnsureMaterial(string path, Color color) { Material mat = AssetDatabase.LoadAssetAtPath<Material>(path); if (mat == null) { Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"); mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); } if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color); else mat.color = color; return mat; }
        private static Material EnsureEmissionMaterial(string path, Color color) { Material mat = EnsureMaterial(path, color * 0.35f); if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * 1.4f); } return mat; }

        private static void SetRef(UnityEngine.Object target, string field, UnityEngine.Object value) { SerializedObject so = new SerializedObject(target); SerializedProperty p = so.FindProperty(field); if (p == null) throw new InvalidOperationException($"{target.GetType().Name}.{field} not found"); p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetRefs(UnityEngine.Object target, string field, params UnityEngine.Object[] values) { SerializedObject so = new SerializedObject(target); SerializedProperty p = so.FindProperty(field); if (p == null) throw new InvalidOperationException($"{target.GetType().Name}.{field} not found"); p.arraySize = values.Length; for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetFloat(UnityEngine.Object target, string field, float value) { SerializedObject so = new SerializedObject(target); SerializedProperty p = so.FindProperty(field); if (p == null) throw new InvalidOperationException(field); p.floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetEnum(UnityEngine.Object target, string field, int value) { SerializedObject so = new SerializedObject(target); SerializedProperty p = so.FindProperty(field); if (p == null) throw new InvalidOperationException(field); p.enumValueIndex = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetMask(UnityEngine.Object target, string field, int value) { SerializedObject so = new SerializedObject(target); SerializedProperty p = so.FindProperty(field); if (p == null) throw new InvalidOperationException(field); p.intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetColor(UnityEngine.Object target, string field, Color value) { SerializedObject so = new SerializedObject(target); SerializedProperty p = so.FindProperty(field); if (p == null) throw new InvalidOperationException(field); p.colorValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
#endif
