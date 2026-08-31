# Mindforge V0.17 release qualification

This document is the canonical promotion procedure for the current Mindforge directed-demo package.

Its purpose is deliberately conservative: **a successful test may only support the layer it actually observed.** A green Python workflow is not a Unity runtime result. A synthetic EEG success is not human EEG evidence. A healthy software refresh contract is not photodiode timing.

The release question is therefore not “is Mindforge green?” It is:

> **What is the highest contiguous gate, on this exact Git commit, for which evidence has actually been observed?**

## Candidate identity

Always capture the exact revision before beginning:

```bash
git status --short
git rev-parse HEAD
```

The working tree should be clean for canonical promotion evidence. Every evidence artifact used by the promotion manifest must name the same Git commit.

For V0.17.1, the camera-obstruction hardening in PR #39 must be included before treating the directed camera as the candidate implementation. The patch removes a framing rule that could otherwise push the camera through a wall or column when the obstruction is closer than the old preferred minimum distance.

## Gate summary

| Gate | Claim | Required observation |
|---|---|---|
| P0 | software contracts | exact-commit pytest / tooling / browser-module CI |
| P1 | Unity package assembles | clean-checkout pinned-Unity import, compile, scene assembly and validator |
| P2 | game is playable without BCI | explicit controller-only full encounter to VICTORY or DEFEAT |
| P3 | derived neural authority reaches the game | `simulated_decision` through the real Awakening/UDP/Unity authority path |
| P4 | authority is reproducible | conventional + neural replay reproduces semantic GameMarker consequences |
| P5 | production decoder survives synthetic EEG | neurOS synthetic EEG through acquisition/quality/FBCCA/dwell into Unity |
| P6 | loop fails safely | forced render/network/source faults abstain or degrade without inventing authority |
| P7 | display is physically qualified | measured luminance timing on the actual display path |
| P8 | real acquisition is identified | Unicorn metadata, channel mapping, units and sample cadence observed |
| P9 | stationary human discrimination | participant Sight vs Guard while stationary |
| P10 | visual selection tolerates moving scene content | participant selection with moving visual context |
| P11 | BCI remains usable while player moves | participant selection while conventional player locomotion occurs |
| P12 | BCI remains usable in light combat | participant-derived authority during low-pressure combat |
| P13 | full closed-loop experience | complete Fractured Signal encounter with human EEG |

Promotion is monotonic. If P3 is unobserved, a later-looking P4 artifact does not make the candidate P4-qualified.

---

## P0: exact-commit software qualification

GitHub Actions now emits a SHA-bound software artifact containing:

- `pytest.xml`
- `software-gate.json`
- `promotion-manifest.json`
- `content-foundry-plan.json`

The cloud workflow intentionally enforces only P0. A healthy manifest should show:

```text
P0     PASS
P1-P13 UNOBSERVED
```

To reproduce P0 locally:

```bash
mkdir -p experiments/reports
pytest --junitxml=experiments/reports/pytest.xml
python tools/mindforge_qualify.py software \
  --junit experiments/reports/pytest.xml \
  --commit "$(git rev-parse HEAD)" \
  --output experiments/reports/software-gate.json \
  --enforce
```

P0 is necessary, never sufficient.

---

## P1: clean-checkout Unity qualification

Use the Unity editor version pinned in `unity/ProjectSettings/ProjectVersion.txt`.

From a clean checkout of the candidate:

```bash
python tools/run_unity_gate.py --commit "$(git rev-parse HEAD)"
```

If Unity is not in a standard location:

```bash
python tools/run_unity_gate.py \
  --unity "/absolute/path/to/Unity" \
  --commit "$(git rev-parse HEAD)"
```

The runner must observe all of the following rather than infer them from source:

1. Unity launches the pinned editor version.
2. the project imports and compiles;
3. `CompetitionBatchRunner.AssembleAndValidate` executes;
4. the generated competition scene passes `CompetitionGateValidator`;
5. the gate report echoes the exact requested Git SHA;
6. the editor exits successfully.

Canonical wrapper evidence:

```text
experiments/reports/unity-gate1-run.json
```

If the Unity process never ran, P1 is UNOBSERVED rather than PASS.

---

## Directed-demo Unity runtime inspection

Before P2, launch the actual current demo through:

```text
Mindforge → Latest → PLAY LATEST (BCI Simulation)
```

Then run:

```text
Mindforge → Latest → Validate Latest Readiness
```

The readiness report may be `PASS`, `FAIL`, or `INCOMPLETE`.

`DEFERRED` checks never count as passes. In edit mode the report is expected to remain `INCOMPLETE` because runtime presentation was not observed. A Play Mode PASS still includes:

```text
physical_ssvep_qualified = false
```

because physical display timing and real EEG are later gates.

### Mandatory V0.17.1 camera regression

Exercise the directed camera around real architecture, not an empty room:

