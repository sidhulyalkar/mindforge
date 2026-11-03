from __future__ import annotations

import hashlib
import json
import subprocess
import sys
from pathlib import Path

import pytest

from mindforge_neuro.native_qualification import (
    PROJECTS, TARGETS, _ingest_records, audit_tape, build_arguments,
    compile_evidence, qualification_report,
)
from mindforge_neuro.game_observation import GameObservation
from mindforge_neuro.cuphead_stage import CupheadStageObservation, SOURCE


ROOT = Path(__file__).resolve().parents[1]
CLI = ROOT / "tools/mindforge_native_qualify.py"


def preflight(game="silksong", *, present=True):
    return {
        "schema": "mindforge.native_build_preflight.v1",
        "game_id": game,
        "build_reference_prerequisites_present": present,
    }


@pytest.mark.parametrize("game,expected_project,target,property_name", [
    ("silksong", "Mindforge.SilksongObservation.csproj", "net46",
     "GameManagedDir"),
    ("hollow_knight", "Mindforge.HKObservation.csproj", "net472",
     "HollowKnightManagedDir"),
    ("cuphead", "Mindforge.CupheadStageObservation.csproj", "net35",
     "CupheadManagedDir"),
])
def test_local_builder_only_compiles_known_source_not_game_dir(
    tmp_path, game, expected_project, target, property_name
):
    game_root = tmp_path / "owned-game"
    evidence = tmp_path / "qualification"
    command = build_arguments(game, game_root, evidence, ROOT)
    assert command[:2] == ["dotnet", "build"]
    assert command[2].endswith(expected_project)
    assert TARGETS[game] == target
    assert any(property_name in arg for arg in command)
    assert any(arg.startswith("-p:OutputPath=") for arg in command)
    assert "BepInEx/plugins" not in " ".join(command)
    assert "DeployToPlugins" not in " ".join(command)
    assert not any(arg.startswith("-p:MindforgeGame=") for arg in command)
    with pytest.raises(ValueError):
        build_arguments("unknown_game", game_root, evidence, ROOT)


def test_compiler_exit_and_real_artifact_both_required(tmp_path):
    compiled = tmp_path / "Mindforge.SilksongObservation.dll"
    compiled.write_bytes(b"synthetic-not-a-real-dll")
    log = tmp_path / "build.log"
    log.write_text("local synthetic compiler log\n", encoding="utf8")
    assert not compile_evidence(
        "silksong", preflight=preflight(), process_exit_code=1,
        dll=compiled, compiler_log=log,
    )["native_build_evidenced"]
    assert not compile_evidence(
        "silksong", preflight=preflight(present=False),
        process_exit_code=0, dll=compiled,
    )["native_build_evidenced"]
    result = compile_evidence(
        "silksong", preflight=preflight(), process_exit_code=0,
        dll=compiled, compiler_log=log,
    )
    assert result["native_build_evidenced"]
    assert result["compiled_artifact"]["sha256"] == hashlib.sha256(
        b"synthetic-not-a-real-dll").hexdigest()
    assert not result["installed_into_game"]
    assert not result["native_runtime_loaded"]
    assert not result["physiological_qualification"]
    assert not compile_evidence(
        "silksong", preflight=preflight(), process_exit_code=0,
        dll=tmp_path / "no-dll-file.dll",
    )["native_build_evidenced"]
    with pytest.raises(ValueError):
        compile_evidence("cuphead", preflight=preflight("silksong"),
                         process_exit_code=0, dll=compiled)


def native_game_observation(game, event, seq, t=.1, version="pinned-1", session="a"):
    data = json.loads(GameObservation.make(
        game, event, seq, source_time_s=t, session_id=session,
    ).to_json())
    data["source_mode"] = "observed_game_hook_unqualified"
    data["game_version"] = version
    return json.dumps(data, sort_keys=True)


def native_cuphead_stage(stage, event, seq, t=.1, version="pinned-1", session="a"):
    data = json.loads(CupheadStageObservation.synthetic(
        stage, seq, event=event, source_time_s=t, session_id=session,
    ).to_json())
    data["source"] = SOURCE
    data["game_version"] = version
    return json.dumps(data, sort_keys=True)


@pytest.mark.parametrize("game", ["silksong", "hollow_knight"])
def test_unqualified_player_tape_can_be_consistent_but_never_boss_proof(game):
    events = [
        native_game_observation(game, "hero_ready", 1, .1),
        native_game_observation(game, "hero_attack", 2, .2),
        native_game_observation(game, "scene_changed", 3, .3),
    ]
    tape = _ingest_records(game, events, "pinned-1")
    assert tape["wire_consistency_passed"]
    assert tape["minimum_scenario_coverage"]
    assert tape["observed_labels"] == ["hero_attack", "hero_ready", "scene_changed"]
    assert tape["native_loader_verified"] is False
    assert tape["combat_engagement_verified"] is False
    assert tape["neural_decision_authorized"] is False
    build = compile_evidence(
        game, preflight=preflight(game), process_exit_code=None, dll=None
    )
    final = qualification_report(game, build, tape)
    assert not final["native_game_qualified"]
    assert not final["real_bci_qualified"]
    assert not final["ready_to_enable_gameplay_modification"]


def test_cuphead_stage_tape_is_not_a_combat_or_parry_observation():
    tape = _ingest_records("cuphead", [
        native_cuphead_stage("map", "sample", 1, .1),
        native_cuphead_stage("unknown", "scene_changed", 2, .2),
        native_cuphead_stage("level", "sample", 3, .3),
    ], "pinned-1")
    assert tape["wire_consistency_passed"]
    assert not tape["boss_phase_verified"]
    assert not tape["ssvep_window_verified"]
    assert not tape["neural_decision_authorized"]


