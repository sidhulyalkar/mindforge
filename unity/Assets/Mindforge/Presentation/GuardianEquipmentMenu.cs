using UnityEngine;
using Mindforge.Combat;
using Mindforge.World;

namespace Mindforge.Presentation
{
    /// <summary>
    /// One reliable player-reference screen for build, controls and current objective.
    /// It is descriptive rather than an inventory editor in V0.5; gameplay and neural
    /// authority remain in their existing systems.
    /// </summary>
    public sealed class GuardianEquipmentMenu : MonoBehaviour
    {
        [SerializeField] private GuardianEquipmentLoadout loadout;
        [SerializeField] private GuardianStamina endurance;
        [SerializeField] private GuardianControlProfileV1 controls;
        [SerializeField] private WorldQuestRuntime quests;

        private bool _visible;
        private GUIStyle _title;
        private GUIStyle _subtitle;
        private GUIStyle _section;
        private GUIStyle _item;
        private GUIStyle _body;
        private GUIStyle _muted;
        private GUIStyle _key;

        private static readonly Color Panel = new Color(0.035f, 0.045f, 0.065f, 0.97f);
        private static readonly Color Card = new Color(0.070f, 0.085f, 0.115f, 0.98f);
        private static readonly Color Blue = new Color(0.24f, 0.64f, 1f, 1f);
        private static readonly Color Green = new Color(0.24f, 1f, 0.58f, 1f);
        private static readonly Color Gold = new Color(0.92f, 0.76f, 0.38f, 1f);
        private static readonly Color Text = new Color(0.94f, 0.96f, 1f, 1f);
        private static readonly Color Muted = new Color(0.64f, 0.70f, 0.80f, 1f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<GuardianEquipmentMenu>(true) != null) return;
            new GameObject("MindforgeEquipmentMenu").AddComponent<GuardianEquipmentMenu>();
        }

        public void Configure(GuardianEquipmentLoadout equipment, GuardianStamina staminaBudget)
        {
            loadout = equipment;
            endurance = staminaBudget;
        }

        private void Resolve()
        {
            if (loadout == null) loadout = FindObjectOfType<GuardianEquipmentLoadout>(true);
            if (endurance == null && loadout != null) endurance = loadout.GetComponent<GuardianStamina>();
            if (endurance == null) endurance = FindObjectOfType<GuardianStamina>(true);
            if (controls == null) controls = GuardianControlProfileV1.ResolveOrCreate();
            if (quests == null) quests = FindObjectOfType<WorldQuestRuntime>(true);
        }

        private void Update()
        {
            Resolve();
            if (controls != null && controls.Pressed(GuardianControlAction.Menu))
                _visible = !_visible;
        }

