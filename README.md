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

Then enter controller-only mode explicitly with **F8** in the Unity Editor or an explicit development launch flag. The resulting run is stamped `CONTROLLER_ONLY_NO_BCI`; no decoder or calibration claims are implied.

### P3/P4 — decision simulation and replay

Use the semantic decision tools without EEG:

```bash
python tools/mindforge_dev.py --source simulated_decision --duration 30 --seed 7
python tools/mindforge_dev.py --source decision_replay --replay examples/decision_replay.jsonl --duration 30
```

### P5 — synthetic EEG through the real decoder

Use neurOS synthetic EEG or an evidence replay through the real Python decoder path, then compare accepted decisions to Unity's semantic consequences.

The promotion ladder deliberately separates software correctness, Unity correctness, game feel, decoder semantics and real BCI evidence.

## Repository map

```text
mindforge/
├── docs/                 # contracts, roadmap, qualification and design notes
├── experiments/          # evidence + generated reports
├── mindforge_neuro/      # decoder / NeuralEvent protocol
├── tools/                # dev, qualification, replay and reporting tools
├── unity/                # Unity 2022.3 project
└── web_demo/             # browser-side combat reference modules
```

## Current Unity showcase

For the current third-person controller-only vertical slice, use:

**Mindforge → Showcase → Build + Play Cinematic Showcase**

The current route is:

```text
Listening Cavern
    ↓
Ruined House
    ↓
Cellar
    ↓
Signal Warden
    ↓
Fractured Signal Arena
```

See [`docs/UNITY_SHOWCASE.md`](docs/UNITY_SHOWCASE.md) for the current controls, route, visual expectations and real-Unity acceptance checklist.

## Scientific scope

The game currently freezes neural authority to two target classes. Any new gaze mechanics, target-lock presentation or world-space placement may reposition the coded visual targets, but it must not silently expand the decoder's semantic authority or change their coded frequencies.

## Status discipline

Source tests are evidence for software contracts only. Unity import/compile, generated-scene validation, controller-only play, synthetic EEG, real hardware and human BCI sessions are separate qualification gates. Do not collapse those into one claim.
