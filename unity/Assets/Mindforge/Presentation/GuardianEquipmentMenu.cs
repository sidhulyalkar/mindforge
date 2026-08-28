using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Readable competition loadout screen. The current build is intentionally
    /// descriptive rather than an inventory editor: one qualified kit, immediately
    /// legible combat rules, and a clear path to future item swapping.
    /// </summary>
    public sealed class GuardianEquipmentMenu : MonoBehaviour
    {
        [SerializeField] private GuardianEquipmentLoadout loadout;
        [SerializeField] private GuardianStamina guardIntegrity;
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

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
            guardIntegrity = staminaBudget;
        }

        private void Resolve()
        {
            if (loadout == null) loadout = FindObjectOfType<GuardianEquipmentLoadout>(true);
            if (guardIntegrity == null && loadout != null) guardIntegrity = loadout.GetComponent<GuardianStamina>();
            if (guardIntegrity == null) guardIntegrity = FindObjectOfType<GuardianStamina>(true);
        }

        private void Update()
        {
            Resolve();
            if (Input.GetKeyDown(toggleKey)) _visible = !_visible;
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

            float width = Mathf.Min(940f, Screen.width - 72f);
            float height = Mathf.Min(650f, Screen.height - 60f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            Fill(panel, Panel);
            Stroke(panel, new Color(0.28f, 0.40f, 0.62f, 0.75f), 2f);

            float x = panel.x + 28f;
            float y = panel.y + 22f;
            GUI.Label(new Rect(x, y, panel.width - 56f, 38f), "WARDEN LOADOUT", _title);
            GUI.Label(new Rect(x, y + 36f, panel.width - 56f, 24f),
                "Physical combat build · hands own every action · neural focus amplifies the equipped tool", _subtitle);
            GUI.Label(new Rect(panel.xMax - 150f, y + 4f, 122f, 24f), "TAB  CLOSE", _key);
            y += 78f;

            float gap = 18f;
            float leftWidth = (panel.width - 56f - gap) * 0.58f;
            float rightWidth = panel.width - 56f - gap - leftWidth;
            float leftX = x;
            float rightX = x + leftWidth + gap;

            DrawLoadoutCard(new Rect(leftX, y, leftWidth, 118f), "MAIN HAND", loadout.MainHand?.displayName ?? "Unequipped", Blue,
                loadout.MainHand != null
                    ? $"{loadout.MainHand.archetype.ToString().ToUpperInvariant()}   DAMAGE {loadout.MainHand.baseDamage:F0}   REACH {loadout.MainHand.reachMeters:F2}m\nF swings a 3-hit chain. The active blade can physically parry hostile projectiles."
                    : "No weapon equipped");
            DrawLoadoutCard(new Rect(leftX, y + 132f, leftWidth, 118f), "OFF HAND", loadout.OffHand?.displayName ?? "Unequipped", Green,
                loadout.OffHand != null
                    ? $"{loadout.OffHand.archetype.ToString().ToUpperInvariant()}   ABSORB {loadout.OffHand.baseDamageAbsorption:P0}   STABILITY {loadout.OffHand.stability:F2}\nRMB or E raises guard. Precise timing reflects projectiles with a Perfect Guard."
                    : "No shield equipped");
            DrawLoadoutCard(new Rect(leftX, y + 264f, leftWidth, 104f), "ARMOR", loadout.Armor?.displayName ?? "Unequipped", Gold,
                loadout.Armor != null
                    ? $"{loadout.Armor.weightClass.ToString().ToUpperInvariant()}   {loadout.Armor.massKg:F1} kg\nArmor currently contributes physical load; mitigation remains intentionally unclaimed."
                    : "No armor equipped");

            Rect rules = new Rect(rightX, y, rightWidth, 230f);
            Fill(rules, Card);
            Stroke(rules, new Color(0.18f, 0.24f, 0.34f, 1f), 1f);
            GUI.Label(new Rect(rules.x + 16f, rules.y + 14f, rules.width - 32f, 24f), "COMBAT CONTROLS", _section);
            float ky = rules.y + 50f;
            DrawControl(ref ky, rules.x + 16f, rules.width - 32f, "WASD", "Move");
            DrawControl(ref ky, rules.x + 16f, rules.width - 32f, "ARROWS / MOUSE", "Aim");
            DrawControl(ref ky, rules.x + 16f, rules.width - 32f, "SPACE", "Directional dash");
            DrawControl(ref ky, rules.x + 16f, rules.width - 32f, "F", "Sword / combo / bullet parry");
            DrawControl(ref ky, rules.x + 16f, rules.width - 32f, "SHIFT", "Pulse Shot");
            DrawControl(ref ky, rules.x + 16f, rules.width - 32f, "RMB / E", "Shield");
            DrawControl(ref ky, rules.x + 16f, rules.width - 32f, "Q · C · R", "Cleave · Counter · Bloom");

            Rect neural = new Rect(rightX, y + 244f, rightWidth, 124f);
            Fill(neural, Card);
            Stroke(neural, new Color(0.18f, 0.24f, 0.34f, 1f), 1f);
            GUI.Label(new Rect(neural.x + 16f, neural.y + 12f, neural.width - 32f, 24f), "NEURAL RESONANCE", _section);
            GUI.Label(new Rect(neural.x + 16f, neural.y + 42f, neural.width - 32f, 70f),
                "BLUE / Sight → blade length, energy and bounded damage\nGREEN / Guard → shield coverage, stability and absorption\nEEG never moves, swings, blocks or dodges for you.", _body);

            float summaryY = y + 390f;
            Rect summary = new Rect(leftX, summaryY, panel.width - 56f, 126f);
            Fill(summary, new Color(0.050f, 0.060f, 0.080f, 0.98f));
            Stroke(summary, new Color(0.18f, 0.24f, 0.34f, 1f), 1f);
            GUI.Label(new Rect(summary.x + 16f, summary.y + 12f, summary.width - 32f, 24f), "CURRENT PHYSICAL PROFILE", _section);
            string integrity = guardIntegrity != null ? $"{guardIntegrity.Value:F0} / {guardIntegrity.Max:F0}" : "-";
            GUI.Label(new Rect(summary.x + 16f, summary.y + 43f, summary.width - 32f, 22f),
                $"{loadout.TotalMassKg:F1} / {loadout.EquipCapacityKg:F1} kg   ·   {loadout.LoadClass.ToString().ToUpperInvariant()} LOAD   ·   GUARD INTEGRITY {integrity}", _item);
            GUI.Label(new Rect(summary.x + 16f, summary.y + 72f, summary.width - 32f, 42f),
                "Movement, dashes and ordinary sword attacks are unrestricted. Difficulty comes from enemy patterns, spacing, timing, HP and defensive decisions rather than a movement stamina tax.", _muted);
        }

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
            GUI.Label(new Rect(x, y, 122f, 22f), key, _key);
            GUI.Label(new Rect(x + 130f, y, width - 130f, 22f), action, _body);
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
            if (_item == null) _item = NewStyle(19, FontStyle.Bold, Text);
            if (_body == null) _body = NewStyle(14, FontStyle.Normal, Text);
            if (_muted == null) _muted = NewStyle(13, FontStyle.Normal, Muted);
            if (_key == null)
            {
                _key = NewStyle(13, FontStyle.Bold, new Color(0.74f, 0.84f, 1f, 1f));
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
