#!/usr/bin/env python3
"""Run the clean-checkout Unity Gate 1 qualification.

This command intentionally requires a real local Unity installation. It does not
substitute a C# parser, mock UnityEngine, or treat source inspection as an observed
Editor compile.

The default path is:

    clean checkout
      -> pinned Unity from ProjectVersion.txt
      -> CompetitionBatchRunner.AssembleAndValidate
      -> generated competition scene
      -> CompetitionGateValidator report
      -> exact-version/commit wrapper evidence
"""
from __future__ import annotations

import argparse
import json
import os
import platform
import shutil
import subprocess
from datetime import datetime, timezone
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_PROJECT = ROOT / "unity"
DEFAULT_GATE_REPORT = ROOT / "experiments" / "reports" / "unity-gate1-latest.json"
DEFAULT_RUN_REPORT = ROOT / "experiments" / "reports" / "unity-gate1-run.json"
DEFAULT_LOG = ROOT / "experiments" / "reports" / "unity-gate1-editor.log"


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def project_version(project: Path) -> str:
    path = project / "ProjectSettings" / "ProjectVersion.txt"
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.startswith("m_EditorVersion:"):
            value = line.split(":", 1)[1].strip()
            if value:
                return value
    raise ValueError(f"could not read m_EditorVersion from {path}")


def git_commit(root: Path) -> str:
    try:
        result = subprocess.run(
            ["git", "rev-parse", "HEAD"], cwd=root, capture_output=True, text=True,
            timeout=5, check=True,
        )
        return result.stdout.strip() or "unknown"
    except Exception:
        return "unknown"


def unity_candidates(version: str) -> list[Path]:
    candidates: list[Path] = []
    env = os.environ.get("UNITY_EDITOR")
    if env:
        candidates.append(Path(env).expanduser())

    system = platform.system()
    if system == "Darwin":
        candidates.extend([
            Path(f"/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/MacOS/Unity"),
            Path("/Applications/Unity/Unity.app/Contents/MacOS/Unity"),
        ])
    elif system == "Windows":
        program_files = os.environ.get("ProgramFiles", r"C:\Program Files")
        candidates.extend([
            Path(program_files) / "Unity" / "Hub" / "Editor" / version / "Editor" / "Unity.exe",
            Path(program_files) / "Unity" / "Editor" / "Unity.exe",
        ])
    else:
        candidates.extend([
            Path(f"/opt/unityhub/Editor/{version}/Editor/Unity"),
            Path(f"/opt/Unity/Hub/Editor/{version}/Editor/Unity"),
            Path("/opt/Unity/Editor/Unity"),
        ])
        command = shutil.which("unity-editor") or shutil.which("Unity")
        if command:
            candidates.append(Path(command))
    return candidates


def locate_unity(version: str, override: str | None) -> Path:
    if override:
        path = Path(override).expanduser().resolve()
        if path.is_file():
            return path
        raise FileNotFoundError(f"Unity executable does not exist: {path}")
    for candidate in unity_candidates(version):
        if candidate.is_file():
            return candidate
    rendered = "\n  ".join(str(p) for p in unity_candidates(version))
    raise FileNotFoundError(
        f"Unity {version} was not found. Set UNITY_EDITOR or pass --unity. Checked:\n  {rendered}")


def build_command(editor: Path, project: Path, log_path: Path) -> list[str]:
    return [
        str(editor),
        "-batchmode",
        "-nographics",
        "-projectPath", str(project),
        "-executeMethod", "Mindforge.Editor.CompetitionBatchRunner.AssembleAndValidate",
        "-logFile", str(log_path),
    ]


def write_json(path: Path, payload: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unity", default=None, help="path to the Unity editor executable")
    parser.add_argument("--project", default=str(DEFAULT_PROJECT))
    parser.add_argument("--gate-report", default=str(DEFAULT_GATE_REPORT))
    parser.add_argument("--output", default=str(DEFAULT_RUN_REPORT))
    parser.add_argument("--log", default=str(DEFAULT_LOG))
    parser.add_argument("--commit", default=None)
    parser.add_argument("--dry-run", action="store_true", help="print the exact command without claiming P1")
    args = parser.parse_args()

    project = Path(args.project).resolve()
    gate_report = Path(args.gate_report).resolve()
    output = Path(args.output).resolve()
    log_path = Path(args.log).resolve()
    pinned = project_version(project)
    editor = locate_unity(pinned, args.unity)
    commit = args.commit or git_commit(ROOT)
    command = build_command(editor, project, log_path)

    if args.dry_run:
        print(json.dumps({"pinned_unity": pinned, "editor": str(editor), "commit": commit, "command": command}, indent=2))
        return

    gate_report.parent.mkdir(parents=True, exist_ok=True)
    log_path.parent.mkdir(parents=True, exist_ok=True)
    if gate_report.exists():
        gate_report.unlink()

    env = os.environ.copy()
    env["MINDFORGE_GIT_SHA"] = commit
    started = utc_now()
    result = subprocess.run(command, cwd=ROOT, env=env, check=False)
    finished = utc_now()

    gate = None
    parse_error = None
    if gate_report.exists():
        try:
            gate = json.loads(gate_report.read_text(encoding="utf-8"))
        except Exception as exc:
            parse_error = str(exc)

    editor_version = gate.get("editor_version") if isinstance(gate, dict) else None
    gate_commit = gate.get("git_sha") if isinstance(gate, dict) else None
    gate_passed = bool(gate and gate.get("passed"))
    exact_version = editor_version == pinned
    exact_commit = bool(commit and commit != "unknown" and gate_commit == commit)
    passed = result.returncode == 0 and gate_passed and exact_version and exact_commit
    report = {
        "schema": "mindforge.unity_gate1_run.v1",
        "generated_utc": finished,
        "started_utc": started,
        "commit": commit,
        "observed_git_sha": gate_commit,
        "exact_git_sha_match": exact_commit,
        "passed": passed,
        "clean_checkout_contract": "configure -> assemble -> validate",
        "pinned_unity_version": pinned,
        "observed_unity_version": editor_version,
        "exact_unity_version_match": exact_version,
        "editor_path": str(editor),
        "project_path": str(project),
        "return_code": result.returncode,
        "gate_report_path": str(gate_report),
        "editor_log_path": str(log_path),
        "gate_report": gate,
        "gate_report_parse_error": parse_error,
    }
    write_json(output, report)
    print(json.dumps(report, indent=2, sort_keys=True))
    if not passed:
        raise SystemExit(2)


if __name__ == "__main__":
    main()
