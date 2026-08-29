using UnityEngine;
using Mindforge.Journey;

namespace Mindforge.Presentation
{
    public enum HackathonEnemyIdentity
    {
        ScrapGoblin = 0,
        Shardsinger = 1,
        BassGolem = 2,
        ChromePenitent = 3,
        RiftStalker = 4,
        ChoirDrone = 5,
        AeroGargoyle = 6,
        PrismMaw = 7,
        VeilReaper = 8,
        OrbitSeraph = 9,
    }

    /// <summary>
    /// Second-pass enemy art direction for the hackathon playthrough. Every identity gets
    /// a readable silhouette hook that survives the elevated camera: horns, tuning forks,
    /// speaker stacks, wings, orbitals, jaws or executioner geometry. The component only
    /// reads JourneyEnemyController intent/recovery state and never owns hitboxes or AI.
    /// </summary>
    [DefaultExecutionOrder(1090)]
    public sealed class HackathonEnemyPresentationV1 : MonoBehaviour
    {
        public const string RootName = "HackathonEnemyDetailV1";

        [SerializeField] private HackathonEnemyIdentity identity;
        [SerializeField] private JourneyEnemyController controller;

        private Transform _root;
        private Transform _motion;
        private Transform _secondary;
        private Material _dark;
        private Material _accent;
        private bool _built;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public HackathonEnemyIdentity Identity => identity;

        public void Configure(HackathonEnemyIdentity value)
        {
            identity = value;
            if (controller == null) controller = GetComponent<JourneyEnemyController>();
        }

        private void Awake()
        {
            if (controller == null) controller = GetComponent<JourneyEnemyController>();
        }

        private void Start() => Build();

        private void OnDestroy()
        {
            if (_dark != null) Destroy(_dark);
            if (_accent != null) Destroy(_accent);
        }

        private void LateUpdate()
        {
            if (!_built) Build();
            if (!_built || controller == null || _motion == null) return;

            float t = Time.unscaledTime;
            float attack = controller.PendingAttack != JourneyEnemyAttackKind.None ? 1f : 0f;
            float recover = controller.IsRecovering ? 1f : 0f;
            float urgency = Mathf.Max(attack, recover * 0.45f);
            float breath = Mathf.Sin(t * (2.1f + (int)identity * 0.07f)) * 0.5f + 0.5f;

            Vector3 targetScale = Vector3.one * (1f + urgency * 0.055f + breath * 0.012f);
            _motion.localScale = Vector3.Lerp(
                _motion.localScale,
                targetScale,
                1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));

            float yaw = (identity == HackathonEnemyIdentity.OrbitSeraph || identity == HackathonEnemyIdentity.ChoirDrone)
                ? 34f
                : 7f;
            _motion.localRotation *= Quaternion.Euler(0f, yaw * Time.unscaledDeltaTime, 0f);

            if (_secondary != null)
            {
                float signed = Mathf.Sin(t * (3.4f + (int)identity * 0.11f));
                _secondary.localRotation = Quaternion.Euler(
                    signed * (4f + urgency * 8f),
                    t * (identity == HackathonEnemyIdentity.RiftStalker ? 62f : 18f),
                    -signed * 6f);
            }
        }

        private void Build()
        {
            if (_built) return;
            if (controller == null) controller = GetComponent<JourneyEnemyController>();
            Transform visuals = transform.Find("Visuals");
            if (visuals == null) return;

            Transform existing = visuals.Find(RootName);
            if (existing != null)
            {
                _root = existing;
                _motion = existing.Find("DetailMotion");
                _secondary = _motion != null ? _motion.Find("SecondaryMotion") : null;
                _built = true;
                return;
            }

            _dark = CreateMaterial("HackathonEnemyDark_" + identity, new Color(0.035f, 0.045f, 0.068f), 0.72f, 0.58f, Color.black);
            Color accentColor = AccentColor(identity);
            _accent = CreateMaterial("HackathonEnemyAccent_" + identity, accentColor, 0.24f, 0.84f, accentColor * 2.0f);

            _root = Node(RootName, visuals, Vector3.zero);
            _motion = Node("DetailMotion", _root, Vector3.zero);
            _secondary = Node("SecondaryMotion", _motion, Vector3.zero);

            switch (identity)
            {
                case HackathonEnemyIdentity.ScrapGoblin: BuildScrapGoblin(); break;
                case HackathonEnemyIdentity.Shardsinger: BuildShardsinger(); break;
                case HackathonEnemyIdentity.BassGolem: BuildBassGolem(); break;
                case HackathonEnemyIdentity.ChromePenitent: BuildChromePenitent(); break;
                case HackathonEnemyIdentity.RiftStalker: BuildRiftStalker(); break;
                case HackathonEnemyIdentity.ChoirDrone: BuildChoirDrone(); break;
                case HackathonEnemyIdentity.AeroGargoyle: BuildAeroGargoyle(); break;
                case HackathonEnemyIdentity.PrismMaw: BuildPrismMaw(); break;
                case HackathonEnemyIdentity.VeilReaper: BuildVeilReaper(); break;
                case HackathonEnemyIdentity.OrbitSeraph: BuildOrbitSeraph(); break;
            }
            _built = true;
        }

