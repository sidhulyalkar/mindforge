using UnityEngine;
using UnityEngine.Rendering;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Grounded locomotion particles for the showcase Guardian. Emits only from already
    /// resolved movement/dodge state and never modifies Rigidbody velocity or combat.
    /// </summary>
    public sealed class GuardianLocomotionVfx : MonoBehaviour
    {
        [SerializeField] private GuardianMotor motor;
        [SerializeField] private float footstepSpeedThreshold = 1.45f;
        [SerializeField] private float minStepInterval = 0.24f;

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
            if (motor != null) motor.DashStarted += OnDash;
        }

        private void OnDisable()
        {
            if (motor != null) motor.DashStarted -= OnDash;
        }

        private void Update()
        {
            if (motor == null || _dust == null) return;
            Vector3 horizontal = Vector3.ProjectOnPlane(motor.Velocity, Vector3.up);
            float speed = horizontal.magnitude;
            if (speed < footstepSpeedThreshold || motor.IsDashing) return;

            _phase += Time.deltaTime * Mathf.Lerp(5.2f, 9.4f, Mathf.Clamp01(speed / 6.2f));
            if (Time.time < _nextStep || Mathf.Sin(_phase) < 0.92f) return;
            _nextStep = Time.time + Mathf.Max(0.12f, minStepInterval - Mathf.Clamp01(speed / 8f) * 0.08f);
            _leftFoot = !_leftFoot;

            Vector3 right = transform.right * (_leftFoot ? -0.18f : 0.18f);
            EmitDust(transform.position + right + Vector3.up * 0.035f, 3, 0.55f, 0.18f);
        }

        private void OnDash()
        {
            if (_dust == null) return;
            Vector3 position = transform.position + Vector3.up * 0.04f;
            EmitDust(position, 14, 1.9f, 0.34f);
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
