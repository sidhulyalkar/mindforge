using System.Collections;
using UnityEngine;

namespace Mindforge.Combat
{
    /// <summary>
    /// Freezes scaled combat time for impact feedback while VEP stimulus code keeps
    /// using realtime/unscaled clocks. Repeated impacts extend one real-time freeze
    /// window instead of recursively capturing an already-zero Time.timeScale.
    /// </summary>
    public sealed class HitStopController : MonoBehaviour
    {
        private Coroutine _routine;
        private double _freezeUntil;
        private float _restoreScale = 1f;
        private bool _ownsTimeScale;

        public bool Frozen => _routine != null;
        public float Remaining => Mathf.Max(0f, (float)(_freezeUntil - Time.realtimeSinceStartupAsDouble));

        public void Pulse(float realSeconds)
        {
            if (realSeconds <= 0f) return;

            double now = Time.realtimeSinceStartupAsDouble;
            _freezeUntil = System.Math.Max(_freezeUntil, now + realSeconds);

            if (_routine != null) return;

            // Do not unpause an externally paused game. We only restore a scale we
            // actually replaced ourselves.
            if (Time.timeScale > 0.0001f)
            {
                _restoreScale = Time.timeScale;
                Time.timeScale = 0f;
                _ownsTimeScale = true;
            }
            else
            {
                _ownsTimeScale = false;
            }

            _routine = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            while (Time.realtimeSinceStartupAsDouble < _freezeUntil)
                yield return null;

            RestoreOwnedTimeScale();
            _routine = null;
            _freezeUntil = 0.0;
        }

        private void RestoreOwnedTimeScale()
        {
            if (!_ownsTimeScale) return;
            Time.timeScale = Mathf.Max(0.0001f, _restoreScale);
            _ownsTimeScale = false;
        }

        private void OnDisable()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            _freezeUntil = 0.0;
            RestoreOwnedTimeScale();
        }
    }
}
