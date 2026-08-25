using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mindforge.SoulWisp;

namespace Mindforge.Combat
{
    /// <summary>
    /// Competition boss scheduler built around cognitive pacing rather than a flat
    /// difficulty ramp.
    ///
    /// Phase I: predictable rhythm / safe aura practice.
    /// Phase II: Echo nodes split physical attention and reward Flux.
    /// Phase III: denser crossfire that pushes counters / Gravity Bloom.
    /// Signal Break: boss vulnerability + VEP visual rest.
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

        private readonly List<FracturedEchoNode> _echoes = new List<FracturedEchoNode>();
        private int _attackIndex;
        private int _lastPhase;
        private Coroutine _loop;

        public event Action<int> PhaseChanged;

        public int Phase
        {
            get
            {
                if (vitals == null) return 1;
                float ratio = vitals.Health / Mathf.Max(1f, vitals.MaxHealth);
                return ratio > 0.68f ? 1 : ratio > 0.34f ? 2 : 3;
            }
        }

        private void OnEnable()
        {
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
        }

        private void OnSignalBreak()
        {
            telegraph?.Clear();
            soulWisp?.RestStimuli(signalBreakVisualRestSeconds);
        }

        private IEnumerator AttackLoop()
        {
            while (true)
            {
                if (vitals == null || !vitals.IsAlive)
                {
                    yield return null;
                    continue;
                }

                if (vitals.Poise != null && vitals.Poise.Broken)
                {
                    telegraph?.Clear();
                    yield return null;
                    continue;
                }

                int phase = Phase;
                if (phase != _lastPhase)
                {
                    _lastPhase = phase;
                    PhaseChanged?.Invoke(phase);
                }

                yield return ExecutePattern(phase);
                float interval = phase == 1 ? phaseOneInterval : phase == 2 ? phaseTwoInterval : phaseThreeInterval;
                yield return new WaitForSeconds(interval);
            }
        }

        private IEnumerator ExecutePattern(int phase)
        {
            _attackIndex++;
            if (phase == 1)
            {
                // Warm-up alternates two highly legible families.
                if ((_attackIndex & 1) == 0)
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
                    yield return new WaitForSeconds(phaseTwoTelegraph * 0.65f);
                }
                else if (index == 1)
                    yield return TelegraphAndFan(3, 16f, 12f, phaseTwoTelegraph, false);
                else if (index == 2)
                    yield return TelegraphAndRadial(radialCount + 4, 11.4f, phaseTwoTelegraph, false);
                else
                    yield return TelegraphAndFan(4, 17f, 9f, phaseTwoTelegraph, true);
                yield break;
            }

            // Controlled overload: more projectiles and Echo pressure, but every
            // family still has an explicit hostile-colored telegraph.
            int phaseThreeIndex = _attackIndex % 5;
            if (phaseThreeIndex == 0)
            {
                SpawnEchoIfNeeded();
                yield return new WaitForSeconds(phaseThreeTelegraph * 0.55f);
            }
            else if (phaseThreeIndex == 1 || phaseThreeIndex == 4)
                yield return TelegraphAndFan(5, 18.5f, 8f, phaseThreeTelegraph, true);
            else
                yield return TelegraphAndRadial(radialCount + 8, 12.5f, phaseThreeTelegraph, phaseThreeIndex == 3);
        }

        private IEnumerator TelegraphAndFan(int count, float speed, float spreadDegrees, float delay, bool heavy)
        {
            if (player == null || projectilePrefab == null) yield break;
            Vector3 origin = projectileOrigin != null ? projectileOrigin.position : transform.position;
            Vector3 center = (player.position - origin).normalized;
            telegraph?.ShowFan(origin, center, count, spreadDegrees, heavy);
            yield return new WaitForSeconds(delay);
            telegraph?.Clear();
            if (vitals != null && vitals.Poise != null && vitals.Poise.Broken) yield break;
            SpawnAimedFan(count, speed, spreadDegrees, heavy);
        }

        private IEnumerator TelegraphAndRadial(int count, float speed, float delay, bool heavy)
        {
            Vector3 origin = projectileOrigin != null ? projectileOrigin.position : transform.position;
            telegraph?.ShowRadial(origin, heavy);
            yield return new WaitForSeconds(delay);
            telegraph?.Clear();
            if (vitals != null && vitals.Poise != null && vitals.Poise.Broken) yield break;
            SpawnRadial(count, speed, heavy);
        }

        private void SpawnAimedFan(int count, float speed, float spreadDegrees, bool heavy)
        {
            if (player == null || projectilePrefab == null) return;
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
            MindforgeProjectile p = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
            p.Configure(CombatTeam.Enemy, direction.normalized * speed, damage, 0f);
        }

        private void SpawnEchoIfNeeded()
        {
            if (echoPrefab == null || player == null) return;
            _echoes.RemoveAll(item => item == null);
            if (_echoes.Count >= Mathf.Max(1, maxEchoes)) return;

            float phase = (_echoes.Count / (float)Mathf.Max(1, maxEchoes)) * Mathf.PI * 2f + _attackIndex * 0.43f;
            FracturedEchoNode echo = Instantiate(echoPrefab, transform.position, Quaternion.identity,
                echoParent != null ? echoParent : transform.parent);
            echo.Initialize(transform, player, playerFlux, phase);
            _echoes.Add(echo);
        }
    }
}
