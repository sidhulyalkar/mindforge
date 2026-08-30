# Mindforge V0.11: Gaze Attention Platform

## Decision

Mindforge should **not** pivot from BCI into an eye-tracking-only game.

The strongest initial product is a **gaze-first hybrid**:

- **controller / keyboard:** frame-critical movement, attacks, parries, dash, explicit confirmation;
- **gaze:** continuous spatial attention, target preference, context preference, tutorial/readability evidence;
- **BCI:** slower strategic transformations where neural evidence is meaningfully different from another button.

In shorthand:

> **Eyes answer WHERE. Hands answer NOW. Brain answers WHICH MODE / TRANSFORMATION.**

This gets a real sensor into the game much earlier than a full EEG stack without throwing away the scientific architecture that makes Mindforge distinctive.

## Why gaze first for the next vertical slice

The current BCI platform deliberately keeps aiming, movement, dash, and frame-critical counters conventional because neural evidence accumulates too slowly and can abstain. That is the right constraint.

Gaze has the complementary shape:

- continuous and spatial;
- naturally aligned with enemies, interactables, weak points, landmarks, HUD elements, and world regions;
- easy to simulate and replay;
- immediately observable by a player and a developer;
- useful even before it is allowed to influence gameplay authority.

That makes gaze a much better **gamebuilding-engine sensor** than replacing the existing Sight/Guard neural mechanics.

## Relevant Pupil Labs reference implementations

### 1. `pupil-labs/real-time-screen-gaze`

Repository: <https://github.com/pupil-labs/real-time-screen-gaze>

This is the closest match for desktop Mindforge. Pupil Labs maps Neon scene-camera gaze onto a physical monitor by detecting AprilTags around a planar screen. Their README uses:

- `pupil_labs.realtime_api.simple.discover_one_device`;
- `GazeMapper(device.get_calibration())`;
- four or more AprilTags with known screen coordinates;
- `receive_matched_scene_video_frame_and_gaze()`;
- `gaze_mapper.process_frame(...)` to recover screen-space gaze.

Mindforge V0.11 follows that public API shape in `tools/mindforge_gaze.py neon-screen` but does not vendor Pupil Labs code.

### 2. `pupil-labs/gaze-control`

Repository: <https://github.com/pupil-labs/gaze-control>

Pupil Labs' assistive gaze-control application demonstrates the central interaction lesson for games: direct gaze-to-click suffers from accidental activation, so selection is stabilized with dwell. Mindforge adopts the same principle but keeps an additional explicit player confirmation for combat target lock.

### 3. `pupil-labs/neon-xr`

Repository: <https://github.com/pupil-labs/neon-xr>

The current Neon XR Core package exposes a `GazeDataProvider` with `RawGazePoint`, `RawGazeDir`, a world-space `GazeRay`, eye-state availability, and a `gazeDataReady` Unity event. Its `NeonGazeDataProvider` discovers hardware and turns mapped gaze into a calibrated gaze direction.

This is the right future adapter for Quest/Pico XR, but it should remain optional because the Neon XR Core package brings Unity Addressables, Input System, and XR Interaction Toolkit dependencies that the current desktop Mindforge project does not otherwise require.

## V0.11 closed-loop architecture

```text
                    DEVELOPMENT / HARDWARE SOURCES

 mouse pointer ───────────────┐
 replay tape ─────────────────┤
                             │
 Neon desktop                 │
   scene video + gaze         │
          ↓                   │
 Pupil Real-Time API          │
          ↓                   │
 AprilTag screen mapper ──────┤
                             │       future
 Neon XR GazeRay ─────────────┼──────── adapter
                             ↓
                      GazeEvent v1
                    loopback UDP 19746
                             ↓
                    UdpGazeReceiver
                    latest-only / bounded
                             ↓
                    GazeAttentionRouter
                  raycast + dwell stability
                             ↓
                  semantic attended target
                             ↓
        ┌────────────────────┴────────────────────┐
        │                                         │
 player presses T                          passive evidence
        │                                HUD / telemetry / QA
        ↓
 GuardianTargetLock acquires
 conventional player-owned lock
        ↓
 GazeTargetLockAssist refines
 target preference on same T frame
        ↓
 existing combat system
```

## Authority rules

Gaze is initially **advisory, not authoritative**.

V0.11 gaze code must never call or synthesize:

- attack / damage;
- dash / locomotion;
- shield or parry timing;
- contextual interaction;
- BCI state mutation;
- target-lock confirmation without the existing target-lock key edge.

