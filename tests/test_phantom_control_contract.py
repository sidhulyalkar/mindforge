from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def test_simulation_calibration_can_drive_phantom_but_live_never_does():
    src = (ROOT / "tools" / "run_unity_calibrated_decoder.py").read_text(encoding="utf-8")
    assert 'args.source_mode == "simulation"' in src
    assert 'args.stream_name == "UnicornMock"' in src
    assert 'PHANTOM_STAGE_COMMAND = {"baseline": "0", "sight": "1", "guard": "2"}' in src
    assert "phantom.send(PHANTOM_STAGE_COMMAND[stage])" in src
    assert "--disable-phantom-control" in src
    assert "source_mode == \"live\"" not in src


def test_torture_control_client_is_local_and_supports_explicit_silence():
    src = (ROOT / "tools" / "phantom_control.py").read_text(encoding="utf-8")
    assert 'default="127.0.0.1"' in src
    assert "default=19744" in src
    assert "sock.sendto" in src
    assert "silence:2.5" in src
