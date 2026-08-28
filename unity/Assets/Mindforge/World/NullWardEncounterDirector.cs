using System;
using UnityEngine;
using Mindforge.Combat;
using Mindforge.Journey;
using Mindforge.SoulWisp;
using Mindforge.Telemetry;

namespace Mindforge.World
{
    [Serializable]
    public sealed class NullWardEncounterZone
    {
        public string id = "zone";
        public string title = "NULL WARD";
        [TextArea] public string lesson = "Read the room.";
        public Transform activationPoint;
        public float activationRadius = 6f;
        public bool requiredForProtocol = true;
        public JourneyEnemyController[] enemies = Array.Empty<JourneyEnemyController>();
        public FracturedEchoNode[] echoes = Array.Empty<FracturedEchoNode>();

        [NonSerialized] public bool started;
        [NonSerialized] public bool cleared;
    }

    /// <summary>
    /// Interconnected Null Ward world authority. It activates authored encounters,
    /// reconstructs ordinary enemies at the Memory Forge, opens the Protocol Veil after
    /// required combat spaces are understood, and hands the existing Fractured Signal
    /// boss its final threshold. It never issues player commands or neural decisions.
    /// </summary>
    public sealed class NullWardEncounterDirector : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private CombatantVitals playerVitals;
        [SerializeField] private GuardianTargetLock targetLock;
        [SerializeField] private SoulWispController soulWisp;
        [SerializeField] private Transform worldStart;
        [SerializeField] private MemoryForgeCheckpoint checkpoint;
        [SerializeField] private NullWardEncounterZone[] zones = Array.Empty<NullWardEncounterZone>();
        [SerializeField] private JourneyGate protocolVeil;
        [SerializeField] private UdpGameMarkerSender markers;

        [Header("Signal Cathedral / boss")]
        [SerializeField] private GameObject bossRoot;
        [SerializeField] private Transform bossTarget;
        [SerializeField] private CombatantVitals bossVitals;
        [SerializeField] private FracturedSignalDirector bossDirector;
        [SerializeField] private Transform bossActivationPoint;
        [SerializeField] private float bossActivationRadius = 4.8f;
        [SerializeField] private JourneyGate bossSeal;

        [Header("World entry")]
        [SerializeField] private bool repositionPlayerWhenCombatWorldOpens = true;
        [SerializeField] private string initialObjective = "MEMORY FORGE · FOLLOW THE CONDUIT INTO THE NULL WARD";

        private bool _initialized;
        private bool _protocolUnlocked;
        private bool _bossStarted;
        private bool _completed;

        public event Action<int, string, string> ZoneStarted;
        public event Action<int, string> ZoneCleared;
        public event Action ProtocolUnlocked;
        public event Action BossStarted;
        public event Action WorldCompleted;
        public event Action<string> ObjectiveChanged;

        public string CurrentObjective { get; private set; }
        public bool ProtocolUnlockedState => _protocolUnlocked;
        public bool BossActive => _bossStarted && bossRoot != null && bossRoot.activeInHierarchy;
        public bool Completed => _completed;
        public NullWardEncounterZone[] Zones => zones;

        public void ConfigureRuntime(
            Transform guardian,
            CombatantVitals guardianVitals,
            GuardianTargetLock lockState,
            SoulWispController wisp,
            Transform start,
            MemoryForgeCheckpoint memoryForge,
            NullWardEncounterZone[] encounterZones,
            JourneyGate cathedralVeil,
            GameObject finalBossRoot,
            Transform finalBossTarget,
            CombatantVitals finalBossVitals,
            FracturedSignalDirector finalBossDirector,
            Transform activationPoint,
            JourneyGate arenaSeal,
            UdpGameMarkerSender markerSender = null)
        {
            player = guardian;
            playerVitals = guardianVitals;
            targetLock = lockState;
            soulWisp = wisp;
            worldStart = start;
            checkpoint = memoryForge;
            zones = encounterZones ?? Array.Empty<NullWardEncounterZone>();
            protocolVeil = cathedralVeil;
            bossRoot = finalBossRoot;
            bossTarget = finalBossTarget;
            bossVitals = finalBossVitals;
            bossDirector = finalBossDirector;
            bossActivationPoint = activationPoint;
            bossSeal = arenaSeal;
            markers = markerSender;
        }

