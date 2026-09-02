using Combat;
using PlayerController;
using States;
using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// Read-only runtime assurance for the inherited Dragon Souls sword pipeline.
    ///
    /// The authoritative chain remains:
    /// PlayerCombatState -> authored attack animation -> CombatController animation
    /// event -> Sword.StartAttack -> CapsuleCollider + Damage -> Health.
    ///
    /// This component never opens/closes a hitbox, changes attack damage, moves the
    /// sword, changes player state, or writes animation parameters. It only observes
    /// the already-working chain so native playtests can prove that real swing windows
    /// and real damage contacts occurred.
    /// </summary>
    [DefaultExecutionOrder(920)]
    [DisallowMultipleComponent]
    public sealed class MindforgeSwordCombatAssuranceV31 : MonoBehaviour
    {
        [SerializeField] private float stuckHitboxWarningSeconds = 1.35f;

        private PlayerStateMachine _player;
        private CombatController _combat;
        private Sword _sword;
        private CapsuleCollider _swordCollider;
        private Damage _damage;
        private TrailRenderer _trail;
        private MindforgeAetherbladePresentationV29 _aetherblade;

        private bool _wasSwingWindowOpen;
        private float _swingWindowOpenedAt;
        private bool _warnedStuckWindow;

        public bool Installed { get; private set; }
        public bool Configured { get; private set; }
        public int SwingWindowsObserved { get; private set; }
        public int PresentedSwingWindowsObserved { get; private set; }
        public int HitsObserved { get; private set; }
        public bool StuckHitboxDetected { get; private set; }
        public string LastAttackName { get; private set; }
        public bool SwingWindowOpen => _swordCollider != null && _damage != null && _swordCollider.enabled && _damage.enabled;
        public bool TrailActive => _trail != null && _trail.enabled;
        public bool AetherbladeInstalled => _aetherblade != null && _aetherblade.Installed;

        private void Start()
        {
            _player = FindObjectOfType<PlayerStateMachine>();
            if (_player != null)
                _combat = _player.combatController;

            _sword = FindObjectOfType<Sword>();
            if (_sword != null)
            {
                _swordCollider = _sword.GetComponent<CapsuleCollider>();
                _damage = _sword.GetComponent<Damage>();
                _trail = _sword.GetComponentInChildren<TrailRenderer>(true);
                _aetherblade = _sword.GetComponent<MindforgeAetherbladePresentationV29>();
            }

            Configured = ValidateConfiguration();
            if (_damage != null)
                _damage.OnHitGiven += HandleHitGiven;
            Installed = true;
        }

        private void OnDestroy()
        {
            if (_damage != null)
                _damage.OnHitGiven -= HandleHitGiven;
        }

        private void Update()
        {
            bool open = SwingWindowOpen;
            if (open && !_wasSwingWindowOpen)
            {
                SwingWindowsObserved++;
                _swingWindowOpenedAt = Time.unscaledTime;
                _warnedStuckWindow = false;
                LastAttackName = _combat != null ? _combat.CurrentAttack.animationName : string.Empty;
                if (TrailActive)
                    PresentedSwingWindowsObserved++;
            }
            else if (open && !_warnedStuckWindow && Time.unscaledTime - _swingWindowOpenedAt > stuckHitboxWarningSeconds)
            {
                StuckHitboxDetected = true;
                _warnedStuckWindow = true;
                Debug.LogWarning(
                    "[Mindforge:V31] Sword hitbox remained active longer than the assurance bound. " +
                    "The observer did not modify it; inspect the inherited attack animation events."
                );
            }

            _wasSwingWindowOpen = open;
        }

        private bool ValidateConfiguration()
        {
            if (_player == null || _combat == null || _sword == null || _swordCollider == null || _damage == null || _trail == null)
                return false;
            if (_combat.SwordLightAttacks == null || _combat.SwordLightAttacks.Length < 3)
                return false;
            if (_combat.SwordHeavyAttacks == null || _combat.SwordHeavyAttacks.Length < 1)
                return false;
            if (!AttackArrayLooksAuthored(_combat.SwordLightAttacks))
                return false;
            if (!AttackArrayLooksAuthored(_combat.SwordHeavyAttacks))
                return false;
            return true;
        }

        private static bool AttackArrayLooksAuthored(Attack[] attacks)
        {
            for (int i = 0; i < attacks.Length; i++)
            {
                Attack attack = attacks[i];
                if (string.IsNullOrEmpty(attack.animationName)) return false;
                if (attack.attackDuration <= 0f || attack.damage <= 0) return false;
            }
            return true;
        }

        private void HandleHitGiven(Collider hit)
        {
            HitsObserved++;
        }
    }
}
