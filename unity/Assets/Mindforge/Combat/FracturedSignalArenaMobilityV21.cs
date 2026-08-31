using System.Reflection;
using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>
    /// Recording-driven spacing retune for the first Fractured Signal encounter.
    ///
    /// V0.19 owns boss locomotion. V0.21 only widens its spacing envelope to match the enlarged
    /// authored arena. The adapter validates the complete private-field contract before changing
    /// anything so an upstream V0.19 refactor cannot leave a partially retuned boss.
    /// </summary>
    [DefaultExecutionOrder(-94)]
    public sealed class FracturedSignalArenaMobilityV21 : MonoBehaviour
    {
        private FracturedSignalFirstBossV19 _movement;
        private bool _attempted;
        private bool _applied;

        public bool Applied => _applied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            FracturedSignalDirector[] bosses = FindObjectsOfType<FracturedSignalDirector>(true);
            for (int i = 0; i < bosses.Length; i++)
            {
                FracturedSignalDirector boss = bosses[i];
                if (boss != null && boss.GetComponent<FracturedSignalArenaMobilityV21>() == null)
                    boss.gameObject.AddComponent<FracturedSignalArenaMobilityV21>();
            }
        }

        private void Awake()
        {
            _movement = GetComponent<FracturedSignalFirstBossV19>();
        }

        private void Start()
        {
            ApplyArenaProfile();
        }

        private void ApplyArenaProfile()
        {
            if (_attempted || _applied) return;
            _attempted = true;
            if (_movement == null) _movement = GetComponent<FracturedSignalFirstBossV19>();
            if (_movement == null)
            {
                Debug.LogError("[Mindforge:BossV21] V19 movement owner missing; arena mobility profile applied nothing.");
                return;
            }

            bool fieldsAvailable =
                CanSet<float>("phaseOnePreferredDistance") &
                CanSet<float>("phaseTwoPreferredDistance") &
                CanSet<float>("phaseThreePreferredDistance") &
                CanSet<float>("distanceBand") &
                CanSet<float>("phaseOneMoveSpeed") &
                CanSet<float>("phaseTwoMoveSpeed") &
                CanSet<float>("phaseThreeMoveSpeed") &
                CanSet<float>("retreatMultiplier") &
                CanSet<float>("orbitBias") &
                CanSet<float>("orbitSideHoldSeconds") &
                CanSet<float>("homeLeashRadius") &
                CanSet<float>("collisionProbeRadius") &
                CanSet<float>("postAttackRecovery");

            if (!fieldsAvailable)
            {
                Debug.LogError("[Mindforge:BossV21] V19 movement field contract changed; arena mobility profile applied nothing.");
                return;
            }

            Set("phaseOnePreferredDistance", 5.25f);
            Set("phaseTwoPreferredDistance", 6.10f);
            Set("phaseThreePreferredDistance", 5.35f);
            Set("distanceBand", 0.95f);
            Set("phaseOneMoveSpeed", 1.90f);
            Set("phaseTwoMoveSpeed", 2.35f);
            Set("phaseThreeMoveSpeed", 2.72f);
            Set("retreatMultiplier", 0.86f);
            Set("orbitBias", 0.80f);
            Set("orbitSideHoldSeconds", 2.35f);
            Set("homeLeashRadius", 9.0f);
            Set("collisionProbeRadius", 0.78f);
            Set("postAttackRecovery", 0.54f);
            _applied = true;
        }

        private static bool CanSet<T>(string fieldName)
        {
            FieldInfo field = typeof(FracturedSignalFirstBossV19).GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null && field.FieldType.IsAssignableFrom(typeof(T));
        }

        private void Set<T>(string fieldName, T value)
        {
            FieldInfo field = typeof(FracturedSignalFirstBossV19).GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(_movement, value);
        }
    }
}