1. back the Guardian toward a wall while orbiting;
2. repeat beside a column or buttress;
3. traverse narrow arch/door transitions;
4. target-lock the boss near a wall and circle laterally;
5. force the desired camera position to be closer to an obstruction than the former 2.65 m framing floor;
6. verify the camera moves **in front of** the obstruction instead of crossing it;
7. verify there is no rapid pop, oscillation, stuck-inside-wall state, or loss of Guardian visibility;
8. verify gameplay FOV remains fixed at 56°;
9. verify calibration/resonance windows still stabilize user orbit rather than altering the coded VEP geometry.

Failure of this spot-check blocks the V0.17.1 camera patch regardless of P0.

### Current gameplay/readability smoke test

Also verify:

- exactly one ordinary contextual `E` prompt is visible;
- Memory Forge wins contextual priority over a nearby bike;
- `T` and mouse-wheel control target selection while arrow keys only orbit the camera;
- `Tab` presents the correct current objective/persistent state;
- Guardian movement, double jump, hover, air dash, dodge and sword remain responsive;
- target-lock, threat telegraphs and projectiles remain readable against the production world;
- Guardian, Echo and boss presentation do not change hit/collision authority;
- no visible production replacement creates ghost collision or reachable void;
- the neural coded cores remain visually distinct from ordinary environmental cyan/green accents;
- the compact HUD does not duplicate old engineering/debug HUDs.

---

## P2: controller-only full encounter

Start the passive evidence recorder **before** entering controller-only qualification:

```bash
python tools/mindforge_playtest.py --require-terminal --prompt-review
```

Then in the Unity Editor:

1. enter Play Mode;
2. press `F8` to request explicit controller-only qualification;
3. play until authoritative `VICTORY` or `DEFEAT`.

The capture locks to the first Unity `session_id` and must observe:

```text
QUALIFICATION_MODE / CONTROLLER_ONLY_NO_BCI
```

A valid P2 machine bundle contains:

```text
experiments/playtests/<UTC>/markers.jsonl
experiments/playtests/<UTC>/encounter.json
experiments/playtests/<UTC>/capture.json
```

If `--prompt-review` is used, subjective feedback is written separately as `review.json`. Human ratings never auto-pass P2.

P2 requires:

- exact candidate Git commit in `capture.json`;
- explicit controller-only declaration;
- non-empty semantic marker stream;
- terminal `VICTORY` or `DEFEAT`.

A timeout, interrupted session, or missing controller-only declaration is preserved as evidence but does not pass P2.

---

## P3: simulated neural authority

P3 checks the **authority architecture**, not EEG decoding.

Run the real Awakening handshake with a deterministic simulated-decision source:

```bash
python tools/mindforge_dev.py decision --calibrate \
  --script sight:3,abstain:1,guard:3,lost:1,recovered:1,sight:3,guard:3 \
  --hz 4 \
  --output-tape experiments/tapes/p3-neural.jsonl
```

Observe Unity simultaneously on the passive marker lane:

```bash
python tools/mindforge_dev.py marker-log \
  --output experiments/markers/p3-unity.jsonl
```

P3 acceptance should demonstrate:

- provenance is `simulated_decision`, never `live` or synthetic EEG;
- calibration/handshake becomes ready through the real authority path;
- Sight and Guard selections can produce their intended derived game consequences;
- ABSTAIN spends no aura and invents no selection;
- source loss closes neural authority;
- recovery requires the normal liveness path rather than silently reopening stale authority;
- conventional movement/combat remains owned by player input.

P3 must never be described as EEG performance.

---

## P4: deterministic replay

Record conventional input with the development-player tape path and save the P3 neural tape.

Replay neural authority through the production UDP boundary:

```bash
python tools/mindforge_dev.py replay experiments/tapes/p3-neural.jsonl --speed 1.0
```

Capture reference and replay semantic marker streams, then compare:

```bash
python tools/mindforge_qualify.py compare-markers \
  experiments/markers/reference.jsonl \
  experiments/markers/replay.jsonl \
  --commit "$(git rev-parse HEAD)" \
  --output experiments/reports/replay-comparison.json \
  --enforce
```

Transport sequence numbers, session IDs and wall-clock timestamps may differ. Semantic game consequences must match exactly. Similarity is diagnostic only.

---

## P5: neurOS synthetic EEG

P5 substitutes participant/acquisition reality while keeping the production Mindforge decoder and Unity authority boundary intact:

```text
neurOS synthetic participant / Unicorn-like sensor
        ↓
LSL acquisition
        ↓
Mindforge quality authority
        ↓
FBCCA + dwell / refractory policy
        ↓
NeuralEvent v2 (source_mode=synthetic_eeg)
        ↓
Unity
        ↓
GameMarker consequence
```

Acceptance requires preserving provenance at every layer. A successful synthetic run may support decoder/fault architecture; it is not human physiological evidence.