        private void Start() => InitializeWorld();

        private void OnDisable()
        {
            UnsubscribeZones();
            if (bossVitals != null) bossVitals.Died -= OnBossDied;
        }

        private void FixedUpdate()
        {
            if (!_initialized) InitializeWorld();
            if (_completed || player == null || playerVitals == null || !playerVitals.IsAlive) return;

            for (int i = 0; i < zones.Length; i++)
            {
                NullWardEncounterZone zone = zones[i];
                if (zone == null || zone.cleared) continue;
                if (!zone.started && IsNear(zone.activationPoint, zone.activationRadius))
                    BeginZone(i);
                if (zone.started && !zone.cleared && IsZoneCleared(zone))
                    CompleteZone(i);
            }

            if (!_protocolUnlocked && RequiredZonesCleared()) UnlockProtocol();
            if (_protocolUnlocked && !_bossStarted && IsNear(bossActivationPoint, bossActivationRadius))
                StartBossEncounter();
        }

        public void PrepareForRespawn()
        {
            SetOrdinaryEnemyPause(true);
            bossDirector?.SetExternalPause(true);
            targetLock?.SetLocked(false);
            markers?.Emit("WORLD_RESPAWN_PREPARE", "world", target: "NULL_WARD", reason: "GUARDIAN_DEFEATED");
        }

        public void ResetOrdinaryEncounters()
        {
            if (_completed) return;
            ResetZones();
            _protocolUnlocked = false;
            protocolVeil?.SetOpen(false, true);
            SetObjective(initialObjective);
            markers?.Emit("WORLD_ENCOUNTERS_RESET", "world", target: "NULL_WARD", reason: "MEMORY_FORGE_RECONSTRUCTION");
        }

        public void ResetForCheckpoint()
        {
            if (_completed) return;
            ResetZones();
            _protocolUnlocked = false;
            protocolVeil?.SetOpen(false, true);
            ResetBossEncounter();
            targetLock?.SetLocked(false);
            targetLock?.Configure(bossTarget);
            soulWisp?.SetTarget(null);
            SetObjective(initialObjective);
            markers?.Emit("WORLD_CHECKPOINT_RESET", "world", target: "NULL_WARD", reason: "MEMORY_FORGE_RECONSTRUCTION");
        }

        public void SetExternalPause(bool paused)
        {
            SetOrdinaryEnemyPause(paused);
            if (_bossStarted) bossDirector?.SetExternalPause(paused);
        }

        private void InitializeWorld()
        {
            if (_initialized) return;
            _initialized = true;
            _protocolUnlocked = false;
            _bossStarted = false;
            _completed = false;

            if (targetLock == null && player != null) targetLock = player.GetComponent<GuardianTargetLock>();
            targetLock?.SetLocked(false);
            targetLock?.Configure(bossTarget);
            soulWisp?.SetTarget(null);

            EnterWorldStart();
            ResetZones();
            protocolVeil?.SetOpen(false, true);
            bossSeal?.SetOpen(true, true);
            ResetBossEncounter();

            if (bossVitals != null)
            {
                bossVitals.Died -= OnBossDied;
                bossVitals.Died += OnBossDied;
            }

            checkpoint?.PrimeAsStartingCheckpoint();
            SetObjective(initialObjective);
            markers?.Emit("NULL_WARD_ENTERED", "world", target: "MEMORY_FORGE", reason: "COMBAT_WORLD_OPEN");
        }

