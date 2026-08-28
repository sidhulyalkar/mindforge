using System;
using UnityEngine;
using Mindforge.Combat;
using Mindforge.SoulWisp;

namespace Mindforge.Journey
{
    [Serializable]
    public sealed class JourneyEncounterStage
    {
        public string id = "stage";
        public string title = "ENCOUNTER";
        [TextArea] public string lesson = "Read the room.";
        public Transform activationPoint;
        public float activationRadius = 4.5f;
        public JourneyGate exitGate;
        public JourneyEnemyController[] enemies = Array.Empty<JourneyEnemyController>();
        [Range(0f, 0.25f)] public float clearHealFraction = 0.06f;

        [NonSerialized] public bool started;
        [NonSerialized] public bool cleared;
    }

    /// <summary>
    /// Deterministic traversal/encounter progression for the first authored journey.
    /// It activates existing enemy authority and gates, but never issues player input,
    /// damage, neural evidence, calibration success or BCI target selection.
    ///
    /// The director lives under the arena root. That root is disabled during Awakening,
    /// so Start() is also the safe transition point for moving the Guardian from the
    /// calibration room into the authored cavern. Calibration geometry is never moved.
    /// </summary>
    public sealed class FirstJourneyDirector : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private CombatantVitals playerVitals;
        [SerializeField] private GuardianTargetLock targetLock;
        [SerializeField] private SoulWispController soulWisp;
        [SerializeField] private JourneyEncounterStage[] stages = Array.Empty<JourneyEncounterStage>();

        [Header("Journey entry")]
        [SerializeField] private Transform journeyStart;
        [SerializeField] private bool repositionPlayerWhenCombatWorldOpens = true;

        [Header("Boss threshold")]
        [SerializeField] private GameObject bossRoot;
        [SerializeField] private Transform bossTarget;
        [SerializeField] private CombatantVitals bossVitals;
        [SerializeField] private Transform bossActivationPoint;
        [SerializeField] private float bossActivationRadius = 4.6f;
        [SerializeField] private JourneyGate bossSeal;

        [Header("Pacing")]
        [SerializeField] private string initialObjective = "FOLLOW THE SIGNAL THROUGH THE CAVERN";
        [SerializeField] private string bossApproachObjective = "THE WARDEN IS BROKEN · ENTER THE SIGNAL CHAMBER";

        private bool _initialized;
        private int _currentStage;
        private bool _bossUnlocked;
        private bool _bossStarted;
        private bool _completed;

        public event Action<int, string, string> StageStarted;
        public event Action<int, string> StageCleared;
        public event Action BossUnlocked;
        public event Action BossStarted;
        public event Action JourneyCompleted;
        public event Action<string> ObjectiveChanged;

        public int CurrentStageIndex => _currentStage;
        public bool BossUnlockedState => _bossUnlocked;
        public bool BossActive => _bossStarted && bossRoot != null && bossRoot.activeInHierarchy;
        public bool Completed => _completed;
        public string CurrentObjective { get; private set; }

        public void ConfigureRuntime(
            Transform guardian,
            CombatantVitals guardianVitals,
            GuardianTargetLock lockState,
            SoulWispController wisp,
            JourneyEncounterStage[] encounterStages,
            GameObject finalBossRoot,
            Transform finalBossTarget,
            CombatantVitals finalBossVitals,
            Transform activationPoint,
            JourneyGate arenaSeal)
        {
            player = guardian;
            playerVitals = guardianVitals;
            targetLock = lockState;
            soulWisp = wisp;
            stages = encounterStages ?? Array.Empty<JourneyEncounterStage>();
            bossRoot = finalBossRoot;
            bossTarget = finalBossTarget;
            bossVitals = finalBossVitals;
            bossActivationPoint = activationPoint;
            bossSeal = arenaSeal;
        }

        private void Start() => InitializeJourney();

        private void OnDisable()
        {
            UnsubscribeAllEnemies();
            if (bossVitals != null) bossVitals.Died -= OnBossDied;
        }

