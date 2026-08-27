using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// First build-screen surface. v1 is intentionally read-only because only one
    /// competition kit is qualified; later inventories can swap the same data model
    /// without rewriting combat systems.
    /// </summary>
    public sealed class GuardianEquipmentMenu : MonoBehaviour
    {
        [SerializeField] private GuardianEquipmentLoadout loadout;
        [SerializeField] private GuardianStamina stamina;
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
        private bool _visible;
        private GUIStyle _title;
        private GUIStyle _slot;
        private GUIStyle _body;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<GuardianEquipmentMenu>(true) != null) return;
            GuardianEquipmentLoadout loadout = FindObjectOfType<GuardianEquipmentLoadout>(true);
            if (loadout == null) return;
            GuardianEquipmentMenu menu = new GameObject("MindforgeEquipmentMenu").AddComponent<GuardianEquipmentMenu>();
            menu.loadout = loadout;
            menu.stamina = loadout.GetComponent<GuardianStamina>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible || loadout == null) return;
            EnsureStyles();
            float width = Mathf.Min(620f, Screen.width - 80f);
            float height = Mathf.Min(610f, Screen.height - 80f);
            Rect panel = new Rect(40f, 40f, width, height);
            GUI.Box(panel, string.Empty);

            float x = panel.x + 24f;
            float y = panel.y + 18f;
            float inner = panel.width - 48f;
            GUI.Label(new Rect(x, y, inner, 36f), "GUARDIAN BUILD", _title);
            y += 48f;

            DrawSlot(ref y, x, inner, "MAIN HAND", loadout.MainHand?.displayName ?? "Unequipped",
                loadout.MainHand != null
                    ? $"{loadout.MainHand.archetype} · {loadout.MainHand.massKg:F1} kg · reach {loadout.MainHand.reachMeters:F2} m · stamina {loadout.MainHand.staminaCost:F0}"
                    : string.Empty);
            DrawSlot(ref y, x, inner, "OFF HAND", loadout.OffHand?.displayName ?? "Unequipped",
                loadout.OffHand != null
                    ? $"{loadout.OffHand.archetype} · {loadout.OffHand.massKg:F1} kg · absorb {loadout.OffHand.baseDamageAbsorption:P0} · stability {loadout.OffHand.stability:F2}"
                    : string.Empty);
            DrawSlot(ref y, x, inner, "ARMOR", loadout.Armor?.displayName ?? "Unequipped",
                loadout.Armor != null
                    ? $"{loadout.Armor.weightClass} · {loadout.Armor.massKg:F1} kg · physical mitigation {loadout.Armor.physicalMitigation:P0}"
                    : string.Empty);

            y += 8f;
            GUI.Label(new Rect(x, y, inner, 28f), "EQUIP LOAD", _slot);
            y += 28f;
            float ratio = Mathf.Clamp01(loadout.LoadRatio);
            Rect bar = new Rect(x, y, inner, 14f);
            Color before = GUI.color;
            GUI.color = new Color(0.11f, 0.12f, 0.16f, 1f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = loadout.LoadClass == EquipLoadClass.Light ? new Color(0.42f, 0.85f, 1f) :
                        loadout.LoadClass == EquipLoadClass.Medium ? new Color(0.55f, 1f, 0.66f) :
                        loadout.LoadClass == EquipLoadClass.Heavy ? new Color(1f, 0.70f, 0.25f) :
                        new Color(1f, 0.25f, 0.25f);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * ratio, bar.height), Texture2D.whiteTexture);
            GUI.color = before;
            y += 24f;
            GUI.Label(new Rect(x, y, inner, 24f),
                $"{loadout.TotalMassKg:F1} / {loadout.EquipCapacityKg:F1} kg     {loadout.LoadClass.ToString().ToUpperInvariant()} LOAD", _body);
            y += 30f;
            GUI.Label(new Rect(x, y, inner, 70f),
                $"Move ×{loadout.MoveSpeedMultiplier:F2}    Roll speed ×{loadout.RollSpeedMultiplier:F2}    Roll stamina ×{loadout.RollStaminaMultiplier:F2}\n" +
                $"Current stamina {(stamina != null ? stamina.Value.ToString("F0") : "-")} / {(stamina != null ? stamina.Max.ToString("F0") : "-")}", _body);
            y += 80f;

            GUI.Label(new Rect(x, y, inner, 84f),
                "BUILD PHILOSOPHY\nEquipment changes reach, commitment, stamina and defense. Neural Sight/Guard amplifies the equipped tool you actively use; it never equips, swings, blocks or dodges for you.", _body);
            y += 94f;
            GUI.Label(new Rect(x, y, inner, 36f),
                "v1 ships one qualified kit. Weapon, shield and armor families are data-backed for future inventory expansion.", _body);
        }

        private void DrawSlot(ref float y, float x, float width, string slot, string item, string stats)
        {
            GUI.Label(new Rect(x, y, width, 22f), slot, _slot);
            y += 22f;
            GUI.Label(new Rect(x, y, width, 26f), item, _body);
            y += 26f;
            GUI.Label(new Rect(x + 16f, y, width - 16f, 34f), stats, _body);
            y += 42f;
        }

        private void EnsureStyles()
        {
            if (_title == null) _title = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
            if (_slot == null) _slot = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
            if (_body == null) _body = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
        }
    }
}
