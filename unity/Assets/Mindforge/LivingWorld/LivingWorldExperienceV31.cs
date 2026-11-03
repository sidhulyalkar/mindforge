using System;
using UnityEngine;
using Mindforge.Calibration;
using Mindforge.Presentation;
using Mindforge.SoulWisp;

namespace Mindforge.LivingWorld
{
    /// <summary>
    /// V3.1 opt-in, presentation-only integration for the V11 authored game route.
    /// OFF by default: no UDP binding, no mesh, no graphics mutation until opt-in.
    /// Never changes RenderSettings, lights, global shaders, camera, gameplay,
    /// coded VEP stimuli, or the Sight/Guard authority receiver.
    /// </summary>
    [DefaultExecutionOrder(780)]
    public sealed class LivingWorldExperienceV31 : MonoBehaviour
    {
        private const string EnabledKey = "mindforge.living_world.v31.enabled";
        private const string IntensityKey = "mindforge.living_world.v31.intensity";
        private const string ReducedKey = "mindforge.living_world.v31.reduced";
        public const string AuthoredWorldRootName = "Mindforge_Demo_World_V11";
        public const float MaximumIntensity = 0.30f;
        public const float ReducedVisualIntensity = 0.06f;

        private static readonly Vector3[] SanctuaryAccents =
        {
            new Vector3(-9.35f, 1.65f, -19.5f),
            new Vector3( 9.35f, 1.65f, -19.5f),
            new Vector3(-9.35f, 1.65f, -13.5f),
            new Vector3( 9.35f, 1.65f, -13.5f),
            new Vector3(-9.35f, 1.65f, -7.5f),
            new Vector3( 9.35f, 1.65f, -7.5f),
        };

        private bool _enabledByPlayer;
        private bool _reducedVisuals;
        private float _intensity;
        private bool _menuVisible;
        private GameObject _connectionRoot;
        private GameObject _visualRoot;
        private LivingWorldPassiveReceiver _receiver;
        private Material _decorativeMaterial;
        private SoulWispController _wisp;
        private AwakeningCalibrationDirector _calibration;
        private float _nextResolveTime;
        private float _lastTick;
        private readonly float[] _frameSamplesMs = new float[240];
        private readonly float[] _scratch = new float[240];
        private int _frameCount;
        private int _writeIndex;
        private float _meanFrameMs;
        private float _p95FrameMs;
        private float _nextStatsTime;

        public bool EnabledByPlayer => _enabledByPlayer;
        public bool ReducedVisuals => _reducedVisuals;
        public float EffectiveIntensity => _enabledByPlayer
            ? Mathf.Min(Mathf.Clamp(_intensity, 0f, MaximumIntensity),
                _reducedVisuals ? ReducedVisualIntensity : MaximumIntensity)
            : 0f;
        public int DecorativeSurfaceCount => SanctuaryAccents.Length;
        public float AverageFrameMilliseconds => _meanFrameMs;
        public float P95FrameMilliseconds => _p95FrameMs;

        /// <summary>Pure bounded visual mapping; not a classifier or gameplay action.</summary>
        public static float MapFeature(float rawValue, float amplitude)
        {
            return Mathf.Clamp01(
                PassiveFeaturePolicy.NeutralValue +
                (Mathf.Clamp01(rawValue) - PassiveFeaturePolicy.NeutralValue) *
                Mathf.Clamp(amplitude, 0f, MaximumIntensity));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            MindforgeDemoV11Marker marker = FindObjectOfType<MindforgeDemoV11Marker>(true);
            if (marker == null || !marker.gameObject.scene.IsValid()) return;
            if (marker.GetComponent<LivingWorldExperienceV31>() != null) return;
            marker.gameObject.AddComponent<LivingWorldExperienceV31>();
        }

