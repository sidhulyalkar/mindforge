using UnityEngine;
using Mindforge.Calibration;

namespace Mindforge.Journey
{
    /// <summary>
    /// Minimal non-authoritative journey HUD. It exposes objective/progression state
    /// without issuing encounter, combat, target-lock or neural commands.
    /// </summary>
    public sealed class FirstJourneyHud : MonoBehaviour
    {
        [SerializeField] private FirstJourneyDirector journey;
        [SerializeField] private AwakeningCalibrationDirector calibration;
        [SerializeField] private float stageBannerSeconds = 2.2f;

        private string _banner;
        private double _bannerUntil;
        private GUIStyle _objectiveStyle;
        private GUIStyle _smallStyle;
        private GUIStyle _bannerStyle;

        public void ConfigureRuntime(FirstJourneyDirector director, AwakeningCalibrationDirector calibrationDirector)
        {
            journey = director;
            calibration = calibrationDirector;
        }

        private void OnEnable()
        {
            Resolve();
            Subscribe();
        }

        private void OnDisable() => Unsubscribe();

        private void Update()
        {
            if (journey == null)
            {
                Unsubscribe();
                Resolve();
                Subscribe();
            }
        }

        private void Resolve()
        {
            if (journey == null) journey = FindObjectOfType<FirstJourneyDirector>(true);
            if (calibration == null) calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
        }

        private void Subscribe()
        {
            if (journey == null) return;
            journey.StageStarted -= OnStageStarted;
            journey.StageCleared -= OnStageCleared;
            journey.BossStarted -= OnBossStarted;
            journey.JourneyCompleted -= OnCompleted;
            journey.StageStarted += OnStageStarted;
            journey.StageCleared += OnStageCleared;
            journey.BossStarted += OnBossStarted;
            journey.JourneyCompleted += OnCompleted;
        }

        private void Unsubscribe()
        {
            if (journey == null) return;
            journey.StageStarted -= OnStageStarted;
            journey.StageCleared -= OnStageCleared;
            journey.BossStarted -= OnBossStarted;
            journey.JourneyCompleted -= OnCompleted;
        }

        private void OnStageStarted(int index, string title, string lesson)
            => ShowBanner(title, stageBannerSeconds);

        private void OnStageCleared(int index, string id)
            => ShowBanner("PATH OPEN", 1.15f);

        private void OnBossStarted() => ShowBanner("THE FRACTURED SIGNAL", 2.4f);
        private void OnCompleted() => ShowBanner("SIGNAL QUIET", 3.0f);

        private void ShowBanner(string value, float seconds)
        {
            _banner = value;
            _bannerUntil = Time.realtimeSinceStartupAsDouble + Mathf.Max(0.1f, seconds);
        }

        private void OnGUI()
        {
            if (journey == null) return;
            EnsureStyles();

            const float left = 18f;
            const float top = 18f;
            float width = Mathf.Min(520f, Screen.width * 0.46f);
            GUI.Box(new Rect(left, top, width, 66f), string.Empty);
            GUI.Label(new Rect(left + 14f, top + 8f, width - 28f, 24f),
                string.IsNullOrWhiteSpace(journey.CurrentObjective) ? "FOLLOW THE SIGNAL" : journey.CurrentObjective,
                _objectiveStyle);

            string inputHint = journey.BossActive
                ? "T lock · WASD strafe · Space dodge · F sword · RMB/E shield"
                : "T lock · ←/→ or wheel cycle · WASD move · Space dodge";
            GUI.Label(new Rect(left + 14f, top + 36f, width - 28f, 20f), inputHint, _smallStyle);

            if (calibration != null && calibration.ControllerOnlyQualificationActive)
            {
                GUI.Label(new Rect(left, top + 70f, width, 20f),
                    "CONTROLLER-ONLY PREVIEW · neural authority disabled",
                    _smallStyle);
            }

            if (!string.IsNullOrEmpty(_banner) && Time.realtimeSinceStartupAsDouble < _bannerUntil)
            {
                float bannerWidth = Mathf.Min(600f, Screen.width - 80f);
                GUI.Box(new Rect((Screen.width - bannerWidth) * 0.5f, 100f, bannerWidth, 48f), string.Empty);
                GUI.Label(new Rect((Screen.width - bannerWidth) * 0.5f, 100f, bannerWidth, 48f), _banner, _bannerStyle);
            }
        }

        private void EnsureStyles()
        {
            if (_objectiveStyle == null)
            {
                _objectiveStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = true,
                };
            }
            if (_smallStyle == null)
            {
                _smallStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleLeft,
                };
            }
            if (_bannerStyle == null)
            {
                _bannerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 21,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };
            }
        }
    }
}
