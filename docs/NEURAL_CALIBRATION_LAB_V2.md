# Neural Calibration Lab v2

A **standalone, offline, derived-only** research diagnostic for task-specific scalp electrode-group selection. This does not decode imagined speech or authorize Unity gameplay.

## Run on your Mac

From the Mindforge repository root, after checking out the v2 feature branch and creating an environment:

    python -m pip install -e '.[test]'
    python tools/mindforge_calibration_lab.py --demo --output experiments/reports/neural-calibration-demo
    python -m pytest -q tests/test_adaptive_spatial.py tests/test_calibration_lab.py

Open experiments/reports/neural-calibration-demo/index.html in a local browser. This demo deliberately plants class-correlated oscillations in C3/Cz. Its accuracy is artificial and provides **no evidence of imagined speech in a person**.

For an offline EEG archive:

    python tools/mindforge_calibration_lab.py --archive sessions.npz \
      --source-kind recorded_eeg --task imagined_speech \
      --output experiments/reports/imagined-speech-pilot

The NPZ must contain:
- eeg_uv: shape (trials, channels, samples), in microvolts
- labels: class name for every epoch
- session_ids: **independently acquired** recording-session identity for every epoch
- channel_names: verified electrode names
- sample_rate_hz: scalar sampling rate

Use **one participant and stable electrode montage** per analysis. There must be at least three independent sessions with repeated examples of each class. Sort sessions chronologically before writing the NPZ: the latest two unique session names by first occurrence are used for validation and untouched final holdout. Never repeatedly tune after seeing the holdout result. A final positive result is still only an initial classifier benchmark, not proof of semantic imagined speech.

## Outputs

- calibration.json: versioned mindforge.calibration_lab.v2 report, with selected electrode group, present/missing scalp sensors, candidate *validation* accuracies, held-out accuracy, per-class counts, spectral high-frequency warning and deliberately disabled live authority flags.
- index.html: standalone responsive, CSP-restricted, JS-free scalp map and evaluation report. No external assets or network requests.

**Privacy:** original recording session identifiers are redacted before serialization. Raw EEG epochs, original NPZ paths, per-trial predictions, and user recordings are not embedded in report files. Aggregated task labels and metrics may still be sensitive and should not be published without consent. Source label recorded_eeg is self-declared and never independently verified.

## Scientific limitations and follow-on

This implementation uses time-binned 4–30 Hz bandpower and a nearest-prototype baseline. It is deliberately simple and interpretably testable. Sensor groups are *scalp locations*, not cortical source estimates. The normal eight-channel Mindforge montage has **no dedicated lateral frontotemporal coverage**; the display explicitly marks missing sensors. Unknown electrodes are listed, never given invented coordinates.

The 35–60 Hz class-predictiveness control only flags some possible muscle confounds; it does not detect or rule out jaw/tongue/facial/neck activity, eye signals, electrode motion or prompt artifacts. Subsequent human studies should collect facial EMG, EOG/gaze and head motion with balanced idle/reading/mouthing/overt/imagined conditions and independent acquisition sessions. The offline-screen flag is *not* an EEG speech-decoding claim.

This PR deliberately leaves existing Mindforge NeuralEvent v2, UDP receiver, SSVEP coded visual stimuli, Sight/Guard abilities, and Unity combat unchanged. Next planned v3 adds a **separate non-authoritative feature stream** for an opt-in, bounded Living World visualizer.
