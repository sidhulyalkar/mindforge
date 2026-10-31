from __future__ import annotations

import numpy as np
import pytest

from mindforge_neuro.adaptive_spatial import ElectrodeGroup, evaluate_electrode_groups


def fixture(*, flip_holdout: bool = False, add_emg: bool = False, noise_only: bool = False):
    rng = np.random.default_rng(81)
    fs, n, n_per_session = 250., 500, 56
    t = np.arange(n) / fs
    names = ("FZ", "C3", "CZ", "C4", "PZ", "PO7", "OZ", "PO8")
    eeg, labels, sessions = [], [], []
    for session in range(3):
        for i in range(n_per_session):
            target = i % 2
            data = rng.normal(0, 0.8, (len(names), n))
            if not noise_only:
                channel = 6 if (flip_holdout and session == 2) else 1
                data[channel] += (1. + 2.2 * target) * np.sin(2*np.pi*10*t + rng.uniform(-1, 1))
            if add_emg:
                data[1] += (1. + 2.2*target) * np.sin(2*np.pi*43*t)
            eeg.append(data)
            labels.append("wall" if target else "bridge")
            sessions.append(f"session-{session}")
    return np.stack(eeg), labels, sessions, names, fs


def test_selects_central_sensors_on_unseen_session_without_changing_ssvep():
    r = evaluate_electrode_groups(*fixture())
    assert r.schema == "mindforge.spatial_selection.v1"
    assert r.chosen_group == "central"
    assert "C3" in r.chosen_electrodes
    assert r.holdout_balanced_accuracy >= 0.85
    assert r.passes_offline_screen
    assert "SCALP_SENSOR_SELECTION_NOT_SPEECH_SOURCE_LOCALIZATION" in r.warnings
    assert "raw_eeg" not in r.to_dict()


def test_final_holdout_cannot_change_selected_group():
    normal = evaluate_electrode_groups(*fixture())
    flipped = evaluate_electrode_groups(*fixture(flip_holdout=True))
    assert normal.chosen_group == flipped.chosen_group
    assert normal.candidate_validation_balanced_accuracy == flipped.candidate_validation_balanced_accuracy
    assert flipped.holdout_balanced_accuracy < normal.holdout_balanced_accuracy


def test_high_frequency_class_confounds_prohibit_pilot_eligibility():
    r = evaluate_electrode_groups(*fixture(add_emg=True))
    assert r.holdout_balanced_accuracy > 0.7
    assert r.high_frequency_proxy_validation_accuracy > 0.7
    assert not r.passes_offline_screen
    assert any(w.startswith("HIGH_FREQUENCY_CLASS_PREDICTIVE") for w in r.warnings)


def test_random_noise_not_promoted():
    r = evaluate_electrode_groups(*fixture(noise_only=True))
    assert not r.passes_offline_screen


def test_independent_sessions_and_class_coverage_required():
    eeg, labels, sessions, channels, fs = fixture()
    with pytest.raises(ValueError, match="independent sessions"):
        evaluate_electrode_groups(eeg, labels, ["one"] * len(labels), channels, fs)
    with pytest.raises(ValueError, match="holdout"):
        evaluate_electrode_groups(
            eeg, labels[:-56] + ["wall"] * 56, sessions, channels, fs)


def test_unusable_montage_fails_closed():
    eeg, labels, sessions, channels, fs = fixture()
    with pytest.raises(ValueError, match="unique"):
        evaluate_electrode_groups(eeg, labels, sessions, ("C3",)*8, fs)
    with pytest.raises(ValueError, match="finite"):
        corrupted = eeg.copy()
        corrupted[1, 1, 3] = np.nan
        evaluate_electrode_groups(corrupted, labels, sessions, channels, fs)
