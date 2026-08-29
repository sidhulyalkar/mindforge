using UnityEngine;
using Mindforge.Journey;

namespace Mindforge.Presentation
{
    public enum AetheriaHordeIdentity
    {
        ScrapGoblin = 0,
        BassGolem = 1,
        AeroGargoyle = 2,
    }

    /// <summary>
    /// Presentation-only Aetheria character layer for three story-facing Menagerie roles.
    /// Existing JourneyEnemyController state remains the sole source of attack/death truth.
    /// Decorative armor, RGB salvage, wings and defeat debris own no colliders or damage.
    /// </summary>
    [DefaultExecutionOrder(1080)]
    public sealed class AetheriaHordeCharacterPresentationV1 : MonoBehaviour
    {
        public const string RootName = "AetheriaHordeIdentityV1";

        [SerializeField] private AetheriaHordeIdentity identity;
        [SerializeField] private JourneyEnemyController controller;

        private Transform _root;
        private Transform _identityRoot;
        private Transform[] _armorDebris;
        private Vector3[] _armorStart;
        private Quaternion[] _armorRotations;
        private Transform _tinySkeleton;
        private Transform _leftWing;
        private Transform _rightWing;
        private Transform _jetL;
        private Transform _jetR;
        private Transform _rgbPack;
        private bool _defeatPlaying;
        private float _defeatStarted;

        private Material _dark;
        private Material _metal;
        private Material _cyan;
        private Material _rose;
        private Material _violet;
        private Material _gold;

        public AetheriaHordeIdentity Identity => identity;

        public void Configure(AetheriaHordeIdentity value)
        {
            identity = value;
            if (controller == null) controller = GetComponent<JourneyEnemyController>();
        }

        private void Awake()
        {
            if (controller == null) controller = GetComponent<JourneyEnemyController>();
        }

        private void Start()
        {
            Build();
        }

        private void OnEnable()
        {
            if (controller == null) controller = GetComponent<JourneyEnemyController>();
            if (controller != null)
            {
                controller.Defeated += OnDefeated;
                controller.Reconstructed += OnReconstructed;
            }
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.Defeated -= OnDefeated;
                controller.Reconstructed -= OnReconstructed;
            }
        }

        private void OnDestroy()
        {
            DestroyMaterial(_dark);
            DestroyMaterial(_metal);
            DestroyMaterial(_cyan);
            DestroyMaterial(_rose);
            DestroyMaterial(_violet);
            DestroyMaterial(_gold);
        }

        private void LateUpdate()
        {
            if (_root == null) Build();
            if (_root == null || controller == null) return;

            float t = Time.unscaledTime;
            switch (identity)
            {
                case AetheriaHordeIdentity.ScrapGoblin:
                    AnimateGoblin(t);
                    break;
                case AetheriaHordeIdentity.BassGolem:
                    AnimateGolem(t);
                    break;
                case AetheriaHordeIdentity.AeroGargoyle:
                    AnimateGargoyle(t);
                    break;
            }
        }

        private void Build()
        {
            if (_root != null) return;
            Transform visuals = transform.Find("Visuals");
            if (visuals == null) return;

            Transform existing = visuals.Find(RootName);
            if (existing != null)
            {
                _root = existing;
                return;
            }

            _dark = Material("HordeDark", new Color(0.025f, 0.032f, 0.05f), 0.82f, 0.62f, Color.black);
            _metal = Material("HordeMetal", new Color(0.15f, 0.18f, 0.23f), 0.88f, 0.72f, new Color(0.04f, 0.06f, 0.09f));
            _cyan = Material("HordeCyan", new Color(0.10f, 0.82f, 1f), 0.22f, 0.86f, new Color(0.05f, 0.72f, 1f) * 2.2f);
            _rose = Material("HordeRose", new Color(1f, 0.08f, 0.48f), 0.16f, 0.82f, new Color(1f, 0.03f, 0.34f) * 2.0f);
            _violet = Material("HordeViolet", new Color(0.54f, 0.08f, 1f), 0.28f, 0.84f, new Color(0.46f, 0.06f, 1f) * 2.4f);
            _gold = Material("HordeGold", new Color(1f, 0.62f, 0.08f), 0.62f, 0.80f, new Color(1f, 0.25f, 0.02f) * 1.3f);

            _root = Node(RootName, visuals, Vector3.zero);
            _identityRoot = visuals.Find("MenagerieIdentityV1");

            if (identity == AetheriaHordeIdentity.ScrapGoblin) BuildGoblin();
            else if (identity == AetheriaHordeIdentity.BassGolem) BuildGolem();
            else BuildGargoyle();
        }

