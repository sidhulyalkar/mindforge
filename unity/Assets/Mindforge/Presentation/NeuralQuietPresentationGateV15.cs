using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mindforge.Calibration;
using Mindforge.SoulWisp;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Separates cinematic ornament from the retinal stimulus field.
    ///
    /// Decorative emissive renderers are hidden before/during calibration and as soon as
    /// a player arms a Wisp resonance window. The actual SightVepCore and GuardVepCore are
    /// explicitly excluded. Renderer enable states are restored afterwards, so this owns
    /// presentation only and never mutates stimulus timing, GameObject activity, collision,
    /// target geometry, or gameplay authority.
    ///
    /// V0.15 also disables the earlier ambient light-breathing component. Slow geometric
    /// ring rotation is retained outside neural windows, but global scene luminance stays
    /// constant so Wisp onset does not coincide with an unnecessary light-pulse transition.
    /// </summary>
    [DefaultExecutionOrder(-70)]
    public sealed class NeuralQuietPresentationGateV15 : MonoBehaviour
    {
        private const string CompetitionSceneName = "Mindforge_Competition";
        private const string RootName = "Mindforge_Neural_Quiet_Presentation_V15";
        private const float DecorativeRotationDegreesPerSecond = 5.5f;

        private static readonly string[] QuietTokens =
        {
            "WispHalo",
            "SanctumSignalRing",
            "SanctumPortalRing",
            "SignalPylonCore",
            "SanctumInlay",
            "ArenaRune",
            "ArenaOuterSignalRing",
            "ArenaFractureCrown",
            "FractureSpireSeam",
            "FracturedSignalHalo",
        };

        private static readonly string[] RotationTokens =
        {
            "WispHalo",
            "SanctumSignalRing",
            "ArenaOuterSignalRing",
            "ArenaFractureCrown",
            "FracturedSignalHalo",
        };

        private AwakeningCalibrationDirector _calibration;
        private SoulWispController _wisp;
        private readonly List<Renderer> _quietRenderers = new List<Renderer>();
        private readonly List<bool> _baselineEnabled = new List<bool>();
        private readonly List<Transform> _rotators = new List<Transform>();
        private bool _resolved;
        private bool _subscribed;
        private bool _lastQuiet;

        public int SuppressedRendererCount => _quietRenderers.Count;
        public bool NeuralQuietActive => _lastQuiet;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.name, CompetitionSceneName, StringComparison.Ordinal))
                return;
            if (FindSceneObject(RootName) != null) return;

            new GameObject(RootName).AddComponent<NeuralQuietPresentationGateV15>();
        }

        private IEnumerator Start()
        {
            // Other AfterSceneLoad installers, including the V0.15 environment builder,
            // complete before Start. One frame also keeps this robust if execution order
            // changes while the scene is being reconstructed in development builds.
            yield return null;
            ResolveAndCache();
            Apply(CurrentQuietState());
        }

        private void OnDestroy()
        {
            if (_subscribed && _calibration != null)
                _calibration.CalibrationStageChanged -= OnCalibrationStageChanged;
        }

        private void Update()
        {
            if (!_resolved)
            {
                ResolveAndCache();
                if (!_resolved) return;
            }

            bool quiet = CurrentQuietState();
            if (quiet != _lastQuiet) Apply(quiet);

            if (!quiet)
            {
                float delta = Time.unscaledDeltaTime * DecorativeRotationDegreesPerSecond;
                for (int i = 0; i < _rotators.Count; i++)
                {
                    Transform rotator = _rotators[i];
                    if (rotator == null) continue;
                    float direction = i % 2 == 0 ? 1f : -1f;
                    rotator.Rotate(0f, 0f, delta * direction, Space.Self);
                }
            }
        }

        private void OnCalibrationStageChanged(string stage)
        {
            if (!_resolved || string.IsNullOrEmpty(stage)) return;

            // AwakeningCalibrationDirector emits the stage event synchronously before
            // the baseline/coded begin marker, so ornament is already hidden when the
            // labelled EEG epoch opens rather than disappearing one rendered frame late.
            if (stage == "baseline" || stage == "sight" || stage == "guard" || stage == "finalizing")
            {
                Apply(true);
                return;
            }

            if (stage == "ready" || stage == "failed" || stage == "controller_only")
                Apply(CurrentQuietState());
        }

        private bool CurrentQuietState()
        {
            return (_calibration != null && _calibration.CalibrationInProgress) ||
                   (_wisp != null && (_wisp.CalibrationStimuliActive || _wisp.ResonanceWindowActive));
        }

        private void ResolveAndCache()
        {
            _calibration = UnityEngine.Object.FindObjectOfType<AwakeningCalibrationDirector>(true);
            _wisp = UnityEngine.Object.FindObjectOfType<SoulWispController>(true);
            if (_calibration == null || _wisp == null) return;

            if (!_subscribed)
            {
                _calibration.CalibrationStageChanged += OnCalibrationStageChanged;
                _subscribed = true;
            }

            // The environment builder's original component modulated accent-light intensity.
            // Keep all scene lights constant for EEG demos; this gate owns only safe geometric
            // rotation outside evidence windows.
            NeuralQuietAmbientMotionV15 ambient = UnityEngine.Object.FindObjectOfType<NeuralQuietAmbientMotionV15>(true);
            if (ambient != null) ambient.enabled = false;

            _quietRenderers.Clear();
            _baselineEnabled.Clear();
            _rotators.Clear();

            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null) continue;
                    string objectName = renderer.gameObject.name;

                    // This opaque placeholder shell wrapped the luminous boss core and read
                    // as a black ball. The collision/vitals live on the original boss root,
                    // so disabling this presentation renderer changes no combat authority.
                    if (string.Equals(objectName, "FracturedSignalCage", StringComparison.Ordinal))
                    {
                        renderer.enabled = false;
                        continue;
                    }

                    if (string.Equals(objectName, "SightVepCore", StringComparison.Ordinal) ||
                        string.Equals(objectName, "GuardVepCore", StringComparison.Ordinal))
                        continue;

                    if (ShouldRotate(objectName)) _rotators.Add(renderer.transform);
                    if (!ShouldSuppress(objectName)) continue;
                    _quietRenderers.Add(renderer);
                    _baselineEnabled.Add(renderer.enabled);
                }
            }

            _resolved = true;
            Debug.Log($"[Mindforge:NeuralQuietV15] Isolated coded visual field; {_quietRenderers.Count} decorative renderers are suppressed during EEG evidence and scene-light luminance is constant.");
        }

        private static bool ShouldSuppress(string objectName)
        {
            for (int i = 0; i < QuietTokens.Length; i++)
                if (objectName.IndexOf(QuietTokens[i], StringComparison.Ordinal) >= 0)
                    return true;
            return false;
        }

        private static bool ShouldRotate(string objectName)
        {
            for (int i = 0; i < RotationTokens.Length; i++)
                if (objectName.IndexOf(RotationTokens[i], StringComparison.Ordinal) >= 0)
                    return true;
            return false;
        }

        private void Apply(bool quiet)
        {
            for (int i = 0; i < _quietRenderers.Count; i++)
            {
                Renderer renderer = _quietRenderers[i];
                if (renderer == null) continue;
                renderer.enabled = quiet ? false : _baselineEnabled[i];
            }
            _lastQuiet = quiet;
        }

        private static GameObject FindSceneObject(string name)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                    if (string.Equals(transforms[i].name, name, StringComparison.Ordinal))
                        return transforms[i].gameObject;
            }
            return null;
        }
    }
}