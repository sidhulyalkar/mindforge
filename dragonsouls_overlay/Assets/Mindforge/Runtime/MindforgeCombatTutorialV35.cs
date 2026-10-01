using System;
using System.IO;
using Combat;
using Inputs;
using States;
using UnityEngine;

namespace Mindforge.Chassis
{
    /// <summary>
    /// V0.35 master gameplay tutorial.
    ///
    /// This component teaches and verifies the inherited Dragon Souls gameplay
    /// vocabulary without taking over its authority. InputReader events, real sword
    /// assurance, target state, Health events, Bonfire events, V0.34 BCI evidence and
    /// BossManager are observed as evidence. It never invokes an attack, moves the
    /// player, changes health, opens sword hitboxes or mutates enemy AI.
    /// </summary>
    [DefaultExecutionOrder(1360)]
    [DisallowMultipleComponent]
    public sealed class MindforgeCombatTutorialV35 : MonoBehaviour
    {
        public enum TutorialStage
        {
            Idle = 0,
            Movement = 1,
            Camera = 2,
            Sprint = 3,
            LightCombo = 4,
            HeavyAttack = 5,
            DamageContact = 6,
            TargetLock = 7,
            TargetSwitch = 8,
            DodgeRoll = 9,
            AimThrow = 10,
            Recall = 11,
            Sheath = 12,
            Heal = 13,
            BonfireRest = 14,
            NeuralAttunement = 15,
            SightFieldUse = 16,
            GuardFieldUse = 17,
            BossEntry = 18,
            BossDefeat = 19,
            Complete = 20,
            Partial = 21,
        }

        [Serializable]
        private sealed class TutorialReceipt
        {
            public string schema = "mindforge.combat_tutorial_receipt.v1";
            public string source_commit;
            public bool clean_source;
            public string unity_version;
            public string session_id;
            public string status;
            public string final_stage;
            public bool movement;
            public bool camera;
            public bool sprint;
            public int light_swing_windows;
            public bool heavy_attack;
            public int sword_hits;
            public bool target_lock;
            public bool target_switch;
            public bool dodge_roll;
            public bool sword_throw;
            public bool sword_recall;
            public bool sheath_toggle;
            public bool heal;
            public bool bonfire_rest;
            public string adaptive_bci_status;
            public bool sight_field_use;
            public bool guard_field_use;
            public bool boss_entry;
            public bool boss_defeat;
            public string generated_utc;
        }

