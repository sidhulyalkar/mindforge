from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "unity" / "Assets" / "Mindforge"


def read(*parts: str) -> str:
    return UNITY.joinpath(*parts).read_text(encoding="utf-8")


def test_game_marker_uses_one_game_session_and_separate_calibration_identity():
    context = read("Telemetry", "MindforgeSessionContext.cs")
    marker = read("Telemetry", "GameMarker.cs")
    sender = read("Telemetry", "UdpGameMarkerSender.cs")
    logger = read("Telemetry", "MindforgeSessionLogger.cs")
    decoder = (ROOT / "tools/run_unity_calibrated_decoder.py").read_text(encoding="utf-8")

    assert "GameSessionId" in context and "StartedUtc" in context
    assert "MindforgeSessionContext.GameSessionId" in sender
    assert "MindforgeSessionContext.GameSessionId" in logger
    assert "MindforgeSessionContext.StartedUtc" in logger
    assert "public string calibration_id" in marker
    assert "calibration_id = calibrationId" in sender
    assert "marker.calibration_id or marker.session_id" in decoder
    assert "session_id=active_game_session" in decoder
    assert "calibration_id=active_calibration" in decoder


def test_game_marker_has_separate_primary_processing_and_passive_observer_lanes():
    sender = read("Telemetry", "UdpGameMarkerSender.cs")
    marker_source = (ROOT / "neuro/mindforge_neuro/markers.py").read_text(encoding="utf-8")
    cli = (ROOT / "tools/mindforge_dev.py").read_text(encoding="utf-8")

    assert "private int port = 19743" in sender
    assert "private int observerPort = 19745" in sender
    assert "Send(bytes, port, \"primary\")" in sender
    assert "Send(bytes, observerPort, \"observer\")" in sender
    assert 'port: int = 19745' in marker_source
    assert 'default=19745' in cli
    assert "19743 is reserved for the active processing consumer" in cli
