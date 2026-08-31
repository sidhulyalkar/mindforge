using System.Collections;
using Mindforge.Combat;
using Mindforge.SoulWisp;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Render-only character body for the first Fractured Signal encounter.
    ///
    /// The old cylinder/shard-cloud presentation is replaced by an asymmetric broken knight:
    /// faceted heart, mask, shoulders, articulated floating arms, fracture blade, ragged plates,
    /// crown and halo. The authoritative Rigidbody/collider/vitals remain on the existing boss.
    ///
    /// All idle/pose/material animation freezes to a neutral state while a calibration or Wisp
    /// resonance visual field is active so the boss cannot become an uncontrolled SSVEP stimulus.
    /// </summary>
    [DefaultExecutionOrder(455)]
    [RequireComponent(typeof(FracturedSignalDirector))]
    public sealed class FracturedSignalCharacterV19 : MonoBehaviour
    {
        public const string RootName = "FracturedSignalCharacterV19";

        [SerializeField] private FracturedSignalDirector director;
        [SerializeField] private CombatantVitals vitals;
        [SerializeField] private FracturedSignalFirstBossV19 movement;
        [SerializeField] private SoulWispController wisp;

        private Transform _root;
        private Transform _torso;
        private Transform _heart;
        private Transform _head;
        private Transform _leftShoulder;
        private Transform _rightShoulder;
        private Transform _leftUpperArm;
        private Transform _leftForearm;
        private Transform _rightUpperArm;
        private Transform _rightForearm;
        private Transform _fractureBlade;
        private Transform _halo;
        private Transform[] _crown;
        private Transform[] _skirts;
        private Renderer _heartRenderer;
        private Renderer _haloRenderer;
        private Material _armorMaterial;
        private Material _edgeMaterial;
        private Material _coreMaterial;
        private Material _voidMaterial;
        private MaterialPropertyBlock _block;
        private float _charge;
        private float _release;
        private float _damage;
        private float _heavy;
        private int _phase = 1;
        private bool _neuralFrozen;
        private bool _built;

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
                if (boss != null && boss.GetComponent<FracturedSignalCharacterV19>() == null)
                    boss.gameObject.AddComponent<FracturedSignalCharacterV19>();
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
                director.PhaseChanged += OnPhase;
                director.AttackTelegraphed += OnTelegraph;
                director.AttackFired += OnFired;
            }
            if (vitals != null) vitals.Damaged += OnDamaged;
        }

        private IEnumerator Start()
        {
            // Older presentation installers may build one or two frames after this component.
            // Hide them again once runtime composition has settled.
            yield return null;
            yield return null;
            HideLegacyPresentation();
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.PhaseChanged -= OnPhase;
                director.AttackTelegraphed -= OnTelegraph;
                director.AttackFired -= OnFired;
            }
            if (vitals != null) vitals.Damaged -= OnDamaged;
        }

        private void Resolve()
        {
            if (director == null) director = GetComponent<FracturedSignalDirector>();
            if (vitals == null) vitals = GetComponent<CombatantVitals>();
            if (movement == null) movement = GetComponent<FracturedSignalFirstBossV19>();
            if (wisp == null) wisp = FindObjectOfType<SoulWispController>(true);
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

            _armorMaterial = CreateMaterial(
                "SignalKnight_Graphite",
                new Color(0.055f, 0.065f, 0.095f),
                0.82f, 0.52f,
                new Color(0.08f, 0.03f, 0.12f) * 0.10f);
            _edgeMaterial = CreateMaterial(
                "SignalKnight_FractureEdge",
                new Color(0.17f, 0.10f, 0.22f),
                0.70f, 0.60f,
                new Color(0.78f, 0.09f, 1f) * 0.65f);
            _coreMaterial = CreateMaterial(
                "SignalKnight_Heart",
                new Color(0.19f, 0.018f, 0.045f),
                0.42f, 0.76f,
                new Color(1f, 0.055f, 0.20f) * 2.25f);
            _voidMaterial = CreateMaterial(
                "SignalKnight_VoidMask",
                new Color(0.008f, 0.010f, 0.018f),
                0.10f, 0.24f,
                Color.black);
            _block = new MaterialPropertyBlock();

            _root = new GameObject(RootName).transform;
            _root.SetParent(transform, false);
            _root.localPosition = new Vector3(0f, 0.30f, 0f);

            _torso = Node("BrokenTorso", _root, new Vector3(0f, 1.12f, 0f));
            Part("TorsoPlate_L", OpenSourceMeshPrimitivesV19.CreateShard(0.92f, 1.60f, 0.34f, -0.12f), _torso,
                new Vector3(-0.38f, 0f, 0.03f), new Vector3(0f, 0f, 14f), Vector3.one, _armorMaterial);
            Part("TorsoPlate_R", OpenSourceMeshPrimitivesV19.CreateShard(1.00f, 1.72f, 0.38f, 0.16f), _torso,
                new Vector3(0.35f, 0.04f, -0.02f), new Vector3(0f, 180f, -11f), Vector3.one, _armorMaterial);
            Part("SternumFracture", OpenSourceMeshPrimitivesV19.CreateShard(0.30f, 1.25f, 0.16f, 0.05f), _torso,
                new Vector3(0.02f, 0.02f, -0.30f), new Vector3(4f, 0f, 0f), Vector3.one, _edgeMaterial);

            _heart = Part("FracturedHeart", OpenSourceMeshPrimitivesV19.CreateFacetedIcosahedron(0.48f), _root,
                new Vector3(0f, 1.08f, -0.34f), new Vector3(0f, 18f, 0f), new Vector3(0.88f, 1.12f, 0.78f), _coreMaterial);
            _heartRenderer = _heart.GetComponent<Renderer>();

            _head = Node("SignalMaskRig", _root, new Vector3(0f, 2.30f, -0.02f));
            Part("Mask", OpenSourceMeshPrimitivesV19.CreateShard(0.74f, 0.98f, 0.30f, 0.03f), _head,
                Vector3.zero, new Vector3(5f, 180f, 0f), Vector3.one, _armorMaterial);
            Part("MaskVoid", OpenSourceMeshPrimitivesV19.CreateShard(0.31f, 0.46f, 0.08f, 0.00f), _head,
                new Vector3(0f, 0.02f, -0.17f), new Vector3(2f, 180f, 0f), Vector3.one, _voidMaterial);
            Part("MaskScar", OpenSourceMeshPrimitivesV19.CreateShard(0.09f, 0.58f, 0.05f, 0.04f), _head,
                new Vector3(0.10f, 0.02f, -0.225f), new Vector3(0f, 180f, -17f), Vector3.one, _coreMaterial);

            _leftShoulder = Part("LeftShoulder", OpenSourceMeshPrimitivesV19.CreateFacetedIcosahedron(0.52f), _root,
                new Vector3(-0.98f, 1.76f, 0.06f), new Vector3(0f, 20f, -10f), new Vector3(1.16f, 0.68f, 0.88f), _armorMaterial);
            _rightShoulder = Part("RightShoulder", OpenSourceMeshPrimitivesV19.CreateShard(0.95f, 1.22f, 0.46f, 0.20f), _root,
                new Vector3(1.05f, 1.83f, 0.02f), new Vector3(0f, -8f, -68f), Vector3.one, _edgeMaterial);

            _leftUpperArm = Part("LeftUpperArm", OpenSourceMeshPrimitivesV19.CreateShard(0.34f, 0.98f, 0.26f, -0.08f), _root,
                new Vector3(-1.15f, 1.18f, 0.03f), new Vector3(0f, 0f, -9f), Vector3.one, _armorMaterial);
            _leftForearm = Part("LeftForearm", OpenSourceMeshPrimitivesV19.CreateShard(0.30f, 0.90f, 0.23f, 0.10f), _root,
                new Vector3(-1.25f, 0.58f, -0.03f), new Vector3(0f, 180f, 8f), Vector3.one, _edgeMaterial);
            Part("LeftPalm", OpenSourceMeshPrimitivesV19.CreateFacetedIcosahedron(0.22f), _root,
                new Vector3(-1.29f, 0.10f, -0.02f), new Vector3(0f, 0f, 0f), new Vector3(0.9f, 1.15f, 0.72f), _armorMaterial);

            _rightUpperArm = Part("RightUpperArm", OpenSourceMeshPrimitivesV19.CreateShard(0.42f, 1.05f, 0.30f, 0.10f), _root,
                new Vector3(1.19f, 1.20f, 0.02f), new Vector3(0f, 180f, 12f), Vector3.one, _armorMaterial);
            _rightForearm = Part("RightForearm", OpenSourceMeshPrimitivesV19.CreateShard(0.38f, 1.02f, 0.28f, -0.09f), _root,
                new Vector3(1.36f, 0.54f, -0.05f), new Vector3(0f, 0f, -12f), Vector3.one, _edgeMaterial);
            _fractureBlade = Part("FractureBlade", OpenSourceMeshPrimitivesV19.CreateShard(0.34f, 2.35f, 0.19f, 0.18f), _root,
                new Vector3(1.55f, -0.40f, -0.07f), new Vector3(0f, 180f, -10f), Vector3.one, _coreMaterial);

            _halo = Part("BrokenHalo", OpenSourceMeshPrimitivesV19.CreateTorus(0.88f, 0.055f, 32, 6), _root,
                new Vector3(0f, 2.33f, 0.36f), new Vector3(90f, 0f, 0f), Vector3.one, _edgeMaterial);
            _haloRenderer = _halo.GetComponent<Renderer>();

            _crown = new Transform[5];
            for (int i = 0; i < _crown.Length; i++)
            {
                float x = (i - 2) * 0.25f;
                float height = i == 3 ? 1.02f : 0.58f + (i % 3) * 0.14f;
                _crown[i] = Part($"Crown_{i:00}", OpenSourceMeshPrimitivesV19.CreateShard(0.18f, height, 0.14f, (i - 2) * 0.025f), _root,
                    new Vector3(x, 2.82f + (i == 3 ? 0.12f : 0f), 0.10f),
                    new Vector3(0f, i * 7f, (i - 2) * -8f), Vector3.one, i == 3 ? _coreMaterial : _edgeMaterial);
            }

            _skirts = new Transform[7];
            for (int i = 0; i < _skirts.Length; i++)
            {
                float a = Mathf.Lerp(-118f, 118f, i / (float)(_skirts.Length - 1)) * Mathf.Deg2Rad;
                float x = Mathf.Sin(a) * 0.72f;
                float z = Mathf.Cos(a) * 0.30f + 0.10f;
                _skirts[i] = Part($"RaggedPlate_{i:00}", OpenSourceMeshPrimitivesV19.CreateShard(0.42f, 1.18f + (i % 2) * 0.22f, 0.22f, (i % 3 - 1) * 0.08f), _root,
                    new Vector3(x, 0.05f - (i % 3) * 0.07f, z),
                    new Vector3(6f + i * 2f, -a * Mathf.Rad2Deg, (i - 3) * 4f), Vector3.one,
                    i % 3 == 0 ? _edgeMaterial : _armorMaterial);
            }

            _built = true;
            ApplyPose(0f, true);
        }

        private void LateUpdate()
        {
            if (!_built) Build();
            if (!_built || _root == null) return;
            HideLegacyPresentation();
            Resolve();

            if (NeuralVisualFieldActive())
            {
                if (!_neuralFrozen)
                {
                    _neuralFrozen = true;
                    _charge = 0f;
                    _release = 0f;
                    _damage = 0f;
                    _heavy = 0f;
                    ApplyPose(Time.unscaledTime, true);
                }
                return;
            }

            _neuralFrozen = false;
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            _charge = Damp(_charge, 0f, 2.9f, dt);
            _release = Damp(_release, 0f, 7.5f, dt);
            _damage = Damp(_damage, 0f, 8.4f, dt);
            _heavy = Damp(_heavy, 0f, 3.7f, dt);
            ApplyPose(Time.unscaledTime, false);
        }

        private void ApplyPose(float time, bool neutral)
        {
            if (_root == null) return;
            float phase01 = Mathf.InverseLerp(1f, 3f, _phase);
            float moving = movement != null && movement.MovementActive ? 1f : 0f;
            float idle = neutral ? 0f : Mathf.Sin(time * Mathf.Lerp(1.15f, 1.55f, phase01));
            float stride = neutral ? 0f : Mathf.Sin(time * (2.5f + phase01 * 0.9f)) * moving;

            _root.localPosition = new Vector3(
                0f,
                0.30f + idle * (0.035f + phase01 * 0.018f) + _release * 0.055f,
                _damage * -0.075f);
            _root.localRotation = Quaternion.Euler(
                _damage * 3.5f,
                stride * 2.4f,
                idle * 0.65f + stride * 1.4f);

            if (_torso != null)
                _torso.localRotation = Quaternion.Euler(-_charge * (5f + _heavy * 4f) + _release * 4f, stride * 1.6f, idle * 0.8f);
            if (_head != null)
                _head.localRotation = Quaternion.Euler(_charge * -7f + _release * 4f, idle * 1.4f, -stride * 0.8f);

            if (_leftShoulder != null)
                _leftShoulder.localRotation = Quaternion.Euler(stride * 3f, 20f, -10f - _charge * 10f);
            if (_rightShoulder != null)
                _rightShoulder.localRotation = Quaternion.Euler(-stride * 2f, -8f, -68f + _charge * 17f - _release * 9f);
            if (_leftUpperArm != null)
                _leftUpperArm.localRotation = Quaternion.Euler(0f, 0f, -9f + idle * 2f + _charge * 14f);
            if (_leftForearm != null)
                _leftForearm.localRotation = Quaternion.Euler(0f, 180f, 8f - _charge * 18f);
            if (_rightUpperArm != null)
                _rightUpperArm.localRotation = Quaternion.Euler(_charge * -8f, 180f, 12f - _charge * 24f + _release * 11f);
            if (_rightForearm != null)
                _rightForearm.localRotation = Quaternion.Euler(_charge * -12f, 0f, -12f - _charge * 28f + _release * 18f);
            if (_fractureBlade != null)
                _fractureBlade.localRotation = Quaternion.Euler(0f, 180f, -10f - _charge * 31f + _release * 21f);

            if (_halo != null)
            {
                _halo.localRotation = Quaternion.Euler(90f, neutral ? 0f : time * (5f + phase01 * 4f), _charge * 5f);
                _halo.localScale = Vector3.one * (1f + _charge * 0.08f + _release * 0.05f);
            }

            if (_crown != null)
            {
                for (int i = 0; i < _crown.Length; i++)
                {
                    Transform crown = _crown[i];
                    if (crown == null) continue;
                    float sway = neutral ? 0f : Mathf.Sin(time * 0.72f + i * 0.8f) * 2.2f;
                    crown.localRotation = Quaternion.Euler(0f, i * 7f, (i - 2) * -8f + sway + _charge * (i - 2) * 2f);
                }
            }

            if (_skirts != null)
            {
                for (int i = 0; i < _skirts.Length; i++)
                {
                    Transform plate = _skirts[i];
                    if (plate == null) continue;
                    float flutter = neutral ? 0f : Mathf.Sin(time * 1.35f + i * 0.77f) * (2.5f + moving * 3f);
                    Vector3 euler = plate.localEulerAngles;
                    plate.localRotation = Quaternion.Euler(6f + i * 2f + flutter, euler.y, (i - 3) * 4f - stride * 2f);
                }
            }

            ApplyPulse(_heartRenderer, new Color(1f, 0.055f, 0.20f), 2.15f + phase01 * 0.65f + _charge * 1.2f + _release * 2.4f);
            ApplyPulse(_haloRenderer, new Color(0.72f, 0.08f, 1f), 0.65f + phase01 * 0.35f + _charge * 0.75f);
        }

        private bool NeuralVisualFieldActive()
        {
            if (wisp == null) wisp = FindObjectOfType<SoulWispController>(true);
            return wisp != null && (wisp.CalibrationStimuliActive || wisp.ResonanceWindowActive);
        }

        private void HideLegacyPresentation()
        {
            Renderer legacy = GetComponent<Renderer>();
            if (legacy != null) legacy.enabled = false;

            Transform showcase = transform.Find("FracturedSignalShowcaseAvatar");
            if (showcase != null && showcase.gameObject.activeSelf) showcase.gameObject.SetActive(false);
            Transform threat = transform.Find("FracturedSignalThreatSilhouette");
            if (threat != null && threat.gameObject.activeSelf) threat.gameObject.SetActive(false);
        }

        private void OnPhase(int phase)
        {
            _phase = Mathf.Clamp(phase, 1, 3);
            _release = 1f;
        }

        private void OnTelegraph(string pattern, int count, bool heavy)
        {
            _charge = 1f;
            _heavy = heavy ? 1f : 0.25f;
        }

        private void OnFired(string pattern, int count, bool heavy)
        {
            _charge = 0f;
            _release = heavy ? 1f : 0.72f;
            _heavy = heavy ? 1f : Mathf.Max(_heavy, 0.25f);
        }

        private void OnDamaged(DamagePacket packet)
        {
            if (packet.Damage > 0f) _damage = 1f;
        }

        private void ApplyPulse(Renderer renderer, Color color, float intensity)
        {
            if (renderer == null || _block == null) return;
            renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColor, color * 0.24f);
            _block.SetColor(ColorProperty, color * 0.24f);
            _block.SetColor(EmissionColor, color * Mathf.Max(0f, intensity));
            renderer.SetPropertyBlock(_block);
        }

        private static Transform Node(string name, Transform parent, Vector3 localPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static Transform Part(
            string name,
            Mesh mesh,
            Transform parent,
            Vector3 localPosition,
            Vector3 localEuler,
            Vector3 localScale,
            Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;
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

        private static float Damp(float value, float target, float sharpness, float dt)
            => Mathf.Lerp(value, target, 1f - Mathf.Exp(-Mathf.Max(0.01f, sharpness) * dt));
    }
}
