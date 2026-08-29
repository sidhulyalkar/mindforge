using UnityEngine;
using Mindforge.Combat;
using Mindforge.Traversal;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Production-detail layer for the Guardian's Prism Squire silhouette. V1 keeps the
    /// broad readable block shape; V2 adds layered armor, a small aether reactor, a segmented
    /// half-cape and motion accents. Everything here is collider-free presentation.
    /// </summary>
    [DefaultExecutionOrder(1110)]
    public sealed class PrismSquirePresentationV2 : MonoBehaviour
    {
        public const string RootName = "PrismSquireOverlayV2";

        [SerializeField] private GuardianMotor motor;
        [SerializeField] private GuardianSwordShieldController combat;
        [SerializeField] private GuardianHoverbikeController bike;
        [SerializeField] private FluxMeter flux;

        private Transform _avatar;
        private Transform _root;
        private Transform _capeRoot;
        private Transform _reactorRing;
        private Transform _crestRoot;
        private Transform _leftShoulder;
        private Transform _rightShoulder;
        private bool _built;

        private Material _pearl;
        private Material _cyan;
        private Material _violet;
        private Material _gold;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Awake() => Resolve();
        private void Start() => TryBuild();

        private void OnDestroy()
        {
            if (_pearl != null) Destroy(_pearl);
            if (_cyan != null) Destroy(_cyan);
            if (_violet != null) Destroy(_violet);
            if (_gold != null) Destroy(_gold);
        }

        private void LateUpdate()
        {
            Resolve();
            if (!_built) TryBuild();
            if (!_built) return;

            float dt = Time.unscaledDeltaTime;
            float time = Time.unscaledTime;
            float speed = motor != null ? Vector3.ProjectOnPlane(motor.Velocity, Vector3.up).magnitude : 0f;
            float speed01 = Mathf.Clamp01(speed / 13.5f);
            bool airborne = motor != null && !motor.IsGrounded;
            bool dashing = motor != null && motor.IsDashing;
            bool mounted = bike != null && bike.Mounted;
            bool attacking = combat != null && combat.IsAttacking;
            float flux01 = flux != null ? Mathf.Clamp01(flux.Value / Mathf.Max(0.001f, flux.Max)) : 0f;

            if (_capeRoot != null)
            {
                float attackKick = attacking ? Mathf.Sin(Mathf.Clamp01(combat.AttackProgress) * Mathf.PI) * 16f : 0f;
                float lift = mounted ? 34f : airborne ? 28f : Mathf.Lerp(8f, 24f, speed01);
                float flutter = Mathf.Sin(time * (4.2f + speed01 * 3.0f)) * (2f + speed01 * 5f);
                Quaternion target = Quaternion.Euler(lift + flutter, -4f, attackKick * 0.25f);
                _capeRoot.localRotation = Quaternion.Slerp(
                    _capeRoot.localRotation,
                    target,
                    1f - Mathf.Exp(-9f * dt));
            }

            if (_reactorRing != null)
            {
                float spin = 24f + speed01 * 48f + flux01 * 58f + (dashing ? 72f : 0f);
                _reactorRing.localRotation *= Quaternion.Euler(0f, 0f, spin * dt);
                float pulse = 1f + flux01 * 0.10f + (dashing ? 0.10f : 0f);
                _reactorRing.localScale = Vector3.Lerp(
                    _reactorRing.localScale,
                    Vector3.one * pulse,
                    1f - Mathf.Exp(-12f * dt));
            }

            if (_crestRoot != null)
            {
                float bob = Mathf.Sin(time * 3.2f) * 1.5f + (attacking ? 5f : 0f);
                _crestRoot.localRotation = Quaternion.Euler(0f, bob, -bob * 0.35f);
            }

            float shoulderKick = attacking ? Mathf.Sin(Mathf.Clamp01(combat.AttackProgress) * Mathf.PI) * 6f : 0f;
            if (_leftShoulder != null)
                _leftShoulder.localRotation = Quaternion.Euler(0f, 0f, -8f - shoulderKick * 0.35f);
            if (_rightShoulder != null)
                _rightShoulder.localRotation = Quaternion.Euler(0f, 0f, 8f + shoulderKick);
        }

        private void Resolve()
        {
            if (motor == null) motor = GetComponent<GuardianMotor>();
            if (combat == null) combat = GetComponent<GuardianSwordShieldController>();
            if (bike == null) bike = GetComponent<GuardianHoverbikeController>();
            if (flux == null) flux = GetComponent<FluxMeter>();
        }

        private void TryBuild()
        {
            if (_built) return;
            _avatar = transform.Find("GuardianShowcaseAvatar");
            if (_avatar == null) return;

            Transform existing = _avatar.Find(RootName);
            if (existing != null)
            {
                _root = existing;
                _capeRoot = _root.Find("AetherHalfCape");
                _reactorRing = _root.Find("BackReactor/ReactorRing");
                _crestRoot = _root.Find("HeroCrest");
                _leftShoulder = _root.Find("ShoulderFins/Left");
                _rightShoulder = _root.Find("ShoulderFins/Right");
                _built = true;
                return;
            }

            _pearl = CreateMaterial("PrismSquireV2_Pearl", new Color(0.82f, 0.90f, 0.98f), 0.58f, 0.82f, new Color(0.05f, 0.10f, 0.18f));
            _cyan = CreateMaterial("PrismSquireV2_Cyan", new Color(0.10f, 0.88f, 1f), 0.18f, 0.88f, new Color(0.08f, 0.82f, 1f) * 2.0f);
            _violet = CreateMaterial("PrismSquireV2_Violet", new Color(0.56f, 0.18f, 1f), 0.20f, 0.84f, new Color(0.48f, 0.10f, 1f) * 1.8f);
            _gold = CreateMaterial("PrismSquireV2_Gold", new Color(1f, 0.66f, 0.14f), 0.66f, 0.84f, new Color(1f, 0.38f, 0.04f) * 1.1f);

            _root = Node(RootName, _avatar, Vector3.zero);

            Transform torso = FindRigNode("Torso");
            Transform head = FindRigNode("Head");
            Transform leftLeg = FindRigNode("LeftLeg");
            Transform rightLeg = FindRigNode("RightLeg");

            if (torso != null)
            {
                Part("LayeredBreastplate", torso, new Vector3(0f, 0.03f, 0.31f), new Vector3(0.70f, 0.50f, 0.10f), _pearl, Vector3.zero);
                Part("ChestPrism", torso, new Vector3(0f, 0.06f, 0.39f), new Vector3(0.22f, 0.22f, 0.06f), _cyan, new Vector3(0f, 0f, 45f));
                Part("WaistGuard", torso, new Vector3(0f, -0.38f, 0.06f), new Vector3(0.74f, 0.16f, 0.42f), _pearl, Vector3.zero);
            }

            Transform shoulders = Node("ShoulderFins", _root, Vector3.zero);
            _leftShoulder = Node("Left", shoulders, new Vector3(-0.48f, 0.68f, 0f));
            _rightShoulder = Node("Right", shoulders, new Vector3(0.48f, 0.68f, 0f));
            Part("FinL_A", _leftShoulder, new Vector3(-0.18f, 0.06f, -0.03f), new Vector3(0.16f, 0.52f, 0.38f), _violet, new Vector3(0f, -8f, -26f));
            Part("FinL_B", _leftShoulder, new Vector3(-0.30f, -0.02f, -0.18f), new Vector3(0.12f, 0.38f, 0.30f), _cyan, new Vector3(0f, 8f, -34f));
            Part("FinR_A", _rightShoulder, new Vector3(0.18f, 0.06f, -0.03f), new Vector3(0.16f, 0.52f, 0.38f), _cyan, new Vector3(0f, 8f, 26f));
            Part("FinR_B", _rightShoulder, new Vector3(0.30f, -0.02f, -0.18f), new Vector3(0.12f, 0.38f, 0.30f), _gold, new Vector3(0f, -8f, 34f));

            Transform reactor = Node("BackReactor", _root, new Vector3(0f, 0.47f, -0.37f));
            Part("ReactorCore", reactor, Vector3.zero, new Vector3(0.26f, 0.26f, 0.16f), _cyan, Vector3.zero, PrimitiveType.Sphere);
            _reactorRing = Node("ReactorRing", reactor, Vector3.zero);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Part("ReactorNode_" + i, _reactorRing, new Vector3(Mathf.Cos(a) * 0.42f, Mathf.Sin(a) * 0.42f, -0.02f), new Vector3(0.08f, 0.08f, 0.08f), i % 2 == 0 ? _violet : _gold, Vector3.zero, PrimitiveType.Sphere);
            }

            _capeRoot = Node("AetherHalfCape", _root, new Vector3(-0.24f, 0.55f, -0.42f));
            for (int i = 0; i < 4; i++)
                Part("CapeSegment_" + i, _capeRoot, new Vector3(-0.08f * i, -0.24f - i * 0.24f, -0.04f - i * 0.07f), new Vector3(0.34f - i * 0.035f, 0.30f, 0.045f), i % 2 == 0 ? _violet : _cyan, new Vector3(8f + i * 5f, 0f, -7f));

            if (head != null)
            {
                _crestRoot = Node("HeroCrest", _root, Vector3.zero);
                Part("CrestBlade", head, new Vector3(0f, 0.50f, -0.04f), new Vector3(0.10f, 0.38f, 0.28f), _gold, new Vector3(-8f, 0f, 0f));
                Part("VisorBrow", head, new Vector3(0f, 0.15f, 0.35f), new Vector3(0.56f, 0.08f, 0.04f), _cyan, Vector3.zero);
                Part("CheekL", head, new Vector3(-0.28f, -0.07f, 0.25f), new Vector3(0.12f, 0.26f, 0.12f), _pearl, new Vector3(0f, 0f, -18f));
                Part("CheekR", head, new Vector3(0.28f, -0.07f, 0.25f), new Vector3(0.12f, 0.26f, 0.12f), _pearl, new Vector3(0f, 0f, 18f));
            }

            AddKnee(leftLeg, "L", _violet);
            AddKnee(rightLeg, "R", _cyan);
            _built = true;
        }

        private void AddKnee(Transform leg, string suffix, Material accent)
        {
            if (leg == null) return;
            Part("KneePlate_" + suffix, leg, new Vector3(0f, -0.18f, 0.24f), new Vector3(0.30f, 0.20f, 0.12f), _pearl, new Vector3(-10f, 0f, 0f));
            Part("KneeSignal_" + suffix, leg, new Vector3(0f, -0.18f, 0.31f), new Vector3(0.12f, 0.06f, 0.035f), accent, Vector3.zero);
        }

        private Transform FindRigNode(string name)
        {
            Transform direct = _avatar.Find(name);
            if (direct != null) return direct;
            Transform bodyMotion = _avatar.Find("Motion_Body");
            if (bodyMotion != null)
            {
                Transform wrapper = bodyMotion.Find("Motion_" + name);
                if (wrapper != null)
                {
                    Transform child = wrapper.Find(name);
                    if (child != null) return child;
                }
            }
            Transform legacy = _avatar.Find("Motion_" + name);
            return legacy != null ? legacy.Find(name) : null;
        }

        private static Transform Node(string name, Transform parent, Vector3 localPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static Transform Part(string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 euler, PrimitiveType primitive = PrimitiveType.Cube)
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
