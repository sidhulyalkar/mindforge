from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CAL = ROOT / "unity/Assets/Mindforge/Calibration/AwakeningCalibrationDirector.cs"
WISP = ROOT / "unity/Assets/Mindforge/SoulWisp/SoulWispController.cs"
RUNNER = ROOT / "tools/run_unity_calibrated_decoder.py"


def test_calibration_uses_same_simultaneous_coded_pair_as_gameplay():
    cal = CAL.read_text(encoding="utf-8")
    wisp = WISP.read_text(encoding="utf-8")
    assert "BeginCalibrationStimuli(bool swapSides)" in wisp
    assert "sightStimulus?.BeginWindow(sharedStart, sharedFrame);" in wisp
    assert "guardStimulus?.BeginWindow(sharedStart, sharedFrame);" in wisp
    assert "_calibrationStimuliActive" in wisp
    assert "SetDisplay(true, true);" in cal
    assert "BeginCalibrationStimuli(swapSides)" in cal
    assert "WaitForEndOfFrame" in cal


def test_calibration_counterbalances_each_semantic_target_across_screen_sides():
    cal = CAL.read_text(encoding="utf-8")
    wisp = WISP.read_text(encoding="utf-8")
    assert 'RunCounterbalancedTarget("sight"' in cal
    assert 'RunCounterbalancedTarget("guard"' in cal
    assert "firstSwap" in cal
    assert "!firstSwap" in cal
    assert "_calibrationSwapSides" in wisp
    assert "float sightSide = _calibrationSwapSides ? 1f : -1f;" in wisp
    assert "float guardSide = -sightSide;" in wisp
    assert "LOOK LEFT" not in cal
    assert "LOOK RIGHT" not in cal


def test_python_preserves_counterbalanced_segments_without_crossing_neutral_gap():
    runner = RUNNER.read_text(encoding="utf-8")
    assert "epochs: dict[str, list[np.ndarray]]" in runner
    assert "epochs.setdefault(stage, []).append(segment)" in runner
    assert "for segment in epochs[stage]:" in runner
    assert "split_windows(" in runner
    assert "segment, cfg.window_samples, hop" in runner
