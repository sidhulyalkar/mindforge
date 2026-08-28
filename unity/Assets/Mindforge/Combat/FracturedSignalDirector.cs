using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mindforge.SoulWisp;

namespace Mindforge.Combat
{
    /// <summary>
    /// Competition boss scheduler built around cognitive pacing rather than a flat
    /// difficulty ramp. Gameplay cadence and telegraph commitment run on the fixed
    /// simulation clock; presentation may animate independently between those facts.
    /// External neural-link pauses suppress enemy authority without granting the
    /// Guardian a free damage window.
    /// </summary>
    public sealed class FracturedSignalDirector : MonoBehaviour
    {
        [SerializeField] private CombatantVitals vitals;
        [SerializeField] private MindforgeProjectile projectilePrefab;
        [SerializeField] private Transform projectileOrigin;
        [SerializeField] private Transform player;
        [SerializeField] private FluxMeter playerFlux;
        [SerializeField] private SoulWispController soulWisp;
        [SerializeField] private FracturedSignalTelegraph telegraph;
        [SerializeField] private FracturedEchoNode echoPrefab;
        [SerializeField] private Transform echoParent;
        [SerializeField] private FracturedSignalMeleeDirector meleeDirector;

        [Header("Signal Break")]
        [SerializeField] private float signalBreakVisualRestSeconds = 2.6f;

        [Header("Phase cadence")]
        [SerializeField] private float phaseOneInterval = 0.82f;
        [SerializeField] private float phaseTwoInterval = 0.66f;
        [SerializeField] private float phaseThreeInterval = 0.48f;
        [SerializeField] private float phaseOneTelegraph = 0.62f;
        [SerializeField] private float phaseTwoTelegraph = 0.52f;
        [SerializeField] private float phaseThreeTelegraph = 0.43f;
        [SerializeField] private int radialCount = 12;
        [SerializeField] private int maxEchoes = 3;

        private static readonly WaitForFixedUpdate FixedStep = new WaitForFixedUpdate();
        private readonly List<FracturedEchoNode> _echoes = new List<FracturedEchoNode>();
        private int _attackIndex;
        private int _lastPhase;
        private Coroutine _loop;
        private bool _externalPaused;

        public event Action<int> PhaseChanged;
        public event Action EchoSpawned;
        public event Action EchoShattered;
        public event Action<string, int, bool> AttackTelegraphed;
        public event Action<string, int, bool> AttackFired;
        public bool ExternalPaused => _externalPaused;

        public int Phase
        {
            get
            {
                if (vitals == null) return 1;
                float ratio = vitals.Health / Mathf.Max(1f, vitals.MaxHealth);
                return ratio > 0.68f ? 1 : ratio > 0.34f ? 2 : 3;
            }
        }

        public void SetExternalPause(bool paused)
        {
            if (_externalPaused == paused) return;
            _externalPaused = paused;
            telegraph?.Clear();
            _echoes.RemoveAll(item => item == null);
            foreach (FracturedEchoNode echo in _echoes) echo?.SetExternalPause(paused);
        }

        /// <summary>
        /// Clears ephemeral boss state so a Memory Forge respawn can reactivate the
        /// same authored encounter from a known baseline. The caller owns boss health
        /// reconstruction and root activation ordering.
        /// </summary>
        public void ResetForCheckpoint()
        {
            if (_loop != null)
            {
                StopCoroutine(_loop);
                _loop = null;
            }
            telegraph?.Clear();
            for (int i = 0; i < _echoes.Count; i++)
            {
                FracturedEchoNode echo = _echoes[i];
                if (echo == null) continue;
                echo.Shattered -= OnEchoShattered;
                Destroy(echo.gameObject);
            }
            _echoes.Clear();
            _attackIndex = 0;
            _externalPaused = false;
            _lastPhase = Phase;
        }

        private void OnEnable()
        {
            ResolveMelee();
            if (vitals != null && vitals.Poise != null) vitals.Poise.BrokenEvent += OnSignalBreak;
            _lastPhase = Phase;
            _loop = StartCoroutine(AttackLoop());
        }

