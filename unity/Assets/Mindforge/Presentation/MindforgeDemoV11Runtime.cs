using System;
using System.Collections;
using UnityEngine;
using Mindforge.Calibration;
using Mindforge.Combat;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Mindforge.Qualification;
#endif

namespace Mindforge.Presentation
{
    /// <summary>
    /// Runtime owner for the clean V0.11 demo. It deliberately installs one camera, one HUD,
    /// and one Guardian visual presentation while leaving gameplay/BCI authority untouched.
    /// </summary>
    public sealed class MindforgeDemoV11Runtime : MonoBehaviour
    {
        private MindforgeDemoV11Marker _marker;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            MindforgeDemoV11Marker marker = UnityEngine.Object.FindObjectOfType<MindforgeDemoV11Marker>(true);
            if (marker == null || marker.GetComponent<MindforgeDemoV11Runtime>() != null) return;
            marker.gameObject.AddComponent<MindforgeDemoV11Runtime>();
        }

        private IEnumerator Start()
        {
            _marker = GetComponent<MindforgeDemoV11Marker>();
            GuardianCombatInput input = null;
            FracturedSignalDirector bossDirector = null;
            Camera camera = null;
            GuardianSwordShieldController physical = null;

            for (int frame = 0; frame < 180; frame++)
            {
                if (input == null) input = UnityEngine.Object.FindObjectOfType<GuardianCombatInput>(true);
                if (bossDirector == null) bossDirector = UnityEngine.Object.FindObjectOfType<FracturedSignalDirector>(true);
                if (camera == null) camera = Camera.main;
                if (input != null && physical == null) physical = input.GetComponent<GuardianSwordShieldController>();
                if (input != null && bossDirector != null && camera != null && physical != null) break;
                yield return null;
            }

            if (_marker == null || input == null || bossDirector == null || camera == null)
            {
                Debug.LogError("[Mindforge:V11] Demo runtime could not resolve Guardian, boss, camera, or marker.");
                yield break;
            }

            DisableLegacyHud();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_marker.ControllerOnlyByDefault)
            {
                ControllerOnlyQualificationBootstrap qualification =
                    UnityEngine.Object.FindObjectOfType<ControllerOnlyQualificationBootstrap>(true);
                if (qualification == null)
                {
                    GameObject q = new GameObject("MindforgeControllerOnlyQualification");
                    qualification = q.AddComponent<ControllerOnlyQualificationBootstrap>();
                }
                qualification.EnterControllerOnly("V11_PRESENTABLE_DEMO");
                yield return null;
            }
#endif

            GameObject guardian = input.gameObject;
            GameObject boss = bossDirector.gameObject;
            Rigidbody guardianBody = guardian.GetComponent<Rigidbody>();
            guardian.transform.position = _marker.GuardianSpawn;
            guardian.transform.rotation = Quaternion.identity;
            if (guardianBody != null)
            {
                guardianBody.velocity = Vector3.zero;
                guardianBody.angularVelocity = Vector3.zero;
            }
            boss.transform.position = _marker.BossSpawn;
            boss.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            GuardianTargetLock targetLock = guardian.GetComponent<GuardianTargetLock>();
            if (targetLock == null) targetLock = guardian.AddComponent<GuardianTargetLock>();
            targetLock.Configure(boss.transform);
            GuardianCombatController combat = guardian.GetComponent<GuardianCombatController>();
            if (combat != null) combat.PrimaryTarget = boss.transform;
            if (physical != null) physical.SetFallbackTarget(boss.transform);

            MindforgeDemoGuardianV11 guardianVisual = guardian.GetComponent<MindforgeDemoGuardianV11>();
            if (guardianVisual == null) guardianVisual = guardian.AddComponent<MindforgeDemoGuardianV11>();
            guardianVisual.Configure(guardian.GetComponent<GuardianMotor>(), physical);

            MindforgeDemoBossV11 bossVisual = boss.GetComponent<MindforgeDemoBossV11>();
            if (bossVisual == null) bossVisual = boss.AddComponent<MindforgeDemoBossV11>();

