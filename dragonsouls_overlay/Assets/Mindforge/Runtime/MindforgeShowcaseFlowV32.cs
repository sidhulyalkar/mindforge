using System;
using States;
using UnityEngine;

namespace Mindforge.Chassis
{
    public enum MindforgeShowcaseStageV32
    {
        Awakening = 0,
        MemoryForge = 1,
        BladeTraining = 2,
        FirstEncounter = 3,
        BciReveal = 4,
        SightPuzzle = 5,
        Traversal = 6,
        EliteEncounter = 7,
        BossApproach = 8,
        BossFight = 9,
        WorldReveal = 10,
    }

    [Flags]
    public enum MindforgeShowcaseMilestoneV32
    {
        None = 0,
        FirstSwingWindow = 1 << 0,
        FirstSwordHit = 1 << 1,
        BciOrbRevealed = 1 << 2,
        TargetLockCombat = 1 << 3,
        DodgeObserved = 1 << 4,
        BossEntered = 1 << 5,
        BossDefeated = 1 << 6,
    }

    /// <summary>
    /// Monotonic presentation/progression observer for the V0.32 showcase chapter.
    /// It never moves the player, selects an attack, changes damage, writes stamina,
    /// or changes Dragon Souls combat state. Spatial checkpoint triggers and existing
    /// combat instrumentation feed evidence into this one chapter-level authority.
    /// </summary>
    [DefaultExecutionOrder(980)]
    [DisallowMultipleComponent]
    public sealed class MindforgeShowcaseFlowV32 : MonoBehaviour
    {
        public const string ProductVersion = "V0.32 Showcase Intro";

        private MindforgeSwordCombatAssuranceV31 _swordAssurance;
        private MindforgeBciOrbV31 _bciOrb;
        private GameObject _bciVisual;
        private int _lastObservedSwingWindows;
        private int _lastObservedHits;
        private float _stageStartedAt;

        public MindforgeShowcaseStageV32 CurrentStage { get; private set; } = MindforgeShowcaseStageV32.Awakening;
        public MindforgeShowcaseStageV32 HighestArrivedStage { get; private set; } = MindforgeShowcaseStageV32.Awakening;
        public MindforgeShowcaseMilestoneV32 Milestones { get; private set; } = MindforgeShowcaseMilestoneV32.None;
        public bool Installed { get; private set; }
        public float CurrentStageElapsed => Time.unscaledTime - _stageStartedAt;

        public event Action<MindforgeShowcaseStageV32> StageChanged;
        public event Action<MindforgeShowcaseMilestoneV32> MilestoneObserved;

        private void Start()
        {
            _swordAssurance = FindObjectOfType<MindforgeSwordCombatAssuranceV31>(true);
            _bciOrb = FindObjectOfType<MindforgeBciOrbV31>(true);
            _stageStartedAt = Time.unscaledTime;
            ResolveBciVisual();
            SetBciVisualVisible(false);
            Installed = FindObjectOfType<PlayerStateMachine>(true) != null;
            Debug.Log("[Mindforge:V32] Showcase flow armed at AWAKENING. Combat remains Dragon Souls-authoritative.");
        }

        private void Update()
        {
            ObserveCombatEvidence();
        }

        public void ObserveStageArrival(MindforgeShowcaseStageV32 stage)
        {
            if (stage > HighestArrivedStage)
                HighestArrivedStage = stage;

            if (stage <= CurrentStage)
                return;

            CurrentStage = stage;
            _stageStartedAt = Time.unscaledTime;
            if (stage >= MindforgeShowcaseStageV32.BciReveal)
            {
                SetBciVisualVisible(true);
                ObserveMilestone(MindforgeShowcaseMilestoneV32.BciOrbRevealed);
            }

            StageChanged?.Invoke(stage);
            Debug.Log($"[Mindforge:V32] Showcase stage -> {stage}.");
        }

        public bool HasMilestone(MindforgeShowcaseMilestoneV32 milestone)
        {
            return (Milestones & milestone) == milestone;
        }

        public void ObserveMilestone(MindforgeShowcaseMilestoneV32 milestone)
        {
            if (milestone == MindforgeShowcaseMilestoneV32.None || HasMilestone(milestone))
                return;
            Milestones |= milestone;
            MilestoneObserved?.Invoke(milestone);
        }

        private void ObserveCombatEvidence()
        {
            if (_swordAssurance == null)
            {
                _swordAssurance = FindObjectOfType<MindforgeSwordCombatAssuranceV31>(true);
                if (_swordAssurance == null) return;
            }

            if (_swordAssurance.SwingWindowsObserved > _lastObservedSwingWindows)
            {
                _lastObservedSwingWindows = _swordAssurance.SwingWindowsObserved;
                ObserveMilestone(MindforgeShowcaseMilestoneV32.FirstSwingWindow);
            }
            if (_swordAssurance.HitsObserved > _lastObservedHits)
            {
                _lastObservedHits = _swordAssurance.HitsObserved;
                ObserveMilestone(MindforgeShowcaseMilestoneV32.FirstSwordHit);
            }
        }

        private void ResolveBciVisual()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            Transform visual = camera.transform.Find("Mindforge_BCI_Orb_V31");
            if (visual != null) _bciVisual = visual.gameObject;
        }

        private void SetBciVisualVisible(bool visible)
        {
            if (_bciVisual == null) ResolveBciVisual();
            if (_bciVisual != null) _bciVisual.SetActive(visible);
        }
    }
}
