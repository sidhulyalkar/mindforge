from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WISP = ROOT / "unity/Assets/Mindforge/SoulWisp/WispResonanceWindow.cs"
STIM = ROOT / "unity/Assets/Mindforge/SoulWisp/VepAuraStimulus.cs"
SOUL = ROOT / "unity/Assets/Mindforge/SoulWisp/SoulWispController.cs"
EVENT = ROOT / "unity/Assets/Mindforge/NeuralBridge/NeuralEvent.cs"
RUNNER = ROOT / "tools/run_unity_calibrated_decoder.py"
SCHEMA = ROOT / "contracts/neural_event.v2.schema.json"


def test_unity_requires_current_epoch_and_post_onset_evidence():
    wisp = WISP.read_text(encoding="utf-8")
    event = EVENT.read_text(encoding="utf-8")
    assert "public long stimulus_epoch = -1;" in event
    assert "public int evidence_ms;" in event
    assert "evt.stimulus_epoch != _windowId" in wisp
    assert "evt.evidence_ms < Mathf.Max(0, minimumEvidenceMs)" in wisp
    assert "private float settleSeconds = 0.09f" in wisp
    assert "private float listeningSeconds = 1.50f" in wisp


def test_stimulus_phase_is_frame_indexed_and_both_targets_share_start_frame():
    stim = STIM.read_text(encoding="utf-8")
    soul = SOUL.read_text(encoding="utf-8")
    assert "private float qualifiedRefreshHz = 120f" in stim
    assert "Time.frameCount - _sessionStartFrame" in stim
    assert "BeginWindow(double sharedStart, int sharedFrame)" in stim
    assert "int sharedFrame = Time.frameCount;" in soul
    assert "BeginWindow(sharedStart, sharedFrame)" in soul
    assert "Time.realtimeSinceStartupAsDouble - _sessionStart" not in stim


def test_production_runner_flushes_pre_epoch_lsl_and_uses_dynamic_runtime():
    runner = RUNNER.read_text(encoding="utf-8")
    assert "source.flush()" in runner
    assert "ResonanceEpochRuntime" in runner
    assert "NEURAL_WINDOW_LISTENING" in runner
    assert "SlidingWindowBuffer" not in runner
    assert "AuraSelectionRuntime" not in runner


def test_v2_schema_allows_epoch_provenance():
    schema = SCHEMA.read_text(encoding="utf-8")
    assert '"stimulus_epoch"' in schema
    assert '"evidence_ms"' in schema
