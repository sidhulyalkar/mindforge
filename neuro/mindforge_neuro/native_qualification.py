"""Local-native integration evidence, deliberately separate from EEG qualification.

This module never decides whether a boss phase is safe, nor whether EEG
selected an ability. Its only possible positive claims concern local reference
files, compiler exit codes and well-formed *unqualified* event captures.
Provenance labels in a tape can be forged by localhost senders, so even
a passing tape MUST be validated against a native loader log and recorded
gameplay before native interoperability is qualified.
"""
from __future__ import annotations

from dataclasses import asdict, dataclass
import hashlib
import json
import math
from pathlib import Path
from typing import Any

from .cuphead_stage import CupheadStageObservation, SOURCE as CUPHEAD_NATIVE_SOURCE
from .game_observation import GameObservation

GAMES = frozenset({"silksong", "hollow_knight", "cuphead"})
TARGETS = {"silksong": "net46", "hollow_knight": "net472", "cuphead": "net35"}
PROJECTS = {
    "silksong": (
        "mods/bepinex/Mindforge.SilksongObservation/Mindforge.SilksongObservation.csproj",
        "Mindforge.SilksongObservation.dll",
    ),
    "hollow_knight": (
        "mods/hollow-knight/Mindforge.HKObservation/Mindforge.HKObservation.csproj",
        "Mindforge.HKObservation.dll",
    ),
    "cuphead": (
        "mods/bepinex/Mindforge.CupheadStageObservation/Mindforge.CupheadStageObservation.csproj",
        "Mindforge.CupheadStageObservation.dll",
    ),
}


def digest_file(path: Path) -> dict[str, Any]:
    """A binary digest is not proof that an assembly works in the game."""
    if not path.is_file():
        return {"sha256": None, "bytes": 0, "present": False}
    h = hashlib.sha256()
    n = 0
    with path.open("rb") as fh:
        for block in iter(lambda: fh.read(1024 * 1024), b""):
            h.update(block)
            n += len(block)
    return {"sha256": h.hexdigest(), "bytes": n, "present": True}


def build_arguments(game: str, game_root: Path, output_dir: Path,
                    repo_root: Path) -> list[str]:
    """Only known source projects, no arbitrary script or deploy target."""
    if game not in GAMES:
        raise ValueError("unsupported native integration")
    project, _ = PROJECTS[game]
    managed_name = {
        "silksong": "Hollow Knight Silksong_Data",
        "hollow_knight": "hollow_knight_Data",
        "cuphead": "Cuphead_Data",
    }[game]
    # Inspect preflight to identify a nonstandard *_Data name before build;
    # do not guess alternate game folders and silently use wrong binaries.
    managed = game_root / managed_name / "Managed"
    props = [f"-p:OutputPath={output_dir.resolve()}/"]
    if game == "hollow_knight":
        props += [f"-p:HollowKnightManagedDir={managed.resolve()}"]
    elif game == "silksong":
        props += [
            f"-p:GameManagedDir={managed.resolve()}",
            f"-p:BepInExDir={(game_root / 'BepInEx' / 'core').resolve()}",
        ]
    else:
        props += [
            f"-p:CupheadManagedDir={managed.resolve()}",
            f"-p:CupheadBepInExCore={(game_root / 'BepInEx' / 'core').resolve()}",
        ]
    return [
        "dotnet", "build", str((repo_root / project).resolve()),
        "-c", "Release", "--nologo", *props,
    ]


def compile_evidence(game: str, *, preflight: dict[str, Any],
                     process_exit_code: int | None, dll: Path | None,
                     compiler_log: Path | None = None) -> dict[str, Any]:
    if game not in GAMES or preflight.get("game_id") != game or (
        preflight.get("schema") != "mindforge.native_build_preflight.v1"
    ):
        raise ValueError("native preflight game/schema mismatch")
    if process_exit_code is not None and type(process_exit_code) is not int:
        raise ValueError("compiler exit code must be integer or absent")
    artifact = digest_file(dll) if dll is not None else {
        "sha256": None, "bytes": 0, "present": False
    }
    log_hash = digest_file(compiler_log) if compiler_log else {
        "sha256": None, "bytes": 0, "present": False
    }
    compiled = bool(
        preflight.get("build_reference_prerequisites_present") is True
        and process_exit_code == 0
        and artifact["present"]
        and artifact["bytes"] > 0
    )
    return {
        "schema": "mindforge.native_build_evidence.v1",
        "game_id": game,
        "project": PROJECTS[game][0],
        "target_framework": TARGETS[game],
        "preflight_passed": preflight.get("build_reference_prerequisites_present") is True,
        "compiler_executed": process_exit_code is not None,
        "compiler_exit_code": process_exit_code,
        "compiled_artifact": artifact,
        "compiler_log": log_hash,
        "native_build_evidenced": compiled,
        "installed_into_game": False,
        "native_runtime_loaded": False,
        "physiological_qualification": False,
    }


