# Mindforge v0.24: session-bound neural authority and causal markers

Status: draft integration. Unity compilation and native gameplay execution have not been verified.

## What changed

Production UDP selection authority now requires exact correspondence between the NeuralEvent v2 session_id and the current Unity game-session ID. The private local-only Editor keyboard simulation path is no longer enabled by merely claiming its source_mode over UDP.

The existing player-armed resonance window, epoch sequence, minimum post-onset EEG duration, motion qualification and measured display timing remain in force.

This reduces incorrect software authority. It does not establish that a neural classification represents a participant's intent.

## Causal observation contract

DualAuraCombatDirector now emits new GameMarker events with a shared trial_id:

- NEURAL_SELECTION_OBSERVED: the game received a proposed selection.
- NEURAL_SELECTION_DENIED: the active-window guard or buff controller rejected it.
- NEURAL_SELECTION_ACCEPTED: the native TryApply function returned true.
- NEURAL_EFFECT_OBSERVED: the corresponding buff was observed active after application.
- NEURAL_EFFECT_UNVERIFIED: the buff was not observed active after application.

The trial identity is selection-v24:<Unity session ID>:<stimulus epoch>:<neural event sequence>. This is a correlation key, not a human-intent label or independent sensor.

These records are emitted through the existing read-only Unity GameMarker observer. The prior NEURAL_BUFF_APPLIED marker remains backward-compatible but lacks a correlation ID. The new audit counts it as unattributed rather than guessing which selection caused it.

## Run the Python gate

From a checked-out repository with Python dependencies installed:

    pip install -e '.[test]'
    pytest -q tests/test_native_evidence_v24.py
    python tools/audit_native_evidence_v24.py \
      --markers experiments/recordings/your-capture/game_markers.jsonl \
      --output experiments/reports/native-evidence-v24.json \
      --enforce-consistency

The consistency option rejects broken event chains. Use --strict-science to demand independently established physiological qualification: this intentionally exits with code 2 even for a clean synthetic or Unity-only capture.

## Required scientific and integration work

1. Compile this branch in the pinned Unity editor and run the new native NUnit tests, plus existing Unity smoke tests.
2. Record source-side emission counts and reconcile against the UDP observer, including dropped first/last packets.
3. Independently inspect actual gameplay effects rather than relying exclusively on the Unity component that applies the effect.
4. Collect consented pseudonymous EEG with independently randomized intention/no-intention trial labels, correct device clock provenance, eye tracking, and optical display verification.
5. Use participant/session-blocked held-out evaluation. Quantify false armings and false effect grants per active opportunity hour with uncertainty, separately for each no-command condition.
6. Use a non-authoritative shadow mode for initial Hollow Knight experiments. Do not activate frequency-tagged stimulation without an appropriate display and participant-safety protocol.

## Limits

- This is instrumentation of the public First Guardian Unity Sight/Guard path, not the separate Orbits Hollow Knight Aegis/Surge mod.
- A contiguous receiver recording is not proof of source completeness.
- The present buff-state check is within the same Unity process and is not independent outcome validation.
- The source-side GameMarker observer may drop packets; the audit reports gaps it sees but cannot infer a missing first or final packet.
- Human neural intent, gaze, acquisition validity, optical display timing and native game qualification are not established here.
- Do not merge the draft merely because Python unit tests pass.
