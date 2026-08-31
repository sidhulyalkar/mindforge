using System.Collections;
using UnityEngine;
using Mindforge.Calibration;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Short authored introduction for the actual canonical V0.11 scene used by
    /// Mindforge > Latest. This is deliberately separate from the competition-scene V0.15
    /// cinematic, whose world coordinates do not match the clean V0.11 route.
    ///
    /// The calibration presentation gate is closed synchronously after scene load, before
    /// Update can auto-start calibration. Camera motion and title presentation happen only
    /// while that gate is closed. The final pose is submitted for a clean frame before
    /// SetIntroReady(true) permits baseline or coded calibration to begin.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class MindforgeCanonicalIntroV17 : MonoBehaviour
    {
        public const string RootName = "Mindforge_CanonicalIntro_V17";

        [SerializeField] private float titleSeconds = 0.85f;
        [SerializeField] private float approachSeconds = 1.45f;
        [SerializeField] private float instructionSeconds = 1.85f;
        [SerializeField] private float parkSeconds = 0.72f;

        private Camera _camera;
        private GuardianCombatInput _input;
        private FracturedSignalDirector _boss;
        private AwakeningCalibrationDirector _calibration;
        private MindforgeDemoCameraV11 _legacyCamera;
        private Transform _guardian;
        private bool _introRunning;
        private bool _skip;
        private bool _complete;
        private double _phaseStarted;
        private int _phase;
        private GUIStyle _title;
        private GUIStyle _subtitle;
        private GUIStyle _hint;

        public bool IntroComplete => _complete;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<MindforgeDemoV11Marker>(true) == null) return;

            // Close the neural presentation gate immediately. RuntimeInitialize runs before
            // the first Update, so a ready neural service cannot start baseline underneath
            // the cinematic camera move.
            AwakeningCalibrationDirector calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
            if (calibration != null)
            {
                calibration.ConfigureIntroGate(true);
                calibration.SetIntroReady(false);
            }

            if (FindObjectOfType<MindforgeCanonicalIntroV17>(true) != null) return;
            new GameObject(RootName).AddComponent<MindforgeCanonicalIntroV17>();
        }

        private IEnumerator Start()
        {
            for (int frame = 0; frame < 240; frame++)
            {
                if (_camera == null) _camera = Camera.main;
                if (_input == null) _input = FindObjectOfType<GuardianCombatInput>(true);
                if (_boss == null) _boss = FindObjectOfType<FracturedSignalDirector>(true);
                if (_calibration == null) _calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
                if (_input != null && _guardian == null) _guardian = _input.transform;
                if (_camera != null && _legacyCamera == null)
                    _legacyCamera = _camera.transform.root.GetComponent<MindforgeDemoCameraV11>();

                // Waiting for the V0.11 runtime camera also means its initial scene handoff
                // has completed, so this intro can become the only camera writer.
                if (_camera != null && _input != null && _boss != null && _calibration != null &&
                    _guardian != null && _legacyCamera != null)
                    break;
                yield return null;
            }

            if (_camera == null || _input == null || _boss == null || _calibration == null ||
                _guardian == null || _legacyCamera == null)
            {
                Debug.LogError("[Mindforge:V17Intro] Canonical intro could not resolve camera, Guardian, boss, calibration or V0.11 camera. Calibration gate remains closed.");
                yield break;
            }

            _calibration.ConfigureIntroGate(true);
            _calibration.SetIntroReady(false);
            _calibration.CalibrationStageChanged += OnCalibrationStageChanged;

            // Presentation suspension only. The intro never invents qualification or combat.
            _input.SetCombatActionsEnabled(false);
            _boss.SetExternalPause(true);
            _legacyCamera.enabled = false;
            _camera.fieldOfView = 56f;

            yield return RunIntro();
        }

        private IEnumerator RunIntro()
        {
            _introRunning = true;
            _complete = false;
            _skip = false;

            Vector3 guardianPosition = _guardian.position;
            Vector3 widePosition = guardianPosition + new Vector3(-7.6f, 4.6f, -7.4f);
            Vector3 approachPosition = guardianPosition + new Vector3(-3.0f, 3.0f, -5.4f);
            Vector3 parkPosition = guardianPosition + new Vector3(0.7f, 3.35f, -6.9f);
            Vector3 wideLook = guardianPosition + new Vector3(0f, 1.15f, 3.8f);
            Vector3 approachLook = guardianPosition + new Vector3(0f, 1.20f, 2.4f);
            Vector3 parkLook = guardianPosition + new Vector3(0f, 1.10f, 4.8f);

            SetCamera(widePosition, wideLook);
            SetPhase(0);
            yield return WaitOrSkip(titleSeconds);

            SetPhase(1);
            yield return TweenCamera(approachPosition, approachLook, _skip ? 0.01f : approachSeconds);
            if (!_skip) yield return WaitOrSkip(instructionSeconds);

            SetPhase(2);
            yield return TweenCamera(parkPosition, parkLook, _skip ? 0.01f : parkSeconds);
            SetCamera(parkPosition, parkLook);

            // The final static view gets one fully submitted frame plus a small non-periodic
            // guard before baseline is allowed to open. No VEP target is active here.
            yield return new WaitForEndOfFrame();
            yield return new WaitForSecondsRealtime(0.12f);

            _introRunning = false;
            _complete = true;
            _calibration.SetIntroReady(true);

            if (_calibration.ControllerOnlyQualificationActive)
            {
                _boss.SetExternalPause(false);
                _input.SetCombatActionsEnabled(true);
            }

            Debug.Log(
                "[Mindforge:V17Intro] Canonical Memory Forge intro complete. Camera parked before neural gate release; " +
                (_calibration.ControllerOnlyQualificationActive
                    ? "controller-only preview authority restored."
                    : "real calibration may now begin when service and display timing are ready."));
        }

        private void Update()
        {
            if (_introRunning && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
                _skip = true;
        }

        private void OnCalibrationStageChanged(string stage)
        {
            if (!_complete || string.IsNullOrEmpty(stage)) return;
            if (stage == "ready" || stage == "controller_only")
            {
                _boss?.SetExternalPause(false);
                // The calibration director already owns the real ready transition. This call
                // merely mirrors the controller-only intro path and cannot create calibration.
                if (stage == "controller_only") _input?.SetCombatActionsEnabled(true);
            }
        }

        private IEnumerator TweenCamera(Vector3 targetPosition, Vector3 lookPoint, float duration)
        {
            if (_camera == null) yield break;
            if (duration <= 0.02f)
            {
                SetCamera(targetPosition, lookPoint);
                yield break;
            }

            Transform rig = _camera.transform.root;
            Vector3 startPosition = rig.position;
            Quaternion startRotation = rig.rotation;
            Quaternion targetRotation = LookRotation(targetPosition, lookPoint);
            float elapsed = 0f;
            while (elapsed < duration && !_skip)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                t = t * t * (3f - 2f * t);
                rig.position = Vector3.Lerp(startPosition, targetPosition, t);
                rig.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                _camera.fieldOfView = 56f;
                yield return null;
            }
            SetCamera(targetPosition, lookPoint);
        }

        private void SetCamera(Vector3 position, Vector3 lookPoint)
        {
            if (_camera == null) return;
            Transform rig = _camera.transform.root;
            rig.position = position;
            rig.rotation = LookRotation(position, lookPoint);
            _camera.fieldOfView = 56f;
        }

        private static Quaternion LookRotation(Vector3 position, Vector3 lookPoint)
        {
            Vector3 direction = lookPoint - position;
            if (direction.sqrMagnitude < 0.001f) direction = Vector3.forward;
            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private IEnumerator WaitOrSkip(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < Mathf.Max(0f, seconds) && !_skip)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void SetPhase(int phase)
        {
            _phase = phase;
            _phaseStarted = Time.realtimeSinceStartupAsDouble;
        }

        private void OnGUI()
        {
            if (!_introRunning) return;
            EnsureStyles();

            float scale = Mathf.Clamp(Screen.height / 1080f, 0.78f, 1.25f);
            Rect veil = new Rect(0f, 0f, Screen.width, Screen.height);
            Color before = GUI.color;
            GUI.color = new Color(0.01f, 0.015f, 0.025f, _phase == 0 ? 0.24f : 0.12f);
            GUI.DrawTexture(veil, Texture2D.whiteTexture);
            GUI.color = before;

            float elapsed = (float)(Time.realtimeSinceStartupAsDouble - _phaseStarted);
            float fade = Mathf.Clamp01(elapsed / 0.24f);
            GUI.color = new Color(1f, 1f, 1f, fade);

            if (_phase == 0)
            {
                GUI.Label(new Rect(0f, Screen.height * 0.38f, Screen.width, 54f * scale), "MINDFORGE", _title);
                GUI.Label(new Rect(0f, Screen.height * 0.38f + 52f * scale, Screen.width, 30f * scale),
                    "THE FIRST GUARDIAN", _subtitle);
            }
            else if (_phase == 1)
            {
                GUI.Label(new Rect(Screen.width * 0.13f, Screen.height * 0.68f, Screen.width * 0.74f, 34f * scale),
                    "THE WISP READS VISUAL RESONANCE", _subtitle);
                GUI.Label(new Rect(Screen.width * 0.13f, Screen.height * 0.68f + 32f * scale, Screen.width * 0.74f, 28f * scale),
                    "BLUE · SIGHT     GREEN · GUARD     UNCLEAR · NO ACTION", _hint);
            }
            else
            {
                string message = _calibration.ControllerOnlyQualificationActive
                    ? "CONTROLLER PREVIEW · BCI SIMULATED"
                    : "CAMERA STABLE · PREPARING NEURAL CALIBRATION";
                GUI.Label(new Rect(Screen.width * 0.15f, Screen.height * 0.74f, Screen.width * 0.70f, 30f * scale),
                    message, _hint);
            }

            GUI.Label(new Rect(0f, Screen.height - 42f * scale, Screen.width, 24f * scale),
                "SPACE · SKIP", _hint);
            GUI.color = before;
        }

        private void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 36,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.94f, 0.97f, 1f, 0.98f) },
            };
            _subtitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.66f, 0.80f, 0.94f, 0.95f) },
            };
            _hint = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.78f, 0.84f, 0.90f, 0.90f) },
            };
        }

        private void OnDestroy()
        {
            if (_calibration != null) _calibration.CalibrationStageChanged -= OnCalibrationStageChanged;
        }
    }
}
