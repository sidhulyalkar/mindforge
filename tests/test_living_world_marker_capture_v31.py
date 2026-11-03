from __future__ import annotations

from pathlib import Path
import sys

import pytest

from mindforge_neuro.markers import GameMarker

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))
import mindforge_capture_game_markers as capture  # noqa: E402


def marker(seq, session="game-one", schema="mindforge.game_marker.v1"):
    return GameMarker(
        schema=schema, seq=seq, session_id=session,
        event="COMBAT_ACTION", category="combat",
        unity_realtime_s=10.0, game_time_s=2.0,
        frame=11, fixed_tick=5, action="DODGE",
    )


class FakeSource:
    def __init__(self, rows):
        self.rows = list(rows)
    def receive(self):
        return self.rows.pop(0) if self.rows else None


def test_capture_rejects_other_sessions_duplicate_and_out_of_order_packets():
    source = FakeSource([
        None, marker(1), marker(1), marker(3),
        marker(2), marker(4, session="second"),
        marker(5, schema="mindforge.calibration_marker.v1"),
    ])
    records = capture.collect_markers(source, seconds=.02)
    assert [m.seq for m in records] == [1, 3]
    assert {m.session_id for m in records} == {"game-one"}


def test_capture_rejects_invalid_limits():
    with pytest.raises(ValueError):
        capture.collect_markers(FakeSource([]), seconds=-1)
    with pytest.raises(ValueError):
        capture.collect_markers(FakeSource([]), seconds=2, limit=0)


def test_empty_capture_cannot_be_presented_as_success(tmp_path, monkeypatch):
    class EmptyContext:
        def __init__(self, *args, **kwargs): pass
        def __enter__(self): return FakeSource([])
        def __exit__(self, *args): return False
    monkeypatch.setattr(capture, "UdpGameMarkerSource", EmptyContext)
    dest = tmp_path / "empty.jsonl"
    assert capture.main(["--seconds", ".02", "--output", str(dest)]) == 2
    assert not dest.exists()


def test_capture_writes_real_marker_jsonl_without_eeg(tmp_path, monkeypatch):
    class FixtureContext:
        def __init__(self, *args, **kwargs): pass
        def __enter__(self): return FakeSource([marker(1), marker(2)])
        def __exit__(self, *args): return False
    monkeypatch.setattr(capture, "UdpGameMarkerSource", FixtureContext)
    dest = tmp_path / "capture.jsonl"
    assert capture.main(["--seconds", ".02", "--output", str(dest)]) == 0
    lines = dest.read_text().splitlines()
    assert len(lines) == 2
    assert "eeg_uv" not in dest.read_text()
    assert all(GameMarker.from_json(line).session_id == "game-one" for line in lines)
