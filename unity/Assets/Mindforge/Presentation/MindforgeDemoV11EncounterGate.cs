using System;
using System.Collections;
using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Presentation-route encounter gating for the V0.11 demo. It does not create attacks or
    /// damage. It only uses existing external-pause seams so enemies become authoritative when
    /// the Guardian reaches their authored combat space instead of firing across the whole map.
    /// </summary>
    public sealed class MindforgeDemoV11EncounterGate : MonoBehaviour
    {
        [SerializeField] private float bossReleaseZ = 82f;
        [SerializeField] private float echoWakeDistance = 18f;

        private Transform _guardian;
        private FracturedSignalDirector _boss;
        private bool _bossReleased;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            MindforgeDemoV11Marker marker = UnityEngine.Object.FindObjectOfType<MindforgeDemoV11Marker>(true);
            if (marker == null || marker.GetComponent<MindforgeDemoV11EncounterGate>() != null) return;
            marker.gameObject.AddComponent<MindforgeDemoV11EncounterGate>();
        }

        private IEnumerator Start()
        {
            MindforgeDemoV11Marker marker = GetComponent<MindforgeDemoV11Marker>();
            GuardianCombatInput input = null;
            for (int frame = 0; frame < 180; frame++)
            {
                if (input == null) input = UnityEngine.Object.FindObjectOfType<GuardianCombatInput>(true);
                if (_boss == null) _boss = UnityEngine.Object.FindObjectOfType<FracturedSignalDirector>(true);
                if (input != null && _boss != null) break;
                yield return null;
            }

            if (input == null || _boss == null) yield break;
            _guardian = input.transform;

            if (marker != null)
            {
                Vector3 guardianSpawn = marker.GuardianSpawn;
                guardianSpawn.y = Mathf.Max(1.05f, guardianSpawn.y);
                _guardian.position = guardianSpawn;

                Vector3 bossSpawn = marker.BossSpawn;
                bossSpawn.y = Mathf.Max(5.9f, bossSpawn.y);
                _boss.transform.position = bossSpawn;
            }

            _boss.SetExternalPause(true);
            GateEchoes();
        }

        private void Update()
        {
            if (_guardian == null || _boss == null) return;

            if (!_bossReleased && _guardian.position.z >= bossReleaseZ)
            {
                _bossReleased = true;
                _boss.SetExternalPause(false);
                Debug.Log("[Mindforge:V11] Fractured Signal encounter released at the authored arena threshold.");
            }
            else if (!_bossReleased && !_boss.ExternalPaused)
            {
                _boss.SetExternalPause(true);
            }

            GateEchoes();
        }

        private void GateEchoes()
        {
            if (_guardian == null) return;
            FracturedEchoNode[] echoes = UnityEngine.Object.FindObjectsOfType<FracturedEchoNode>(true);
            for (int i = 0; i < echoes.Length; i++)
            {
                FracturedEchoNode echo = echoes[i];
                if (echo == null || !echo.name.StartsWith("V11Echo_", StringComparison.Ordinal)) continue;
                Vector3 delta = echo.transform.position - _guardian.position;
                delta.y = 0f;
                echo.SetExternalPause(delta.sqrMagnitude > echoWakeDistance * echoWakeDistance);
            }
        }
    }
}
