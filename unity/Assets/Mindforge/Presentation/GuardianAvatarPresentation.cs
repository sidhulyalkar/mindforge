using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Replaceable procedural character presentation for the showcase slice. The
    /// authoritative Guardian collider/rigidbody remain untouched; this component
    /// observes motor/combat state and animates collider-free visual geometry only.
    /// </summary>
    public sealed class GuardianAvatarPresentation : MonoBehaviour
    {
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private GuardianCombatInput input;
        [SerializeField] private GuardianSwordShieldController physicalCombat;
        [SerializeField] private CombatantVitals vitals;

        private Transform _visualRoot;
        private Transform _torso;
        private Transform _head;
        private Transform _leftArm;
        private Transform _rightArm;
        private Transform _leftLeg;
        private Transform _rightLeg;
        private Transform _mantle;
        private Renderer[] _renderers;
        private Material _armorMaterial;
        private Material _clothMaterial;
        private Material _accentMaterial;
        private MaterialPropertyBlock _block;
        private float _stride;
        private float _damageFlash;
        private Quaternion _facing = Quaternion.identity;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            Resolve();
            BuildVisuals();
            if (vitals != null) vitals.Damaged += OnDamaged;
        }

        private void OnDestroy()
        {
            if (vitals != null) vitals.Damaged -= OnDamaged;
            if (_armorMaterial != null) Destroy(_armorMaterial);
            if (_clothMaterial != null) Destroy(_clothMaterial);
            if (_accentMaterial != null) Destroy(_accentMaterial);
        }

        private void Resolve()
        {
            if (motor == null) motor = GetComponent<GuardianMotor>();
            if (input == null) input = GetComponent<GuardianCombatInput>();
            if (physicalCombat == null) physicalCombat = GetComponent<GuardianSwordShieldController>();
            if (vitals == null) vitals = GetComponent<CombatantVitals>();
        }

        private void BuildVisuals()
        {
            if (transform.Find("GuardianShowcaseAvatar") != null) return;

            Renderer legacy = GetComponent<Renderer>();
            if (legacy != null) legacy.enabled = false;

            _armorMaterial = CreateMaterial("GuardianArmor", new Color(0.12f, 0.16f, 0.26f), 0.92f, 0.72f, new Color(0.08f, 0.16f, 0.34f));
            _clothMaterial = CreateMaterial("GuardianCloth", new Color(0.045f, 0.065f, 0.13f), 0.10f, 0.42f, new Color(0.02f, 0.05f, 0.13f));
            _accentMaterial = CreateMaterial("GuardianAether", new Color(0.22f, 0.48f, 0.95f), 0.62f, 0.82f, new Color(0.12f, 0.54f, 1f) * 2.0f);
            _block = new MaterialPropertyBlock();

            _visualRoot = NewNode("GuardianShowcaseAvatar", transform, new Vector3(0f, 0.02f, 0f));

            Part("Pelvis", PrimitiveType.Cube, _visualRoot, new Vector3(0f, -0.08f, 0f), new Vector3(0.56f, 0.30f, 0.36f), _armorMaterial);
            _torso = Part("Torso", PrimitiveType.Cube, _visualRoot, new Vector3(0f, 0.31f, 0f), new Vector3(0.70f, 0.66f, 0.40f), _armorMaterial);
            Part("ChestAether", PrimitiveType.Cube, _torso, new Vector3(0f, 0.08f, 0.215f), new Vector3(0.34f, 0.10f, 0.035f), _accentMaterial);

            Part("Neck", PrimitiveType.Cylinder, _visualRoot, new Vector3(0f, 0.72f, 0f), new Vector3(0.14f, 0.12f, 0.14f), _clothMaterial);
            _head = Part("Head", PrimitiveType.Sphere, _visualRoot, new Vector3(0f, 0.94f, 0f), new Vector3(0.38f, 0.42f, 0.38f), _armorMaterial);
            Part("Visor", PrimitiveType.Cube, _head, new Vector3(0f, 0.02f, 0.205f), new Vector3(0.29f, 0.065f, 0.035f), _accentMaterial);
            Part("CrownFin", PrimitiveType.Cube, _head, new Vector3(0f, 0.24f, -0.02f), new Vector3(0.07f, 0.22f, 0.20f), _armorMaterial);

            _leftArm = Limb("LeftArm", _visualRoot, new Vector3(-0.47f, 0.38f, 0f), _armorMaterial);
            _rightArm = Limb("RightArm", _visualRoot, new Vector3(0.47f, 0.38f, 0f), _armorMaterial);
            _leftLeg = Leg("LeftLeg", _visualRoot, new Vector3(-0.21f, -0.43f, 0f), _armorMaterial);
            _rightLeg = Leg("RightLeg", _visualRoot, new Vector3(0.21f, -0.43f, 0f), _armorMaterial);

            _mantle = Part("Mantle", PrimitiveType.Cube, _visualRoot, new Vector3(0f, 0.31f, -0.29f), new Vector3(0.82f, 0.72f, 0.055f), _clothMaterial);
            _mantle.localRotation = Quaternion.Euler(8f, 0f, 0f);
            Part("MantleMark", PrimitiveType.Cube, _mantle, new Vector3(0f, 0.04f, -0.035f), new Vector3(0.18f, 0.34f, 0.025f), _accentMaterial);

            Part("LeftPauldron", PrimitiveType.Sphere, _visualRoot, new Vector3(-0.48f, 0.60f, 0f), new Vector3(0.30f, 0.20f, 0.34f), _armorMaterial);
            Part("RightPauldron", PrimitiveType.Sphere, _visualRoot, new Vector3(0.48f, 0.60f, 0f), new Vector3(0.30f, 0.20f, 0.34f), _armorMaterial);

            _renderers = _visualRoot.GetComponentsInChildren<Renderer>(true);
            _facing = transform.rotation;
        }

        private Transform Limb(string name, Transform parent, Vector3 position, Material material)
        {
            Transform root = NewNode(name, parent, position);
            Part("Upper", PrimitiveType.Capsule, root, new Vector3(0f, -0.13f, 0f), new Vector3(0.20f, 0.38f, 0.20f), material);
            Part("Gauntlet", PrimitiveType.Sphere, root, new Vector3(0f, -0.48f, 0.04f), new Vector3(0.19f, 0.19f, 0.19f), _accentMaterial);
            return root;
        }

        private Transform Leg(string name, Transform parent, Vector3 position, Material material)
        {
            Transform root = NewNode(name, parent, position);
            Part("Greave", PrimitiveType.Capsule, root, new Vector3(0f, -0.18f, 0f), new Vector3(0.22f, 0.44f, 0.22f), material);
            Part("Boot", PrimitiveType.Cube, root, new Vector3(0f, -0.55f, 0.10f), new Vector3(0.24f, 0.16f, 0.38f), _clothMaterial);
            return root;
        }

        private void LateUpdate()
        {
            Resolve();
            if (_visualRoot == null) return;

            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            Vector3 aim = input != null ? input.CurrentAimDirection : transform.forward;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;
            if (aim.sqrMagnitude < 0.01f) aim = Vector3.forward;
            Quaternion desired = Quaternion.LookRotation(aim.normalized, Vector3.up);
            _facing = Quaternion.Slerp(_facing, desired, 1f - Mathf.Exp(-15f * dt));
            _visualRoot.rotation = _facing;

            float speed = motor != null ? Vector3.ProjectOnPlane(motor.Velocity, Vector3.up).magnitude : 0f;
            float move01 = Mathf.Clamp01(speed / 6.0f);
            _stride += dt * Mathf.Lerp(2.2f, 9.5f, move01);
            float step = Mathf.Sin(_stride) * 28f * move01;

            bool guarding = physicalCombat != null && physicalCombat.IsGuarding;
            bool attacking = physicalCombat != null && physicalCombat.IsAttacking;
            bool dashing = motor != null && motor.IsDashing;
            int combo = physicalCombat != null ? physicalCombat.ComboStep : 1;

            if (_leftLeg != null) _leftLeg.localRotation = Quaternion.Euler(step, 0f, 0f);
            if (_rightLeg != null) _rightLeg.localRotation = Quaternion.Euler(-step, 0f, 0f);

            float armSwing = step * 0.65f;
            Quaternion leftArm = Quaternion.Euler(-armSwing, 0f, guarding ? -48f : -8f);
            Quaternion rightArm = Quaternion.Euler(armSwing, 0f, attacking ? 48f : 8f);
            if (guarding) leftArm = Quaternion.Euler(-72f, -10f, -46f);
            if (attacking)
            {
                float side = combo == 2 ? -1f : 1f;
                rightArm = Quaternion.Euler(combo >= 3 ? -66f : -48f, 24f * side, (combo >= 3 ? 78f : 62f) * side);
            }
            if (_leftArm != null) _leftArm.localRotation = Quaternion.Slerp(_leftArm.localRotation, leftArm, 1f - Mathf.Exp(-18f * dt));
            if (_rightArm != null) _rightArm.localRotation = Quaternion.Slerp(_rightArm.localRotation, rightArm, 1f - Mathf.Exp(-18f * dt));

            float lean = dashing ? 17f : attacking ? (combo >= 3 ? 11f : 7f) : guarding ? -4f : Mathf.Sin(_stride * 2f) * 1.2f * move01;
            if (_torso != null) _torso.localRotation = Quaternion.Slerp(_torso.localRotation, Quaternion.Euler(lean, 0f, 0f), 1f - Mathf.Exp(-12f * dt));
            if (_mantle != null)
            {
                float flutter = Mathf.Sin(Time.unscaledTime * 7f) * (2f + speed * 0.8f);
                _mantle.localRotation = Quaternion.Euler(8f + Mathf.Clamp(speed * 2.2f, 0f, 18f), 0f, flutter);
            }

            float bob = (Mathf.Abs(Mathf.Sin(_stride * 2f)) * 0.035f * move01) - (dashing ? 0.10f : 0f);
            _visualRoot.localPosition = new Vector3(0f, 0.02f + bob, 0f);

            _damageFlash = Mathf.MoveTowards(_damageFlash, 0f, dt * 5.5f);
            ApplyDamageFlash();
        }

        private void OnDamaged(DamagePacket packet)
        {
            if (packet.Damage > 0f) _damageFlash = 1f;
        }

        private void ApplyDamageFlash()
        {
            if (_renderers == null || _block == null) return;
            bool flashing = _damageFlash > 0.001f;
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer renderer = _renderers[i];
                if (renderer == null) continue;
                if (!flashing)
                {
                    // These renderers are owned only by this visual rig. Clearing the
                    // block restores each material's authored blue/emissive identity.
                    renderer.SetPropertyBlock(null);
                    continue;
                }

                renderer.GetPropertyBlock(_block);
                _block.SetColor(EmissionColor, new Color(1f, 0.22f, 0.12f) * (_damageFlash * 2.2f));
                renderer.SetPropertyBlock(_block);
            }
        }

        private static Transform NewNode(string name, Transform parent, Vector3 localPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static Transform Part(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
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
