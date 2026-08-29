using UnityEngine;

namespace Mindforge.World
{
    /// <summary>
    /// Translates existing concrete gameplay events into stable semantic world facts.
    /// It is an observer only: no spawning, gates, damage, movement, rewards or neural state.
    /// </summary>
    [DefaultExecutionOrder(-760)]
    public sealed class HackathonWorldSemanticBridgeV1 : MonoBehaviour
    {
        [SerializeField] private WorldSignalBus signals;
        [SerializeField] private WorldStateLedger ledger;
        [SerializeField] private HackathonPlaythroughDirectorV1 playthrough;
        [SerializeField] private ArenaMenagerieDirector menagerie;
        [SerializeField] private NullWardEncounterDirector nullWard;
        [SerializeField] private MemoryForgeCheckpoint checkpoint;

        private bool _subscribed;

        public void ConfigureRuntime(
            WorldSignalBus signalBus,
            WorldStateLedger stateLedger,
            HackathonPlaythroughDirectorV1 playthroughDirector,
            ArenaMenagerieDirector menagerieDirector,
            NullWardEncounterDirector worldDirector,
            MemoryForgeCheckpoint memoryForge)
        {
            Unsubscribe();
            signals = signalBus;
            ledger = stateLedger;
            playthrough = playthroughDirector;
            menagerie = menagerieDirector;
            nullWard = worldDirector;
            checkpoint = memoryForge;
            Subscribe();
            SeedCurrentFacts();
        }

        private void Awake() => Resolve();

        private void OnEnable()
        {
            Resolve();
            Subscribe();
            SeedCurrentFacts();
        }

        private void OnDisable() => Unsubscribe();

        private void Resolve()
        {
            if (signals == null) signals = GetComponent<WorldSignalBus>();
            if (ledger == null) ledger = GetComponent<WorldStateLedger>();
            if (playthrough == null) playthrough = FindObjectOfType<HackathonPlaythroughDirectorV1>(true);
            if (menagerie == null) menagerie = FindObjectOfType<ArenaMenagerieDirector>(true);
            if (nullWard == null) nullWard = FindObjectOfType<NullWardEncounterDirector>(true);
            if (checkpoint == null) checkpoint = FindObjectOfType<MemoryForgeCheckpoint>(true);
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            Resolve();
            if (playthrough != null) playthrough.StageChanged += OnStageChanged;
            if (menagerie != null)
            {
                menagerie.WaveStarted += OnMenagerieWaveStarted;
                menagerie.WaveCleared += OnMenagerieWaveCleared;
                menagerie.Completed += OnMenagerieCompleted;
            }
            if (nullWard != null)
            {
                nullWard.ZoneStarted += OnZoneStarted;
                nullWard.ZoneCleared += OnZoneCleared;
                nullWard.ProtocolUnlocked += OnProtocolUnlocked;
                nullWard.BossStarted += OnBossStarted;
                nullWard.WorldCompleted += OnWorldCompleted;
            }
            if (checkpoint != null)
            {
                checkpoint.Activated += OnCheckpointActivated;
                checkpoint.Respawned += OnCheckpointRespawned;
            }
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (playthrough != null) playthrough.StageChanged -= OnStageChanged;
            if (menagerie != null)
            {
                menagerie.WaveStarted -= OnMenagerieWaveStarted;
                menagerie.WaveCleared -= OnMenagerieWaveCleared;
                menagerie.Completed -= OnMenagerieCompleted;
            }
            if (nullWard != null)
            {
                nullWard.ZoneStarted -= OnZoneStarted;
                nullWard.ZoneCleared -= OnZoneCleared;
                nullWard.ProtocolUnlocked -= OnProtocolUnlocked;
                nullWard.BossStarted -= OnBossStarted;
                nullWard.WorldCompleted -= OnWorldCompleted;
            }
            if (checkpoint != null)
            {
                checkpoint.Activated -= OnCheckpointActivated;
                checkpoint.Respawned -= OnCheckpointRespawned;
            }
            _subscribed = false;
        }

        private void SeedCurrentFacts()
        {
            if (ledger == null) return;
            if (playthrough != null)
            {
                ledger.SetInt("journey.stage", playthrough.StageIndex, "semantic_seed");
                ledger.SetBool("region." + playthrough.Stage.ToString().ToLowerInvariant() + ".entered", true, "semantic_seed");
            }
            if (menagerie != null && menagerie.Complete)
            {
                ledger.SetInt("encounter.menagerie.waves_cleared", 3, "semantic_seed");
                ledger.SetBool("encounter.menagerie.complete", true, "semantic_seed");
            }
            if (nullWard != null)
            {
                ledger.SetBool("world.null_ward.protocol_open", nullWard.ProtocolUnlockedState, "semantic_seed");
                ledger.SetBool("world.null_ward.complete", nullWard.Completed, "semantic_seed");
            }
            if (checkpoint != null && checkpoint.Active)
                ledger.SetBool("checkpoint.memory_forge.active", true, "semantic_seed");
        }