def _ingest_records(game: str, records: list[str],
                    exact_version: str) -> dict[str, Any]:
    """Validate a local recording without claiming physical game provenance."""
    if game not in GAMES or not exact_version or len(exact_version) > 128:
        raise ValueError("game and exact build must be explicit")
    accepted = []
    rejects: dict[str, int] = {}
    def reject(reason: str) -> None:
        rejects[reason] = rejects.get(reason, 0) + 1

    session: str | None = None
    seq = -1
    last_s = -1.0
    for line in records:
        if not line.strip():
            continue
        try:
            packet = (CupheadStageObservation.from_json(line) if game == "cuphead"
                      else GameObservation.from_json(line))
        except (TypeError, ValueError, KeyError, UnicodeError, json.JSONDecodeError):
            reject("invalid_packet")
            continue
        expected_source = (CUPHEAD_NATIVE_SOURCE if game == "cuphead"
                           else "observed_game_hook_unqualified")
        source = packet.source if game == "cuphead" else packet.source_mode
        if packet.game_id != game or source != expected_source:
            reject("wrong_game_or_source")
            continue
        if packet.game_version != exact_version:
            reject("wrong_installed_build")
            continue
        if session is None:
            session = packet.session_id
        if packet.session_id != session:
            reject("interleaved_or_new_session")
            continue
        if packet.seq <= seq:
            reject("replayed_or_reordered")
            continue
        if packet.source_time_s < last_s:
            reject("source_clock_reversed")
            continue
        seq = packet.seq
        last_s = packet.source_time_s
        accepted.append(packet)

    labels = [
        (p.stage if game == "cuphead" else p.event) for p in accepted
    ]
    if game == "cuphead":
        # Stage observations are not boss evidence. Include actual scene
        # transitions to verify fail-unknown behavior.
        expected = {"level", "map"}
        has_transition = any(
            p.event == "scene_changed" and p.stage == "unknown"
            for p in accepted
        )
        coverage = expected.issubset(set(labels)) and has_transition
    else:
        coverage = (
            "hero_ready" in labels and "hero_attack" in labels
            and "scene_changed" in labels
        )
    result = {
        "schema": "mindforge.native_tape_audit.v1",
        "game_id": game,
        "exact_game_version": exact_version,
        "accepted_events": len(accepted),
        "rejected_events": rejects,
        "single_session_id_present": session is not None,
        "observed_labels": sorted(set(labels)),
        "minimum_scenario_coverage": coverage,
        "wire_consistency_passed": bool(accepted and not rejects and coverage),
        "video_correspondence_checked": False,
        "native_loader_verified": False,
        "gameplay_parity_verified": False,
        "combat_engagement_verified": False,
        "boss_phase_verified": False,
        "ssvep_window_verified": False,
        "neural_decision_authorized": False,
    }
    return result


def audit_tape(game: str, tape: Path | None, *, exact_version: str) -> dict[str, Any]:
    if tape is None or not tape.is_file():
        report = _ingest_records(game, [], exact_version)
        report["tape_present"] = False
        return report
    # Avoid loading arbitrary huge streams into memory from a game capture.
    if tape.stat().st_size > 16 * 1024 * 1024:
        raise ValueError("refusing oversized game-observation tape")
    text = tape.read_text(encoding="utf-8")
    report = _ingest_records(game, text.splitlines(), exact_version)
    report["tape_present"] = True
    report["tape_sha256"] = digest_file(tape)["sha256"]
    return report


def qualification_report(game: str, build: dict[str, Any],
                         tape: dict[str, Any]) -> dict[str, Any]:
    if game not in GAMES or build.get("game_id") != game or tape.get("game_id") != game:
        raise ValueError("cross-game qualification evidence")
    if (build.get("schema") != "mindforge.native_build_evidence.v1" or
            tape.get("schema") != "mindforge.native_tape_audit.v1"):
        raise ValueError("wrong evidence schema")
    gates = {
        "local_reference_files": bool(build.get("preflight_passed")),
        "native_compiler_produced_dll": bool(build.get("native_build_evidenced")),
        "unqualified_observation_tape_consistent": bool(tape.get("wire_consistency_passed")),
        # Not computer-verifiable here without local loader/footage evidence.
        "loader_and_hooks_validated_in_installed_game": False,
        "video_to_event_alignment_reviewed": False,
        "gameplay_frame_pacing_and_controls_parity": False,
        "visual_stimulus_physical_qualification": False,
        "participant_held_out_eeg_selection": False,
    }
    return {
        "schema": "mindforge.native_qualification.v1",
        "game_id": game,
        "gates": gates,
        "software_only_evidence_complete": all(list(gates.values())[:3]),
        "native_game_qualified": False,
        "real_bci_qualified": False,
        "ready_to_enable_gameplay_modification": False,
        "next_required_evidence": [
            key for key, passed in gates.items() if not passed
        ],
    }
