#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using Unity.Profiling;
using UnityEngine;

namespace Mindforge.Qualification
{
    /// <summary>
    /// Controller-only runtime presentation evidence probe.
    ///
    /// The probe is deliberately excluded from non-development player builds and starts
    /// only after ControllerOnlyQualificationBootstrap is active. It records profiler
    /// counters without changing quality, simulation timing, gameplay or VEP output.
    /// Per-frame sampling performs no managed allocation; report construction/writing
    /// happens only after the measurement window closes.
    /// </summary>
    public sealed class PresentationPerformanceProbe : MonoBehaviour
    {
        private const string ReportSchema = "mindforge.presentation_runtime.v1";

        [SerializeField, Min(0)] private int warmupFrames = 120;
        [SerializeField, Min(60)] private int sampleFrames = 600;

        private ControllerOnlyQualificationBootstrap _controllerOnly;
        private bool _sampling;
        private bool _completed;
        private int _nextControllerLookupFrame;
        private int _warmupRemaining;
        private int _sampleIndex;
        private long[] _mainThreadNanoseconds;

        private ProfilerRecorder _mainThread;
        private ProfilerRecorder _drawCalls;
        private ProfilerRecorder _batches;
        private ProfilerRecorder _setPassCalls;
        private ProfilerRecorder _triangles;
        private ProfilerRecorder _gcAllocated;

        private double _mainThreadMsSum;
        private double _mainThreadMsMax;
        private double _drawCallsSum;
        private long _drawCallsMax;
        private double _batchesSum;
        private long _batchesMax;
        private double _setPassSum;
        private long _setPassMax;
        private double _trianglesSum;
        private long _trianglesMax;
        private double _gcBytesSum;
        private long _gcBytesMax;

        [Serializable]
        private sealed class RuntimePresentationReport
        {
            public string schema = ReportSchema;
            public string generated_utc;
            public bool controller_only = true;
            public string unity_version;
            public string platform;
            public string graphics_device;
            public int screen_width;
            public int screen_height;
            public int target_frame_rate;
            public int sample_frames;
            public int warmup_frames;
            public bool main_thread_valid;
            public bool draw_calls_valid;
            public bool batches_valid;
            public bool setpass_valid;
            public bool triangles_valid;
            public bool gc_allocated_valid;
            public double main_thread_ms_mean;
            public double main_thread_ms_p95;
            public double main_thread_ms_max;
            public double draw_calls_mean;
            public long draw_calls_max;
            public double batches_mean;
            public long batches_max;
            public double setpass_calls_mean;
            public long setpass_calls_max;
            public double triangles_mean;
            public long triangles_max;
            public double gc_allocated_bytes_mean;
            public long gc_allocated_bytes_max;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindObjectOfType<PresentationPerformanceProbe>(true) != null) return;
            new GameObject("MindforgePresentationPerformanceProbe")
                .AddComponent<PresentationPerformanceProbe>();
        }

        private void Update()
        {
            if (_completed || _sampling) return;
            if (_controllerOnly == null)
            {
                if (Time.frameCount < _nextControllerLookupFrame) return;
                _nextControllerLookupFrame = Time.frameCount + 30;
                _controllerOnly = FindObjectOfType<ControllerOnlyQualificationBootstrap>(true);
            }
            if (_controllerOnly == null || !_controllerOnly.Active) return;
            BeginSampling();
        }

        private void LateUpdate()
        {
            if (!_sampling || _completed) return;
            if (_warmupRemaining > 0)
            {
                _warmupRemaining--;
                return;
            }

            if (_sampleIndex >= _mainThreadNanoseconds.Length)
            {
                FinishSampling();
                return;
            }

            long mainNs = ValueOrZero(_mainThread);
            long drawCalls = ValueOrZero(_drawCalls);
            long batches = ValueOrZero(_batches);
            long setPass = ValueOrZero(_setPassCalls);
            long triangles = ValueOrZero(_triangles);
            long gcBytes = ValueOrZero(_gcAllocated);

            _mainThreadNanoseconds[_sampleIndex] = mainNs;
            double mainMs = mainNs * 1e-6;
            _mainThreadMsSum += mainMs;
            _mainThreadMsMax = Math.Max(_mainThreadMsMax, mainMs);
            _drawCallsSum += drawCalls;
            _drawCallsMax = Math.Max(_drawCallsMax, drawCalls);
            _batchesSum += batches;
            _batchesMax = Math.Max(_batchesMax, batches);
            _setPassSum += setPass;
            _setPassMax = Math.Max(_setPassMax, setPass);
            _trianglesSum += triangles;
            _trianglesMax = Math.Max(_trianglesMax, triangles);
            _gcBytesSum += gcBytes;
            _gcBytesMax = Math.Max(_gcBytesMax, gcBytes);
            _sampleIndex++;

            if (_sampleIndex >= _mainThreadNanoseconds.Length)
                FinishSampling();
        }

