# Mindforge V0.13 — Channel Wisp Combat Vertical Slice

## Product thesis

Mindforge should feel like an action game first and a BCI instrument second.

The player owns frame-critical action with hands. The Wisp creates short strategic moments where the player deliberately asks for a neural choice. EEG then chooses between two slower combat transformations:

- **Sight** — offense/exposure. Existing combat code amplifies damage/range and visual blade energy after an accepted Sight event.
- **Guard** — sustain/stability. Existing combat code restores health over the aura window and rewards successful physical counter timing.
- **Concord** — earned by overlapping accepted Sight and Guard windows. It remains a higher-order state that can unlock Twin Eclipse, but the physical release remains conventional input.

The player can always continue moving, aiming, attacking, evading and target-switching while channeling. The game does not slow time for BCI.

## Core loop

1. Fight conventionally and read enemy telegraphs.
2. Create a tactical opening through movement, evade, counter, stagger or distance.
3. Hold **V — Channel Wisp**.
4. The fantasy Wisp continues drifting, while two neutral coded cores materialize and settle into stable camera-relative retinal geometry.
5. After a short settle interval, the cores begin their 10/12-Hz coded luminance window from one shared local phase epoch.
6. EEG may resolve **Sight** or **Guard** early through dynamic stopping. If no acceptable selection arrives by the maximum window duration, the game abstains.
7. An accepted aura changes the next few seconds of combat. An abstention spends nothing.
8. A short cooldown prevents stimulus spam, then ordinary combat continues.

`V` answers **WHEN**. It never encodes **WHICH**.

## Why this is the initial design

Public-data qualification makes several design constraints clear:

- always-listening SSVEP creates an avoidable zero-class/false-activation problem;
- forced-choice accuracy is not equivalent to safe game authority;
- one public Guttmann smoke subject already showed asymmetric 10-vs-12 decoder behavior, so wrong commands must be more expensive than abstentions;
- peripheral visual stimulation and gaze remain confounds to measure, not assumptions to hide;
- the final stimulus must be defined in retinal/display terms, not arbitrary Unity world units;
- covert attention remains experimental until the overt/covert dataset earns that promotion.

Therefore the first game mechanic is **triggered overt SSVEP with graceful abstention**.

## Wisp visual architecture

The Wisp has two deliberately separate visual layers.

### 1. Fantasy shell

The companion may drift, curl, trail, react to combat and carry narrative personality. It is presentation-only.

### 2. Coded cores

The Sight and Guard cores are measurement-facing stimuli. During resonance they use:

- camera-relative placement;
- angular diameter, currently 3 degrees as a tunable starting point;
- angular center-to-center separation, currently 10 degrees as a tunable starting point;
- a small vertical angular offset;
- shared phase start;
- no coded modulation outside the listening window;
- neutral rest luminance during priming/rest.

These are **engineering starting values**, not validated human-performance claims. Physical monitor size, distance, refresh timing, luminance, GPU presentation and individual physiology still require qualification.

## State machine

```text
IDLE
  |
  | hold V and combat target exists
  v
PRIMING (~0.18 s)
  | cores settle at neutral luminance
  v
LISTENING (<=1.25 s)
  | fresh derived neural event only
  |-----------------------------|
  | accepted Sight/Guard        | no acceptable evidence
  v                             v
RESOLVED                     ABSTAINED
  |                             |
  | brief readable feedback     | "signal unclear, no aura spent"
  |-----------------------------|
                v
             COOLDOWN
                |
                v
              IDLE
```

Releasing V, losing the combat target, losing the BCI link or receiving participant-stop ends authority immediately.

## Authority invariants

The resonance system may not:

- move the Guardian;
- rotate the camera;
- create/switch target lock;
- attack;
- parry/counter;
- dash/evade;
- interact;
- spend an aura on timeout;
- replay a selection that Unity had already observed before the current listening epoch.

