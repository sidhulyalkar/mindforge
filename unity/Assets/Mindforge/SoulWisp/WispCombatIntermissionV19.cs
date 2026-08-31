using System.Collections;
using System.Collections.Generic;
using Mindforge.Combat;
using Mindforge.Neural;
using UnityEngine;

namespace Mindforge.SoulWisp
{
    /// <summary>
    /// Keeps the manual Wisp interaction as a deliberate listening beat inside combat.
    ///
    /// Arming V pauses hostile boss authority, existing hostile projectiles, and new Guardian
    /// combat commands while ordinary movement/jump remain owned by GuardianCombatInput.
    /// The intermission ends only when the Wisp window fully ends. It restores only authority
    /// that it personally suspended and never overrides a neural-link safety stop.
    /// </summary>
    [DefaultExecutionOrder(-110)]
    public sealed class WispCombatIntermissionV19 : MonoBehaviour
    {
        private readonly List<MindforgeProjectile> _pausedProjectiles = new List<MindforgeProjectile>(24);
        private WispResonanceWindow _window;
        private FracturedSignalDirector _boss;
        private GuardianCombatInput _guardianInput;
        private NeuralLinkContingency _linkContingency;
        private bool _windowSubscribed;
        private bool _linkSubscribed;
        private bool _active;
        private bool _pausedBossByUs;
        private bool _pausedGuardianByUs;
        private bool _reassertAfterLinkRecovery;

        public bool Active => _active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<WispCombatIntermissionV19>(true) != null) return;
            new GameObject("Mindforge_WispCombatIntermission_V19").AddComponent<WispCombatIntermissionV19>();
        }

        private IEnumerator Start()
        {
            for (int frame = 0; frame < 300; frame++)
            {
                Resolve();
                Subscribe();
                if (_window != null && _boss != null && _guardianInput != null)
                    yield break;
                yield return null;
            }

            Debug.LogWarning("[Mindforge:WispV19] Combat intermission could not resolve Wisp/boss/Guardian authority; disabled.");
            enabled = false;
        }

        private void OnEnable()
        {
            Resolve();
            Subscribe();
        }

        private void Update()
        {
            if (!_active) return;
            Resolve();
            SubscribeLink();

            // Checkpoint/death/scene lifecycle can disable the Wisp component without emitting
            // WindowEnded. Never leave combat stranded in a V19-owned pause in that case.
            if (_window == null || !_window.isActiveAndEnabled || _window.State == WispResonanceState.Idle)
            {
                ReleaseIntermission();
                return;
            }

            // Recovery callbacks are intentionally turned into a next-frame action. That makes
            // this composition independent of callback/execution ordering with NeuralLinkContingency
            // while still performing zero continuous projectile discovery during neural evidence.
            if (_reassertAfterLinkRecovery)
            {
                _reassertAfterLinkRecovery = false;
                ReassertIntermission();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
            ReleaseIntermission();
        }

        private void Resolve()
        {
            if (_window == null) _window = FindObjectOfType<WispResonanceWindow>(true);
            if (_boss == null) _boss = FindObjectOfType<FracturedSignalDirector>(true);
            if (_guardianInput == null) _guardianInput = FindObjectOfType<GuardianCombatInput>(true);
            if (_linkContingency == null) _linkContingency = FindObjectOfType<NeuralLinkContingency>(true);
        }

        private void Subscribe()
        {
            if (!_windowSubscribed && _window != null)
            {
                _window.WindowArmed += OnWindowArmed;
                _window.WindowEnded += OnWindowEnded;
                _windowSubscribed = true;
            }
            SubscribeLink();
        }

        private void SubscribeLink()
        {
            if (_linkSubscribed || _linkContingency == null) return;
            _linkContingency.DegradationStateChanged += OnLinkDegradationChanged;
            _linkSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (_windowSubscribed && _window != null)
            {
                _window.WindowArmed -= OnWindowArmed;
                _window.WindowEnded -= OnWindowEnded;
            }
            if (_linkSubscribed && _linkContingency != null)
                _linkContingency.DegradationStateChanged -= OnLinkDegradationChanged;
            _windowSubscribed = false;
            _linkSubscribed = false;
        }

        private void OnWindowArmed(long windowId)
        {
            if (_active) return;
            Resolve();
            SubscribeLink();
            _active = true;
            _reassertAfterLinkRecovery = false;

            _pausedBossByUs = _boss != null && !_boss.ExternalPaused;
            if (_pausedBossByUs) _boss.SetExternalPause(true);

            _pausedGuardianByUs = _guardianInput != null && _guardianInput.CombatActionsEnabled;
            if (_pausedGuardianByUs) _guardianInput.SetCombatActionsEnabled(false);

            PauseExistingProjectiles();
            Debug.Log($"[Mindforge:WispV19] Window {windowId} entered combat intermission.");
        }

        private void OnWindowEnded(long windowId)
        {
            ReleaseIntermission();
            Debug.Log($"[Mindforge:WispV19] Window {windowId} left combat intermission.");
        }

        private void OnLinkDegradationChanged(bool degraded)
        {
            if (!_active || degraded) return;
            _reassertAfterLinkRecovery = true;
        }

        private void ReassertIntermission()
        {
            if (_boss != null && !_boss.ExternalPaused)
            {
                _pausedBossByUs = true;
                _boss.SetExternalPause(true);
            }

            if (_guardianInput != null && _guardianInput.CombatActionsEnabled)
            {
                _pausedGuardianByUs = true;
                _guardianInput.SetCombatActionsEnabled(false);
            }

            ReassertProjectilePause();
        }

        private void PauseExistingProjectiles()
        {
            _pausedProjectiles.Clear();
            ReassertProjectilePause();
        }

        private void ReassertProjectilePause()
        {
            MindforgeProjectile[] projectiles = FindObjectsOfType<MindforgeProjectile>(true);
            for (int i = 0; i < projectiles.Length; i++)
            {
                MindforgeProjectile projectile = projectiles[i];
                if (projectile == null || projectile.ExternalPaused) continue;
                projectile.SetExternalPause(true);
                if (!_pausedProjectiles.Contains(projectile))
                    _pausedProjectiles.Add(projectile);
            }
        }

        private void ReleaseIntermission()
        {
            if (!_active) return;
            _active = false;
            _reassertAfterLinkRecovery = false;

            Resolve();
            bool safetyStopActive = _linkContingency != null &&
                                    (_linkContingency.Degraded || _linkContingency.ParticipantStopped);
            if (!safetyStopActive)
            {
                if (_pausedBossByUs && _boss != null) _boss.SetExternalPause(false);
                if (_pausedGuardianByUs && _guardianInput != null) _guardianInput.SetCombatActionsEnabled(true);
                for (int i = 0; i < _pausedProjectiles.Count; i++)
                {
                    MindforgeProjectile projectile = _pausedProjectiles[i];
                    if (projectile != null && projectile.ExternalPaused)
                        projectile.SetExternalPause(false);
                }
            }

            _pausedBossByUs = false;
            _pausedGuardianByUs = false;
            _pausedProjectiles.Clear();
        }
    }
}