        private void Update()
        {
            if (!_initialized) InitializeJourney();
            if (_completed || player == null) return;

            if (_currentStage < stages.Length)
            {
                JourneyEncounterStage stage = stages[_currentStage];
                if (stage == null) { AdvancePastInvalidStage(); return; }
                if (!stage.started && IsNear(stage.activationPoint, stage.activationRadius)) BeginStage(_currentStage);
                else if (stage.started && !stage.cleared && IsStageCleared(stage)) CompleteStage(_currentStage);
                return;
            }

            if (_bossUnlocked && !_bossStarted && IsNear(bossActivationPoint, bossActivationRadius))
                StartBossEncounter();
        }

        public void SetExternalPause(bool paused)
        {
            if (stages == null) return;
            for (int i = 0; i < stages.Length; i++)
            {
                JourneyEncounterStage stage = stages[i];
                if (stage?.enemies == null) continue;
                for (int j = 0; j < stage.enemies.Length; j++)
                    stage.enemies[j]?.SetExternalPause(paused);
            }
        }

        private void InitializeJourney()
        {
            if (_initialized) return;
            _initialized = true;
            _currentStage = 0;
            _bossUnlocked = false;
            _bossStarted = false;
            _completed = false;

            if (targetLock == null && player != null) targetLock = player.GetComponent<GuardianTargetLock>();
            targetLock?.SetLocked(false);
            targetLock?.Configure(bossTarget);
            soulWisp?.SetTarget(null);
            EnterJourneyStart();

            if (stages != null)
            {
                for (int i = 0; i < stages.Length; i++)
                {
                    JourneyEncounterStage stage = stages[i];
                    if (stage == null) continue;
                    stage.started = false;
                    stage.cleared = false;
                    stage.exitGate?.SetOpen(false, true);
                    if (stage.enemies == null) continue;
                    for (int j = 0; j < stage.enemies.Length; j++)
                    {
                        JourneyEnemyController enemy = stage.enemies[j];
                        if (enemy == null) continue;
                        enemy.Defeated -= OnEnemyDefeated;
                        enemy.gameObject.SetActive(false);
                    }
                }
            }

            bossSeal?.SetOpen(true, true);
            if (bossVitals != null)
            {
                bossVitals.Died -= OnBossDied;
                bossVitals.Died += OnBossDied;
            }
            if (bossRoot != null) bossRoot.SetActive(false);
            SetObjective(initialObjective);
        }

        private void EnterJourneyStart()
        {
            if (!repositionPlayerWhenCombatWorldOpens || player == null || journeyStart == null) return;

            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = journeyStart.position;
                body.rotation = journeyStart.rotation;
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.WakeUp();
            }
            else
            {
                player.SetPositionAndRotation(journeyStart.position, journeyStart.rotation);
            }

