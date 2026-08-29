using UnityEngine;
using Mindforge.Combat;
using Mindforge.World;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Compact gameplay HUD for the grounded-world build. Health is the dominant state,
    /// endurance communicates dodge-roll availability, Flux remains strategic, and the
    /// boss bar appears only once the final encounter is active. This component observes
    /// gameplay state only and never mutates combat or neural authority.
    /// </summary>
    [DefaultExecutionOrder(950)]
    public sealed class GroundedCombatHud : MonoBehaviour
    {
        private CombatantVitals _player;
        private CombatantVitals _boss;
        private GuardianStamina _endurance;
        private FluxMeter _flux;
        private GuardianMotor _motor;
        private NullWardEncounterDirector _ward;
        private FracturedSignalDirector _bossDirector;
        private GUIStyle _title;
        private GUIStyle _small;
        private GUIStyle _value;
        private GUIStyle _bossStyle;
        private float _damageFlashUntil;
        private float _lastDamage;

        private static readonly Color Panel = new Color(0.018f, 0.024f, 0.034f, 0.88f);
        private static readonly Color Health = new Color(0.95f, 0.22f, 0.28f, 1f);
        private static readonly Color HealthLow = new Color(1f, 0.10f, 0.12f, 1f);
        private static readonly Color Endurance = new Color(0.30f, 0.88f, 0.54f, 1f);
        private static readonly Color Flux = new Color(0.63f, 0.34f, 1f, 1f);
        private static readonly Color Text = new Color(0.95f, 0.97f, 1f, 1f);
        private static readonly Color Muted = new Color(0.62f, 0.69f, 0.78f, 1f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<GuardianCombatInput>(true) == null) return;
            if (FindObjectOfType<GroundedCombatHud>(true) != null) return;
            new GameObject("MindforgeGroundedCombatHud").AddComponent<GroundedCombatHud>();
        }

        private void OnEnable()
        {
            Resolve();
            SuppressLegacyHud();
            if (_player != null)
            {
                _player.Damaged -= OnDamaged;
                _player.Damaged += OnDamaged;
            }
        }

        private void OnDisable()
        {
            if (_player != null) _player.Damaged -= OnDamaged;
        }

        private void Update()
        {
            // RuntimeInitialize ordering is intentionally unspecified. If the legacy HUD
            // installs a frame later, keep presentation ownership singular.
            SuppressLegacyHud();
        }

        private static void SuppressLegacyHud()
        {
            CombatStateHud legacy = FindObjectOfType<CombatStateHud>(true);
            if (legacy != null && legacy.enabled) legacy.enabled = false;
        }

        private void Resolve()
        {
            CombatantVitals[] all = FindObjectsOfType<CombatantVitals>(true);
            for (int i = 0; i < all.Length; i++)
            {
                CombatantVitals candidate = all[i];
                if (candidate == null) continue;
                if (candidate.Team == CombatTeam.Guardian && _player == null) _player = candidate;
                if (candidate.Team == CombatTeam.Enemy && candidate.GetComponent<FracturedSignalDirector>() != null)
                    _boss = candidate;
            }
            if (_endurance == null) _endurance = FindObjectOfType<GuardianStamina>(true);
            if (_flux == null) _flux = FindObjectOfType<FluxMeter>(true);
            if (_motor == null) _motor = FindObjectOfType<GuardianMotor>(true);
            if (_ward == null) _ward = FindObjectOfType<NullWardEncounterDirector>(true);
            if (_bossDirector == null) _bossDirector = FindObjectOfType<FracturedSignalDirector>(true);
        }

        private void OnDamaged(DamagePacket packet)
        {
            _lastDamage = Mathf.Max(0f, packet.Damage);
            _damageFlashUntil = Time.unscaledTime + 0.55f;
        }

        private void OnGUI()
        {
            if (_player == null || _endurance == null || _flux == null) Resolve();
            if (_player == null) return;
            EnsureStyles();

            DrawPlayerVitals();
            if (BossVisible()) DrawBossVitals();
        }

        private void DrawPlayerVitals()
        {
            const float x = 24f;
            const float y = 24f;
            const float width = 382f;
            float hp01 = Ratio(_player.Health, _player.MaxHealth);
            bool low = hp01 <= 0.30f;
            bool flashing = Time.unscaledTime < _damageFlashUntil;

            Rect panel = new Rect(x, y, width, 112f);
            Fill(panel, Panel);
            Stroke(panel, flashing ? new Color(1f, 0.20f, 0.22f, 0.95f) : new Color(0.24f, 0.34f, 0.48f, 0.66f), flashing ? 2f : 1f);

            GUI.Label(new Rect(x + 14f, y + 8f, 180f, 22f), low ? "GUARDIAN · CRITICAL" : "GUARDIAN", _title);
            GUI.Label(new Rect(x + 220f, y + 8f, 146f, 22f), $"{_player.Health:F0} / {_player.MaxHealth:F0}", _value);

            DrawBar(new Rect(x + 14f, y + 34f, width - 28f, 18f), hp01, low ? HealthLow : Health);
            if (flashing && _lastDamage > 0.01f)
                GUI.Label(new Rect(x + width - 90f, y + 54f, 76f, 18f), $"-{_lastDamage:F0}", _value);

            GUI.Label(new Rect(x + 14f, y + 58f, 84f, 17f), "ENDURANCE", _small);
            DrawBar(new Rect(x + 102f, y + 61f, width - 116f, 9f), _endurance.Ratio, Endurance);
            GUI.Label(new Rect(x + 14f, y + 77f, 84f, 17f), "FLUX", _small);
            DrawBar(new Rect(x + 102f, y + 80f, width - 116f, 7f), Ratio(_flux.Value, _flux.Max), Flux);

            string movement = _motor != null && !_motor.IsGrounded
                ? "SPACE ×2 / HOLD · SHIFT AIR DASH"
                : "F / LMB BLADE   ·   SHIFT / RMB ROLL   ·   SPACE ×2";
            GUI.Label(new Rect(x + 14f, y + 92f, width - 28f, 18f), movement, _small);
        }

        private void DrawBossVitals()
        {
            if (_boss == null) return;
            float width = Mathf.Min(620f, Screen.width - 100f);
            float x = (Screen.width - width) * 0.5f;
            float y = 25f;
            Rect panel = new Rect(x, y, width, 58f);
            Fill(panel, new Color(0.022f, 0.020f, 0.032f, 0.90f));
            Stroke(panel, new Color(0.56f, 0.24f, 0.78f, 0.78f), 1f);
            int phase = _bossDirector != null ? _bossDirector.Phase : 1;
            GUI.Label(new Rect(x + 14f, y + 7f, width - 28f, 20f), $"THE FRACTURED SIGNAL · PHASE {phase}", _bossStyle);
            DrawBar(new Rect(x + 14f, y + 32f, width - 28f, 12f), Ratio(_boss.Health, _boss.MaxHealth), Health);
        }

        private bool BossVisible()
        {
            if (_boss == null || !_boss.IsAlive) return false;
            return _ward == null ? _boss.gameObject.activeInHierarchy : _ward.BossActive;
        }

        private void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Text },
            };
            _small = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = Muted },
            };
            _value = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperRight,
                normal = { textColor = Text },
            };
            _bossStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Text },
            };
        }

        private static float Ratio(float value, float max)
            => max <= 0.0001f ? 0f : Mathf.Clamp01(value / max);

        private static void DrawBar(Rect rect, float ratio, Color color)
        {
            Fill(rect, new Color(0.06f, 0.07f, 0.09f, 0.95f));
            Rect inner = new Rect(rect.x + 2f, rect.y + 2f, Mathf.Max(0f, (rect.width - 4f) * Mathf.Clamp01(ratio)), Mathf.Max(1f, rect.height - 4f));
            Fill(inner, color);
            Stroke(rect, new Color(0.45f, 0.50f, 0.58f, 0.55f), 1f);
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
