using System;
using System.Collections.Generic;
using Mindforge.Combat;
using Mindforge.SoulWisp;
using UnityEngine;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Presentation-only arena response for the Fractured Signal encounter.
    ///
    /// It listens to the existing boss phase/telegraph/fire events and changes only visual
    /// transforms, emission and local light intensity. It never applies damage, collision,
    /// movement, target changes, timing authority or neural evidence.
    /// </summary>
    [DefaultExecutionOrder(900)]
    public sealed class FracturedArenaDynamicsV27 : MonoBehaviour
    {
        [SerializeField] private FracturedSignalDirector director;
        [SerializeField] private SoulWispController wisp;

        private Transform[] _spines = Array.Empty<Transform>();
        private Vector3[] _spineBaseScales = Array.Empty<Vector3>();
        private Renderer[] _signalRenderers = Array.Empty<Renderer>();
        private Light[] _arenaLights = Array.Empty<Light>();
        private float[] _lightBaseIntensity = Array.Empty<float>();
        private MaterialPropertyBlock _block;
        private int _phase = 1;
        private float _charge;
        private float _release;
        private bool _resolved;
        private bool _neuralFrozen;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            Resolve();
            ResolveChildren();
        }

        private void OnEnable()
        {
            Resolve();
            if (director != null)
            {
                _phase = director.Phase;
                director.PhaseChanged += OnPhaseChanged;
                director.AttackTelegraphed += OnTelegraphed;
                director.AttackFired += OnFired;
            }
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.PhaseChanged -= OnPhaseChanged;
                director.AttackTelegraphed -= OnTelegraphed;
                director.AttackFired -= OnFired;
            }
        }

        private void Resolve()
        {
            if (director == null) director = FindObjectOfType<FracturedSignalDirector>(true);
            if (wisp == null) wisp = FindObjectOfType<SoulWispController>(true);
            if (_block == null) _block = new MaterialPropertyBlock();
        }

        private void ResolveChildren()
        {
            List<Transform> spines = new List<Transform>();
            List<Renderer> signal = new List<Renderer>();
            Transform[] all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform child = all[i];
                if (child == null) continue;
                if (child.name.StartsWith("V27_CorruptionSpine_", StringComparison.Ordinal)) spines.Add(child);
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                string n = renderer.gameObject.name;
                if (n.IndexOf("Signal", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Rite", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Fracture", StringComparison.OrdinalIgnoreCase) >= 0)
                    signal.Add(renderer);
            }

            _spines = spines.ToArray();
            _spineBaseScales = new Vector3[_spines.Length];
            for (int i = 0; i < _spines.Length; i++) _spineBaseScales[i] = _spines[i].localScale;
            _signalRenderers = signal.ToArray();
            _arenaLights = GetComponentsInChildren<Light>(true);
            _lightBaseIntensity = new float[_arenaLights.Length];
            for (int i = 0; i < _arenaLights.Length; i++) _lightBaseIntensity[i] = _arenaLights[i].intensity;
            _resolved = true;
        }

        private void LateUpdate()
        {
            Resolve();
            if (!_resolved) ResolveChildren();

            bool neural = NeuralVisualFieldActive();
            if (neural)
            {
                if (!_neuralFrozen)
                {
                    _neuralFrozen = true;
                    _charge = 0f;
                    _release = 0f;
                    ApplyState(true);
                }
                return;
            }

            _neuralFrozen = false;
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            _charge = Damp(_charge, 0f, 3.0f, dt);
            _release = Damp(_release, 0f, 7.0f, dt);
            ApplyState(false);
        }

        private void ApplyState(bool neutral)
        {
            float phase01 = Mathf.InverseLerp(1f, 3f, _phase);
            float growth = neutral ? 0.42f : Mathf.Lerp(0.48f, 1.0f, phase01) + _release * 0.08f;
            for (int i = 0; i < _spines.Length; i++)
            {
                Transform spine = _spines[i];
                if (spine == null) continue;
                Vector3 baseScale = i < _spineBaseScales.Length ? _spineBaseScales[i] : Vector3.one;
                float stagger = 0.94f + (i % 3) * 0.035f;
                spine.localScale = new Vector3(baseScale.x, baseScale.y * growth * stagger, baseScale.z);
            }

            float emission = neutral ? 0.55f : 0.80f + phase01 * 0.65f + _charge * 1.35f + _release * 0.75f;
            Color color = Color.Lerp(new Color(0.26f, 0.18f, 0.38f), new Color(0.95f, 0.055f, 1.0f), Mathf.Lerp(0.35f, 0.78f, phase01));
            for (int i = 0; i < _signalRenderers.Length; i++)
            {
                Renderer renderer = _signalRenderers[i];
                if (renderer == null) continue;
                renderer.GetPropertyBlock(_block);
                _block.SetColor(BaseColor, color * 0.24f);
                _block.SetColor(ColorProperty, color * 0.24f);
                _block.SetColor(EmissionColor, color * emission);
                renderer.SetPropertyBlock(_block);
            }

            for (int i = 0; i < _arenaLights.Length; i++)
            {
                Light light = _arenaLights[i];
                if (light == null) continue;
                float baseline = i < _lightBaseIntensity.Length ? _lightBaseIntensity[i] : 1f;
                light.intensity = neutral
                    ? baseline * 0.55f
                    : baseline * (0.74f + phase01 * 0.30f + _charge * 0.42f + _release * 0.28f);
            }
        }

        private bool NeuralVisualFieldActive()
        {
            if (wisp == null) wisp = FindObjectOfType<SoulWispController>(true);
            return wisp != null && (wisp.CalibrationStimuliActive || wisp.ResonanceWindowActive);
        }

        private void OnPhaseChanged(int phase)
        {
            _phase = Mathf.Clamp(phase, 1, 3);
            _release = 1f;
        }

        private void OnTelegraphed(string pattern, int count, bool heavy)
        {
            _charge = heavy ? 1f : 0.72f;
        }

        private void OnFired(string pattern, int count, bool heavy)
        {
            _charge = 0f;
            _release = heavy ? 1f : 0.65f;
        }

        private static float Damp(float value, float target, float sharpness, float dt)
            => Mathf.Lerp(value, target, 1f - Mathf.Exp(-Mathf.Max(0.01f, sharpness) * dt));
    }
}
