from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
RUNNER = ROOT / "tools" / "run_unity_calibrated_decoder.py"
MARKERS = ROOT / "neuro" / "mindforge_neuro" / "markers.py"


def test_game_marker_parser_preserves_layout_identity():
    text = MARKERS.read_text(encoding="utf-8")
    assert "trial_id: Optional[str]" in text
    assert 'trial_id=(str(payload["trial_id"]) if payload.get("trial_id") is not None else None)' in text


def test_calibrated_decoder_binds_training_and_runtime_epochs_to_one_layout():
    text = RUNNER.read_text(encoding="utf-8")
    for token in (
        "def marker_layout_id(marker: GameMarker)",
        '"--require-layout-id"',
        "active_layout_id: str | None = None",
        'reject_calibration("stimulus_layout_id_missing")',
        'f"stimulus_layout_changed:{active_layout_id or \'-\'}->{layout_id or \'-\'}"',
        '"stimulus_layout_id": active_layout_id',
        "if args.require_layout_id and layout_id != active_layout_id:",
        "runtime.cancel_epoch(marker.stimulus_epoch)",
    ):
        assert token in text


def test_layout_binding_does_not_turn_gaze_into_decoder_input():
    text = RUNNER.read_text(encoding="utf-8").lower()
    for forbidden in (
        "gaze_event",
        "gaze_profile",
        "udp 19746",
        "pupil_labs",
        "eye_image",
    ):
        assert forbidden not in text
