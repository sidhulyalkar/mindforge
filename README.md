# MINDFORGE: The First Guardian

### A BCI action game where your hands fight and your visual attention commands a living soul companion.

> **Target:** BR41N.IO Designers' Hackathon at IEEE SMC 2026  
> **Category:** BCI Gaming  
> **Event:** October 4–5, 2026  
> **Engine:** Unity 2022.3 LTS, pinned to `2022.3.62f3`  
> **Primary BCI:** g.tec Unicorn Hybrid Black  
> **Primary paradigm:** two-target SSVEP / visual evoked potential selection

Mindforge is built around one rule:

> **Hands own precision. The brain owns transformation.**

Movement, aiming, dashing, shooting, cleaving, counters and projectile manipulation remain conventional controls. EEG does not replace the fast control loop. A living **Soul Wisp** adds a slower strategic layer through two coded visual targets:

- **Sight / blue / 10 Hz:** offensive amplification.
- **Guard / green / 12 Hz:** recovery amplification.

Mindforge does not claim to read abstract thoughts such as “damage” or “heal.” The decoder asks a narrower question: **which coded visual target produces the stronger steady-state visual evoked response in posterior EEG?** The game assigns the fantasy meaning.

## The combat loop

```text
enemy attack
   ↓
THREAT
   ↓
near miss / counter / capture
   ↓
RESOURCE
   ↓
Flux / reflected projectile / poise pressure
   ↓
WEAPON
```

The Guardian's manual verbs are:

- **Pulse Shot** — mobile ranged pressure;
- **Rift Cleave** — close-range poise damage and knockback;
- **Phase Dash** — repositioning and near-miss Flux harvesting;
- **Counter Pulse** — a short projectile-reflection window;
- **Gravity Bloom** — spend full Flux to capture hostile projectiles and fire them back.

Sight and Guard remain the **only two neural target classes**.

If independently timed Sight and Guard buffs overlap, **Concord** is established and remains available for a forgiving grace period. Full Flux + Concord + Gravity Bloom becomes **Twin Eclipse**.

```text
Guard accepted
      ↓
Sight accepted while Guard remains
      ↓
CONCORD
      ↓
eyes return to combat
      ↓
dash / counter / build Flux
      ↓
Gravity Bloom
      ↓
TWIN ECLIPSE
```

## Architecture: raw EEG never enters Unity

Mindforge treats the closed loop as two versioned contracts:

```text
                 NeuralEvent v2
Python / neurOS ───────────────► Unity

                  GameMarker v1
Python / neurOS ◄─────────────── Unity
```

`NeuralEvent` carries derived evidence and bounded neural authority. `GameMarker` carries presentation and gameplay facts. **Raw EEG stays outside the game process.**

The inbound path therefore remains:

```text
hardware / replay / neurOS
        ↓
Python acquisition
        ↓
quality + calibration + FBCCA + dwell
        ↓
NeuralEvent v2
        ↓
UDP 127.0.0.1:19742
        ↓
Unity authority boundary
```

Unity publishes semantic consequences such as:

```text
PHASE_DASH
PULSE_SHOT
RIFT_CLEAVE / RIFT_CLEAVE_HIT
COUNTER_PULSE / COUNTER_REFLECT
NEAR_MISS
PLAYER_DAMAGED / BOSS_DAMAGED
GRAVITY_BLOOM_CHARGE / RELEASE
TWIN_ECLIPSE_CHARGE / RELEASE
NEURAL_BUFF_APPLIED
CONCORD_ESTABLISHED
BOSS_PHASE
SIGNAL_BREAK
FLUX_CHANGED
BCI_DEGRADED / BCI_RECOVERED
VICTORY / DEFEAT
```

The marker transport is explicitly non-authoritative. Recorder failure is allowed to lose evidence; it is not allowed to change the fight.

See [`docs/BCI_GAME_PLATFORM.md`](docs/BCI_GAME_PLATFORM.md) for the complete contract and causal model.

## Four workflows to know

Mindforge is designed so development does not begin with electrodes.

### P1 — cold-start Unity qualification

The competition scene is generated, not trusted as a pre-baked artifact. From a clean checkout:

```bash
python tools/run_unity_gate.py
```

The runner locates the pinned Unity editor and executes:

```text
clean checkout
   ↓
Unity import + compile
   ↓
project configuration
   ↓
CompetitionSceneAssembler
   ↓
generated competition scene
   ↓
CompetitionGateValidator
   ↓
experiments/reports/unity-gate1-latest.json
```

A P1 pass must be an **observed Unity result**. Python source tests are not a substitute for a Unity compile.

### P2 — controller-only game qualification

P2 intentionally tests the game with **no BCI authority at all**.

Start the evidence capture first:

```bash
python tools/mindforge_playtest.py --require-terminal
```

Then open the generated competition scene, press Play and press **F8** in the Unity Editor.

