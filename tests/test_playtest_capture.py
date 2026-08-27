from pathlib import Path

import pytest

from mindforge_neuro.markers import GameMarker, GAME_MARKER_V1
from mindforge_neuro.playtest import CONTROLLER_ONLY_MODE, finalize_playtest_bundle, sha256_file


ROOT = Path(__file__).resolve().parents[1]


def marker(
    seq: int,
    event: str,
    *,
    session: str = "session-a",
    t: float = 0.0,
    reason: str | None = None,
) -> GameMarker:
    return GameMarker(
        schema=GAME_MARKER_V1,
        seq=seq,
        session_id=session,
        calibration_id=None,
        event=event,
        category="qualification" if event == "QUALIFICATION_MODE" else "game",
        unity_realtime_s=t,
        game_time_s=t,
        frame=seq,
        fixed_tick=seq * 2,
        reason=reason if reason is not None else (CONTROLLER_ONLY_MODE if event == "QUALIFICATION_MODE" else None),
    )


def write_markers(path: Path, items: list[GameMarker]) -> None:
    path.write_text("\n".join(item.to_json() for item in items) + ("\n" if items else ""), encoding="utf-8")


def test_finalize_playtest_bundle_hashes_and_summarizes_one_controller_only_session(tmp_path: Path):
    markers = tmp_path / "markers.jsonl"
    write_markers(
        markers,
        [
            marker(1, "QUALIFICATION_MODE", t=1.0),
            marker(2, "PHASE_DASH", t=2.0),
            marker(3, "VICTORY", t=121.0),
        ],
    )

    capture, encounter = finalize_playtest_bundle(
        markers,
        tmp_path,
        stop_reason="TERMINAL_VICTORY",
        started_utc="2026-08-27T08:00:00Z",
        ended_utc="2026-08-27T08:03:00Z",
        expected_session_id="session-a",
        git_commit="deadbeef",
    )

    assert capture.schema == "mindforge.playtest_capture.v1"
    assert capture.session_id == "session-a"
    assert capture.marker_count == 3
    assert capture.terminal_observed is True
    assert capture.outcome == "VICTORY"
    assert capture.stop_reason == "TERMINAL_VICTORY"
    assert capture.git_commit == "deadbeef"
    assert capture.marker_sha256 == sha256_file(markers)
    assert len(capture.marker_sha256) == 64
    assert capture.controller_only_declared is True
    assert capture.qualification_modes == (CONTROLLER_ONLY_MODE,)
    assert encounter.outcome == "VICTORY"
    assert encounter.phase_dashes == 1
    assert (tmp_path / "capture.json").exists()
    assert (tmp_path / "encounter.json").exists()


def test_terminal_run_without_controller_only_declaration_is_preserved_but_not_promoted(tmp_path: Path):
    markers = tmp_path / "markers.jsonl"
    write_markers(markers, [marker(1, "PHASE_DASH", t=1.0), marker(2, "VICTORY", t=30.0)])

    capture, encounter = finalize_playtest_bundle(
        markers,
        tmp_path,
        stop_reason="TERMINAL_VICTORY",
        started_utc="2026-08-27T08:00:00Z",
    )

    assert encounter.outcome == "VICTORY"
    assert capture.terminal_observed is True
    assert capture.controller_only_declared is False
    assert capture.qualification_modes == ()


def test_finalize_playtest_bundle_rejects_cross_session_contamination(tmp_path: Path):
    markers = tmp_path / "markers.jsonl"
    write_markers(markers, [marker(1, "PHASE_DASH", session="a"), marker(2, "VICTORY", session="b")])

    with pytest.raises(ValueError, match="multiple Unity sessions"):
        finalize_playtest_bundle(
            markers,
            tmp_path,
            stop_reason="TEST",
            started_utc="2026-08-27T08:00:00Z",
        )


def test_empty_capture_is_preserved_as_incomplete_evidence(tmp_path: Path):
    markers = tmp_path / "markers.jsonl"
    write_markers(markers, [])

    capture, encounter = finalize_playtest_bundle(
        markers,
        tmp_path,
        stop_reason="WAIT_FOR_SESSION_TIMEOUT",
        started_utc="2026-08-27T08:00:00Z",
    )

    assert capture.session_id is None
    assert capture.marker_count == 0
    assert capture.terminal_observed is False
    assert capture.outcome == "INCOMPLETE"
    assert capture.controller_only_declared is False
    assert capture.qualification_modes == ()
    assert "NO_MARKERS" in encounter.diagnostic_flags


def test_p2_capture_cli_fails_closed_without_explicit_controller_only_provenance():
    tool = (ROOT / "tools/mindforge_playtest.py").read_text(encoding="utf-8")
    assert "if not capture_report.controller_only_declared:" in tool
    branch = tool[tool.index("if not capture_report.controller_only_declared:"):tool.index("if args.require_terminal")]
    assert "P2 FAIL" in branch
    assert "return 4" in branch
    assert "CONTROLLER_ONLY_MODE" in tool
