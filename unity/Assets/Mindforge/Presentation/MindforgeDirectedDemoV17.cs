using System;
using System.Collections;
using UnityEngine;
using Mindforge.Calibration;
using Mindforge.Combat;
using Mindforge.SoulWisp;

namespace Mindforge.Presentation
{
    /// <summary>
    /// V0.17 directed-demo composition for the canonical Mindforge scene.
    ///
    /// This layer owns presentation only. It reads conventional target-lock, combat-input,
    /// calibration and Wisp state, then installs a closer fixed-FOV gameplay camera,
    /// one canonical demo HUD and a non-emissive target-presence ring. It never moves the
    /// Guardian, changes combat state, creates/cycles target lock, emits neural evidence,
    /// changes VEP phase/frequency or changes collision.
    /// </summary>
    [DefaultExecutionOrder(-42)]
    public sealed class MindforgeDirectedDemoV17 : MonoBehaviour
    {
        public const string RootName = "Mindforge_DirectedDemo_V17";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<MindforgeDemoV11Marker>(true) == null) return;
            if (FindObjectOfType<MindforgeDirectedDemoV17>(true) != null) return;
            new GameObject(RootName).AddComponent<MindforgeDirectedDemoV17>();
        }

        private IEnumerator Start()
        {
            GuardianCombatInput input = null;
            GuardianTargetLock targetLock = null;
            Camera camera = null;
            AwakeningCalibrationDirector calibration = null;
            SoulWispController wisp = null;
            AuraBuffController buffs = null;

            for (int frame = 0; frame < 240; frame++)
            {
                if (input == null) input = FindObjectOfType<GuardianCombatInput>(true);
                if (input != null && targetLock == null) targetLock = input.GetComponent<GuardianTargetLock>();
                if (camera == null) camera = Camera.main;
                if (calibration == null) calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
                if (wisp == null) wisp = FindObjectOfType<SoulWispController>(true);
                if (input != null && buffs == null) buffs = input.GetComponent<AuraBuffController>();
                if (input != null && targetLock != null && camera != null && calibration != null && wisp != null) break;
                yield return null;
            }

            if (input == null || targetLock == null || camera == null || calibration == null || wisp == null)
            {
                Debug.LogError("[Mindforge:V17] Directed demo could not resolve Guardian input, target lock, camera, calibration or Wisp.");
                Destroy(gameObject);
                yield break;
            }

            // V0.17 replaces, rather than layers over, the old V0.11 runtime HUD.
            MindforgeDemoHudV11 legacyHud = FindObjectOfType<MindforgeDemoHudV11>(true);
            if (legacyHud != null) legacyHud.enabled = false;

            GameObject cameraOwner = camera.transform.root.gameObject;
            MindforgeGameplayCameraV17 cameraV17 = cameraOwner.GetComponent<MindforgeGameplayCameraV17>();
            if (cameraV17 == null) cameraV17 = cameraOwner.AddComponent<MindforgeGameplayCameraV17>();
            cameraV17.Configure(input.transform, input, targetLock, camera, calibration, wisp);

            MindforgeTargetPresenceV17 targetPresence = gameObject.AddComponent<MindforgeTargetPresenceV17>();
            targetPresence.Configure(targetLock, calibration, wisp);

            MindforgeDemoHudV17 hud = gameObject.AddComponent<MindforgeDemoHudV17>();
            hud.Configure(input, targetLock, calibration, wisp, buffs);

            Debug.Log(
                "[Mindforge:V17] Directed demo installed: closer fixed-FOV ARPG framing, " +
                "single canonical HUD, target presence and neural-window camera stabilization. " +
                "Gameplay and BCI authority remain unchanged.");
        }
    }

    /// <summary>
    /// Gameplay-only successor to the V0.11 camera. It waits until the intro/reveal has
    /// actually returned combat input, then transfers presentation ownership from the legacy
    /// camera. FOV remains fixed so coded-core angular geometry is not modulated by combat.
    /// During an armed Wisp window user orbit and target-driven yaw changes freeze, reducing
    /// avoidable background optic flow while the coded cores own visual attention.
    /// </summary>
    [DefaultExecutionOrder(620)]
    public sealed class MindforgeGameplayCameraV17 : MonoBehaviour
    {
        private const float FixedFov = 56f;
        private const float PivotHeight = 1.56f;
        private const float Pitch = 18.0f;
        private const float FreeDistance = 6.65f;
        private const float LockDistance = 7.75f;
        private const float FreeShoulder = 0.58f;
        private const float LockShoulder = 0.26f;
        private const float FreeLookAhead = 5.4f;
        private const float LockTargetHeight = 1.05f;
        private const float LockLookWeight = 0.57f;
        private const float CollisionPadding = 0.28f;
        private const float CollisionSafetyEpsilon = 0.02f;

        private Transform _guardian;
        private GuardianCombatInput _input;
        private GuardianTargetLock _targetLock;
        private Camera _camera;
        private AwakeningCalibrationDirector _calibration;
        private SoulWispController _wisp;
        private MindforgeDemoCameraV11 _legacy;
        private readonly RaycastHit[] _hits = new RaycastHit[18];
        private Vector3 _positionVelocity;
        private float _yaw;
        private bool _active;
        private bool _disabledLegacy;
        private bool _initialized;

        public bool DirectedCameraActive => _active;
        public bool NeuralStabilizationActive => _active && NeuralVisualFieldActive();

        public void Configure(
            Transform guardian,
            GuardianCombatInput input,
            GuardianTargetLock targetLock,
            Camera camera,
            AwakeningCalibrationDirector calibration,
            SoulWispController wisp)
        {
            _guardian = guardian;
            _input = input;
            _targetLock = targetLock;
            _camera = camera;
            _calibration = calibration;
            _wisp = wisp;
            _legacy = camera != null ? camera.transform.root.GetComponent<MindforgeDemoCameraV11>() : null;
        }

        private void Update()
        {
            if (!_active)
            {
                if (_input != null && _input.CombatActionsEnabled) ActivateGameplayCamera();
                return;
            }

            bool locked = _targetLock != null && _targetLock.Locked && _targetLock.Target != null;
            if (NeuralVisualFieldActive()) return;

            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            if (!locked)
            {
                float orbit = Input.GetAxis("Mouse X") * 1.45f;
                if (Input.GetKey(KeyCode.LeftArrow)) orbit -= 70f * dt;
                if (Input.GetKey(KeyCode.RightArrow)) orbit += 70f * dt;
                _yaw = Mathf.Clamp(_yaw + orbit, -78f, 78f);
            }
            else
            {
                Vector3 pivot = _guardian.position + Vector3.up * PivotHeight;
                Vector3 targetPoint = _targetLock.Target.position + Vector3.up * LockTargetHeight;
                Vector3 flat = Vector3.ProjectOnPlane(targetPoint - pivot, Vector3.up);
                if (flat.sqrMagnitude > 0.001f)
                {
                    float targetYaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
                    _yaw = Mathf.LerpAngle(_yaw, targetYaw, 1f - Mathf.Exp(-13f * dt));
                }
            }
        }

        private void LateUpdate()
        {
            if (!_active || _guardian == null || _camera == null) return;
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            if (dt <= 0f) return;

            bool locked = _targetLock != null && _targetLock.Locked && _targetLock.Target != null;
            bool neural = NeuralVisualFieldActive();
            Vector3 pivot = _guardian.position + Vector3.up * PivotHeight;
            float distance = locked ? LockDistance : FreeDistance;
            float shoulder = locked ? LockShoulder : FreeShoulder;

            Quaternion orbit = Quaternion.Euler(Pitch, _yaw, 0f);
            Vector3 right = Quaternion.Euler(0f, _yaw, 0f) * Vector3.right;
            Vector3 desired = pivot + orbit * Vector3.back * distance + right * shoulder;
            desired = ResolveCollision(pivot, desired);

            Vector3 lookPoint;
            if (locked)
            {
                Vector3 targetPoint = _targetLock.Target.position + Vector3.up * LockTargetHeight;
                lookPoint = Vector3.Lerp(pivot, targetPoint, LockLookWeight);
            }
            else
            {
                lookPoint = pivot + orbit * Vector3.forward * FreeLookAhead;
            }

            Vector3 look = lookPoint - desired;
            if (look.sqrMagnitude < 0.001f) look = _guardian.forward;
            Quaternion desiredRotation = Quaternion.LookRotation(look.normalized, Vector3.up);

            float positionSmooth = neural ? 0.115f : 0.044f;
            float rotationSharpness = neural ? 8.5f : 22f;
            if (!_initialized)
            {
                transform.position = desired;
                transform.rotation = desiredRotation;
                _initialized = true;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    desired,
                    ref _positionVelocity,
                    positionSmooth,
                    Mathf.Infinity,
                    dt);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    desiredRotation,
                    1f - Mathf.Exp(-rotationSharpness * dt));
            }

            _camera.fieldOfView = FixedFov;
            _camera.nearClipPlane = 0.06f;
            _camera.farClipPlane = 420f;
        }

        private void ActivateGameplayCamera()
        {
            if (_active || _guardian == null || _camera == null) return;
            Vector3 pivot = _guardian.position + Vector3.up * PivotHeight;
            Vector3 flatBack = Vector3.ProjectOnPlane(_camera.transform.position - pivot, Vector3.up);
            if (flatBack.sqrMagnitude > 0.001f)
            {
                flatBack.Normalize();
                _yaw = Mathf.Atan2(-flatBack.x, -flatBack.z) * Mathf.Rad2Deg;
            }
            _yaw = Mathf.Clamp(_yaw, -78f, 78f);

            if (_legacy != null && _legacy.enabled)
            {
                _legacy.enabled = false;
                _disabledLegacy = true;
            }

            _initialized = true;
            _active = true;
            Debug.Log("[Mindforge:V17] Gameplay camera authority transferred from V0.11 presentation to fixed-FOV V0.17 framing.");
        }

        private bool NeuralVisualFieldActive()
        {
            return (_calibration != null && _calibration.CalibrationInProgress) ||
                   (_wisp != null && (_wisp.CalibrationStimuliActive || _wisp.ResonanceWindowActive));
        }

        private Vector3 ResolveCollision(Vector3 pivot, Vector3 desired)
        {
            Vector3 delta = desired - pivot;
            float distance = delta.magnitude;
            if (distance <= 0.01f) return desired;
            Vector3 direction = delta / distance;

            int count = Physics.SphereCastNonAlloc(
                pivot,
                0.24f,
                direction,
                _hits,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore);

            bool found = false;
            float nearest = distance;
            for (int i = 0; i < count; i++)
            {
                Collider collider = _hits[i].collider;
                if (collider == null || IsGuardian(collider.transform) || IsDynamicActor(collider)) continue;
                if (_hits[i].distance < 0f || _hits[i].distance >= nearest) continue;
                nearest = _hits[i].distance;
                found = true;
            }
            if (!found) return desired;

            // Collision is a hard upper bound on camera distance. A preferred framing
            // distance must never push the camera back through a nearer wall or column.
            float clearance = Mathf.Max(0f, nearest - CollisionSafetyEpsilon);
            float resolved = Mathf.Max(0f, nearest - CollisionPadding);
            resolved = Mathf.Min(resolved, clearance);
            return pivot + direction * resolved;
        }

        private bool IsGuardian(Transform candidate)
            => candidate != null && _guardian != null && (candidate == _guardian || candidate.IsChildOf(_guardian));

        private static bool IsDynamicActor(Collider collider)
            => collider != null && collider.GetComponentInParent<CombatantVitals>() != null;

        private void OnDisable()
        {
            if (_disabledLegacy && _legacy != null) _legacy.enabled = true;
            _disabledLegacy = false;
            _active = false;
        }
    }

    /// <summary>
    /// Static target-presence marker. The ring is presentation only, contains no collider,
    /// and is hidden for the entire neural visual-field interval so it cannot become a second
    /// salient target beside the 10/12 Hz coded cores.
    /// </summary>
    public sealed class MindforgeTargetPresenceV17 : MonoBehaviour
    {
        private GuardianTargetLock _targetLock;
        private AwakeningCalibrationDirector _calibration;
        private SoulWispController _wisp;
        private LineRenderer _ring;
        private Material _material;

        public bool Visible => _ring != null && _ring.enabled;

        public void Configure(
            GuardianTargetLock targetLock,
            AwakeningCalibrationDirector calibration,
            SoulWispController wisp)
        {
            _targetLock = targetLock;
            _calibration = calibration;
            _wisp = wisp;
        }

        private void Start() => BuildRing();

        private void LateUpdate()
        {
            if (_ring == null) return;
            if (NeuralVisualFieldActive())
            {
                _ring.enabled = false;
                return;
            }

            Transform target = _targetLock != null && _targetLock.Locked ? _targetLock.Target : null;
            if (target == null)
            {
                _ring.enabled = false;
                return;
            }

            _ring.enabled = true;
            transform.position = target.position + Vector3.up * 0.055f;
            transform.rotation = Quaternion.identity;
        }

        private void BuildRing()
        {
            if (_ring != null) return;
            _ring = gameObject.AddComponent<LineRenderer>();
            _ring.useWorldSpace = false;
            _ring.loop = true;
            _ring.positionCount = 64;
            _ring.widthMultiplier = 0.035f;
            _ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ring.receiveShadows = false;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                _material = new Material(shader) { name = "V17_TargetPresence" };
                _ring.sharedMaterial = _material;
            }
            Color color = new Color(0.74f, 0.82f, 0.88f, 0.58f);
            _ring.startColor = color;
            _ring.endColor = color;

            const float radius = 1.42f;
            for (int i = 0; i < _ring.positionCount; i++)
            {
                float a = i / (float)_ring.positionCount * Mathf.PI * 2f;
                _ring.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
            _ring.enabled = false;
        }

        private bool NeuralVisualFieldActive()
        {
            return (_calibration != null && _calibration.CalibrationInProgress) ||
                   (_wisp != null && (_wisp.CalibrationStimuliActive || _wisp.ResonanceWindowActive));
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }

    /// <summary>
    /// Canonical read-only HUD for the Latest demo. It replaces the legacy V0.11 HUD and
    /// puts conventional combat, target state and neural affordance into one quiet hierarchy.
    /// </summary>
    public sealed class MindforgeDemoHudV17 : MonoBehaviour
    {
        private GuardianCombatInput _input;
        private GuardianTargetLock _targetLock;
        private AwakeningCalibrationDirector _calibration;
        private SoulWispController _wisp;
        private AuraBuffController _buffs;
        private CombatantVitals _guardianVitals;
        private GuardianStamina _stamina;
        private FluxMeter _flux;
        private GUIStyle _label;
        private GUIStyle _small;
        private GUIStyle _center;
        private GUIStyle _strong;

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
            ResolveGuardianState();
        }

        private void Update()
        {
            if (_guardianVitals == null || _stamina == null || _flux == null) ResolveGuardianState();
        }

        private void OnGUI()
        {
            if (_guardianVitals == null) return;
            EnsureStyles();
            float scale = Mathf.Clamp(Screen.height / 1080f, 0.78f, 1.25f);
            DrawGuardianPanel(scale);
            DrawTargetPanel(scale);
            DrawNeuralChip(scale);
            DrawContextGuide(scale);
        }

        private void DrawGuardianPanel(float scale)
        {
            float x = 26f * scale;
            float y = 24f * scale;
            float w = 300f * scale;
            float h = 88f * scale;
            Rect panel = new Rect(x, y, w, h);
            Fill(panel, new Color(0.015f, 0.020f, 0.032f, 0.78f));
            Stroke(panel, new Color(0.72f, 0.80f, 0.88f, 0.28f), 1f);
            GUI.Label(new Rect(x + 12f * scale, y + 5f * scale, w - 24f * scale, 18f * scale), "GUARDIAN", _strong);
            DrawBar(new Rect(x + 12f * scale, y + 29f * scale, w - 24f * scale, 9f * scale), Ratio(_guardianVitals.Health, _guardianVitals.MaxHealth), new Color(0.88f, 0.28f, 0.36f, 0.96f));
            DrawBar(new Rect(x + 12f * scale, y + 48f * scale, w - 24f * scale, 6f * scale), _stamina != null ? _stamina.Ratio : 0f, new Color(0.44f, 0.84f, 0.66f, 0.94f));
            DrawBar(new Rect(x + 12f * scale, y + 64f * scale, w - 24f * scale, 5f * scale), _flux != null ? Ratio(_flux.Value, _flux.Max) : 0f, new Color(0.36f, 0.70f, 0.96f, 0.92f));
            GUI.Label(new Rect(x + 12f * scale, y + 70f * scale, w - 24f * scale, 14f * scale),
                $"HP {_guardianVitals.Health:0}/{_guardianVitals.MaxHealth:0}   ·   ENDURANCE   ·   FLUX", _small);
        }

        private void DrawTargetPanel(float scale)
        {
            CombatantVitals target = ResolveTargetVitals();
            if (target == null) return;
            float w = Mathf.Min(430f * scale, Screen.width * 0.40f);
            float x = (Screen.width - w) * 0.5f;
            float y = 24f * scale;
            Rect panel = new Rect(x, y, w, 54f * scale);
            Fill(panel, new Color(0.015f, 0.020f, 0.032f, 0.70f));
            GUI.Label(new Rect(x + 10f * scale, y + 3f * scale, w - 20f * scale, 20f * scale), "THE FRACTURED SIGNAL", _center);
            DrawBar(new Rect(x + 12f * scale, y + 30f * scale, w - 24f * scale, 8f * scale), Ratio(target.Health, target.MaxHealth), new Color(0.78f, 0.16f, 0.30f, 0.96f));
            string lockState = _targetLock != null && _targetLock.Locked ? "TARGET LOCKED" : "T · LOCK TARGET";
            GUI.Label(new Rect(x + 10f * scale, y + 39f * scale, w - 20f * scale, 13f * scale), lockState, _small);
        }

        private void DrawNeuralChip(float scale)
        {
            string state;
            Color color;
            if (_calibration != null && _calibration.ControllerOnlyQualificationActive)
            {
                state = "BCI SIMULATION";
                color = new Color(0.78f, 0.82f, 0.88f, 0.90f);
            }
            else if (_calibration != null && _calibration.CalibrationReady)
            {
                state = "NEURAL LINK · READY";
                color = new Color(0.28f, 0.92f, 0.70f, 0.95f);
            }
            else if (_calibration != null && _calibration.CalibrationInProgress)
            {
                state = "NEURAL LINK · CALIBRATING";
                color = new Color(0.36f, 0.70f, 0.98f, 0.92f);
            }
            else
            {
                state = "NEURAL LINK · ATTUNE";
                color = new Color(0.52f, 0.72f, 0.94f, 0.86f);
            }

            float w = 190f * scale;
            float x = Screen.width - w - 26f * scale;
            Rect chip = new Rect(x, 24f * scale, w, 28f * scale);
            Fill(chip, new Color(0.015f, 0.020f, 0.032f, 0.72f));
            Color before = GUI.color;
            GUI.color = color;
            GUI.Label(chip, state, _center);
            GUI.color = before;
        }

        private void DrawContextGuide(float scale)
        {
            if (_input == null || !_input.CombatActionsEnabled) return;
            string message;
            Color accent = new Color(0.78f, 0.84f, 0.90f, 0.94f);

            if (_wisp != null && _wisp.ResonanceWindowActive)
            {
                message = "NEURAL WINDOW  ·  KEEP GAZE ON BLUE / GREEN";
                accent = new Color(0.48f, 0.82f, 1.0f, 0.96f);
            }
            else if (_buffs != null && _buffs.ConcordActive)
            {
                message = "CONCORD  ·  EXECUTE THE OPENING";
                accent = new Color(0.88f, 0.76f, 0.36f, 0.96f);
            }
            else if (_buffs != null && _buffs.SightActive)
            {
                message = "SIGHT  ·  BREAK POISE · PRESS THE OPENING";
                accent = new Color(0.32f, 0.68f, 1.0f, 0.96f);
            }
            else if (_buffs != null && _buffs.GuardActive)
            {
                message = "GUARD  ·  COUNTER THE NEXT THREAT";
                accent = new Color(0.30f, 0.94f, 0.62f, 0.96f);
            }
            else if (_targetLock == null || !_targetLock.Locked)
            {
                message = "T  ·  LOCK THE FRACTURED SIGNAL";
            }
            else
            {
                message = "V HOLD  ·  CHANNEL WISP";
            }

            float w = Mathf.Min(560f * scale, Screen.width * 0.55f);
            float h = 34f * scale;
            Rect panel = new Rect((Screen.width - w) * 0.5f, Screen.height - h - 26f * scale, w, h);
            Fill(panel, new Color(0.012f, 0.018f, 0.030f, 0.76f));
            Stroke(panel, new Color(accent.r, accent.g, accent.b, 0.34f), 1f);
            Color before = GUI.color;
            GUI.color = accent;
            GUI.Label(panel, message, _center);
            GUI.color = before;
        }

        private CombatantVitals ResolveTargetVitals()
        {
            Transform target = _targetLock != null && _targetLock.Target != null ? _targetLock.Target : null;
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

        private void ResolveGuardianState()
        {
            if (_input == null) _input = FindObjectOfType<GuardianCombatInput>(true);
            if (_input == null) return;
            if (_targetLock == null) _targetLock = _input.GetComponent<GuardianTargetLock>();
            if (_buffs == null) _buffs = _input.GetComponent<AuraBuffController>();
            if (_guardianVitals == null) _guardianVitals = _input.GetComponent<CombatantVitals>();
            if (_stamina == null) _stamina = _input.GetComponent<GuardianStamina>();
            if (_flux == null) _flux = _input.GetComponent<FluxMeter>();
            if (_calibration == null) _calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
            if (_wisp == null) _wisp = FindObjectOfType<SoulWispController>(true);
        }

        private void EnsureStyles()
        {
            if (_label != null) return;
            _label = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.90f, 0.93f, 0.97f, 0.96f) },
                alignment = TextAnchor.MiddleLeft,
            };
            _small = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                normal = { textColor = new Color(0.72f, 0.78f, 0.84f, 0.90f) },
                alignment = TextAnchor.MiddleCenter,
            };
            _center = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter,
            };
            _strong = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.97f, 0.99f, 0.98f) },
                alignment = TextAnchor.MiddleLeft,
            };
        }

        private static float Ratio(float value, float max) => max > 0f ? Mathf.Clamp01(value / max) : 0f;

        private static void DrawBar(Rect rect, float ratio, Color color)
        {
            Fill(rect, new Color(0.065f, 0.078f, 0.10f, 0.90f));
            Rect fill = rect;
            fill.width *= Mathf.Clamp01(ratio);
            Fill(fill, color);
        }

        private static void Fill(Rect rect, Color color)
        {
            Color before = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = before;
        }

        private static void Stroke(Rect rect, Color color, float thickness)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
            Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }
    }
}