        private void OnDisable()
        {
            if (vitals != null && vitals.Poise != null) vitals.Poise.BrokenEvent -= OnSignalBreak;
            if (_loop != null) StopCoroutine(_loop);
            _loop = null;
            telegraph?.Clear();
            foreach (FracturedEchoNode echo in _echoes)
                if (echo != null) echo.Shattered -= OnEchoShattered;
        }

        private void OnSignalBreak()
        {
            telegraph?.Clear();
            // Sensory relief is presentation. It deliberately does not schedule combat.
            soulWisp?.RestStimuli(signalBreakVisualRestSeconds);
        }

        private IEnumerator AttackLoop()
        {
            while (true)
            {
                if (!AttackAuthorityAvailable())
                {
                    telegraph?.Clear();
                    yield return FixedStep;
                    continue;
                }

                int phase = Phase;
                if (phase != _lastPhase)
                {
                    _lastPhase = phase;
                    PhaseChanged?.Invoke(phase);
                }

                yield return ExecutePattern(phase);
                if (!AttackAuthorityAvailable()) continue;
                float interval = phase == 1 ? phaseOneInterval : phase == 2 ? phaseTwoInterval : phaseThreeInterval;
                yield return WaitCombatTicks(SecondsToTicks(interval));
            }
        }

        private IEnumerator ExecutePattern(int phase)
        {
            if (!AttackAuthorityAvailable()) yield break;
            _attackIndex++;
            FracturedSignalMeleeDirector melee = ResolveMelee();

            if (phase == 1)
            {
                if (_attackIndex % 4 == 0 && melee != null && melee.CanEngage)
                    yield return melee.ExecuteCleave(phase, false);
                else if ((_attackIndex & 1) == 0)
                    yield return TelegraphAndFan(2, 14.5f, 12f, phaseOneTelegraph, false);
                else
                    yield return TelegraphAndRadial(radialCount, 10.2f, phaseOneTelegraph, false);
                yield break;
            }

            if (phase == 2)
            {
                int index = _attackIndex % 4;
                if (index == 0)
                {
                    SpawnEchoIfNeeded();
                    yield return WaitCombatTicks(SecondsToTicks(phaseTwoTelegraph * 0.65f));
                }
                else if (index == 1)
                    yield return TelegraphAndFan(3, 16f, 12f, phaseTwoTelegraph, false);
                else if (index == 2)
                    yield return TelegraphAndRadial(radialCount + 4, 11.4f, phaseTwoTelegraph, false);
                else if (melee != null && melee.CanEngage)
                    yield return melee.ExecuteCleave(phase, true);
                else
                    yield return TelegraphAndFan(4, 17f, 9f, phaseTwoTelegraph, true);
                yield break;
            }

            int phaseThreeIndex = _attackIndex % 5;
            if (phaseThreeIndex == 0)
            {
                SpawnEchoIfNeeded();
                yield return WaitCombatTicks(SecondsToTicks(phaseThreeTelegraph * 0.55f));
            }
            else if (phaseThreeIndex == 1 || phaseThreeIndex == 4)
            {
                yield return TelegraphAndFan(5, 18.5f, 8f, phaseThreeTelegraph, true);
            }
            else if (phaseThreeIndex == 2 && melee != null && melee.CanEngage)
            {
                yield return melee.ExecuteSlam(phase, true);
            }
            else if (phaseThreeIndex == 3 && melee != null && melee.CanEngage)
            {
                yield return melee.ExecuteCleave(phase, true);
            }
            else
            {
                yield return TelegraphAndRadial(radialCount + 8, 12.5f, phaseThreeTelegraph, phaseThreeIndex == 3);
            }
        }

        private FracturedSignalMeleeDirector ResolveMelee()
        {
            if (meleeDirector == null) meleeDirector = GetComponent<FracturedSignalMeleeDirector>();
            return meleeDirector;
        }

