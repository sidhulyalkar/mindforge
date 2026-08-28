from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_presentation_performance_probe_is_development_only_and_controller_only():
    source = read("Qualification", "PresentationPerformanceProbe.cs")

    assert source.startswith("#if UNITY_EDITOR || DEVELOPMENT_BUILD")
    assert "ControllerOnlyQualificationBootstrap" in source
    assert "_controllerOnly.Active" in source
    assert 'ReportSchema = "mindforge.presentation_runtime.v1"' in source
    assert "RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)" in source

    for forbidden in (
        "UdpNeuralReceiver",
        "NeuralEvent",
        "VepAuraStimulus",
        "TryApply(",
        "ReceiveDamage(",
        "TryLightAttack(",
        "SetGuardHeld(",
        "RequestDash(",
        "Time.timeScale =",
        "Time.fixedDeltaTime =",
        "QualitySettings.SetQualityLevel",
        "ScalableBufferManager",
    ):
        assert forbidden not in source


def test_presentation_performance_probe_uses_official_unity_profiler_counters():
    source = read("Qualification", "PresentationPerformanceProbe.cs")

    for token in (
        'ProfilerRecorder.StartNew(category, statName, 1)',
        'ProfilerCategory.Internal, "Main Thread"',
        'ProfilerCategory.Render, "Draw Calls Count"',
        'ProfilerCategory.Render, "Batches Count"',
        'ProfilerCategory.Render, "SetPass Calls Count"',
        'ProfilerCategory.Render, "Triangles Count"',
        'ProfilerCategory.Memory, "GC Allocated In Frame"',
        "recorder.Valid ? recorder.LastValue : 0L",
        "mainNs * 1e-6",
    ):
        assert token in source


def test_runtime_sampling_has_no_per_frame_collection_growth_or_json_work():
    source = read("Qualification", "PresentationPerformanceProbe.cs")
    late_start = source.index("private void LateUpdate()")
    begin_start = source.index("private void BeginSampling()")
    late_body = source[late_start:begin_start]

    assert "new " not in late_body
    assert ".Add(" not in late_body
    assert "File.WriteAllText" not in late_body
    assert "JsonUtility" not in late_body
    assert "_mainThreadNanoseconds[_sampleIndex] = mainNs" in late_body

    # Allocation-heavy reporting/sorting happens only after the sample window closes.
    finish_start = source.index("private void FinishSampling()")
    start_recorder = source.index("private static ProfilerRecorder StartRecorder")
    finish_body = source[finish_start:start_recorder]
    assert "Array.Sort" in finish_body
    assert "File.WriteAllText" in finish_body
    assert "JsonUtility.ToJson" in finish_body


def test_runtime_report_records_frame_time_draw_work_and_gc_without_claiming_quality():
    source = read("Qualification", "PresentationPerformanceProbe.cs")

    for field in (
        "main_thread_ms_mean",
        "main_thread_ms_p95",
        "main_thread_ms_max",
        "draw_calls_mean",
        "batches_mean",
        "setpass_calls_mean",
        "triangles_mean",
        "gc_allocated_bytes_mean",
        "gc_allocated_bytes_max",
        "unity_version",
        "graphics_device",
        "screen_width",
        "screen_height",
    ):
        assert field in source

    assert '"presentation-runtime-latest.json"' in source
    assert "controller_only = true" in source
