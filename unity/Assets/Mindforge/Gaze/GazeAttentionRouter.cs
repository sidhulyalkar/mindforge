using System;
using Mindforge.Combat;
using UnityEngine;

namespace Mindforge.Gaze
{
    /// <summary>
    /// Converts fresh screen-mapped gaze into stable semantic attention. The router may
    /// suggest what the player appears to be looking at, but it never attacks, moves,
    /// guards, interacts, or creates target-lock authority on its own.
    /// </summary>
    public sealed class GazeAttentionRouter : MonoBehaviour
    {
        [SerializeField] private UdpGazeReceiver receiver;
        [SerializeField] private Camera gazeCamera;
        [SerializeField, Range(0f, 1f)] private float minimumConfidence = 0.25f;
        [SerializeField] private float maximumRayDistance = 80f;
        [SerializeField] private float targetDwellSeconds = 0.12f;
        [SerializeField] private float targetReleaseGraceSeconds = 0.18f;
        [SerializeField] private float sampleTimeoutSeconds = 0.55f;

        public event Action<Transform> SuggestedCombatTargetChanged;

        private Transform _rawCandidate;
        private Transform _stableCandidate;
        private double _rawCandidateSince;
        private double _lastCandidateSeenAt = double.NegativeInfinity;
        private double _lastSampleAt = double.NegativeInfinity;
        private Vector2 _lastViewportPoint = new Vector2(0.5f, 0.5f);
        private Ray _lastRay;
        private string _lastSourceMode = "none";
        private float _lastConfidence;
        private bool _lastFixation;

        public Transform SuggestedCombatTarget => IsFresh ? _stableCandidate : null;
        public Vector2 LastViewportPoint => _lastViewportPoint;
        public Ray LastWorldRay => _lastRay;
        public string LastSourceMode => _lastSourceMode;
        public float LastConfidence => _lastConfidence;
        public bool LastFixation => _lastFixation;
        public bool IsFresh => Time.realtimeSinceStartupAsDouble - _lastSampleAt <= sampleTimeoutSeconds;

        private void OnEnable()
        {
            if (receiver == null) receiver = FindObjectOfType<UdpGazeReceiver>();
            if (gazeCamera == null) gazeCamera = Camera.main;
            Bind(receiver);
        }

        private void OnDisable()
        {
            if (receiver != null) receiver.SampleReceived -= Accept;
            ClearSuggestion();
        }

        public void Bind(UdpGazeReceiver source)
        {
            if (receiver == source && receiver != null)
            {
                receiver.SampleReceived -= Accept;
                receiver.SampleReceived += Accept;
                return;
            }

            if (receiver != null) receiver.SampleReceived -= Accept;
            receiver = source;
            if (receiver != null) receiver.SampleReceived += Accept;
        }

        public bool TryGetStableEnemy(out Transform target)
        {
            target = SuggestedCombatTarget;
            if (target == null) return false;
            CombatantVitals vitals = target.GetComponentInParent<CombatantVitals>();
            if (vitals == null) vitals = target.GetComponent<CombatantVitals>();
            return vitals != null && vitals.Team == CombatTeam.Enemy && vitals.IsAlive && target.gameObject.activeInHierarchy;
        }

        private void Accept(GazeEvent sample)
        {
            double now = Time.realtimeSinceStartupAsDouble;
            _lastSampleAt = now;
            if (sample == null)
            {
                ObserveCandidate(null, now);
                return;
            }

            _lastSourceMode = string.IsNullOrWhiteSpace(sample.source_mode) ? "unknown" : sample.source_mode;
            _lastConfidence = sample.confidence;
            _lastFixation = sample.fixation;

            if (!sample.IsUsable(minimumConfidence))
            {
                ObserveCandidate(null, now);
                return;
            }

            if (gazeCamera == null) gazeCamera = Camera.main;
            if (gazeCamera == null)
            {
                ObserveCandidate(null, now);
                return;
            }

            _lastViewportPoint = sample.UnityViewportPoint;
            _lastRay = gazeCamera.ViewportPointToRay(new Vector3(_lastViewportPoint.x, _lastViewportPoint.y, 0f));
            ObserveCandidate(ResolveEnemy(_lastRay), now);
        }

        private Transform ResolveEnemy(Ray ray)
        {
            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                Mathf.Max(1f, maximumRayDistance),
                ~0,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                Transform hit = hits[i].transform;
                if (hit == null) continue;

                CombatantVitals vitals = hit.GetComponentInParent<CombatantVitals>();
                if (vitals == null) vitals = hit.GetComponent<CombatantVitals>();
                if (vitals != null)
                {
                    if (vitals.Team == CombatTeam.Enemy && vitals.IsAlive && vitals.gameObject.activeInHierarchy)
                        return vitals.transform;

                    // The third-person camera can legitimately ray through the player body.
                    // Player/allied combatants are skipped; ordinary world geometry still blocks.
                    continue;
                }

                return null;
            }

            return null;
        }

        private void ObserveCandidate(Transform candidate, double now)
        {
            if (candidate != _rawCandidate)
            {
                _rawCandidate = candidate;
                _rawCandidateSince = now;
            }

            if (candidate != null)
            {
                _lastCandidateSeenAt = now;
                if (now - _rawCandidateSince >= Mathf.Max(0f, targetDwellSeconds))
                    SetStable(candidate);
                return;
            }

            if (_stableCandidate != null && now - _lastCandidateSeenAt > Mathf.Max(0f, targetReleaseGraceSeconds))
                SetStable(null);
        }

        private void Update()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (now - _lastSampleAt > Mathf.Max(0.05f, sampleTimeoutSeconds))
            {
                _rawCandidate = null;
                SetStable(null);
                return;
            }

            if (_stableCandidate != null && _rawCandidate != _stableCandidate &&
                now - _rawCandidateSince > Mathf.Max(0f, targetReleaseGraceSeconds))
                SetStable(null);
        }

        private void SetStable(Transform candidate)
        {
            if (_stableCandidate == candidate) return;
            _stableCandidate = candidate;
            SuggestedCombatTargetChanged?.Invoke(_stableCandidate);
        }

        private void ClearSuggestion()
        {
            _rawCandidate = null;
            _lastSampleAt = double.NegativeInfinity;
            SetStable(null);
        }
    }
}
