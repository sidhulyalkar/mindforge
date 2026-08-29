using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Serious cyber-warlock presentation for the existing Fractured Signal boss authority.
    /// Projectile cadence, melee resolution, health phases and checkpoint behavior remain in
    /// FracturedSignalDirector / FracturedSignalMeleeDirector. This layer only interprets
    /// their events as controlled body motion, hard-light weapon pose and emission.
    /// </summary>
    [DefaultExecutionOrder(1200)]
    public sealed class LordMalatractPresentationV1 : MonoBehaviour
    {
        public const string RootName = "LordMalatractPresentationV1";

        [SerializeField] private FracturedSignalDirector director;
        [SerializeField] private FracturedSignalMeleeDirector melee;
        [SerializeField] private CombatantVitals vitals;

        private Transform _root;
        private Transform _torso;
        private Transform _head;
        private Transform _leftArm;
        private Transform _rightArm;
        private Transform _weaponPivot;
        private Transform _weaponCore;
        private Transform _crownL;
        private Transform _crownR;
        private Transform[] _mantleCables;
        private Light _visorLight;
        private int _phase = 1;
        private float _charge;
        private float _fire;
        private float _meleeCharge;
        private float _damageFlash;
        private string _meleePattern = string.Empty;

        private Material _black;
        private Material _metal;
        private Material _violet;
        private Material _hot;
        private Material _cyan;

        private void Awake()
        {
            Resolve();
        }

        private void Start()
        {
            DisableLegacyBossAvatar();
            Build();
        }

        private void OnEnable()
        {
            Resolve();
            if (director != null)
            {
                director.PhaseChanged += OnPhaseChanged;
                director.AttackTelegraphed += OnAttackTelegraphed;
                director.AttackFired += OnAttackFired;
                _phase = director.Phase;
            }
            if (melee != null)
            {
                melee.MeleeTelegraphed += OnMeleeTelegraphed;
                melee.MeleeResolved += OnMeleeResolved;
            }
            if (vitals != null) vitals.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.PhaseChanged -= OnPhaseChanged;
                director.AttackTelegraphed -= OnAttackTelegraphed;
                director.AttackFired -= OnAttackFired;
            }
            if (melee != null)
            {
                melee.MeleeTelegraphed -= OnMeleeTelegraphed;
                melee.MeleeResolved -= OnMeleeResolved;
            }
            if (vitals != null) vitals.Damaged -= OnDamaged;
        }

        private void OnDestroy()
        {
            DestroyMaterial(_black);
            DestroyMaterial(_metal);
            DestroyMaterial(_violet);
            DestroyMaterial(_hot);
            DestroyMaterial(_cyan);
        }

        private void LateUpdate()
        {
            if (_root == null)
            {
                DisableLegacyBossAvatar();
                Build();
            }
            if (_root == null) return;

            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            float time = Time.unscaledTime;
            _charge = Mathf.MoveTowards(_charge, 0f, dt * 1.35f);
            _fire = Mathf.MoveTowards(_fire, 0f, dt * 4.5f);
            _meleeCharge = Mathf.MoveTowards(_meleeCharge, 0f, dt * 1.9f);
            _damageFlash = Mathf.MoveTowards(_damageFlash, 0f, dt * 5.6f);

            float phase01 = (_phase - 1) * 0.5f;
            float breath = Mathf.Sin(time * (1.15f + phase01 * 0.28f)) * 0.012f;
            if (_torso != null)
                _torso.localPosition = new Vector3(0f, 1.82f + breath + _damageFlash * 0.025f, 0f);

            if (_head != null)
            {
                float attention = _charge * 3.2f + _meleeCharge * 2.2f;
                _head.localRotation = Quaternion.Euler(-2f - _meleeCharge * 4f, Mathf.Sin(time * 0.36f) * 1.1f, attention * 0.35f);
            }

            AnimateArms(time);
            AnimateWeapon(time, phase01);
            AnimateCrown(phase01);
            AnimateMantle(time, phase01);

            if (_visorLight != null)
            {
                Color baseColor = _phase == 1
                    ? new Color(0.52f, 0.04f, 0.86f)
                    : _phase == 2 ? new Color(0.72f, 0.06f, 1f) : new Color(1f, 0.05f, 0.34f);
                _visorLight.color = Color.Lerp(baseColor, Color.white, Mathf.Max(_fire, _damageFlash) * 0.58f);
                _visorLight.intensity = 1.5f + phase01 * 1.4f + _charge * 2.5f + _fire * 3.2f;
                _visorLight.range = 4.8f + phase01 * 1.2f;
            }
        }

        private void Resolve()
        {
            if (director == null) director = GetComponent<FracturedSignalDirector>();
            if (melee == null) melee = GetComponent<FracturedSignalMeleeDirector>();
            if (vitals == null) vitals = GetComponent<CombatantVitals>();
        }

        private void DisableLegacyBossAvatar()
        {
            Transform legacy = transform.Find("FracturedSignalShowcaseAvatar");
            if (legacy == null) return;
            Renderer[] renderers = legacy.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = false;
            LineRenderer[] lines = legacy.GetComponentsInChildren<LineRenderer>(true);
            for (int i = 0; i < lines.Length; i++) lines[i].enabled = false;
            Light[] lights = legacy.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++) lights[i].enabled = false;
        }

        private void Build()
        {
            if (_root != null) return;
            Transform existing = transform.Find(RootName);
            if (existing != null)
            {
                _root = existing;
                return;
            }

            _black = Material("Malatract_Obsidian", new Color(0.015f, 0.018f, 0.028f), 0.88f, 0.76f, new Color(0.01f, 0.01f, 0.02f));
            _metal = Material("Malatract_Titanium", new Color(0.10f, 0.12f, 0.16f), 0.94f, 0.82f, new Color(0.02f, 0.025f, 0.04f));
            _violet = Material("Malatract_Control", new Color(0.34f, 0.03f, 0.60f), 0.40f, 0.90f, new Color(0.42f, 0.02f, 0.90f) * 2.7f);
            _hot = Material("Malatract_Hot", new Color(0.92f, 0.06f, 0.30f), 0.22f, 0.94f, new Color(1f, 0.02f, 0.22f) * 3.1f);
            _cyan = Material("Malatract_StolenAether", new Color(0.06f, 0.55f, 0.72f), 0.34f, 0.88f, new Color(0.04f, 0.62f, 0.82f) * 1.7f);

            _root = Node(RootName, transform, Vector3.zero);
            _torso = Node("Malatract_TorsoRig", _root, new Vector3(0f, 1.82f, 0f));
            Part("TorsoArmor", _torso, Vector3.zero, new Vector3(1.10f, 1.46f, 0.70f), _black, Vector3.zero);
            Part("ChestControlBar", _torso, new Vector3(0f, 0.16f, 0.37f), new Vector3(0.58f, 0.08f, 0.045f), _violet, Vector3.zero);
            Part("ChestAetherSeal", _torso, new Vector3(0f, -0.15f, 0.39f), new Vector3(0.22f, 0.22f, 0.04f), _cyan, new Vector3(0f, 0f, 45f));
            Part("Waist", _root, new Vector3(0f, 0.95f, 0f), new Vector3(0.70f, 0.38f, 0.55f), _metal, Vector3.zero);

            BuildLeg(-1f, "L");
            BuildLeg(1f, "R");
            BuildArm(-1f, "L", out _leftArm);
            BuildArm(1f, "R", out _rightArm);

            _head = Node("Malatract_HeadRig", _root, new Vector3(0f, 2.92f, 0f));
            Part("MalatractMask", _head, Vector3.zero, new Vector3(0.58f, 0.62f, 0.52f), _black, Vector3.zero);
            Part("MalatractVisor", _head, new Vector3(0f, 0.02f, 0.276f), new Vector3(0.42f, 0.055f, 0.026f), _hot, Vector3.zero);
            Part("MaskJaw", _head, new Vector3(0f, -0.25f, 0.10f), new Vector3(0.40f, 0.22f, 0.38f), _metal, new Vector3(8f, 0f, 0f));

            _crownL = Part("MalatractCrownL", _head, new Vector3(-0.24f, 0.52f, -0.02f), new Vector3(0.09f, 0.82f, 0.12f), _metal, new Vector3(-16f, 0f, -21f));
            _crownR = Part("MalatractCrownR", _head, new Vector3(0.24f, 0.52f, -0.02f), new Vector3(0.09f, 0.82f, 0.12f), _metal, new Vector3(-16f, 0f, 21f));
            Part("CrownSignalL", _crownL, new Vector3(0f, 0.18f, 0.07f), new Vector3(0.032f, 0.50f, 0.026f), _violet, Vector3.zero);
            Part("CrownSignalR", _crownR, new Vector3(0f, 0.18f, 0.07f), new Vector3(0.032f, 0.50f, 0.026f), _violet, Vector3.zero);

            _weaponPivot = Node("Malatract_WeaponPivot", _rightArm != null ? _rightArm : _root, new Vector3(0f, -0.62f, 0.14f));
            Part("WeaponEmitter", _weaponPivot, Vector3.zero, new Vector3(0.20f, 0.42f, 0.22f), _metal, Vector3.zero);
            _weaponCore = Part("OrderedRuinBlade", _weaponPivot, new Vector3(0f, -0.12f, 1.05f), new Vector3(0.12f, 0.12f, 2.10f), _hot, new Vector3(0f, 0f, 0f));
            Part("OrderedRuinSheath", _weaponPivot, new Vector3(0f, -0.12f, 1.05f), new Vector3(0.19f, 0.19f, 2.16f), _violet, Vector3.zero);

            _mantleCables = new Transform[6];
            for (int i = 0; i < _mantleCables.Length; i++)
            {
                float x = (i - 2.5f) * 0.20f;
                _mantleCables[i] = Part($"MantleCable_{i}", _root, new Vector3(x, 1.55f, -0.48f), new Vector3(0.07f, 1.55f + (i % 2) * 0.22f, 0.07f), i % 2 == 0 ? _black : _metal, new Vector3(8f + i * 2f, 0f, (i - 2.5f) * 4f));
            }

            GameObject lightGo = new GameObject("MalatractVisorLight");
            lightGo.transform.SetParent(_head, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.04f, 0.50f);
            _visorLight = lightGo.AddComponent<Light>();
            _visorLight.type = LightType.Point;
            _visorLight.shadows = LightShadows.None;
            _visorLight.range = 5.2f;
            _visorLight.intensity = 1.8f;
        }

        private void BuildLeg(float side, string suffix)
        {
            Transform hip = Node("Hip_" + suffix, _root, new Vector3(0.32f * side, 0.72f, 0f));
            Part("Thigh_" + suffix, hip, new Vector3(0f, -0.34f, 0f), new Vector3(0.32f, 0.72f, 0.40f), _black, new Vector3(0f, 0f, side * -2f));
            Part("Shin_" + suffix, hip, new Vector3(0f, -0.98f, 0.04f), new Vector3(0.28f, 0.72f, 0.32f), _metal, new Vector3(0f, 0f, side * 2f));
            Part("Foot_" + suffix, hip, new Vector3(0f, -1.35f, 0.15f), new Vector3(0.34f, 0.20f, 0.62f), _black, Vector3.zero);
        }

        private void BuildArm(float side, string suffix, out Transform arm)
        {
            arm = Node("ArmRig_" + suffix, _root, new Vector3(0.76f * side, 2.20f, 0f));
            Part("Pauldron_" + suffix, arm, Vector3.zero, new Vector3(0.52f, 0.42f, 0.66f), _metal, new Vector3(0f, 0f, side * 7f));
            Part("Forearm_" + suffix, arm, new Vector3(0.08f * side, -0.58f, 0.04f), new Vector3(0.28f, 0.86f, 0.32f), _black, new Vector3(0f, 0f, side * -4f));
            Part("Hand_" + suffix, arm, new Vector3(0.10f * side, -1.04f, 0.10f), new Vector3(0.28f, 0.28f, 0.30f), _metal, Vector3.zero);
        }

        private void AnimateArms(float time)
        {
            float charge = Mathf.Max(_charge, _meleeCharge);
            if (_leftArm != null)
            {
                float fanPose = _charge > 0.05f ? -22f * _charge : 0f;
                _leftArm.localRotation = Quaternion.Euler(fanPose, -8f * _charge, -6f);
            }
            if (_rightArm != null)
            {
                float melee = _meleePattern == "SLAM" ? -58f : _meleePattern == "CLEAVE" ? -34f : -8f;
                _rightArm.localRotation = Quaternion.Euler(melee * _meleeCharge, 10f * charge, 5f);
            }
        }

        private void AnimateWeapon(float time, float phase01)
        {
            if (_weaponPivot == null || _weaponCore == null) return;
            float active = Mathf.Max(_fire, _meleeCharge);
            float pulse = 1f + Mathf.Sin(time * 4.8f) * 0.025f + active * 0.18f + phase01 * 0.10f;
            _weaponCore.localScale = new Vector3(0.12f, 0.12f, 2.10f * pulse);
            float sweep = _meleePattern == "CLEAVE" ? _meleeCharge * 54f : _meleePattern == "SLAM" ? _meleeCharge * -18f : 0f;
            _weaponPivot.localRotation = Quaternion.Euler(-12f - _meleeCharge * 18f, sweep, -4f);
        }

        private void AnimateCrown(float phase01)
        {
            if (_crownL != null) _crownL.localRotation = Quaternion.Euler(-16f - phase01 * 9f, 0f, -21f - phase01 * 8f);
            if (_crownR != null) _crownR.localRotation = Quaternion.Euler(-16f - phase01 * 9f, 0f, 21f + phase01 * 8f);
        }

        private void AnimateMantle(float time, float phase01)
        {
            if (_mantleCables == null) return;
            for (int i = 0; i < _mantleCables.Length; i++)
            {
                Transform cable = _mantleCables[i];
                if (cable == null) continue;
                float sway = Mathf.Sin(time * (0.72f + i * 0.04f) + i * 0.71f) * (2.2f + phase01 * 1.4f);
                cable.localRotation = Quaternion.Euler(8f + i * 2f, sway * 0.25f, (i - 2.5f) * 4f + sway);
            }
        }

        private void OnPhaseChanged(int phase)
        {
            _phase = Mathf.Clamp(phase, 1, 3);
            _fire = 1f;
        }

        private void OnAttackTelegraphed(string pattern, int count, bool heavy)
        {
            _charge = heavy ? 1f : 0.72f;
        }

        private void OnAttackFired(string pattern, int count, bool heavy)
        {
            _fire = heavy ? 1f : 0.72f;
        }

        private void OnMeleeTelegraphed(string pattern, Vector3 direction, float range, float arc, bool heavy)
        {
            _meleePattern = pattern ?? string.Empty;
            _meleeCharge = heavy ? 1f : 0.78f;
        }

        private void OnMeleeResolved(string pattern, string outcome, float damage)
        {
            _meleePattern = pattern ?? _meleePattern;
            _fire = 0.9f;
        }

        private void OnDamaged(DamagePacket packet)
        {
            if (packet.Damage > 0f) _damageFlash = 1f;
        }

        private static Transform Node(string name, Transform parent, Vector3 position)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            return go.transform;
        }

        private static Transform Part(string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 euler)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
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
