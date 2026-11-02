from __future__ import annotations

import json
import socket
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


def test_synthetic_mod_bridge_emits_real_loopback_datagrams():
    """The cosmetic transport can be tested end-to-end with no game or EEG."""
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as listener:
        listener.bind(("127.0.0.1", 0))
        listener.settimeout(3.0)
        out_port = listener.getsockname()[1]
        # The demo does not bind an input listener; use a distinct valid port.
        in_port = 19752 if out_port != 19752 else 19751
        proc = subprocess.Popen(
            [
                sys.executable,
                str(ROOT / "tools/mindforge_mod_bridge.py"),
                "--game", "silksong",
                "--demo",
                "--seconds", "0.7",
                "--hz", "20",
                "--input-port", str(in_port),
                "--output-port", str(out_port),
            ],
            cwd=ROOT,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
        )
        try:
            payload, peer = listener.recvfrom(4096)
            packet = json.loads(payload)
            assert peer[0] == "127.0.0.1"
            assert packet["schema"] == "mindforge.mod_state.v1"
            assert packet["game_id"] == "silksong"
            assert packet["source_mode"] == "simulated_decision"
            assert packet["presentation_only"] is True
            assert packet["link"] == "connected"
            assert packet["sight_active"] is True
            assert "raw_eeg" not in packet
            assert "target_frequency" not in packet
        finally:
            if proc.poll() is None:
                proc.terminate()
            proc.communicate(timeout=5)