        private void OnGUI()
        {
            Resolve();
            if (!_visible || loadout == null) return;
            EnsureStyles();

            Color before = GUI.color;
            GUI.color = new Color(0.005f, 0.008f, 0.014f, 0.78f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = before;

            float width = Mathf.Min(980f, Screen.width - 72f);
            float height = Mathf.Min(680f, Screen.height - 60f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            Fill(panel, Panel);
            Stroke(panel, new Color(0.28f, 0.40f, 0.62f, 0.75f), 2f);

            float x = panel.x + 28f;
            float y = panel.y + 22f;
            GUI.Label(new Rect(x, y, panel.width - 56f, 38f), "GUARDIAN KIT + CONTROLS", _title);
            GUI.Label(new Rect(x, y + 36f, panel.width - 56f, 24f),
                "Hands own precision · neural focus transforms bounded game state · one contextual interaction button operates the world", _subtitle);
            GUI.Label(new Rect(panel.xMax - 180f, y + 4f, 152f, 24f), Label(GuardianControlAction.Menu, "TAB") + "  CLOSE", _key);
            y += 78f;

            float gap = 18f;
            float leftWidth = (panel.width - 56f - gap) * 0.56f;
            float rightWidth = panel.width - 56f - gap - leftWidth;
            float leftX = x;
            float rightX = x + leftWidth + gap;

            DrawLoadoutCard(new Rect(leftX, y, leftWidth, 118f), "MAIN HAND", loadout.MainHand?.displayName ?? "Unequipped", Blue,
                loadout.MainHand != null
                    ? $"{loadout.MainHand.archetype.ToString().ToUpperInvariant()}   DAMAGE {loadout.MainHand.baseDamage:F0}   BASE REACH {loadout.MainHand.reachMeters:F2}m\n{Label(GuardianControlAction.Blade, "F / LMB")} chains committed swings. An active blade can physically parry hostile projectiles."
                    : "No weapon equipped");
            DrawLoadoutCard(new Rect(leftX, y + 132f, leftWidth, 118f), "DEFENSE", "Endurance Evade", Green,
                $"{Label(GuardianControlAction.EvadeBoost, "SHIFT / RMB")} rolls through ground pressure. Airborne input becomes one air dash per airtime.\nOn a hoverbike the same control becomes boost: same physical vocabulary, context-specific verb.");
            DrawLoadoutCard(new Rect(leftX, y + 264f, leftWidth, 104f), "ARMOR", loadout.Armor?.displayName ?? "Unequipped", Gold,
                loadout.Armor != null
                    ? $"{loadout.Armor.weightClass.ToString().ToUpperInvariant()}   {loadout.Armor.massKg:F1} kg\nArmor contributes physical load while the retired shield contributes no active mass."
                    : "No armor equipped");

            Rect objective = new Rect(leftX, y + 382f, leftWidth, 108f);
            Fill(objective, Card);
            Stroke(objective, new Color(0.20f, 0.78f, 1f, 0.42f), 1f);
            GUI.Label(new Rect(objective.x + 16f, objective.y + 10f, objective.width - 32f, 22f), "CURRENT OBJECTIVE", _section);
            GUI.Label(new Rect(objective.x + 16f, objective.y + 36f, objective.width - 32f, 64f), CurrentObjective(), _body);

            Rect rules = new Rect(rightX, y, rightWidth, 354f);
            Fill(rules, Card);
            Stroke(rules, new Color(0.18f, 0.24f, 0.34f, 1f), 1f);
            GUI.Label(new Rect(rules.x + 16f, rules.y + 14f, rules.width - 32f, 24f), "PLAYER VOCABULARY", _section);
            float ky = rules.y + 48f;
            DrawControl(ref ky, rules.x + 16f, rules.width - 32f, "WASD", "Move relative to camera");
            DrawControl(ref ky, rules.x + 16f, rules.width - 32f, "MOUSE / ARROWS", "Orbit camera");
            DrawControl(ref ky, rules.x + 16f, rules.width - 32f, Label(GuardianControlAction.JumpHover, "SPACE"), "Jump ×2 · hold descending to hover");
            DrawControl(ref ky, rules.x + 16f, rules.width - 32f, Label(GuardianControlAction.EvadeBoost, "SHIFT / RMB"), "Evade · air dash · mounted boost");
            DrawControl(ref ky, rules.x + 16f, rules.width - 32f, Label(GuardianControlAction.Interact, "E"), "Context: ride · dismount · reconstruct · use world");
            DrawControl(ref ky, rules.x + 16f, rules.width - 32f, Label(GuardianControlAction.TargetLock, "T"), "Lock / unlock enemy · wheel cycles target");
            DrawControl(ref ky, rules.x + 16f, rules.width - 32f, Label(GuardianControlAction.Blade, "F / LMB"), "Aetherblade combo / projectile parry");
            DrawControl(ref ky, rules.x + 16f, rules.width - 32f,
                Label(GuardianControlAction.Cleave, "Q") + " · " + Label(GuardianControlAction.Counter, "C") + " · " + Label(GuardianControlAction.Bloom, "R"),
                "Cleave · Counter · Bloom / Eclipse");

            Rect neural = new Rect(rightX, y + 368f, rightWidth, 122f);
            Fill(neural, Card);
            Stroke(neural, new Color(0.18f, 0.24f, 0.34f, 1f), 1f);
            GUI.Label(new Rect(neural.x + 16f, neural.y + 10f, neural.width - 32f, 22f), "NEURAL RESONANCE", _section);
            GUI.Label(new Rect(neural.x + 16f, neural.y + 34f, neural.width - 32f, 82f),
                "BLUE / Sight → bounded blade length, energy and damage\nGREEN / Guard → retained neural channel; no shield action in this build\nEEG never moves, interacts, jumps, evades, locks, swings or parries.", _body);

            float summaryY = y + 506f;
            Rect summary = new Rect(leftX, summaryY, panel.width - 56f, 82f);
            Fill(summary, new Color(0.050f, 0.060f, 0.080f, 0.98f));
            Stroke(summary, new Color(0.18f, 0.24f, 0.34f, 1f), 1f);
            string stamina = endurance != null ? $"{endurance.Value:F0} / {endurance.Max:F0}" : "-";
            GUI.Label(new Rect(summary.x + 16f, summary.y + 10f, summary.width - 32f, 22f),
                $"{loadout.TotalMassKg:F1} / {loadout.EquipCapacityKg:F1} kg   ·   {loadout.LoadClass.ToString().ToUpperInvariant()} LOAD   ·   ENDURANCE {stamina}", _item);
            GUI.Label(new Rect(summary.x + 16f, summary.y + 38f, summary.width - 32f, 36f),
                "Core rhythm: READ → COMMIT → EVADE → REPOSITION. Keep the control vocabulary small; let encounters create depth.", _muted);
        }

        private string CurrentObjective()
        {
            if (quests == null) return "Explore Aetheria and follow the combat route.";
            WorldQuestDefinition quest = quests.GetPrimaryActiveQuest();
            if (quest == null) return "No active objective. Explore Aetheria.";
            WorldQuestStepDefinition step = quests.GetCurrentStep(quest.id);
            if (step == null) return quest.title ?? "Objective complete";
            string description = string.IsNullOrWhiteSpace(step.description) ? string.Empty : "\n" + step.description;
            return (quest.title ?? "JOURNEY") + "\n→ " + (step.title ?? step.id) + description;
        }

        private string Label(GuardianControlAction action, string fallback)
            => controls != null ? controls.Label(action) : fallback;

        private void DrawLoadoutCard(Rect rect, string slot, string item, Color accent, string details)
        {
            Fill(rect, Card);
            Stroke(rect, new Color(accent.r, accent.g, accent.b, 0.52f), 1f);
            Fill(new Rect(rect.x, rect.y, 5f, rect.height), accent);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 12f, rect.width - 34f, 20f), slot, _subtitle);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 36f, rect.width - 34f, 28f), item, _item);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 68f, rect.width - 34f, rect.height - 76f), details, _muted);
        }

        private void DrawControl(ref float y, float x, float width, string key, string action)
        {
            GUI.Label(new Rect(x, y, 138f, 20f), key, _key);
            GUI.Label(new Rect(x + 146f, y, width - 146f, 20f), action, _body);
            y += 27f;
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

        private void EnsureStyles()
        {
            if (_title == null) _title = NewStyle(30, FontStyle.Bold, Text);
            if (_subtitle == null) _subtitle = NewStyle(13, FontStyle.Bold, Muted);
            if (_section == null) _section = NewStyle(15, FontStyle.Bold, Text);
            if (_item == null) _item = NewStyle(18, FontStyle.Bold, Text);
            if (_body == null) _body = NewStyle(13, FontStyle.Normal, Text);
            if (_muted == null) _muted = NewStyle(12, FontStyle.Normal, Muted);
            if (_key == null)
            {
                _key = NewStyle(12, FontStyle.Bold, new Color(0.74f, 0.84f, 1f, 1f));
                _key.alignment = TextAnchor.MiddleLeft;
            }
        }

        private static GUIStyle NewStyle(int size, FontStyle style, Color color)
        {
            GUIStyle gui = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = style,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
            };
            gui.normal.textColor = color;
            return gui;
        }
    }
}
