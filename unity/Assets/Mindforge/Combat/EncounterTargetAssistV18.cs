using System.Collections;
using Mindforge.Journey;
using Mindforge.Presentation;
using Mindforge.SoulWisp;
using Mindforge.Telemetry;
using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>
    /// Conventional encounter-framing assist for the Latest demo.
    ///
    /// It only acts while the player has no target lock. Bosses inside the authored encounter
    /// envelope are preferred, followed by a small set of high-information Journey enemies.
    /// A manual target-lock release suppresses automatic reacquisition for a grace period so
    /// the player always retains an escape hatch. The assist is frozen for every neural visual
    /// interval so SSVEP geometry can never be changed by targeting automation mid-window.
    ///
    /// This component is conventional game/camera assistance. It consumes no EEG/gaze evidence
    /// and never creates attacks, damage, buffs, neural selections, or stimulus timing.
    /// </summary>
    [DefaultExecutionOrder(80)]
    public sealed class EncounterTargetAssistV18 : MonoBehaviour
    {
        public const string RootName = "Mindforge_EncounterTargetAssist_V18";

        [SerializeField] private GuardianCombatInput input;
        [SerializeField] private GuardianTargetLock targetLock;
        [SerializeField] private SoulWispController wisp;
        [SerializeField] private UdpGameMarkerSender markers;

        [Header("Encounter framing")]
        [SerializeField] private float bossAutoLockRange = 23.5f;
        [SerializeField] private float priorityEnemyAutoLockRange = 13.5f;
        [SerializeField] private float manualReleaseGraceSeconds = 8.0f;
        [SerializeField] private bool autoLockBosses = true;
        [SerializeField] private bool autoLockPriorityEnemies = true;

        private float _manualReleaseUntil;
        private Transform _lastAutoTarget;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<MindforgeDemoV11Marker>(true) == null) return;
            if (FindObjectOfType<EncounterTargetAssistV18>(true) != null) return;
            new GameObject(RootName).AddComponent<EncounterTargetAssistV18>();
        }

        private IEnumerator Start()
        {
            for (int frame = 0; frame < 240; frame++)
            {
                Resolve();
                if (input != null && targetLock != null) yield break;
                yield return null;
            }

            Debug.LogError("[Mindforge:TargetAssist] Guardian input/target lock could not be resolved; assist disabled.");
            enabled = false;
        }

        private void Update()
        {
            if (input == null || targetLock == null) Resolve();
            if (input == null || targetLock == null || !input.CombatActionsEnabled) return;

            GuardianControlProfileV1 controls = GuardianControlProfileV1.ResolveOrCreate();
            if (controls != null && controls.Pressed(GuardianControlAction.TargetLock))
            {
                // GuardianTargetLock executes first. If the lock is now absent, this was an
                // explicit player release and automation backs off rather than immediately
                // recreating the lock on the same frame.
                if (!targetLock.Locked)
                {
                    _manualReleaseUntil = Time.unscaledTime + Mathf.Max(0.25f, manualReleaseGraceSeconds);
                }
                else
                {
                    _manualReleaseUntil = 0f;
                    _lastAutoTarget = null;
                }
                return;
            }

            if (NeuralVisualFieldActive() || targetLock.Locked || Time.unscaledTime < _manualReleaseUntil)
                return;

            Transform candidate = null;
            string reason = string.Empty;
            float distance = float.PositiveInfinity;

            if (autoLockBosses)
                candidate = FindBossCandidate(out distance);
            if (candidate != null)
            {
                reason = "boss_encounter_auto_lock";
            }
            else if (autoLockPriorityEnemies)
            {
                candidate = FindPriorityJourneyCandidate(out distance);
                if (candidate != null) reason = "priority_enemy_auto_lock";
            }

            if (candidate == null || candidate == _lastAutoTarget && Time.unscaledTime < _manualReleaseUntil)
                return;

            if (!targetLock.TryLockTarget(candidate, reason)) return;

            _lastAutoTarget = candidate;
            markers?.Emit(
                "TARGET_LOCK_ASSIST",
                "targeting",
                target: candidate.name,
                reason: reason,
                value: distance);
            Debug.Log($"[Mindforge:TargetAssist] {reason} -> {candidate.name} at {distance:0.0}m.");
        }

        private void Resolve()
        {
            if (input == null) input = FindObjectOfType<GuardianCombatInput>(true);
            if (input != null && targetLock == null) targetLock = input.GetComponent<GuardianTargetLock>();
            if (wisp == null) wisp = FindObjectOfType<SoulWispController>(true);
            if (markers == null) markers = FindObjectOfType<UdpGameMarkerSender>(true);
        }

        private Transform FindBossCandidate(out float bestDistance)
        {
            bestDistance = float.PositiveInfinity;
            Transform best = null;
            FracturedSignalDirector[] bosses = FindObjectsOfType<FracturedSignalDirector>(true);
            for (int i = 0; i < bosses.Length; i++)
            {
                FracturedSignalDirector boss = bosses[i];
                if (boss == null || !boss.gameObject.activeInHierarchy) continue;
                CombatantVitals vitals = boss.GetComponent<CombatantVitals>();
                if (vitals == null) vitals = boss.GetComponentInParent<CombatantVitals>();
                if (vitals == null || vitals.Team != CombatTeam.Enemy || !vitals.IsAlive) continue;

                float distance = HorizontalDistance(input.transform, vitals.transform);
                if (distance > Mathf.Max(1f, bossAutoLockRange) || distance >= bestDistance) continue;
                bestDistance = distance;
                best = vitals.transform;
            }
            return best;
        }

        private Transform FindPriorityJourneyCandidate(out float selectedDistance)
        {
            selectedDistance = float.PositiveInfinity;
            Transform best = null;
            float bestScore = float.NegativeInfinity;
            JourneyEnemyController[] enemies = FindObjectsOfType<JourneyEnemyController>(true);
            for (int i = 0; i < enemies.Length; i++)
            {
                JourneyEnemyController enemy = enemies[i];
                if (enemy == null || !enemy.gameObject.activeInHierarchy || !enemy.IsAlive || !enemy.Armed) continue;

                int priority = PriorityFor(enemy.Archetype);
                if (priority <= 0) continue;
                float distance = HorizontalDistance(input.transform, enemy.transform);
                if (distance > Mathf.Max(1f, priorityEnemyAutoLockRange)) continue;

                float telegraphBonus = enemy.PendingAttack != JourneyEnemyAttackKind.None ? 18f : 0f;
                float score = priority + telegraphBonus - distance * 2.2f;
                if (score <= bestScore) continue;
                bestScore = score;
                selectedDistance = distance;
                best = enemy.transform;
            }
            return best;
        }

        private static int PriorityFor(JourneyEnemyArchetype archetype)
        {
            switch (archetype)
            {
                case JourneyEnemyArchetype.SignalWarden: return 70;
                case JourneyEnemyArchetype.ChromePenitent: return 58;
                case JourneyEnemyArchetype.NullSentry: return 52;
                default: return 0;
            }
        }

        private bool NeuralVisualFieldActive()
        {
            return wisp != null && (wisp.CalibrationStimuliActive || wisp.ResonanceWindowActive);
        }

        private static float HorizontalDistance(Transform a, Transform b)
        {
            if (a == null || b == null) return float.PositiveInfinity;
            Vector3 delta = b.position - a.position;
            delta.y = 0f;
            return delta.magnitude;
        }
    }
}
