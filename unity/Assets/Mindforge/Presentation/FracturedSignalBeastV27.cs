using System.Collections;
using System.Collections.Generic;
using Mindforge.Combat;
using Mindforge.SoulWisp;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Organic render-only body for the first Fractured Signal encounter.
    ///
    /// V0.27 replaces the abstract broken-knight/shard silhouette with a low, heavy cathedral
    /// parasite: a continuous lofted body, broad jaw, sensory eyes, forelimbs and dorsal signal
    /// eruptions. The existing boss root Rigidbody/collider/vitals/movement/attacks remain the
    /// only gameplay authority.
    /// </summary>
    [DefaultExecutionOrder(890)]
    [RequireComponent(typeof(FracturedSignalDirector))]
    public sealed class FracturedSignalBeastV27 : MonoBehaviour
    {
        public const string RootName = "FracturedSignalBeastV27";

        [SerializeField] private FracturedSignalDirector director;
        [SerializeField] private CombatantVitals vitals;
        [SerializeField] private FracturedSignalFirstBossV19 movement;
        [SerializeField] private SoulWispController wisp;

        private Transform _guardian;
        private Transform _root;
        private Transform _body;
        private Transform _head;
        private Transform _jaw;
        private Transform _leftForelimb;
        private Transform _rightForelimb;
        private Transform _leftFeel;
        private Transform _rightFeel;
        private Transform[] _crystals;
        private Renderer[] _crystalRenderers;
        private Renderer[] _eyeRenderers;
        private Renderer _mouthRenderer;
        private Material _hide;
        private Material _belly;
        private Material _maw;
        private Material _corruption;
        private Material _eye;
        private MaterialPropertyBlock _block;
        private float _charge;
        private float _release;
        private float _damage;
        private float _heavy;
        private int _phase = 1;
        private bool _built;
        private bool _neuralFrozen;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            FracturedSignalDirector[] bosses = FindObjectsOfType<FracturedSignalDirector>(true);
            for (int i = 0; i < bosses.Length; i++)
            {
                FracturedSignalDirector boss = bosses[i];
                if (boss != null && boss.GetComponent<FracturedSignalBeastV27>() == null)
                    boss.gameObject.AddComponent<FracturedSignalBeastV27>();
            }
        }

        private void Awake()
        {
            Resolve();
            Build();
            HideLegacyPresentation();
        }

        private void OnEnable()
        {
            Resolve();
            if (director != null)
            {
                _phase = director.Phase;
                director.PhaseChanged += OnPhaseChanged;
                director.AttackTelegraphed += OnTelegraphed;
                director.AttackFired += OnAttackFired;
            }
            if (vitals != null) vitals.Damaged += OnDamaged;
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 4; i++) yield return null;
            Resolve();
            HideLegacyPresentation();
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.PhaseChanged -= OnPhaseChanged;
                director.AttackTelegraphed -= OnTelegraphed;
                director.AttackFired -= OnAttackFired;
            }
            if (vitals != null) vitals.Damaged -= OnDamaged;
        }

        private void Resolve()
        {
            if (director == null) director = GetComponent<FracturedSignalDirector>();
            if (vitals == null) vitals = GetComponent<CombatantVitals>();
            if (movement == null) movement = GetComponent<FracturedSignalFirstBossV19>();
            if (wisp == null) wisp = FindObjectOfType<SoulWispController>(true);
            if (_guardian == null)
            {
                GuardianCombatInput input = FindObjectOfType<GuardianCombatInput>(true);
                if (input != null) _guardian = input.transform;
            }
        }

        private void Build()
        {
            Transform existing = transform.Find(RootName);
            if (existing != null)
            {
                _root = existing;
                _built = true;
                return;
            }

            _hide = CreateMaterial(
                "V27_Beast_StoneFlesh",
                new Color(0.28f, 0.235f, 0.255f),
                0.03f, 0.32f,
                new Color(0.10f, 0.018f, 0.12f) * 0.22f);
            _belly = CreateMaterial(
                "V27_Beast_Belly",
                new Color(0.14f, 0.115f, 0.125f),
                0.01f, 0.22f,
                Color.black);
            _maw = CreateMaterial(
                "V27_Beast_Maw",
                new Color(0.012f, 0.008f, 0.012f),
                0.0f, 0.18f,
                new Color(0.40f, 0.015f, 0.10f) * 0.28f);
            _corruption = CreateMaterial(
                "V27_Beast_SignalCrystal",
                new Color(0.21f, 0.025f, 0.22f),
                0.46f, 0.68f,
                new Color(0.95f, 0.045f, 1.0f) * 2.20f);
            _eye = CreateMaterial(
                "V27_Beast_SensoryEye",
                new Color(0.03f, 0.20f, 0.24f),
                0.10f, 0.82f,
                new Color(0.18f, 0.92f, 1.0f) * 2.75f);
            _block = new MaterialPropertyBlock();

            _root = new GameObject(RootName).transform;
            _root.SetParent(transform, false);
            _root.localPosition = new Vector3(0f, 0.12f, -0.15f);

            _body = MeshPart("ParasiteBody", BuildOrganicBodyMesh(26, 22), _root, _hide);
            _body.localPosition = new Vector3(0f, 0.62f, -0.38f);

            Transform belly = MeshPart("BellyMass", BuildOrganicBodyMesh(18, 18, 0.78f, 0.64f), _root, _belly);
            belly.localPosition = new Vector3(0f, 0.34f, -0.10f);
            belly.localScale = new Vector3(0.88f, 0.62f, 0.80f);

            _head = new GameObject("BeastHeadRig").transform;
            _head.SetParent(_root, false);
            _head.localPosition = new Vector3(0f, 1.03f, 1.62f);
            Transform headMass = MeshPart("BroadJowl", BuildHeadMesh(18, 20), _head, _hide);
            headMass.localPosition = Vector3.zero;

            Transform leftCheek = Primitive("LeftJowl", PrimitiveType.Sphere, _head, new Vector3(-0.54f, -0.10f, 0.54f), new Vector3(0.78f, 0.58f, 0.72f), _hide);
            Transform rightCheek = Primitive("RightJowl", PrimitiveType.Sphere, _head, new Vector3(0.54f, -0.10f, 0.54f), new Vector3(0.78f, 0.58f, 0.72f), _hide);
            leftCheek.localRotation = Quaternion.Euler(0f, -12f, -6f);
            rightCheek.localRotation = Quaternion.Euler(0f, 12f, 6f);

            Transform mawCavity = Primitive("MawCavity", PrimitiveType.Sphere, _head, new Vector3(0f, -0.19f, 0.92f), new Vector3(0.88f, 0.34f, 0.30f), _maw);
            _mouthRenderer = mawCavity.GetComponent<Renderer>();

            _jaw = new GameObject("LowerJawRig").transform;
            _jaw.SetParent(_head, false);
            _jaw.localPosition = new Vector3(0f, -0.34f, 0.78f);
            Transform lowerJaw = MeshPart("LowerJaw", BuildJawMesh(14, 16), _jaw, _belly);
            lowerJaw.localPosition = new Vector3(0f, -0.03f, 0.20f);
            Transform tongue = Primitive("SignalTongue", PrimitiveType.Capsule, _jaw, new Vector3(0f, -0.08f, 0.48f), new Vector3(0.18f, 0.36f, 0.13f), _corruption);
            tongue.localRotation = Quaternion.Euler(72f, 0f, 0f);

            _eyeRenderers = new Renderer[2];
            Transform eyeL = Primitive("SensoryEye_L", PrimitiveType.Sphere, _head, new Vector3(-0.46f, 0.28f, 0.78f), Vector3.one * 0.17f, _eye);
            Transform eyeR = Primitive("SensoryEye_R", PrimitiveType.Sphere, _head, new Vector3(0.46f, 0.28f, 0.78f), Vector3.one * 0.17f, _eye);
            _eyeRenderers[0] = eyeL.GetComponent<Renderer>();
            _eyeRenderers[1] = eyeR.GetComponent<Renderer>();

            _leftForelimb = MeshPart("LeftForelimb", BuildTaperedAppendageMesh(12), _root, _hide);
            _leftForelimb.localPosition = new Vector3(-1.13f, 0.46f, 0.82f);
            _leftForelimb.localScale = new Vector3(0.38f, 1.25f, 0.38f);
            _leftForelimb.localRotation = Quaternion.Euler(68f, -8f, 22f);
            _rightForelimb = MeshPart("RightForelimb", BuildTaperedAppendageMesh(12), _root, _hide);
            _rightForelimb.localPosition = new Vector3(1.13f, 0.46f, 0.82f);
            _rightForelimb.localScale = new Vector3(0.38f, 1.25f, 0.38f);
            _rightForelimb.localRotation = Quaternion.Euler(68f, 8f, -22f);

            _leftFeel = MeshPart("LeftSensoryFeel", BuildTaperedAppendageMesh(10), _head, _belly);
            _leftFeel.localPosition = new Vector3(-0.44f, 0.18f, 0.73f);
            _leftFeel.localScale = new Vector3(0.10f, 0.72f, 0.10f);
            _leftFeel.localRotation = Quaternion.Euler(70f, -18f, 26f);
            _rightFeel = MeshPart("RightSensoryFeel", BuildTaperedAppendageMesh(10), _head, _belly);
            _rightFeel.localPosition = new Vector3(0.44f, 0.18f, 0.73f);
            _rightFeel.localScale = new Vector3(0.10f, 0.72f, 0.10f);
            _rightFeel.localRotation = Quaternion.Euler(70f, 18f, -26f);

            BuildDorsalCrystals();
            _built = true;
            ApplyPose(0f, true);
        }

        private void BuildDorsalCrystals()
        {
            const int count = 9;
            _crystals = new Transform[count];
            _crystalRenderers = new Renderer[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                float z = Mathf.Lerp(-1.65f, 1.12f, t);
                float x = Mathf.Sin(i * 1.73f) * Mathf.Lerp(0.18f, 0.44f, Mathf.Sin(t * Mathf.PI));
                float y = 1.37f + Mathf.Sin(t * Mathf.PI) * 0.38f;
                Transform crystal = MeshPart($"SignalCrystal_{i:00}", BuildCrystalMesh(), _root, _corruption);
                crystal.localPosition = new Vector3(x, y, z);
                crystal.localRotation = Quaternion.Euler(-12f + i * 4f, i * 29f, (i - 4) * 5f);
                float height = 0.54f + (i % 3) * 0.19f + Mathf.Sin(t * Mathf.PI) * 0.40f;
                crystal.localScale = new Vector3(0.22f, height, 0.22f);
                _crystals[i] = crystal;
                _crystalRenderers[i] = crystal.GetComponent<Renderer>();
            }
        }

        private void LateUpdate()
        {
            if (!_built) Build();
            if (!_built || _root == null) return;
            Resolve();
            HideLegacyPresentation();

            if (NeuralVisualFieldActive())
            {
                if (!_neuralFrozen)
                {
                    _neuralFrozen = true;
                    _charge = 0f;
                    _release = 0f;
                    _damage = 0f;
                    _heavy = 0f;
                    ApplyPose(0f, true);
                }
                return;
            }

            _neuralFrozen = false;
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            _charge = Damp(_charge, 0f, 2.6f, dt);
            _release = Damp(_release, 0f, 6.6f, dt);
            _damage = Damp(_damage, 0f, 8.5f, dt);
            _heavy = Damp(_heavy, 0f, 3.2f, dt);
            ApplyPose(Time.unscaledTime, false);
        }

        private void ApplyPose(float time, bool neutral)
        {
            float phase01 = Mathf.InverseLerp(1f, 3f, _phase);
            float moving = movement != null && movement.MovementActive ? 1f : 0f;
            float breath = neutral ? 0f : Mathf.Sin(time * Mathf.Lerp(1.05f, 1.38f, phase01));
            float crawl = neutral ? 0f : Mathf.Sin(time * (2.15f + phase01 * 0.55f)) * moving;

            _root.localPosition = new Vector3(0f, 0.12f + breath * 0.025f, -0.15f - _damage * 0.08f);
            _root.localRotation = Quaternion.Euler(_damage * 2.8f, crawl * 1.8f, crawl * 0.9f);

            if (_body != null)
            {
                float inhale = 1f + breath * 0.022f + _charge * 0.018f;
                _body.localScale = new Vector3(inhale, 1f + breath * 0.035f, 1f - breath * 0.012f);
            }

            float headYaw = 0f;
            float headPitch = 0f;
            if (!neutral && _guardian != null)
            {
                Vector3 local = transform.InverseTransformDirection(_guardian.position + Vector3.up * 0.9f - _head.position);
                headYaw = Mathf.Clamp(Mathf.Atan2(local.x, Mathf.Max(0.001f, local.z)) * Mathf.Rad2Deg, -18f, 18f);
                headPitch = Mathf.Clamp(-Mathf.Atan2(local.y, Mathf.Max(0.2f, new Vector2(local.x, local.z).magnitude)) * Mathf.Rad2Deg, -8f, 7f);
            }
            if (_head != null)
                _head.localRotation = Quaternion.Euler(headPitch - _charge * 6f + _release * 4f, headYaw, breath * 0.8f);

            float jawOpen = neutral ? 0f : Mathf.Clamp01(_charge * 0.82f + _release * 0.72f + _heavy * 0.18f);
            if (_jaw != null)
                _jaw.localRotation = Quaternion.Euler(-jawOpen * 31f + _damage * 5f, 0f, 0f);

            if (_leftForelimb != null)
                _leftForelimb.localRotation = Quaternion.Euler(68f + crawl * 10f - _charge * 7f, -8f, 22f + crawl * 8f);
            if (_rightForelimb != null)
                _rightForelimb.localRotation = Quaternion.Euler(68f - crawl * 10f - _charge * 7f, 8f, -22f - crawl * 8f);

            if (_leftFeel != null)
                _leftFeel.localRotation = Quaternion.Euler(70f + breath * 4f, -18f - _charge * 12f, 26f + crawl * 3f);
            if (_rightFeel != null)
                _rightFeel.localRotation = Quaternion.Euler(70f - breath * 4f, 18f + _charge * 12f, -26f - crawl * 3f);

            if (_crystals != null)
            {
                for (int i = 0; i < _crystals.Length; i++)
                {
                    Transform crystal = _crystals[i];
                    if (crystal == null) continue;
                    float pulse = neutral ? 0f : Mathf.Sin(time * 0.78f + i * 0.71f) * 0.025f;
                    Vector3 scale = crystal.localScale;
                    scale.x = Mathf.Max(0.08f, scale.x * (1f + pulse));
                    scale.z = Mathf.Max(0.08f, scale.z * (1f + pulse));
                    crystal.localScale = scale;
                }
            }

            float corruptionIntensity = neutral ? 1.25f : 1.65f + phase01 * 0.65f + _charge * 0.85f + _release * 1.15f;
            ApplyEmission(_crystalRenderers, new Color(0.95f, 0.045f, 1f), corruptionIntensity);
            ApplyEmission(_eyeRenderers, new Color(0.18f, 0.92f, 1f), neutral ? 1.4f : 2.1f + _charge * 0.65f + _damage * 0.35f);
            if (_mouthRenderer != null)
                ApplyEmission(_mouthRenderer, new Color(0.78f, 0.025f, 0.16f), neutral ? 0.14f : 0.22f + jawOpen * 0.72f);
        }

        private void HideLegacyPresentation()
        {
            Renderer legacy = GetComponent<Renderer>();
            if (legacy != null) legacy.enabled = false;
            DisableChild("V11BossVisual");
            DisableChild(FracturedSignalCharacterV19.RootName);
            DisableChild("FracturedSignalShowcaseAvatar");
            DisableChild("FracturedSignalThreatSilhouette");
        }

        private void DisableChild(string name)
        {
            Transform child = transform.Find(name);
            if (child != null && child != _root && child.gameObject.activeSelf)
                child.gameObject.SetActive(false);
        }

        private bool NeuralVisualFieldActive()
        {
            if (wisp == null) wisp = FindObjectOfType<SoulWispController>(true);
            return wisp != null && (wisp.CalibrationStimuliActive || wisp.ResonanceWindowActive);
        }

        private void OnPhaseChanged(int phase)
        {
            _phase = Mathf.Clamp(phase, 1, 3);
            _release = 1f;
        }

        private void OnTelegraphed(string pattern, int count, bool heavy)
        {
            _charge = 1f;
            _heavy = heavy ? 1f : 0.30f;
        }

        private void OnAttackFired(string pattern, int count, bool heavy)
        {
            _charge = 0f;
            _release = heavy ? 1f : 0.70f;
            _heavy = heavy ? 1f : Mathf.Max(_heavy, 0.30f);
        }

        private void OnDamaged(DamagePacket packet)
        {
            if (packet.Damage > 0f) _damage = 1f;
        }

        private void ApplyEmission(Renderer[] renderers, Color color, float intensity)
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++) ApplyEmission(renderers[i], color, intensity);
        }

        private void ApplyEmission(Renderer renderer, Color color, float intensity)
        {
            if (renderer == null || _block == null) return;
            renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColor, color * 0.22f);
            _block.SetColor(ColorProperty, color * 0.22f);
            _block.SetColor(EmissionColor, color * Mathf.Max(0f, intensity));
            renderer.SetPropertyBlock(_block);
        }

        private static Transform MeshPart(string name, Mesh mesh, Transform parent, Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            return go.transform;
        }

        private static Transform Primitive(string name, PrimitiveType primitive, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            return go.transform;
        }

        private static Material CreateMaterial(string name, Color color, float metallic, float smoothness, Color emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader) { name = name };
            if (material.HasProperty(BaseColor)) material.SetColor(BaseColor, color);
            else if (material.HasProperty(ColorProperty)) material.SetColor(ColorProperty, color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty(EmissionColor))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor(EmissionColor, emission);
            }
            return material;
        }

        private static Mesh BuildOrganicBodyMesh(int rings, int sides, float widthScale = 1f, float heightScale = 1f)
        {
            rings = Mathf.Max(8, rings);
            sides = Mathf.Max(10, sides);
            List<Vector3> vertices = new List<Vector3>((rings + 1) * sides + 2);
            List<Vector2> uv = new List<Vector2>((rings + 1) * sides + 2);
            List<int> triangles = new List<int>(rings * sides * 6 + sides * 6);

            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings;
                float z = Mathf.Lerp(-2.45f, 1.82f, t);
                float arch = Mathf.Sin(t * Mathf.PI);
                float headBulge = Mathf.Clamp01((t - 0.63f) / 0.37f);
                float rx = (0.46f + arch * 0.94f + headBulge * 0.24f) * widthScale;
                float ry = (0.40f + arch * 0.54f - headBulge * 0.08f) * heightScale;
                float centreY = 0.12f + arch * 0.16f + Mathf.Sin(t * 6.7f) * 0.025f;
                float centreX = Mathf.Sin(t * 8.2f) * 0.055f;

                for (int s = 0; s < sides; s++)
                {
                    float u = s / (float)sides;
                    float a = u * Mathf.PI * 2f;
                    float surface = 1f + Mathf.Sin(a * 3f + t * 8f) * 0.028f + Mathf.Cos(a * 5f - t * 10f) * 0.018f;
                    float x = centreX + Mathf.Cos(a) * rx * surface;
                    float y = centreY + Mathf.Sin(a) * ry * surface;
                    vertices.Add(new Vector3(x, y, z));
                    uv.Add(new Vector2(u, t));
                }
            }

            for (int r = 0; r < rings; r++)
            {
                int nextRing = r + 1;
                for (int s = 0; s < sides; s++)
                {
                    int n = (s + 1) % sides;
                    int a = r * sides + s;
                    int b = r * sides + n;
                    int c = nextRing * sides + n;
                    int d = nextRing * sides + s;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(a); triangles.Add(d); triangles.Add(c);
                }
            }

            int back = vertices.Count;
            vertices.Add(new Vector3(0f, 0.12f, -2.46f));
            uv.Add(new Vector2(0.5f, 0f));
            int front = vertices.Count;
            vertices.Add(new Vector3(0f, 0.12f, 1.83f));
            uv.Add(new Vector2(0.5f, 1f));
            for (int s = 0; s < sides; s++)
            {
                int n = (s + 1) % sides;
                triangles.Add(back); triangles.Add(n); triangles.Add(s);
                int a = rings * sides + s;
                int b = rings * sides + n;
                triangles.Add(front); triangles.Add(a); triangles.Add(b);
            }

            return FinishMesh("V27_OrganicBeastBody", vertices, uv, triangles);
        }

        private static Mesh BuildHeadMesh(int rings, int sides)
        {
            Mesh mesh = BuildOrganicBodyMesh(rings, sides, 0.78f, 0.66f);
            mesh.name = "V27_BroadJowl";
            return mesh;
        }

        private static Mesh BuildJawMesh(int rings, int sides)
        {
            Mesh mesh = BuildOrganicBodyMesh(rings, sides, 0.58f, 0.30f);
            mesh.name = "V27_LowerJaw";
            return mesh;
        }

        private static Mesh BuildTaperedAppendageMesh(int sides)
        {
            sides = Mathf.Max(6, sides);
            List<Vector3> vertices = new List<Vector3>((sides + 1) * 2);
            List<Vector2> uv = new List<Vector2>(vertices.Capacity);
            List<int> triangles = new List<int>(sides * 6);
            for (int i = 0; i <= sides; i++)
            {
                float u = i / (float)sides;
                float a = u * Mathf.PI * 2f;
                float c = Mathf.Cos(a);
                float s = Mathf.Sin(a);
                vertices.Add(new Vector3(c * 0.34f, -0.5f, s * 0.34f));
                vertices.Add(new Vector3(c * 0.12f, 0.5f, s * 0.12f));
                uv.Add(new Vector2(u, 0f));
                uv.Add(new Vector2(u, 1f));
                if (i >= sides) continue;
                int a0 = i * 2;
                int a1 = a0 + 2;
                int b0 = a0 + 1;
                int b1 = a0 + 3;
                triangles.Add(a0); triangles.Add(b1); triangles.Add(b0);
                triangles.Add(a0); triangles.Add(a1); triangles.Add(b1);
            }
            return FinishMesh("V27_TaperedAppendage", vertices, uv, triangles);
        }

        private static Mesh BuildCrystalMesh()
        {
            Vector3[] v =
            {
                new Vector3(-0.42f, -0.5f, -0.30f),
                new Vector3(0.42f, -0.5f, -0.30f),
                new Vector3(0.34f, -0.5f, 0.34f),
                new Vector3(-0.34f, -0.5f, 0.34f),
                new Vector3(0.08f, 0.50f, 0.02f),
            };
            int[] t =
            {
                0,1,4, 1,2,4, 2,3,4, 3,0,4,
                0,3,2, 0,2,1,
            };
            Mesh mesh = new Mesh { name = "V27_SignalCrystal" };
            mesh.vertices = v;
            mesh.triangles = t;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh FinishMesh(string name, List<Vector3> vertices, List<Vector2> uv, List<int> triangles)
        {
            Mesh mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float Damp(float value, float target, float sharpness, float dt)
            => Mathf.Lerp(value, target, 1f - Mathf.Exp(-Mathf.Max(0.01f, sharpness) * dt));

        private void OnDestroy()
        {
            if (_hide != null) Destroy(_hide);
            if (_belly != null) Destroy(_belly);
            if (_maw != null) Destroy(_maw);
            if (_corruption != null) Destroy(_corruption);
            if (_eye != null) Destroy(_eye);
        }
    }
}
