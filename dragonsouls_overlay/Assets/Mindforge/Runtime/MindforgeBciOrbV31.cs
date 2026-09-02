using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Camera-anchored, presentation-only BCI stimulus preview for the Dragon Souls
    /// production slice. Three small nodes use analytic sinusoidal luminance modulation
    /// at requested 8/10/12 Hz frequencies for Sight/Guard/Concord.
    ///
    /// IMPORTANT: these are requested simulation frequencies, not measured display
    /// frequencies. Monitor refresh, frame pacing and display response determine the
    /// physically presented stimulus. This component publishes no BCI decisions and
    /// owns no movement, combat, damage, camera or game-pause authority. The B key
    /// only pauses/resumes this local visual modulation while leaving the orb visible.
    /// </summary>
    [DefaultExecutionOrder(940)]
    [DisallowMultipleComponent]
    public sealed class MindforgeBciOrbV31 : MonoBehaviour
    {
        public const float SightFrequencyHz = 8f;
        public const float GuardFrequencyHz = 10f;
        public const float ConcordFrequencyHz = 12f;
        public const int StimulusNodeCount = 3;

        [Header("Preview placement")]
        [SerializeField] private Vector3 cameraLocalPosition = new Vector3(0.67f, -0.34f, 2.15f);
        [SerializeField] private float orbScale = 1f;

        [Header("Temporal preview")]
        [SerializeField] private bool simulationEnabled = true;
        [SerializeField, Range(0f, 0.22f)] private float reducedContrast = 0.18f;
        [SerializeField] private bool allowHighContrastPreview = false;
        [SerializeField, Range(0.22f, 0.70f)] private float highContrast = 0.48f;
        [SerializeField] private float selectionHoldSeconds = 0.70f;

        [Header("Visual language")]
        [SerializeField] private Color sightColor = new Color(0.20f, 0.92f, 1.00f, 1f);
        [SerializeField] private Color guardColor = new Color(0.92f, 0.76f, 0.34f, 1f);
        [SerializeField] private Color concordColor = new Color(0.90f, 0.24f, 0.72f, 1f);
        [SerializeField] private Color coreColor = new Color(0.35f, 0.72f, 0.86f, 1f);
        [SerializeField] private float baseEmission = 2.6f;

        private sealed class StimulusNode
        {
            public MindforgeIntentV29 intent;
            public float frequencyHz;
            public Color color;
            public Material material;
            public Transform transform;
        }

        private readonly List<StimulusNode> _nodes = new List<StimulusNode>(StimulusNodeCount);
        private Transform _visualRoot;
        private Material _coreMaterial;
        private Material _shellMaterial;
        private TextMeshPro _headerLabel;
        private MindforgeIntentV29 _selectedIntent = MindforgeIntentV29.None;
        private float _selectedUntil;
        private Camera _camera;

        public bool Installed { get; private set; }
        public bool SimulationEnabled => simulationEnabled;
        public bool HighContrastPreviewEnabled => allowHighContrastPreview;
        public bool ReducedContrastDefault => !allowHighContrastPreview && reducedContrast <= 0.22f;
        public float CurrentContrast => allowHighContrastPreview ? highContrast : reducedContrast;
        public int NodeCount => _nodes.Count;
        public string FrequencyLabel => "Sight 8 Hz | Guard 10 Hz | Concord 12 Hz";

        private void Start()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                enabled = false;
                return;
            }

            BuildOrb();
            UpdateHeader();
            MindforgeIntentBusV29.IntentPublished += HandleIntentPublished;
            Installed = _visualRoot != null && _nodes.Count == StimulusNodeCount;
        }

        private void OnDestroy()
        {
            MindforgeIntentBusV29.IntentPublished -= HandleIntentPublished;
            if (_visualRoot != null) Destroy(_visualRoot.gameObject);
            DestroyRuntimeMaterial(_coreMaterial);
            DestroyRuntimeMaterial(_shellMaterial);
            for (int i = 0; i < _nodes.Count; i++)
                DestroyRuntimeMaterial(_nodes[i].material);
        }

        private void LateUpdate()
        {
            if (!Installed) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.bKey.wasPressedThisFrame)
            {
                simulationEnabled = !simulationEnabled;
                UpdateHeader();
            }

            float now = Time.unscaledTime;
            float contrast = Mathf.Clamp01(CurrentContrast);
            bool hasSelection = _selectedIntent != MindforgeIntentV29.None && now <= _selectedUntil;

            for (int i = 0; i < _nodes.Count; i++)
            {
                StimulusNode node = _nodes[i];
                float wave = simulationEnabled
                    ? 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * node.frequencyHz * now)
                    : 0.5f;
                float low = 1f - contrast;
                float high = 1f + contrast;
                float luminance = Mathf.Lerp(low, high, wave);

                bool selected = hasSelection && node.intent == _selectedIntent;
                float selectionGain = selected ? 1.25f : 1f;
                ApplyEmission(node.material, node.color, baseEmission * luminance * selectionGain);
                if (node.transform != null)
                {
                    float targetScale = selected ? 0.080f : 0.068f;
                    node.transform.localScale = Vector3.one * targetScale * orbScale;
                }
            }

            if (_coreMaterial != null)
            {
                Color resolved = coreColor;
                if (hasSelection)
                    resolved = Color.Lerp(coreColor, ColorForIntent(_selectedIntent), 0.58f);
                float corePulse = simulationEnabled
                    ? 1f + Mathf.Sin(now * Mathf.PI * 1.4f) * 0.08f
                    : 1f;
                ApplyEmission(_coreMaterial, resolved, 1.8f * corePulse);
            }
        }

        public float GetRequestedFrequencyHz(MindforgeIntentV29 intent)
        {
            switch (intent)
            {
                case MindforgeIntentV29.Sight: return SightFrequencyHz;
                case MindforgeIntentV29.Guard: return GuardFrequencyHz;
                case MindforgeIntentV29.Concord: return ConcordFrequencyHz;
                default: return 0f;
            }
        }

        private void BuildOrb()
        {
            GameObject root = new GameObject("Mindforge_BCI_Orb_V31");
            root.transform.SetParent(_camera.transform, false);
            root.transform.localPosition = cameraLocalPosition;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            _visualRoot = root.transform;

            _shellMaterial = CreateLitMaterial(
                "MF_V31_BCI_OrbShell",
                new Color(0.055f, 0.075f, 0.105f, 1f),
                0.68f,
                0.78f);
            GameObject shell = CreateSphere("OrbShell", _visualRoot, Vector3.zero, 0.115f * orbScale, _shellMaterial);
            Renderer shellRenderer = shell.GetComponent<Renderer>();
            if (shellRenderer != null)
            {
                shellRenderer.shadowCastingMode = ShadowCastingMode.Off;
                shellRenderer.receiveShadows = false;
            }

            _coreMaterial = CreateEmissionMaterial("MF_V31_BCI_Core", coreColor, 1.8f);
            CreateSphere("SignalCore", _visualRoot, new Vector3(0f, 0f, -0.010f), 0.062f * orbScale, _coreMaterial);

            CreateStimulus(
                MindforgeIntentV29.Sight,
                SightFrequencyHz,
                sightColor,
                new Vector3(-0.145f, 0.092f, -0.012f),
                "SIGHT\n8 Hz");
            CreateStimulus(
                MindforgeIntentV29.Guard,
                GuardFrequencyHz,
                guardColor,
                new Vector3(0.145f, 0.092f, -0.012f),
                "GUARD\n10 Hz");
            CreateStimulus(
                MindforgeIntentV29.Concord,
                ConcordFrequencyHz,
                concordColor,
                new Vector3(0f, -0.155f, -0.012f),
                "CONCORD\n12 Hz");

            CreateHeader();
        }

        private void CreateStimulus(
            MindforgeIntentV29 intent,
            float frequency,
            Color color,
            Vector3 localPosition,
            string labelText)
        {
            Material material = CreateEmissionMaterial("MF_V31_BCI_" + intent, color, baseEmission);
            GameObject nodeObject = CreateSphere(intent + "Stimulus", _visualRoot, localPosition, 0.068f * orbScale, material);
            Renderer renderer = nodeObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            CreateLabel(intent + "Label", localPosition + new Vector3(0f, -0.076f, 0.002f), labelText, color, 0.66f);
            _nodes.Add(new StimulusNode
            {
                intent = intent,
                frequencyHz = frequency,
                color = color,
                material = material,
                transform = nodeObject.transform,
            });
        }

        private void CreateHeader()
        {
            _headerLabel = CreateLabel(
                "PreviewLabel",
                new Vector3(0f, 0.205f, 0.002f),
                string.Empty,
                new Color(0.78f, 0.86f, 0.94f, 1f),
                0.56f);
            if (_headerLabel != null)
                _headerLabel.fontStyle = FontStyles.SmallCaps;
        }

        private void UpdateHeader()
        {
            if (_headerLabel == null) return;
            _headerLabel.text = simulationEnabled
                ? "BCI SIM  •  REDUCED CONTRAST  •  B PAUSE"
                : "BCI SIM PAUSED  •  B RESUME";
        }

        private TextMeshPro CreateLabel(string name, Vector3 localPosition, string text, Color color, float fontSize)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_visualRoot, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * 0.10f * orbScale;

            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.enableWordWrapping = false;
            tmp.rectTransform.sizeDelta = new Vector2(2.2f, 0.55f);
            return tmp;
        }

        private static GameObject CreateSphere(string name, Transform parent, Vector3 localPosition, float diameter, Material material)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent, false);
            sphere.transform.localPosition = localPosition;
            sphere.transform.localRotation = Quaternion.identity;
            sphere.transform.localScale = Vector3.one * diameter;
            Collider collider = sphere.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            Renderer renderer = sphere.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            return sphere;
        }

        private static Material CreateEmissionMaterial(string name, Color color, float intensity)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader) { name = name };
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.18f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.70f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.70f);
            ApplyEmission(material, color, intensity);
            return material;
        }

        private static Material CreateLitMaterial(string name, Color color, float metallic, float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            return material;
        }

        private static void ApplyEmission(Material material, Color color, float intensity)
        {
            if (material == null) return;
            float baseGain = Mathf.Lerp(0.62f, 1.0f, Mathf.Clamp01(intensity / 3f));
            Color baseColor = color * baseGain;
            baseColor.a = color.a;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
            if (material.HasProperty("_Color")) material.SetColor("_Color", baseColor);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * Mathf.Max(0f, intensity));
            }
        }

        private void HandleIntentPublished(MindforgeIntentEventV29 evt)
        {
            _selectedIntent = evt.Intent;
            _selectedUntil = Time.unscaledTime + selectionHoldSeconds * Mathf.Lerp(0.65f, 1.25f, evt.Confidence);
        }

        private Color ColorForIntent(MindforgeIntentV29 intent)
        {
            switch (intent)
            {
                case MindforgeIntentV29.Sight: return sightColor;
                case MindforgeIntentV29.Guard: return guardColor;
                case MindforgeIntentV29.Concord: return concordColor;
                default: return coreColor;
            }
        }

        private static void DestroyRuntimeMaterial(Material material)
        {
            if (material != null) Destroy(material);
        }
    }
}