        private void BuildGoblin()
        {
            _rgbPack = Node("Goblin_RGBHoard", _root, new Vector3(0f, 0.58f, -0.45f));
            Part("RGB_Cyan", _rgbPack, new Vector3(-0.18f, 0f, 0f), new Vector3(0.12f, 0.38f, 0.12f), _cyan, new Vector3(8f, 0f, -12f));
            Part("RGB_Rose", _rgbPack, new Vector3(0f, 0.04f, -0.03f), new Vector3(0.12f, 0.46f, 0.12f), _rose, new Vector3(-10f, 0f, 7f));
            Part("RGB_Gold", _rgbPack, new Vector3(0.18f, -0.02f, 0.02f), new Vector3(0.12f, 0.34f, 0.12f), _gold, new Vector3(14f, 0f, 9f));
            Part("Goblin_LaserDaggerL", _root, new Vector3(-0.43f, 0.25f, 0.40f), new Vector3(0.075f, 0.08f, 0.86f), _cyan, new Vector3(0f, -18f, -28f));
            Part("Goblin_LaserDaggerR", _root, new Vector3(0.43f, 0.25f, 0.40f), new Vector3(0.075f, 0.08f, 0.86f), _rose, new Vector3(0f, 18f, 28f));
            Part("Goblin_StolenDrive", _root, new Vector3(0f, 0.50f, -0.72f), new Vector3(0.38f, 0.18f, 0.22f), _metal, new Vector3(0f, 15f, 0f));
        }

        private void BuildGolem()
        {
            _armorDebris = new Transform[8];
            _armorStart = new Vector3[_armorDebris.Length];
            _armorRotations = new Quaternion[_armorDebris.Length];

            _armorDebris[0] = Part("Golem_PlateChest", _root, new Vector3(0f, 0.98f, 0.45f), new Vector3(1.18f, 0.78f, 0.18f), _metal, Vector3.zero);
            _armorDebris[1] = Part("Golem_PlateBack", _root, new Vector3(0f, 1.00f, -0.42f), new Vector3(1.10f, 0.72f, 0.16f), _dark, Vector3.zero);
            _armorDebris[2] = Part("Golem_ShoulderL", _root, new Vector3(-0.78f, 1.22f, 0f), new Vector3(0.48f, 0.42f, 0.64f), _metal, new Vector3(0f, 0f, -10f));
            _armorDebris[3] = Part("Golem_ShoulderR", _root, new Vector3(0.78f, 1.22f, 0f), new Vector3(0.48f, 0.42f, 0.64f), _metal, new Vector3(0f, 0f, 10f));
            _armorDebris[4] = Part("Golem_HelmL", _root, new Vector3(-0.30f, 1.78f, 0.06f), new Vector3(0.42f, 0.50f, 0.48f), _dark, new Vector3(0f, 0f, -8f));
            _armorDebris[5] = Part("Golem_HelmR", _root, new Vector3(0.30f, 1.78f, 0.06f), new Vector3(0.42f, 0.50f, 0.48f), _dark, new Vector3(0f, 0f, 8f));
            _armorDebris[6] = Part("Golem_HipL", _root, new Vector3(-0.45f, 0.36f, 0f), new Vector3(0.42f, 0.32f, 0.50f), _metal, Vector3.zero);
            _armorDebris[7] = Part("Golem_HipR", _root, new Vector3(0.45f, 0.36f, 0f), new Vector3(0.42f, 0.32f, 0.50f), _metal, Vector3.zero);

            for (int i = 0; i < _armorDebris.Length; i++)
            {
                _armorStart[i] = _armorDebris[i].localPosition;
                _armorRotations[i] = _armorDebris[i].localRotation;
            }

            Transform speaker = Node("BassGolem_SubwooferCore", _root, new Vector3(0f, 0.98f, 0.56f));
            Part("BassConeOuter", speaker, Vector3.zero, new Vector3(0.62f, 0.62f, 0.10f), _dark, new Vector3(90f, 0f, 0f), PrimitiveType.Cylinder);
            Part("BassConeMid", speaker, new Vector3(0f, 0f, 0.08f), new Vector3(0.43f, 0.43f, 0.08f), _violet, new Vector3(90f, 0f, 0f), PrimitiveType.Cylinder);
            Part("BassConeHot", speaker, new Vector3(0f, 0f, 0.15f), new Vector3(0.16f, 0.16f, 0.07f), _rose, new Vector3(90f, 0f, 0f), PrimitiveType.Cylinder);

            _tinySkeleton = Node("BassGolem_TinyEmbarrassedSkeleton", _root, new Vector3(0f, 0.44f, 0f));
            Part("TinySpine", _tinySkeleton, new Vector3(0f, 0.30f, 0f), new Vector3(0.15f, 0.60f, 0.14f), _metal, Vector3.zero);
            Part("TinyHead", _tinySkeleton, new Vector3(0f, 0.72f, 0f), new Vector3(0.32f, 0.30f, 0.30f), _dark, Vector3.zero);
            Part("TinyArmL", _tinySkeleton, new Vector3(-0.21f, 0.36f, 0f), new Vector3(0.10f, 0.46f, 0.10f), _metal, new Vector3(0f, 0f, -22f));
            Part("TinyArmR", _tinySkeleton, new Vector3(0.21f, 0.36f, 0f), new Vector3(0.10f, 0.46f, 0.10f), _metal, new Vector3(0f, 0f, 22f));
            Part("TinyEye", _tinySkeleton, new Vector3(0f, 0.73f, 0.16f), new Vector3(0.16f, 0.035f, 0.025f), _rose, Vector3.zero);
            _tinySkeleton.gameObject.SetActive(false);
        }