        private IEnumerator TelegraphAndFan(int count, float speed, float spreadDegrees, float delay, bool heavy)
        {
            if (player == null || projectilePrefab == null || !AttackAuthorityAvailable()) yield break;
            Vector3 origin = projectileOrigin != null ? projectileOrigin.position : transform.position;
            Vector3 center = (player.position - origin).normalized;
            AttackTelegraphed?.Invoke("FAN", count, heavy);
            telegraph?.ShowFan(origin, center, count, spreadDegrees, heavy);
            yield return WaitCombatTicks(SecondsToTicks(delay));
            telegraph?.Clear();
            if (!AttackAuthorityAvailable()) yield break;
            AttackFired?.Invoke("FAN", count, heavy);
            SpawnAimedFan(count, speed, spreadDegrees, heavy);
        }

        private IEnumerator TelegraphAndRadial(int count, float speed, float delay, bool heavy)
        {
            if (!AttackAuthorityAvailable()) yield break;
            Vector3 origin = projectileOrigin != null ? projectileOrigin.position : transform.position;
            AttackTelegraphed?.Invoke("RADIAL", count, heavy);
            telegraph?.ShowRadial(origin, count, heavy);
            yield return WaitCombatTicks(SecondsToTicks(delay));
            telegraph?.Clear();
            if (!AttackAuthorityAvailable()) yield break;
            AttackFired?.Invoke("RADIAL", count, heavy);
            SpawnRadial(count, speed, heavy);
        }

        private IEnumerator WaitCombatTicks(int ticks)
        {
            int remaining = Mathf.Max(1, ticks);
            while (remaining-- > 0)
            {
                yield return FixedStep;
                if (!AttackAuthorityAvailable()) yield break;
            }
        }

        private bool AttackAuthorityAvailable()
        {
            if (vitals == null || !vitals.IsAlive || _externalPaused) return false;
            return vitals.Poise == null || !vitals.Poise.Broken;
        }

        private static int SecondsToTicks(float seconds)
        {
            float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(0f, seconds) / dt));
        }

        private void SpawnAimedFan(int count, float speed, float spreadDegrees, bool heavy)
        {
            if (player == null || projectilePrefab == null || !AttackAuthorityAvailable()) return;
            Vector3 origin = projectileOrigin != null ? projectileOrigin.position : transform.position;
            Vector3 center = (player.position - origin).normalized;
            for (int i = 0; i < count; i++)
            {
                float offset = (i - (count - 1) * 0.5f) * spreadDegrees;
                Vector3 direction = Quaternion.AngleAxis(offset, Vector3.up) * center;
                Spawn(origin, direction, speed, heavy ? 15f : 10f + Phase * 2f);
            }
        }

        private void SpawnRadial(int count, float speed, bool heavy)
        {
            if (!AttackAuthorityAvailable()) return;
            Vector3 origin = projectileOrigin != null ? projectileOrigin.position : transform.position;
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * 360f;
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
                Spawn(origin, direction, speed, heavy ? 13f : 8f + Phase);
            }
        }

        private void Spawn(Vector3 origin, Vector3 direction, float speed, float damage)
        {
            if (!AttackAuthorityAvailable()) return;
            MindforgeProjectile p = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
            p.Configure(CombatTeam.Enemy, direction.normalized * speed, damage, 0f);
        }

        private void SpawnEchoIfNeeded()
        {
            if (echoPrefab == null || player == null || !AttackAuthorityAvailable()) return;
            _echoes.RemoveAll(item => item == null);
            if (_echoes.Count >= Mathf.Max(1, maxEchoes)) return;
            float phase = (_echoes.Count / (float)Mathf.Max(1, maxEchoes)) * Mathf.PI * 2f + _attackIndex * 0.43f;
            FracturedEchoNode echo = Instantiate(echoPrefab, transform.position, Quaternion.identity,
                echoParent != null ? echoParent : transform.parent);
            echo.Initialize(transform, player, playerFlux, phase);
            echo.Shattered += OnEchoShattered;
            echo.SetExternalPause(_externalPaused);
            _echoes.Add(echo);
            EchoSpawned?.Invoke();
        }

        private void OnEchoShattered()
        {
            EchoShattered?.Invoke();
        }
    }
}