Mindforge enters a development-only controller qualification mode that:

- disables the neural receiver;
- disables neural aura authority;
- disarms neural-link contingency;
- opens the real Fractured Signal arena;
- leaves a persistent `P2 CONTROLLER-ONLY · BCI DISABLED` label;
- emits `QUALIFICATION_MODE / CONTROLLER_ONLY_NO_BCI` to the GameMarker stream.

The bootstrap is compiled only for the Unity Editor or a Development Build. It is absent from release player builds.

A Development Build may also request the mode explicitly with:

```text
-mindforge-controller-only
```

or environment variable:

```text
MINDFORGE_CONTROLLER_ONLY=1
```

Each capture produces a small evidence bundle:

```text
experiments/playtests/<UTC stamp>/
├── markers.jsonl
├── encounter.json
└── capture.json
```

`capture.json` records the Unity session ID, marker count, terminal outcome status, stop reason, Git head when available and SHA-256 of the marker stream. `encounter.json` summarizes game-design facts such as counter conversion, near misses, damage pressure, Signal Break cadence, Bloom/Twin Eclipse usage and BCI degradation.

There is deliberately no synthetic “fun score.” Metrics diagnose; playtesting judges.

### P3 — no-headset neural authority

The simulated-decision source now participates in the real Awakening handshake instead of bypassing it:

```bash
python tools/mindforge_dev.py decision --calibrate \
  --script sight:3,abstain:1,guard:3,lost:1,recovered:1,sight:3,guard:3 \
  --hz 4 \
  --output-tape experiments/tapes/p3-neural.jsonl
```

The source is explicitly labelled `simulated_decision`. It is useful for testing authority, latency, abstention, loss/recovery, Concord timing and game fairness. It is **not EEG evidence**.

For manual S0 development, Unity Q/E produces only non-authoritative manual intent. One Python service owns calibration, liveness, sequencing and the sole authoritative NeuralEvent stream:

```bash
python tools/mindforge_dev.py manual-service
```

This prevents two independent producers from accidentally competing over NeuralEvent sequence authority.

### P4 — deterministic replay

Mindforge records conventional input on the authoritative 120 Hz simulation tick using `mindforge.guardian_input_tape.v1`.

A Development Player can record with:

```text
-mindforgeInputMode record
```

and replay with:

```text
-mindforgeInputMode replay -mindforgeInputTape <path>
```

Replay exhaustion fails neutral. It never silently falls back to live controls.

Neural authority can be replayed separately through the same production UDP boundary:

```bash
python tools/mindforge_dev.py replay experiments/tapes/p3-neural.jsonl --speed 1.0
```

Then compare the semantic GameMarker consequence streams:

```bash
python tools/mindforge_qualify.py compare-markers \
  experiments/markers/reference.jsonl \
  experiments/markers/replay.jsonl \
  --output experiments/reports/replay-comparison.json \
  --enforce
```

Timestamps, session IDs and transport sequence numbers may differ. Gameplay semantics must match exactly. Similarity is diagnostic only and cannot create a pass.

## Development realities

The same Unity authority boundary can be driven by increasingly realistic sources:

| Level | Source | What is substituted |
|---|---|---|
| S0 | `manual` | human intent mapped to derived neural authority by an explicit dev service |
| S1 | `simulated_decision` | decoder output |
| S2 | `decision_replay` | recorded decoder output |
| S3 | `eeg_replay` | recorded EEG through the production decoder |
| S4 | `synthetic_eeg` | neurOS participant/sensor/fault world |
| S5 | `live` | physical participant + headset |

These labels are evidence boundaries. S1 is not synthetic EEG. S4 is not human evidence.

## Defensive neural authority

The initial engineering configuration is intentionally conventional and inspectable:

```text
sampling rate          250 Hz
analysis window        1.25 s
Sight target           10 Hz
Guard target           12 Hz
harmonics              3
filter-bank CCA        FBCCA
posterior decoder      Pz / PO7 / Oz / PO8
quality authority      full 8-channel montage
stable dwell           2 accepted windows
```

Default Unicorn-like montage:

```text
Fz C3 Cz C4 Pz PO7 Oz PO8
```

The quality layer conservatively flags engineering failure signatures such as saturation, disconnected channels, common-mode transients, extreme derivatives and broad high-frequency contamination. These are engineering suspicion flags, not physiological diagnoses.

Suspicious or ambiguous evidence yields `ABSTAIN`. No guessed brain button is emitted.

`BCI_HEARTBEAT` is separate from `ABSTAIN`: transport liveness is not presented as classifier evidence.

`NeuralEvent v2` also carries provenance and freshness fields including:

```text
session_id
calibration_id
source_sample_start
source_sample_end
decoder_time_ns
authority_ttl_ms
```

Unity evaluates selection TTL using the **Unity-process packet receive clock**. Independent process monotonic clocks are never subtracted from one another.

