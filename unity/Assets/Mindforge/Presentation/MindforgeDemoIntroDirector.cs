using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Mindforge.Calibration;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Demo-facing opening sequence for the competition scene.
    ///
    /// Camera motion, title animation and decorative presentation are allowed only before
    /// the calibration gate is released. The director parks the camera at a fixed pose,
    /// clears its transient UI, and only then calls SetIntroReady(true). A successful
    /// calibration receives a separate arena reveal after the coded stimuli are already off.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public sealed class MindforgeDemoIntroDirector : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float titleHoldSeconds = 0.85f;
        [SerializeField] private float approachSeconds = 2.15f;
        [SerializeField] private float instructionSeconds = 2.80f;
        [SerializeField] private float calibrationParkSeconds = 1.10f;
        [SerializeField] private float arenaRevealSeconds = 1.75f;
        [SerializeField] private float controlsHoldSeconds = 8.0f;
        [SerializeField] private KeyCode skipKey = KeyCode.Space;
        [SerializeField] private KeyCode researchHudKey = KeyCode.F7;

        private Camera _camera;
        private Transform _cameraRig;
        private AwakeningCalibrationDirector _calibration;
        private GuardianCombatInput _input;
        private FracturedSignalDirector _boss;
        private CanvasGroup _titleOverlay;
        private CanvasGroup _instructionPanel;
        private CanvasGroup _controlRibbon;
        private CanvasGroup _researchHud;
        private Text _titleText;
        private Text _subtitleText;
        private Text _instructionText;
        private Text _phaseText;
        private Text _controlsText;
        private Text _calibrationStatus;
        private Transform _introWidePose;
        private Transform _wispPose;
        private Transform _calibrationPose;
        private Transform _arenaRevealPose;
        private Transform _gameplayPose;

        private bool _configured;
        private bool _introRunning;
        private bool _skipRequested;
        private bool _arenaRevealRunning;
        private bool _researchVisible;

        public bool IntroComplete { get; private set; }

        public void Configure(
            Camera camera,
            Transform cameraRig,
            AwakeningCalibrationDirector calibration,
            GuardianCombatInput input,
            FracturedSignalDirector boss,
            CanvasGroup titleOverlay,
            CanvasGroup instructionPanel,
            CanvasGroup controlRibbon,
            CanvasGroup researchHud,
            Text titleText,
            Text subtitleText,
            Text instructionText,
            Text phaseText,
            Text controlsText,
            Text calibrationStatus,
            Transform introWidePose,
            Transform wispPose,
            Transform calibrationPose,
            Transform arenaRevealPose,
            Transform gameplayPose)
        {
            _camera = camera;
            _cameraRig = cameraRig;
            _calibration = calibration;
            // The shared calibration director defaults to no cinematic dependency.
            // Only this competition-demo installer opts into the V0.15 intro gate.
            _calibration?.ConfigureIntroGate(true);
            _calibration?.SetIntroReady(false);
            _input = input;
            _boss = boss;
            _titleOverlay = titleOverlay;
            _instructionPanel = instructionPanel;
            _controlRibbon = controlRibbon;
            _researchHud = researchHud;
            _titleText = titleText;
            _subtitleText = subtitleText;
            _instructionText = instructionText;
            _phaseText = phaseText;
            _controlsText = controlsText;
            _calibrationStatus = calibrationStatus;
            _introWidePose = introWidePose;
            _wispPose = wispPose;
            _calibrationPose = calibrationPose;
            _arenaRevealPose = arenaRevealPose;
            _gameplayPose = gameplayPose;
            _configured = true;
        }

        private void Start()
        {
            if (!_configured)
            {
                Debug.LogError("[Mindforge:DemoIntro] Missing runtime configuration. Intro gate remains closed.");
                _calibration?.SetIntroReady(false);
                return;
            }

            _calibration.SetIntroReady(false);
            _calibration.CalibrationStageChanged += OnCalibrationStageChanged;
            SetResearchHud(false);
            SetGroup(_instructionPanel, 0f, false);
            SetGroup(_controlRibbon, 0f, false);
            StartCoroutine(RunIntro());
        }

        private void OnDestroy()
        {
            if (_calibration != null) _calibration.CalibrationStageChanged -= OnCalibrationStageChanged;
        }

        private void Update()
        {
            if (_introRunning && (Input.GetKeyDown(skipKey) || Input.GetKeyDown(KeyCode.Return)))
                _skipRequested = true;

            if (Input.GetKeyDown(researchHudKey))
                SetResearchHud(!_researchVisible);
        }

        private IEnumerator RunIntro()
        {
            _introRunning = true;
            IntroComplete = false;
            _skipRequested = false;
            _input?.SetCombatActionsEnabled(false);
            _boss?.SetExternalPause(true);

            SnapRig(_introWidePose);
            SetTitle("MINDFORGE", "NEURAL COMBAT PROTOTYPE");
            SetGroup(_titleOverlay, 1f, true);
            yield return WaitOrSkip(titleHoldSeconds);

            if (!_skipRequested)
            {
                StartCoroutine(FadeGroup(_titleOverlay, 0f, 1.15f, false));
                yield return TweenRig(_wispPose, approachSeconds);
            }

            if (!_skipRequested)
            {
                _instructionText.text =
                    "HOLD V TO OPEN A NEURAL WINDOW\n" +
                    "LOOK AT BLUE: SIGHT   ·   LOOK AT GREEN: GUARD\n" +
                    "KEEP YOUR GAZE ON YOUR CHOICE · UNCLEAR SIGNALS DO NOTHING";
                _phaseText.text = "HANDS FIGHT · EEG CONFIRMS THE WISP STATE";
                SetGroup(_instructionPanel, 1f, true);
                yield return WaitOrSkip(instructionSeconds);
            }

            SetGroup(_instructionPanel, 0f, false);
            yield return TweenRig(_calibrationPose, _skipRequested ? 0.01f : calibrationParkSeconds);
            SnapRig(_calibrationPose);

            // Give the final camera pose one clean rendered frame before the calibration
            // director may start baseline or periodic stimulation.
            yield return new WaitForEndOfFrame();
            yield return new WaitForSecondsRealtime(0.18f);

            SetGroup(_titleOverlay, 0f, false);
            _introRunning = false;
            IntroComplete = true;
            _calibration.SetIntroReady(true);
            _boss?.SetExternalPause(true);
        }

        private void OnCalibrationStageChanged(string stage)
        {
            if (string.IsNullOrEmpty(stage)) return;
            if (_phaseText != null)
            {
                switch (stage)
                {
                    case "baseline": _phaseText.text = "BASELINE · KEEP STILL"; break;
                    case "sight": _phaseText.text = "LOOK AT BLUE · SIGHT"; break;
                    case "guard": _phaseText.text = "LOOK AT GREEN · GUARD"; break;
                    case "finalizing": _phaseText.text = "DECODER CALIBRATING"; break;
                    case "failed": _phaseText.text = "SIGNAL UNCLEAR · RETRY AVAILABLE"; break;
                }
            }

            if ((stage == "ready" || stage == "controller_only") && !_arenaRevealRunning)
                StartCoroutine(RunArenaReveal(stage == "controller_only"));
        }

        private IEnumerator RunArenaReveal(bool controllerOnly)
        {
            _arenaRevealRunning = true;
            _input?.SetCombatActionsEnabled(false);
            _boss?.SetExternalPause(true);
            SetGroup(_instructionPanel, 0f, false);

            SetTitle(controllerOnly ? "CONTROLLER PREVIEW" : "LINK STABLE",
                controllerOnly ? "BCI AUTHORITY DISABLED" : "COMBAT AUTHORITY OPEN");
            SetGroup(_titleOverlay, 0f, true);
            yield return FadeGroup(_titleOverlay, 1f, 0.20f, true);
            SnapRig(_arenaRevealPose);
            yield return new WaitForEndOfFrame();
            yield return FadeGroup(_titleOverlay, 0f, 0.42f, false);
            yield return TweenRig(_gameplayPose, arenaRevealSeconds);
            SnapRig(_gameplayPose);

            if (_calibrationStatus != null) _calibrationStatus.enabled = false;
            if (_controlsText != null)
            {
                _controlsText.text =
                    "WASD MOVE   ·   SPACE JUMP/HOVER   ·   SHIFT/RMB EVADE   ·   F/LMB BLADE\n" +
                    "T TARGET   ·   V NEURAL WINDOW   ·   Q CLEAVE   ·   C COUNTER   ·   R BLOOM";
            }
            SetGroup(_controlRibbon, 1f, true);

            _boss?.SetExternalPause(false);
            _input?.SetCombatActionsEnabled(true);
            yield return new WaitForSecondsRealtime(Mathf.Max(1f, controlsHoldSeconds));
            yield return FadeGroup(_controlRibbon, 0f, 0.65f, false);
            _arenaRevealRunning = false;
        }

        private IEnumerator TweenRig(Transform target, float duration)
        {
            if (_cameraRig == null || target == null) yield break;
            if (duration <= 0.02f)
            {
                SnapRig(target);
                yield break;
            }

            Vector3 startPosition = _cameraRig.position;
            Quaternion startRotation = _cameraRig.rotation;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (_introRunning && _skipRequested) break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                _cameraRig.position = Vector3.Lerp(startPosition, target.position, t);
                _cameraRig.rotation = Quaternion.Slerp(startRotation, target.rotation, t);
                yield return null;
            }
            SnapRig(target);
        }

        private IEnumerator FadeGroup(CanvasGroup group, float target, float duration, bool active)
        {
            if (group == null) yield break;
            if (active) group.gameObject.SetActive(true);
            float start = group.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration)));
                yield return null;
            }
            group.alpha = target;
            // Demo overlays are informational only. They never own input or raycasts.
            group.interactable = false;
            group.blocksRaycasts = false;
            if (!active && target <= 0.001f) group.gameObject.SetActive(false);
        }

        private IEnumerator WaitOrSkip(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds && !_skipRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void SnapRig(Transform target)
        {
            if (_cameraRig == null || target == null) return;
            _cameraRig.position = target.position;
            _cameraRig.rotation = target.rotation;
        }

        private void SetTitle(string title, string subtitle)
        {
            if (_titleText != null) _titleText.text = title;
            if (_subtitleText != null) _subtitleText.text = subtitle;
        }

        private static void SetGroup(CanvasGroup group, float alpha, bool active)
        {
            if (group == null) return;
            group.gameObject.SetActive(active || alpha > 0.001f);
            group.alpha = alpha;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private void SetResearchHud(bool visible)
        {
            _researchVisible = visible;
            if (_researchHud == null) return;
            _researchHud.alpha = visible ? 1f : 0f;
            _researchHud.interactable = false;
            _researchHud.blocksRaycasts = false;
        }
    }
}