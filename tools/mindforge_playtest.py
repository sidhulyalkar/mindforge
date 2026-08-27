#!/usr/bin/env python3
"""Capture one explicit controller-only Unity playtest and emit a reproducible P2 evidence bundle."""
from __future__ import annotations

import argparse
import json
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

from mindforge_neuro.markers import UdpGameMarkerSource
from mindforge_neuro.playtest import CONTROLLER_ONLY_MODE, finalize_playtest_bundle
from mindforge_neuro.qualification import utc_now


def _default_output_dir() -> Path:
    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    return Path("experiments") / "playtests" / stamp


def _git_commit() -> str | None:
    try:
        root = Path(__file__).resolve().parents[1]
        value = subprocess.check_output(
            ["git", "rev-parse", "HEAD"], cwd=root, stderr=subprocess.DEVNULL, text=True
        ).strip()
        return value or None
    except (OSError, subprocess.CalledProcessError):
        return None


def _ask_rating(label: str) -> int | None:
    while True:
        value = input(f"{label} [1-5, Enter to skip]: ").strip()
        if not value:
            return None
        try:
            rating = int(value)
        except ValueError:
            print("Please enter an integer from 1 to 5, or press Enter to skip.")
            continue
        if 1 <= rating <= 5:
            return rating
        print("Please enter an integer from 1 to 5, or press Enter to skip.")


def _ask_yes_no(label: str) -> bool | None:
    while True:
        value = input(f"{label} [y/n, Enter to skip]: ").strip().lower()
        if not value:
            return None
        if value in {"y", "yes"}:
            return True
        if value in {"n", "no"}:
            return False
        print("Please enter y, n, or press Enter to skip.")


def _ask_text(label: str) -> str | None:
    value = input(f"{label} [Enter to skip]: ").strip()
    return value or None


def _write_human_review(output_dir: Path, capture_report) -> None:
    if not sys.stdin.isatty():
        print("Skipping --prompt-review because stdin is not interactive.")
        return

    print("\nHuman P2 review. These answers stay separate from machine telemetry and do not auto-pass P2.")
    try:
        review = {
            "schema": "mindforge.playtest_review.v1",
            "generated_utc": utc_now(),
            "session_id": capture_report.session_id,
            "git_commit": capture_report.git_commit,
            "marker_sha256": capture_report.marker_sha256,
            "outcome": capture_report.outcome,
            "clarity_1_to_5": _ask_rating("How clear were the controls and encounter goals?"),
            "responsiveness_1_to_5": _ask_rating("How responsive did movement/aim/combat feel?"),
            "enjoyment_1_to_5": _ask_rating("How enjoyable was the encounter?"),
            "intentionally_targeted_echo": _ask_yes_no("Could you intentionally aim at and attack an Echo node?"),
            "could_explain_bci_role": _ask_yes_no("Could you explain what EEG controls versus what your hands control?"),
            "favorite_moment": _ask_text("Favorite moment"),
            "confusing_moment": _ask_text("Most confusing or frustrating moment"),
            "next_improvement": _ask_text("One change you most want next"),
        }
    except (EOFError, KeyboardInterrupt):
        print("\nHuman review skipped; machine evidence remains intact.")
        return

    path = output_dir / "review.json"
    path.write_text(json.dumps(review, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(f"Human review: {path}")


def capture(args: argparse.Namespace) -> int:
    output_dir = Path(args.output_dir) if args.output_dir else _default_output_dir()
    output_dir.mkdir(parents=True, exist_ok=True)
    marker_path = output_dir / "markers.jsonl"
    marker_path.write_text("", encoding="utf-8")

    started_utc = utc_now()
    wait_deadline = time.monotonic() + args.wait_seconds
    active_started: float | None = None
    last_marker_at: float | None = None
    active_session: str | None = None
    stop_reason = "UNKNOWN"
    count = 0

    print(f"Mindforge P2 capture -> udp://{args.host}:{args.port}")
    print(f"Evidence directory: {output_dir}")
    print("Start the Unity competition scene, then press F8 in the Editor for explicit controller-only qualification.")
    print("The run is locked to the first observed Unity session_id and stops on VICTORY/DEFEAT.")

    try:
        with UdpGameMarkerSource(args.host, args.port, timeout_s=0.25) as source, marker_path.open(
            "a", encoding="utf-8", buffering=1
        ) as handle:
            while True:
                now = time.monotonic()
                marker = source.receive()

                if marker is None:
                    if active_session is None and now >= wait_deadline:
                        stop_reason = "WAIT_FOR_SESSION_TIMEOUT"
                        break
                    if active_started is not None and now - active_started >= args.max_seconds:
                        stop_reason = "MAX_DURATION"
                        break
                    if last_marker_at is not None and now - last_marker_at >= args.idle_seconds:
                        stop_reason = "MARKER_IDLE_TIMEOUT"
                        break
                    continue

                if not marker.session_id:
                    continue
                if active_session is None:
                    active_session = marker.session_id
                    active_started = now
                    print(f"Locked Unity session: {active_session}")
                if marker.session_id != active_session:
                    if args.verbose:
                        print(f"Ignoring marker from other session: {marker.session_id}")
                    continue

                handle.write(marker.to_json() + "\n")
                handle.flush()
                count += 1
                last_marker_at = now

                if args.verbose:
                    print(f"#{marker.seq:05d} {marker.category}/{marker.event} {marker.reason or ''}")

                if marker.event in {"VICTORY", "DEFEAT"}:
                    stop_reason = f"TERMINAL_{marker.event}"
                    break
    except KeyboardInterrupt:
        stop_reason = "INTERRUPTED"
        print("\nCapture interrupted; finalizing partial evidence bundle.")

    capture_report, encounter = finalize_playtest_bundle(
        marker_path,
        output_dir,
        stop_reason=stop_reason,
        started_utc=started_utc,
        ended_utc=utc_now(),
        expected_session_id=active_session,
        git_commit=_git_commit(),
    )

    print(json.dumps(capture_report.to_dict(), indent=2, sort_keys=True))
    print(json.dumps(encounter.to_dict(), indent=2, sort_keys=True))

    if args.prompt_review:
        _write_human_review(output_dir, capture_report)

    if count == 0:
        return 3
    if not capture_report.controller_only_declared:
        print(
            f"P2 FAIL: session never declared QUALIFICATION_MODE/{CONTROLLER_ONLY_MODE}. "
            "The evidence bundle is preserved but is not controller-only qualification evidence."
        )
        return 4
    if args.require_terminal and not capture_report.terminal_observed:
        return 2
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=19745, help="passive GameMarker observer lane")
    parser.add_argument("--output-dir", default=None)
    parser.add_argument("--wait-seconds", type=float, default=90.0, help="time allowed for first session marker")
    parser.add_argument("--max-seconds", type=float, default=900.0, help="maximum active-session capture duration")
    parser.add_argument("--idle-seconds", type=float, default=30.0, help="stop if an active session emits no markers this long")
    parser.add_argument("--require-terminal", action="store_true", help="return non-zero unless VICTORY or DEFEAT is observed")
    parser.add_argument(
        "--prompt-review",
        action="store_true",
        help="after capture, optionally write a separate human-reported review.json; never affects P2 pass/fail",
    )
    parser.add_argument("--verbose", action="store_true")
    return parser


def main() -> None:
    parser = build_parser()
    args = parser.parse_args()
    if args.wait_seconds <= 0 or args.max_seconds <= 0 or args.idle_seconds <= 0:
        parser.error("timeout values must be > 0")
    raise SystemExit(capture(args))


if __name__ == "__main__":
    main()
