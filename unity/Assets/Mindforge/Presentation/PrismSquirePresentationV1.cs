using UnityEngine;
using Mindforge.Combat;
using Mindforge.Traversal;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Presentation-only Prism Squire identity layered over the procedural Guardian rig.
    /// Oversized block armor and independent helmet wobble create the comic contrast while
    /// existing locomotion/combat components remain the sole gameplay authority.
    /// </summary>
    [DefaultExecutionOrder(1100)]
    public sealed class PrismSquirePresentationV1 : MonoBehaviour
    {
        public const string RootName = "PrismSquireOverlayV1";

        [SerializeField] private GuardianMotor motor;
        [SerializeField] private GuardianSwordShieldController combat;
        [SerializeField] private GuardianHoverbikeController bike;

        private Transform _avatar;
        private Transform _root;
        private Transform _helmetWobble;
        private Transform _bodyMotion;
        private Transform _leftLegMotion;
        private Transform _rightLegMotion;
        private Transform _leftArmMotion;
        private bool _built;

        private Material _white;
        private Material _cyan;
        private Material _rose;
        private Material _gold;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            Resolve();
        }

        private void Start()
        {
            TryBuild();
        }

        private void OnDestroy()
        {
            if (_white != null) Destroy(_white);
            if (_cyan != null) Destroy(_cyan);
            if (_rose != null) Destroy(_rose);
            if (_gold != null) Destroy(_gold);
        }

        private void LateUpdate()
        {
            Resolve();
            if (!_built) TryBuild();
            if (!_built) return;

            float time = Time.unscaledTime;
            float speed = motor != null ? Vector3.ProjectOnPlane(motor.Velocity, Vector3.up).magnitude : 0f;
            float speed01 = Mathf.Clamp01(speed / 12f);
            bool mounted = bike != null && bike.Mounted;
            bool attacking = combat != null && combat.IsAttacking;

            if (_helmetWobble != null)
            {
                float wobbleHz = mounted ? 3.2f : Mathf.Lerp(2.2f, 6.8f, speed01);
                float wobble = Mathf.Sin(time * wobbleHz * Mathf.PI * 2f);
                float side = Mathf.Sin(time * wobbleHz * 0.73f * Mathf.PI * 2f + 0.8f);
                float attackKick = attacking ? Mathf.Sin(Mathf.Clamp01(combat.AttackProgress) * Mathf.PI) * 8f : 0f;
                _helmetWobble.localRotation = Quaternion.Euler(
                    mounted ? -7f + wobble * 2.4f : wobble * (2.5f + speed01 * 5.5f),
                    side * (mounted ? 2f : 4f),
                    side * (mounted ? 3.5f : 7f) + attackKick);
            }

            // GuardianMotionPolish owns ordinary gait. This late presentation layer only
            // overrides the wrappers while mounted so the rider does not run in place.
            if (mounted)
            {
                if (_bodyMotion != null)
                    _bodyMotion.localPosition = Vector3.Lerp(
                        _bodyMotion.localPosition,
                        new Vector3(0f, -0.18f, -0.05f),
                        1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
                if (_leftLegMotion != null)
                    _leftLegMotion.localRotation = Quaternion.Euler(56f, -10f, -8f);
                if (_rightLegMotion != null)
                    _rightLegMotion.localRotation = Quaternion.Euler(56f, 10f, 8f);
                if (_leftArmMotion != null && !attacking)
                    _leftArmMotion.localRotation = Quaternion.Euler(-42f, -8f, -24f);
            }
        }

        private void Resolve()
        {
            if (motor == null) motor = GetComponent<GuardianMotor>();
            if (combat == null) combat = GetComponent<GuardianSwordShieldController>();
            if (bike == null) bike = GetComponent<GuardianHoverbikeController>();
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
                BindMotionNodes();
                _built = true;
                return;
            }

            _white = CreateMaterial("PrismSquire_Pearl", new Color(0.88f, 0.93f, 1f), 0.52f, 0.78f, new Color(0.10f, 0.22f, 0.36f));
            _cyan = CreateMaterial("PrismSquire_Cyan", new Color(0.18f, 0.92f, 1f), 0.22f, 0.88f, new Color(0.10f, 0.85f, 1f) * 2.2f);
            _rose = CreateMaterial("PrismSquire_Rose", new Color(1f, 0.20f, 0.64f), 0.18f, 0.84f, new Color(1f, 0.08f, 0.50f) * 1.8f);
            _gold = CreateMaterial("PrismSquire_Gold", new Color(1f, 0.72f, 0.18f), 0.64f, 0.82f, new Color(1f, 0.44f, 0.06f) * 1.4f);

            _root = NewNode(RootName, _avatar, Vector3.zero);

            Transform torso = FindRigNode("Torso");
            Transform head = FindRigNode("Head");
            Transform leftArm = FindRigNode("LeftArm");
            Transform rightArm = FindRigNode("RightArm");
            Transform leftLeg = FindRigNode("LeftLeg");
            Transform rightLeg = FindRigNode("RightLeg");

            if (torso != null)
            {
                Part("PrismChest", torso, new Vector3(0f, 0.01f, 0.015f), new Vector3(0.82f, 0.72f, 0.49f), _white);
                Part("PrismChestBand", torso, new Vector3(0f, 0.08f, 0.267f), new Vector3(0.58f, 0.12f, 0.035f), _cyan);
                Part("PrismBuckle", torso, new Vector3(0f, -0.26f, 0.27f), new Vector3(0.18f, 0.18f, 0.04f), _gold);
            }

            if (head != null)
            {
                _helmetWobble = NewNode("PrismHelmetWobble", head, Vector3.zero);
                Part("OversizedHelmet", _helmetWobble, new Vector3(0f, 0.04f, 0f), new Vector3(0.66f, 0.64f, 0.62f), _white);
                Part("HelmetVisor", _helmetWobble, new Vector3(0f, 0.03f, 0.325f), new Vector3(0.47f, 0.105f, 0.035f), _cyan);
                Part("HelmetCrest", _helmetWobble, new Vector3(0f, 0.40f, -0.02f), new Vector3(0.12f, 0.30f, 0.30f), _rose);
                Part("HelmetPrism", _helmetWobble, new Vector3(0.22f, 0.32f, 0.01f), new Vector3(0.10f, 0.18f, 0.12f), _gold);
            }

            AddLimbArmor(leftArm, "L", -1f);
            AddLimbArmor(rightArm, "R", 1f);
            AddBootArmor(leftLeg, "L", _rose);
            AddBootArmor(rightLeg, "R", _cyan);

            // A floating guild pennant keeps the block silhouette playful without changing
            // the authoritative body or adding collision.
            Part("GuildPennant", _root, new Vector3(-0.52f, 0.42f, -0.36f), new Vector3(0.10f, 0.54f, 0.08f), _rose);
            Part("GuildPennantTip", _root, new Vector3(-0.52f, 0.12f, -0.36f), new Vector3(0.22f, 0.12f, 0.08f), _gold);

            BindMotionNodes();
            _built = true;
        }

        private void AddLimbArmor(Transform limb, string suffix, float side)
        {
            if (limb == null) return;
            Part("BlockPauldron_" + suffix, limb, new Vector3(side * 0.015f, 0.03f, 0f), new Vector3(0.34f, 0.26f, 0.38f), side < 0f ? _rose : _cyan);
            Part("BlockGauntlet_" + suffix, limb, new Vector3(0f, -0.46f, 0.04f), new Vector3(0.24f, 0.22f, 0.24f), _white);
        }

        private void AddBootArmor(Transform leg, string suffix, Material accent)
        {
            if (leg == null) return;
            Part("BlockGreave_" + suffix, leg, new Vector3(0f, -0.18f, 0f), new Vector3(0.28f, 0.48f, 0.28f), _white);
            Part("BootGlow_" + suffix, leg, new Vector3(0f, -0.55f, 0.21f), new Vector3(0.25f, 0.08f, 0.16f), accent);
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
                    Transform wrappedChild = wrapper.Find(name);
                    if (wrappedChild != null) return wrappedChild;
                }
            }

            Transform legacyMotion = _avatar.Find("Motion_" + name);
            return legacyMotion != null ? legacyMotion.Find(name) : null;
        }

        private void BindMotionNodes()
        {
            if (_avatar == null) return;
            _bodyMotion = _avatar.Find("Motion_Body");
            if (_bodyMotion == null) return;
            _leftLegMotion = _bodyMotion.Find("Motion_LeftLeg");
            _rightLegMotion = _bodyMotion.Find("Motion_RightLeg");
            _leftArmMotion = _bodyMotion.Find("Motion_LeftArm");
        }

        private static Transform NewNode(string name, Transform parent, Vector3 localPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static Transform Part(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
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
