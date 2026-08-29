using System;
using UnityEngine;

namespace Mindforge.World
{
    public enum HackathonPlaythroughStage
    {
        Arrival = 0,
        Causeway = 1,
        BrokenMomentum = 2,
        RuinedChoir = 3,
        Gravitas = 4,
        Crucible = 5,
        Aftermath = 6,
    }

    /// <summary>
    /// Monotonic high-level playthrough state for the hackathon vertical slice.
    /// This is deliberately not a combat, movement, spawn, checkpoint or neural authority.
    /// It only translates existing world position + Menagerie completion into a stable story
    /// progression signal that future quests, VO, analytics and esports observers can consume.
    /// </summary>
    [DefaultExecutionOrder(950)]
    public sealed class HackathonPlaythroughDirectorV1 : MonoBehaviour
    {
        [SerializeField] private Transform guardian;
        [SerializeField] private ArenaMenagerieDirector menagerie;
        [SerializeField] private HackathonPlaythroughStage stage = HackathonPlaythroughStage.Arrival;

        public event Action<HackathonPlaythroughStage, HackathonPlaythroughStage> StageChanged;

        public HackathonPlaythroughStage Stage => stage;
        public int StageIndex => (int)stage;
        public bool EncounterCleared => menagerie != null && menagerie.Complete;

        public void ConfigureRuntime(Transform guardianTransform, ArenaMenagerieDirector encounter)
        {
            guardian = guardianTransform;
            menagerie = encounter;
            stage = HackathonPlaythroughStage.Arrival;
        }

        private void Awake() => Resolve();
        private void OnEnable()
        {
            Resolve();
            if (menagerie != null) menagerie.Completed += OnEncounterCompleted;
        }

        private void OnDisable()
        {
            if (menagerie != null) menagerie.Completed -= OnEncounterCompleted;
        }

        private void Update()
        {
            Resolve();
            if (guardian == null) return;

            HackathonPlaythroughStage candidate = ResolveFromPosition(guardian.position.z);
            if (menagerie != null && menagerie.Complete)
                candidate = HackathonPlaythroughStage.Aftermath;

            // Progress is intentionally monotonic. Backtracking changes location, not story state.
            if ((int)candidate > (int)stage)
                Advance(candidate);
        }

        private void OnEncounterCompleted()
        {
            Advance(HackathonPlaythroughStage.Aftermath);
        }

        private void Advance(HackathonPlaythroughStage next)
        {
            if ((int)next <= (int)stage) return;
            HackathonPlaythroughStage before = stage;
            stage = next;
            StageChanged?.Invoke(before, stage);
            Debug.Log($"[Mindforge:Playthrough] Stage {before} -> {stage}");
        }

        private static HackathonPlaythroughStage ResolveFromPosition(float z)
        {
            if (z >= 12f) return HackathonPlaythroughStage.Crucible;
            if (z >= 0f) return HackathonPlaythroughStage.Gravitas;
            if (z >= -18f) return HackathonPlaythroughStage.RuinedChoir;
            if (z >= -36f) return HackathonPlaythroughStage.BrokenMomentum;
            if (z >= -50f) return HackathonPlaythroughStage.Causeway;
            return HackathonPlaythroughStage.Arrival;
        }

        private void Resolve()
        {
            if (guardian == null)
            {
                GameObject player = GameObject.Find("Guardian");
                if (player != null) guardian = player.transform;
            }
            if (menagerie == null)
                menagerie = FindObjectOfType<ArenaMenagerieDirector>(true);
        }
    }
}