        private void BuildScrapGoblin()
        {
            Part("GoblinEarL", _motion, new Vector3(-0.46f, 0.88f, 0.02f), new Vector3(0.16f, 0.48f, 0.16f), _accent, new Vector3(0f, 0f, 48f));
            Part("GoblinEarR", _motion, new Vector3(0.46f, 0.88f, 0.02f), new Vector3(0.16f, 0.48f, 0.16f), _accent, new Vector3(0f, 0f, -48f));
            Part("GoblinPackFinL", _secondary, new Vector3(-0.32f, 0.40f, -0.56f), new Vector3(0.14f, 0.56f, 0.34f), _dark, new Vector3(18f, -12f, -20f));
            Part("GoblinPackFinR", _secondary, new Vector3(0.32f, 0.40f, -0.56f), new Vector3(0.14f, 0.56f, 0.34f), _dark, new Vector3(18f, 12f, 20f));
        }

        private void BuildShardsinger()
        {
            Part("SingerForkL", _motion, new Vector3(-0.30f, 1.18f, 0f), new Vector3(0.12f, 1.18f, 0.12f), _accent, new Vector3(0f, 0f, -10f));
            Part("SingerForkR", _motion, new Vector3(0.30f, 1.18f, 0f), new Vector3(0.12f, 1.18f, 0.12f), _accent, new Vector3(0f, 0f, 10f));
            Part("SingerCrossbar", _motion, new Vector3(0f, 0.58f, 0f), new Vector3(0.72f, 0.12f, 0.12f), _dark, Vector3.zero);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                Part("SingerShard_" + i, _secondary, new Vector3(Mathf.Cos(a) * 0.80f, 0.68f, Mathf.Sin(a) * 0.80f), new Vector3(0.12f, 0.46f, 0.12f), _accent, new Vector3(i * 17f, i * 31f, i * 11f));
            }
        }

        private void BuildBassGolem()
        {
            Part("GolemShoulderStackL", _motion, new Vector3(-0.88f, 1.20f, 0f), new Vector3(0.52f, 0.62f, 0.72f), _dark, new Vector3(0f, 0f, -8f));
            Part("GolemShoulderStackR", _motion, new Vector3(0.88f, 1.20f, 0f), new Vector3(0.52f, 0.62f, 0.72f), _dark, new Vector3(0f, 0f, 8f));
            Part("GolemSpeakerJaw", _motion, new Vector3(0f, 0.56f, 0.56f), new Vector3(0.82f, 0.24f, 0.16f), _accent, Vector3.zero);
            Part("GolemAnchorL", _motion, new Vector3(-0.62f, 0.02f, 0f), new Vector3(0.44f, 0.22f, 0.70f), _dark, Vector3.zero);
            Part("GolemAnchorR", _motion, new Vector3(0.62f, 0.02f, 0f), new Vector3(0.44f, 0.22f, 0.70f), _dark, Vector3.zero);
        }

        private void BuildChromePenitent()
        {
            Part("PenitentHaloL", _secondary, new Vector3(-0.52f, 1.10f, -0.12f), new Vector3(0.10f, 1.25f, 0.10f), _accent, new Vector3(0f, 0f, -34f));
            Part("PenitentHaloR", _secondary, new Vector3(0.52f, 1.10f, -0.12f), new Vector3(0.10f, 1.25f, 0.10f), _accent, new Vector3(0f, 0f, 34f));
            Part("PenitentCollar", _motion, new Vector3(0f, 0.94f, 0f), new Vector3(1.05f, 0.16f, 0.64f), _dark, Vector3.zero);
            Part("PenitentExecutionEdge", _motion, new Vector3(0.70f, 0.42f, 0.32f), new Vector3(0.12f, 0.18f, 1.28f), _accent, new Vector3(0f, 18f, 34f));
        }

        private void BuildRiftStalker()
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Part("StalkerScytheLegA_" + side, _motion, new Vector3(side * 0.62f, 0.24f, 0.26f), new Vector3(0.12f, 0.18f, 1.12f), _dark, new Vector3(0f, side * 32f, side * 24f));
                Part("StalkerScytheLegB_" + side, _motion, new Vector3(side * 0.72f, 0.18f, -0.38f), new Vector3(0.12f, 0.16f, 1.00f), _accent, new Vector3(0f, -side * 28f, side * 18f));
            }
            Part("StalkerBackSpine", _secondary, new Vector3(0f, 0.76f, -0.38f), new Vector3(0.16f, 1.18f, 0.18f), _accent, new Vector3(-42f, 0f, 0f));
        }

        private void BuildChoirDrone()
        {
            Part("ChoirHaloStem", _motion, new Vector3(0f, 0.90f, 0f), new Vector3(0.12f, 1.35f, 0.12f), _dark, Vector3.zero);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Part("ChoirNode_" + i, _secondary, new Vector3(Mathf.Cos(a) * 0.92f, 0.92f + Mathf.Sin(a * 2f) * 0.16f, Mathf.Sin(a) * 0.92f), new Vector3(0.20f, 0.20f, 0.20f), i % 2 == 0 ? _accent : _dark, Vector3.zero, PrimitiveType.Sphere);
            }
        }

        private void BuildAeroGargoyle()
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Part("GargoyleOuterWing_" + side, _motion, new Vector3(side * 0.92f, 0.82f, -0.10f), new Vector3(1.20f, 0.10f, 0.58f), _dark, new Vector3(0f, side * 12f, side * 28f));
                Part("GargoyleWingEdge_" + side, _motion, new Vector3(side * 1.30f, 0.74f, -0.34f), new Vector3(0.76f, 0.06f, 0.20f), _accent, new Vector3(0f, side * 18f, side * 36f));
            }
            Part("GargoyleTailJet", _secondary, new Vector3(0f, 0.42f, -0.78f), new Vector3(0.22f, 0.22f, 0.90f), _accent, Vector3.zero);
        }

        private void BuildPrismMaw()
        {
            Part("MawUpperJaw", _motion, new Vector3(0f, 0.72f, 0.52f), new Vector3(1.14f, 0.20f, 0.52f), _dark, new Vector3(-10f, 0f, 0f));
            Part("MawLowerJaw", _motion, new Vector3(0f, 0.34f, 0.56f), new Vector3(1.06f, 0.18f, 0.48f), _dark, new Vector3(12f, 0f, 0f));
            for (int i = -2; i <= 2; i++)
                Part("MawTooth_" + i, _secondary, new Vector3(i * 0.20f, 0.54f, 0.84f), new Vector3(0.08f, 0.28f, 0.08f), _accent, new Vector3(24f, 0f, 0f));
        }

        private void BuildVeilReaper()
        {
            Part("ReaperHood", _motion, new Vector3(0f, 1.44f, 0.02f), new Vector3(0.78f, 0.86f, 0.72f), _dark, Vector3.zero);
            Part("ReaperVisor", _motion, new Vector3(0f, 1.42f, 0.38f), new Vector3(0.48f, 0.08f, 0.06f), _accent, Vector3.zero);
            Part("ReaperScytheShaft", _secondary, new Vector3(0.72f, 0.82f, 0.08f), new Vector3(0.10f, 2.30f, 0.10f), _dark, new Vector3(0f, 0f, 18f));
            Part("ReaperScytheBlade", _secondary, new Vector3(0.98f, 1.86f, 0.20f), new Vector3(0.12f, 0.22f, 1.16f), _accent, new Vector3(0f, 36f, 62f));
        }

        private void BuildOrbitSeraph()
        {
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                Part("SeraphOrbital_" + i, _secondary, new Vector3(Mathf.Cos(a) * 1.08f, 0.94f, Mathf.Sin(a) * 1.08f), new Vector3(0.16f, 0.16f, 0.16f), _accent, Vector3.zero, PrimitiveType.Sphere);
            }
            for (int side = -1; side <= 1; side += 2)
                Part("SeraphBladeWing_" + side, _motion, new Vector3(side * 0.76f, 0.82f, -0.08f), new Vector3(0.18f, 1.28f, 0.32f), _dark, new Vector3(-8f, 0f, side * 30f));
        }

        private static Color AccentColor(HackathonEnemyIdentity value)
        {
            switch (value)
            {
                case HackathonEnemyIdentity.ScrapGoblin: return new Color(0.08f, 0.92f, 0.90f);
                case HackathonEnemyIdentity.Shardsinger: return new Color(0.52f, 0.22f, 1f);
                case HackathonEnemyIdentity.BassGolem: return new Color(1f, 0.12f, 0.42f);
                case HackathonEnemyIdentity.ChromePenitent: return new Color(1f, 0.62f, 0.12f);
                case HackathonEnemyIdentity.RiftStalker: return new Color(0.18f, 1f, 0.48f);
                case HackathonEnemyIdentity.ChoirDrone: return new Color(0.18f, 0.76f, 1f);
                case HackathonEnemyIdentity.AeroGargoyle: return new Color(0.96f, 0.18f, 0.72f);
                case HackathonEnemyIdentity.PrismMaw: return new Color(0.64f, 0.20f, 1f);
                case HackathonEnemyIdentity.VeilReaper: return new Color(1f, 0.16f, 0.24f);
                default: return new Color(0.22f, 0.90f, 1f);
            }
        }

        private static Transform Node(string name, Transform parent, Vector3 position)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            return go.transform;
        }

        private static Transform Part(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Vector3 euler,
            PrimitiveType primitive = PrimitiveType.Cube)
        {
            GameObject go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
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
    }
}
