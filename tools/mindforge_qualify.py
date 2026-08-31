#!/usr/bin/env python3
"""Generate machine-readable Mindforge promotion evidence.

Examples:

    python tools/mindforge_qualify.py software \
      --junit experiments/reports/pytest.xml \
      --commit $(git rev-parse HEAD) \
      --output experiments/reports/software-gate.json

    python tools/mindforge_qualify.py compare-markers \
      experiments/markers/reference.jsonl experiments/markers/replay.jsonl \
      --commit $(git rev-parse HEAD) \
      --output experiments/reports/replay-comparison.json

    python tools/mindforge_qualify.py manifest \
      --commit $(git rev-parse HEAD) \
      --software experiments/reports/software-gate.json \
      --unity experiments/reports/unity-gate1-run.json \
      --controller experiments/playtests/<capture>/capture.json \
      --replay experiments/reports/replay-comparison.json \
      --require-through P2 \
      --output experiments/reports/promotion-manifest.json

A manifest is fail-closed. Evidence without exact Git identity is UNBOUND, evidence
from another revision is STALE, and a missing gate is UNOBSERVED. ``--require-through``
turns that policy into an executable release gate.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path

from mindforge_neuro.qualification import (
    build_promotion_manifest,
    build_software_gate,
    compare_marker_files,
    write_json,
)


GATES = tuple(f"P{i}" for i in range(14))


def read_optional(path: str | None) -> dict | None:
    if not path:
        return None
    return json.loads(Path(path).read_text(encoding="utf-8"))


def run_software(args: argparse.Namespace) -> None:
    report = build_software_gate(args.junit, commit=args.commit)
    write_json(args.output, report)
    print(json.dumps(report.__dict__, indent=2, sort_keys=True))
    if args.enforce and not report.passed:
        raise SystemExit(2)


def run_compare(args: argparse.Namespace) -> None:
    report = compare_marker_files(args.reference, args.candidate, commit=args.commit)
    write_json(args.output, report)
    print(json.dumps(report.__dict__, indent=2, sort_keys=True))
    if args.enforce and not report.exact_match:
        raise SystemExit(3)


def _require_through(report: dict, required_gate: str) -> tuple[bool, list[str]]:
    required_index = GATES.index(required_gate)
    failures: list[str] = []
    by_gate = {entry["gate"]: entry for entry in report["gates"]}
    for gate in GATES[: required_index + 1]:
        status = by_gate[gate]["status"]
        if status != "PASS":
            failures.append(f"{gate}={status}")
    return not failures, failures


def run_manifest(args: argparse.Namespace) -> None:
    report = build_promotion_manifest(
        commit=args.commit,
        software_report=read_optional(args.software),
        unity_report=read_optional(args.unity),
        controller_report=read_optional(args.controller),
        replay_report=read_optional(args.replay),
    )
    write_json(args.output, report)
    print(json.dumps(report, indent=2, sort_keys=True))

    if args.require_through:
        passed, failures = _require_through(report, args.require_through)
        if not passed:
            print(
                f"promotion requirement through {args.require_through} not met: "
                + ", ".join(failures)
            )
            raise SystemExit(4)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)

    software = sub.add_parser("software", help="convert pytest JUnit XML into a P0 evidence artifact")
    software.add_argument("--junit", required=True)
    software.add_argument("--commit", default="unknown")
    software.add_argument("--output", required=True)
    software.add_argument("--enforce", action="store_true")
    software.set_defaults(func=run_software)

    compare = sub.add_parser("compare-markers", help="compare semantic GameMarker consequences exactly")
    compare.add_argument("reference")
    compare.add_argument("candidate")
    compare.add_argument("--commit", default="unknown")
    compare.add_argument("--output", required=True)
    compare.add_argument("--enforce", action="store_true")
    compare.set_defaults(func=run_compare)

    manifest = sub.add_parser("manifest", help="assemble exact-commit gate reports without inventing missing evidence")
    manifest.add_argument("--commit", default="unknown")
    manifest.add_argument("--software")
    manifest.add_argument("--unity")
    manifest.add_argument("--controller")
    manifest.add_argument("--replay")
    manifest.add_argument(
        "--require-through",
        choices=GATES,
        help="exit nonzero unless every gate from P0 through this gate is an exact-commit PASS",
    )
    manifest.add_argument("--output", required=True)
    manifest.set_defaults(func=run_manifest)
    return parser


def main() -> None:
    args = build_parser().parse_args()
    args.func(args)


if __name__ == "__main__":
    main()
