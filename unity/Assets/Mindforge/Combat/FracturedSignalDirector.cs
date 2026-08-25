using System.Collections;
using UnityEngine;
using Mindforge.SoulWisp;

namespace Mindforge.Combat
{
    /// <summary>
    /// Competition boss scheduler. Signal Break deliberately doubles as a visual
    /// rest phase: the boss is vulnerable while SSVEP modulation is held steady.
    /// </summary>
    public sealed class FracturedSignalDirector : MonoBehaviour
    {
        [SerializeField] private CombatantVitals vitals;
        [SerializeField] private MindforgeProjectile projectilePrefab;
        [SerializeField] private Transform projectileOrigin;
        [SerializeField] private Transform player;
        [SerializeField] private SoulWispController soulWisp;
        [SerializeField] private float signalBreakVisualRestSeconds = 2.6f;
        [SerializeField] private float phaseOneInterval = 1.35f;
        [SerializeField] private float phaseTwoInterval = 1.05f;
        [SerializeField] private float phaseThreeInterval = 0.82f;
        [SerializeField] private int radialCount = 12;

        private int _attackIndex;
        private Coroutine _loop;

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
            _loop = StartCoroutine(AttackLoop());
        }

        private void OnDisable()
        {
            if (vitals != null && vitals.Poise != null) vitals.Poise.BrokenEvent -= OnSignalBreak;
            if (_loop != null) StopCoroutine(_loop);
            _loop = null;
        }

        private void OnSignalBreak()
        {
            soulWisp?.RestStimuli(signalBreakVisualRestSeconds);
        }

        private IEnumerator AttackLoop()
        {
            while (true)
            {
                if (vitals != null && vitals.IsAlive && (vitals.Poise == null || !vitals.Poise.Broken))
                {
                    ExecutePattern();
                    float wait = Phase == 1 ? phaseOneInterval : Phase == 2 ? phaseTwoInterval : phaseThreeInterval;
                    yield return new WaitForSeconds(wait);
                }
                else yield return null;
            }
        }

        private void ExecutePattern()
        {
            _attackIndex++;
            int choices = Phase == 1 ? 2 : 3;
            switch (_attackIndex % choices)
            {
                case 0: SpawnAimedFan(1 + Phase, 14f + Phase * 1.5f, 10f + Phase * 3f); break;
                case 1: SpawnRadial(radialCount + Phase * 3, 10f + Phase * 1.2f); break;
                default: SpawnAimedFan(3 + Phase, 17f, 8f); break;
            }
        }

        private void SpawnAimedFan(int count, float speed, float spreadDegrees)
        {
            if (player == null || projectilePrefab == null) return;
            Vector3 origin = projectileOrigin != null ? projectileOrigin.position : transform.position;
            Vector3 center = (player.position - origin).normalized;
            for (int i = 0; i < count; i++)
            {
                float offset = (i - (count - 1) * 0.5f) * spreadDegrees;
                Vector3 direction = Quaternion.AngleAxis(offset, Vector3.up) * center;
                Spawn(origin, direction, speed, 10f + Phase * 3f);
            }
        }

        private void SpawnRadial(int count, float speed)
        {
            Vector3 origin = projectileOrigin != null ? projectileOrigin.position : transform.position;
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * 360f;
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
                Spawn(origin, direction, speed, 8f + Phase);
            }
        }

        private void Spawn(Vector3 origin, Vector3 direction, float speed, float damage)
        {
            MindforgeProjectile p = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
            p.Configure(CombatTeam.Enemy, direction.normalized * speed, damage, 0f);
        }
    }
}
