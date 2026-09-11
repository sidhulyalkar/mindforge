# Mindforge V0.34 Evidence Bundle

V0.34 produces evidence in multiple processes on purpose. Unity owns the participant-facing tutorial and derived gaze correlation. Python owns EEG acquisition, calibration and decoder decisions. Promotion should therefore require those artifacts to agree on the same causal session rather than trusting any one file in isolation.

## Artifacts

A complete synthetic or live V0.34 run can produce:

```text
Unity tutorial
  -> v34-tutorial-<session>.json

Python calibrated decoder
  -> calibration-<calibration_id>.json

Unity passive gaze/BCI correlator
  -> v34-gaze-bci-<session>.jsonl
```

The first two artifacts are required for a complete BCI onboarding claim. Aggregate gaze/BCI evidence is optional for a basic neural run and required when claiming the gaze-observed V0.34 path.

## Cross-process authority keys

The bundle validator joins evidence using:

```text
source_commit
session_id
calibration_id
stimulus_layout_id
neural_source_mode
```

The tutorial receipt and calibration report must describe the same game session, calibration and frozen presentation geometry. A complete tutorial must also report the same neural source mode as the calibration service that authorized it.

The gaze/BCI JSONL is checked record-by-record against the same session, calibration and layout identities. It remains aggregate evidence only. Raw gaze coordinates are rejected by the validator.

## Validation

Validate the complete bundle with:

```bash
python tools/validate_v34_session_bundle.py \
  "/path/to/v34-tutorial-<session>.json" \
  "experiments/reports/calibration-<calibration_id>.json" \
  --gaze-evidence "/path/to/v34-gaze-bci-<session>.jsonl" \
  --expected-commit "$(git rev-parse HEAD)" \
  --require-clean \
  --require-gaze-evidence \
  --output experiments/reports/v34-session-bundle.json
```

For a BCI-only rehearsal where gaze was intentionally not observed, omit both `--gaze-evidence` and `--require-gaze-evidence`.

A passing output uses schema:

```text
mindforge.v34_session_bundle_validation.v1
```

and contains SHA-256 identities for the tutorial receipt, calibration report and optional gaze evidence file. This makes the compact validation receipt a manifest for the exact artifacts that were checked rather than a free-floating success flag.

## Fail-closed conditions

The bundle does not pass if any of the following occurs:

- the tutorial receipt itself fails provenance, layout, practice or scientific-boundary validation;
- the calibration report is not the V0.34 repeated-block held-out protocol;
- held-out calibration evidence is insufficient or below the configured promotion gates;
- promotion metrics do not equal the held-out metrics;
- session, calibration, layout or neural source identities disagree across processes;
- gaze evidence is required but missing or lacks at least two causal neural-window records;
- aggregate gaze evidence contains raw-coordinate-like fields;
- either underlying validator detects raw-EEG-shaped evidence leakage.

## Scientific boundary

A passing bundle proves internal consistency of the software evidence that was supplied. It does **not** by itself prove:

- that synthetic gaze came from an eye tracker;
- that synthetic EEG came from a participant;
- that live EEG acquisition units/electrode metadata were valid;
- that 10 Hz and 12 Hz were physically emitted by the display;
- that human performance generalizes beyond the measured session.

Those claims remain on later promotion gates. In particular, physical SSVEP presentation timing remains unobserved until an external measurement such as a photodiode or suitable high-speed camera is bound to the tested display and gameplay load.