        private void Start()
        {
            // No speculative integration with the competition/calibration-only
            // scene or arbitrary playable worlds. V11 is the explicit, authored target.
            Transform authoredWorld = FindAuthoredV11World();
            if (authoredWorld == null)
            {
                Debug.LogWarning("[LivingWorld:V31] V11 authored world missing; integration off.");
                enabled = false;
                return;
            }
            // Latest V0.28 retains the V11 marker, but installs a completely
            // different V24 cathedral with its own world/rendering ownership.
            // Never inject the legacy sanctuary accents into that newer route.
            if (authoredWorld.Find("Mindforge_White_Cathedral_V24") != null)
            {
                Debug.Log("[LivingWorld:V31] Latest cathedral detected: legacy preview intentionally disabled.");
                enabled = false;
                return;
            }

            // Create an inactive receiver so component OnEnable cannot bind UDP
            // before the user's saved or explicit opt-in is applied.
            _connectionRoot = new GameObject("LivingWorld_V31_Inactive_UDP");
            _connectionRoot.SetActive(false);
            _connectionRoot.transform.SetParent(transform, false);
            _receiver = _connectionRoot.AddComponent<LivingWorldPassiveReceiver>();

            // Dedicated offscreen/side-wall ornament only. We do not search for
            // or alter existing arena, Guardian, boss or SSVEP renderers.
            _visualRoot = new GameObject("LivingWorld_V31_Ornament_Only");
            _visualRoot.SetActive(false);
            // Inherit the authored world's active state, including pre-calibration
            // arena deactivation, so decorative pieces cannot float by themselves.
            _visualRoot.transform.SetParent(authoredWorld, false);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                Debug.LogWarning("[LivingWorld:V31] Unlit/Color unavailable; feature disabled.");
                enabled = false;
                return;
            }
            _decorativeMaterial = new Material(shader) { name = "LivingWorld_V31_Dedicated_Unlit" };
            if (_decorativeMaterial.HasProperty("_Color"))
                _decorativeMaterial.SetColor("_Color", new Color(.13f, .22f, .27f, 1f));

            for (int i = 0; i < SanctuaryAccents.Length; i++)
            {
                GameObject ornament = new GameObject("PassiveSanctumEdge_" + i.ToString("00"));
                ornament.transform.SetParent(_visualRoot.transform, false);
                ornament.transform.position = SanctuaryAccents[i];
                LineRenderer line = ornament.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.positionCount = 2;
                line.SetPosition(0, Vector3.zero);
                line.SetPosition(1, new Vector3(0f, 1.0f, 0f));
                line.startWidth = .055f;
                line.endWidth = .035f;
                line.numCapVertices = 0;
                line.receiveShadows = false;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.sharedMaterial = _decorativeMaterial;
                line.startColor = Color.white;
                line.endColor = Color.white;
                ornament.AddComponent<LivingWorldReactiveSurface>().Configure(_receiver, this);
                // Note: LineRenderer has no Collider, Rigidbody or gameplay layer.
            }

            _intensity = Mathf.Clamp(PlayerPrefs.GetFloat(IntensityKey, .16f), 0f, MaximumIntensity);
            _reducedVisuals = PlayerPrefs.GetInt(ReducedKey, 1) != 0;
            SetOptIn(PlayerPrefs.GetInt(EnabledKey, 0) == 1, save: false);
        }

