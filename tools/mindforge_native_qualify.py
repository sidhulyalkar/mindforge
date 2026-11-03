#!/usr/bin/env python3
"""Local native qualification: build artifacts, strict tape checks, honest gates.

Never installs mods or launches a game. Works on an owned, mod-enabled Windows
installation. The compiled DLL and private local logs stay in --evidence-dir.
The JSON results intentionally cannot assert runtime loading or EEG validity.

Examples:
  python tools/mindforge_native_qualify.py build --game cuphead \
      --game-root "C:/Program Files (x86)/Steam/steamapps/common/Cuphead" \
      --evidence-dir experiments/local-native/cuphead
  python tools/mindforge_native_qualify.py evaluate --game cuphead \
      --evidence-dir experiments/local-native/cuphead \
      --tape cuphead-stages.jsonl --expected-build "1.3.5"
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import shutil
import subprocess
import sys

from mindforge_neuro.native_qualification import (
    GAMES, PROJECTS, build_arguments, compile_evidence, digest_file,
    audit_tape, qualification_report,
)
from mindforge_neuro import native_qualification as qualification
from mindforge_neuro import game_observation as _keep_import  # no native/game import
# Preflight script is importable from repository's tools directory.
from mindforge_native_preflight import (
    inspect_cuphead, inspect_hollow_knight, inspect_silksong,
)

REPO = Path(__file__).resolve().parents[1]


def dump(path: Path, data: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2, sort_keys=True) + "\n",
                    encoding="utf-8")


def readable_json(path: Path) -> dict:
    if not path.is_file() or path.stat().st_size > 2_000_000:
        raise ValueError("missing or oversized evidence JSON: " + path.name)
    data = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(data, dict):
        raise ValueError("evidence JSON must be a single object")
    return data


def build(game: str, game_root: Path, evidence: Path) -> int:
    root = game_root.resolve()
    out = evidence.resolve()
    # All generated artifacts must remain OUTSIDE the game installation.
    if out == root or root in out.parents:
        raise ValueError("evidence output must not be inside the installed game")
    preflight_fn = {
        "cuphead": inspect_cuphead,
        "silksong": inspect_silksong,
        "hollow_knight": inspect_hollow_knight,
    }[game]
    preflight = preflight_fn(root)
    dump(out / "preflight.json", preflight)
    dll_name = PROJECTS[game][1]
    build_dir = out / "build"
    log = out / "build.log"
    exit_code = None
    if not preflight["build_reference_prerequisites_present"]:
        log.parent.mkdir(parents=True, exist_ok=True)
        log.write_text(
            "Native build not attempted: required local files missing.\n",
            encoding="utf-8"
        )
        print("Build blocked by missing local game assembly/loader references.", file=sys.stderr)
    elif not shutil.which("dotnet"):
        log.parent.mkdir(parents=True, exist_ok=True)
        log.write_text("Native build not attempted: dotnet not in PATH.\n",
                       encoding="utf-8")
        print("Build blocked: install .NET SDK and required targeting packs.", file=sys.stderr)
    else:
        build_dir.mkdir(parents=True, exist_ok=True)
        command = build_arguments(game, root, build_dir, REPO)
        try:
            result = subprocess.run(command, cwd=REPO, capture_output=True,
                                    text=True, timeout=240, check=False)
            exit_code = result.returncode
            # Logs can contain private machine paths and should remain local.
            log.write_text(result.stdout + "\n" + result.stderr, encoding="utf-8")
        except subprocess.TimeoutExpired:
            exit_code = 124
            log.write_text("Native compiler exceeded 240-second limit.\n",
                           encoding="utf-8")
        except OSError as exc:
            exit_code = 127
            log.write_text("Native compiler could not be executed: " +
                           type(exc).__name__ + "\n", encoding="utf-8")

    dll = build_dir / dll_name
    report = compile_evidence(
        game, preflight=preflight, process_exit_code=exit_code,
        dll=dll, compiler_log=log
    )
    dump(out / "build-evidence.json", report)
    print(json.dumps(report, indent=2, sort_keys=True))
    # Exit success only when a real local compiler produced a nonempty DLL.
    return 0 if report["native_build_evidenced"] else 2


def evaluate(game: str, evidence: Path, tape: Path | None,
             exact_version: str) -> int:
    out = evidence.resolve()
    build_report = readable_json(out / "build-evidence.json")
    if build_report.get("game_id") != game:
        raise ValueError("evidence directory is for a different game")
    if not isinstance(build_report.get("compiled_artifact"), dict):
        raise ValueError("missing compiler artifact digest")
    compiled = build_report["compiled_artifact"]
    # Verify that build evidence refers to the actual on-disk artifact.
    expected_dll = out / "build" / PROJECTS[game][1]
    actual = digest_file(expected_dll)
    if (compiled.get("sha256") != actual["sha256"]
            or compiled.get("bytes") != actual["bytes"]):
        raise ValueError("compiled DLL digest mismatch, repeat local build")
    # The CLI must not allow a stale build manifest to count as valid merely
    # because a different game DLL was copied into the evidence folder.
    source_build = dict(build_report)
    source_build["native_build_evidenced"] = bool(
        source_build.get("native_build_evidenced") and actual["present"]
    )
    tape_report = audit_tape(game, tape, exact_version=exact_version)
    dump(out / "tape-audit.json", tape_report)
    result = qualification_report(game, source_build, tape_report)
    dump(out / "qualification.json", result)
    print(json.dumps(result, indent=2, sort_keys=True))
    # This is an evaluation result, not a passing native game qualification.
    return 0


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    builder = commands.add_parser("build", help="Build only, NEVER install")
    builder.add_argument("--game", required=True, choices=sorted(GAMES))
    builder.add_argument("--game-root", type=Path, required=True)
    builder.add_argument("--evidence-dir", type=Path, required=True)

    evaluator = commands.add_parser("evaluate", help="Report actual vs pending evidence")
    evaluator.add_argument("--game", required=True, choices=sorted(GAMES))
    evaluator.add_argument("--evidence-dir", type=Path, required=True)
    evaluator.add_argument("--tape", type=Path,
                           help="Optional derived native observation JSONL")
    evaluator.add_argument("--expected-build", required=True,
                           help="Exact Application.version from installed game log")
    args = parser.parse_args()
    try:
        if args.command == "build":
            code = build(args.game, args.game_root, args.evidence_dir)
        else:
            code = evaluate(args.game, args.evidence_dir, args.tape,
                            args.expected_build)
    except (ValueError, OSError, json.JSONDecodeError) as exc:
        parser.error(str(exc))
        return
    raise SystemExit(code)


if __name__ == "__main__":
    main()