`DualAuraCombatDirector` now fails closed: `AURA_SELECTED` is ignored unless `WispResonanceWindow.CanAcceptSelection` is true. `AuraBuffController` still performs its own confidence/quality acceptance after that gate.

## Combat identity

### Sight: commit

Sight should reward a player who has created an offensive opening. It should improve the *consequence* of the player's next physical actions, never perform those actions itself.

Current vertical-slice payoffs already support this identity:

- stronger attacks;
- larger cleave reach/arc;
- stronger pulse behavior;
- visible armament amplification;
- realized neural bonus damage telemetry.

Future tuning should favor weak-point readability, poise pressure and spatial reach over simply inflating damage.

### Guard: survive and counter

Guard should reward a player who expects pressure and still executes physical defense correctly.

Current vertical-slice payoffs:

- bounded regeneration;
- extra healing on successful physical projectile counters;
- no automatic block/parry.

Future tuning should favor recovery, poise stability and counter opportunity rather than invulnerability.

### Concord: sequence mastery

Sight then Guard, or Guard then Sight, can overlap to establish Concord. This makes repeated neural choices strategically meaningful while preserving a two-class decoder.

Concord should remain rare enough that the player plans around it. It can empower Twin Eclipse, but `R` remains the explicit physical release.

## Unity test path

### Immediate Editor gameplay test, no hardware

1. Open the normal combat scene.
2. Enter Play Mode and engage an enemy.
3. Hold **V**.
4. Verify the two coded cores appear only during the resonance ritual.
5. During **LISTENING**:
   - press **1** for a Sight gameplay simulation;
   - press **2** for a Guard gameplay simulation;
   - press **0** to simulate abstention.
6. Release V early and verify the window cancels without applying an aura.
7. Let the timer expire and verify `SIGNAL UNCLEAR · NO AURA SPENT`.
8. Keep moving/attacking/evading while V is held and verify those controls remain conventional and responsive.

Editor 1/2/0 simulation is compiled only under `UNITY_EDITOR`. It validates the game loop and presentation, not the BCI pipeline.

### BCI integration test

Run the neural service and send real/replay `mindforge.neural_event.v2` selections. Confirm:

- selections before V are ignored;
- selections received during Priming are not authoritative;
- only sequences newer than the Listening boundary can resolve;
- low-confidence/low-quality selections rejected by `AuraBuffController` leave the window open;
- decoder ABSTAIN resolves gracefully;
- stale/lost link aborts the active window;
- participant-stop dominates everything.

## Instrumentation

Each decision window emits derived game markers:

- `NEURAL_WINDOW_ARMED`
- `NEURAL_WINDOW_LISTENING`
- `NEURAL_WINDOW_RESOLVED`
- `NEURAL_WINDOW_ABSTAINED`
- `NEURAL_WINDOW_ENDED`

The marker carries the window id as `stimulus_epoch`. No raw EEG, eye image or gaze stream crosses this gameplay telemetry boundary.

This gives us the future join key for latency, gaze geometry, decoder evidence and realized gameplay payoff.

## Playtest questions

Do not optimize just for decoder accuracy. For the first Unity playtests record:

1. Does V feel like a tactical commitment or an interruption?
2. Can the player still read enemy attacks during the 1.25-s window?
3. Is 0.18 s enough for the cores to visually settle?
4. Are the cores easy to distinguish without staring at UI labels?
5. Does abstention feel fair rather than broken?
6. Is Sight obviously useful after resolution?
7. Is Guard useful without feeling automatic?
8. Can a player intentionally pursue Concord?
9. How often does the player choose to channel during pressure vs after creating space?
10. Does the Wisp still feel like a companion when the coded cores become technical/stable?

## Next evidence-driven tuning

After this Unity slice is playable, the next changes should be driven by two matrices rather than aesthetics alone:

- public EEG: subject × frequency pair × window length × montage × decoder;
- gameplay: encounter state × channel timing × outcome × abstention/wrong-selection cost × realized payoff.

The design is promoted only when those two matrices agree that the neural mechanic is both physiologically credible and genuinely fun.
