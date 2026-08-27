using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using Mindforge.Presentation;
using Mindforge.SoulWisp;

namespace Mindforge.Qualification
{
    /// <summary>
    /// Software-side electro-optical qualification aid. It requests VSync/120 Hz,
    /// records frame-visible photodiode phase edges, and exports those software edge
    /// timestamps for comparison with the oscilloscope trace. It never substitutes
    /// for a physical photodiode measurement.
    /// </summary>
    public sealed class DisplayQualificationController : MonoBehaviour
    {
        [SerializeField] private DisplayTimingMonitor timingMonitor;
        [SerializeField] private PhotodiodePatch photodiodePatch;
        [SerializeField] private int targetRefreshHz = 120;
        [SerializeField] private KeyCode exportKey = KeyCode.F12;
        [SerializeField] private int maximumEdges = 20000;

        private readonly List<string> _edges = new List<string>();
        private bool _lastPhase;
        private bool _hasPhase;

        public float SystemRefreshHz
        {
            get
            {
                RefreshRate rate = Screen.currentResolution.refreshRateRatio;
                return rate.denominator == 0 ? 0f : (float)rate.numerator / rate.denominator;
            }
        }

        private void Awake()
        {
            targetRefreshHz = Mathf.Clamp(targetRefreshHz, 60, 240);
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = targetRefreshHz;
        }

        private void Update()
        {
            if (photodiodePatch != null && photodiodePatch.Visible && photodiodePatch.ActiveStimulus != null)
            {
                bool phase = photodiodePatch.ActiveStimulus.IsHighPhase;
                if (!_hasPhase || phase != _lastPhase)
                {
                    _hasPhase = true;
                    _lastPhase = phase;
                    if (_edges.Count < Mathf.Max(100, maximumEdges))
                    {
                        _edges.Add(string.Join(",",
                            Time.frameCount.ToString(CultureInfo.InvariantCulture),
                            Time.realtimeSinceStartupAsDouble.ToString("F9", CultureInfo.InvariantCulture),
                            photodiodePatch.ActiveFrequencyHz.ToString("F3", CultureInfo.InvariantCulture),
                            phase ? "1" : "0",
                            timingMonitor != null ? timingMonitor.ObservedRefreshHz.ToString("F3", CultureInfo.InvariantCulture) : "0",
                            timingMonitor != null ? timingMonitor.DropFraction.ToString("F6", CultureInfo.InvariantCulture) : "0"));
                    }
                }
            }
            else
            {
                _hasPhase = false;
            }

            if (Input.GetKeyDown(exportKey)) ExportSoftwareEdges();
        }

        public string ExportSoftwareEdges()
        {
            string dir = Path.Combine(Application.persistentDataPath, "display_qualification");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"display-timing-{DateTime.UtcNow:yyyyMMddTHHmmssZ}.csv");
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine("frame,unity_realtime_s,stimulus_hz,high_phase,observed_refresh_hz,software_drop_fraction");
                foreach (string edge in _edges) writer.WriteLine(edge);
            }
            Debug.Log($"[Mindforge] Software display-edge log: {path}");
            return path;
        }
    }
}
