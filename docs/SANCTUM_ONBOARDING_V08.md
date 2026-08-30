# Sanctum Onboarding V0.8

V0.8 changes Mindforge's first impression from a compressed combat corridor into a bright, spacious **Sanctum initiation sequence**. The reference target is a futuristic sacred city: monumental ivory architecture, restrained gold structure, cyan/verdant signal technology, real traversal clearances, gardens/water and a visible world beyond the starting building.

The change is intentionally layered on V0.5–V0.7 rather than replacing their authorities:

```text
V0.5  one control profile + one contextual E
V0.6  stable world IDs + profile-v2 persistence
V0.7  scalable presentation-only world art
V0.8  opening pacing + sanctum spatial recomposition + participant frequency ranking
```

## Why the old opening failed

The August 30 Unity capture made four problems obvious:

1. **Architecture occupied the movement lane.** Dense posts/frames repeatedly crossed the camera and constrained dodge/jump space.
2. **Combat arrived before comprehension.** The Guardian lost most health within seconds while still learning camera/lock/dodge vocabulary.
3. **Rift Hollows deliberately collapsed distance.** The arena ecosystem added two small floor rushers to the Causeway specifically to destroy the safe ranged rhythm.
4. **The world had no reveal.** The first route read as a dark obstacle tunnel rather than the entrance to a coherent civilization.

V0.8 treats those as one design problem, not four isolated tuning bugs.

## Spatial contract

The new opening follows a simple hierarchy:

### Human scale

- central processional/traversal lane: roughly **10–12 m clear width**;
- major threshold: roughly **12 m usable width**;
- first sentinel court: roughly **30 m × 12.5 m**;
- initiation hall: roughly **30 m × 25 m**;
- decorative pillars and gardens stay outside the central movement thirds;
- decorative rings, arches and distant skyline pieces own no gameplay collision.

These are game-scale targets, not architectural code requirements. The practical test is whether a player can strafe, roll, double-jump, hover, air-dash and read multiple threats without the building competing for the same physical volume.

### Architectural scale

Piers, arches, gallery ribs and signal apparatus frame the human-scale lanes rather than subdividing them. V0.8 uses tall bays at approximately ±11 m from center and reserves the center for navigation/combat readability.

### Monument scale

The player should understand that the starting hall is one room inside a larger civilization. The threshold therefore reveals presentation-only cathedral towers, bridge/canal structure, greenery and distant skyline mass. These forms are deliberately outside the immediate collision volume.

## Visual language

V0.8 supersedes the dark opening palette with:

- `SanctumIvoryV08`
- `SanctumPearlV08`
- `SanctumGoldV08`
- `SanctumBlueGlassV08`
- `SanctumWaterV08`
- `SanctumGardenV08`
- `SanctumSkyV08`

The new materials retain Mindforge's normal/occlusion/metal-response vocabulary but discard the old dark albedo for sanctum surfaces. A procedural blue sky and warmer directional light establish a true daylight environment.

The existing Memory Forge checkpoint remains the one physical checkpoint authority. V0.8 simply builds a bright Forge altar around its existing interaction point.

## First 10–15 minute rhythm

V0.8 defines six monotonic opening phases. They are semantic pacing states, not alternate gameplay engines.

### 1. ARRIVAL

Purpose: orientation and awe.

- no Causeway combat can be reached before the Sanctum threshold;
- player learns movement/camera/jump/hover/dodge in broad safe space;
- Memory Forge and resonance stations are visually obvious;
- enemy projectile speed scale, if any hostile shot exists through debug/edge cases: **0.60×**.

### 2. CALIBRATION

Purpose: introduce the cognitive layer before combat density.

Three visual resonance stations currently represent nominal 8/10/12 Hz candidates. In controller preview, the player can inspect them with the existing contextual `E` router. These short render-frame flashes are **not scientific stimulation evidence**.

A genuine neural session remains owned by `AwakeningCalibrationDirector` and Python processing. Its current qualified causal order stays:

`baseline -> Sight -> Guard -> Python acceptance -> combat authority`

Projectile scale: **0.60×**.

### 3. PRACTICE

Purpose: move through the large threshold and practice traversal in a safe terrace.

Projectile scale: **0.66×**.

### 4. WORLD REVEAL

Purpose: let the player see what Mindforge can become before demanding a fight.

The threshold/terrace exposes gardens, water, bridges and distant cathedral-city forms. This is a narrative/navigation beat rather than a combat escalation.

Projectile scale: **0.70×**.

### 5. FIRST ENCOUNTER

Purpose: one legible combat lesson in a large court.

The two Causeway Rift Hollows are removed from the actual encounter list and destroyed during scene composition. The first fight starts with the existing suspended Null Sentries spread to opposite sides of a broad court. Their slow tracking projectiles are meant to teach read -> move -> close distance.

Projectile scale: **0.74×**.

### 6. RELEASED

Purpose: normal world progression can begin.

Later Market/Court combat remains richer, but hostile projectiles retain a modest global readability reduction for the current build: **0.82×** of authored launch speed.

This is intentionally easier than the old build. We should increase difficulty later by better encounter composition, telegraph overlap and enemy decision-making, not by making the first magenta bolt a rifle round.

## Enemy ecology

The opening no longer uses floor-crawling Rift Hollows. Early combat should prefer silhouettes that feel native to the Guardian's world:

- suspended Null Sentries;
- hovering/standing casters;
- upright Penitent/guardian forms;
- later larger Wardens and aerial threats.

