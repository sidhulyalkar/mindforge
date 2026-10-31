"""Offline, leakage-controlled EEG electrode-group selection for experimental tasks.

Scalp electrode groups are *sensor locations*, not proven cortical sources.
Selection never grants live BCI authority and never changes SSVEP defaults.
"""
from __future__ import annotations

from dataclasses import asdict, dataclass
from typing import Sequence

import numpy as np


@dataclass(frozen=True)
class ElectrodeGroup:
    name: str
    electrodes: tuple[str, ...]


# Broad, exploratory sensor groupings. Not anatomical source localization.
DEFAULT_GROUPS = (
    ElectrodeGroup("central", ("C3", "CZ", "C4", "FC3", "FCZ", "FC4", "CP3", "CPZ", "CP4")),
    ElectrodeGroup("frontocentral", ("F3", "FZ", "F4", "FC3", "FCZ", "FC4", "C3", "CZ", "C4")),
    ElectrodeGroup("lateral_frontotemporal", ("F7", "FT7", "T7", "TP7", "F8", "FT8", "T8", "TP8")),
    ElectrodeGroup("posterior_control", ("P3", "PZ", "P4", "PO7", "PO8", "O1", "OZ", "O2")),
)

# Intentionally conservative band powers: a baseline, not imagined-speech decoding.
FEATURE_BANDS_HZ = ((4., 8.), (8., 13.), (13., 20.), (20., 30.))
ARTIFACT_PROXY_BAND_HZ = (35., 60.)


@dataclass(frozen=True)
class SpatialSelectionReport:
    schema: str
    task: str
    channel_names: tuple[str, ...]
    chosen_group: str
    chosen_electrodes: tuple[str, ...]
    candidate_validation_balanced_accuracy: dict[str, float]
    validation_balanced_accuracy: float
    holdout_balanced_accuracy: float
    chance_balanced_accuracy: float
    high_frequency_proxy_validation_accuracy: float | None
    high_frequency_proxy_holdout_accuracy: float | None
    train_sessions: tuple[str, ...]
    validation_session: str
    holdout_session: str
    class_labels: tuple[str, ...]
    warnings: tuple[str, ...]
    eligible_for_live_pilot: bool

    def to_dict(self) -> dict:
        """Derived metrics and electrode names only; no sample-level EEG leaves this report."""
        return asdict(self)


def _balanced_accuracy(truth: np.ndarray, predicted: np.ndarray, classes: np.ndarray) -> float:
    return float(np.mean([np.mean(predicted[truth == c] == c) for c in classes]))


def _predict_prototype(x_train: np.ndarray, y_train: np.ndarray,
                       x_test: np.ndarray, classes: np.ndarray) -> np.ndarray:
    """Diagonal-standardized nearest-class-centroid baseline; train-only statistics."""
    mean = np.mean(x_train, axis=0)
    scale = np.std(x_train, axis=0)
    scale = np.where(scale < 1e-6, 1., scale)
    train_z = np.clip((x_train - mean) / scale, -20., 20.)
    test_z = np.clip((x_test - mean) / scale, -20., 20.)
    centers = np.stack([train_z[y_train == c].mean(axis=0) for c in classes])
    squared_distances = ((test_z[:, None, :] - centers[None, :, :]) ** 2).mean(axis=2)
    return classes[np.argmin(squared_distances, axis=1)]


