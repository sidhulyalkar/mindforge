using System;
using System.Collections;
using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Keeps late-created V0.11 Echo visuals synchronized with the already-authoritative
    /// FracturedEchoNode shatter/reconstruction lifecycle. The gameplay node still decides
    /// when an Echo is alive; this bridge only mirrors that truth into presentation.
    /// </summary>
    [DefaultExecutionOrder(820)]
    public sealed class MindforgeDemoV11EchoLifecycle : MonoBehaviour
    {
        private FracturedEchoNode _echo;
        private Transform _visual;
        private bool _subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            MindforgeDemoV11Marker marker = Object.FindObjectOfType<MindforgeDemoV11Marker>(true);
            if (marker == null || marker.GetComponent<MindforgeDemoV11EchoLifecycleBootstrap>() != null) return;
            marker.gameObject.AddComponent<MindforgeDemoV11EchoLifecycleBootstrap>();
        }

        public void Configure(FracturedEchoNode echo)
        {
            if (_echo != echo)
            {
                Unsubscribe();
                _echo = echo;
            }
            ResolveVisual();
            Subscribe();
        }

        private IEnumerator Start()
        {
            if (_echo == null) _echo = GetComponent<FracturedEchoNode>();
            for (int frame = 0; frame < 180 && _visual == null; frame++)
            {
                ResolveVisual();
                if (_visual == null) yield return null;
            }
            Subscribe();
            SyncFromVitals();
        }

        private void OnEnable()
        {
            if (_echo == null) _echo = GetComponent<FracturedEchoNode>();
            Subscribe();
        }

        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();

        private void ResolveVisual()
        {
            if (_visual == null) _visual = transform.Find("V11EchoArchetype");
        }

        private void Subscribe()
        {
            if (_subscribed || _echo == null) return;
            _echo.Shattered += OnShattered;
            _echo.Reconstructed += OnReconstructed;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _echo == null) return;
            _echo.Shattered -= OnShattered;
            _echo.Reconstructed -= OnReconstructed;
            _subscribed = false;
        }

        private void OnShattered()
        {
            ResolveVisual();
            if (_visual != null) _visual.gameObject.SetActive(false);
        }

        private void OnReconstructed()
        {
            ResolveVisual();
            if (_visual != null) _visual.gameObject.SetActive(true);
        }

        private void SyncFromVitals()
        {
            ResolveVisual();
            if (_visual == null || _echo == null || _echo.Vitals == null) return;
            _visual.gameObject.SetActive(_echo.Vitals.IsAlive);
        }
    }

    /// <summary>Marker-scoped installer for every authored route Echo.</summary>
    internal sealed class MindforgeDemoV11EchoLifecycleBootstrap : MonoBehaviour
    {
        private IEnumerator Start()
        {
            for (int frame = 0; frame < 180; frame++)
            {
                FracturedEchoNode[] echoes = Object.FindObjectsOfType<FracturedEchoNode>(true);
                int configured = 0;
                for (int i = 0; i < echoes.Length; i++)
                {
                    FracturedEchoNode echo = echoes[i];
                    if (echo == null || !echo.name.StartsWith("V11Echo_", StringComparison.Ordinal)) continue;
                    MindforgeDemoV11EchoLifecycle bridge = echo.GetComponent<MindforgeDemoV11EchoLifecycle>();
                    if (bridge == null) bridge = echo.gameObject.AddComponent<MindforgeDemoV11EchoLifecycle>();
                    bridge.Configure(echo);
                    configured++;
                }
                if (configured >= 3) yield break;
                yield return null;
            }
        }
    }
}