## Two clocks: combat crunch without corrupting the stimulus

Combat targets a 120 Hz fixed simulation while the visual stimulus uses real/unscaled time.

```text
light impact       20 ms
Counter Pulse      20 ms
Rift Cleave        55 ms
Signal Break       80 ms
Twin Eclipse      120 ms
```

`HitStopController` owns one extendable realtime freeze window. VEP phase continues through combat freezes.

Each aura is split into:

```text
Aura Root
├── coded VEP core
└── non-coded feedback shell / fantasy presentation
```

The coded core does not react to classifier score, quality, damage, Flux, camera shake or hit-stop. The shell may communicate state with slower non-periodic presentation.

Short haptic echoes happen only **after** accepted neural decisions. Continuous rumble during evidence accumulation is excluded.

## The Fractured Signal

The reference competition encounter has three readable phases rather than a single escalating projectile soup.

**Phase I — Warm-up.** Predictable aimed fans and radial patterns teach movement, counters and Wisp cadence.

**Phase II — Attention split.** Fractured Echo nodes add spatial priorities and Flux opportunities.

**Phase III — Controlled overload.** Crossfire and heavy attacks combine learned threat grammars while preserving telegraph readability.

**Signal Break.** Poise collapse creates a short relief/punish window, rests VEP modulation at steady luminance and visually resets the fight.

## neurOS is the wind tunnel

Mindforge uses neurOS for simulation, perturbation, replay and qualification rather than as a frame-by-frame game dependency.

```text
neurOS synthetic participant / EEG
        ↓
LSL UnicornMock
        ↓
Mindforge quality + FBCCA + dwell
        ↓
NeuralEvent v2
        ↓
Unity
        ↓
GameMarker v1
        ↓
qualification evidence
```

Phantom can attack the loop with weak responders, endogenous alpha, blinks, movement/controller contamination, channel degradation, saturation, dropout, jitter, dropped chunks, source silence and recovery.

Synthetic success is used to falsify assumptions before real sessions. It is not human physiological evidence.

## Promotion ladder

```text
P0  software contracts + exact-head CI artifact
 ↓
P1  clean-checkout Unity import/compile/scene assembly
 ↓
P2  controller-only full encounter
 ↓
P3  simulated_decision → real Awakening → Unity
 ↓
P4  conventional + neural replay reproduction
 ↓
P5  neurOS synthetic EEG → production decoder → Unity
 ↓
P6  forced render/network fault rehearsal
 ↓
P7  measured physical display timing
 ↓
P8  real Unicorn acquisition metadata/units
 ↓
P9  stationary Sight vs Guard
 ↓
P10 moving selection
 ↓
P11 selection while player moves
 ↓
P12 light combat
 ↓
P13 full Fractured Signal encounter
```

Promotion is monotonic. A green software gate does not imply physical display evidence. Synthetic EEG does not imply human SSVEP performance.

## Current claim boundary

The repository-level software architecture and tests can be verified in CI, which emits an exact-head `mindforge.software_gate.v1` artifact.

Until separately observed, Mindforge does **not** claim:

- successful Unity Editor/Player compile for a new head merely because Python CI is green;
- physically measured 10/12 Hz luminance timing;
- verified Unicorn metadata/units on the competition machine;
- human SSVEP performance;
- human full-combat BCI performance;
- final production art/audio quality.

Those are evidence gates, not README optimism.

## Development priority

The architecture is now intentionally constrained. Do **not** add another neural target class, motor-imagery locomotion, P300 menus, emotion recognition, foundation-model inference in the control path, VR dependencies or generalized plugin machinery before the reference loop is proven.

The highest-value sequence is:

1. pass P1 on the pinned Unity editor;
2. run repeated P2 controller-only encounters and improve movement, telegraphs, counter feel, hit confirmation, audio and boss choreography;
3. stress the polished fight with P3 simulated neural uncertainty;
4. make P4 reproduction boringly deterministic;
5. attack the loop with neurOS at P5;
6. then spend scarce headset/human time on P6–P13.

The deeper roadmap lives in [`docs/DEVELOPMENT_ROADMAP.md`](docs/DEVELOPMENT_ROADMAP.md). Scene assembly details are in [`docs/UNITY_SCENE_WIRING.md`](docs/UNITY_SCENE_WIRING.md), and the Phantom path is documented in [`docs/PHANTOM_UNICORN_LAB.md`](docs/PHANTOM_UNICORN_LAB.md).

## North star

Mindforge should not be remembered as a game controlled badly by EEG.

It should demonstrate a stronger possibility:

> **A fast physical action game can remain responsive and expressive while uncertain neural attention controls a slower strategic layer that ordinary input does not replicate.**

The hands fight the enemy. The Soul Wisp turns visual attention into power. The platform makes clear why the BCI was allowed to do what it did.