        private void BeginSampling()
        {
            _sampling = true;
            _warmupRemaining = Mathf.Max(0, warmupFrames);
            int frames = Mathf.Max(60, sampleFrames);
            _mainThreadNanoseconds = new long[frames];
            _sampleIndex = 0;

            _mainThread = StartRecorder(ProfilerCategory.Internal, "Main Thread");
            _drawCalls = StartRecorder(ProfilerCategory.Render, "Draw Calls Count");
            _batches = StartRecorder(ProfilerCategory.Render, "Batches Count");
            _setPassCalls = StartRecorder(ProfilerCategory.Render, "SetPass Calls Count");
            _triangles = StartRecorder(ProfilerCategory.Render, "Triangles Count");
            _gcAllocated = StartRecorder(ProfilerCategory.Memory, "GC Allocated In Frame");

            Debug.Log(
                $"[Mindforge:PresentationPerf] Controller-only measurement armed: " +
                $"warmup={_warmupRemaining} frames, sample={frames} frames.");
        }

        private void FinishSampling()
        {
            if (_completed) return;
            _sampling = false;
            _completed = true;

            int count = Mathf.Max(1, _sampleIndex);
            Array.Sort(_mainThreadNanoseconds, 0, _sampleIndex);
            int p95Index = Mathf.Clamp(Mathf.CeilToInt(_sampleIndex * 0.95f) - 1, 0, Math.Max(0, _sampleIndex - 1));
            double p95Ms = _sampleIndex > 0 ? _mainThreadNanoseconds[p95Index] * 1e-6 : 0.0;

            RuntimePresentationReport report = new RuntimePresentationReport
            {
                generated_utc = DateTime.UtcNow.ToString("O"),
                unity_version = Application.unityVersion,
                platform = Application.platform.ToString(),
                graphics_device = SystemInfo.graphicsDeviceName,
                screen_width = Screen.width,
                screen_height = Screen.height,
                target_frame_rate = Application.targetFrameRate,
                sample_frames = _sampleIndex,
                warmup_frames = Mathf.Max(0, warmupFrames),
                main_thread_valid = _mainThread.Valid,
                draw_calls_valid = _drawCalls.Valid,
                batches_valid = _batches.Valid,
                setpass_valid = _setPassCalls.Valid,
                triangles_valid = _triangles.Valid,
                gc_allocated_valid = _gcAllocated.Valid,
                main_thread_ms_mean = _mainThreadMsSum / count,
                main_thread_ms_p95 = p95Ms,
                main_thread_ms_max = _mainThreadMsMax,
                draw_calls_mean = _drawCallsSum / count,
                draw_calls_max = _drawCallsMax,
                batches_mean = _batchesSum / count,
                batches_max = _batchesMax,
                setpass_calls_mean = _setPassSum / count,
                setpass_calls_max = _setPassMax,
                triangles_mean = _trianglesSum / count,
                triangles_max = _trianglesMax,
                gc_allocated_bytes_mean = _gcBytesSum / count,
                gc_allocated_bytes_max = _gcBytesMax,
            };

            string output = ReportPath();
            string directory = Path.GetDirectoryName(output);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(output, JsonUtility.ToJson(report, true));

            Debug.Log(
                $"[Mindforge:PresentationPerf] Measurement complete: main={report.main_thread_ms_mean:F2} ms mean / " +
                $"{report.main_thread_ms_p95:F2} ms p95, drawCalls={report.draw_calls_mean:F1}, " +
                $"batches={report.batches_mean:F1}, setPass={report.setpass_calls_mean:F1}, " +
                $"GC={report.gc_allocated_bytes_mean:F1} B/frame. Report: {output}");

            DisposeRecorders();
        }

        private static ProfilerRecorder StartRecorder(ProfilerCategory category, string statName)
        {
            try
            {
                return ProfilerRecorder.StartNew(category, statName, 1);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Mindforge:PresentationPerf] Profiler counter unavailable: {statName} ({exception.Message})");
                return default;
            }
        }

        private static long ValueOrZero(ProfilerRecorder recorder)
            => recorder.Valid ? recorder.LastValue : 0L;

        private void OnDisable() => DisposeRecorders();

        private void DisposeRecorders()
        {
            if (_mainThread.Valid) _mainThread.Dispose();
            if (_drawCalls.Valid) _drawCalls.Dispose();
            if (_batches.Valid) _batches.Dispose();
            if (_setPassCalls.Valid) _setPassCalls.Dispose();
            if (_triangles.Valid) _triangles.Dispose();
            if (_gcAllocated.Valid) _gcAllocated.Dispose();

            _mainThread = default;
            _drawCalls = default;
            _batches = default;
            _setPassCalls = default;
            _triangles = default;
            _gcAllocated = default;
        }

        private static string ReportPath()
        {
#if UNITY_EDITOR
            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            return Path.Combine(repoRoot, "experiments", "reports", "presentation-runtime-latest.json");
#else
            return Path.Combine(Application.persistentDataPath, "presentation-runtime-latest.json");
#endif
        }
    }
}
#endif
