using System;
using UnityEngine;
using Mindforge.Neural;
using Mindforge.Presentation;

namespace Mindforge.SoulWisp
{
    /// <summary>
    /// Diegetic decoder feedback for the NON-CODED visual shell around each aura.
    ///
    /// Never attach these renderers to the same material/mesh that carries the
    /// measured 10/12 Hz luminance code. Decoder evidence is allowed to alter halo
    /// size, particles and slow shell color, but it must not amplitude-modulate the
    /// stimulus core that produced the EEG evidence.
    /// </summary>
    public sealed class NeuralAuraFeedback : MonoBehaviour
    {
        [Serializable]
        private sealed class AuraShell
        {
            public Transform root;
            public Renderer shellRenderer;
            public ParticleSystem particles;

            [NonSerialized] public Vector3 baseScale;
            [NonSerialized] public Vector3 baseLocalPosition;
            [NonSerialized] public float baseEmissionRate;
            [NonSerialized] public float smoothScore;
            [NonSerialized] public MaterialPropertyBlock block;
        }

        [SerializeField] private UdpNeuralReceiver receiver;
        [SerializeField] private CombatVisualPalette palette;
        [SerializeField] private AuraShell sight;
        [SerializeField] private AuraShell guard;
        [SerializeField] private AudioSource evidenceTone;

        [Header("Shell response")]
        [SerializeField] private float scaleAtZero = 0.90f;
        [SerializeField] private float scaleAtStrongEvidence = 1.16f;
        [SerializeField] private float smoothing = 7.5f;
        [SerializeField] private float disconnectedJitter = 0.025f;
        [SerializeField] private float artifactJitter = 0.045f;

        private float _sightScore;
        private float _guardScore;
        private float _quality;
        private bool _artifact;
        private bool _connected;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            Initialize(sight);
            Initialize(guard);
        }

        private void OnEnable()
        {
            if (receiver == null) return;
            receiver.EvidenceReceived += OnEvidence;
            receiver.ConnectionStateChanged += OnConnectionStateChanged;
            _connected = receiver.IsConnected;
        }

        private void OnDisable()
        {
            if (receiver != null)
            {
                receiver.EvidenceReceived -= OnEvidence;
                receiver.ConnectionStateChanged -= OnConnectionStateChanged;
            }
            ResetShell(sight);
            ResetShell(guard);
            if (evidenceTone != null) evidenceTone.Stop();
        }

        private static void Initialize(AuraShell shell)
        {
            if (shell == null || shell.root == null) return;
            shell.baseScale = shell.root.localScale;
            shell.baseLocalPosition = shell.root.localPosition;
            shell.block = new MaterialPropertyBlock();
            if (shell.particles != null)
            {
                var emission = shell.particles.emission;
                shell.baseEmissionRate = emission.rateOverTime.constant;
            }
        }

        private void OnEvidence(NeuralEvent evt)
        {
            if (evt == null) return;
            _quality = Mathf.Clamp01(evt.quality);
            _artifact = evt.artifact;
            if (evt.has_evidence)
            {
                _sightScore = Mathf.Clamp01(evt.sight_score);
                _guardScore = Mathf.Clamp01(evt.guard_score);
            }
            else
            {
                _sightScore = 0f;
                _guardScore = 0f;
            }
        }

        private void OnConnectionStateChanged(bool connected)
        {
            _connected = connected;
            if (!connected)
            {
                _sightScore = 0f;
                _guardScore = 0f;
            }
        }

        private void Update()
        {
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            Color sightColor = palette != null ? palette.sightTarget : new Color(0.29f, 0.62f, 1f, 1f);
            Color guardColor = palette != null ? palette.guardTarget : new Color(0.27f, 0.95f, 0.60f, 1f);
            UpdateShell(sight, _sightScore, sightColor, dt, 11.7f);
            UpdateShell(guard, _guardScore, guardColor, dt, 23.1f);

            if (evidenceTone != null)
            {
                float evidence = Mathf.Max(sight != null ? sight.smoothScore : 0f,
                                           guard != null ? guard.smoothScore : 0f);
                float authority = (_connected && !_artifact) ? _quality : 0f;
                evidenceTone.volume = Mathf.Lerp(evidenceTone.volume, evidence * authority * 0.10f,
                    1f - Mathf.Exp(-5f * dt));
                evidenceTone.pitch = 0.92f + evidence * 0.28f;
                if (evidenceTone.volume > 0.003f && !evidenceTone.isPlaying) evidenceTone.Play();
            }
        }

        private void UpdateShell(AuraShell shell, float score, Color reservedColor, float dt, float noiseSeed)
        {
            if (shell == null || shell.root == null) return;
            float response = 1f - Mathf.Exp(-Mathf.Max(0.1f, smoothing) * dt);
            shell.smoothScore = Mathf.Lerp(shell.smoothScore, score, response);

            float authority = _connected && !_artifact ? _quality : 0f;
            float scale = Mathf.Lerp(scaleAtZero, scaleAtStrongEvidence, shell.smoothScore * authority);
            shell.root.localScale = Vector3.Lerp(shell.root.localScale, shell.baseScale * scale, response);

            float jitter = !_connected ? disconnectedJitter : _artifact ? artifactJitter : 0f;
            if (jitter > 0f)
            {
                float t = Time.unscaledTime;
                Vector3 offset = new Vector3(
                    Mathf.PerlinNoise(noiseSeed, t * 13f) - 0.5f,
                    Mathf.PerlinNoise(noiseSeed + 7.1f, t * 15f) - 0.5f,
                    0f) * jitter;
                shell.root.localPosition = shell.baseLocalPosition + offset;
            }
            else
            {
                shell.root.localPosition = Vector3.Lerp(shell.root.localPosition, shell.baseLocalPosition, response);
            }

            Color muted = new Color(0.55f, 0.57f, 0.64f, 1f);
            Color shellColor = Color.Lerp(muted, reservedColor, Mathf.Clamp01(0.25f + authority * 0.75f));
            float emission = 0.18f + shell.smoothScore * authority * 1.25f;

            if (shell.shellRenderer != null)
            {
                shell.shellRenderer.GetPropertyBlock(shell.block);
                shell.block.SetColor(BaseColor, shellColor);
                shell.block.SetColor(ColorProperty, shellColor);
                shell.block.SetColor(EmissionColor, shellColor * emission);
                shell.shellRenderer.SetPropertyBlock(shell.block);
            }

            if (shell.particles != null)
            {
                var main = shell.particles.main;
                main.startColor = shellColor;
                var particleEmission = shell.particles.emission;
                particleEmission.rateOverTime = shell.baseEmissionRate * (0.20f + shell.smoothScore * authority * 1.15f);
            }
        }

        private static void ResetShell(AuraShell shell)
        {
            if (shell == null || shell.root == null) return;
            shell.root.localScale = shell.baseScale;
            shell.root.localPosition = shell.baseLocalPosition;
            if (shell.particles != null)
            {
                var emission = shell.particles.emission;
                emission.rateOverTime = shell.baseEmissionRate;
            }
        }
    }
}