        private Transform FindAuthoredV11World()
        {
            // The systems kernel leaves the arena inactive until calibration.
            // GameObject.Find would miss its authored children at scene startup.
            UnityEngine.SceneManagement.Scene scene = gameObject.scene;
            if (!scene.IsValid()) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] all = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                    if (all[i] != null && all[i].name == AuthoredWorldRootName)
                        return all[i];
            }
            return null;
        }

        private void Update()
        {
            float dt = Mathf.Clamp(Time.unscaledDeltaTime, 0f, .1f);
            _frameSamplesMs[_writeIndex] = dt * 1000f;
            _writeIndex = (_writeIndex + 1) % _frameSamplesMs.Length;
            _frameCount = Mathf.Min(_frameCount + 1, _frameSamplesMs.Length);
            if (Time.unscaledTime >= _nextStatsTime)
            {
                _nextStatsTime = Time.unscaledTime + 1f;
                UpdateStats();
            }
            bool quiet = InNeuralEvidenceWindow();
            if (quiet) _menuVisible = false;
            else if (Input.GetKeyDown(KeyCode.F10)) _menuVisible = !_menuVisible;
        }

        private bool InNeuralEvidenceWindow()
        {
            if (Time.unscaledTime >= _nextResolveTime && (_wisp == null || _calibration == null))
            {
                _nextResolveTime = Time.unscaledTime + 1f;
                if (_wisp == null) _wisp = FindObjectOfType<SoulWispController>(true);
                if (_calibration == null) _calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
            }
            // Fail quiet during unresolved scene initialization, including GUI.
            return _wisp == null || _calibration == null ||
                   _calibration.CalibrationInProgress ||
                   _wisp.CalibrationStimuliActive || _wisp.ResonanceWindowActive;
        }

        private void OnGUI()
        {
            if (!_menuVisible || InNeuralEvidenceWindow()) return;
            float width = Mathf.Min(305f, Screen.width - 24f);
            if (width < 230f) return;
            GUILayout.BeginArea(new Rect(Screen.width - width - 12f, 16f, width, 247f), GUI.skin.box);
            GUILayout.Label("Living World · Passive BCI");
            GUILayout.Label("Visuals only. F10 hides this menu.");
            bool proposed = GUILayout.Toggle(_enabledByPlayer, "Enable passive scenery");
            if (proposed != _enabledByPlayer) SetOptIn(proposed);
            GUILayout.Label("Intensity: " + Mathf.RoundToInt(_intensity * 100f) + "% (max 30%)");
            float nextIntensity = GUILayout.HorizontalSlider(_intensity, 0f, MaximumIntensity);
            if (Mathf.Abs(nextIntensity - _intensity) > .001f)
            {
                _intensity = Mathf.Clamp(nextIntensity, 0f, MaximumIntensity);
                PlayerPrefs.SetFloat(IntensityKey, _intensity);
            }
            bool reduced = GUILayout.Toggle(_reducedVisuals, "Reduced visuals (recommended)");
            if (reduced != _reducedVisuals)
            {
                _reducedVisuals = reduced;
                PlayerPrefs.SetInt(ReducedKey, reduced ? 1 : 0);
            }
            GUILayout.Label("Status: " + (_enabledByPlayer && _receiver != null && _receiver.Fresh
                ? "feature stream / " + _receiver.LastSourceMode
                : "neutral / off or no valid feature"));
            GUILayout.Label("Frame avg/p95: " + _meanFrameMs.ToString("F1") + " / " +
                            _p95FrameMs.ToString("F1") + " ms");
            if (GUILayout.Button("Disable and restore baseline")) SetOptIn(false);
            GUILayout.EndArea();
        }

        public void SetOptIn(bool on, bool save = true)
        {
            // Enabling via PlayerPrefs remains explicitly controlled by the player,
            // not inferred from an EEG value or confidence.
            _enabledByPlayer = on && _receiver != null && _decorativeMaterial != null;
            if (_connectionRoot != null) _connectionRoot.SetActive(_enabledByPlayer);
            if (_visualRoot != null) _visualRoot.SetActive(_enabledByPlayer);
            if (save)
            {
                PlayerPrefs.SetInt(EnabledKey, _enabledByPlayer ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        private void UpdateStats()
        {
            if (_frameCount < 3) return;
            float sum = 0f;
            for (int i = 0; i < _frameCount; i++)
            {
                float value = _frameSamplesMs[i];
                _scratch[i] = value;
                sum += value;
            }
            Array.Sort(_scratch, 0, _frameCount);
            _meanFrameMs = sum / _frameCount;
            _p95FrameMs = _scratch[Mathf.Clamp(Mathf.CeilToInt(.95f * _frameCount) - 1, 0, _frameCount - 1)];
        }

        private void OnDisable()
        {
            _menuVisible = false;
            if (_connectionRoot != null) _connectionRoot.SetActive(false);
            if (_visualRoot != null) _visualRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_decorativeMaterial != null) Destroy(_decorativeMaterial);
        }
    }
}