        private void OnStageChanged(HackathonPlaythroughStage before, HackathonPlaythroughStage after)
        {
            ledger?.SetInt("journey.stage", (int)after, "playthrough_stage");
            ledger?.SetBool("region." + after.ToString().ToLowerInvariant() + ".entered", true, "playthrough_stage");
            signals?.Publish(
                WorldSignalKind.RegionEntered,
                "region.entered",
                subject: after.ToString(),
                intValue: (int)after,
                reason: before + "->" + after);
        }

        private void OnMenagerieWaveStarted(int index)
        {
            ledger?.SetInt("encounter.menagerie.wave", index + 1, "menagerie_wave_started");
            signals?.Publish(
                WorldSignalKind.EncounterWaveStarted,
                "encounter.wave.started",
                subject: "menagerie_crucible",
                intValue: index + 1,
                reason: "authored_3_4_3");
        }

        private void OnMenagerieWaveCleared(int index)
        {
            ledger?.SetInt("encounter.menagerie.waves_cleared", index + 1, "menagerie_wave_cleared");
            signals?.Publish(
                WorldSignalKind.EncounterWaveCleared,
                "encounter.wave.cleared",
                subject: "menagerie_crucible",
                intValue: index + 1,
                floatValue: (index + 1) / 3f,
                reason: "all_active_enemies_defeated");
        }

        private void OnMenagerieCompleted()
        {
            ledger?.SetInt("encounter.menagerie.waves_cleared", 3, "menagerie_complete");
            ledger?.SetBool("encounter.menagerie.complete", true, "menagerie_complete");
            signals?.Publish(
                WorldSignalKind.EncounterCleared,
                "encounter.cleared",
                subject: "menagerie_crucible",
                intValue: 3,
                floatValue: 1f,
                reason: "ten_enemy_exam_complete");
        }

        private void OnZoneStarted(int index, string title, string lesson)
        {
            string id = ZoneId(index);
            ledger?.SetBool("encounter.null_ward." + id + ".started", true, "zone_started");
            signals?.Publish(
                WorldSignalKind.EncounterStarted,
                "encounter.started",
                subject: id,
                intValue: index,
                reason: title);
        }

        private void OnZoneCleared(int index, string id)
        {
            string normalized = string.IsNullOrWhiteSpace(id) ? ZoneId(index) : id.Trim().ToLowerInvariant();
            ledger?.SetBool("encounter.null_ward." + normalized + ".cleared", true, "zone_cleared");
            signals?.Publish(
                WorldSignalKind.EncounterCleared,
                "encounter.cleared",
                subject: normalized,
                intValue: index,
                floatValue: 1f,
                reason: "null_ward_zone_complete");
        }

        private void OnProtocolUnlocked()
        {
            ledger?.SetBool("world.null_ward.protocol_open", true, "protocol_unlocked");
            signals?.Publish(
                WorldSignalKind.Milestone,
                "world.protocol_opened",
                subject: "signal_cathedral",
                intValue: 1,
                reason: "required_zones_cleared");
        }

        private void OnBossStarted()
        {
            ledger?.SetBool("boss.malatract.started", true, "boss_started");
            signals?.Publish(
                WorldSignalKind.BossStarted,
                "boss.started",
                subject: "lord_malatract",
                reason: "cathedral_threshold");
        }

        private void OnWorldCompleted()
        {
            ledger?.SetBool("world.null_ward.complete", true, "world_complete");
            signals?.Publish(
                WorldSignalKind.WorldCompleted,
                "world.completed",
                subject: "null_ward",
                floatValue: 1f,
                reason: "lord_malatract_defeated");
        }

        private void OnCheckpointActivated()
        {
            ledger?.SetBool("checkpoint.memory_forge.active", true, "checkpoint_activated");
            signals?.Publish(
                WorldSignalKind.Checkpoint,
                "checkpoint.activated",
                subject: "memory_forge",
                reason: "world_entry");
        }

        private void OnCheckpointRespawned()
        {
            signals?.Publish(
                WorldSignalKind.Checkpoint,
                "checkpoint.respawned",
                subject: "memory_forge",
                reason: "guardian_reconstructed");
        }

        private string ZoneId(int index)
        {
            if (nullWard != null && nullWard.Zones != null && index >= 0 && index < nullWard.Zones.Length)
            {
                NullWardEncounterZone zone = nullWard.Zones[index];
                if (zone != null && !string.IsNullOrWhiteSpace(zone.id)) return zone.id.Trim().ToLowerInvariant();
            }
            return "zone_" + Mathf.Max(0, index);
        }
    }
}
