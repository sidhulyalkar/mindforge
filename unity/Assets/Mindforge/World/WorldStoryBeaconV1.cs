using System;
using UnityEngine;

namespace Mindforge.World
{
    /// <summary>
    /// Conventional world-position story discovery. Crossing the authored radius records one
    /// durable semantic fact and publishes one narrative signal. It has no collider and no
    /// combat, movement, encounter, reward or neural authority.
    /// </summary>
    [DefaultExecutionOrder(930)]
    public sealed class WorldStoryBeaconV1 : MonoBehaviour
    {
        [SerializeField] private Transform guardian;
        [SerializeField] private WorldStateLedger ledger;
        [SerializeField] private WorldSignalBus signals;
        [SerializeField] private string storyId = "story.unknown";
        [SerializeField] private string title = "AETHERIA";
        [SerializeField, TextArea(2, 4)] private string line;
        [SerializeField, Min(0.5f)] private float radius = 3.5f;

        private bool _discovered;

        public string StoryId => storyId;
        public bool Discovered => _discovered;

        public void ConfigureRuntime(
            Transform guardianTransform,
            WorldStateLedger stateLedger,
            WorldSignalBus signalBus,
            string id,
            string heading,
            string storyLine,
            float discoveryRadius)
        {
            Unsubscribe();
            guardian = guardianTransform;
            ledger = stateLedger;
            signals = signalBus;
            storyId = Normalize(id);
            title = heading ?? string.Empty;
            line = storyLine ?? string.Empty;
            radius = Mathf.Max(0.5f, discoveryRadius);
            Subscribe();
            ResolveExistingState();
        }

        private void Awake()
        {
            Resolve();
            ResolveExistingState();
        }

        private void OnEnable()
        {
            Resolve();
            Subscribe();
            ResolveExistingState();
        }

        private void OnDisable() => Unsubscribe();

        private void FixedUpdate()
        {
            if (_discovered) return;
            Resolve();
            if (guardian == null || ledger == null || signals == null) return;
            Vector3 delta = Vector3.ProjectOnPlane(guardian.position - transform.position, Vector3.up);
            float r = Mathf.Max(0.5f, radius);
            if (delta.sqrMagnitude > r * r) return;
            Discover();
        }

        private void Discover()
        {
            if (_discovered || ledger == null || signals == null) return;
            string id = Normalize(storyId);
            string key = "story." + id + ".discovered";
            if (ledger.TryGetBool(key, out bool already) && already)
            {
                _discovered = true;
                return;
            }

            _discovered = true;
            if (!ledger.SetBool(key, true, "world_story_discovery")) return;
            signals.Publish(
                WorldSignalKind.StoryDiscovered,
                "story.discovered",
                subject: id,
                stateKey: key,
                stringValue: line ?? string.Empty,
                intValue: 1,
                floatValue: 1f,
                reason: title ?? string.Empty);
        }

        private void ResolveExistingState()
        {
            if (ledger == null) return;
            string id = Normalize(storyId);
            _discovered = ledger.TryGetBool("story." + id + ".discovered", out bool value) && value;
        }

        private void OnWorldStateChanged(string key, WorldStateEntry before, WorldStateEntry after)
        {
            string expected = "story." + Normalize(storyId) + ".discovered";
            if (!string.Equals(key, expected, StringComparison.Ordinal)) return;
            _discovered = after != null && after.type == WorldStateValueType.Bool && after.bool_value;
        }

        private void Subscribe()
        {
            if (ledger == null) return;
            ledger.SnapshotRestored -= ResolveExistingState;
            ledger.SnapshotRestored += ResolveExistingState;
            ledger.StateChanged -= OnWorldStateChanged;
            ledger.StateChanged += OnWorldStateChanged;
        }

        private void Unsubscribe()
        {
            if (ledger == null) return;
            ledger.SnapshotRestored -= ResolveExistingState;
            ledger.StateChanged -= OnWorldStateChanged;
        }

        private void Resolve()
        {
            if (guardian == null)
            {
                GameObject player = GameObject.Find("Guardian");
                if (player != null) guardian = player.transform;
            }
            if (ledger == null) ledger = FindObjectOfType<WorldStateLedger>(true);
            if (signals == null) signals = FindObjectOfType<WorldSignalBus>(true);
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim().ToLowerInvariant();
    }
}
