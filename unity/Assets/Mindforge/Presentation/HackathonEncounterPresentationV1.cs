using UnityEngine;
using Mindforge.World;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Event-driven presentation for the first large Menagerie encounter. Wave timing and
    /// enemy activation remain ArenaMenagerieDirector authority. This layer only animates
    /// authored beacons/crowns and exposes no combat, movement, damage or neural surface.
    /// </summary>
    [DefaultExecutionOrder(1120)]
    public sealed class HackathonEncounterPresentationV1 : MonoBehaviour
    {
        [SerializeField] private ArenaMenagerieDirector director;
        [SerializeField] private Transform[] waveBeacons;
        [SerializeField] private Transform victoryCrown;
        [SerializeField] private float pulseSeconds = 0.72f;

        private float _pulseUntil;
        private int _activeWave = -1;
        private bool _complete;
        private Vector3 _crownStart;
        private bool _crownCaptured;

        public void ConfigureRuntime(ArenaMenagerieDirector encounter, Transform[] beacons, Transform crown)
        {
            director = encounter;
            waveBeacons = beacons;
            victoryCrown = crown;
            CaptureCrown();
        }

        private void Awake()
        {
            if (director == null) director = GetComponent<ArenaMenagerieDirector>();
            CaptureCrown();
        }

        private void OnEnable()
        {
            if (director == null) director = GetComponent<ArenaMenagerieDirector>();
            if (director == null) return;
            director.WaveStarted += OnWaveStarted;
            director.WaveCleared += OnWaveCleared;
            director.Completed += OnCompleted;
        }

        private void OnDisable()
        {
            if (director == null) return;
            director.WaveStarted -= OnWaveStarted;
            director.WaveCleared -= OnWaveCleared;
            director.Completed -= OnCompleted;
        }

        private void LateUpdate()
        {
            float time = Time.unscaledTime;
            float pulse01 = _pulseUntil > time
                ? Mathf.Clamp01((_pulseUntil - time) / Mathf.Max(0.05f, pulseSeconds))
                : 0f;
            float pulse = Mathf.Sin((1f - pulse01) * Mathf.PI) * pulse01;

            if (waveBeacons != null)
            {
                for (int i = 0; i < waveBeacons.Length; i++)
                {
                    Transform beacon = waveBeacons[i];
                    if (beacon == null) continue;
                    bool active = i == _activeWave && !_complete;
                    float target = active ? 1.0f : 0.72f;
                    float accent = active ? pulse * 0.34f : 0f;
                    float scale = target + accent;
                    beacon.localScale = Vector3.Lerp(
                        beacon.localScale,
                        new Vector3(scale, scale, scale),
                        1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
                    beacon.localRotation *= Quaternion.Euler(0f, (active ? 22f : 6f) * Time.unscaledDeltaTime, 0f);
                }
            }

            if (victoryCrown != null && _crownCaptured)
            {
                Vector3 target = _complete ? _crownStart + Vector3.up * 3.2f : _crownStart;
                victoryCrown.localPosition = Vector3.Lerp(
                    victoryCrown.localPosition,
                    target,
                    1f - Mathf.Exp(-3.8f * Time.unscaledDeltaTime));
                if (_complete)
                    victoryCrown.localRotation *= Quaternion.Euler(0f, 18f * Time.unscaledDeltaTime, 0f);
            }
        }

        private void OnWaveStarted(int index)
        {
            _activeWave = Mathf.Max(0, index);
            _pulseUntil = Time.unscaledTime + Mathf.Max(0.05f, pulseSeconds);
        }

        private void OnWaveCleared(int index)
        {
            _activeWave = index;
            _pulseUntil = Time.unscaledTime + Mathf.Max(0.05f, pulseSeconds) * 0.7f;
        }

        private void OnCompleted()
        {
            _complete = true;
            _activeWave = -1;
            _pulseUntil = Time.unscaledTime + Mathf.Max(0.05f, pulseSeconds);
        }

        private void CaptureCrown()
        {
            if (_crownCaptured || victoryCrown == null) return;
            _crownStart = victoryCrown.localPosition;
            _crownCaptured = true;
        }
    }
}
