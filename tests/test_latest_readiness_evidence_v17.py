from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
AUDIT = ROOT / "unity/Assets/Mindforge/Editor/MindforgeLatestReadinessAuditV17.cs"


def source() -> str:
    return AUDIT.read_text(encoding="utf-8")


def test_deferred_runtime_checks_can_never_inflate_readiness_to_pass():
    text = source()
    assert 'status = "DEFERRED"' in text
    assert "observed = false" in text
    assert "passed = false" in text
    assert "report.deferred_checks++" in text
    assert "report.all_required_observed = report.deferred_checks == 0" in text
    assert "report.all_passed = report.failed_checks == 0 && report.deferred_checks == 0" in text
    assert 'report.deferred_checks > 0\n                    ? "INCOMPLETE"' in text
    assert "deferred until Play Mode; no runtime pass claimed" in text


def test_live_display_health_requires_an_actual_runtime_measurement():
    text = source()
    assert "if (!monitor.HasMeasurement)" in text
    assert 'AddDeferred(report, "live_display_timing_health"' in text
    assert "runtime timing measurement not complete; no display-health claim recorded" in text
    assert 'Add(report, "live_display_timing_health", monitor.TimingHealthy' in text
    assert "!monitor.HasMeasurement || monitor.TimingHealthy" not in text


def test_readiness_report_keeps_physical_ssvep_explicitly_out_of_scope():
    text = source()
    assert "physical_ssvep_qualified = false" in text
    assert "photodiode + real EEG still required" in text
    assert 'readiness_status == "PASS"' in text
    assert 'readiness_status == "FAIL"' in text
    assert "Debug.LogWarning(summary)" in text
