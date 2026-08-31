from __future__ import annotations

import json
from pathlib import Path

from mindforge_neuro.markers import GameMarker
from mindforge_neuro.qualification import (
    build_promotion_manifest,
    build_software_gate,
    compare_marker_streams,
)


ROOT = Path(__file__).resolve().parents[1]


def marker(event: str, *, session: str, seq: int, realtime: float, value: float = 0.0) -> GameMarker:
    return GameMarker(
        schema="mindforge.game_marker.v1",
        seq=seq,
        session_id=session,
        calibration_id="cal-a",
        event=event,
        category="combat_outcome",
        unity_realtime_s=realtime,
        game_time_s=realtime,
        frame=seq * 10,
        fixed_tick=seq * 20,
        value=value,
        boss_phase=2,
    )


def test_software_gate_turns_junit_into_commit_bound_evidence(tmp_path: Path):
    junit = tmp_path / "pytest.xml"
    junit.write_text(
        '<testsuites><testsuite name="pytest" tests="47" failures="0" errors="0" skipped="2"/></testsuites>',
        encoding="utf-8",
    )
    report = build_software_gate(junit, commit="abc123")
    assert report.schema == "mindforge.software_gate.v1"
    assert report.commit == "abc123"
    assert report.tests == 47
    assert report.skipped == 2
    assert report.passed is True


def test_software_gate_fails_closed_on_test_failures(tmp_path: Path):
    junit = tmp_path / "pytest.xml"
    junit.write_text(
        '<testsuite name="pytest" tests="4" failures="1" errors="0" skipped="0"/>',
        encoding="utf-8",
    )
    assert build_software_gate(junit).passed is False


def test_marker_replay_comparison_ignores_transport_identity_but_not_semantics():
    reference = [
        marker("COUNTER_REFLECT", session="reference", seq=1, realtime=1.0),
        marker("SIGNAL_BREAK", session="reference", seq=2, realtime=2.0, value=1.0),
    ]
    replay = [
        marker("COUNTER_REFLECT", session="replay", seq=91, realtime=101.0),
        marker("SIGNAL_BREAK", session="replay", seq=92, realtime=103.0, value=1.0),
    ]
    report = compare_marker_streams(reference, replay, commit="abc123")
    assert report.commit == "abc123"
    assert report.exact_match is True
    assert report.similarity == 1.0
    assert report.first_mismatch_index == -1

    changed = [*replay[:-1], marker("VICTORY", session="replay", seq=92, realtime=103.0, value=1.0)]
    mismatch = compare_marker_streams(reference, changed, commit="abc123")
    assert mismatch.exact_match is False
    assert mismatch.first_mismatch_index == 1


def test_promotion_manifest_never_invents_unobserved_gates():
    manifest = build_promotion_manifest(
        commit="deadbeef",
        software_report={"passed": True, "commit": "deadbeef"},
        unity_report=None,
        replay_report={"exact_match": True, "commit": "deadbeef"},
    )
    by_gate = {entry["gate"]: entry for entry in manifest["gates"]}
    assert manifest["schema"] == "mindforge.promotion_manifest.v2"
    assert by_gate["P0"]["status"] == "PASS"
    assert by_gate["P1"]["status"] == "UNOBSERVED"
    assert by_gate["P4"]["status"] == "PASS"
    assert by_gate["P5"]["status"] == "UNOBSERVED"
    # Promotion is monotonic. P4 being green cannot jump over an unobserved P1.
    assert manifest["highest_contiguous_pass"] == "P0"
    assert manifest["fully_qualified"] is False


def test_promotion_manifest_rejects_unbound_and_stale_evidence():
    unbound = build_promotion_manifest(
        commit="targetsha",
        software_report={"passed": True},
    )
    assert unbound["gates"][0]["status"] == "UNBOUND"

    stale = build_promotion_manifest(
        commit="targetsha",
        software_report={"passed": True, "commit": "oldsha"},
    )
    assert stale["gates"][0]["status"] == "STALE"
    assert stale["gates"][0]["evidence_commit"] == "oldsha"


def test_controller_only_full_encounter_can_populate_p2_only_when_terminal_and_exact_commit():
    base = {
        "git_commit": "feedface",
        "controller_only_declared": True,
        "terminal_observed": True,
        "outcome": "VICTORY",
        "marker_count": 42,
    }
    manifest = build_promotion_manifest(
        commit="feedface",
        software_report={"passed": True, "commit": "feedface"},
        unity_report={"passed": True, "observed_git_sha": "feedface"},
        controller_report=base,
    )
    by_gate = {entry["gate"]: entry for entry in manifest["gates"]}
    assert by_gate["P0"]["status"] == "PASS"
    assert by_gate["P1"]["status"] == "PASS"
    assert by_gate["P2"]["status"] == "PASS"
    assert by_gate["P3"]["status"] == "UNOBSERVED"
    assert manifest["highest_contiguous_pass"] == "P2"

    no_terminal = build_promotion_manifest(
        commit="feedface",
        controller_report={**base, "terminal_observed": False, "outcome": "INCOMPLETE"},
    )
    assert no_terminal["gates"][2]["status"] == "FAIL"

    stale = build_promotion_manifest(
        commit="feedface",
        controller_report={**base, "git_commit": "cafebabe"},
    )
    assert stale["gates"][2]["status"] == "STALE"


def test_qualification_cli_exposes_commit_bound_controller_and_monotonic_enforcement():
    cli = (ROOT / "tools/mindforge_qualify.py").read_text(encoding="utf-8")
    assert 'compare.add_argument("--commit"' in cli
    assert 'manifest.add_argument("--controller")' in cli
    assert '"--require-through"' in cli
    assert "controller_report=read_optional(args.controller)" in cli
    assert "status != \"PASS\"" in cli
    assert "raise SystemExit(4)" in cli


def test_unity_gate_is_clean_checkout_batch_qualification_not_source_only():
    batch = (ROOT / "unity/Assets/Mindforge/Editor/CompetitionBatchRunner.cs").read_text(encoding="utf-8")
    gate = (ROOT / "unity/Assets/Mindforge/Editor/CompetitionGateValidator.cs").read_text(encoding="utf-8")
    runner = (ROOT / "tools/run_unity_gate.py").read_text(encoding="utf-8")
    workflow = (ROOT / ".github/workflows/test-neuro.yml").read_text(encoding="utf-8")

    assert "CompetitionSceneAssembler.BuildAndValidate()" in batch
    assert "EditorApplication.Exit(0)" in batch
    assert '"-batchmode"' in runner
    assert '"Mindforge.Editor.CompetitionBatchRunner.AssembleAndValidate"' in runner
    assert "ProjectVersion.txt" in runner
    assert "exact_unity_version_match" in runner
    assert "unity-gate1-run.json" in runner
    assert "mindforge-software-evidence" in workflow
    assert "pytest --junitxml" in workflow
    assert "Assemble exact-commit promotion manifest" in workflow
    assert "promotion-manifest.json" in workflow
    assert "--require-through P0" in workflow

    # The process-generated Gate 1 evidence must echo the exact Git identity supplied
    # to Unity, and the Python wrapper must require that identity to match before P1.
    assert "public string git_sha" in gate
    assert 'Environment.GetEnvironmentVariable("MINDFORGE_GIT_SHA")' in gate
    assert 'gate.get("git_sha")' in runner
    assert '"observed_git_sha"' in runner
    assert '"exact_git_sha_match"' in runner
    assert "gate_commit == commit" in runner
    assert "and exact_commit" in runner
