using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Production BCI presentation for the first real two-class experiment.
    /// Sight is requested at 10 Hz and Guard at 12 Hz, matching the Python decoder.
    /// Concord remains a game semantic but is intentionally not presented as a
    /// selectable EEG target in V0.33.
    /// </summary>
    [DefaultExecutionOrder(930)]
    [DisallowMultipleComponent]
    public sealed class MindforgeBciStimulusV33 : MonoBehaviour
    {
        public const float SightFrequencyHz = 10f;
        public const float GuardFrequencyHz = 12f;
        public const int ProductionTargetCount = 2;

        public enum PresentationMode
        {
            Hidden = 0,
            Idle = 1,
            CalibrationBaseline = 2,
            CalibrationSight = 3,
            CalibrationGuard = 4,
            Listening = 5,
            Resolved = 6,
        }

        [Header("Placement")]
        [SerializeField] private Vector3 cameraLocalPosition = new Vector3(0.64f, -0.30f, 2.05f);
        [SerializeField] private float panelScale = 1f;

        [Header("Temporal presentation")]
        [SerializeField, Range(0f, 0.22f)] private float reducedContrast = 0.18f;
        [SerializeField] private float selectionHoldSeconds = 0.70f;

        [Header("Visual language")]
        [SerializeField] private Color sightColor = new Color(0.20f, 0.92f, 1.00f, 1f);
        [SerializeField] private Color guardColor = new Color(0.92f, 0.76f, 0.34f, 1f);
        [SerializeField] private Color shellColor = new Color(0.045f, 0.065f, 0.095f, 1f);
        [SerializeField] private float baseEmission = 2.4f;

        private sealed class StimulusNode
        {
            public MindforgeIntentV29 intent;
            public float frequencyHz;
            public Color color;
            public Material material;
            public Transform transform;
        }

        private readonly List<StimulusNode> _nodes = new List<StimulusNode>(ProductionTargetCount);
        private Transform _root;
        private Material _shellMaterial;
        private TextMeshPro _header;
        private Camera _camera;
        private MindforgeIntentV29 _selected = MindforgeIntentV29.None;
        private float _selectedUntil;
        private bool _participantPaused;
        private long _activeEpoch = -1;

        public event Action<bool> ParticipantPauseChanged;

        public bool Installed { get; private set; }
        public bool ParticipantPaused => _participantPaused;
        public bool ModulationActive => !_participantPaused &&
            (Mode == PresentationMode.CalibrationSight ||
             Mode == PresentationMode.CalibrationGuard ||
             Mode == PresentationMode.Listening);
        public int NodeCount => _nodes.Count;
        public long ActiveEpoch => _activeEpoch;
        public PresentationMode Mode { get; private set; } = PresentationMode.Hidden;
        public string FrequencyLabel => "Sight 10 Hz | Guard 12 Hz";

        private void Start()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                Debug.LogWarning("[Mindforge:V33] No main camera; BCI stimulus disabled.");
                enabled = false;
                return;
            }

            BuildPanel();
            SetMode(PresentationMode.Idle);
            Installed = _root != null && _nodes.Count == ProductionTargetCount;
        }

        private void OnDestroy()
        {
            if (_root != null) Destroy(_root.gameObject);
            DestroyMaterial(_shellMaterial);
            for (int i = 0; i < _nodes.Count; i++) DestroyMaterial(_nodes[i].material);
        }

        private void LateUpdate()
        {
            if (!Installed) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.bKey.wasPressedThisFrame)
                ToggleParticipantPause();

            float now = Time.unscaledTime;
            bool selected = _selected != MindforgeIntentV29.None && now <= _selectedUntil;
            bool temporal = ModulationActive;
            float contrast = Mathf.Clamp(reducedContrast, 0f, 0.22f);

            for (int i = 0; i < _nodes.Count; i++)
            {
                StimulusNode node = _nodes[i];
                float wave = temporal
                    ? 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * node.frequencyHz * now)
                    : 0.5f;
                float luminance = Mathf.Lerp(1f - contrast, 1f + contrast, wave);

                bool isSelected = selected && node.intent == _selected;
                bool isCalibrationFocus =
                    (Mode == PresentationMode.CalibrationSight && node.intent == MindforgeIntentV29.Sight) ||
                    (Mode == PresentationMode.CalibrationGuard && node.intent == MindforgeIntentV29.Guard);

                float gain = isSelected ? 1.30f : isCalibrationFocus ? 1.12f : 1f;
                ApplyEmission(node.material, node.color, baseEmission * luminance * gain);

                float diameter = isSelected ? 0.094f : isCalibrationFocus ? 0.086f : 0.078f;
                if (node.transform != null)
                    node.transform.localScale = Vector3.one * diameter * panelScale;
            }
        }

        public void ToggleParticipantPause()
        {
            SetParticipantPaused(!_participantPaused);
        }

        public void SetParticipantPaused(bool paused)
        {
            if (_participantPaused == paused) return;
            _participantPaused = paused;
            if (_participantPaused)
            {
                _activeEpoch = -1;
                _selected = MindforgeIntentV29.None;
            }
            UpdateHeader();
            ParticipantPauseChanged?.Invoke(_participantPaused);
        }

        public void SetVisible(bool visible)
        {
            if (_root != null) _root.gameObject.SetActive(visible);
            if (!visible) Mode = PresentationMode.Hidden;
            else if (Mode == PresentationMode.Hidden) SetMode(PresentationMode.Idle);
        }

        public void BeginCalibrationBaseline()
        {
            _activeEpoch = -1;
            SetMode(PresentationMode.CalibrationBaseline);
        }

        public void BeginCalibrationTarget(MindforgeIntentV29 intent)
        {
            _activeEpoch = -1;
            if (intent == MindforgeIntentV29.Sight) SetMode(PresentationMode.CalibrationSight);
            else if (intent == MindforgeIntentV29.Guard) SetMode(PresentationMode.CalibrationGuard);
        }

        public void BeginListening(long epoch)
        {
            _activeEpoch = epoch;
            _selected = MindforgeIntentV29.None;
            SetMode(PresentationMode.Listening);
        }

        public void ResolveSelection(MindforgeIntentV29 intent)
        {
            _selected = intent;
            _selectedUntil = Time.unscaledTime + selectionHoldSeconds;
            SetMode(PresentationMode.Resolved);
        }

        public void EndListening()
        {
            _activeEpoch = -1;
            _selected = MindforgeIntentV29.None;
            SetMode(PresentationMode.Idle);
        }

        private void SetMode(PresentationMode mode)
        {
            Mode = mode;
            if (_root != null && !_root.gameObject.activeSelf && mode != PresentationMode.Hidden)
                _root.gameObject.SetActive(true);
            UpdateHeader();
        }

        private void BuildPanel()
        {
            GameObject root = new GameObject("Mindforge_BCI_Targets_V33");
            root.transform.SetParent(_camera.transform, false);
            root.transform.localPosition = cameraLocalPosition;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            _root = root.transform;

            _shellMaterial = CreateLitMaterial("MF_V33_BCI_Shell", shellColor);
            GameObject shell = CreateSphere("Shell", _root, Vector3.zero, 0.17f * panelScale, _shellMaterial);
            Renderer shellRenderer = shell.GetComponent<Renderer>();
            if (shellRenderer != null)
            {
                shellRenderer.shadowCastingMode = ShadowCastingMode.Off;
                shellRenderer.receiveShadows = false;
            }

            CreateStimulus(MindforgeIntentV29.Sight, SightFrequencyHz, sightColor,
                new Vector3(-0.105f, 0f, -0.025f), "SIGHT\n10 Hz");
            CreateStimulus(MindforgeIntentV29.Guard, GuardFrequencyHz, guardColor,
                new Vector3(0.105f, 0f, -0.025f), "GUARD\n12 Hz");

            _header = CreateLabel("Header", new Vector3(0f, 0.145f, 0.002f), string.Empty,
                new Color(0.80f, 0.88f, 0.96f, 1f), 0.58f);
        }

        private void CreateStimulus(
            MindforgeIntentV29 intent,
            float frequency,
            Color color,
            Vector3 localPosition,
            string label)
        {
            Material material = CreateEmissionMaterial("MF_V33_BCI_" + intent, color, baseEmission);
            GameObject node = CreateSphere(intent + "Target", _root, localPosition, 0.078f * panelScale, material);
            Renderer renderer = node.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            CreateLabel(intent + "Label", localPosition + new Vector3(0f, -0.085f, 0.002f), label, color, 0.65f);
            _nodes.Add(new StimulusNode
            {
                intent = intent,
                frequencyHz = frequency,
                color = color,
                material = material,
                transform = node.transform,
            });
        }

        private void UpdateHeader()
        {
            if (_header == null) return;
            if (_participantPaused)
            {
                _header.text = "BCI PAUSED  •  B RESUME";
                return;
            }

            switch (Mode)
            {
                case PresentationMode.CalibrationBaseline:
                    _header.text = "CALIBRATION  •  REST";
                    break;
                case PresentationMode.CalibrationSight:
                    _header.text = "CALIBRATION  •  FOCUS SIGHT";
                    break;
                case PresentationMode.CalibrationGuard:
                    _header.text = "CALIBRATION  •  FOCUS GUARD";
                    break;
                case PresentationMode.Listening:
                    _header.text = $"NEURAL WINDOW #{_activeEpoch}";
                    break;
                case PresentationMode.Resolved:
                    _header.text = _selected == MindforgeIntentV29.None ? "NO STABLE RESONANCE" : _selected.ToString().ToUpperInvariant();
                    break;
                default:
                    _header.text = "BCI READY  •  B PAUSE";
                    break;
            }
        }

        private TextMeshPro CreateLabel(string name, Vector3 localPosition, string text, Color color, float fontSize)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_root, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * 0.10f * panelScale;
            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.enableWordWrapping = false;
            tmp.rectTransform.sizeDelta = new Vector2(2.5f, 0.60f);
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
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.12f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.72f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.72f);
            ApplyEmission(material, color, intensity);
            return material;
        }

        private static Material CreateLitMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.28f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.75f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.75f);
            return material;
        }

        private static void ApplyEmission(Material material, Color color, float intensity)
        {
            if (material == null) return;
            Color baseColor = color * Mathf.Lerp(0.62f, 1.0f, Mathf.Clamp01(intensity / 3f));
            baseColor.a = color.a;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
            if (material.HasProperty("_Color")) material.SetColor("_Color", baseColor);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * Mathf.Max(0f, intensity));
            }
        }

        private static void DestroyMaterial(Material material)
        {
            if (material != null) Destroy(material);
        }
    }
}