            Physics.SyncTransforms();
            Debug.Log("[Mindforge:Journey] Combat world opened. Guardian entered the Listening Cavern; Awakening/calibration geometry was left untouched.");
        }

        private void BeginStage(int index)
        {
            if (index < 0 || index >= stages.Length) return;
            JourneyEncounterStage stage = stages[index];
            if (stage == null || stage.started) return;
            stage.started = true;
            stage.exitGate?.SetOpen(false);

            if (stage.enemies != null)
            {
                for (int i = 0; i < stage.enemies.Length; i++)
                {
                    JourneyEnemyController enemy = stage.enemies[i];
                    if (enemy == null) continue;
                    enemy.gameObject.SetActive(true);
                    enemy.Defeated -= OnEnemyDefeated;
                    enemy.Defeated += OnEnemyDefeated;
                    enemy.Arm();
                }
            }

            string objective = string.IsNullOrWhiteSpace(stage.lesson)
                ? stage.title
                : stage.title + " · " + stage.lesson;
            SetObjective(objective);
            StageStarted?.Invoke(index, stage.title, stage.lesson);
        }

        private void CompleteStage(int index)
        {
            if (index < 0 || index >= stages.Length) return;
            JourneyEncounterStage stage = stages[index];
            if (stage == null || stage.cleared) return;
            stage.cleared = true;

            if (stage.enemies != null)
            {
                for (int i = 0; i < stage.enemies.Length; i++)
                    if (stage.enemies[i] != null) stage.enemies[i].Defeated -= OnEnemyDefeated;
            }

            stage.exitGate?.SetOpen(true);
            if (playerVitals != null && stage.clearHealFraction > 0f)
                playerVitals.Heal(playerVitals.MaxHealth * Mathf.Clamp01(stage.clearHealFraction));
            StageCleared?.Invoke(index, stage.id);
            _currentStage++;

            if (_currentStage >= stages.Length)
            {
                _bossUnlocked = true;
                SetObjective(bossApproachObjective);
                BossUnlocked?.Invoke();
            }
            else
            {
                JourneyEncounterStage next = stages[_currentStage];
                string nextTitle = next != null && !string.IsNullOrWhiteSpace(next.title)
                    ? next.title
                    : "DEEPER INTO THE RUIN";
                SetObjective("PATH OPEN · " + nextTitle);
            }
        }

        private void StartBossEncounter()
        {
            if (_bossStarted || !_bossUnlocked) return;
            _bossStarted = true;
            bossSeal?.SetOpen(false);
            if (bossRoot != null) bossRoot.SetActive(true);
            targetLock?.Configure(bossTarget);
            soulWisp?.SetTarget(bossTarget);
            SetObjective("THE FRACTURED SIGNAL · BREAK THE SOURCE");
            BossStarted?.Invoke();
        }

        private void OnBossDied()
        {
            if (_completed) return;
            _completed = true;
            bossSeal?.SetOpen(true);
            soulWisp?.SetTarget(null);
            targetLock?.SetLocked(false);
            SetObjective("THE SIGNAL IS QUIET");
            JourneyCompleted?.Invoke();
        }

        private void OnEnemyDefeated(JourneyEnemyController enemy)
        {
            if (_currentStage < 0 || _currentStage >= stages.Length) return;
            JourneyEncounterStage stage = stages[_currentStage];
            if (stage != null && stage.started && !stage.cleared && IsStageCleared(stage))
                CompleteStage(_currentStage);
        }

        private bool IsStageCleared(JourneyEncounterStage stage)
        {
            if (stage == null || stage.enemies == null || stage.enemies.Length == 0) return true;
            for (int i = 0; i < stage.enemies.Length; i++)
            {
                JourneyEnemyController enemy = stage.enemies[i];
                if (enemy != null && enemy.Vitals != null && enemy.Vitals.IsAlive) return false;
            }
            return true;
        }

        private bool IsNear(Transform point, float radius)
        {
            if (point == null || player == null) return false;
            Vector3 delta = point.position - player.position;
            delta.y = 0f;
            return delta.sqrMagnitude <= Mathf.Max(0.5f, radius) * Mathf.Max(0.5f, radius);
        }

        private void AdvancePastInvalidStage()
        {
            _currentStage++;
            if (_currentStage >= stages.Length)
            {
                _bossUnlocked = true;
                SetObjective(bossApproachObjective);
                BossUnlocked?.Invoke();
            }
        }

        private void SetObjective(string value)
        {
            CurrentObjective = value ?? string.Empty;
            ObjectiveChanged?.Invoke(CurrentObjective);
        }

        private void UnsubscribeAllEnemies()
        {
            if (stages == null) return;
            for (int i = 0; i < stages.Length; i++)
            {
                JourneyEncounterStage stage = stages[i];
                if (stage?.enemies == null) continue;
                for (int j = 0; j < stage.enemies.Length; j++)
                    if (stage.enemies[j] != null) stage.enemies[j].Defeated -= OnEnemyDefeated;
            }
        }
    }
}