The `GazeTargetLockAssist` runs after the ordinary target-lock component. If the player has just pressed `T` and a stable gaze target exists, it refines the newly created conventional lock toward that target using the lock component's existing public cycle operation. If `T` released an existing lock, gaze does nothing.

This is deliberately conservative. It creates a perceptible gameplay benefit without the eye-tracking "Midas touch" problem where merely looking at an object accidentally activates it.

## GazeEvent v1 boundary

`contracts/gaze_event.v1.schema.json` contains only derived game-space evidence:

```text
schema
seq
source_mode
timestamp_ns
x, y                 normalized [0, 1]
confidence            normalized [0, 1]
fixation
worn
coordinate_origin     top_left | bottom_left
surface
```

Raw eye images, raw scene video, vendor buffers, and detailed eye-state biometrics stay outside Unity.

`timestamp_ns` is provenance only. Unity never subtracts a source-process timestamp from its own realtime clock. Packet freshness is measured from local socket receive time, matching the existing NeuralEvent transport discipline.

## Desktop Neon bring-up

Install the optional hardware dependencies in a separate environment:

```bash
pip install "pupil-labs-realtime-api>=1.1.0" real_time_screen_gaze
```

Then run Mindforge borderless or full-screen on the primary display and start:

```bash
python tools/mindforge_gaze.py neon-screen
```

The bridge:

1. creates four unique AprilTags in small top-most windows;
2. discovers one Neon Companion device on the local network;
3. gets the scene-camera calibration;
4. defines the display as a tracked Pupil surface;
5. receives matched scene frames + gaze;
6. maps gaze into screen pixels;
7. normalizes to `[0, 1]`;
8. emits `GazeEvent v1` on loopback UDP `19746`.

All four markers must remain visible to the glasses' scene camera. For a first validation, use one monitor and 100% OS scaling where practical; multi-monitor and scaled-window mapping should be qualified separately rather than assumed.

## Zero-hardware development

### Mouse-as-gaze

```bash
python tools/mindforge_gaze.py mouse
```

Move the pointer over different enemies and press `T`. The target lock should prefer the enemy under the simulated gaze after the dwell threshold.

### Fixed point

```bash
python tools/mindforge_gaze.py point --x 0.5 --y 0.5
```

Useful for deterministic center-screen qualification.

### Replay

```bash
python tools/mindforge_gaze.py replay experiments/gaze/session.jsonl
```

Replay creates fresh sequence identity and declares `source_mode=gaze_replay`.

## What to build next

### V0.11A: qualify target preference

Acceptance gate:

- no gaze stream => existing controls are bit-for-bit behaviorally unchanged;
- gaze stream => development HUD shows a fresh point/source;
- dwell on enemy A, press `T` => A becomes the player-owned target when reachable;
- look between enemies without `T` => no lock changes;
- gaze disconnect => suggestion expires within the configured timeout;
- malformed, stale, out-of-order packets cannot affect gameplay;
- mouse simulation, replay, and live Neon use the same Unity path.

### V0.11B: gaze-aware contextual interaction

Reuse the single contextual `E` router. Gaze may break ties among already valid nearby interactables, but `E` remains the authority edge. This is a particularly strong fit for Memory Forge, mounts, loot, shrines, NPCs, and dense worldbuilding.

### V0.11C: BCI + gaze composition

The most distinctive mechanic is not "look to shoot." It is **neural mode + spatial attention**:

- BCI **Sight** establishes a slower neural state;
- gaze chooses the enemy, weak point, glyph, or hidden object within that state;
- sustained attention reveals or charges information rather than dealing automatic damage;
- conventional input executes any frame-critical action.

Similarly, BCI **Guard** can modify defensive state while gaze tells the game which threat the player is tracking. This creates a multimodal grammar rather than three competing control schemes.

### V0.11D: gamebuilding / UX analytics

Gaze can also accelerate development itself. Record semantic AOIs instead of raw video:

- first-fixation latency on enemies and telegraphs;
- dwell on tutorial prompts;
- missed landmarks / exits;
- attention before damage events;
- target-switch hesitation;
- boss weak-point discovery;
- whether the player actually looks at a new mechanic before being punished by it.

Those measures can become automated playtest evidence for scene composition and encounter readability.

## Product framing

The strongest near-term pitch is:

> **Mindforge is a multimodal game engine where conventional controls own precision, gaze supplies continuous spatial attention, and BCI supplies slower high-level cognitive transformations. Every modality is replayable, provenance-labeled, and prevented from claiming authority it did not earn.**

That is both easier to demonstrate than an EEG-only game and more technically differentiated than a conventional eye-controlled game.
