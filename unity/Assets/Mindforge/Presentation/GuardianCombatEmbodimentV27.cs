using System.Collections;
using Mindforge.Combat;
using Mindforge.SoulWisp;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Presentation-only upper-body combat rig for the procedural Guardian.
    ///
    /// The authoritative sword sweep remains entirely inside GuardianSwordShieldController.
    /// This component reads that state after simulation and makes the visible shoulder, elbow,
    /// wrist and hilt follow the same combo intent. It never changes attack permission, reach,
    /// damage, parry geometry, locomotion, stamina, target selection or neural evidence.
    /// </summary>
    [DefaultExecutionOrder(840)]
    public sealed class GuardianCombatEmbodimentV27 : MonoBehaviour
    {
        public const string RootName = "GuardianCombatEmbodimentV27";

        [SerializeField] private GuardianCombatInput input;
        [SerializeField] private GuardianSwordShieldController combat;
        [SerializeField] private SoulWispController wisp;

        private Transform _visualRoot;
        private Transform _torso;
        private Transform _chest;
        private Transform _helmet;
        private Transform _leftArm;
        private Transform _swordRoot;
        private Transform _rigRoot;
        private Transform _shoulderArmor;
        private Transform _upperArm;
        private Transform _elbowArmor;
        private Transform _forearm;
        private Transform _hand;
        private Quaternion _torsoBase;
        private Quaternion _chestBase;
        private Quaternion _helmetBase;
        private Quaternion _leftArmBase;
        private Material _armor;
        private Material _underSuit;
        private Material _accent;
        private bool _ready;

        private const float UpperLength = 0.47f;
        private const float ForeLength = 0.46f;
        private const float ArmThickness = 0.34f;

        private static Mesh _upperMesh;
        private static Mesh _foreMesh;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            MindforgeDemoV11Marker marker = FindObjectOfType<MindforgeDemoV11Marker>(true);
            if (marker == null) return;
            GuardianCombatInput guardian = FindObjectOfType<GuardianCombatInput>(true);
            if (guardian == null || guardian.GetComponent<GuardianCombatEmbodimentV27>() != null) return;
            guardian.gameObject.AddComponent<GuardianCombatEmbodimentV27>();
        }

        private IEnumerator Start()
        {
            Resolve();
            for (int frame = 0; frame < 240; frame++)
            {
                Resolve();
                _visualRoot = transform.Find("V11GuardianVisual");
                _swordRoot = transform.Find("PhysicalArsenalRig/SwordRoot");
                if (_visualRoot != null && _swordRoot != null && combat != null) break;
                yield return null;
            }

            if (_visualRoot == null || _swordRoot == null || combat == null)
            {
                Debug.LogWarning("[Mindforge:V27Guardian] Guardian visual or Aetherblade hierarchy unavailable; embodiment skipped.");
                yield break;
            }

            BindBody();
            BuildArm();
            _ready = true;
        }

        private void Resolve()
        {
            if (input == null) input = GetComponent<GuardianCombatInput>();
            if (combat == null) combat = GetComponent<GuardianSwordShieldController>();
            if (wisp == null) wisp = FindObjectOfType<SoulWispController>(true);
        }

        private void BindBody()
        {
            _torso = _visualRoot.Find("Torso");
            _chest = _visualRoot.Find("ChestPlate");
            _helmet = _visualRoot.Find("Helmet");
            _leftArm = _visualRoot.Find("ArmL");
            _torsoBase = _torso != null ? _torso.localRotation : Quaternion.identity;
            _chestBase = _chest != null ? _chest.localRotation : Quaternion.identity;
            _helmetBase = _helmet != null ? _helmet.localRotation : Quaternion.identity;
            _leftArmBase = _leftArm != null ? _leftArm.localRotation : Quaternion.identity;

            HideLegacyPart("ArmR");
            HideLegacyPart("HandR");
        }

        private void HideLegacyPart(string childName)
        {
            Transform part = _visualRoot != null ? _visualRoot.Find(childName) : null;
            if (part == null) return;
            Renderer[] renderers = part.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].enabled = false;
        }

        private void BuildArm()
        {
            Transform existing = _visualRoot.Find(RootName);
            if (existing != null) Destroy(existing.gameObject);

            _armor = CreateMaterial("V27_GuardianArm_Armor", new Color(0.20f, 0.24f, 0.30f), 0.72f, 0.46f, Color.black);
            _underSuit = CreateMaterial("V27_GuardianArm_Undersuit", new Color(0.025f, 0.035f, 0.052f), 0.18f, 0.28f, Color.black);
            _accent = CreateMaterial("V27_GuardianArm_AetherTrim", new Color(0.07f, 0.30f, 0.46f), 0.40f, 0.70f, new Color(0.18f, 0.78f, 1f) * 1.65f);

            _rigRoot = new GameObject(RootName).transform;
            _rigRoot.SetParent(_visualRoot, false);

            _upperMesh = _upperMesh != null ? _upperMesh : BuildTaperedLimbMesh(0.56f, 0.43f, 12);
            _foreMesh = _foreMesh != null ? _foreMesh : BuildTaperedLimbMesh(0.48f, 0.58f, 12);

            _shoulderArmor = CreatePart("RightPauldron", PrimitiveType.Sphere, _rigRoot, _armor);
            _shoulderArmor.localScale = new Vector3(0.42f, 0.30f, 0.38f);
            _upperArm = CreateMeshPart("RightUpperArm", _upperMesh, _rigRoot, _underSuit);
            _elbowArmor = CreatePart("RightElbowGuard", PrimitiveType.Sphere, _rigRoot, _accent);
            _elbowArmor.localScale = new Vector3(0.20f, 0.16f, 0.20f);
            _forearm = CreateMeshPart("RightForearm", _foreMesh, _rigRoot, _armor);
            _hand = CreatePart("RightGauntlet", PrimitiveType.Sphere, _rigRoot, _armor);
            _hand.localScale = new Vector3(0.17f, 0.14f, 0.22f);

            Transform wristBand = CreatePart("AetherWristBand", PrimitiveType.Cylinder, _hand, _accent);
            wristBand.localPosition = new Vector3(0f, 0f, -0.11f);
            wristBand.localRotation = Quaternion.Euler(90f, 0f, 0f);
            wristBand.localScale = new Vector3(0.13f, 0.055f, 0.13f);
        }

        private void LateUpdate()
        {
            if (!_ready || _visualRoot == null || combat == null || _swordRoot == null) return;
            Resolve();

            bool neural = NeuralVisualFieldActive();
            Vector3 aim = input != null ? input.CurrentAimDirection : transform.forward;
            aim = Vector3.ProjectOnPlane(aim, Vector3.up);
            if (aim.sqrMagnitude < 0.001f) aim = transform.forward;
            aim.Normalize();

            bool attacking = !neural && combat.IsAttacking;
            bool guarding = !neural && combat.IsGuarding;
            int combo = Mathf.Clamp(combat.ComboStep, 1, 3);
            float progress = attacking ? Mathf.Clamp01(combat.AttackProgress) : 0f;

            ApplyUpperBodyPose(aim, attacking, guarding, combo, progress, neural);

            Vector3 shoulder = _visualRoot.TransformPoint(new Vector3(0.48f, 1.27f, 0.03f));
            Vector3 desiredWrist = ComputeWristTarget(aim, attacking, guarding, combo, progress, neural);
            SolveArm(shoulder, desiredWrist, aim, attacking, combo, progress);
        }

        private void ApplyUpperBodyPose(Vector3 aim, bool attacking, bool guarding, int combo, float progress, bool neural)
        {
            Vector3 localAim = transform.InverseTransformDirection(aim);
            float yaw = Mathf.Atan2(localAim.x, Mathf.Max(0.001f, localAim.z)) * Mathf.Rad2Deg;
            yaw = Mathf.Clamp(yaw, -28f, 28f);
            float attackWeight = attacking ? Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI) : guarding ? 0.55f : 0.18f;
            if (neural) attackWeight = 0f;

            float torsoYaw = yaw * Mathf.Lerp(0.12f, 0.50f, attackWeight);
            float torsoPitch = attacking && combo >= 3 ? Mathf.Lerp(-5f, 8f, progress) : 0f;
            float recoil = attacking ? Mathf.Sin(progress * Mathf.PI) * (combo >= 3 ? -4.5f : -2.0f) : 0f;

            SetLocalRotation(_torso, _torsoBase * Quaternion.Euler(torsoPitch, torsoYaw, recoil), 22f);
            SetLocalRotation(_chest, _chestBase * Quaternion.Euler(torsoPitch * 0.65f, torsoYaw * 1.10f, recoil * 0.45f), 24f);
            SetLocalRotation(_helmet, _helmetBase * Quaternion.Euler(0f, torsoYaw * 0.72f, 0f), 18f);

            if (_leftArm != null && attacking)
            {
                float counter = combo >= 3 ? -34f : -18f - Mathf.Sin(progress * Mathf.PI) * 18f;
                Quaternion desired = _leftArmBase * Quaternion.Euler(counter, -10f, -18f);
                SetLocalRotation(_leftArm, desired, 24f);
            }
        }

        private Vector3 ComputeWristTarget(Vector3 aim, bool attacking, bool guarding, int combo, float p, bool neural)
        {
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(up, aim).normalized;
            Vector3 basePoint = transform.position;

            if (neural)
                return basePoint + up * 0.72f + right * 0.34f + aim * 0.16f;

            if (guarding)
                return basePoint + up * 1.02f + right * 0.05f + aim * 0.40f;

            bool combatReady = combat.CurrentConventionalTarget != null;
            if (!attacking)
                return basePoint + up * (combatReady ? 0.84f : 0.70f) + right * (combatReady ? 0.38f : 0.34f) + aim * (combatReady ? 0.30f : 0.14f);

            float t = SmoothAttack(p);
            if (combo == 1)
            {
                float angle = Mathf.Lerp(-58f, 68f, t) * Mathf.Deg2Rad;
                Vector3 arc = right * Mathf.Cos(angle) + aim * Mathf.Sin(angle);
                return basePoint + up * (0.84f + Mathf.Sin(t * Mathf.PI) * 0.18f) + arc * 0.70f;
            }
            if (combo == 2)
            {
                float angle = Mathf.Lerp(72f, -70f, t) * Mathf.Deg2Rad;
                Vector3 arc = right * Mathf.Cos(angle) + aim * Mathf.Sin(angle);
                return basePoint + up * (0.88f + Mathf.Sin(t * Mathf.PI) * 0.13f) + arc * 0.72f;
            }

            Vector3 start = basePoint + up * 1.47f + right * 0.34f - aim * 0.18f;
            Vector3 control = basePoint + up * 1.74f - right * 0.02f + aim * 0.18f;
            Vector3 end = basePoint + up * 0.72f - right * 0.16f + aim * 0.74f;
            return Quadratic(start, control, end, t);
        }

        private void SolveArm(Vector3 shoulder, Vector3 desiredWrist, Vector3 aim, bool attacking, int combo, float progress)
        {
            Vector3 toWrist = desiredWrist - shoulder;
            float rawDistance = Mathf.Max(0.001f, toWrist.magnitude);
            Vector3 direction = toWrist / rawDistance;
            float maxReach = UpperLength + ForeLength - 0.025f;
            float distance = Mathf.Clamp(rawDistance, 0.12f, maxReach);
            Vector3 wrist = shoulder + direction * distance;

            float along = (UpperLength * UpperLength - ForeLength * ForeLength + distance * distance) / (2f * distance);
            float height = Mathf.Sqrt(Mathf.Max(0.001f, UpperLength * UpperLength - along * along));
            Vector3 bendSeed = transform.right * 0.92f + transform.forward * 0.30f + Vector3.up * 0.16f;
            if (attacking && combo == 2) bendSeed += transform.forward * 0.24f;
            if (attacking && combo >= 3) bendSeed += Vector3.up * 0.30f;
            Vector3 bend = Vector3.ProjectOnPlane(bendSeed, direction);
            if (bend.sqrMagnitude < 0.001f) bend = Vector3.ProjectOnPlane(transform.right, direction);
            bend.Normalize();
            float flex = attacking ? Mathf.Lerp(0.86f, 1.06f, Mathf.Sin(progress * Mathf.PI)) : 0.94f;
            Vector3 elbow = shoulder + direction * along + bend * height * flex;

            _shoulderArmor.position = shoulder;
            _shoulderArmor.rotation = Quaternion.LookRotation((elbow - shoulder).normalized, Vector3.up);
            SetSegment(_upperArm, shoulder, elbow, ArmThickness);
            _elbowArmor.position = elbow;
            _elbowArmor.rotation = Quaternion.LookRotation((wrist - elbow).normalized, Vector3.up);
            SetSegment(_forearm, elbow, wrist, ArmThickness * 0.92f);
            _hand.position = wrist;
            _hand.rotation = Quaternion.Slerp(_hand.rotation, _swordRoot.rotation, 1f - Mathf.Exp(-32f * Mathf.Min(Time.deltaTime, 0.05f)));

            // Visual hilt translation only. The physical sweep is mathematical and remains owned by
            // GuardianSwordShieldController, so moving the presentation hilt cannot create contact.
            _swordRoot.position = wrist + _swordRoot.forward * 0.015f;
        }

        private static void SetSegment(Transform segment, Vector3 a, Vector3 b, float thickness)
        {
            if (segment == null) return;
            Vector3 delta = b - a;
            float length = Mathf.Max(0.01f, delta.magnitude);
            segment.position = (a + b) * 0.5f;
            segment.rotation = Quaternion.FromToRotation(Vector3.up, delta / length);
            segment.localScale = new Vector3(thickness, length, thickness);
        }

        private static float SmoothAttack(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static Vector3 Quadratic(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }

        private void SetLocalRotation(Transform target, Quaternion desired, float sharpness)
        {
            if (target == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            target.localRotation = Quaternion.Slerp(target.localRotation, desired, 1f - Mathf.Exp(-sharpness * dt));
        }

        private bool NeuralVisualFieldActive()
        {
            if (wisp == null) wisp = FindObjectOfType<SoulWispController>(true);
            return wisp != null && (wisp.CalibrationStimuliActive || wisp.ResonanceWindowActive);
        }

        private static Transform CreateMeshPart(string name, Mesh mesh, Transform parent, Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return go.transform;
        }

        private static Transform CreatePart(string name, PrimitiveType primitive, Transform parent, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.transform.SetParent(parent, false);
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

        private static Mesh BuildTaperedLimbMesh(float bottomRadius, float topRadius, int sides)
        {
            sides = Mathf.Max(6, sides);
            Vector3[] vertices = new Vector3[(sides + 1) * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[sides * 6];
            for (int i = 0; i <= sides; i++)
            {
                float u = i / (float)sides;
                float a = u * Mathf.PI * 2f;
                float c = Mathf.Cos(a);
                float s = Mathf.Sin(a);
                vertices[i] = new Vector3(c * bottomRadius, -0.5f, s * bottomRadius);
                vertices[i + sides + 1] = new Vector3(c * topRadius, 0.5f, s * topRadius);
                uv[i] = new Vector2(u, 0f);
                uv[i + sides + 1] = new Vector2(u, 1f);
                if (i >= sides) continue;
                int t = i * 6;
                int a0 = i;
                int a1 = i + 1;
                int b0 = i + sides + 1;
                int b1 = i + sides + 2;
                triangles[t] = a0;
                triangles[t + 1] = b0;
                triangles[t + 2] = b1;
                triangles[t + 3] = a0;
                triangles[t + 4] = b1;
                triangles[t + 5] = a1;
            }
            Mesh mesh = new Mesh { name = "V27_TaperedGuardianLimb" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            if (_armor != null) Destroy(_armor);
            if (_underSuit != null) Destroy(_underSuit);
            if (_accent != null) Destroy(_accent);
        }
    }
}