def test_cuphead_stage_requires_actual_map_level_and_transition():
    for events in [
        [native_cuphead_stage("level", "sample", 1, .1)],
        [native_cuphead_stage("map", "sample", 1, .1),
         native_cuphead_stage("level", "sample", 2, .2)],
        [native_cuphead_stage("map", "sample", 1, .1),
         native_cuphead_stage("unknown", "scene_changed", 2, .2)],
    ]:
        assert not _ingest_records("cuphead", events, "pinned-1")[
            "wire_consistency_passed"]


def test_cross_game_synthetic_wrong_version_replay_and_clock_reversal_rejected():
    tape = _ingest_records("hollow_knight", [
        native_game_observation("hollow_knight", "hero_ready", 1, .1),
        native_game_observation("hollow_knight", "hero_ready", 1, .2),
        native_game_observation("hollow_knight", "hero_attack", 2, .05),
        native_game_observation("silksong", "hero_attack", 3, .3),
        native_game_observation("hollow_knight", "hero_attack", 4, .4,
                                version="not-pinned"),
        GameObservation.make("hollow_knight", "hero_attack", 5).to_json(),
        native_game_observation("hollow_knight", "scene_changed", 6, .5,
                                session="another"),
    ], "pinned-1")
    assert not tape["wire_consistency_passed"]
    assert tape["rejected_events"]["replayed_or_reordered"] == 1
    assert tape["rejected_events"]["source_clock_reversed"] == 1
    assert tape["rejected_events"]["wrong_game_or_source"] == 2
    assert tape["rejected_events"]["wrong_installed_build"] == 1
    assert tape["rejected_events"]["interleaved_or_new_session"] == 1


def test_native_tape_file_size_limits_and_missing_file_are_not_success(tmp_path):
    missing = audit_tape("cuphead", tmp_path / "missing.jsonl", exact_version="pinned-1")
    assert not missing["tape_present"]
    assert missing["accepted_events"] == 0
    assert not missing["wire_consistency_passed"]
    big = tmp_path / "too-big.jsonl"
    with big.open("wb") as fp:
        fp.truncate(16 * 1024 * 1024 + 1)
    with pytest.raises(ValueError):
        audit_tape("cuphead", big, exact_version="pinned-1")


def test_qualification_rejects_cross_game_evidence():
    build = compile_evidence(
        "silksong", preflight=preflight(), process_exit_code=None, dll=None
    )
    tape = _ingest_records("cuphead", [], "pinned-1")
    with pytest.raises(ValueError):
        qualification_report("silksong", build, tape)


def test_local_cli_fails_without_files_and_never_attempts_fake_compile(tmp_path):
    game = tmp_path / "empty-install"
    game.mkdir()
    out = tmp_path / "result"
    r = subprocess.run(
        [sys.executable, str(CLI), "build",
         "--game", "hollow_knight", "--game-root", str(game),
         "--evidence-dir", str(out)],
        cwd=ROOT, text=True, capture_output=True, timeout=12,
    )
    assert r.returncode == 2
    build = json.loads((out / "build-evidence.json").read_text())
    assert build["native_build_evidenced"] is False
    assert build["compiler_executed"] is False
    assert not (out / "build" / PROJECTS["hollow_knight"][1]).exists()
    assert not list(game.iterdir())


def test_local_cli_forbids_evidence_output_inside_game_directory(tmp_path):
    game = tmp_path / "my-game"
    game.mkdir()
    r = subprocess.run(
        [sys.executable, str(CLI), "build", "--game", "cuphead",
         "--game-root", str(game),
         "--evidence-dir", str(game / "should-not-be-created")],
        cwd=ROOT, text=True, capture_output=True, timeout=12,
    )
    assert r.returncode != 0
    assert "must not be inside the installed game" in r.stderr
    assert not list(game.iterdir())


def test_local_cli_evaluate_checks_compiled_dll_hash_before_claims(tmp_path):
    out = tmp_path / "evidence"
    dll = out / "build" / PROJECTS["cuphead"][1]
    dll.parent.mkdir(parents=True)
    dll.write_bytes(b"fixture")
    info = compile_evidence(
        "cuphead", preflight=preflight("cuphead"), process_exit_code=0,
        dll=dll,
    )
    (out / "build-evidence.json").write_text(
        json.dumps(info), encoding="utf8"
    )
    tape = tmp_path / "native-tape.jsonl"
    tape.write_text("\n".join([
        native_cuphead_stage("map", "sample", 1, .1),
        native_cuphead_stage("unknown", "scene_changed", 2, .2),
        native_cuphead_stage("level", "sample", 3, .3),
    ]) + "\n", encoding="utf8")
    args = [
        sys.executable, str(CLI), "evaluate",
        "--game", "cuphead", "--evidence-dir", str(out),
        "--tape", str(tape), "--expected-build", "pinned-1",
    ]
    r = subprocess.run(args, cwd=ROOT, text=True, capture_output=True, timeout=12)
    assert r.returncode == 0, r.stderr
    report = json.loads((out / "qualification.json").read_text())
    assert report["software_only_evidence_complete"] is True
    assert not report["native_game_qualified"]
    assert not report["real_bci_qualified"]
    assert not report["ready_to_enable_gameplay_modification"]
    dll.write_bytes(b"tampered")
    r = subprocess.run(args, cwd=ROOT, text=True, capture_output=True, timeout=12)
    assert r.returncode != 0
    assert "digest mismatch" in r.stderr
