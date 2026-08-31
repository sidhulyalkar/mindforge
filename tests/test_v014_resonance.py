from __future__ import annotations

import numpy as np

from mindforge_neuro import AuraTarget, SsvepConfig, SsvepDecoder
from mindforge_neuro.calibration import calibrate_decoder, normalize_calibrated_scores
from mindforge_neuro.resonance import ResonanceEpochBuffer, ResonanceEpochRuntime


def synthetic(freq: float, cfg: SsvepConfig, seconds: float, seed: int, amplitude: float = 16.0) -> np.ndarray:
    rng = np.random.default_rng(seed)
    n = int(round(seconds * cfg.sample_rate_hz))
    t = np.arange(n) / cfg.sample_rate_hz
    channels = []
    for _ in range(8):
        phase = rng.uniform(0, 2 * np.pi)
        sig = amplitude * np.sin(2 * np.pi * freq * t + phase)
        sig += 0.35 * amplitude * np.sin(2 * np.pi * 2 * freq * t + 0.5 * phase)
        sig += rng.normal(0.0, 3.0, n)
        channels.append(sig)
    return np.stack(channels)


def profile(cfg: SsvepConfig, decoder: SsvepDecoder):
    trials = []
    for i in range(10):
        trials.append((AuraTarget.SIGHT, synthetic(cfg.blue_frequency_hz, cfg, cfg.window_seconds, 100 + i)))
        trials.append((AuraTarget.GUARD, synthetic(cfg.green_frequency_hz, cfg, cfg.window_seconds, 200 + i)))
    return calibrate_decoder(decoder, trials, model_id="v014-test")


def test_variable_fbcca_can_score_short_cumulative_windows():
    cfg = SsvepConfig(window_seconds=1.25)
    decoder = SsvepDecoder(cfg)
    sight = decoder.decide_window(synthetic(cfg.blue_frequency_hz, cfg, 0.75, 1), min_score=0.05, min_margin=0.01)
    guard = decoder.decide_window(synthetic(cfg.green_frequency_hz, cfg, 0.75, 2), min_score=0.05, min_margin=0.01)
    assert sight.target == AuraTarget.SIGHT
    assert guard.target == AuraTarget.GUARD


def test_calibration_learns_target_specific_unattended_leakage():
    cfg = SsvepConfig(window_seconds=1.25)
    decoder = SsvepDecoder(cfg)
    p = profile(cfg, decoder)
    assert p.normalization_ready
    assert p.sight_off_scale >= 0.02
    assert p.guard_off_scale >= 0.02
    z = normalize_calibrated_scores(p, {AuraTarget.SIGHT: p.sight_off_center, AuraTarget.GUARD: p.guard_off_center})
    assert abs(z[AuraTarget.SIGHT]) < 1e-9
    assert abs(z[AuraTarget.GUARD]) < 1e-9


def test_epoch_buffer_discards_marker_to_photon_guard_samples():
    cfg = SsvepConfig()
    buffer = ResonanceEpochBuffer(8, cfg.sample_rate_hz, 1.25, onset_guard_seconds=0.025)
    buffer.begin(4)
    guard_samples = buffer.onset_guard_samples
    assert guard_samples >= 6
    x = np.ones((8, guard_samples + 10), dtype=float)
    ts = np.arange(x.shape[1]) / cfg.sample_rate_hz
    buffer.push(x, ts)
    assert buffer.count == 10


def test_resonance_runtime_emits_only_for_current_epoch_and_reports_post_onset_duration():
    cfg = SsvepConfig(window_seconds=1.25)
    decoder = SsvepDecoder(cfg)
    p = profile(cfg, decoder)
    runtime = ResonanceEpochRuntime(decoder, p, source_mode="synthetic_eeg", initial_seq=40)
    runtime.begin_epoch(17, session_id="game")

    eeg = synthetic(cfg.blue_frequency_hz, cfg, 1.35, 901, amplitude=24.0)
    ts = np.arange(eeg.shape[1]) / cfg.sample_rate_hz
    event = None
    for start in range(0, eeg.shape[1], 25):
        candidate = runtime.push(eeg[:, start:start + 25], ts[start:start + 25])
        if candidate is not None:
            event = candidate
            break
    assert event is not None
    assert event.event.value == "AURA_SELECTED"
    assert event.target == AuraTarget.SIGHT
    assert event.stimulus_epoch == 17
    assert 550 <= event.evidence_ms <= 1250
    assert not runtime.active


def test_resonance_runtime_fails_closed_at_final_checkpoint():
    cfg = SsvepConfig(window_seconds=1.25)
    decoder = SsvepDecoder(cfg)
    p = profile(cfg, decoder)
    runtime = ResonanceEpochRuntime(decoder, p, source_mode="synthetic_eeg")
    runtime.begin_epoch(99)
    n = int(round(1.35 * cfg.sample_rate_hz))
    flat = np.zeros((8, n), dtype=float)
    ts = np.arange(n) / cfg.sample_rate_hz
    event = runtime.push(flat, ts)
    assert event is not None
    assert event.event.value == "ABSTAIN"
    assert event.target is None
    assert event.stimulus_epoch == 99
    assert event.evidence_ms == 1250
