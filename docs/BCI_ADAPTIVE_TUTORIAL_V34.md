# Mindforge V0.34 Adaptive BCI Tutorial

## Purpose

V0.34 turns participant onboarding into a reproducible measurement protocol while
keeping the experience game-like. It is stacked on the V0.33 production neural seam
rather than replacing it.

The core modality grammar remains:

- **hands / keyboard:** frame-critical movement, camera, attacks and explicit actions;
- **gaze:** continuous spatial-attention evidence and tutorial/readability measurement;
- **BCI:** slower semantic selection between Sight and Guard.

Gaze is deliberately **not** an EEG-classifier feature in this tranche. That separation
lets us measure EEG-only performance first and later test multimodal fusion as a real
ablation instead of accidentally allowing gaze to perform the neural classification.

## System stack

```text
V0.31 Dragon Souls gameplay authority
            ↓
V0.33 production BCI spine
  Sight 10 Hz / Guard 12 Hz
  calibration identity
  causal stimulus_epoch windows
  quality/confidence/artifact gates
  semantic Sight/Guard receptors
            ↓
V0.34 adaptive onboarding
  derived gaze UDP 19746
  gaze interaction profile
  bounded presentation recommendation
  FROZEN stimulus layout
  tutorial stages + receipt
```

Raw EEG stays in the Python/acquisition process. Raw eye images, scene video, pupil
video and vendor eye-state payloads stay outside the Dragon Souls Unity project.

## Tutorial stages

The runtime tutorial starts with **F9** during development. The production playthrough
can later invoke the same state machine from the Awakening/Memory Forge chapter.

1. **Controls** — WASD movement, Arrow-key view and Space light attack. After a minimum
   practice period the player presses Enter.
2. **Gaze Health** — one central target establishes that mapped gaze exists and can hold
   a stable region. Failure does not block the game; V0.34 falls back to a default layout.
3. **Gaze Grid** — nine screen positions measure bias, dispersion and target occupancy.
4. **Free Explore** — short natural viewing interval with no demanded target.
5. **Gaze Switch** — repeated spatial switches measure acquisition behavior under changes
   in attended screen position.
6. **Layout Freeze** — gaze evidence is converted into a conservative presentation
   recommendation. The visual geometry becomes immutable before EEG calibration.
7. **Waiting For Neural Service** — ordinary gameplay remains safe while Unity waits for
   the Python decoder's service-ready fact.
8. **Calibration** — the unchanged V0.33 authority protocol performs baseline, Sight and
   Guard presentation; only a matching Python `CALIBRATION_READY` can declare success.
9. **Sight Practice** — production causal window opens until Sight is demonstrated or a
   bounded retry count is exhausted.
10. **Guard Practice** — same for Guard.
11. **Movement Stress** — the player holds WASD or an Arrow key during one neural window
    so degradation under ordinary game interaction becomes observable rather than assumed.
12. **Complete / Partial** — a machine-readable tutorial receipt is written. Partial mode
    is safe and playable but cannot be promoted as a successful BCI onboarding session.

## Gaze profile

`mindforge.gaze_profile.v1` describes interaction geometry, not cognition or personality.
The first profile contains:

- total and usable gaze sample counts;
- usable fraction;
- prompt count and stable-prompt count;
- mean screen-space bias across prompted targets;
- median and p90 within-prompt dispersion;
- median and p90 stable acquisition latency;
- median target occupancy;
- recommended gaze AOI radius;
- recommended pre-stimulus acquisition lead.

A target is considered stably acquired only when a recent local-time window contains a
minimum number of usable samples, enough samples lie inside the active AOI, the median
point is close to the target, and p90 dispersion stays below a bounded threshold.

The vendor `fixation` boolean is not used as tutorial authority. This makes mouse replay,
Neon surface gaze and future gaze providers comparable through one game-level stability
contract.

## Conservative adaptation

The first V0.34 adaptation intentionally does very little.

If gaze evidence is too sparse or unreliable, V0.34 freezes the V0.33 default layout.
If the profile is usable, V0.34 may increase the symmetric left/right target separation
within a narrow bound when measured dispersion is larger. It also stores a recommended
AOI radius and acquisition lead for gaze analytics.

V0.34 does **not**:

- change Sight/Guard frequencies;
- reorder semantic labels;
- move targets continuously during a calibrated session;
- feed gaze into the SSVEP decoder;
- grant gaze attack, movement, damage or target-lock authority;
- infer psychological traits from gaze behavior.