        private void EnterWorldStart()
        {
            if (!repositionPlayerWhenCombatWorldOpens || player == null || worldStart == null) return;
            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = worldStart.position;
                body.rotation = worldStart.rotation;
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.WakeUp();
            }
            else
            {
                player.SetPositionAndRotation(worldStart.position, worldStart.rotation);
            }
            Physics.SyncTransforms();
        }

        private void BeginZone(int index)
        {
            if (index < 0 || index >= zones.Length) return;
            NullWardEncounterZone zone = zones[index];
            if (zone == null || zone.started || zone.cleared) return;
            zone.started = true;

            if (zone.enemies != null)
            {
                for (int i = 0; i < zone.enemies.Length; i++)
                {
                    JourneyEnemyController enemy = zone.enemies[i];
                    if (enemy == null) continue;
                    if (!enemy.gameObject.activeSelf) enemy.gameObject.SetActive(true);
                    enemy.Defeated -= OnEnemyDefeated;
                    enemy.Defeated += OnEnemyDefeated;
                    enemy.Arm();
                }
            }
            if (zone.echoes != null)
            {
                for (int i = 0; i < zone.echoes.Length; i++)
                {
                    FracturedEchoNode echo = zone.echoes[i];
                    if (echo == null) continue;
                    if (!echo.gameObject.activeSelf) echo.gameObject.SetActive(true);
                    echo.SetExternalPause(false);
                }
            }

            string objective = string.IsNullOrWhiteSpace(zone.lesson)
                ? zone.title
                : zone.title + " · " + zone.lesson;
            SetObjective(objective);
            markers?.Emit("WORLD_ZONE_STARTED", "world", target: zone.id, reason: zone.title);
            ZoneStarted?.Invoke(index, zone.title, zone.lesson);
        }

        private void CompleteZone(int index)
        {
            if (index < 0 || index >= zones.Length) return;
            NullWardEncounterZone zone = zones[index];
            if (zone == null || zone.cleared) return;
            zone.cleared = true;
            UnsubscribeZone(zone);
            markers?.Emit("WORLD_ZONE_CLEARED", "world", target: zone.id, reason: zone.title);
            ZoneCleared?.Invoke(index, zone.id);
            SetObjective(_protocolUnlocked ? "SIGNAL CATHEDRAL · CROSS THE PROTOCOL VEIL" : "EXPLORE THE NULL WARD · FIND THE SOURCE");
        }

        private void UnlockProtocol()
        {
            _protocolUnlocked = true;
            protocolVeil?.SetOpen(true);
            SetObjective("SIGNAL CATHEDRAL · THE PROTOCOL VEIL IS OPEN");
            markers?.Emit("PROTOCOL_VEIL_OPENED", "world", target: "SIGNAL_CATHEDRAL", reason: "REQUIRED_ZONES_CLEARED");
            ProtocolUnlocked?.Invoke();
        }

        private void StartBossEncounter()
        {
            if (_bossStarted || !_protocolUnlocked) return;
            _bossStarted = true;
            bossSeal?.SetOpen(false);
            if (bossRoot != null) bossRoot.SetActive(true);
            targetLock?.Configure(bossTarget);
            soulWisp?.SetTarget(bossTarget);
            SetObjective("THE FRACTURED SIGNAL · BREAK THE SOURCE");
            markers?.Emit("BOSS_THRESHOLD_CROSSED", "world", target: "THE_FRACTURED_SIGNAL", reason: "CONVENTIONAL_TRAVERSAL");
            BossStarted?.Invoke();
        }

        private void OnBossDied()
        {
            if (_completed) return;
            _completed = true;
            bossSeal?.SetOpen(true);
            protocolVeil?.SetOpen(true);
            targetLock?.SetLocked(false);
            soulWisp?.SetTarget(null);
            SetObjective("THE SIGNAL IS QUIET · NULL WARD RECONNECTED");
            markers?.Emit("NULL_WARD_COMPLETE", "world", target: "THE_FRACTURED_SIGNAL", reason: "VICTORY");
            WorldCompleted?.Invoke();
        }

        private void ResetZones()
        {
            UnsubscribeZones();
            if (zones == null) return;
            for (int i = 0; i < zones.Length; i++)
            {
                NullWardEncounterZone zone = zones[i];
                if (zone == null) continue;
                zone.started = false;
                zone.cleared = false;
                if (zone.enemies != null)
                {
                    for (int j = 0; j < zone.enemies.Length; j++)
                    {
                        JourneyEnemyController enemy = zone.enemies[j];
                        if (enemy == null) continue;
                        enemy.ConfigureCheckpointLifecycle(true);
                        enemy.ResetForCheckpoint();
                        enemy.Disarm();
                        enemy.gameObject.SetActive(false);
                    }
                }
                if (zone.echoes != null)
                {
                    for (int j = 0; j < zone.echoes.Length; j++)
                    {
                        FracturedEchoNode echo = zone.echoes[j];
                        if (echo == null) continue;
                        echo.ResetForCheckpoint();
                        echo.SetExternalPause(true);
                        echo.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void ResetBossEncounter()
        {
            _bossStarted = false;
            if (bossRoot != null) bossRoot.SetActive(false);
            bossVitals?.ResetForCheckpoint(true);
            bossDirector?.ResetForCheckpoint();
            bossDirector?.SetExternalPause(false);
            bossSeal?.SetOpen(true, true);
        }

        private void SetOrdinaryEnemyPause(bool paused)
        {
            if (zones == null) return;
            for (int i = 0; i < zones.Length; i++)
            {
                NullWardEncounterZone zone = zones[i];
                if (zone == null) continue;
                if (zone.enemies != null)
                    for (int j = 0; j < zone.enemies.Length; j++)
                        if (zone.enemies[j] != null && zone.enemies[j].gameObject.activeInHierarchy)
                            zone.enemies[j].SetExternalPause(paused);
                if (zone.echoes != null)
                    for (int j = 0; j < zone.echoes.Length; j++)
                        if (zone.echoes[j] != null && zone.echoes[j].gameObject.activeInHierarchy)
                            zone.echoes[j].SetExternalPause(paused);
            }
        }

        private void OnEnemyDefeated(JourneyEnemyController enemy)
        {
            for (int i = 0; i < zones.Length; i++)
            {
                NullWardEncounterZone zone = zones[i];
                if (zone != null && zone.started && !zone.cleared && IsZoneCleared(zone))
                    CompleteZone(i);
            }
        }

        private bool RequiredZonesCleared()
        {
            if (zones == null || zones.Length == 0) return true;
            bool foundRequired = false;
            for (int i = 0; i < zones.Length; i++)
            {
                NullWardEncounterZone zone = zones[i];
                if (zone == null || !zone.requiredForProtocol) continue;
                foundRequired = true;
                if (!zone.cleared) return false;
            }
            return foundRequired;
        }

        private static bool IsZoneCleared(NullWardEncounterZone zone)
        {
            if (zone == null) return true;
            if (zone.enemies != null)
            {
                for (int i = 0; i < zone.enemies.Length; i++)
                {
                    JourneyEnemyController enemy = zone.enemies[i];
                    if (enemy != null && enemy.Vitals != null && enemy.Vitals.IsAlive) return false;
                }
            }
            if (zone.echoes != null)
            {
                for (int i = 0; i < zone.echoes.Length; i++)
                {
                    FracturedEchoNode echo = zone.echoes[i];
                    if (echo != null && echo.Vitals != null && echo.Vitals.IsAlive) return false;
                }
            }
            return true;
        }

        private bool IsNear(Transform point, float radius)
        {
            if (point == null || player == null) return false;
            Vector3 delta = Vector3.ProjectOnPlane(point.position - player.position, Vector3.up);
            float r = Mathf.Max(0.5f, radius);
            return delta.sqrMagnitude <= r * r;
        }

        private void UnsubscribeZones()
        {
            if (zones == null) return;
            for (int i = 0; i < zones.Length; i++) UnsubscribeZone(zones[i]);
        }

        private void UnsubscribeZone(NullWardEncounterZone zone)
        {
            if (zone?.enemies == null) return;
            for (int i = 0; i < zone.enemies.Length; i++)
                if (zone.enemies[i] != null) zone.enemies[i].Defeated -= OnEnemyDefeated;
        }

        private void SetObjective(string value)
        {
            CurrentObjective = value ?? string.Empty;
            ObjectiveChanged?.Invoke(CurrentObjective);
        }
    }
}
