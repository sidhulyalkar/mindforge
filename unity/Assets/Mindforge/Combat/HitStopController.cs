using System.Collections;
using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>
    /// Freezes scaled gameplay time for impact feedback. VEP stimulus code must use
    /// unscaled/realtime clocks, so hit-stop never changes the visual target frequency.
    /// </summary>
    public sealed class HitStopController : MonoBehaviour
    {
        private Coroutine _routine;
        private float _restoreScale = 1f;

        public void Pulse(float realSeconds)
        {
            if (realSeconds <= 0f) return;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Run(realSeconds));
        }

        private IEnumerator Run(float realSeconds)
        {
            _restoreScale = Mathf.Max(0.0001f, Time.timeScale);
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(realSeconds);
            Time.timeScale = _restoreScale;
            _routine = null;
        }

        private void OnDisable()
        {
            if (Mathf.Approximately(Time.timeScale, 0f)) Time.timeScale = _restoreScale;
        }
    }
}
