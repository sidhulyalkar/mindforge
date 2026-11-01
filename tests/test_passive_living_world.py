from __future__ import annotations

import json
from pathlib import Path
import socket
import subprocess
import sys

import numpy as np
import pytest

from mindforge_neuro.passive_features import (
    DEFAULT_PORT, NeuralFeatureFrame, PassiveUdpEmitter, demo_frames,
    posterior_alpha_relative_power,
)


def test_passive_frame_roundtrip_has_no_commands_or_raw_eeg():
    frame = NeuralFeatureFrame.create(1, 0.63, source_session_id="participant_session")
    payload = frame.to_json()
    assert NeuralFeatureFrame.from_json(payload) == frame
    assert "eeg_uv" not in payload and "AURA_SELECTED" not in payload
    assert json.loads(payload)["passive_only"] is True


@pytest.mark.parametrize("field,value", [
    ("passive_only", False), ("quality", float("nan")), ("value", -0.1),
    ("value", 1.1), ("artifact", 1), ("ttl_ms", 0), ("source_mode", "gameplay"),
    ("sample_end", -2), ("source_session_id", ""),
])
def test_rejects_invalid_or_authoritative_frames(field, value):
    record = json.loads(NeuralFeatureFrame.create(1, 0.5).to_json())
    record[field] = value
    with pytest.raises((ValueError, TypeError)):
        NeuralFeatureFrame.from_dict(record)


def test_rejects_unknown_fields_and_invalid_shape():
    record = json.loads(NeuralFeatureFrame.create(1, 0.5).to_json())
    record["raw_eeg"] = [[1, 2]]
    with pytest.raises(ValueError):
        NeuralFeatureFrame.from_dict(record)
    record.pop("raw_eeg")
    record.pop("feature")
    with pytest.raises(ValueError):
        NeuralFeatureFrame.from_dict(record)


def test_alpha_relative_power_increases_with_synthetic_posterior_alpha():
    rng = np.random.default_rng(10)
    fs = 250
    t = np.arange(500) / fs
    labels = ("FZ", "C3", "CZ", "C4", "PZ", "PO7", "OZ", "PO8")
    noise = rng.normal(0, 0.7, size=(8, 500))
    weak = noise.copy()
    strong = noise.copy()
    for idx in (4, 5, 6, 7):
        weak[idx] += 3 * np.sin(2 * np.pi * 20 * t)
        strong[idx] += 5 * np.sin(2 * np.pi * 10 * t)
    low = posterior_alpha_relative_power(weak, labels, fs)
    high = posterior_alpha_relative_power(strong, labels, fs)
    assert low is not None and high is not None
    assert 0 <= low[0] < high[0] <= 1
    assert high[1] >= 0.65


def test_alpha_extractor_fails_on_missing_posterior_and_nonfinite_data():
    rng = np.random.default_rng(6)
    eeg = rng.normal(size=(8, 500))
    with pytest.raises(ValueError, match="posterior"):
        posterior_alpha_relative_power(eeg, ("FZ", "C3", "CZ", "C4", "F3", "F4", "F7", "F8"), 250)
    eeg[4, 1] = float("nan")
    with pytest.raises(ValueError, match="finite"):
        posterior_alpha_relative_power(eeg, ("FZ", "C3", "CZ", "C4", "PZ", "PO7", "OZ", "PO8"), 250)


def test_demo_reproducible_and_always_labeled_as_simulated():
    a = demo_frames(steps=25, hz=5, seed=12)
    b = demo_frames(steps=25, hz=5, seed=12)
    assert a == b
    assert [f.seq for f in a] == list(range(25))
    assert all(f.source_mode == "simulated_feature" and f.passive_only for f in a)
    assert len({f.value for f in a}) >= 15


def test_emitter_restricts_to_loopback():
    with pytest.raises(ValueError, match="loopback"):
        PassiveUdpEmitter(host="0.0.0.0")
    with pytest.raises(ValueError):
        PassiveUdpEmitter(port=80)
    assert DEFAULT_PORT != 19742  # Sight/Guard authority port


def test_real_loopback_udp_packet_is_passive_only():
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sink:
        sink.bind(("127.0.0.1", 0))
        sink.settimeout(2.)
        port = sink.getsockname()[1]
        with PassiveUdpEmitter(port=port) as sender:
            sender.send(NeuralFeatureFrame.create(3, .55))
        data, address = sink.recvfrom(4096)
        assert address[0] == "127.0.0.1"
        assert NeuralFeatureFrame.from_json(data).passive_only


def test_cli_roundtrip_replay_downgrades_provenance(tmp_path):
    script = Path(__file__).resolve().parents[1] / "tools" / "mindforge_living_world.py"
    one, two = tmp_path / "demo.jsonl", tmp_path / "replay.jsonl"
    first = subprocess.run([sys.executable, str(script), "--demo", "--seconds", "1",
                            "--hz", "5", "--dry-run", "--output-tape", str(one)],
                           check=True, capture_output=True, text=True)
    assert "PASSIVE ONLY" in first.stdout
    assert len(one.read_text().splitlines()) == 5
    subprocess.run([sys.executable, str(script), "--replay", str(one), "--dry-run",
                    "--output-tape", str(two)], check=True, capture_output=True)
    sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))
    from mindforge_living_world import read_tape
    original, replay = read_tape(one), read_tape(two)
    assert len(original) == len(replay)
    assert all(f.source_mode == "feature_replay" for _, f in replay)
    assert all(f.source_session_id != original[0][1].source_session_id for _, f in replay)


def test_cli_rejects_corrupted_tape(tmp_path):
    sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))
    from mindforge_living_world import read_tape
    file = tmp_path / "bad.jsonl"
    file.write_text('{"offset_s": 0, "frame": {"schema": "game_command"}}\n')
    with pytest.raises(ValueError):
        read_tape(file)


def test_unity_boundaries_remain_passive():
    root = Path(__file__).resolve().parents[1] / "unity" / "Assets" / "Mindforge" / "LivingWorld"
    reader = (root / "LivingWorldPassiveReceiver.cs").read_text()
    model = (root / "PassiveFeatureFrame.cs").read_text()
    surface = (root / "LivingWorldReactiveSurface.cs").read_text()
    assert "IPAddress.Loopback" in reader and "19747" in reader
    assert "AddComponent<UdpNeuralReceiver>" not in reader and "EventReceived" not in reader
    assert "AURA_SELECTED" not in model
    assert "VepAuraStimulus" in surface and "ResonanceWindowActive" in surface
    assert "SetPropertyBlock(_original)" in surface
    assert "Application.targetFrameRate" not in surface
