from __future__ import annotations

import numpy as np

from mindforge_neuro import AuraTarget, SsvepConfig, SsvepDecoder
from mindforge_neuro.calibration import calibrate_decoder
from mindforge_neuro.runtime import AuraSelectionRuntime


def synthetic_window(freq: float, cfg: SsvepConfig, seed: int) -> np.ndarray:
    rng = np.random.default_rng(seed)
    t = np.arange(cfg.window_samples) / cfg.sample_rate_hz
    channels = []
    for _ in range(8):
        phase = rng.uniform(0, 2 * np.pi)
        sig = 12.0 * np.sin(2 * np.pi * freq * t + phase)
        sig += 4.0 * np.sin(2 * np.pi * 2 * freq * t + phase / 2)
        sig += rng.normal(0, 5.0, size=t.shape)
        channels.append(sig)
    return np.stack(channels)


def calibration_profile(decoder: SsvepDecoder, cfg: SsvepConfig):
    trials = []
    for i in range(8):
        trials.append((AuraTarget.SIGHT, synthetic_window(cfg.blue_frequency_hz, cfg, 10 + i)))
        trials.append((AuraTarget.GUARD, synthetic_window(cfg.green_frequency_hz, cfg, 30 + i)))
    return calibrate_decoder(decoder, trials, model_id="test-session")


def test_decoder_distinguishes_dual_aura_targets():
    cfg = SsvepConfig(window_seconds=1.5)
    decoder = SsvepDecoder(cfg)
    blue = decoder.decide(synthetic_window(cfg.blue_frequency_hz, cfg, 1))
    green = decoder.decide(synthetic_window(cfg.green_frequency_hz, cfg, 2))
    assert blue.target == AuraTarget.SIGHT and blue.accepted
    assert green.target == AuraTarget.GUARD and green.accepted


def test_obvious_artifact_abstains():
    cfg = SsvepConfig(window_seconds=1.5)
    decoder = SsvepDecoder(cfg)
    x = synthetic_window(cfg.blue_frequency_hz, cfg, 3)
    x[:, 20] = 1000.0
    decision = decoder.decide(x)
    assert not decision.accepted
    assert decision.quality.artifact


def test_controller_emg_loses_authority():
    cfg = SsvepConfig(window_seconds=1.5)
    decoder = SsvepDecoder(cfg)
    x = synthetic_window(cfg.blue_frequency_hz, cfg, 4)
    t = np.arange(cfg.window_samples) / cfg.sample_rate_hz
    emg = 20.0 * np.sin(2 * np.pi * 43.0 * t) + 14.0 * np.sin(2 * np.pi * 61.0 * t + 0.3)
    x[[1, 2, 3], :] += emg
    decision = decoder.decide(x)
    assert not decision.accepted
    assert decision.quality.reason == "EMG_SUSPECTED"


def test_common_mode_blink_loses_authority():
    cfg = SsvepConfig(window_seconds=1.5)
    decoder = SsvepDecoder(cfg)
    x = synthetic_window(cfg.green_frequency_hz, cfg, 5)
    phase = np.linspace(0, np.pi, 80)
    pulse = 90.0 * np.sin(phase) ** 2
    x[:, 90:170] += np.asarray([1.0, .55, .65, .55, .45, .35, .30, .35])[:, None] * pulse
    decision = decoder.decide(x)
    assert not decision.accepted
    assert decision.quality.reason in {"COMMON_MODE_TRANSIENT", "FAST_TRANSIENT"}


def test_calibration_and_dwell_emit_stable_selection_with_evidence():
    cfg = SsvepConfig(window_seconds=1.5, dwell_windows=2)
    decoder = SsvepDecoder(cfg)
    profile = calibration_profile(decoder, cfg)
    assert profile.training_accuracy > 0.9

    runtime = AuraSelectionRuntime(decoder, profile, source_mode="simulation")
    first = runtime.process(synthetic_window(cfg.blue_frequency_hz, cfg, 90), now=0.0)
    second = runtime.process(synthetic_window(cfg.blue_frequency_hz, cfg, 91), now=1.5)
    assert first.event.value == "ABSTAIN"
    assert first.has_evidence
    assert first.sight_score > first.guard_score
    assert first.source_mode == "simulation"
    assert second.event.value == "AURA_SELECTED"
    assert second.target == AuraTarget.SIGHT
    assert second.margin > 0


def test_sliding_window_buffer_emits_with_hop():
    from mindforge_neuro.acquisition import SlidingWindowBuffer

    buf = SlidingWindowBuffer(channels=2, window_samples=5, hop_samples=2)
    x = np.arange(16, dtype=float).reshape(2, 8)
    ts = np.arange(8, dtype=float) / 250.0
    outputs = buf.push(x, ts)
    assert len(outputs) == 2
    assert outputs[0][0].shape == (2, 5)
    assert outputs[-1][1][-1] == ts[-2]
