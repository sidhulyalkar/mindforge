from __future__ import annotations

import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


def test_session_logger_preserves_v2_neural_provenance_and_local_transport_evidence():
    logger = (ROOT / "unity/Assets/Mindforge/Telemetry/MindforgeSessionLogger.cs").read_text(encoding="utf-8")
    for token in (
        "neural_schema",
        "neural_seq",
        "neural_session_id",
        "calibration_id",
        "source_sample_start",
        "source_sample_end",
        "decoder_time_ns",
        "authority_ttl_ms",
        "transport_queue_depth",
        "dropped_packet_age",
        "dropped_backpressure",
        "dropped_expired_authority",
    ):
        assert token in logger
    assert 'schema = "mindforge.session.v1"' in logger
    assert "record.dropped_expired_authority = receiver.DroppedExpiredAuthority" in logger
    assert "raw EEG" in logger


def test_developer_cli_all_subcommand_parsers_start_cleanly():
    tool = ROOT / "tools/mindforge_dev.py"
    for args in (["--help"], ["decision", "--help"], ["replay", "--help"], ["marker-log", "--help"]):
        result = subprocess.run(
            [sys.executable, str(tool), *args],
            cwd=ROOT,
            capture_output=True,
            text=True,
            timeout=10,
            check=False,
        )
        assert result.returncode == 0, result.stderr
        assert "usage:" in result.stdout.lower()