def _band_features(epochs: np.ndarray, fs: float, bands: Sequence[tuple[float, float]]) -> np.ndarray:
    """Per-epoch/ch/temporal-half log band powers, shape (trials, channels, features)."""
    n = epochs.shape[-1]
    parts = []
    for segment in (epochs[..., : n // 2], epochs[..., n // 2 :]):
        length = segment.shape[-1]
        tapered = (segment - segment.mean(axis=-1, keepdims=True)) * np.hanning(length)
        spectrum = np.abs(np.fft.rfft(tapered, axis=-1)) ** 2
        freqs = np.fft.rfftfreq(length, 1. / fs)
        for low, high in bands:
            mask = (freqs >= low) & (freqs < high)
            if not mask.any():
                raise ValueError(f"too few samples to resolve frequency band {low}-{high} Hz")
            parts.append(np.log1p(spectrum[..., mask].mean(axis=-1)))
    return np.stack(parts, axis=-1)


def evaluate_electrode_groups(
    epochs_uv: np.ndarray,
    labels: Sequence[str],
    session_ids: Sequence[str],
    channel_names: Sequence[str],
    sample_rate_hz: float,
    *,
    task: str = "imagined_speech",
    groups: Sequence[ElectrodeGroup] = DEFAULT_GROUPS,
    min_gain_over_chance: float = 0.15,
    high_frequency_proxy_gate: float = 0.70,
) -> SpatialSelectionReport:
    """Select on a *validation session*; evaluate once on an untouched holdout session.

    Input epochs: trials x EEG channels x samples, in microvolts. Provide distinct,
    independently acquired session IDs (not random trial groups). A third session is
    mandatory. Promotability is only a screen for further supervised human pilots.
    Even a good result does not prove cortical source, silent speech, or gameplay safety.
    """
    eeg = np.asarray(epochs_uv, dtype=np.float64)
    y = np.asarray(labels, dtype=str)
    s = np.asarray(session_ids, dtype=str)
    names = tuple(str(x).strip().upper() for x in channel_names)
    if eeg.ndim != 3 or len(eeg) < 12 or eeg.shape[1] != len(names) or eeg.shape[2] < 128:
        raise ValueError("expected at least 12 epochs with shape (epochs, channels, >=128 samples)")
    if y.shape != (len(eeg),) or s.shape != (len(eeg),):
        raise ValueError("labels and session_ids must have one value per epoch")
    if not (np.isfinite(eeg).all() and np.isfinite(sample_rate_hz) and sample_rate_hz >= 128):
        raise ValueError("EEG must be finite; sample rate must be >= 128 Hz")
    if len(names) != len(set(names)) or any(not name for name in names):
        raise ValueError("channel names must be unique and nonempty")
    if not (0. <= min_gain_over_chance <= 1. and 0. <= high_frequency_proxy_gate <= 1.):
        raise ValueError("invalid evaluation thresholds")
    if not task.strip():
        raise ValueError("task must be named")
    classes = np.unique(y)
    sessions = tuple(dict.fromkeys(s.tolist()))
    if len(classes) < 2 or len(sessions) < 3:
        raise ValueError(">=2 classes and >=3 independent sessions are required")
    if any(not name for name in classes) or any(not sid for sid in sessions):
        raise ValueError("class labels and session IDs must be nonempty")
    # The latest two sessions *as provided* are reserved for validation and final holdout.
    train_sessions, val_session, test_session = sessions[:-2], sessions[-2], sessions[-1]
    train = np.isin(s, train_sessions)
    val = s == val_session
    test = s == test_session
    for subset_name, mask in (("training", train), ("validation", val), ("holdout", test)):
        # Classes should each have repeated evidence, not one isolated trial.
        if any(np.sum(y[mask] == c) < 3 for c in classes):
            raise ValueError(f"{subset_name} session set requires >=3 examples of each class")
    features = _band_features(eeg, sample_rate_hz, FEATURE_BANDS_HZ)
    candidates: list[tuple[str, tuple[int, ...]]] = []
    for group in (*groups, ElectrodeGroup("all_scalp", names)):
        indices = tuple(i for i, name in enumerate(names) if name in group.electrodes)
        if len(indices) >= 2 and group.name not in {name for name, _ in candidates}:
            candidates.append((group.name, indices))
    if not candidates:
        raise ValueError("no electrode group contains >=2 available channels")
    scores: dict[str, float] = {}
    for name, idx in candidates:
        x = features[:, idx, :].reshape(len(eeg), -1)
        prediction = _predict_prototype(x[train], y[train], x[val], classes)
        scores[name] = _balanced_accuracy(y[val], prediction, classes)
    # Prefer the smaller sensor group at equal accuracy; never inspect test to choose.
    chosen, indices = max(candidates, key=lambda pair: (scores[pair[0]], -len(pair[1])))
    selected = features[:, indices, :].reshape(len(eeg), -1)
    frozen_prediction = _predict_prototype(selected[train | val], y[train | val], selected[test], classes)
    holdout_score = _balanced_accuracy(y[test], frozen_prediction, classes)
    chance = 1. / len(classes)

    proxy_val = proxy_test = None
    warnings: list[str] = []
    nyquist = sample_rate_hz / 2.
    if nyquist > ARTIFACT_PROXY_BAND_HZ[1]:
        hf = _band_features(eeg, sample_rate_hz, (ARTIFACT_PROXY_BAND_HZ,))
        xhf = hf[:, indices, :].reshape(len(eeg), -1)
        proxy_val = _balanced_accuracy(y[val], _predict_prototype(xhf[train], y[train], xhf[val], classes), classes)
        proxy_test = _balanced_accuracy(y[test], _predict_prototype(xhf[train | val], y[train | val], xhf[test], classes), classes)
        if max(proxy_val, proxy_test) >= high_frequency_proxy_gate:
            warnings.append("HIGH_FREQUENCY_CLASS_PREDICTIVE: possible EMG/task confound; review independently")
    else:
        warnings.append("HIGH_FREQUENCY_PROXY_UNAVAILABLE: sample-rate Nyquist insufficient")
    if scores[chosen] < chance + min_gain_over_chance:
        warnings.append("VALIDATION_NOT_ABOVE_BASELINE")
    if holdout_score < chance + min_gain_over_chance:
        warnings.append("HOLDOUT_NOT_ABOVE_BASELINE")
    if task == "imagined_speech":
        warnings.append("SCALP_SENSOR_SELECTION_NOT_SPEECH_SOURCE_LOCALIZATION")
    eligible = (scores[chosen] >= chance + min_gain_over_chance and
                holdout_score >= chance + min_gain_over_chance and
                proxy_val is not None and proxy_test is not None and
                max(proxy_val, proxy_test) < high_frequency_proxy_gate)
    return SpatialSelectionReport(
        schema="mindforge.spatial_selection.v1", task=task, channel_names=names,
        chosen_group=chosen, chosen_electrodes=tuple(names[i] for i in indices),
        candidate_validation_balanced_accuracy=scores,
        validation_balanced_accuracy=scores[chosen], holdout_balanced_accuracy=holdout_score,
        chance_balanced_accuracy=chance,
        high_frequency_proxy_validation_accuracy=proxy_val,
        high_frequency_proxy_holdout_accuracy=proxy_test,
        train_sessions=tuple(train_sessions), validation_session=val_session,
        holdout_session=test_session, class_labels=tuple(classes.tolist()),
        warnings=tuple(warnings), eligible_for_live_pilot=bool(eligible),
    )
