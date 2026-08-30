from __future__ import annotations

import numpy as np

from mindforge_neuro.calibration import personalized_ssvep_config, rank_participant_frequency_pairs
from mindforge_neuro.config import SsvepConfig
from mindforge_neuro.events import EventType, NeuralEvent, SourceMode


def _window(
    nominal_hz: float,
    *,
    seed: int,
    actual_hz: float | None = None,
    amplitude_uv: float = 6.0,
) -> np.ndarray:
    config = SsvepConfig()
    rng = np.random.default_rng(seed)
    t = np.arange(config.window_samples, dtype=float) / config.sample_rate_hz
    eeg = rng.normal(0.0, 0.22, size=(8, config.window_samples))
    frequency = nominal_hz if actual_hz is None else actual_hz
    # Posterior channels carry phase-diverse visual response so the median common-mode
    # artifact detector does not mistake a synthetic test target for a global transient.
    for offset, channel in enumerate((4, 5, 6, 7)):
        phase = 0.37 * offset + 0.05 * seed
        eeg[channel] += amplitude_uv * np.sin(2.0 * np.pi * frequency * t + phase)
        eeg[channel] += 1.1 * np.sin(2.0 * np.pi * frequency * 2.0 * t + phase * 0.7)
    # Keep non-posterior channels physiologically non-flat without adding target evidence.
    for channel in (0, 1, 2, 3):
        eeg[channel] += 0.45 * np.sin(2.0 * np.pi * 9.2 * t + channel * 0.8)
    return eeg


def test_frequency_pair_ranking_prefers_repeatable_participant_response():
    trials = []
    # 8 Hz is intentionally a poor candidate for this synthetic participant: the display
    # label says 8 but the captured response is unrelated 17 Hz activity.
    for i in range(5):
        trials.append((8.0, _window(8.0, seed=10 + i, actual_hz=17.0, amplitude_uv=2.5)))
        trials.append((10.0, _window(10.0, seed=30 + i)))
        trials.append((12.0, _window(12.0, seed=50 + i)))

    profile = rank_participant_frequency_pairs(trials, minimum_trials_per_frequency=3)
    assert profile.selected_sight_hz == 10.0
    assert profile.selected_guard_hz == 12.0
    assert profile.best.balanced_accuracy >= 0.9
    assert profile.best.usable_trials == 10
    assert profile.candidate_frequencies_hz == (8.0, 10.0, 12.0)
    assert all(
        profile.evaluations[i].objective >= profile.evaluations[i + 1].objective
        for i in range(len(profile.evaluations) - 1)
    )

    personalized = personalized_ssvep_config(profile)
    assert personalized.blue_frequency_hz == 10.0
    assert personalized.green_frequency_hz == 12.0


def test_frequency_ranking_requires_clean_repeated_evidence_for_each_candidate():
    trials = [(10.0, _window(10.0, seed=1)), (12.0, _window(12.0, seed=2))]
    try:
        rank_participant_frequency_pairs(trials, minimum_trials_per_frequency=3)
    except ValueError as exc:
        assert "enough clean trials" in str(exc)
    else:
        raise AssertionError("one trial per candidate must not qualify participant frequency selection")


def test_calibration_frequency_metadata_round_trips_as_derived_scalars_only():
    candidate = NeuralEvent.create(
        seq=11,
        event=EventType.CALIBRATION_CANDIDATE_SCORE,
        target=None,
        confidence=0.83,
        quality=0.91,
        model_id="participant-frequency-ranking-v1",
        source_mode=SourceMode.LIVE.value,
        session_id="session-v08",
        calibration_id="cal-v08",
        stimulus_hz=10.0,
        candidate_rank=1,
        authority_ttl_ms=0,
    )
    restored = NeuralEvent.from_json(candidate.to_json())
    assert restored.event == EventType.CALIBRATION_CANDIDATE_SCORE
    assert restored.stimulus_hz == 10.0
    assert restored.candidate_rank == 1
    assert restored.has_evidence is False

    ready = NeuralEvent.create(
        seq=12,
        event=EventType.CALIBRATION_READY,
        target=None,
        confidence=0.88,
        quality=0.93,
        model_id="participant-frequency-ranking-v1",
        source_mode=SourceMode.LIVE.value,
        session_id="session-v08",
        calibration_id="cal-v08",
        selected_sight_hz=10.0,
        selected_guard_hz=12.0,
        authority_ttl_ms=0,
    )
    payload = ready.to_dict()
    assert payload["selected_sight_hz"] == 10.0
    assert payload["selected_guard_hz"] == 12.0
    assert "eeg" not in payload
    assert "samples" not in payload
    assert "channels" not in payload
