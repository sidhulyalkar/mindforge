# Mindforge BCI Gameplay Design

The BCI layer should feel like a coherent semantic system inside the game, not a collection of raw decoder outputs mapped directly onto arbitrary buttons.

## Stable semantic interface

Mindforge keeps three primary intents:

- Sight
- Guard
- Concord

The intended pipeline is:

`signal -> decoder -> confidence -> semantic intent -> gameplay adapter`

Raw EEG never directly calls Unity movement, damage, attack or scene-transition methods.

## Development simulation

The V0.31 orb currently renders requested temporal stimuli:

- Sight: 8 Hz
- Guard: 10 Hz
- Concord: 12 Hz

These values are requested simulation frequencies. They are not yet measured optical presentation frequencies.

Actual stimulus timing depends on:

- monitor refresh rate;
- Unity frame cadence;
- compositor behavior;
- dropped/duplicated frames;
- display pixel response.

Physical qualification should eventually use frame telemetry and a photodiode.

## Presentation safety boundary

The development orb defaults to reduced luminance modulation and allows temporal modulation to be paused with B while leaving the interface visible.

Reduced contrast is not a safety guarantee. High-contrast flicker should never become the default showcase mode.

## V0.32 reveal pacing

The orb should not be visible from the first frame of the game.

V0.32 starts with it hidden during Awakening, then reveals it at the dedicated `BciReveal` chapter beat. That lets the player first understand ordinary movement/combat before adding a second perceptual language.

## Sight

Core meaning: information.

Progression:

1. highlight a signal-bearing object;
2. reveal enemy weakpoint / safe boss window;
3. reveal hidden traversal or secret path;
4. prioritize meaningful targets in visually complex encounters.

Showcase micro-puzzle target:

The player reaches a chamber with multiple plausible paths or interactable structures. Sight reveals one true neural resonance, such as:

- a hidden bridge;
- a weak architectural seam;
- a glyph sequence;
- an ambush indicator;
- a secret side chamber.

The mechanic must expose useful information, not simply recolor the screen.

## Guard

Core meaning: stabilization.

Progression:

1. visualize a defensive resonance window;
2. temporary stabilization against a clearly telegraphed hazard;
3. environmental hazard resistance;
4. defensive counter opportunity.

Guard should not duplicate the dodge roll. Roll is active locomotor avoidance; Guard should alter the interpretation or stability of specific neural hazards.

## Concord

Core meaning: synchronization.

Progression:

1. synchronize with shrine / world device;
2. align a mechanism or route;
3. influence enemy/device state;
4. manipulate boss/world-state rules.

Potential Fractured Signal use:

Fracture nodes appear in the final phase. Concord can stabilize or synchronize a node, creating a temporary damage opening. A controller-simulated intent should use the exact same semantic adapter as a future decoded intent.

## Confidence handling

Future gameplay adapters should receive at least:

- intent;
- confidence;
- source;
- timestamp;
- decoder/session identity if relevant.

Avoid binary actions from low-confidence single samples.

Possible policy:

- below threshold: no gameplay action;
- medium confidence: preview / pre-highlight;
- high confidence: confirm semantic action;
- repeated conflicting intents: decay / abstain rather than oscillate world state.

## Combat coexistence

BCI should complement ordinary controller/mouse combat rather than consume every action.

Good BCI roles:

- expose information;
- alter a world-state window;
- stabilize a hazard;
- synchronize a device;
- open an optional tactical advantage.

Poor early BCI roles:

- direct analog movement;
- every sword swing;
- frame-critical dodge timing;
- camera rotation;
- raw target selection without confidence gating.

## Scientific qualification ladder

The game should distinguish increasingly strong claims:

1. visual simulation exists;
2. semantic controller-simulated intent reaches Unity;
3. replayed decoder decisions reproduce semantics;
4. synthetic EEG reaches production decoder + Unity;
5. display timing is physically measured;
6. acquisition metadata/units are validated;
7. stationary human intent separation is observed;
8. moving selection is observed;
9. selection while player moves is observed;
10. light-combat BCI is observed;
11. full boss encounter is observed.

Do not collapse these into one vague "BCI works" claim.

## Logging

For meaningful BCI experiments, log:

- requested stimulus frequencies;
- render-frame timestamps;
- semantic intent events;
- confidence;
- player movement state;
- combat state;
- target state;
- encounter phase;
- dropped-frame / performance signals;
- future acquisition sample timestamps.

This allows later analysis of whether combat motion or visual load degrades selection accuracy.