            GameObject cameraOwner = camera.transform.root.gameObject;
            MindforgeDemoCameraV11 cameraRig = cameraOwner.GetComponent<MindforgeDemoCameraV11>();
            if (cameraRig == null) cameraRig = cameraOwner.AddComponent<MindforgeDemoCameraV11>();
            cameraRig.Configure(guardian.transform, boss.transform, targetLock, camera);

            MindforgeDemoHudV11 hud = GetComponent<MindforgeDemoHudV11>();
            if (hud == null) hud = gameObject.AddComponent<MindforgeDemoHudV11>();
            hud.Configure(guardian.transform, boss.transform);

            InitializeEchoes(guardian.transform, guardian.GetComponent<FluxMeter>());
            Debug.Log(
                "[Mindforge:V11] Presentable demo runtime ready: one camera, one HUD, one Guardian shell, " +
                "clean route collision, compact enemy presentation, and inherited deterministic combat/BCI authority.");
        }

        private static void DisableLegacyHud()
        {
            GameObject competitionHud = GameObject.Find("CompetitionHUD");
            if (competitionHud != null) competitionHud.SetActive(false);

            CombatStateHud combatHud = UnityEngine.Object.FindObjectOfType<CombatStateHud>(true);
            if (combatHud != null) combatHud.enabled = false;
            ProductionHudV09 productionHud = UnityEngine.Object.FindObjectOfType<ProductionHudV09>(true);
            if (productionHud != null) productionHud.enabled = false;
        }

        private static void InitializeEchoes(Transform guardian, FluxMeter flux)
        {
            FracturedEchoNode[] echoes = UnityEngine.Object.FindObjectsOfType<FracturedEchoNode>(true);
            for (int i = 0; i < echoes.Length; i++)
            {
                FracturedEchoNode echo = echoes[i];
                if (echo == null || !echo.name.StartsWith("V11Echo_", StringComparison.Ordinal)) continue;
                string suffix = echo.name.Substring("V11Echo_".Length);
                GameObject anchorObject = FindIncludingInactive("V11EchoAnchor_" + suffix);
                if (anchorObject == null) continue;
                float phase = i * 2.1f;
                echo.ConfigureWorldEcho(anchorObject.transform, guardian, flux, phase, 0.85f);
                if (echo.GetComponent<MindforgeDemoEchoV11>() == null)
                    echo.gameObject.AddComponent<MindforgeDemoEchoV11>();
            }
        }

        private static GameObject FindIncludingInactive(string name)
        {
            Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform t = transforms[i];
                if (t != null && t.gameObject.scene.IsValid() && string.Equals(t.name, name, StringComparison.Ordinal))
                    return t.gameObject;
            }
            return null;
        }
    }

    /// <summary>
    /// Bounded elevated ARPG camera for V0.11. The route is authored around this envelope, and
    /// the camera resolves world collision without ever becoming gameplay authority.
    /// </summary>
    internal sealed class MindforgeDemoCameraV11 : MonoBehaviour
    {
        private Transform _guardian;
        private Transform _boss;
        private GuardianTargetLock _targetLock;
        private Camera _camera;
        private Vector3 _velocity;
        private float _userYaw;
        private bool _initialized;
        private readonly RaycastHit[] _hits = new RaycastHit[16];

        private const float BaseYaw = 18f;
        private const float Pitch = 24f;
        private const float FreeDistance = 8.4f;
        private const float LockDistance = 9.6f;
        private const float PivotHeight = 1.45f;
        private const float MinDistance = 3.0f;

        public void Configure(Transform guardian, Transform boss, GuardianTargetLock targetLock, Camera camera)
        {
            _guardian = guardian;
            _boss = boss;
            _targetLock = targetLock;
            _camera = camera;
            _initialized = false;
        }

        private void Update()
        {
            if (_guardian == null || (_targetLock != null && _targetLock.Locked)) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float input = Input.GetAxis("Mouse X") * 1.5f;
            if (Input.GetKey(KeyCode.LeftArrow)) input -= 62f * dt;
            if (Input.GetKey(KeyCode.RightArrow)) input += 62f * dt;
            _userYaw = Mathf.Clamp(_userYaw + input, -32f, 32f);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void LateUpdate()
        {
            if (_guardian == null || _camera == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (dt <= 0f) return;

            bool locked = _targetLock != null && _targetLock.Locked && _targetLock.Target != null;
            Vector3 pivot = _guardian.position + Vector3.up * PivotHeight;
            float yaw = BaseYaw + _userYaw;
            float distance = FreeDistance;
            Vector3 lookPoint = pivot + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * 5f;

            if (locked)
            {
                Transform target = _targetLock.Target;
                Vector3 targetPoint = target.position + Vector3.up * 1.0f;
                Vector3 flat = Vector3.ProjectOnPlane(targetPoint - pivot, Vector3.up);
                if (flat.sqrMagnitude > 0.01f)
                    yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
                distance = LockDistance;
                lookPoint = Vector3.Lerp(pivot, targetPoint, 0.48f);
            }

            Quaternion orbit = Quaternion.Euler(Pitch, yaw, 0f);
            Vector3 desired = pivot + orbit * Vector3.back * distance;
            desired = ResolveCollision(pivot, desired);

            Vector3 look = lookPoint - desired;
            if (look.sqrMagnitude < 0.001f) look = _guardian.forward;
            Quaternion rotation = Quaternion.LookRotation(look.normalized, Vector3.up);

            if (!_initialized)
            {
                transform.position = desired;
                transform.rotation = rotation;
                _initialized = true;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, 0.055f, Mathf.Infinity, dt);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 1f - Mathf.Exp(-18f * dt));
            }

            _camera.fieldOfView = 56f;
            _camera.nearClipPlane = 0.08f;
            _camera.farClipPlane = 260f;
        }

        private Vector3 ResolveCollision(Vector3 pivot, Vector3 desired)
        {
            Vector3 delta = desired - pivot;
            float distance = delta.magnitude;
            if (distance < 0.01f) return desired;
            Vector3 direction = delta / distance;

            int count = Physics.SphereCastNonAlloc(pivot, 0.28f, direction, _hits, distance, ~0, QueryTriggerInteraction.Ignore);
            float nearest = distance;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                Collider collider = _hits[i].collider;
                if (collider == null || IsGuardian(collider.transform) || IsDynamicActor(collider)) continue;
                if (_hits[i].distance < nearest)
                {
                    nearest = _hits[i].distance;
                    found = true;
                }
            }
            if (!found) return desired;

            float resolved = Mathf.Max(MinDistance, nearest - 0.30f);
            Vector3 candidate = pivot + direction * resolved;
            if (Physics.CheckSphere(candidate, 0.22f, ~0, QueryTriggerInteraction.Ignore))
                candidate += Vector3.up * 0.8f;
            return candidate;
        }

        private bool IsGuardian(Transform candidate)
            => candidate != null && _guardian != null && (candidate == _guardian || candidate.IsChildOf(_guardian));

        private static bool IsDynamicActor(Collider collider)
            => collider != null && collider.GetComponentInParent<CombatantVitals>() != null;
    }

    /// <summary>
    /// Cohesive stylized Guardian shell. It replaces the root capsule renderer only and leaves
    /// authoritative Rigidbody/collider/combat components untouched. The Aetherblade remains
    /// owned by PhysicalArsenalBootstrap and sits adjacent to the authored right hand.
    /// </summary>
    internal sealed class MindforgeDemoGuardianV11 : MonoBehaviour
    {
        private GuardianMotor _motor;
        private GuardianSwordShieldController _physical;
        private Transform _visualRoot;
        private Vector3 _baseLocalPosition;

        public void Configure(GuardianMotor motor, GuardianSwordShieldController physical)
        {
            _motor = motor;
            _physical = physical;
            BuildIfNeeded();
        }

        private void Start() => BuildIfNeeded();

        private void BuildIfNeeded()
        {
            if (_visualRoot != null || transform.Find("V11GuardianVisual") != null) return;
            Renderer rootRenderer = GetComponent<Renderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;

            _visualRoot = NewNode("V11GuardianVisual", transform, Vector3.zero);
            _baseLocalPosition = _visualRoot.localPosition;
            Material armor = RuntimeMaterialV11.Armor;
            Material dark = RuntimeMaterialV11.Dark;
            Material gold = RuntimeMaterialV11.Gold;
            Material aether = RuntimeMaterialV11.Aether;

            Primitive("Torso", PrimitiveType.Capsule, _visualRoot, new Vector3(0f, 1.03f, 0f), new Vector3(0.43f, 0.56f, 0.32f), armor);
            Primitive("ChestPlate", PrimitiveType.Cube, _visualRoot, new Vector3(0f, 1.12f, 0.30f), new Vector3(0.52f, 0.42f, 0.10f), dark);
            Primitive("Sternum", PrimitiveType.Cube, _visualRoot, new Vector3(0f, 1.10f, 0.365f), new Vector3(0.075f, 0.32f, 0.035f), gold);
            Primitive("AetherCore", PrimitiveType.Sphere, _visualRoot, new Vector3(0f, 1.18f, 0.41f), Vector3.one * 0.12f, aether);
            Primitive("Pelvis", PrimitiveType.Cube, _visualRoot, new Vector3(0f, 0.55f, 0f), new Vector3(0.52f, 0.28f, 0.34f), dark);
            Primitive("Helmet", PrimitiveType.Sphere, _visualRoot, new Vector3(0f, 1.72f, 0.02f), new Vector3(0.40f, 0.42f, 0.38f), armor);
            Primitive("Visor", PrimitiveType.Cube, _visualRoot, new Vector3(0f, 1.73f, 0.37f), new Vector3(0.33f, 0.09f, 0.055f), aether);
            Primitive("Crown", PrimitiveType.Cube, _visualRoot, new Vector3(0f, 2.08f, -0.01f), new Vector3(0.09f, 0.28f, 0.09f), gold, new Vector3(0f, 0f, 12f));

            Limb("ArmL", _visualRoot, new Vector3(-0.48f, 1.05f, 0.02f), new Vector3(0.18f, 0.50f, 0.18f), dark, new Vector3(0f, 0f, -12f));
            Limb("ArmR", _visualRoot, new Vector3(0.48f, 1.05f, 0.02f), new Vector3(0.18f, 0.50f, 0.18f), dark, new Vector3(0f, 0f, 12f));
            Primitive("HandL", PrimitiveType.Sphere, _visualRoot, new Vector3(-0.36f, 0.58f, 0.15f), Vector3.one * 0.16f, armor);
            Primitive("HandR", PrimitiveType.Sphere, _visualRoot, new Vector3(0.34f, 0.58f, 0.16f), Vector3.one * 0.16f, armor);
            Limb("LegL", _visualRoot, new Vector3(-0.22f, 0.13f, 0f), new Vector3(0.20f, 0.56f, 0.22f), armor, Vector3.zero);
            Limb("LegR", _visualRoot, new Vector3(0.22f, 0.13f, 0f), new Vector3(0.20f, 0.56f, 0.22f), armor, Vector3.zero);
            Primitive("Mantle", PrimitiveType.Cube, _visualRoot, new Vector3(0f, 1.03f, -0.28f), new Vector3(0.88f, 0.70f, 0.055f), dark, new Vector3(8f, 0f, 0f));
        }

        private void LateUpdate()
        {
            if (_visualRoot == null || _motor == null) return;
            Vector3 localVelocity = transform.InverseTransformDirection(_motor.Velocity);
            float speed = Vector3.ProjectOnPlane(_motor.Velocity, Vector3.up).magnitude;
            float pitch = Mathf.Clamp(-localVelocity.z * 0.65f, -7f, 7f);
            float roll = Mathf.Clamp(-localVelocity.x * 0.70f, -8f, 8f);
            if (_motor.IsDashing) pitch -= 9f;
            _visualRoot.localRotation = Quaternion.Slerp(_visualRoot.localRotation, Quaternion.Euler(pitch, 0f, roll), 1f - Mathf.Exp(-12f * Time.deltaTime));
            float bob = speed > 0.4f && _motor.IsGrounded ? Mathf.Sin(Time.time * 9.2f) * 0.018f : 0f;
            _visualRoot.localPosition = _baseLocalPosition + Vector3.up * bob;
        }

        private static void Limb(string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 euler)
            => Primitive(name, PrimitiveType.Capsule, parent, position, scale, material, euler);

        private static Transform NewNode(string name, Transform parent, Vector3 position)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            return go.transform;
        }

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 scale, Material material, Vector3? euler = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            return go;
        }
    }

    internal sealed class MindforgeDemoBossV11 : MonoBehaviour
    {
        private Transform _crown;

        private void Start()
        {
            Renderer rootRenderer = GetComponent<Renderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;
            if (transform.Find("V11BossVisual") != null) return;

            Transform root = NewNode("V11BossVisual", transform, Vector3.zero);
            Primitive("BossBody", PrimitiveType.Cylinder, root, new Vector3(0f, 1.35f, 0f), new Vector3(1.15f, 1.35f, 1.15f), RuntimeMaterialV11.Dark);
            Primitive("BossCore", PrimitiveType.Sphere, root, new Vector3(0f, 1.55f, 0.64f), Vector3.one * 0.48f, RuntimeMaterialV11.Hostile);
            Primitive("BossCrownStem", PrimitiveType.Cube, root, new Vector3(0f, 3.0f, 0f), new Vector3(0.35f, 1.25f, 0.35f), RuntimeMaterialV11.Gold);
            _crown = NewNode("BossOrbitCrown", root, new Vector3(0f, 2.15f, 0f));
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f;
                Vector3 p = Quaternion.Euler(0f, a, 0f) * new Vector3(1.55f, 0f, 0f);
                Primitive("CrownBlade_" + i, PrimitiveType.Cube, _crown, p, new Vector3(0.20f, 1.2f, 0.46f), i % 2 == 0 ? RuntimeMaterialV11.Hostile : RuntimeMaterialV11.Gold, new Vector3(0f, -a, 18f));
            }
        }

        private void Update()
        {
            if (_crown != null) _crown.Rotate(Vector3.up, 18f * Time.deltaTime, Space.Self);
        }

        private static Transform NewNode(string name, Transform parent, Vector3 position)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            return go.transform;
        }

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 scale, Material material, Vector3? euler = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            return go;
        }
    }

    internal sealed class MindforgeDemoEchoV11 : MonoBehaviour
    {
        private Transform _orbit;

        private void Start()
        {
            Renderer rootRenderer = GetComponent<Renderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;
            if (transform.Find("V11EchoVisual") != null) return;
            _orbit = new GameObject("V11EchoVisual").transform;
            _orbit.SetParent(transform, false);
            Primitive("EchoCore", PrimitiveType.Sphere, _orbit, Vector3.zero, Vector3.one * 0.35f, RuntimeMaterialV11.Hostile);
            Primitive("EchoShellA", PrimitiveType.Cube, _orbit, Vector3.zero, new Vector3(0.55f, 0.55f, 0.12f), RuntimeMaterialV11.Dark, new Vector3(25f, 35f, 45f));
            Primitive("EchoShellB", PrimitiveType.Cube, _orbit, Vector3.zero, new Vector3(0.12f, 0.55f, 0.55f), RuntimeMaterialV11.Gold, new Vector3(-25f, -35f, 45f));
        }

        private void Update()
        {
            if (_orbit != null) _orbit.Rotate(new Vector3(22f, 58f, 15f) * Time.deltaTime, Space.Self);
        }

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 scale, Material material, Vector3? euler = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            return go;
        }
    }

    internal sealed class MindforgeDemoHudV11 : MonoBehaviour
    {
        private Transform _guardian;
        private Transform _boss;
        private CombatantVitals _vitals;
        private CombatantVitals _bossVitals;
        private GuardianStamina _stamina;
        private FluxMeter _flux;
        private AwakeningCalibrationDirector _calibration;
        private GUIStyle _title;
        private GUIStyle _small;
        private GUIStyle _objective;
        private double _started;
        private string _district = string.Empty;
        private double _districtChanged;

        public void Configure(Transform guardian, Transform boss)
        {
            _guardian = guardian;
            _boss = boss;
            Resolve();
            _started = Time.realtimeSinceStartupAsDouble;
            UpdateDistrict(true);
        }

        private void Update()
        {
            if (_guardian == null || _vitals == null) Resolve();
            UpdateDistrict(false);
        }

        private void Resolve()
        {
            if (_guardian == null)
            {
                GuardianCombatInput input = UnityEngine.Object.FindObjectOfType<GuardianCombatInput>(true);
                if (input != null) _guardian = input.transform;
            }
            if (_guardian != null)
            {
                if (_vitals == null) _vitals = _guardian.GetComponent<CombatantVitals>();
                if (_stamina == null) _stamina = _guardian.GetComponent<GuardianStamina>();
                if (_flux == null) _flux = _guardian.GetComponent<FluxMeter>();
            }
            if (_boss == null)
            {
                FracturedSignalDirector director = UnityEngine.Object.FindObjectOfType<FracturedSignalDirector>(true);
                if (director != null) _boss = director.transform;
            }
            if (_boss != null && _bossVitals == null) _bossVitals = _boss.GetComponent<CombatantVitals>();
            if (_calibration == null) _calibration = UnityEngine.Object.FindObjectOfType<AwakeningCalibrationDirector>(true);
        }

        private void UpdateDistrict(bool force)
        {
            if (_guardian == null) return;
            string next = DistrictFor(_guardian.position.z);
            if (!force && next == _district) return;
            _district = next;
            _districtChanged = Time.realtimeSinceStartupAsDouble;
        }

        private void OnGUI()
        {
            if (_vitals == null) return;
            EnsureStyles();
            float scale = Mathf.Clamp(Screen.height / 1080f, 0.78f, 1.15f);
            float x = 22f * scale;
            float y = 20f * scale;
            float w = 250f * scale;
            float h = 70f * scale;
            Rect panel = new Rect(x, y, w, h);
            Fill(panel, new Color(0.025f, 0.032f, 0.040f, 0.78f));
            Stroke(panel, new Color(0.76f, 0.69f, 0.48f, 0.36f), 1f);
            GUI.Label(new Rect(x + 10f * scale, y + 4f * scale, w - 20f * scale, 16f * scale), "GUARDIAN", _title);
            DrawBar(new Rect(x + 10f * scale, y + 26f * scale, w - 20f * scale, 7f * scale), Ratio(_vitals.Health, _vitals.MaxHealth), new Color(0.86f, 0.28f, 0.35f, 0.96f));
            DrawBar(new Rect(x + 10f * scale, y + 41f * scale, w - 20f * scale, 5f * scale), _stamina != null ? _stamina.Ratio : 0f, new Color(0.47f, 0.82f, 0.68f, 0.96f));
            DrawBar(new Rect(x + 10f * scale, y + 54f * scale, w - 20f * scale, 4f * scale), _flux != null ? Ratio(_flux.Value, _flux.Max) : 0f, new Color(0.25f, 0.68f, 0.92f, 0.94f));
            GUI.Label(new Rect(x + 10f * scale, y + 56f * scale, w - 20f * scale, 12f * scale), $"{_vitals.Health:0}/{_vitals.MaxHealth:0}   ENDURANCE   FLUX", _small);

            string neural = _calibration != null && _calibration.ControllerOnlyQualificationActive
                ? "DEMO · BCI OFF"
                : _calibration != null && _calibration.CalibrationReady
                    ? "NEURAL LINK · READY"
                    : "NEURAL LINK · ATTUNE";
            Vector2 chipSize = _small.CalcSize(new GUIContent(neural));
            Rect chip = new Rect(Screen.width - chipSize.x - 44f * scale, 22f * scale, chipSize.x + 20f * scale, 22f * scale);
            Fill(chip, new Color(0.025f, 0.032f, 0.040f, 0.68f));
            GUI.Label(chip, neural, _small);

            string objective = ObjectiveFor(_guardian != null ? _guardian.position.z : -100f);
            float objectiveWidth = Mathf.Min(490f * scale, Screen.width * 0.44f);
            Rect objectiveRect = new Rect(22f * scale, Screen.height - 42f * scale, objectiveWidth, 24f * scale);
            Fill(objectiveRect, new Color(0.025f, 0.032f, 0.040f, 0.68f));
            GUI.Label(new Rect(objectiveRect.x + 10f * scale, objectiveRect.y, objectiveRect.width - 20f * scale, objectiveRect.height), objective, _objective);

            if (Time.realtimeSinceStartupAsDouble - _districtChanged < 2.4)
            {
                Vector2 size = _title.CalcSize(new GUIContent(_district));
                Rect districtRect = new Rect((Screen.width - size.x - 34f * scale) * 0.5f, 34f * scale, size.x + 34f * scale, 30f * scale);
                Fill(districtRect, new Color(0.025f, 0.032f, 0.040f, 0.58f));
                GUI.Label(districtRect, _district, _title);
            }

            if (Time.realtimeSinceStartupAsDouble - _started < 9.0)
            {
                const string controls = "WASD MOVE   ·   SPACE JUMP/HOVER   ·   SHIFT DODGE   ·   F/LMB BLADE   ·   T LOCK";
                Vector2 size = _small.CalcSize(new GUIContent(controls));
                Rect r = new Rect((Screen.width - size.x - 26f * scale) * 0.5f, Screen.height - 40f * scale, size.x + 26f * scale, 22f * scale);
                Fill(r, new Color(0.025f, 0.032f, 0.040f, 0.62f));
                GUI.Label(r, controls, _small);
            }
        }

        private string ObjectiveFor(float z)
        {
            if (_bossVitals != null && !_bossVitals.IsAlive) return "SIGNAL STABILIZED · DEMO COMPLETE";
            if (z < -2f) return "LEAVE THE MEMORY FORGE";
            if (z < 32f) return "CROSS THE CAUSEWAY · SHATTER THE ECHO";
            if (z < 58f) return "BREAK THROUGH THE MARKET";
            if (z < 83f) return "ASCEND THE CHOIR TOWER";
            return "CONFRONT THE FRACTURED SIGNAL";
        }

        private static string DistrictFor(float z)
        {
            if (z < -2f) return "MEMORY FORGE SANCTUM";
            if (z < 32f) return "NEON CAUSEWAY";
            if (z < 58f) return "MARKET OF BROKEN MOMENTUM";
            if (z < 83f) return "CHOIR TOWER ASCENT";
            return "FRACTURED SIGNAL";
        }

        private void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.94f, 0.94f, 0.90f, 0.96f) },
            };
            _small = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.78f, 0.84f, 0.88f, 0.92f) },
            };
            _objective = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.91f, 0.90f, 0.82f, 0.94f) },
            };
        }

        private static float Ratio(float value, float max) => max > 0f ? Mathf.Clamp01(value / max) : 0f;

        private static void DrawBar(Rect rect, float ratio, Color color)
        {
            Fill(rect, new Color(0.08f, 0.10f, 0.12f, 0.90f));
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

    internal static class RuntimeMaterialV11
    {
        private static Material _armor;
        private static Material _dark;
        private static Material _gold;
        private static Material _aether;
        private static Material _hostile;

        public static Material Armor => _armor ??= Create("V11_Runtime_Armor", new Color(0.54f, 0.58f, 0.62f), 0.72f, 0.70f, Color.black);
        public static Material Dark => _dark ??= Create("V11_Runtime_Dark", new Color(0.055f, 0.070f, 0.085f), 0.58f, 0.68f, Color.black);
        public static Material Gold => _gold ??= Create("V11_Runtime_Gold", new Color(0.60f, 0.42f, 0.17f), 0.86f, 0.76f, Color.black);
        public static Material Aether => _aether ??= Create("V11_Runtime_Aether", new Color(0.08f, 0.45f, 0.70f), 0.18f, 0.82f, new Color(0.18f, 0.82f, 1f) * 3.1f);
        public static Material Hostile => _hostile ??= Create("V11_Runtime_Hostile", new Color(0.30f, 0.05f, 0.12f), 0.24f, 0.76f, new Color(1f, 0.10f, 0.28f) * 2.8f);

        private static Material Create(string name, Color color, float metallic, float smoothness, Color emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (emission.maxColorComponent > 0.001f && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            return material;
        }
    }
}