        private void BuildGargoyle()
        {
            _leftWing = Node("Gargoyle_WingL", _root, new Vector3(-0.42f, 0.88f, -0.08f));
            _rightWing = Node("Gargoyle_WingR", _root, new Vector3(0.42f, 0.88f, -0.08f));
            Part("WingBladeL_A", _leftWing, new Vector3(-0.42f, 0f, -0.08f), new Vector3(0.78f, 0.10f, 0.42f), _metal, new Vector3(0f, -12f, -24f));
            Part("WingBladeL_B", _leftWing, new Vector3(-0.70f, -0.05f, -0.32f), new Vector3(0.68f, 0.08f, 0.32f), _dark, new Vector3(0f, 16f, -12f));
            Part("WingBladeR_A", _rightWing, new Vector3(0.42f, 0f, -0.08f), new Vector3(0.78f, 0.10f, 0.42f), _metal, new Vector3(0f, 12f, 24f));
            Part("WingBladeR_B", _rightWing, new Vector3(0.70f, -0.05f, -0.32f), new Vector3(0.68f, 0.08f, 0.32f), _dark, new Vector3(0f, -16f, 12f));
            Part("Gargoyle_HornL", _root, new Vector3(-0.18f, 1.36f, 0.10f), new Vector3(0.10f, 0.46f, 0.10f), _metal, new Vector3(-20f, 0f, -18f));
            Part("Gargoyle_HornR", _root, new Vector3(0.18f, 1.36f, 0.10f), new Vector3(0.10f, 0.46f, 0.10f), _metal, new Vector3(-20f, 0f, 18f));
            _jetL = Part("Gargoyle_JetL", _root, new Vector3(-0.30f, 0.60f, -0.48f), new Vector3(0.14f, 0.14f, 0.64f), _cyan, Vector3.zero);
            _jetR = Part("Gargoyle_JetR", _root, new Vector3(0.30f, 0.60f, -0.48f), new Vector3(0.14f, 0.14f, 0.64f), _rose, Vector3.zero);
        }

        private void AnimateGoblin(float time)
        {
            if (_rgbPack == null) return;
            float urgency = controller.PendingAttack != JourneyEnemyAttackKind.None ? 1f : controller.IsRecovering ? 0.55f : 0.25f;
            float wobble = Mathf.Sin(time * (8.5f + urgency * 5f));
            _rgbPack.localRotation = Quaternion.Euler(wobble * 10f * urgency, wobble * 14f, -wobble * 12f);
        }

