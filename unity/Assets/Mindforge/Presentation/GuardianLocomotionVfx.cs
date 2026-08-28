using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Grounded locomotion particles for the showcase Guardian. Emits only from already
    /// resolved movement/dodge/jump state and never modifies Rigidbody velocity or combat.
    /// </summary>
    public sealed class GuardianLocomotionVfx : MonoBehaviour
    {
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private float footstepSpeedThreshold = 1.45f;
        [SerializeField] private float fullSpeedReference = 11.2f;
        [SerializeField] private float minStepInterval = 0.20f;

        private ParticleSystem _dust;
        private Material _dustMaterial;
        private float _nextStep;
        private float _phase;
        private bool _leftFoot;

        private void Awake()
        {
            if (motor == null) motor = GetComponent<GuardianMotor>();
            BuildDust();
        }

        private void OnEnable()
        {
            if (motor == null) motor = GetComponent<GuardianMotor>();
            if (motor == null) return;
            motor.DashStarted += OnDash;
            motor.Jumped += OnJumped;
            motor.Landed += OnLanded;
        }

        private void OnDisable()
        {
            if (motor == null) return;
            motor.DashStarted -= OnDash;
            motor.Jumped -= OnJumped;
            motor.Landed -= OnLanded;
        }

        private void Update()
        {
            if (motor == null || _dust == null || !motor.IsGrounded) return;
            Vector3 horizontal = Vector3.ProjectOnPlane(motor.Velocity, Vector3.up);
            float speed = horizontal.magnitude;
            if (speed < footstepSpeedThreshold || motor.IsDashing) return;

            float speed01 = Mathf.Clamp01(speed / Mathf.Max(0.1f, fullSpeedReference));
            _phase += Time.deltaTime * Mathf.Lerp(5.2f, 11.4f, speed01);
            if (Time.time < _nextStep || Mathf.Sin(_phase) < 0.92f) return;
            _nextStep = Time.time + Mathf.Max(0.10f, minStepInterval - speed01 * 0.075f);
            _leftFoot = !_leftFoot;

            Vector3 right = transform.right * (_leftFoot ? -0.18f : 0.18f);
            EmitDust(transform.position + right + Vector3.up * 0.035f, 3, 0.55f, 0.18f);
        }

        private void OnDash()
        {
            if (_dust == null || motor == null || !motor.IsGrounded) return;
            Vector3 position = transform.position + Vector3.up * 0.04f;
            EmitDust(position, 14, 1.9f, 0.34f);
        }

        private void OnJumped()
        {
            if (_dust == null) return;
            EmitDust(transform.position + Vector3.up * 0.03f, 7, 0.90f, 0.30f);
        }

        private void OnLanded(float impactSpeed)
        {
            if (_dust == null) return;
            float impact01 = Mathf.Clamp01(impactSpeed / 14f);
            int count = Mathf.RoundToInt(Mathf.Lerp(5f, 16f, impact01));
            EmitDust(
                transform.position + Vector3.up * 0.025f,
                count,
                Mathf.Lerp(0.75f, 1.75f, impact01),
                Mathf.Lerp(0.18f, 0.42f, impact01));
        }

        private void BuildDust()
        {
            GameObject go = new GameObject("CinematicGroundInteractionVfx");
            go.transform.SetParent(transform, false);
            _dust = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = _dust.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 96;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.62f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.13f);
            main.startSpeed = 0f;
            main.gravityModifier = 0.18f;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.18f, 0.20f, 0.24f, 0.30f),
                new Color(0.34f, 0.30f, 0.28f, 0.16f));

            ParticleSystem.EmissionModule emission = _dust.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = _dust.shape;
            shape.enabled = false;
            ParticleSystem.ColorOverLifetimeModule color = _dust.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.32f, 0.12f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            ParticleSystemRenderer renderer = _dust.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            if (shader != null)
            {
                _dustMaterial = new Material(shader) { name = "MindforgeGroundDustRuntime" };
                renderer.sharedMaterial = _dustMaterial;
            }
        }

        private void EmitDust(Vector3 position, int count, float spread, float upward)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 disk = Random.insideUnitCircle * spread;
                ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
                emit.position = position + new Vector3(disk.x, 0f, disk.y) * 0.28f;
                emit.velocity = new Vector3(disk.x, upward * Random.Range(0.65f, 1.25f), disk.y) * Random.Range(0.35f, 0.80f);
                emit.startLifetime = Random.Range(0.28f, 0.58f);
                emit.startSize = Random.Range(0.045f, 0.14f) * (count > 8 ? 1.25f : 1f);
                _dust.Emit(emit, 1);
            }
        }

        private void OnDestroy()
        {
            if (_dustMaterial != null) Destroy(_dustMaterial);
        }
    }
}
