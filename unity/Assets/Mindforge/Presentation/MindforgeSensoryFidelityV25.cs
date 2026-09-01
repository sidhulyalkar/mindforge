using System;
using System.Collections;
using UnityEngine;
using Mindforge.Calibration;
using Mindforge.Combat;
using Mindforge.SoulWisp;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Canonical V0.25 runtime presentation root for Latest.
    ///
    /// It promotes presentation systems that were previously trapped behind the legacy showcase
    /// firewall: pooled impact VFX, locomotion accents, tiny camera impulses, a deeper Fractured
    /// Signal surface, quieter HUD hierarchy, diegetic conventional prompts and restrained spatial
    /// audio. Every child is downstream of authoritative gameplay state and freezes/suppresses
    /// distracting motion during neural visual-field intervals.
    /// </summary>
    [DefaultExecutionOrder(-30)]
    public sealed class MindforgeSensoryFidelityV25 : MonoBehaviour
    {
        public const string RootName = "Mindforge_SensoryFidelity_Runtime_V25";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            MindforgeDemoV11Marker marker = FindObjectOfType<MindforgeDemoV11Marker>(true);
            if (marker == null || marker.GetComponent<MindforgeSensoryFidelityV25>() != null) return;
            marker.gameObject.AddComponent<MindforgeSensoryFidelityV25>();
        }

        private IEnumerator Start()
        {
            GuardianCombatInput input = null;
            GuardianMotor motor = null;
            GuardianTargetLock targetLock = null;
            GuardianSwordShieldController physical = null;
            FracturedSignalDirector boss = null;
            Camera camera = null;
            AwakeningCalibrationDirector calibration = null;
            SoulWispController wisp = null;
            AuraBuffController buffs = null;

            for (int frame = 0; frame < 240; frame++)
            {
                if (input == null) input = FindObjectOfType<GuardianCombatInput>(true);
                if (input != null && motor == null) motor = input.GetComponent<GuardianMotor>();
                if (input != null && targetLock == null) targetLock = input.GetComponent<GuardianTargetLock>();
                if (input != null && physical == null) physical = input.GetComponent<GuardianSwordShieldController>();
                if (input != null && buffs == null) buffs = input.GetComponent<AuraBuffController>();
                if (boss == null) boss = FindObjectOfType<FracturedSignalDirector>(true);
                if (camera == null) camera = Camera.main;
                if (calibration == null) calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
                if (wisp == null) wisp = FindObjectOfType<SoulWispController>(true);
                if (input != null && motor != null && targetLock != null && physical != null &&
                    boss != null && camera != null && calibration != null && wisp != null) break;
                yield return null;
            }

            if (input == null || motor == null || targetLock == null || physical == null ||
                boss == null || camera == null || calibration == null || wisp == null)
            {
                Debug.LogError("[Mindforge:V25] Sensory fidelity could not resolve canonical gameplay presentation dependencies.");
                yield break;
            }

            MindforgeDemoHudV17 v17Hud = FindObjectOfType<MindforgeDemoHudV17>(true);
            if (v17Hud != null) v17Hud.enabled = false;

            if (GetComponent<CombatVfxOrchestrator>() == null)
                gameObject.AddComponent<CombatVfxOrchestrator>();

            MindforgeDemoHudV25 hud = gameObject.AddComponent<MindforgeDemoHudV25>();
            hud.Configure(input, targetLock, calibration, wisp, buffs);

            MindforgeDiegeticGuideV25 guide = gameObject.AddComponent<MindforgeDiegeticGuideV25>();
            guide.Configure(input.transform, targetLock, calibration, wisp, buffs, camera);

            MindforgeLocomotionVfxV25 locomotion = gameObject.AddComponent<MindforgeLocomotionVfxV25>();
            locomotion.Configure(motor, calibration, wisp);

            MindforgeCameraImpactV25 impact = gameObject.AddComponent<MindforgeCameraImpactV25>();
            impact.Configure(camera, input, physical, boss, calibration, wisp);

            FracturedSignalFidelityV25 signal = boss.GetComponent<FracturedSignalFidelityV25>();
            if (signal == null) signal = boss.gameObject.AddComponent<FracturedSignalFidelityV25>();
            signal.Configure(boss, calibration, wisp);

            MindforgeSpatialAudioV25 audio = gameObject.AddComponent<MindforgeSpatialAudioV25>();
            audio.Configure(input.transform, physical, motor, boss.transform, calibration, wisp);

            Debug.Log(
                "[Mindforge:V25] Canonical sensory presentation installed: pooled combat/locomotion VFX, " +
                "bounded camera impact, fractured-signal depth shader, diegetic prompts, quiet HUD and spatial audio. " +
                "All systems are read-only with respect to gameplay and neural authority.");
        }
    }

    [DefaultExecutionOrder(860)]
    public sealed class MindforgeCameraImpactV25 : MonoBehaviour
    {
        private Camera _camera;
        private GuardianCombatInput _input;
        private GuardianSwordShieldController _physical;
        private FracturedSignalDirector _boss;
        private CombatantVitals _bossVitals;
        private AwakeningCalibrationDirector _calibration;
        private SoulWispController _wisp;
        private Vector3 _kick;
        private Vector3 _velocity;
        private float _alternator = 1f;
        private bool _subscribed;

        public void Configure(
            Camera camera,
            GuardianCombatInput input,
            GuardianSwordShieldController physical,
            FracturedSignalDirector boss,
            AwakeningCalibrationDirector calibration,
            SoulWispController wisp)
        {
            _camera = camera;
            _input = input;
            _physical = physical;
            _boss = boss;
            _bossVitals = boss != null ? boss.GetComponent<CombatantVitals>() : null;
            _calibration = calibration;
            _wisp = wisp;
            Subscribe();
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed || _physical == null || _boss == null || _bossVitals == null) return;
            _physical.SwordHit += OnSwordHit;
            _physical.PerfectGuard += OnPerfectGuard;
            _physical.GuardBroken += OnGuardBroken;
            _bossVitals.Damaged += OnBossDamaged;
            _boss.AttackFired += OnBossAttackFired;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (_physical != null)
            {
                _physical.SwordHit -= OnSwordHit;
                _physical.PerfectGuard -= OnPerfectGuard;
                _physical.GuardBroken -= OnGuardBroken;
            }
            if (_bossVitals != null) _bossVitals.Damaged -= OnBossDamaged;
            if (_boss != null) _boss.AttackFired -= OnBossAttackFired;
            _subscribed = false;
        }

        private void OnSwordHit(float damage, float neuralBonus)
        {
            if (NeuralVisualFieldActive()) return;
            Vector3 direction = _input != null ? _input.CurrentAimDirection : Vector3.forward;
            AddDirectionalKick(direction, neuralBonus > 0.001f ? 0.060f : 0.042f, 0.020f);
        }

        private void OnPerfectGuard()
        {
            if (NeuralVisualFieldActive()) return;
            Vector3 direction = _input != null ? -_input.CurrentAimDirection : Vector3.back;
            AddDirectionalKick(direction, 0.070f, 0.035f);
        }

        private void OnGuardBroken()
        {
            if (NeuralVisualFieldActive()) return;
            _kick += new Vector3(0.075f * _alternator, -0.045f, -0.070f);
            _alternator *= -1f;
        }

        private void OnBossDamaged(DamagePacket packet)
        {
            if (NeuralVisualFieldActive() || packet.Damage <= 0f) return;
            float magnitude = packet.Heavy ? 0.050f : 0.022f;
            _kick += new Vector3(magnitude * _alternator, magnitude * 0.28f, -magnitude * 0.45f);
            _alternator *= -1f;
        }

        private void OnBossAttackFired(string pattern, int count, bool heavy)
        {
            if (NeuralVisualFieldActive() || !heavy) return;
            _kick += new Vector3(0.025f * _alternator, 0.020f, -0.040f);
            _alternator *= -1f;
        }

        private void AddDirectionalKick(Vector3 worldDirection, float lateral, float depth)
        {
            if (_camera == null) return;
            Vector3 local = _camera.transform.InverseTransformDirection(worldDirection.sqrMagnitude > 0.001f
                ? worldDirection.normalized
                : Vector3.forward);
            _kick += new Vector3(local.x * lateral + lateral * 0.25f * _alternator, local.y * lateral * 0.35f, -depth);
            _kick = Vector3.ClampMagnitude(_kick, 0.115f);
            _alternator *= -1f;
        }

        private void LateUpdate()
        {
            if (_camera == null) return;
            if (NeuralVisualFieldActive())
            {
                _kick = Vector3.zero;
                _velocity = Vector3.zero;
                return;
            }

            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            _kick = Vector3.SmoothDamp(_kick, Vector3.zero, ref _velocity, 0.065f, Mathf.Infinity, dt);
            Vector3 offset = _camera.transform.right * _kick.x +
                             _camera.transform.up * _kick.y +
                             _camera.transform.forward * _kick.z;
            _camera.transform.position += offset;
        }

        private bool NeuralVisualFieldActive()
            => (_calibration != null && _calibration.CalibrationInProgress) ||
               (_wisp != null && (_wisp.CalibrationStimuliActive || _wisp.ResonanceWindowActive));
    }

    public sealed class MindforgeLocomotionVfxV25 : MonoBehaviour
    {
        private GuardianMotor _motor;
        private AwakeningCalibrationDirector _calibration;
        private SoulWispController _wisp;
        private PresentationFxPool _pool;
        private bool _subscribed;

        private static readonly Color DashColor = new Color(0.20f, 0.78f, 1.0f, 1f);
        private static readonly Color JumpColor = new Color(0.42f, 0.92f, 1.0f, 1f);
        private static readonly Color LandingColor = new Color(0.86f, 0.89f, 0.92f, 1f);

        public void Configure(GuardianMotor motor, AwakeningCalibrationDirector calibration, SoulWispController wisp)
        {
            _motor = motor;
            _calibration = calibration;
            _wisp = wisp;
            _pool = PresentationFxPool.GetOrCreate();
            Subscribe();
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed || _motor == null) return;
            _motor.DashStarted += OnDash;
            _motor.AirDashStarted += OnAirDash;
            _motor.Jumped += OnJump;
            _motor.DoubleJumped += OnDoubleJump;
            _motor.Landed += OnLanded;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _motor == null) return;
            _motor.DashStarted -= OnDash;
            _motor.AirDashStarted -= OnAirDash;
            _motor.Jumped -= OnJump;
            _motor.DoubleJumped -= OnDoubleJump;
            _motor.Landed -= OnLanded;
            _subscribed = false;
        }

        private void OnDash()
        {
            if (NeuralVisualFieldActive()) return;
            Vector3 p = _motor.transform.position + Vector3.up * 0.12f;
            _pool?.EmitRing(p, Vector3.up, DashColor, 0.28f, 1.45f, 0.26f, 0.045f);
            _pool?.EmitBurst(p, DashColor, 16, 3.7f, 0.075f);
        }

        private void OnAirDash()
        {
            if (NeuralVisualFieldActive()) return;
            Vector3 p = _motor.transform.position + Vector3.up * 0.70f;
            _pool?.EmitRing(p, _motor.transform.forward, DashColor, 0.25f, 1.10f, 0.22f, 0.040f);
            _pool?.EmitBurst(p, DashColor, 20, 4.4f, 0.070f);
        }

        private void OnJump()
        {
            if (NeuralVisualFieldActive()) return;
            Vector3 p = _motor.transform.position + Vector3.up * 0.08f;
            _pool?.EmitRing(p, Vector3.up, JumpColor, 0.20f, 0.88f, 0.20f, 0.030f);
        }

        private void OnDoubleJump()
        {
            if (NeuralVisualFieldActive()) return;
            Vector3 p = _motor.transform.position + Vector3.up * 0.60f;
            _pool?.EmitRing(p, Vector3.up, JumpColor, 0.28f, 1.25f, 0.26f, 0.045f);
            _pool?.EmitBurst(p, JumpColor, 18, 3.4f, 0.065f);
        }

        private void OnLanded(float impactSpeed)
        {
            if (NeuralVisualFieldActive() || impactSpeed < 3.2f) return;
            Vector3 p = _motor.transform.position + Vector3.up * 0.06f;
            float strength = Mathf.InverseLerp(3.2f, 14f, impactSpeed);
            _pool?.EmitRing(p, Vector3.up, LandingColor, 0.28f, Mathf.Lerp(0.75f, 1.65f, strength), 0.26f, 0.035f);
            _pool?.EmitBurst(p, LandingColor, Mathf.RoundToInt(Mathf.Lerp(8, 22, strength)), Mathf.Lerp(1.8f, 4.0f, strength), 0.055f);
        }

        private bool NeuralVisualFieldActive()
            => (_calibration != null && _calibration.CalibrationInProgress) ||
               (_wisp != null && (_wisp.CalibrationStimuliActive || _wisp.ResonanceWindowActive));
    }

    public sealed class FracturedSignalFidelityV25 : MonoBehaviour
    {
        private FracturedSignalDirector _director;
        private AwakeningCalibrationDirector _calibration;
        private SoulWispController _wisp;
        private Material _armor;
        private Material _edge;
        private Material _core;
        private Material _void;
        private bool _applied;

        public void Configure(FracturedSignalDirector director, AwakeningCalibrationDirector calibration, SoulWispController wisp)
        {
            _director = director;
            _calibration = calibration;
            _wisp = wisp;
        }

        private IEnumerator Start()
        {
            for (int frame = 0; frame < 180 && !_applied; frame++)
            {
                TryApply();
                if (!_applied) yield return null;
            }
        }

        private void Update()
        {
            if (!_applied) TryApply();
            if (!_applied) return;

            bool frozen = NeuralVisualFieldActive();
            float motion = frozen ? 0f : 1f;
            float phase = _director != null ? Mathf.Clamp(_director.Phase, 1, 3) : 1;
            SetMotion(_armor, motion, 0.016f + phase * 0.004f);
            SetMotion(_edge, motion, 0.026f + phase * 0.007f);
            SetMotion(_core, motion, 0.050f + phase * 0.010f);
            SetMotion(_void, 0f, 0f);
        }

        private void TryApply()
        {
            Transform visual = transform.Find(FracturedSignalCharacterV19.RootName);
            if (visual == null) return;
            Shader shader = Shader.Find("Mindforge/FracturedSignalV25");
            if (shader == null) return;

            if (_armor == null)
            {
                _armor = MakeMaterial(shader, "V25_Signal_Armor", new Color(0.025f, 0.030f, 0.055f), new Color(0.20f, 0.045f, 0.32f) * 0.75f, 0.020f, 3.2f, 0.85f);
                _edge = MakeMaterial(shader, "V25_Signal_Edge", new Color(0.085f, 0.030f, 0.11f), new Color(0.92f, 0.065f, 1.0f) * 1.65f, 0.034f, 4.5f, 1.45f);
                _core = MakeMaterial(shader, "V25_Signal_Core", new Color(0.12f, 0.008f, 0.035f), new Color(1.0f, 0.035f, 0.22f) * 2.35f, 0.065f, 5.5f, 1.75f);
                _void = MakeMaterial(shader, "V25_Signal_Void", new Color(0.004f, 0.006f, 0.012f), Color.black, 0f, 2f, 0f);
            }

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                string n = renderer.gameObject.name;
                if (n.IndexOf("Void", StringComparison.OrdinalIgnoreCase) >= 0)
                    renderer.sharedMaterial = _void;
                else if (n.IndexOf("Heart", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("FractureBlade", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("MaskScar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("Crown_03", StringComparison.OrdinalIgnoreCase) >= 0)
                    renderer.sharedMaterial = _core;
                else if (n.IndexOf("Fracture", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("Halo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("Crown", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("RightShoulder", StringComparison.OrdinalIgnoreCase) >= 0)
                    renderer.sharedMaterial = _edge;
                else
                    renderer.sharedMaterial = _armor;
            }

            _applied = renderers.Length > 0;
        }

        private static Material MakeMaterial(
            Shader shader,
            string name,
            Color baseColor,
            Color emission,
            float displacement,
            float frequency,
            float fresnel)
        {
            Material material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_EmissionColor", emission);
            material.SetFloat("_Displacement", displacement);
            material.SetFloat("_SpatialFrequency", frequency);
            material.SetFloat("_MotionScale", 1f);
            material.SetFloat("_FresnelPower", 3.1f);
            material.SetFloat("_FresnelStrength", fresnel);
            material.SetFloat("_Roughness", 0.38f);
            return material;
        }

        private static void SetMotion(Material material, float motion, float displacement)
        {
            if (material == null) return;
            material.SetFloat("_MotionScale", motion);
            material.SetFloat("_Displacement", displacement);
        }

        private bool NeuralVisualFieldActive()
            => (_calibration != null && _calibration.CalibrationInProgress) ||
               (_wisp != null && (_wisp.CalibrationStimuliActive || _wisp.ResonanceWindowActive));

        private void OnDestroy()
        {
            if (_armor != null) Destroy(_armor);
            if (_edge != null) Destroy(_edge);
            if (_core != null) Destroy(_core);
            if (_void != null) Destroy(_void);
        }
    }

    public sealed class MindforgeDiegeticGuideV25 : MonoBehaviour
    {
        private Transform _guardian;
        private GuardianTargetLock _targetLock;
        private AwakeningCalibrationDirector _calibration;
        private SoulWispController _wisp;
        private AuraBuffController _buffs;
        private Camera _camera;
        private TextMesh _targetText;
        private TextMesh _guardianText;

        public void Configure(
            Transform guardian,
            GuardianTargetLock targetLock,
            AwakeningCalibrationDirector calibration,
            SoulWispController wisp,
            AuraBuffController buffs,
            Camera camera)
        {
            _guardian = guardian;
            _targetLock = targetLock;
            _calibration = calibration;
            _wisp = wisp;
            _buffs = buffs;
            _camera = camera;
            Build();
        }

        private void Start() => Build();

        private void Build()
        {
            if (_targetText == null) _targetText = CreateText("V25_Target_Diegetic", new Color(0.78f, 0.90f, 1f, 0.94f));
            if (_guardianText == null) _guardianText = CreateText("V25_Guardian_Diegetic", new Color(0.36f, 0.82f, 1f, 0.95f));
        }

        private TextMesh CreateText(string name, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            TextMesh text = go.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 42;
            text.characterSize = 0.038f;
            text.color = color;
            text.text = string.Empty;
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            return text;
        }

        private void LateUpdate()
        {
            if (_targetText == null || _guardianText == null || _guardian == null || _camera == null) return;
            if (NeuralVisualFieldActive())
            {
                SetVisible(_targetText, false);
                SetVisible(_guardianText, false);
                return;
            }

            Transform target = _targetLock != null ? _targetLock.Target : null;
            bool locked = _targetLock != null && _targetLock.Locked && target != null;
            if (target != null)
            {
                _targetText.text = locked ? "TARGET LINKED" : "T  //  LOCK FRACTURED SIGNAL";
                _targetText.transform.position = target.position + Vector3.up * 3.25f;
                FaceCamera(_targetText.transform);
                SetVisible(_targetText, true);
            }
            else
            {
                SetVisible(_targetText, false);
            }

            string guardianMessage = string.Empty;
            if (_buffs != null && _buffs.ConcordActive) guardianMessage = "CONCORD  //  EXECUTE";
            else if (_buffs != null && _buffs.SightActive) guardianMessage = "SIGHT  //  BREAK POISE";
            else if (_buffs != null && _buffs.GuardActive) guardianMessage = "GUARD  //  COUNTER";
            else if (locked) guardianMessage = "V HOLD  //  CHANNEL WISP";

            if (!string.IsNullOrEmpty(guardianMessage))
            {
                _guardianText.text = guardianMessage;
                _guardianText.transform.position = _guardian.position + Vector3.up * 2.45f;
                FaceCamera(_guardianText.transform);
                SetVisible(_guardianText, true);
            }
            else
            {
                SetVisible(_guardianText, false);
            }
        }

        private void FaceCamera(Transform t)
        {
            Vector3 toward = _camera.transform.position - t.position;
            if (toward.sqrMagnitude > 0.001f) t.rotation = Quaternion.LookRotation(toward.normalized, Vector3.up);
        }

        private static void SetVisible(TextMesh text, bool visible)
        {
            Renderer renderer = text != null ? text.GetComponent<Renderer>() : null;
            if (renderer != null) renderer.enabled = visible;
        }

        private bool NeuralVisualFieldActive()
            => (_calibration != null && _calibration.CalibrationInProgress) ||
               (_wisp != null && (_wisp.CalibrationStimuliActive || _wisp.ResonanceWindowActive));
    }

    public sealed class MindforgeDemoHudV25 : MonoBehaviour
    {
        private GuardianCombatInput _input;
        private GuardianTargetLock _targetLock;
        private AwakeningCalibrationDirector _calibration;
        private SoulWispController _wisp;
        private AuraBuffController _buffs;
        private CombatantVitals _guardianVitals;
        private GuardianStamina _stamina;
        private FluxMeter _flux;
        private GUIStyle _title;
        private GUIStyle _small;
        private GUIStyle _center;

        public void Configure(
            GuardianCombatInput input,
            GuardianTargetLock targetLock,
            AwakeningCalibrationDirector calibration,
            SoulWispController wisp,
            AuraBuffController buffs)
        {
            _input = input;
            _targetLock = targetLock;
            _calibration = calibration;
            _wisp = wisp;
            _buffs = buffs;
            Resolve();
        }

        private void Update() => Resolve();

        private void Resolve()
        {
            if (_input == null) _input = FindObjectOfType<GuardianCombatInput>(true);
            if (_input == null) return;
            if (_targetLock == null) _targetLock = _input.GetComponent<GuardianTargetLock>();
            if (_guardianVitals == null) _guardianVitals = _input.GetComponent<CombatantVitals>();
            if (_stamina == null) _stamina = _input.GetComponent<GuardianStamina>();
            if (_flux == null) _flux = _input.GetComponent<FluxMeter>();
            if (_buffs == null) _buffs = _input.GetComponent<AuraBuffController>();
            if (_calibration == null) _calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
            if (_wisp == null) _wisp = FindObjectOfType<SoulWispController>(true);
        }

        private void OnGUI()
        {
            if (_guardianVitals == null) return;
            EnsureStyles();
            float scale = Mathf.Clamp(Screen.height / 1080f, 0.78f, 1.25f);
            DrawGuardian(scale);
            DrawBoss(scale);
            DrawNeuralChip(scale);
            DrawNeuralInstruction(scale);
        }

        private void DrawGuardian(float scale)
        {
            float x = 24f * scale;
            float y = 22f * scale;
            float w = 274f * scale;
            Rect panel = new Rect(x, y, w, 72f * scale);
            Fill(panel, new Color(0.018f, 0.026f, 0.040f, 0.74f));
            Stroke(panel, new Color(0.46f, 0.68f, 0.82f, 0.28f), 1f);
            GUI.Label(new Rect(x + 10f * scale, y + 3f * scale, w - 20f * scale, 18f * scale), "GUARDIAN // LINK 01", _title);
            DrawBar(new Rect(x + 10f * scale, y + 26f * scale, w - 20f * scale, 8f * scale), Ratio(_guardianVitals.Health, _guardianVitals.MaxHealth), new Color(0.92f, 0.34f, 0.40f, 0.96f));
            DrawBar(new Rect(x + 10f * scale, y + 42f * scale, w - 20f * scale, 5f * scale), _stamina != null ? _stamina.Ratio : 0f, new Color(0.50f, 0.92f, 0.74f, 0.92f));
            DrawBar(new Rect(x + 10f * scale, y + 55f * scale, w - 20f * scale, 4f * scale), _flux != null ? Ratio(_flux.Value, _flux.Max) : 0f, new Color(0.24f, 0.74f, 1.0f, 0.96f));
        }

        private void DrawBoss(float scale)
        {
            CombatantVitals target = ResolveTargetVitals();
            if (target == null) return;
            float w = Mathf.Min(390f * scale, Screen.width * 0.38f);
            float x = (Screen.width - w) * 0.5f;
            float y = 23f * scale;
            Rect panel = new Rect(x, y, w, 43f * scale);
            Fill(panel, new Color(0.020f, 0.020f, 0.036f, 0.66f));
            Stroke(panel, new Color(0.90f, 0.20f, 0.76f, 0.25f), 1f);
            GUI.Label(new Rect(x, y + 1f * scale, w, 17f * scale), "THE FRACTURED SIGNAL", _center);
            DrawBar(new Rect(x + 12f * scale, y + 25f * scale, w - 24f * scale, 7f * scale), Ratio(target.Health, target.MaxHealth), new Color(0.94f, 0.10f, 0.48f, 0.96f));
        }

        private void DrawNeuralChip(float scale)
        {
            string state;
            Color accent;
            if (_calibration != null && _calibration.ControllerOnlyQualificationActive)
            {
                state = "BCI // SIMULATION";
                accent = new Color(0.68f, 0.82f, 0.92f, 0.94f);
            }
            else if (_calibration != null && _calibration.CalibrationReady)
            {
                state = "NEURAL // READY";
                accent = new Color(0.32f, 0.94f, 0.72f, 0.96f);
            }
            else if (_calibration != null && _calibration.CalibrationInProgress)
            {
                state = "NEURAL // CALIBRATING";
                accent = new Color(0.36f, 0.72f, 1.0f, 0.96f);
            }
            else
            {
                state = "NEURAL // ATTUNE";
                accent = new Color(0.46f, 0.70f, 0.92f, 0.90f);
            }

            float w = 178f * scale;
            Rect chip = new Rect(Screen.width - w - 24f * scale, 23f * scale, w, 26f * scale);
            Fill(chip, new Color(0.018f, 0.026f, 0.040f, 0.70f));
            Stroke(chip, new Color(accent.r, accent.g, accent.b, 0.30f), 1f);
            Color before = GUI.color;
            GUI.color = accent;
            GUI.Label(chip, state, _center);
            GUI.color = before;
        }

        private void DrawNeuralInstruction(float scale)
        {
            string text = null;
            if (_wisp != null && _wisp.ResonanceWindowActive)
                text = "NEURAL WINDOW  //  HOLD GAZE ON THE CODED CORES";
            else if (_calibration != null && _calibration.CalibrationInProgress)
                text = "CALIBRATION  //  FOLLOW THE CODED CORES";
            if (string.IsNullOrEmpty(text)) return;

            float w = Mathf.Min(540f * scale, Screen.width * 0.58f);
            Rect panel = new Rect((Screen.width - w) * 0.5f, Screen.height - 60f * scale, w, 30f * scale);
            Fill(panel, new Color(0.012f, 0.020f, 0.034f, 0.82f));
            Stroke(panel, new Color(0.32f, 0.72f, 1.0f, 0.42f), 1f);
            GUI.Label(panel, text, _center);
        }

        private CombatantVitals ResolveTargetVitals()
        {
            Transform target = _targetLock != null ? _targetLock.Target : null;
            if (target != null)
            {
                CombatantVitals direct = target.GetComponentInParent<CombatantVitals>();
                if (direct != null && direct.Team == CombatTeam.Enemy && direct.IsAlive) return direct;
            }
            CombatantVitals[] all = FindObjectsOfType<CombatantVitals>(true);
            CombatantVitals strongest = null;
            for (int i = 0; i < all.Length; i++)
            {
                CombatantVitals candidate = all[i];
                if (candidate == null || candidate.Team != CombatTeam.Enemy || !candidate.IsAlive || !candidate.gameObject.activeInHierarchy) continue;
                if (strongest == null || candidate.MaxHealth > strongest.MaxHealth) strongest = candidate;
            }
            return strongest;
        }

        private void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.88f, 0.94f, 0.98f, 0.96f) },
            };
            _small = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.68f, 0.76f, 0.84f, 0.90f) },
            };
            _center = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
            };
        }

        private static float Ratio(float value, float maximum)
            => maximum > 0.001f ? Mathf.Clamp01(value / maximum) : 0f;

        private static void DrawBar(Rect rect, float ratio, Color color)
        {
            Fill(rect, new Color(0.06f, 0.08f, 0.11f, 0.88f));
            Rect fill = rect;
            fill.width *= Mathf.Clamp01(ratio);
            Fill(fill, color);
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void Stroke(Rect rect, Color color, float width)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, width), color);
            Fill(new Rect(rect.x, rect.yMax - width, rect.width, width), color);
            Fill(new Rect(rect.x, rect.y, width, rect.height), color);
            Fill(new Rect(rect.xMax - width, rect.y, width, rect.height), color);
        }
    }

    public sealed class MindforgeSpatialAudioV25 : MonoBehaviour
    {
        private Transform _guardian;
        private GuardianSwordShieldController _physical;
        private GuardianMotor _motor;
        private Transform _boss;
        private AwakeningCalibrationDirector _calibration;
        private SoulWispController _wisp;
        private AudioSource _bossHum;
        private AudioSource _playerFx;
        private AudioClip _humClip;
        private AudioClip _hitClip;
        private AudioClip _dashClip;
        private bool _subscribed;

        public void Configure(
            Transform guardian,
            GuardianSwordShieldController physical,
            GuardianMotor motor,
            Transform boss,
            AwakeningCalibrationDirector calibration,
            SoulWispController wisp)
        {
            _guardian = guardian;
            _physical = physical;
            _motor = motor;
            _boss = boss;
            _calibration = calibration;
            _wisp = wisp;
            BuildAudio();
            Subscribe();
        }

        private void BuildAudio()
        {
            if (_bossHum != null) return;
            _humClip = BuildTone("V25_FracturedSignal_Hum", 2.0f, 55f, 82.5f, 0.18f);
            _hitClip = BuildTone("V25_Aetherblade_Impact", 0.09f, 330f, 610f, 0.32f);
            _dashClip = BuildTone("V25_PhaseDash", 0.12f, 105f, 210f, 0.24f);

            GameObject bossAudio = new GameObject("V25_FracturedSignal_SpatialHum");
            bossAudio.transform.SetParent(_boss, false);
            bossAudio.transform.localPosition = Vector3.up * 1.1f;
            _bossHum = bossAudio.AddComponent<AudioSource>();
            _bossHum.clip = _humClip;
            _bossHum.loop = true;
            _bossHum.playOnAwake = false;
            _bossHum.spatialBlend = 1f;
            _bossHum.minDistance = 3.5f;
            _bossHum.maxDistance = 34f;
            _bossHum.rolloffMode = AudioRolloffMode.Logarithmic;
            _bossHum.volume = 0.075f;
            _bossHum.Play();

            GameObject playerAudio = new GameObject("V25_Guardian_ImpactAudio");
            playerAudio.transform.SetParent(_guardian, false);
            _playerFx = playerAudio.AddComponent<AudioSource>();
            _playerFx.playOnAwake = false;
            _playerFx.spatialBlend = 0.35f;
            _playerFx.volume = 0.13f;
        }

        private void Subscribe()
        {
            if (_subscribed || _physical == null || _motor == null) return;
            _physical.SwordHit += OnSwordHit;
            _physical.PerfectGuard += OnPerfectGuard;
            _motor.DashStarted += OnDash;
            _motor.AirDashStarted += OnDash;
            _subscribed = true;
        }

        private void OnEnable() => Subscribe();

        private void OnDisable()
        {
            if (!_subscribed) return;
            if (_physical != null)
            {
                _physical.SwordHit -= OnSwordHit;
                _physical.PerfectGuard -= OnPerfectGuard;
            }
            if (_motor != null)
            {
                _motor.DashStarted -= OnDash;
                _motor.AirDashStarted -= OnDash;
            }
            _subscribed = false;
        }

        private void Update()
        {
            bool neural = NeuralVisualFieldActive();
            if (_bossHum != null) _bossHum.volume = neural ? 0f : 0.075f;
            if (_playerFx != null) _playerFx.volume = neural ? 0f : 0.13f;
        }

        private void OnSwordHit(float damage, float neuralBonus)
        {
            if (!NeuralVisualFieldActive() && _playerFx != null && _hitClip != null)
                _playerFx.PlayOneShot(_hitClip, neuralBonus > 0.001f ? 1.15f : 0.88f);
        }

        private void OnPerfectGuard()
        {
            if (!NeuralVisualFieldActive() && _playerFx != null && _hitClip != null)
                _playerFx.PlayOneShot(_hitClip, 1.20f);
        }

        private void OnDash()
        {
            if (!NeuralVisualFieldActive() && _playerFx != null && _dashClip != null)
                _playerFx.PlayOneShot(_dashClip, 0.72f);
        }

        private bool NeuralVisualFieldActive()
            => (_calibration != null && _calibration.CalibrationInProgress) ||
               (_wisp != null && (_wisp.CalibrationStimuliActive || _wisp.ResonanceWindowActive));

        private static AudioClip BuildTone(string name, float seconds, float f0, float f1, float gain)
        {
            const int sampleRate = 22050;
            int samples = Mathf.Max(128, Mathf.CeilToInt(seconds * sampleRate));
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float u = i / (float)Mathf.Max(1, samples - 1);
                float attack = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u * 18f));
                float release = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - u) * 10f));
                float envelope = Mathf.Min(attack, release);
                float signal = Mathf.Sin(t * f0 * Mathf.PI * 2f) * 0.68f +
                               Mathf.Sin(t * f1 * Mathf.PI * 2f) * 0.32f;
                data[i] = signal * gain * envelope;
            }
            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (_humClip != null) Destroy(_humClip);
            if (_hitClip != null) Destroy(_hitClip);
            if (_dashClip != null) Destroy(_dashClip);
        }
    }
}