The deeper Menagerie roster remains available after onboarding. V0.8 does not erase the game's difficulty ceiling; it changes when that vocabulary appears.

## Participant-specific BCI calibration

### Boundary

Raw EEG remains in the Python/acquisition side. Unity receives only derived scalar events. V0.8 extends the existing v2 event with optional calibration metadata:

- `stimulus_hz`
- `candidate_rank`
- `selected_sight_hz`
- `selected_guard_hz`

No raw channel/sample arrays are added.

### Frequency ranking

`rank_participant_frequency_pairs(...)` accepts repeated labeled candidate-frequency EEG trials and evaluates every eligible pair using the existing filter-bank CCA decoder.

For each pair it computes:

- artifact/quality-gated usable trial count;
- per-frequency classification accuracy;
- balanced accuracy;
- median true-frequency CCA score;
- median winner margin;
- mean signal-quality score.

The ranking objective weights balanced accuracy most heavily, with bounded contributions from margin, response strength and quality. A candidate needs repeated clean trials for both frequencies and a minimum frequency separation.

The winning pair can be converted to a gameplay `SsvepConfig` with `personalized_ssvep_config(...)`.

### Critical display qualification rule

The algorithm ranks **measured participant response to stimuli that were actually presented**. It does not make an arbitrary nominal frequency render correctly.

Before any pair becomes release/BCI authority on a particular device, validate:

1. actual frame cadence and refresh-rate compatibility;
2. luminance waveform with a photodiode or equivalent timing measurement;
3. timing jitter/dropped-frame behavior under gameplay load;
4. participant comfort;
5. repeatable held-out discrimination performance;
6. decoder thresholds/abstention behavior on that participant.

The 8/10/12 Hz orbs in controller preview are therefore visual demonstrations only until the real stimulus path is bound and measured.

## Controller-only preview vs neural calibration

Controller-only builds must remain playable without a headset. V0.8 handles that honestly:

- inspect any two resonance stations -> threshold may open for game preview;
- marker reason explicitly says `VISUAL_PREVIEW_NOT_NEURAL_EVIDENCE` / `CONTROLLER_PREVIEW_COMPLETE`;
- no `CalibrationReady` neural fact is invented;
- genuine `CALIBRATION_READY` from Python opens the threshold immediately and is persisted separately as neural accepted.

This keeps design iteration fast without contaminating BCI evidence.

## Persistence

V0.8 writes only `profile.*` facts into the existing V0.6 profile-v2 architecture:

- opening phase;
- threshold unlocked;
- whether genuine neural calibration was accepted;
- controller-preview station visits;
- selected Sight/Guard frequencies;
- calibration confidence/quality/id.

`OpeningExperiencePersistenceV08` restores opening phase and physically reopens the existing `JourneyGate` after profile-v2 load. It does not introduce another save file or gate authority.

## Runtime playtest gate

Static CI is necessary but insufficient. Before merging V0.8, rebuild through **Mindforge -> Showcase -> Build + Play Cinematic Showcase** and verify:

1. **Brightness**: sanctuary reads ivory/white, not gray basalt; blue sky is visible where the scene exposes it.
2. **Spawn safety**: no enemy damage while the player is still orienting in the initiation hall.
3. **Spacing**: camera does not repeatedly clip behind pillars in the center lane; roll/double-jump/air-dash are comfortable.
4. **Door scale**: the threshold visually reads as a real monumental doorway and does not pinch the camera/Guardian.
5. **One E**: Forge + resonance stations still produce at most one contextual prompt; Forge wins when overlapping because priority remains 30 vs station 21.
6. **Controller preview honesty**: inspecting two resonance stations opens the threshold without making any UI/log claim of neural calibration success.
7. **Real calibration path**: with the Python service, accepted calibration opens threshold and retains the existing baseline/Sight/Guard marker sequence.
8. **Projectile readability**: first Sentry bolts are substantially slower than the August 30 capture and can be read/dodged by a new player.
9. **No floor rushers**: Causeway Rift Hollows do not spawn/arm in the opening.
10. **First court spacing**: Sentries begin widely separated and do not immediately sandwich the Guardian against architecture.
11. **World reveal**: the player can see a legible larger city/landscape promise through/after the threshold.
12. **Natural layer**: water/gardens improve scale and calm without blocking traversal.
13. **Memory Forge**: bright Forge altar visually matches the existing checkpoint/prompt and remains the sole checkpoint authority.
14. **Persistence**: after Forge rest/restart, threshold/opening phase restore correctly; V0.6 loot/shortcut/shrine idempotence still passes.
15. **V0.5 controls**: T + mouse wheel target cycling and arrow-camera ownership remain unchanged.
16. **Performance**: capture editor/player frame timing through the sanctuary and threshold; do not promote the visuals if lighting/render density breaks the future stimulus timing budget.

## Next world-production tranche

After V0.8 passes runtime playtesting, the next highest-value work is not another input system. It is building the larger world promised by the vista:

- replace distant primitive towers with baked/editable cathedral-city prefabs;
- establish district-specific 20–40 piece modular kits on the V0.7 topology seams;
- create proper exterior roads, bridges, plazas, gardens and vertical circulation;
- add sparse NPCs/social life before increasing enemy density;
- introduce exploration pickups/shrines/dialogue through the existing V0.6 stable-ID architecture;
- profile/qualify a display-timed SSVEP stimulus renderer independently from decorative orb visuals;
- progressively replace early placeholder enemy geometry with coherent Sentinel/Penitent/Warden silhouettes.

The governing experience rule is:

**wonder first -> attunement second -> readable practice third -> combat depth later.**