        [Header("Pacing")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private float movementEvidenceSeconds = 0.55f;
        [SerializeField] private float cameraEvidenceSeconds = 0.45f;
        [SerializeField] private int requiredLightSwingWindows = 3;
        [SerializeField] private float neuralRetrySeconds = 0.90f;
        [SerializeField] private int maximumNeuralFieldAttempts = 4;

        private PlayerStateMachine _player;
        private InputReader _input;
        private MindforgeSwordCombatAssuranceV31 _swordAssurance;
        private MindforgeAdaptiveBciTutorialV34 _adaptiveTutorial;
        private MindforgeNeuralWindowControllerV33 _windows;
        private MindforgeSightReceptorV33 _sight;
        private MindforgeGuardReceptorV33 _guard;
        private MindforgeBciMarkerSenderV33 _markers;
        private MindforgeNativeProvenanceV33 _provenance;

        private bool _bound;
        private bool _started;
        private bool _receiptWritten;
        private float _stageEnteredAt;
        private float _movementEvidence;
        private float _cameraEvidence;
        private int _lightInputs;
        private int _heavyInputs;
        private int _targetInputs;
        private int _targetSelectInputs;
        private int _rollInputs;
        private int _aimInputs;
        private int _weaponReturnInputs;
        private int _sheathInputs;
        private bool _healObserved;
        private bool _bonfireObserved;
        private bool _bossEntered;
        private bool _bossDefeated;

        private int _baselineSwingWindows;
        private int _baselineHits;
        private int _baselineHeavyInputs;
        private int _baselineTargetInputs;
        private int _baselineTargetSelectInputs;
        private int _baselineRollInputs;
        private int _baselineAimInputs;
        private int _baselineWeaponReturnInputs;
        private int _baselineSheathInputs;
        private int _baselineSightActivations;
        private int _baselineGuardActivations;
        private Transform _targetAtStageEntry;
        private bool _aimAttackObserved;
        private bool _throwObserved;
        private bool _recallObserved;
        private bool _sheathObserved;
        private int _neuralAttempts;
        private float _nextNeuralAttemptAt;
        private string _receiptPath;

        public event Action<TutorialStage> StageChanged;

        public TutorialStage Stage { get; private set; } = TutorialStage.Idle;
        public bool Started => _started;
        public bool Finished => Stage == TutorialStage.Complete || Stage == TutorialStage.Partial;
        public string ReceiptPath => _receiptPath;

        private void Start()
        {
            ResolveDependencies();
            Bind();
            if (autoStart) BeginTutorial();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void BeginTutorial()
        {
            ResolveDependencies();
            Bind();
            if (_player == null || _input == null || _swordAssurance == null)
            {
                EnterStage(TutorialStage.Partial, "required_gameplay_authority_missing");
                return;
            }

            _started = true;
            _receiptWritten = false;
            _movementEvidence = 0f;
            _cameraEvidence = 0f;
            _healObserved = false;
            _bonfireObserved = false;
            _bossEntered = false;
            _bossDefeated = false;
            _throwObserved = false;
            _recallObserved = false;
            _sheathObserved = false;
            EnterStage(TutorialStage.Movement, "master_tutorial_started");
        }

        private void Update()
        {
            ResolveDependencies();
            if (!_started || Finished || _player == null || _input == null) return;

            switch (Stage)
            {
                case TutorialStage.Movement:
                    UpdateMovement();
                    break;
                case TutorialStage.Camera:
                    UpdateCamera();
                    break;
                case TutorialStage.Sprint:
                    if (_player.isSprinting || _input.SprintHold)
                    {
                        _sprintObserved = true;
                        EnterStage(TutorialStage.LightCombo, "sprint_observed");
                    }
                    break;
                case TutorialStage.LightCombo:
                    if (_swordAssurance != null &&
                        _swordAssurance.SwingWindowsObserved - _baselineSwingWindows >= requiredLightSwingWindows &&
                        _lightInputs - _baselineLightInputs >= requiredLightSwingWindows)
                    {
                        _lightComboObserved = true;
                        EnterStage(TutorialStage.HeavyAttack, "light_combo_windows_observed");
                    }
                    break;
                case TutorialStage.HeavyAttack:
                    if (_heavyInputs > _baselineHeavyInputs &&
                        _swordAssurance != null &&
                        _swordAssurance.SwingWindowsObserved > _baselineSwingWindows)
                    {
                        _heavyAttackObserved = true;
                        EnterStage(TutorialStage.DamageContact, "heavy_swing_observed");
                    }
                    break;
                case TutorialStage.DamageContact:
                    if (_swordAssurance != null && _swordAssurance.HitsObserved > _baselineHits)
                    {
                        _damageContactObserved = true;
                        EnterStage(TutorialStage.TargetLock, "real_sword_contact_observed");
                    }
                    break;
                case TutorialStage.TargetLock:
                    if (_targetInputs > _baselineTargetInputs &&
                        _player.targetableCheck != null &&
                        _player.targetableCheck.CurrentTargetTransform != null)
                    {
                        _targetLockObserved = true;
                        EnterStage(TutorialStage.TargetSwitch, "target_lock_observed");
                    }
                    break;
                case TutorialStage.TargetSwitch:
                    UpdateTargetSwitch();
                    break;
                case TutorialStage.DodgeRoll:
                    if (_rollInputs > _baselineRollInputs && (_player.isRoll || _player.health.IsInvulnerable))
                    {
                        _rollObserved = true;
                        EnterStage(TutorialStage.AimThrow, "roll_state_observed");
                    }
                    break;
                case TutorialStage.AimThrow:
                    UpdateAimThrow();
                    break;
                case TutorialStage.Recall:
                    if (_weaponReturnInputs > _baselineWeaponReturnInputs &&
                        _player.combatController != null &&
                        _player.combatController.IsSwordReturned)
                    {
                        _recallObserved = true;
                        EnterStage(TutorialStage.Sheath, "sword_returned");
                    }
                    break;
                case TutorialStage.Sheath:
                    if (_sheathInputs > _baselineSheathInputs &&
                        _player.combatController != null &&
                        _player.combatController.IsSwordInSheath)
                    {
                        _sheathObserved = true;
                        EnterStage(TutorialStage.Heal, "sword_sheathed");
                    }
                    break;
                case TutorialStage.Heal:
                    if (_healObserved)
                        EnterStage(TutorialStage.BonfireRest, "real_heal_observed");
                    break;
                case TutorialStage.BonfireRest:
                    if (_bonfireObserved)
                        EnterStage(TutorialStage.NeuralAttunement, "bonfire_rest_observed");
                    break;
                case TutorialStage.NeuralAttunement:
                    UpdateNeuralAttunement();
                    break;
                case TutorialStage.SightFieldUse:
                    UpdateNeuralFieldUse(MindforgeIntentV29.Sight);
                    break;
                case TutorialStage.GuardFieldUse:
                    UpdateNeuralFieldUse(MindforgeIntentV29.Guard);
                    break;
                case TutorialStage.BossEntry:
                    if (BossManager.Instance != null && BossManager.Instance.IsInBoss)
                    {
                        _bossEntered = true;
                        EnterStage(TutorialStage.BossDefeat, "boss_entry_observed");
                    }
                    break;
            }
        }

        private void UpdateMovement()
        {
            if (_input.MovementOn2DAxis.sqrMagnitude > 0.20f)
                _movementEvidence += Time.unscaledDeltaTime;
            else
                _movementEvidence = Mathf.Max(0f, _movementEvidence - Time.unscaledDeltaTime * 0.5f);

            if (_movementEvidence >= movementEvidenceSeconds)
                EnterStage(TutorialStage.Camera, "movement_observed");
        }

        private void UpdateCamera()
        {
            if (_input.CameraMovementOn2DAxis.sqrMagnitude > 0.04f)
                _cameraEvidence += Time.unscaledDeltaTime;
            else
                _cameraEvidence = Mathf.Max(0f, _cameraEvidence - Time.unscaledDeltaTime * 0.5f);

            if (_cameraEvidence >= cameraEvidenceSeconds)
                EnterStage(TutorialStage.Sprint, "camera_control_observed");
        }

        private void UpdateTargetSwitch()
        {
            if (_player.targetableCheck == null) return;
            Transform current = _player.targetableCheck.CurrentTargetTransform;
            if (_targetSelectInputs > _baselineTargetSelectInputs &&
                current != null && _targetAtStageEntry != null && current != _targetAtStageEntry)
            {
                _targetSwitchObserved = true;
                EnterStage(TutorialStage.DodgeRoll, "target_switch_observed");
            }
        }

        private void UpdateAimThrow()
        {
            if (_player.combatController == null) return;
            if (_aimInputs > _baselineAimInputs && _aimAttackObserved)
            {
                if (!_player.combatController.IsSwordReturned)
                {
                    _throwObserved = true;
                    EnterStage(TutorialStage.Recall, "sword_throw_observed");
                }
            }
        }

        private void UpdateNeuralAttunement()
        {
            if (_adaptiveTutorial == null)
            {
                EnterStage(TutorialStage.Partial, "adaptive_bci_tutorial_missing");
                return;
            }

            if (!_adaptiveTutorial.Started)
            {
                _adaptiveTutorial.BeginTutorial();
                return;
            }

            if (!_adaptiveTutorial.Finished) return;

            if (_adaptiveTutorial.Stage == MindforgeAdaptiveBciTutorialV34.TutorialStage.Complete)
                EnterStage(TutorialStage.SightFieldUse, "adaptive_bci_complete");
            else
                EnterStage(TutorialStage.Partial, "adaptive_bci_partial");
        }

        private void UpdateNeuralFieldUse(MindforgeIntentV29 expected)
        {
            if (_windows == null)
            {
                EnterStage(TutorialStage.Partial, "neural_window_controller_missing");
                return;
            }

            int activation = expected == MindforgeIntentV29.Sight
                ? (_sight != null ? _sight.ActivationCount : 0)
                : (_guard != null ? _guard.ActivationCount : 0);
            int baseline = expected == MindforgeIntentV29.Sight
                ? _baselineSightActivations
                : _baselineGuardActivations;

            if (activation > baseline)
            {
                if (expected == MindforgeIntentV29.Sight) _sightFieldObserved = true;
                else _guardFieldObserved = true;
                EnterStage(
                    expected == MindforgeIntentV29.Sight ? TutorialStage.GuardFieldUse : TutorialStage.BossEntry,
                    expected == MindforgeIntentV29.Sight ? "sight_field_use_observed" : "guard_field_use_observed"
                );
                return;
            }

            if (_windows.IsListening || Time.unscaledTime < _nextNeuralAttemptAt) return;
            if (_neuralAttempts >= maximumNeuralFieldAttempts)
            {
                EnterStage(TutorialStage.Partial, expected.ToString().ToLowerInvariant() + "_field_use_exhausted");
                return;
            }

            if (_windows.OpenWindow("master_tutorial_" + expected.ToString().ToLowerInvariant(), requireCalibration: true))
            {
                _neuralAttempts++;
                _nextNeuralAttemptAt = Time.unscaledTime + neuralRetrySeconds;
            }
        }

        private void EnterStage(TutorialStage next, string reason)
        {
            if (Stage == next) return;
            Stage = next;
            _stageEnteredAt = Time.unscaledTime;
            if (Stage == TutorialStage.Heal) _healObserved = false;
            if (Stage == TutorialStage.BonfireRest) _bonfireObserved = false;
            CaptureStageBaselines();
            StageChanged?.Invoke(Stage);
            _markers?.SendTutorialStage("master_" + Stage.ToString().ToLowerInvariant(), "begin", reason);
            Debug.Log($"[Mindforge:V35:TUTORIAL] {Stage} ({reason}).");

            if (Stage == TutorialStage.Complete || Stage == TutorialStage.Partial)
                WriteReceipt();
        }

        private void CaptureStageBaselines()
        {
            _baselineSwingWindows = _swordAssurance != null ? _swordAssurance.SwingWindowsObserved : 0;
            _baselineHits = _swordAssurance != null ? _swordAssurance.HitsObserved : 0;
            _baselineHeavyInputs = _heavyInputs;
            _baselineTargetInputs = _targetInputs;
            _baselineTargetSelectInputs = _targetSelectInputs;
            _baselineRollInputs = _rollInputs;
            _baselineAimInputs = _aimInputs;
            _baselineWeaponReturnInputs = _weaponReturnInputs;
            _baselineSheathInputs = _sheathInputs;
            _baselineSightActivations = _sight != null ? _sight.ActivationCount : 0;
            _baselineGuardActivations = _guard != null ? _guard.ActivationCount : 0;
            _targetAtStageEntry = _player != null && _player.targetableCheck != null
                ? _player.targetableCheck.CurrentTargetTransform
                : null;
            _aimAttackObserved = false;
            _neuralAttempts = 0;
            _nextNeuralAttemptAt = Time.unscaledTime;
        }

        private void ResolveDependencies()
        {
            if (_player == null) _player = FindObjectOfType<PlayerStateMachine>(true);
            if (_player != null && _input == null) _input = _player.InputReader;
            if (_swordAssurance == null) _swordAssurance = FindObjectOfType<MindforgeSwordCombatAssuranceV31>(true);
            if (_adaptiveTutorial == null) _adaptiveTutorial = FindObjectOfType<MindforgeAdaptiveBciTutorialV34>(true);
            if (_windows == null) _windows = FindObjectOfType<MindforgeNeuralWindowControllerV33>(true);
            if (_sight == null) _sight = FindObjectOfType<MindforgeSightReceptorV33>(true);
            if (_guard == null) _guard = FindObjectOfType<MindforgeGuardReceptorV33>(true);
            if (_markers == null) _markers = FindObjectOfType<MindforgeBciMarkerSenderV33>(true);
            if (_provenance == null) _provenance = FindObjectOfType<MindforgeNativeProvenanceV33>(true);
            if (!_bound) Bind();
        }

        private void Bind()
        {
            if (_bound || _input == null || _player == null) return;

            _input.LightAttackEvent += HandleLightAttack;
            _input.HeavyAttackEvent += HandleHeavyAttack;
            _input.TargetEvent += HandleTarget;
            _input.TargetSelectEvent += HandleTargetSelect;
            _input.RollEvent += HandleRoll;
            _input.AimHoldEvent += HandleAim;
            _input.WeaponReturnEvent += HandleWeaponReturn;
            _input.SheathUnsheathSword += HandleSheath;
            _player.health.OnHealthIncreased += HandleHealthIncreased;
            _player.health.OnDead += HandlePlayerDead;
            _player.OnPlayerRespawn += HandlePlayerRespawn;

            if (BonfiresManager.Instance != null)
                BonfiresManager.Instance.OnTakeRestEvent += HandleBonfireRest;
            if (BossManager.Instance != null)
                BossManager.Instance.OnBossDefeated += HandleBossDefeated;

            _bound = true;
        }

        private void Unbind()
        {
            if (!_bound) return;

            if (_input != null)
            {
                _input.LightAttackEvent -= HandleLightAttack;
                _input.HeavyAttackEvent -= HandleHeavyAttack;
                _input.TargetEvent -= HandleTarget;
                _input.TargetSelectEvent -= HandleTargetSelect;
                _input.RollEvent -= HandleRoll;
                _input.AimHoldEvent -= HandleAim;
                _input.WeaponReturnEvent -= HandleWeaponReturn;
                _input.SheathUnsheathSword -= HandleSheath;
            }
            if (_player != null && _player.health != null)
            {
                _player.health.OnHealthIncreased -= HandleHealthIncreased;
                _player.health.OnDead -= HandlePlayerDead;
                _player.OnPlayerRespawn -= HandlePlayerRespawn;
            }
            if (BonfiresManager.Instance != null)
                BonfiresManager.Instance.OnTakeRestEvent -= HandleBonfireRest;
            if (BossManager.Instance != null)
                BossManager.Instance.OnBossDefeated -= HandleBossDefeated;

            _bound = false;
        }

        private void HandleLightAttack()
        {
            _lightInputs++;
            if (Stage == TutorialStage.AimThrow && _input != null && _input.AimHold)
                _aimAttackObserved = true;
        }

        private void HandleHeavyAttack()
        {
            _heavyInputs++;
            if (Stage == TutorialStage.AimThrow && _input != null && _input.AimHold)
                _aimAttackObserved = true;
        }

        private void HandleTarget() { _targetInputs++; }
        private void HandleTargetSelect(Vector2 direction) { _targetSelectInputs++; }
        private void HandleRoll() { _rollInputs++; }
        private void HandleAim() { _aimInputs++; }
        private void HandleWeaponReturn() { _weaponReturnInputs++; }
        private void HandleSheath() { _sheathInputs++; }
        private void HandleHealthIncreased() { _healObserved = true; }
        private void HandleBonfireRest() { _bonfireObserved = true; }

        private void HandlePlayerDead()
        {
            Debug.Log("[Mindforge:V35:TUTORIAL] Player death observed; inherited respawn remains authoritative.");
        }

        private void HandlePlayerRespawn()
        {
            Debug.Log("[Mindforge:V35:TUTORIAL] Player respawn observed; tutorial stage preserved.");
        }

        private void HandleBossDefeated()
        {
            _bossDefeated = true;
            if (Stage == TutorialStage.BossDefeat)
                EnterStage(TutorialStage.Complete, "boss_defeat_observed");
        }

        private string Instruction()
        {
            switch (Stage)
            {
                case TutorialStage.Movement: return "MOVE  •  WASD. Keep moving until control is confirmed.";
                case TutorialStage.Camera: return "LOOK  •  Arrow keys or mouse. Sweep the camera deliberately.";
                case TutorialStage.Sprint: return "SPRINT  •  Left Shift while moving.";
                case TutorialStage.LightCombo: return "LIGHT COMBO  •  LMB or Space. Produce three real sword swing windows.";
                case TutorialStage.HeavyAttack: return "HEAVY  •  RMB. The tutorial waits for the authored heavy input plus a real swing window.";
                case TutorialStage.DamageContact: return "CONTACT  •  Land one real Aetherblade hit on an enemy.";
                case TutorialStage.TargetLock: return "TARGET LOCK  •  MMB or T while an enemy is in range.";
                case TutorialStage.TargetSwitch: return "SWITCH TARGET  •  Mouse wheel while locked. The selected target must actually change.";
                case TutorialStage.DodgeRoll: return "DODGE  •  Left Alt. Enter the inherited roll state.";
                case TutorialStage.AimThrow: return "THROW  •  Hold Q to aim, then LMB / Space or RMB to throw the Aetherblade.";
                case TutorialStage.Recall: return "RECALL  •  Press R and wait until the sword is physically returned.";
                case TutorialStage.Sheath: return "SHEATHE  •  Press X until the sword reaches its sheath state.";
                case TutorialStage.Heal: return "HEAL  •  After taking damage, press H. Advancement requires real health restoration.";
                case TutorialStage.BonfireRest: return "REST  •  Use E at a bonfire and complete an actual rest.";
                case TutorialStage.NeuralAttunement: return "NEURAL ATTUNEMENT  •  Complete the V0.34 gaze + Sight/Guard calibration protocol.";
                case TutorialStage.SightFieldUse: return "SIGHT IN THE FIELD  •  A calibrated neural window will open. Resolve Sight.";
                case TutorialStage.GuardFieldUse: return "GUARD IN THE FIELD  •  Resolve Guard through the same calibrated semantic path.";
                case TutorialStage.BossEntry: return "BOSS ENTRY  •  Reach the Fractured Signal and trigger the inherited boss encounter.";
                case TutorialStage.BossDefeat: return "MASTERY  •  Defeat the boss using the systems you just proved.";
                case TutorialStage.Complete: return "FORGE COMPLETE  •  Full tutorial evidence captured.";
                case TutorialStage.Partial: return "PARTIAL  •  The tutorial stopped without claiming unobserved abilities.";
                default: return "MINDFORGE MASTER TUTORIAL";
            }
        }

        private void OnGUI()
        {
            if (!_started) return;
            if (Stage == TutorialStage.NeuralAttunement &&
                _adaptiveTutorial != null && _adaptiveTutorial.Started && !_adaptiveTutorial.Finished)
                return;

            float width = Mathf.Min(920f, Screen.width - 48f);
            Rect panel = new Rect(24f, Screen.height - 132f, width, 96f);
            GUI.Box(panel, "");
            GUI.Label(new Rect(panel.x + 16f, panel.y + 10f, panel.width - 32f, 24f),
                "MINDFORGE • MASTER TUTORIAL • " + Stage.ToString().ToUpperInvariant());
            GUI.Label(new Rect(panel.x + 16f, panel.y + 38f, panel.width - 32f, 48f), Instruction());
        }

        private void WriteReceipt()
        {
            if (_receiptWritten) return;
            _receiptWritten = true;

            TutorialReceipt receipt = new TutorialReceipt
            {
                source_commit = _provenance != null ? _provenance.SourceCommit : null,
                clean_source = _provenance != null && _provenance.IsCleanSource,
                unity_version = Application.unityVersion,
                session_id = _markers != null ? _markers.SessionId : null,
                status = Stage == TutorialStage.Complete ? "complete" : "partial",
                final_stage = Stage.ToString(),
                movement = _movementEvidence >= movementEvidenceSeconds,
                camera = _cameraEvidence >= cameraEvidenceSeconds,
                sprint = _sprintObserved,
                light_swing_windows = _swordAssurance != null ? _swordAssurance.SwingWindowsObserved : 0,
                heavy_attack = _heavyInputs > 0,
                sword_hits = _swordAssurance != null ? _swordAssurance.HitsObserved : 0,
                target_lock = _targetInputs > 0,
                target_switch = _targetSelectInputs > 0,
                dodge_roll = _rollInputs > 0,
                sword_throw = _throwObserved,
                sword_recall = _recallObserved,
                sheath_toggle = _sheathObserved,
                heal = _healObserved,
                bonfire_rest = _bonfireObserved,
                adaptive_bci_status = _adaptiveTutorial != null ? _adaptiveTutorial.StatusLabel : "missing",
                sight_field_use = _sight != null && _sight.ActivationCount > 0,
                guard_field_use = _guard != null && _guard.ActivationCount > 0,
                boss_entry = _bossEntered,
                boss_defeat = _bossDefeated,
                generated_utc = DateTime.UtcNow.ToString("o"),
            };

            try
            {
                string directory = Path.Combine(Application.persistentDataPath, "mindforge-tutorial");
                Directory.CreateDirectory(directory);
                string session = string.IsNullOrEmpty(receipt.session_id) ? Guid.NewGuid().ToString("N") : receipt.session_id;
                _receiptPath = Path.Combine(directory, "v35-master-tutorial-" + session + ".json");
                File.WriteAllText(_receiptPath, JsonUtility.ToJson(receipt, true) + Environment.NewLine);
                Debug.Log($"[Mindforge:V35:TUTORIAL] receipt={_receiptPath} status={receipt.status}.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Mindforge:V35:TUTORIAL] receipt write failed: {ex.Message}");
            }
        }
    }
}