        private void AnimateGolem(float time)
        {
            if (_defeatPlaying)
            {
                AnimateArmorExplosion(time);
                return;
            }

            Transform speaker = _root != null ? _root.Find("BassGolem_SubwooferCore") : null;
            if (speaker == null) return;
            float telegraph = controller.AttackTelegraphProgress01;
            float pulse = 1f + Mathf.Sin(time * 6.2f) * 0.035f + telegraph * 0.22f;
            speaker.localScale = Vector3.one * pulse;
        }

        private void AnimateArmorExplosion(float time)
        {
            if (_armorDebris == null) return;
            float elapsed = Mathf.Max(0f, time - _defeatStarted);
            float p = Mathf.Clamp01(elapsed / 0.72f);
            for (int i = 0; i < _armorDebris.Length; i++)
            {
                Transform plate = _armorDebris[i];
                if (plate == null) continue;
                float angle = i / (float)_armorDebris.Length * Mathf.PI * 2f;
                Vector3 outward = new Vector3(Mathf.Cos(angle), 0.45f + (i % 3) * 0.18f, Mathf.Sin(angle));
                Vector3 ballistic = outward * (1.5f + i * 0.10f) * p + Vector3.down * (1.6f * p * p);
                plate.localPosition = _armorStart[i] + ballistic;
                plate.localRotation = _armorRotations[i] * Quaternion.Euler(p * (110f + i * 31f), p * (170f + i * 17f), p * (90f + i * 23f));
            }
            if (_tinySkeleton != null && elapsed > 0.14f) _tinySkeleton.gameObject.SetActive(true);
            if (_identityRoot != null && elapsed > 0.28f)
            {
                Renderer[] renderers = _identityRoot.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = false;
            }
        }

        private void AnimateGargoyle(float time)
        {
            float telegraph = controller.AttackTelegraphProgress01;
            float lockSnap = controller.AttackTrackingLocked ? 1f : 0f;
            float flap = Mathf.Sin(time * (7.0f + telegraph * 5.0f));
            if (_leftWing != null) _leftWing.localRotation = Quaternion.Euler(0f, -18f - telegraph * 14f, -18f + flap * 9f - lockSnap * 7f);
            if (_rightWing != null) _rightWing.localRotation = Quaternion.Euler(0f, 18f + telegraph * 14f, 18f - flap * 9f + lockSnap * 7f);
            float jet = 0.74f + telegraph * 0.65f + (controller.CurrentAttackId == "gargoyle_dive" ? 0.55f : 0f);
            if (_jetL != null) _jetL.localScale = new Vector3(0.14f, 0.14f, jet);
            if (_jetR != null) _jetR.localScale = new Vector3(0.14f, 0.14f, jet);
        }

        private void OnDefeated(JourneyEnemyController enemy)
        {
            if (identity != AetheriaHordeIdentity.BassGolem) return;
            _defeatPlaying = true;
            _defeatStarted = Time.unscaledTime;
        }

        private void OnReconstructed(JourneyEnemyController enemy)
        {
            _defeatPlaying = false;
            if (_tinySkeleton != null) _tinySkeleton.gameObject.SetActive(false);
            if (_armorDebris != null)
            {
                for (int i = 0; i < _armorDebris.Length; i++)
                {
                    if (_armorDebris[i] == null) continue;
                    _armorDebris[i].localPosition = _armorStart[i];
                    _armorDebris[i].localRotation = _armorRotations[i];
                }
            }
            if (_identityRoot != null)
            {
                Renderer[] renderers = _identityRoot.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = true;
            }
        }

        private static Transform Node(string name, Transform parent, Vector3 localPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static Transform Part(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, Vector3 euler, PrimitiveType type = PrimitiveType.Cube)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = localScale;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            return go.transform;
        }

        private static Material Material(string name, Color color, float metallic, float smoothness, Color emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            return material;
        }

        private static void DestroyMaterial(Material material)
        {
            if (material != null) Destroy(material);
        }
    }
}
