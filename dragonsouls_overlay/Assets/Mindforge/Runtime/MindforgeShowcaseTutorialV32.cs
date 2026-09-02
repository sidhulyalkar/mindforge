using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Presentation-only chapter prompt layer. It listens to the showcase flow and
    /// creates a small non-interactive screen-space label. It never consumes input,
    /// changes time scale, or mutates player/combat state.
    /// </summary>
    [DefaultExecutionOrder(990)]
    [DisallowMultipleComponent]
    public sealed class MindforgeShowcaseTutorialV32 : MonoBehaviour
    {
        [SerializeField] private float defaultHoldSeconds = 4.2f;
        [SerializeField] private float fadeSeconds = 0.45f;

        private MindforgeShowcaseFlowV32 _flow;
        private GameObject _root;
        private CanvasGroup _group;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _body;
        private float _visibleUntil;
        private float _fadeStart;

        public bool Installed { get; private set; }
        public string CurrentPromptTitle => _title == null ? string.Empty : _title.text;

        private void Start()
        {
            BuildUi();
            _flow = FindObjectOfType<MindforgeShowcaseFlowV32>(true);
            if (_flow != null)
            {
                _flow.StageChanged += HandleStageChanged;
                _flow.MilestoneObserved += HandleMilestoneObserved;
                ShowStage(_flow.CurrentStage);
            }
            Installed = _root != null && _title != null && _body != null;
        }

        private void OnDestroy()
        {
            if (_flow != null)
            {
                _flow.StageChanged -= HandleStageChanged;
                _flow.MilestoneObserved -= HandleMilestoneObserved;
            }
            if (_root != null) Destroy(_root);
        }

        private void Update()
        {
            if (_group == null) return;
            float now = Time.unscaledTime;
            if (now <= _visibleUntil)
            {
                _group.alpha = Mathf.MoveTowards(_group.alpha, 1f, Time.unscaledDeltaTime / Mathf.Max(0.05f, fadeSeconds));
                return;
            }

            if (_fadeStart <= 0f) _fadeStart = now;
            _group.alpha = Mathf.MoveTowards(_group.alpha, 0f, Time.unscaledDeltaTime / Mathf.Max(0.05f, fadeSeconds));
        }

        private void HandleStageChanged(MindforgeShowcaseStageV32 stage)
        {
            ShowStage(stage);
        }

        private void HandleMilestoneObserved(MindforgeShowcaseMilestoneV32 milestone)
        {
            if (milestone == MindforgeShowcaseMilestoneV32.FirstSwordHit &&
                _flow != null && _flow.CurrentStage == MindforgeShowcaseStageV32.BladeTraining)
            {
                Show("AETHERBLADE", "CONTACT CONFIRMED  •  KEEP MOVING", 2.2f);
            }
        }

        private void ShowStage(MindforgeShowcaseStageV32 stage)
        {
            switch (stage)
            {
                case MindforgeShowcaseStageV32.Awakening:
                    Show("AWAKEN", "WASD  MOVE    •    MOUSE  LOOK", 5f);
                    break;
                case MindforgeShowcaseStageV32.MemoryForge:
                    Show("MEMORY FORGE", "FOLLOW THE CYAN SIGNAL", defaultHoldSeconds);
                    break;
                case MindforgeShowcaseStageV32.BladeTraining:
                    Show("AETHERBLADE", "LMB  LIGHT COMBO    •    RMB  HEAVY", 5f);
                    break;
                case MindforgeShowcaseStageV32.FirstEncounter:
                    Show("FIRST FRACTURE", "MMB  LOCK TARGET    •    LEFT ALT  ROLL", 5f);
                    break;
                case MindforgeShowcaseStageV32.BciReveal:
                    Show("NEURAL ORB ONLINE", "SIGHT 8 Hz    •    GUARD 10 Hz    •    CONCORD 12 Hz    •    B PAUSES STIMULUS", 6.5f);
                    break;
                case MindforgeShowcaseStageV32.SightPuzzle:
                    Show("SIGHT", "READ THE SIGNAL. FIND WHAT THE WORLD IS HIDING.", defaultHoldSeconds);
                    break;
                case MindforgeShowcaseStageV32.Traversal:
                    Show("DESCENT", "EXPLORE THE VERTICAL ROUTE. SECRETS ARE OPTIONAL.", defaultHoldSeconds);
                    break;
                case MindforgeShowcaseStageV32.EliteEncounter:
                    Show("BROKEN CHOIR", "CONTROL SPACE. SWITCH TARGETS. PUNISH RECOVERY.", defaultHoldSeconds);
                    break;
                case MindforgeShowcaseStageV32.BossApproach:
                    Show("SIGNAL COLLAPSE", "THE FRACTURED SIGNAL IS AHEAD.", 5.5f);
                    break;
                case MindforgeShowcaseStageV32.BossFight:
                    Show("THE FRACTURED SIGNAL", "LEARN  •  ADAPT  •  SYNCHRONIZE", 5.5f);
                    break;
                case MindforgeShowcaseStageV32.WorldReveal:
                    Show("THE FORGE OPENS", "THE SANCTUM WAS ONLY THE FIRST REGION.", 6f);
                    break;
            }
        }

        private void Show(string title, string body, float holdSeconds)
        {
            if (_title == null || _body == null) return;
            _title.text = title;
            _body.text = body;
            _group.alpha = 0f;
            _fadeStart = 0f;
            _visibleUntil = Time.unscaledTime + Mathf.Max(1f, holdSeconds);
        }

        private void BuildUi()
        {
            _root = new GameObject("Mindforge_Showcase_Tutorial_UI_V32");
            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 280;
            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            _group = _root.AddComponent<CanvasGroup>();
            _group.interactable = false;
            _group.blocksRaycasts = false;

            GameObject panel = new GameObject("PromptPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(_root.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 72f);
            panelRect.sizeDelta = new Vector2(760f, 112f);
            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.025f, 0.035f, 0.050f, 0.70f);
            panelImage.raycastTarget = false;

            _title = CreateText(panel.transform, "PromptTitle", new Vector2(0f, 26f), new Vector2(710f, 38f), 26f);
            _title.fontStyle = FontStyles.Bold;
            _title.color = new Color(0.74f, 0.94f, 1f, 1f);
            _body = CreateText(panel.transform, "PromptBody", new Vector2(0f, -20f), new Vector2(710f, 42f), 17f);
            _body.color = new Color(0.82f, 0.86f, 0.91f, 1f);
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            float fontSize)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            return text;
        }
    }
}
