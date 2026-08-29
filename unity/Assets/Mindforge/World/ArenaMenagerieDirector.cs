using System;
using UnityEngine;
using Mindforge.Journey;

namespace Mindforge.World
{
    /// <summary>
    /// Demo-only encounter scheduler for the Menagerie Crucible. JourneyEnemyController
    /// remains the sole ordinary-enemy combat authority; this component only decides when
    /// authored enemies are active. It never moves the Guardian, resolves damage, or reads
    /// neural evidence. Waves are fixed-tick deterministic so capture/replay timing is stable.
    ///
    /// Important: the scheduler intentionally does not call ResetForCheckpoint on fresh
    /// menagerie instances. That generic reset reapplies the base archetype defaults and
    /// would erase the serialized role-specific attack grammar authored by the editor pass.
    /// Rebuild the showcase scene to restart a completed menagerie run.
    /// </summary>
    public sealed class ArenaMenagerieDirector : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform activationPoint;
        [SerializeField] private float activationRadius = 7.4f;
        [SerializeField] private JourneyEnemyController[] roster = Array.Empty<JourneyEnemyController>();
        [SerializeField] private int[] waveSizes = { 3, 3, 4 };
        [SerializeField, Min(1)] private int interWaveDelayTicks = 84;

        private bool _prepared;
        private bool _started;
        private bool _completed;
        private int _waveIndex = -1;
        private int _waveStart;
        private int _waveEnd;
        private long _advanceAtTick = long.MaxValue;

        public event Action<int> WaveStarted;
        public event Action<int> WaveCleared;
        public event Action Completed;

        public bool Started => _started;
        public bool Complete => _completed;
        public int WaveIndex => _waveIndex;
        public int WaveCount => waveSizes != null ? waveSizes.Length : 0;

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
            Transform trigger,
            JourneyEnemyController[] enemies,
            int[] authoredWaveSizes = null)
        {
            player = guardian;
            activationPoint = trigger;
            roster = enemies ?? Array.Empty<JourneyEnemyController>();
            if (authoredWaveSizes != null && authoredWaveSizes.Length > 0)
                waveSizes = authoredWaveSizes;
            _prepared = false;
            PrepareRoster();
        }

        private void Start() => PrepareRoster();
        private void OnDisable() => UnsubscribeAll();

        private void FixedUpdate()
        {
            PrepareRoster();
            if (_completed || player == null || activationPoint == null) return;

            if (!_started)
            {
                if (IsNearActivation())
                {
                    _started = true;
                    StartWave(0);
                }
                return;
            }

            if (_waveIndex < 0 || _waveIndex >= WaveCount) return;
            if (!CurrentWaveCleared()) return;

            if (_advanceAtTick == long.MaxValue)
            {
                WaveCleared?.Invoke(_waveIndex);
                _advanceAtTick = FixedTick + Mathf.Max(1, interWaveDelayTicks);
                return;
            }

            if (FixedTick < _advanceAtTick) return;
            int next = _waveIndex + 1;
            if (next >= WaveCount)
            {
                _completed = true;
                _advanceAtTick = long.MaxValue;
                Completed?.Invoke();
                Debug.Log("[Mindforge:Menagerie] Crucible complete: all ten enemy identities cleared.");
                return;
            }
            StartWave(next);
        }

        private void PrepareRoster()
        {
            if (_prepared) return;
            _prepared = true;
            if (roster == null) roster = Array.Empty<JourneyEnemyController>();
            for (int i = 0; i < roster.Length; i++)
            {
                JourneyEnemyController enemy = roster[i];
                if (enemy == null) continue;
                enemy.ConfigureCheckpointLifecycle(true);
                enemy.Disarm();
                enemy.gameObject.SetActive(false);
            }
        }

        private void StartWave(int index)
        {
            if (waveSizes == null || index < 0 || index >= waveSizes.Length) return;
            UnsubscribeAll();
            _waveIndex = index;
            _waveStart = 0;
            for (int i = 0; i < index; i++) _waveStart += Mathf.Max(0, waveSizes[i]);
            _waveEnd = Mathf.Min(roster.Length, _waveStart + Mathf.Max(0, waveSizes[index]));
            _advanceAtTick = long.MaxValue;

            for (int i = 0; i < roster.Length; i++)
            {
                JourneyEnemyController enemy = roster[i];
                if (enemy == null) continue;
                bool inWave = i >= _waveStart && i < _waveEnd;
                if (!inWave)
                {
                    enemy.Disarm();
                    enemy.gameObject.SetActive(false);
                    continue;
                }

                enemy.gameObject.SetActive(true);
                enemy.Defeated -= OnEnemyDefeated;
                enemy.Defeated += OnEnemyDefeated;
                enemy.Arm();
            }

            WaveStarted?.Invoke(index);
            Debug.Log($"[Mindforge:Menagerie] Wave {index + 1}/{WaveCount} armed ({_waveEnd - _waveStart} enemies).");
        }

        private bool CurrentWaveCleared()
        {
            if (_waveEnd <= _waveStart) return true;
            for (int i = _waveStart; i < _waveEnd && i < roster.Length; i++)
            {
                JourneyEnemyController enemy = roster[i];
                if (enemy != null && enemy.Vitals != null && enemy.Vitals.IsAlive) return false;
            }
            return true;
        }

        private void OnEnemyDefeated(JourneyEnemyController enemy)
        {
            if (enemy != null) enemy.Defeated -= OnEnemyDefeated;
        }

        private void UnsubscribeAll()
        {
            if (roster == null) return;
            for (int i = 0; i < roster.Length; i++)
                if (roster[i] != null) roster[i].Defeated -= OnEnemyDefeated;
        }

        private bool IsNearActivation()
        {
            Vector3 delta = Vector3.ProjectOnPlane(activationPoint.position - player.position, Vector3.up);
            float r = Mathf.Max(0.5f, activationRadius);
            return delta.sqrMagnitude <= r * r;
        }
    }
}
