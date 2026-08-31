using UnityEngine;
using Mindforge.Combat;
using Mindforge.Neural;

namespace Mindforge.SoulWisp
{
    /// <summary>
    /// Minimal presentation for the resonance ritual. It intentionally never displays raw
    /// decoder scores. Ordinary players see only actionable state: arm, hold, resolve, or
    /// graceful uncertainty. The coded cores themselves carry Sight/Guard color identity.
    /// </summary>
    [RequireComponent(typeof(WispResonanceWindow))]
    public sealed class WispResonanceHud : MonoBehaviour
    {
        [SerializeField] private WispResonanceWindow window;
        [SerializeField] private SoulWispController wisp;
        [SerializeField] private GuardianControlProfileV1 controls;

        private GUIStyle _center;
        private GUIStyle _option;
        private GUIStyle _small;

        private static readonly Color Sight = new Color(0.30f, 0.68f, 1f, 0.98f);
        private static readonly Color Guard = new Color(0.30f, 1f, 0.62f, 0.98f);
        private static readonly Color Neutral = new Color(0.86f, 0.82f, 1f, 0.96f);

        private void Awake() => Resolve();

        private void Resolve()
        {
            if (window == null) window = GetComponent<WispResonanceWindow>();
            if (wisp == null) wisp = GetComponent<SoulWispController>();
            if (controls == null) controls = GuardianControlProfileV1.ResolveOrCreate();
        }

        private void OnGUI()
        {
            if (window == null || wisp == null || controls == null) Resolve();
            if (window == null || wisp == null || controls == null) return;
            EnsureStyles();

            string key = controls.Label(GuardianControlAction.ChannelWisp);
            if (window.State == WispResonanceState.Idle)
            {
                if (window.CanArm) DrawArmPrompt(key);
                return;
            }

            float scale = Mathf.Clamp(Screen.height / 1080f, 0.78f, 1.18f);
            float panelWidth = Mathf.Min(Screen.width * 0.58f, 650f * scale);
            float panelHeight = 66f * scale;
            Rect panel = new Rect((Screen.width - panelWidth) * 0.5f, Screen.height * 0.70f, panelWidth, panelHeight);
            Fill(panel, new Color(0.015f, 0.020f, 0.034f, 0.74f));
            Stroke(panel, new Color(0.78f, 0.73f, 1f, 0.28f), Mathf.Max(1f, scale));

            switch (window.State)
            {
                case WispResonanceState.Priming:
                    DrawCentered(panel, "WISP ATTUNING", Neutral);
                    DrawProgress(panel, window.StateProgress, Neutral);
                    break;
                case WispResonanceState.Listening:
                    DrawListening(panel, key);
                    DrawProgress(panel, window.StateProgress, Neutral);
                    break;
                case WispResonanceState.Resolved:
                    DrawResolved(panel);
                    break;
                case WispResonanceState.Abstained:
                    DrawCentered(panel, "SIGNAL UNCLEAR  ·  NO AURA SPENT", Neutral);
                    break;
                case WispResonanceState.Cooldown:
                    DrawCentered(panel, "WISP RECOVERING", new Color(0.72f, 0.72f, 0.82f, 0.86f));
                    DrawProgress(panel, window.StateProgress, new Color(0.72f, 0.72f, 0.82f, 0.72f));
                    break;
            }
        }

        private void DrawArmPrompt(string key)
        {
            const float width = 220f;
            const float height = 24f;
            Rect r = new Rect(Screen.width - width - 24f, Screen.height - height - 54f, width, height);
            Fill(r, new Color(0.015f, 0.020f, 0.034f, 0.58f));
            Color before = GUI.color;
            GUI.color = Neutral;
            GUI.Label(r, key + "  HOLD · CHANNEL WISP", _small);
            GUI.color = before;
        }

        private void DrawListening(Rect panel, string key)
        {
            float inner = panel.width * 0.42f;
            Rect left = new Rect(panel.center.x - inner - 12f, panel.y + 8f, inner, 28f);
            Rect right = new Rect(panel.center.x + 12f, panel.y + 8f, inner, 28f);
            Color before = GUI.color;
            GUI.color = Sight;
            GUI.Label(left, "SIGHT", _option);
            GUI.color = Guard;
            GUI.Label(right, "GUARD", _option);
            GUI.color = Neutral;
            GUI.Label(new Rect(panel.x, panel.y + 32f, panel.width, 20f), "HOLD " + key + "  ·  LET THE WISP RESOLVE", _small);
#if UNITY_EDITOR
            GUI.Label(new Rect(panel.x, panel.y - 19f, panel.width, 18f), "EDITOR SIM  ·  1 SIGHT   2 GUARD   0 ABSTAIN", _small);
#endif
            GUI.color = before;
        }

        private void DrawResolved(Rect panel)
        {
            bool sight = window.LastResolvedTarget == AuraTarget.Sight;
            DrawCentered(panel, sight ? "SIGHT RESONATES" : "GUARD RESONATES", sight ? Sight : Guard);
        }

        private void DrawCentered(Rect panel, string text, Color color)
        {
            Color before = GUI.color;
            GUI.color = color;
            GUI.Label(new Rect(panel.x + 8f, panel.y + 12f, panel.width - 16f, 36f), text, _center);
            GUI.color = before;
        }

        private static void DrawProgress(Rect panel, float progress, Color color)
        {
            Rect baseRect = new Rect(panel.x + 12f, panel.yMax - 8f, panel.width - 24f, 3f);
            Fill(baseRect, new Color(1f, 1f, 1f, 0.08f));
            Rect fill = baseRect;
            fill.width *= Mathf.Clamp01(progress);
            Fill(fill, new Color(color.r, color.g, color.b, 0.76f));
        }

        private void EnsureStyles()
        {
            if (_center != null) return;
            _center = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 19,
                fontStyle = FontStyle.Bold,
            };
            _option = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
            };
            _small = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
            };
        }

        private static void Fill(Rect rect, Color color)
        {
            Color before = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = before;
        }

        private static void Stroke(Rect rect, Color color, float thickness)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
            Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }
    }
}
