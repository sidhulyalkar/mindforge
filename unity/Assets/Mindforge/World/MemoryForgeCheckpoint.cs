using System;
using UnityEngine;
using Mindforge.Combat;
using Mindforge.Telemetry;

namespace Mindforge.World
{
    /// <summary>
    /// Null Ward checkpoint. It reconstructs physical encounter state only; BCI
    /// calibration remains an explicit scientific workflow outside this fantasy layer.
    /// </summary>
    public sealed class MemoryForgeCheckpoint : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Rigidbody playerBody;
        [SerializeField] private CombatantVitals playerVitals;
        [SerializeField] private GuardianStamina guardIntegrity;
        [SerializeField] private GuardianTargetLock targetLock;
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private Transform interactionPoint;
        [SerializeField] private NullWardEncounterDirector world;
        [SerializeField] private UdpGameMarkerSender markers;
        [SerializeField] private KeyCode interactKey = KeyCode.G;
        [SerializeField] private float interactionRadius = 2.35f;
        [SerializeField, Min(1)] private int respawnDelayTicks = 54;

        private bool _active;
        private bool _respawnPending;
        private long _respawnAtTick;
        private GUIStyle _promptStyle;

        public event Action Activated;
        public event Action Respawned;
        public bool Active => _active;
        public bool RespawnPending => _respawnPending;

        private long FixedTick
        {
            get
            {
                float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
                return (long)Math.Round(Time.fixedTime / dt);
            }
        }

        public void ConfigureRuntime(
            Transform guardian,
            CombatantVitals vitals,
            GuardianStamina integrity,
            GuardianTargetLock lockState,
            Transform spawn,
            Transform interaction,
            NullWardEncounterDirector director,
            UdpGameMarkerSender markerSender = null)
        {
            Unsubscribe();
            player = guardian;
            playerBody = guardian != null ? guardian.GetComponent<Rigidbody>() : null;
            playerVitals = vitals;
            guardIntegrity = integrity;
            targetLock = lockState;
            respawnPoint = spawn;
            interactionPoint = interaction;
            world = director;
            markers = markerSender;
            Subscribe();
        }

        private void OnEnable()
        {
            Resolve();
            Subscribe();
        }

        private void OnDisable() => Unsubscribe();

        private void Update()
        {
            if (!_active || _respawnPending || !PlayerInRange()) return;
            if (Input.GetKeyDown(interactKey)) RestAndReconstruct();
        }

        private void FixedUpdate()
        {
            if (!_respawnPending || FixedTick < _respawnAtTick) return;
            _respawnPending = false;
            world?.ResetForCheckpoint();
            RestoreGuardian(true);
            markers?.Emit("CHECKPOINT_RESPAWN", "world", target: "MEMORY_FORGE", reason: "PHYSICAL_RECONSTRUCTION");
            Respawned?.Invoke();
        }

        public void PrimeAsStartingCheckpoint()
        {
            if (_active) return;
            _active = true;
            markers?.Emit("CHECKPOINT_ACTIVATED", "world", target: "MEMORY_FORGE", reason: "WORLD_ENTRY");
            Activated?.Invoke();
        }

        public void RestAndReconstruct()
        {
            if (!_active) PrimeAsStartingCheckpoint();
            world?.ResetOrdinaryEncounters();
            RestoreGuardian(false);
            markers?.Emit("CHECKPOINT_REST", "world", target: "MEMORY_FORGE", reason: "CONVENTIONAL_INTERACTION");
        }

        private void OnPlayerDied()
        {
            if (!_active || _respawnPending) return;
            _respawnPending = true;
            _respawnAtTick = FixedTick + Mathf.Max(1, respawnDelayTicks);
            targetLock?.SetLocked(false);
            world?.PrepareForRespawn();
            markers?.Emit("CHECKPOINT_RESPAWN_PENDING", "world", target: "MEMORY_FORGE", reason: "GUARDIAN_DEFEATED");
        }

        private void RestoreGuardian(bool relocate)
        {
            Resolve();
            playerVitals?.ResetForCheckpoint(true);
            guardIntegrity?.ResetFull();
            targetLock?.SetLocked(false);

            if (relocate && player != null && respawnPoint != null)
            {
                if (playerBody != null)
                {
                    playerBody.position = respawnPoint.position;
                    playerBody.rotation = respawnPoint.rotation;
                    playerBody.velocity = Vector3.zero;
                    playerBody.angularVelocity = Vector3.zero;
                    playerBody.WakeUp();
                }
                else
                {
                    player.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);
                }
                Physics.SyncTransforms();
            }
        }

        private bool PlayerInRange()
        {
            if (player == null || interactionPoint == null) return false;
            Vector3 delta = Vector3.ProjectOnPlane(player.position - interactionPoint.position, Vector3.up);
            float radius = Mathf.Max(0.5f, interactionRadius);
            return delta.sqrMagnitude <= radius * radius;
        }

        private void Resolve()
        {
            if (player != null)
            {
                if (playerBody == null) playerBody = player.GetComponent<Rigidbody>();
                if (playerVitals == null) playerVitals = player.GetComponent<CombatantVitals>();
                if (guardIntegrity == null) guardIntegrity = player.GetComponent<GuardianStamina>();
                if (targetLock == null) targetLock = player.GetComponent<GuardianTargetLock>();
            }
        }

        private void Subscribe()
        {
            if (playerVitals == null) return;
            playerVitals.Died -= OnPlayerDied;
            playerVitals.Died += OnPlayerDied;
        }

        private void Unsubscribe()
        {
            if (playerVitals != null) playerVitals.Died -= OnPlayerDied;
        }

        private void OnGUI()
        {
            if (!_active || _respawnPending || !PlayerInRange()) return;
            if (_promptStyle == null)
            {
                _promptStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleCenter,
                };
            }
            const float width = 350f;
            GUI.Label(
                new Rect((Screen.width - width) * 0.5f, Screen.height - 92f, width, 34f),
                $"{interactKey}  RECONSTRUCT AT MEMORY FORGE",
                _promptStyle);
        }
    }
}
