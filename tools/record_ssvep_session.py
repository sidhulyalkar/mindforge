#!/usr/bin/env python3
"""Record Mindforge game-marker + rendered-SSVEP observer lanes without affecting gameplay.

This recorder deliberately stores normalized raw event records as JSONL. It does not compute
training tensors, window EEG, or assign intention labels. Those are derived artifacts and must
remain reproducible from the preserved source streams.
"""

from __future__ import annotations

import argparse
import json
import os
import socket
import subprocess
import time
from datetime import datetime, timezone
from pathlib import Path

from mindforge_neuro.markers import GameMarker
from mindforge_neuro.ssvep_observations import SsvepObservation


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def source_revision() -> str | None:
    try:
        return subprocess.check_output(
            ["git", "rev-parse", "HEAD"], text=True, stderr=subprocess.DEVNULL
        ).strip()
    except (OSError, subprocess.SubprocessError):
        return None


def bind_udp(host: str, port: int) -> socket.socket:
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    sock.bind((host, port))
    sock.setblocking(False)
    return sock


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-root", type=Path, default=Path("experiments/recordings"))
    parser.add_argument("--participant-id", default=None,
                        help="Optional pseudonymous research participant ID. Never use a real name.")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--game-marker-port", type=int, default=19745)
    parser.add_argument("--ssvep-observation-port", type=int, default=19746)
    parser.add_argument("--poll-seconds", type=float, default=0.005)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    capture_id = datetime.now(timezone.utc).strftime("capture-%Y%m%dT%H%M%SZ")
    capture_dir = args.output_root / capture_id
    capture_dir.mkdir(parents=True, exist_ok=False)

    manifest_path = capture_dir / "manifest.json"
    markers_path = capture_dir / "game_markers.jsonl"
    observations_path = capture_dir / "ssvep_observations.jsonl"

    marker_socket = bind_udp(args.host, args.game_marker_port)
    observation_socket = bind_udp(args.host, args.ssvep_observation_port)
    sockets = {
        marker_socket: "marker",
        observation_socket: "observation",
    }

    started_utc = utc_now()
    started_monotonic = time.monotonic()
    marker_count = 0
    observation_count = 0
    invalid_marker_count = 0
    invalid_observation_count = 0
    session_ids: set[str] = set()

    print(f"Recording Mindforge SSVEP evidence to {capture_dir}")
    print(f"  game markers:       udp://{args.host}:{args.game_marker_port}")
    print(f"  SSVEP observations: udp://{args.host}:{args.ssvep_observation_port}")
    print("Press Ctrl+C to stop cleanly.")

    try:
        with markers_path.open("a", encoding="utf-8") as markers_file, \
             observations_path.open("a", encoding="utf-8") as observations_file:
            while True:
                had_packet = False
                for sock, kind in tuple(sockets.items()):
                    try:
                        raw, _remote = sock.recvfrom(65535)
                    except BlockingIOError:
                        continue
                    had_packet = True

                    if kind == "marker":
                        try:
                            marker = GameMarker.from_json(raw)
                        except (UnicodeDecodeError, json.JSONDecodeError, ValueError, TypeError, OverflowError):
                            invalid_marker_count += 1
                            continue
                        session_ids.add(marker.session_id)
                        markers_file.write(marker.to_json() + "\n")
                        markers_file.flush()
                        marker_count += 1
                    else:
                        try:
                            observation = SsvepObservation.from_json(raw)
                        except (UnicodeDecodeError, json.JSONDecodeError, ValueError, TypeError, OverflowError):
                            invalid_observation_count += 1
                            continue
                        session_ids.add(observation.session_id)
                        observations_file.write(json.dumps(observation.to_dict(), separators=(",", ":"), sort_keys=True) + "\n")
                        observations_file.flush()
                        observation_count += 1

                if not had_packet:
                    time.sleep(max(0.001, args.poll_seconds))
    except KeyboardInterrupt:
        pass
    finally:
        marker_socket.close()
        observation_socket.close()

    ended_utc = utc_now()
    manifest = {
        "schema": "mindforge.ssvep_capture_manifest.v1",
        "capture_id": capture_id,
        "participant_id": args.participant_id,
        "participant_id_is_pseudonymous": args.participant_id is not None,
        "started_utc": started_utc,
        "ended_utc": ended_utc,
        "duration_s": max(0.0, time.monotonic() - started_monotonic),
        "source_revision": source_revision(),
        "host": args.host,
        "game_marker_port": args.game_marker_port,
        "ssvep_observation_port": args.ssvep_observation_port,
        "session_ids": sorted(session_ids),
        "counts": {
            "game_markers": marker_count,
            "ssvep_observations": observation_count,
            "invalid_game_markers": invalid_marker_count,
            "invalid_ssvep_observations": invalid_observation_count,
        },
        "files": {
            "game_markers": markers_path.name,
            "ssvep_observations": observations_path.name,
        },
        "raw_eeg_included": False,
        "note": (
            "Observer capture only. Join raw EEG/gaze externally by pseudonymous participant, "
            "Unity session, stimulus epoch/trial, and synchronized timestamps. Do not infer "
            "physiological ground truth from editor simulation."
        ),
    }
    manifest_path.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")

    print(
        f"Stopped: {marker_count} game markers, {observation_count} SSVEP observations "
        f"across {len(session_ids)} Unity session(s)."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
