from __future__ import annotations

import json
import socket
import subprocess
import sys
import time
from pathlib import Path

from mindforge_neuro.game_observation import GameObservation


ROOT = Path(__file__).resolve().parents[1]


def free_udp_port() -> int:
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as s:
        s.bind(("127.0.0.1", 0))
        return s.getsockname()[1]


def test_hk_modhooks_udp_event_to_cosmetic_orb_then_scene_change_withdrawal():
    ports = set()
    while len(ports) < 3:
        port = free_udp_port()
        if port not in (19742, 19747, 19752, 19753, 19754, 19755, 19756):
            ports.add(port)
    obs_port, context_port, orb_port = sorted(ports)

    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as receiver, \
         socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sender:
        receiver.bind(("127.0.0.1", orb_port))
        receiver.settimeout(.20)
        context = subprocess.Popen(
            [sys.executable, str(ROOT / "tools/mindforge_orb_context_bridge.py"),
             "--game", "hollow_knight",
             "--input-port", str(context_port),
             "--output-port", str(orb_port), "--hz", "20"],
            cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE, text=True)
        activity = subprocess.Popen(
            [sys.executable, str(ROOT / "tools/mindforge_activity_preview.py"),
             "--game", "hollow_knight",
             "--expected-build", "exact-test-build",
             "--input-port", str(obs_port),
             "--context-port", str(context_port), "--seconds", "8", "--hz", "20"],
            cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE, text=True)
        try:
            # These wire events carry genuine Hollow Knight ModHooks labels,
            # but are synthetic fixtures rather than native-game observations.
            # CI runners may take longer than 550ms to start both Python
            # processes. Repeatedly send whole, ordered action bursts until
            # a real receiver acknowledgement (primed state) arrives.
            seen_primed = False
            sequence = 0
            deadline = time.monotonic() + 4.0
            while time.monotonic() < deadline and not seen_primed:
                for name in ("hero_ready", "hero_attack", "hero_attack"):
                    sequence += 1
                    packet = GameObservation.make(
                        "hollow_knight", name, sequence,
                        source_mode="observed_game_hook_unqualified",
                        session_id="native-test-session",
                        game_version="exact-test-build",
                        source_time_s=sequence * .1)
                    sender.sendto(packet.to_json().encode(),
                                  ("127.0.0.1", obs_port))
                    time.sleep(.075)

                next_burst = min(deadline, time.monotonic() + .48)
                while time.monotonic() < next_burst:
                    try:
                        raw, _ = receiver.recvfrom(4096)
                    except socket.timeout:
                        continue
                    frame = json.loads(raw)
                    assert frame["presentation_only"] is True
                    assert frame["stimulus_enabled"] is False
                    assert frame["placement"] != "player_anchor"
                    if frame["mode"] == "primed":
                        assert frame["source"] == "observed_game_hook_unqualified"
                        assert frame["selection_opportunity_only"] is False
                        seen_primed = True
                        break
            assert seen_primed, "the live UDP chain never produced a primed orb"

            scene = GameObservation.make(
                "hollow_knight", "scene_changed", sequence + 1,
                source_mode="observed_game_hook_unqualified",
                session_id="native-test-session", game_version="exact-test-build",
                in_game=False, source_time_s=.8)
            sender.sendto(scene.to_json().encode(), ("127.0.0.1", obs_port))
            seen_hidden = False
            deadline = time.monotonic() + 1.4
            while time.monotonic() < deadline:
                try:
                    raw, _ = receiver.recvfrom(4096)
                except socket.timeout:
                    continue
                frame = json.loads(raw)
                if frame["mode"] == "hidden":
                    seen_hidden = True
                    break
            assert seen_hidden, "scene change did not withdraw the orb"
        finally:
            for proc in (activity, context):
                proc.terminate()
                try:
                    proc.communicate(timeout=3)
                except subprocess.TimeoutExpired:
                    proc.kill()
                    proc.communicate(timeout=3)