After layout freeze, `MindforgeBciStimulusV33.LayoutFrozen` prevents geometry changes
while calibration or listening is active.

## Calibration-layout identity

The existing `mindforge.game_marker.v1` already carries `trial_id`. V0.34 uses this field
to bind calibration stages, neural windows, tutorial stages and semantic selections to
the current frozen layout ID without introducing a new marker schema.

A valid causal record therefore has join keys roughly like:

```text
Unity session_id
  + calibration_id
  + stimulus layout / trial_id
  + stimulus_epoch
  + neural source_mode
```

This prevents a calibration recorded under one visual geometry from silently becoming
evidence for another geometry.

## Gaze transport

The production overlay now has a dedicated `MindforgeUdpGazeReceiverV34` on loopback UDP
19746. It mirrors the existing gaze-platform discipline:

- background socket thread;
- loopback-only bind;
- bounded packet queue;
- oldest-packet backpressure dropping;
- local `Stopwatch` receive-age gating;
- monotonic sequence filtering;
- newest-valid-sample publication;
- stale connection expiry.

The existing Python bridge remains the source:

```bash
python tools/mindforge_gaze.py mouse
python tools/mindforge_gaze.py point --x 0.5 --y 0.5
python tools/mindforge_gaze.py replay experiments/gaze/session.jsonl
python tools/mindforge_gaze.py neon-screen
```

## Tutorial receipt

A finished tutorial writes `mindforge.tutorial_receipt.v1` under
`Application.persistentDataPath/mindforge-bci` and includes:

- exact Mindforge source commit and clean/dirty provenance;
- Unity version and game session identity;
- complete/partial tutorial status;
- gaze source mode and derived gaze profile;
- frozen layout identity and whether the default layout was used;
- calibration identity/state;
- Sight and Guard practice results and attempt counts;
- abstentions/mismatches;
- movement-stress outcome;
- latest accepted neural source mode;
- V0.33 session-log path;
- explicit `raw_eeg_in_unity=false`;
- explicit `physical_display_timing_observed=false`.

Validate independently with:

```bash
python tools/validate_tutorial_receipt_v34.py \
  "/path/to/v34-tutorial-<session>.json" \
  --expected-commit "$(git rev-parse HEAD)" \
  --require-clean
```

A partial receipt can be inspected with `--allow-partial` but is not successful tutorial
evidence.

## Native bring-up

V0.34 is isolated from the V0.33 native branch so Mac verification can continue without
moving its exact head.

After V0.33 B0 is green, switch to V0.34:

```bash
git switch feat/v34-adaptive-bci-tutorial
bash tools/bootstrap_dragonsouls_chassis.sh --refresh
```

Open the pinned Dragon Souls Unity project and run:

**Mindforge -> World V0.34 -> PLAY ADAPTIVE BCI TUTORIAL**

For zero-hardware gaze bring-up, start in another terminal:

```bash
python tools/mindforge_gaze.py mouse
```

Then press F9. The mouse pointer is only a simulated gaze source and the receipt must keep
that source label. Once gaze profiling/layout freeze reaches the neural-service stage,
start the synthetic EEG path:

```bash
python tools/run_unity_calibrated_decoder.py \
  --stream-name UnicornMock \
  --source-mode synthetic_eeg
```

Run **Mindforge -> World V0.34 -> Audit Adaptive Tutorial** after the session.

## Promotion ladder

The intended evidence ladder is:

- **A0 software:** CI/source contracts only;
- **A1 native tutorial shell:** V0.34 scene builds, F9 stages progress, no unexpected red Console entries;
- **A2 simulated gaze:** mouse/replay produces a gaze profile and frozen layout;
- **A3 synthetic closed loop:** synthetic EEG completes calibration and Sight/Guard practice through the production decoder;
- **A4 movement stress:** at least one neural outcome/abstention while ordinary keyboard/camera input is active;
- **A5 live gaze:** hardware-mapped gaze profile on the target display;
- **A6 live EEG stationary:** real acquisition metadata plus held-out Sight/Guard performance;
- **A7 live multimodal playthrough:** tutorial + authored Sight puzzle + Guard encounter;
- **A8 physical timing:** external display timing measurement under representative gameplay load.

A later gaze+EEG fusion model should be evaluated against the EEG-only baseline as a new
experimental gate. V0.34 intentionally does not smuggle that future result into today's
architecture.