At minimum record:

- synthetic world/configuration seed;
- sample rate/channel montage/units presented to the decoder;
- quality flags and abstentions;
- emitted NeuralEvent provenance;
- Unity session/calibration IDs;
- semantic GameMarker consequences.

---

## P6: render/network/source fault rehearsal

Use deterministic Phantom controls and transport faults rather than manually unplugging random components with no record.

Examples:

```bash
python tools/phantom_control.py gain:0.65 silence:2.5 0 j --delay 1.0
```

The exact command sequence must be recorded alongside the session evidence.

Acceptance principle:

> **failure may remove neural authority; failure may never fabricate it.**

Exercise at least:

- source silence;
- loss and recovery;
- poor/ambiguous classifier evidence;
- degraded channels or gain;
- network delay/dropout where available;
- render timing unhealthy while a coded window would otherwise be armed.

Expected behavior includes ABSTAIN/degradation, bounded expiry, no stale selection resurrection, and conventional combat remaining available.

---

## P7: physical display timing

Software cadence is not P7.

Measure the actual luminance output of the release display path, preferably with a photodiode or equivalent independent sensor, while the final game camera/post-processing/render pipeline is active.

The artifact must identify:

- candidate Git commit;
- machine/GPU/display identity;
- display mode and measured refresh cadence;
- requested Sight/Guard nominal frequencies;
- measured waveform/cadence results;
- dropped/irregular presentation evidence;
- analysis script/version;
- pass/fail criteria.

Do not infer P7 from `DisplayTimingMonitor` alone. That monitor is a software health guard, not an external luminance measurement.

---

## P8: real Unicorn acquisition

Before interpreting any human signal, independently establish the acquisition facts on the actual hardware path:

- device identity/firmware if available;
- channel names/order;
- reference/ground configuration;
- sample rate;
- sample units/scaling;
- timestamp behavior;
- dropped/duplicate samples;
- channel quality behavior;
- exact mapping into the decoder montage.

A visually plausible EEG trace is not sufficient evidence for correct units or channel identity.

---

## P9-P13: staged human progression

Do not jump directly from “stationary selection worked once” to a boss demo.

### P9 stationary Sight vs Guard

Participant stationary, low visual clutter, repeated randomized/labelled target trials. Establish that the participant/device/display combination produces usable evidence above abstention and chance expectations under the chosen acceptance contract.

### P10 moving selection

Introduce controlled motion in the visual scene while the participant remains physically still. Verify that game rendering/background motion does not destroy target discrimination or produce unacceptable false selections.

### P11 selection while player moves

Allow conventional locomotion while the neural window is requested. The V0.17 motion-qualification layer should fail closed during disallowed high-motion states rather than interpreting contaminated evidence.

### P12 light combat

Introduce readable low-pressure attacks. Validate that attending to Sight/Guard does not systematically hide lethal information and that abstention remains fair.

### P13 full Fractured Signal encounter

Only after P0-P12 are contiguous should a full human-EEG encounter be treated as promotion evidence.

Record at minimum:

- participant/session pseudonymous ID;
- exact game/decoder/calibration revision;
- acquisition metadata;
- display-timing evidence reference;
- neural decisions and abstentions;
- relevant quality metrics;
- Unity semantic consequences;
- encounter outcome;
- conventional controller/input tape where enabled;
- safety/operator notes.

Raw EEG should remain outside the Unity boundary.

---

## Assemble the promotion manifest

After P0-P2 artifacts exist for the same exact candidate commit:

```bash
python tools/mindforge_qualify.py manifest \
  --commit "$(git rev-parse HEAD)" \
  --software experiments/reports/software-gate.json \
  --unity experiments/reports/unity-gate1-run.json \
  --controller experiments/playtests/<UTC>/capture.json \
  --require-through P2 \
  --output experiments/reports/promotion-manifest.json
```

Evidence from another commit becomes `STALE`. Evidence with no Git identity becomes `UNBOUND`. Missing evidence remains `UNOBSERVED`.

P3 and P5-P13 currently remain explicit human/operator/lab gates until dedicated machine-readable evidence adapters exist. Do not manually edit the promotion manifest to make them green.

## Release decision

A candidate is suitable for the next gate only when all lower gates are contiguous passes.

For a controller-only public/gameplay build, P0-P2 plus the directed-demo runtime checklist are the minimum credible software/game package claim.

For a BCI demonstration, use the highest actually observed neural gate in the language describing the demo. For example:

- `P3-qualified simulated neural authority` is not EEG;
- `P5-qualified synthetic EEG closed loop` is not human EEG;
- `P9-qualified stationary human SSVEP` is not full-combat BCI;
- only P13 supports the full human closed-loop encounter claim.

The point of the ladder is not bureaucracy. It is to make every impressive Mindforge demo answer one question cleanly:

> **What, exactly, did we observe?**
