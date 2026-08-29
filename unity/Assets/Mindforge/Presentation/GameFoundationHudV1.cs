using UnityEngine;
using Mindforge.Telemetry;
using Mindforge.World;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Compact read-only journey surface for the vertical slice. It renders the active quest,
    /// current step, progression currencies and short world-memory discoveries. It owns no
    /// input, combat, encounter, progression, story-state or neural authority.
    /// </summary>
    [DefaultExecutionOrder(1300)]
    public sealed class GameFoundationHudV1 : MonoBehaviour
    {
        [SerializeField] private WorldQuestRuntime quests;
        [SerializeField] private PlayerProgressionLedger progression;
        [SerializeField] private WorldSignalBus signals;
        [SerializeField] private CompetitiveRunObserverV1 runObserver;
        [SerializeField, Min(1f)] private float storyDurationSeconds = 5.0f;

        private string _storyHeading;
        private string _storyLine;
        private float _storyUntil;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _panelStyle;

        public void ConfigureRuntime(
            WorldQuestRuntime questRuntime,
            PlayerProgressionLedger progressionLedger,
            WorldSignalBus signalBus,
            CompetitiveRunObserverV1 observer)
        {
            Unsubscribe();
            quests = questRuntime;
            progression = progressionLedger;
            signals = signalBus;
            runObserver = observer;
            Subscribe();
        }

        private void Awake() => Resolve();

        private void OnEnable()
        {
            Resolve();
            Subscribe();
        }

        private void OnDisable() => Unsubscribe();

        private void Resolve()
        {
            if (quests == null) quests = FindObjectOfType<WorldQuestRuntime>(true);
            if (progression == null) progression = FindObjectOfType<PlayerProgressionLedger>(true);
            if (signals == null) signals = FindObjectOfType<WorldSignalBus>(true);
            if (runObserver == null) runObserver = FindObjectOfType<CompetitiveRunObserverV1>(true);
        }

        private void Subscribe()
        {
            if (signals == null) return;
            signals.SignalPublished -= OnSignal;
            signals.SignalPublished += OnSignal;
        }

        private void Unsubscribe()
        {
            if (signals != null) signals.SignalPublished -= OnSignal;
        }

        private void OnSignal(WorldSignal signal)
        {
            if (signal == null) return;
            if (signal.kind == WorldSignalKind.StoryDiscovered)
            {
                _storyHeading = signal.reason ?? string.Empty;
                _storyLine = signal.string_value ?? string.Empty;
                _storyUntil = Time.unscaledTime + Mathf.Max(1f, storyDurationSeconds);
            }
            else if (signal.kind == WorldSignalKind.QuestCompleted)
            {
                _storyHeading = "JOURNEY COMPLETE";
                _storyLine = signal.reason ?? signal.subject ?? string.Empty;
                _storyUntil = Time.unscaledTime + 3.5f;
            }
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null) return;
            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(12, 12, 9, 9),
            };
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
            };
            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
            };
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawJourneyPanel();
            DrawStoryPulse();
        }

        private void DrawJourneyPanel()
        {
            if (quests == null) return;
            WorldQuestDefinition quest = quests.GetPrimaryActiveQuest();
            if (quest == null) return;
            WorldQuestProgress state = quests.GetProgress(quest.id);
            WorldQuestStepDefinition step = quests.GetCurrentStep(quest.id);

            float width = Mathf.Min(480f, Screen.width - 32f);
            float height = 82f;
            Rect panel = new Rect(16f, Screen.height - height - 16f, width, height);
            GUI.Box(panel, GUIContent.none, _panelStyle);
            GUI.Label(new Rect(panel.x + 12f, panel.y + 8f, width - 24f, 22f), quest.title ?? "JOURNEY", _titleStyle);
            string stepText = step != null ? step.title : "Objective resolved";
            GUI.Label(new Rect(panel.x + 12f, panel.y + 31f, width - 24f, 22f), stepText, _bodyStyle);

            int completed = state != null ? state.completed_steps : 0;
            int total = state != null ? state.total_steps : 0;
            int resonance = progression != null ? progression.Resonance : 0;
            int mastery = progression != null ? progression.Mastery : 0;
            string run = runObserver != null && runObserver.Running ? $"  ·  RUN {runObserver.ElapsedSeconds:0.0}s" : string.Empty;
            GUI.Label(
                new Rect(panel.x + 12f, panel.y + 54f, width - 24f, 20f),
                $"STEP {Mathf.Min(completed + 1, Mathf.Max(1, total))}/{Mathf.Max(1, total)}  ·  RESONANCE {resonance}  ·  MASTERY {mastery}{run}",
                _bodyStyle);
        }

        private void DrawStoryPulse()
        {
            if (Time.unscaledTime >= _storyUntil || string.IsNullOrWhiteSpace(_storyLine)) return;
            float width = Mathf.Min(680f, Screen.width - 48f);
            float x = (Screen.width - width) * 0.5f;
            Rect panel = new Rect(x, 72f, width, 90f);
            GUI.Box(panel, GUIContent.none, _panelStyle);
            GUI.Label(new Rect(panel.x + 16f, panel.y + 10f, width - 32f, 22f), _storyHeading ?? string.Empty, _titleStyle);
            GUI.Label(new Rect(panel.x + 16f, panel.y + 35f, width - 32f, 46f), _storyLine ?? string.Empty, _bodyStyle);
        }
    }
}
