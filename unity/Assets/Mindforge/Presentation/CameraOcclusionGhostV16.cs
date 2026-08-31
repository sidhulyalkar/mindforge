using System;
using System.Collections.Generic;
using UnityEngine;
using Mindforge.Calibration;
using Mindforge.SoulWisp;

namespace Mindforge.Presentation
{
    /// <summary>
    /// Prevents decorative architecture from filling the camera with opaque slabs.
    ///
    /// Only Renderer.enabled is changed. Colliders, transforms, GameObjects and gameplay
    /// authority remain untouched. Visibility changes are frozen while baseline/calibration
    /// or a player-armed resonance window owns EEG evidence, avoiding a large scene-luminance
    /// transient during the neural decision itself.
    /// </summary>
    [DefaultExecutionOrder(80)]
    public sealed class CameraOcclusionGhostV16 : MonoBehaviour
    {
        private static readonly string[] RootNames =
        {
            "Mindforge_AetheriaWorld_V1",
            "Mindforge_GroundedWorld_V1",
            "Mindforge_Production_Art_V09",
            "Mindforge_Demo_Environment_V15",
        };

        private static readonly string[] CandidateTokens =
        {
            "Wall", "Tower", "Monolith", "Pillar", "Column", "Arch", "Rib", "Canopy",
            "Spire", "Facade", "Buttress", "Gate", "Threshold",
        };

        private static readonly string[] NeverGhostTokens =
        {
            "SightVepCore", "GuardVepCore", "Photodiode", "Floor", "Ground", "Road",
            "CombatDisc", "Guardian", "Wisp", "FracturedSignalVisual", "Boss",
        };

        [SerializeField] private float guardianAimHeight = 1.12f;
        [SerializeField] private float releaseGraceSeconds = 0.10f;
        [SerializeField] private float minimumBoundsMagnitude = 0.55f;

        private Camera _camera;
        private Transform _guardian;
        private AwakeningCalibrationDirector _calibration;
        private SoulWispController _wisp;
        private readonly List<Entry> _entries = new List<Entry>(160);
        private bool _cached;

        private sealed class Entry
        {
            public Renderer renderer;
            public bool baselineEnabled;
            public float occludedUntil;
        }

        public int CandidateCount => _entries.Count;

        public void Configure(
            Camera camera,
            Transform guardian,
            AwakeningCalibrationDirector calibration,
            SoulWispController wisp)
        {
            _camera = camera;
            _guardian = guardian;
            _calibration = calibration;
            _wisp = wisp;
        }

        private void Start() => CacheCandidates();

        private void LateUpdate()
        {
            if (_camera == null || _guardian == null) return;
            if (!_cached) CacheCandidates();
            if (NeuralEvidenceOwnsVisualField()) return;

            Vector3 origin = _camera.transform.position;
            Vector3 destination = _guardian.position + Vector3.up * guardianAimHeight;
            Vector3 delta = destination - origin;
            float guardianDistance = delta.magnitude;
            if (guardianDistance <= 0.1f) return;

            Ray cameraRay = new Ray(origin, delta / guardianDistance);
            float now = Time.unscaledTime;

            for (int i = 0; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];
                Renderer renderer = entry.renderer;
                if (renderer == null) continue;

                bool blocks = false;
                Bounds bounds = renderer.bounds;
                if (bounds.size.sqrMagnitude >= minimumBoundsMagnitude * minimumBoundsMagnitude &&
                    bounds.IntersectRay(cameraRay, out float hitDistance) &&
                    hitDistance > 0.05f && hitDistance < guardianDistance - 0.20f)
                {
                    blocks = true;
                    entry.occludedUntil = now + Mathf.Max(0f, releaseGraceSeconds);
                }

                bool shouldHide = blocks || now < entry.occludedUntil;
                bool targetEnabled = entry.baselineEnabled && !shouldHide;
                if (renderer.enabled != targetEnabled) renderer.enabled = targetEnabled;
            }
        }

        private bool NeuralEvidenceOwnsVisualField()
        {
            if (_calibration == null) _calibration = FindObjectOfType<AwakeningCalibrationDirector>(true);
            if (_wisp == null) _wisp = FindObjectOfType<SoulWispController>(true);
            return (_calibration != null && _calibration.CalibrationInProgress) ||
                   (_wisp != null && (_wisp.CalibrationStimuliActive || _wisp.ResonanceWindowActive));
        }

        private void CacheCandidates()
        {
            RestoreAll();
            _entries.Clear();

            HashSet<Renderer> seen = new HashSet<Renderer>();
            for (int r = 0; r < RootNames.Length; r++)
            {
                GameObject root = VisualIdentityV16Installer.FindSceneObject(RootNames[r]);
                if (root == null) continue;
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null || seen.Contains(renderer)) continue;
                    string objectName = renderer.gameObject.name;
                    if (!HasAny(objectName, CandidateTokens) || HasAny(objectName, NeverGhostTokens)) continue;
                    seen.Add(renderer);
                    _entries.Add(new Entry
                    {
                        renderer = renderer,
                        baselineEnabled = renderer.enabled,
                        occludedUntil = 0f,
                    });
                }
            }

            _cached = true;
            Debug.Log($"[Mindforge:V16] Camera readability registered {_entries.Count} presentation occluders. Collision remains untouched.");
        }

        private void OnDisable() => RestoreAll();
        private void OnDestroy() => RestoreAll();

        private void RestoreAll()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];
                if (entry?.renderer != null) entry.renderer.enabled = entry.baselineEnabled;
            }
        }

        private static bool HasAny(string source, string[] tokens)
        {
            if (string.IsNullOrEmpty(source)) return false;
            for (int i = 0; i < tokens.Length; i++)
                if (source.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }
    }
}
