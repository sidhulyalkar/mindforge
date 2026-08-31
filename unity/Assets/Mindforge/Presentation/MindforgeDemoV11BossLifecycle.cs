using System.Collections;
using UnityEngine;
using Mindforge.Combat;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Mirrors existing boss vitality into the late-created V0.11 presentation roots.
    /// CombatantVitals remains the only death/reset truth. This component never changes
    /// health or encounter state; it only collapses/re-enables visual children.
    /// </summary>
    [DefaultExecutionOrder(840)]
    public sealed class MindforgeDemoV11BossLifecycle : MonoBehaviour
    {
        private CombatantVitals _vitals;
        private Transform _baseVisual;
        private Transform _phaseVisual;
        private bool _wasAlive;
        private float _deathStarted;
        private bool _ready;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            MindforgeDemoV11Marker marker = Object.FindObjectOfType<MindforgeDemoV11Marker>(true);
            if (marker == null || marker.GetComponent<MindforgeDemoV11BossLifecycleBootstrap>() != null) return;
            marker.gameObject.AddComponent<MindforgeDemoV11BossLifecycleBootstrap>();
        }

        public void Configure(CombatantVitals vitals)
        {
            _vitals = vitals;
            ResolveVisuals();
            _wasAlive = _vitals != null && _vitals.IsAlive;
            if (_wasAlive) RestoreVisuals();
            _ready = _vitals != null;
        }

        private IEnumerator Start()
        {
            if (_vitals == null) _vitals = GetComponent<CombatantVitals>();
            for (int frame = 0; frame < 180; frame++)
            {
                ResolveVisuals();
                if (_baseVisual != null && _phaseVisual != null) break;
                yield return null;
            }
            if (_vitals == null) yield break;
            _wasAlive = _vitals.IsAlive;
            if (_wasAlive) RestoreVisuals();
            _ready = true;
        }

        private void Update()
        {
            if (!_ready || _vitals == null) return;
            ResolveVisuals();
            bool alive = _vitals.IsAlive;

            if (alive && !_wasAlive)
            {
                RestoreVisuals();
            }
            else if (!alive && _wasAlive)
            {
                _deathStarted = Time.unscaledTime;
            }

            if (!alive) UpdateCollapse();
            _wasAlive = alive;
        }

        private void ResolveVisuals()
        {
            if (_baseVisual == null) _baseVisual = transform.Find("V11BossVisual");
            if (_phaseVisual == null) _phaseVisual = transform.Find("V11BossPhaseStaging");
        }

        private void UpdateCollapse()
        {
            float t = Mathf.Clamp01((Time.unscaledTime - _deathStarted) / 0.62f);
            float scale = 1f - Mathf.SmoothStep(0f, 1f, t);
            SetScale(_baseVisual, scale);
            SetScale(_phaseVisual, scale);
            if (t < 1f) return;
            if (_baseVisual != null) _baseVisual.gameObject.SetActive(false);
            if (_phaseVisual != null) _phaseVisual.gameObject.SetActive(false);
        }

        private void RestoreVisuals()
        {
            Restore(_baseVisual);
            Restore(_phaseVisual);
        }

        private static void Restore(Transform visual)
        {
            if (visual == null) return;
            visual.gameObject.SetActive(true);
            visual.localScale = Vector3.one;
        }

        private static void SetScale(Transform visual, float scale)
        {
            if (visual == null || !visual.gameObject.activeSelf) return;
            visual.localScale = Vector3.one * Mathf.Clamp01(scale);
        }
    }

    internal sealed class MindforgeDemoV11BossLifecycleBootstrap : MonoBehaviour
    {
        private IEnumerator Start()
        {
            for (int frame = 0; frame < 180; frame++)
            {
                FracturedSignalDirector director = Object.FindObjectOfType<FracturedSignalDirector>(true);
                if (director != null)
                {
                    CombatantVitals vitals = director.GetComponent<CombatantVitals>();
                    if (vitals != null)
                    {
                        MindforgeDemoV11BossLifecycle lifecycle = director.GetComponent<MindforgeDemoV11BossLifecycle>();
                        if (lifecycle == null) lifecycle = director.gameObject.AddComponent<MindforgeDemoV11BossLifecycle>();
                        lifecycle.Configure(vitals);
                        yield break;
                    }
                }
                yield return null;
            }
        }
    }
}
