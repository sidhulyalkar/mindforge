from __future__ import annotations

import json
import socket
import subprocess
import sys
import time
from pathlib import Path

from mindforge_neuro.context_transport import make_context_packet

ROOT = Path(__file__).resolve().parents[1]


def _free_udp_port() -> int:
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as probe:
        probe.bind(("127.0.0.1", 0))
        return probe.getsockname()[1]


def test_context_sidecar_fails_hidden_then_renders_and_expires():
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as monitor:
        monitor.bind(("127.0.0.1", 0))
        monitor.settimeout(0.25)
        output_port = monitor.getsockname()[1]
        input_port = _free_udp_port()
        while input_port == output_port or input_port in (19742, 19747, 19752, 19753):
            input_port = _free_udp_port()
        process = subprocess.Popen([
            sys.executable, str(ROOT / "tools/mindforge_orb_context_bridge.py"),
            "--game", "cuphead", "--hz", "20",
            "--input-port", str(input_port),
            "--output-port", str(output_port),
        ], cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE, text=True)
        try:
            with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sender:
                start = time.monotonic()
                got_hidden = False
                got_present = False
                got_expired = False
                sequence = 0
                present_at = None
                while time.monotonic() - start < 4.0 and not got_expired:
                    if not got_present:
                        sequence += 1
                        frame = make_context_packet(
                            "cuphead", "synthetic-sidecar-test", sequence,
                            ttl_ms=200, enemy_engaged=True)
                        sender.sendto(frame.to_json().encode("utf-8"),
                                      ("127.0.0.1", input_port))
                    try:
                        raw, peer = monitor.recvfrom(4096)
                    except socket.timeout:
                        continue
                    assert peer[0] == "127.0.0.1"
                    state = json.loads(raw)
                    assert state["schema"] == "mindforge.orb_context_state.v1"
                    assert state["game_id"] == "cuphead"
                    assert state["presentation_only"] is True
                    assert state["stimulus_enabled"] is False
                    assert state["placement"] != "player_anchor"
                    if state["mode"] == "hidden" and not got_present:
                        got_hidden = True
                    if state["mode"] == "present":
                        got_present = True
                        present_at = time.monotonic()
                    if got_present and state["mode"] == "hidden" and (
                        time.monotonic() - present_at >= 0.15
                    ):
                        got_expired = True
                assert got_present, "game-context packet never reached presentation bridge"
                assert got_expired, "orb did not withdraw after context TTL"
        finally:
            process.terminate()
            try:
                process.communicate(timeout=3)
            except subprocess.TimeoutExpired:
                process.kill()
                process.communicate(timeout=3)
